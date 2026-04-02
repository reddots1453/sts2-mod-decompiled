# STS2 Community Stats — 完整部署指南

本文档涵盖让 Mod 达成全部预期功能所需的**所有工作**，包括服务器搭建、数据库设计、API 实现、预计算管线、运维监控，以及客户端 Mod 的最终打包发布。

**本指南附带一个完整可运行的服务端项目**，位于 `server/` 目录下。

---

## 目录

1. [架构总览](#1-架构总览)
2. [项目结构](#2-项目结构)
3. [本地开发环境](#3-本地开发环境)
4. [生产服务器部署 (VPS)](#4-生产服务器部署-vps)
5. [数据库详解](#5-数据库详解)
6. [Redis 缓存层](#6-redis-缓存层)
7. [API 服务详解](#7-api-服务详解)
8. [预计算管线](#8-预计算管线)
9. [Nginx 反向代理与 HTTPS](#9-nginx-反向代理与-https)
10. [版本管理与 ID 迁移](#10-版本管理与-id-迁移)
11. [客户端 Mod 发布](#11-客户端-mod-发布)
12. [运维与监控](#12-运维与监控)
13. [安全与反滥用](#13-安全与反滥用)
14. [扩容指南](#14-扩容指南)
15. [故障排除](#15-故障排除)
16. [成本估算](#16-成本估算)
17. [开发排期](#17-开发排期)

---

## 1. 架构总览

```
┌──────────────────┐         HTTPS          ┌─────────────────────────────────┐
│  STS2 Game       │ ◄─────────────────────► │  Nginx (TLS termination)       │
│  + Mod DLL       │   POST /v1/runs         │  - rate limiting               │
│                  │   GET  /v1/stats/*       │  - gzip                        │
│  ┌──────────┐    │                         │  - SSL (Let's Encrypt)         │
│  │StatsProvider│  │                         └──────────┬──────────────────────┘
│  │ApiClient  │   │                                    │ :8080
│  │OfflineQueue│  │                         ┌──────────▼──────────────────────┐
│  └──────────┘    │                         │  FastAPI (Python 3.12)          │
└──────────────────┘                         │  - Upload ingestion             │
                                             │  - Stats query (cache-first)    │
                                             │  - APScheduler (precompute)     │
                                             └──┬────────────────┬─────────────┘
                                                │                │
                                         ┌──────▼──────┐  ┌─────▼────────┐
                                         │ PostgreSQL   │  │    Redis     │
                                         │ (持久存储)    │  │ (预计算缓存)  │
                                         │              │  │              │
                                         │ 9 张核心表   │  │ TTL 15 min   │
                                         └─────────────┘  └──────────────┘
```

**数据流**：

| 场景 | 流程 | 延迟 |
|------|------|------|
| 上传 Run | Game → Nginx → FastAPI → PostgreSQL (事务写入) | ~50ms |
| 查询统计 | Game → Nginx → FastAPI → Redis → 返回 JSON | ~5ms (命中) |
| 缓存未命中 | Game → Nginx → FastAPI → PostgreSQL → Redis → 返回 | ~200ms |
| 预计算 | APScheduler → PostgreSQL 聚合 → Redis 写入 | 每 10 分钟 |

---

## 2. 项目结构

```
server/
├── docker-compose.yml         # 一键部署编排
├── Dockerfile                 # API 服务镜像
├── .env.example               # 环境变量模板
│
├── app/                       # FastAPI 应用
│   ├── __init__.py
│   ├── main.py                # 入口: 路由、中间件、生命周期
│   ├── config.py              # 配置 (从环境变量读取)
│   ├── models.py              # Pydantic 数据模型 (与 C# ApiModels 对应)
│   ├── database.py            # asyncpg 连接池管理
│   ├── cache.py               # Redis 连接 + 缓存键设计
│   ├── ingest.py              # Run 上传 → 拆分写入各表
│   ├── aggregation.py         # SQL 聚合查询 → BulkStatsBundle
│   ├── precompute.py          # 定时预计算 worker
│   ├── id_migration.py        # 跨版本 ID 迁移
│   └── requirements.txt       # Python 依赖
│
├── sql/
│   ├── 001_init.sql           # 建表脚本 (自动执行)
│   └── 002_maintenance.sql    # 定期维护 SQL
│
├── nginx/
│   └── default.conf           # Nginx 反向代理配置
│
└── scripts/
    ├── setup_server.sh        # VPS 一键初始化脚本
    └── seed_test_data.py      # 测试数据生成器
```

---

## 3. 本地开发环境

### 3.1 前置条件

- Docker Desktop (Windows/Mac) 或 Docker Engine (Linux)
- Python 3.11+ (用于本地运行种子脚本)
- Git

### 3.2 一键启动

```bash
cd mods/sts2_community_stats/server

# 创建环境文件
cp .env.example .env
# 编辑 .env，设置 DB_PASSWORD

# 启动所有服务
docker compose up -d --build

# 查看日志
docker compose logs -f api
```

### 3.3 验证服务

```bash
# 健康检查
curl http://localhost:8080/health
# 预期输出: {"status":"healthy","db":"ok","redis":"ok"}

# 查看空的 bulk stats
curl "http://localhost:8080/v1/stats/bulk?char=IRONCLAD&ver=v0.99.1"
# 预期输出: {"generated_at":"...","total_runs":0,"cards":{},...}
```

### 3.4 注入测试数据

```bash
# 安装 httpx (一次性)
pip install httpx

# 生成 200 条模拟 Run 数据
python scripts/seed_test_data.py --runs 200 --api-url http://localhost:8080

# 等待 ~10 秒让预计算运行，然后验证
curl "http://localhost:8080/v1/stats/bulk?char=IRONCLAD&ver=v0.99.1" | python -m json.tool
```

### 3.5 Mod 连接本地服务器

编辑 `mods/sts2_community_stats/config.json`:

```json
{
  "api_base_url": "http://localhost:8080/v1"
}
```

启动游戏 → Mod 会自动连接本地 API。

### 3.6 停止/重置

```bash
# 停止服务 (保留数据)
docker compose down

# 停止并删除所有数据 (重置)
docker compose down -v
```

---

## 4. 生产服务器部署 (VPS)

### 4.1 推荐配置

| 阶段 | 日活玩家 | VPS 配置 | 预估月成本 |
|------|----------|-----------|-----------|
| 起步 | <1,000 | 2 核 4GB RAM / 40GB SSD | $10-25/月 |
| 增长 | 1K-10K | 4 核 8GB RAM / 80GB SSD | $40-80/月 |
| 规模 | 10K+ | 专用 DB + 读副本 + 负载均衡 | $200+/月 |

**推荐云服务商**（按性价比排序）：
- **Hetzner** — 欧洲/美国，2C4G 约 €4.5/月，性价比极高
- **Vultr / DigitalOcean** — 全球节点，$10-20/月
- **Railway / Fly.io** — 免运维 PaaS，适合起步
- **AWS Lightsail** — $10/月起，按需扩容到 EC2

### 4.2 手动部署步骤 (推荐 Ubuntu 22.04+)

#### 第一步：购买 VPS 并配置 SSH

```bash
# 在本地生成 SSH 密钥 (如果没有)
ssh-keygen -t ed25519 -C "sts2stats"

# 创建 VPS 时上传公钥，或之后复制:
ssh-copy-id root@YOUR_SERVER_IP

# 登录
ssh root@YOUR_SERVER_IP
```

#### 第二步：配置域名 DNS

在域名注册商处添加 A 记录：
```
api.sts2stats.com  →  YOUR_SERVER_IP
```

等待 DNS 生效（通常 5 分钟，最长 48 小时）。验证：
```bash
dig api.sts2stats.com +short
# 应返回服务器 IP
```

#### 第三步：上传代码并运行安装脚本

```bash
# 在本地执行: 上传 server/ 目录到 VPS
rsync -avz --progress \
    mods/sts2_community_stats/server/ \
    root@YOUR_SERVER_IP:/opt/sts2stats/

# SSH 到服务器
ssh root@YOUR_SERVER_IP

# 执行安装脚本
cd /opt/sts2stats
chmod +x scripts/setup_server.sh
bash scripts/setup_server.sh api.sts2stats.com admin@youremail.com
```

脚本会自动完成：
1. 安装 Docker、Nginx、Certbot
2. 生成强密码并写入 `.env`
3. 申请 Let's Encrypt SSL 证书
4. 启动 Docker Compose 全部服务
5. 运行健康检查
6. 配置定时备份和维护任务

#### 第四步：验证部署

```bash
# 在服务器上
curl -sf https://api.sts2stats.com/health
# {"status":"healthy","db":"ok","redis":"ok"}

# 在本地
curl https://api.sts2stats.com/v1/stats/bulk?char=IRONCLAD&ver=v0.99.1

# 注入测试数据
python scripts/seed_test_data.py --runs 50 --api-url https://api.sts2stats.com
```

#### 第五步：修改 Mod 指向生产服务器

编辑 `src/Config/ModConfig.cs`:
```csharp
public static string ApiBaseUrl { get; set; } = "https://api.sts2stats.com/v1";
```

重新编译:
```bash
cd mods/sts2_community_stats
dotnet build -c Release
```

### 4.3 一键脚本部署 (适合快速搭建)

如果不想手动操作，安装脚本 `scripts/setup_server.sh` 封装了完整流程：

```bash
# 语法: setup_server.sh <域名> <邮箱> [数据库密码]
bash scripts/setup_server.sh api.sts2stats.com admin@example.com MyStr0ngP@ss

# 密码不提供时会自动生成随机密码
```

---

## 5. 数据库详解

### 5.1 表结构概览

| 表名 | 用途 | 预计行增长 |
|------|------|-----------|
| `runs` | 每局游戏主记录 | 1 行/run |
| `card_choices` | 卡牌选择记录 | ~20 行/run |
| `event_choices` | 事件选项选择 | ~5 行/run |
| `relic_records` | 最终遗物列表 | ~8 行/run |
| `encounter_records` | 遭遇战记录 | ~15 行/run |
| `contributions` | 战斗贡献归因 | ~50 行/run |
| `shop_purchases` | 商店购买 | ~3 行/run |
| `card_removals` | 卡牌移除 | ~2 行/run |
| `card_upgrades` | 卡牌升级 | ~5 行/run |
| `game_versions` | 游戏版本注册 | 极少 |
| `id_migrations` | ID 跨版本迁移 | 手动维护 |

**关键设计**: 所有子表都包含 `run_id` 外键引用 `runs(id) ON DELETE CASCADE`，便于整局删除。同时冗余了 `character`、`game_version`、`ascension` 字段以避免 JOIN。

### 5.2 索引策略

```sql
-- 按查询维度建立复合索引 (character, game_version, ascension, entity_id)
-- 这是聚合查询最频繁的 WHERE 条件组合
CREATE INDEX idx_cc_stats ON card_choices (character, game_version, ascension, card_id);
CREATE INDEX idx_ec_stats ON event_choices (character, game_version, event_id);
CREATE INDEX idx_rr_stats ON relic_records (character, game_version, relic_id);
CREATE INDEX idx_er_stats ON encounter_records (character, game_version, encounter_id);
```

### 5.3 数据量估算

| 日活玩家 | 预估日 Run 数 | 日新增行数 | 月数据增长 |
|----------|-------------|-----------|-----------|
| 1K | ~3K | ~300K | ~50MB |
| 10K | ~30K | ~3M | ~500MB |
| 100K | ~300K | ~30M | ~5GB |

### 5.4 手动执行 SQL

```bash
# 进入数据库容器
docker compose exec -it db psql -U sts2stats -d sts2stats

# 查看表行数
SELECT schemaname, relname, n_live_tup
FROM pg_stat_user_tables ORDER BY n_live_tup DESC;

# 查看每个角色的 run 数
SELECT character, COUNT(*) FROM runs GROUP BY character ORDER BY COUNT(*) DESC;

# 查看某张卡的统计
SELECT card_id,
    AVG(was_picked::int)::real AS pick_rate,
    AVG(CASE WHEN was_picked THEN win::int END)::real AS win_rate,
    COUNT(*) AS n
FROM card_choices
WHERE character = 'IRONCLAD' AND card_id = 'BASH'
GROUP BY card_id;
```

### 5.5 备份与恢复

```bash
# 备份 (自动配置为每日 4AM)
docker compose exec -T db pg_dump -U sts2stats -Fc sts2stats > backup.dump

# 恢复到新数据库
docker compose exec -T db pg_restore -U sts2stats -d sts2stats --clean backup.dump

# 查看备份
ls -la /backups/sts2stats/
```

---

## 6. Redis 缓存层

### 6.1 缓存键设计

```
键格式:                         示例:
bulk:{char}:{asc}:{ver}        bulk:IRONCLAD:10-14:v0.99.1
                               → BulkStatsBundle JSON, TTL 15 min
```

### 6.2 进阶区间映射

客户端发送精确的 `min_asc`/`max_asc`，服务端映射到预计算区间：

| 客户端请求 | 映射区间 | 说明 |
|-----------|---------|------|
| asc 0-4 | `0-4` | 新手段 |
| asc 5-9 | `5-9` | |
| asc 10-14 | `10-14` | |
| asc 15-19 | `15-19` | 高难度 |
| asc 20 | `20-20` | 最高难度 |

### 6.3 缓存命中率监控

```bash
# 查看 Redis 统计
docker compose exec redis redis-cli INFO stats | grep keyspace

# 查看当前缓存的键
docker compose exec redis redis-cli KEYS "bulk:*"

# 手动清除缓存 (如需强制刷新)
docker compose exec redis redis-cli FLUSHDB
```

### 6.4 内存配置

```yaml
# docker-compose.yml 中已配置:
redis-server --maxmemory 256mb --maxmemory-policy allkeys-lru
```

- `256MB` 足够存储 ~1000 个 bundle (~200KB/个)
- `allkeys-lru` 策略: 内存满时淘汰最近最少使用的键
- 生产环境可按需调整到 512MB-1GB

---

## 7. API 服务详解

### 7.1 端点清单

| Method | Path | 功能 | Rate Limit |
|--------|------|------|-----------|
| `GET` | `/health` | 健康检查 | 无 |
| `POST` | `/v1/runs` | 上传 Run | 10/min/IP |
| `GET` | `/v1/stats/bulk` | 批量统计 (主接口) | 60/min/IP |
| `GET` | `/v1/stats/cards` | 按需卡牌查询 | 60/min/IP |
| `GET` | `/v1/stats/relics` | 按需遗物查询 | 60/min/IP |
| `GET` | `/v1/stats/events/{id}` | 按需事件查询 | 60/min/IP |
| `GET` | `/v1/stats/encounters` | 按需遭遇战查询 | 60/min/IP |
| `GET` | `/v1/meta/versions` | 可用版本列表 | 无 |

### 7.2 核心查询参数

所有 `/v1/stats/*` 端点共享以下查询参数：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `char` | string | 是 | 角色名 (IRONCLAD, SILENT, ...) |
| `ver` | string | 是 | 游戏版本 (v0.99.1) |
| `min_asc` | int | 否 | 最低进阶 (默认 0) |
| `max_asc` | int | 否 | 最高进阶 (默认 20) |

### 7.3 上传请求格式

```http
POST /v1/runs HTTP/1.1
Content-Type: application/json
X-Mod-Version: 1.0.0

{
  "mod_version": "1.0.0",
  "game_version": "v0.99.1",
  "character": "IRONCLAD",
  "ascension": 15,
  "win": true,
  "player_win_rate": 0.45,
  "num_players": 1,
  "floor_reached": 57,
  "card_choices": [
    {"card_id": "BASH", "upgrade_level": 0, "was_picked": true, "floor": 3},
    {"card_id": "ANGER", "upgrade_level": 0, "was_picked": false, "floor": 3}
  ],
  "event_choices": [
    {"event_id": "NEOW", "option_index": 2, "total_options": 4}
  ],
  "final_deck": [
    {"card_id": "BASH", "upgrade_level": 1}
  ],
  "final_relics": ["BURNING_BLOOD", "VAJRA"],
  "encounters": [
    {"encounter_id": "JAW_WORM", "encounter_type": "normal",
     "damage_taken": 8, "turns_taken": 4, "player_died": false, "floor": 1}
  ],
  "contributions": [],
  "shop_purchases": [],
  "card_removals": [],
  "card_upgrades": []
}
```

### 7.4 响应格式 (BulkStatsBundle)

```json
{
  "generated_at": "2026-04-01T12:00:00+00:00",
  "total_runs": 42850,
  "cards": {
    "BASH": {
      "pick": 0.72,
      "win": 0.48,
      "removal": 0.15,
      "upgrade": 0.35,
      "shop_buy": 0.02,
      "n": 38500
    }
  },
  "relics": {
    "BURNING_BLOOD": {
      "win": 0.52,
      "pick": 0.95,
      "shop_buy": 0.0,
      "n": 40000
    }
  },
  "events": {
    "NEOW": {
      "options": [
        {"idx": 0, "sel": 0.35, "win": 0.42, "n": 15000},
        {"idx": 1, "sel": 0.30, "win": 0.45, "n": 12800}
      ]
    }
  },
  "encounters": {
    "JAW_WORM": {
      "type": "normal",
      "avg_dmg": 6.2,
      "death": 0.02,
      "avg_turns": 3.8,
      "n": 35000
    }
  }
}
```

### 7.5 聚合 SQL 参考

**卡牌统计** (`aggregation.py`):
```sql
SELECT
    card_id,
    AVG(was_picked::int)::real       AS pick_rate,
    AVG(CASE WHEN was_picked THEN win::int END)::real AS win_rate,
    COUNT(*)                          AS sample_size
FROM card_choices
WHERE character = $1
  AND game_version = $2
  AND ascension BETWEEN $3 AND $4
GROUP BY card_id;
```

**遗物统计**:
```sql
SELECT
    relic_id,
    AVG(win::int)::real  AS win_rate,
    COUNT(*)             AS sample_size
FROM relic_records
WHERE character = $1 AND game_version = $2
  AND ascension BETWEEN $3 AND $4
GROUP BY relic_id;
```

**事件统计**:
```sql
SELECT
    event_id, option_index,
    COUNT(*)::real / SUM(COUNT(*)) OVER (PARTITION BY event_id) AS selection_rate,
    AVG(win::int)::real AS win_rate,
    COUNT(*)            AS sample_size
FROM event_choices
WHERE character = $1 AND game_version = $2
  AND ascension BETWEEN $3 AND $4
GROUP BY event_id, option_index;
```

**遭遇战统计**:
```sql
SELECT
    encounter_id, encounter_type,
    AVG(damage_taken)::real      AS avg_dmg,
    AVG(turns_taken)::real       AS avg_turns,
    AVG(player_died::int)::real  AS death_rate,
    COUNT(*)                     AS sample_size
FROM encounter_records
WHERE character = $1 AND game_version = $2
  AND ascension BETWEEN $3 AND $4
GROUP BY encounter_id, encounter_type;
```

---

## 8. 预计算管线

### 8.1 工作原理

```
APScheduler (每 10 分钟)
  └── precompute_all()
      └── 遍历: 活跃版本 × 6角色 × 5区间 = ~60 组合
          └── 对每组: compute_bulk_stats() → Redis.SET(key, json, TTL=15min)
```

### 8.2 维度组合

| 维度 | 枚举 | 数量 |
|------|------|------|
| 角色 | IRONCLAD, SILENT, DEFECT, WATCHER, VAGABOND, NECROMANCER | 6 |
| 进阶区间 | 0-4, 5-9, 10-14, 15-19, 20 | 5 |
| 版本 | 通常 1-2 个活跃版本 | ~2 |
| **总计** | | **~60 组** |

### 8.3 性能预期

| 数据量 | 单次全量预计算耗时 | PostgreSQL 负载 |
|--------|-------------------|----------------|
| 10K runs | ~5 秒 | 低 |
| 100K runs | ~30 秒 | 中 |
| 1M runs | ~3 分钟 | 需要只读副本 |

### 8.4 配置调整

通过环境变量控制:
```bash
PRECOMPUTE_INTERVAL_MINUTES=10   # 预计算间隔 (分钟)
CACHE_TTL_SECONDS=900            # Redis 缓存 TTL (秒)
```

**注意**: `CACHE_TTL_SECONDS` 应该 > `PRECOMPUTE_INTERVAL_MINUTES × 60`，否则缓存会在预计算之前过期。

---

## 9. Nginx 反向代理与 HTTPS

### 9.1 角色

```
客户端 → [443/HTTPS] → Nginx → [8080/HTTP] → FastAPI
                  ↑
            SSL 终结 + Gzip + 速率限制
```

### 9.2 SSL 证书管理

**首次申请** (安装脚本自动完成):
```bash
certbot certonly --webroot -w /var/www/certbot \
    -d api.sts2stats.com --non-interactive --agree-tos -m admin@example.com
```

**自动续期**: Docker Compose 中 `certbot` 服务每 12 小时自动检查并续期。

**手动续期**:
```bash
docker compose exec certbot certbot renew --dry-run  # 测试
docker compose exec certbot certbot renew             # 实际续期
docker compose restart nginx                           # 重载证书
```

### 9.3 Nginx 速率限制

```nginx
# /v1/runs: 每 IP 10 次/分钟 (突发 5 次)
limit_req zone=api_upload burst=5 nodelay;

# /v1/stats/*: 每 IP 60 次/分钟 (突发 20 次)
limit_req zone=api_query burst=20 nodelay;
```

### 9.4 Cloudflare 替代方案 (更简单)

如果不想自行管理 Nginx + SSL，可使用 Cloudflare (免费):

1. 域名 DNS 转到 Cloudflare
2. 开启 Proxied 模式 (橙色云)
3. SSL 设置为 "Full (Strict)"
4. 自动获得 CDN + DDoS 防护 + 免费 SSL
5. 移除 Docker Compose 中的 `nginx` 和 `certbot` 服务
6. API 容器直接暴露 `80:8080`

---

## 10. 版本管理与 ID 迁移

### 10.1 游戏版本自动注册

每次上传 Run 时，如果是新版本，自动注册到 `game_versions` 表:
```sql
INSERT INTO game_versions (version)
VALUES ($1)
ON CONFLICT (version) DO UPDATE SET last_seen = NOW();
```

### 10.2 ID 迁移 (卡牌/遗物改名)

STS2 在 Early Access 期间可能重命名实体。手动维护迁移表:

```sql
-- 例: v0.99.2 中 BASH 改名为 HEAVY_BASH
INSERT INTO id_migrations VALUES ('BASH', 'HEAVY_BASH', 'v0.99.2', 'card');
```

`id_migration.py` 在启动时加载迁移表到内存，支持链式迁移 (A→B→C)。

### 10.3 版本过期

`002_maintenance.sql` 自动处理：
- 30 天无上传 → 标记 `is_active = FALSE`
- 非活跃版本 6 个月后 → 删除原始数据

---

## 11. 客户端 Mod 发布

### 11.1 编译发布版

```bash
cd mods/sts2_community_stats

# 修改 ApiBaseUrl 指向生产服务器
# 编辑 src/Config/ModConfig.cs:
#   public static string ApiBaseUrl { get; set; } = "https://api.sts2stats.com/v1";

dotnet build -c Release
```

### 11.2 发布目录结构

STS2 的 ModManager 要求 DLL 与 `manifest.json` 同目录:

```
sts2_community_stats/
├── manifest.json                  # Mod 元信息
├── sts2_community_stats.dll       # 编译输出
└── config.json                    # (可选) 用户配置覆盖
```

### 11.3 发布渠道

- **Steam Workshop**: 通过 STS2 Workshop 工具发布
- **手动安装**: 将文件夹放入 `[游戏目录]/mods/`
- **GitHub Releases**: 提供 zip 下载

---

## 12. 运维与监控

### 12.1 日常运维命令

```bash
cd /opt/sts2stats

# 查看所有服务状态
docker compose ps

# 查看实时日志
docker compose logs -f api                # API 日志
docker compose logs -f db                 # 数据库日志
docker compose logs --tail=100 api        # 最近 100 行

# 重启单个服务
docker compose restart api

# 更新代码并重部署
git pull  # 或 rsync 新代码
docker compose up -d --build api          # 仅重建 API

# 进入数据库交互
docker compose exec -it db psql -U sts2stats -d sts2stats

# 进入 Redis 交互
docker compose exec -it redis redis-cli
```

### 12.2 关键指标

| 指标 | 健康阈值 | 检查方法 |
|------|---------|---------|
| `/health` 响应 | 200, <100ms | `curl -w '%{time_total}' /health` |
| 日 Run 上传量 | 无突变 >50% | `SELECT COUNT(*) FROM runs WHERE created_at > NOW() - '1d'` |
| Redis 命中率 | >80% | `redis-cli INFO stats \| grep keyspace` |
| PG 连接数 | <80 (of 100) | `SELECT count(*) FROM pg_stat_activity` |
| 磁盘使用 | <80% | `df -h /` |
| Docker 日志大小 | <500MB | `du -sh /var/lib/docker/containers/` |

### 12.3 告警 (可选)

推荐使用 [Uptime Kuma](https://github.com/louislam/uptime-kuma) (自部署，免费):

```bash
# 在同一台或另一台 VPS 上
docker run -d --name uptime-kuma \
    -p 3001:3001 \
    -v uptime-kuma:/app/data \
    louislam/uptime-kuma:1
```

配置监控:
- HTTP(s) 监控: `https://api.sts2stats.com/health`
- 检查间隔: 5 分钟
- 通知: Telegram / Discord / Email

### 12.4 自动备份

安装脚本已配置 cron 任务:

```
# 每日 4AM: PostgreSQL 完整备份
0 4 * * * pg_dump → /backups/sts2stats/sts2stats_YYYYMMDD.dump

# 每日 5AM: 清理 30 天前的备份
0 5 * * * find /backups -mtime +30 -delete

# 每周日 3AM: 数据库维护 (清理过期版本数据)
0 3 * * 0 psql -f 002_maintenance.sql
```

---

## 13. 安全与反滥用

### 13.1 输入校验 (已内置于 models.py)

| 校验项 | 规则 |
|--------|------|
| ascension | 0-20 |
| character | 大写字母，32 字符以内 |
| card_choices | 最多 500 条 |
| encounters | 最多 100 条 |
| contributions | 最多 5000 条 |
| damage_taken | 0-999,999 |
| turns_taken | 0-999 |
| cost | 0-9,999 |
| floor | 0-200 |
| 字符串字段 | 最多 64 字符 |

### 13.2 速率限制

三层防护:

1. **Nginx 层**: `limit_req_zone` (最前端，最快)
2. **FastAPI 层**: `slowapi` (应用级别)
3. **应用逻辑**: Pydantic 模型校验 (字段级别)

### 13.3 Mod 版本封锁

服务端可拒绝有已知 bug 的 Mod 版本:

```bash
# 在 .env 中配置
BLOCKED_MOD_VERSIONS=0.9.0,0.9.1
```

被封锁的客户端会收到 `426 Upgrade Required` 响应。

### 13.4 网络安全

- PostgreSQL 和 Redis **仅绑定 127.0.0.1**，不对外暴露
- 所有外部流量通过 Nginx + HTTPS
- Docker Compose 使用内部网络通信
- `.env` 文件权限 `600` (仅 root 可读)

---

## 14. 扩容指南

### 14.1 垂直扩容 (加大配置)

适用于日活 < 10K:

```yaml
# docker-compose.yml 中调整 PostgreSQL 参数
command: >
  postgres
    -c shared_buffers=512MB        # 原 256MB
    -c effective_cache_size=1GB    # 原 512MB
    -c work_mem=32MB               # 原 16MB
    -c max_connections=200         # 原 100
```

### 14.2 水平扩容

适用于日活 > 10K:

```
                    ┌──────────────────────┐
                    │  Load Balancer       │
                    │  (Nginx / HAProxy)   │
                    └────┬────────────┬────┘
                         │            │
                 ┌───────▼──┐  ┌─────▼─────┐
                 │ API-1    │  │ API-2     │
                 │ (precomp)│  │ (no prec) │
                 └─────┬────┘  └─────┬─────┘
                       │             │
                ┌──────▼─────────────▼──────┐
                │                           │
         ┌──────▼──────┐          ┌─────────▼─┐
         │ PG Primary  │──复制──→│ PG Replica │
         │ (写入)      │          │ (只读查询)  │
         └─────────────┘          └────────────┘
```

关键步骤:
1. 分离 PostgreSQL 到独立服务器 (或使用托管 RDS)
2. 添加只读副本用于聚合查询
3. API 多实例部署，仅 1 实例运行预计算
4. Redis 集群或 Sentinel 模式

### 14.3 预计算优化

当数据量超过 100 万条时:

```sql
-- 创建物化视图加速聚合
CREATE MATERIALIZED VIEW mv_card_stats AS
SELECT character, game_version, ascension / 5 * 5 AS asc_bucket,
       card_id,
       AVG(was_picked::int)::real AS pick_rate,
       AVG(CASE WHEN was_picked THEN win::int END)::real AS win_rate,
       COUNT(*) AS n
FROM card_choices
GROUP BY character, game_version, asc_bucket, card_id;

-- 定时刷新 (替代实时聚合)
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_card_stats;
```

---

## 15. 故障排除

### 15.1 服务启动失败

```bash
# 查看详细错误
docker compose logs api --tail=50

# 常见原因:
# 1. 数据库未就绪 → 等 db 健康后再启动 api (docker-compose 已配置 depends_on)
# 2. .env 文件缺失 → cp .env.example .env
# 3. 端口冲突 → netstat -tlnp | grep 8080
```

### 15.2 Mod 连接不上服务器

```
# 客户端日志位置: [游戏目录]/logs/godot.log
# 搜索关键字:

[CommunityStats] Preload network error:          → 网络问题
[CommunityStats] Query failed: GET ... → 404     → URL 路径错误
[CommunityStats] Upload failed: 429              → 被速率限制
[CommunityStats] Upload failed: 426              → Mod 版本被封锁
```

**检查清单**:
1. `config.json` 中 `api_base_url` 是否正确 (末尾不要加 `/`)
2. 服务器端口 443 是否开放 (`ufw allow 443`)
3. DNS 是否生效 (`dig api.sts2stats.com`)
4. SSL 证书是否有效 (`curl -vI https://api.sts2stats.com/health`)

### 15.3 数据不显示

```bash
# 1. 检查 Redis 有无缓存
docker compose exec redis redis-cli KEYS "bulk:*"

# 2. 检查数据库有无数据
docker compose exec db psql -U sts2stats -d sts2stats -c "SELECT COUNT(*) FROM runs;"

# 3. 手动触发预计算 (进入 API 容器)
docker compose exec api python -c "
import asyncio
from app.database import init_pool
from app.cache import init_redis
from app.precompute import precompute_all
async def main():
    await init_pool()
    await init_redis()
    n = await precompute_all()
    print(f'Precomputed {n} bundles')
asyncio.run(main())
"
```

### 15.4 性能问题

```bash
# PostgreSQL 慢查询 (>500ms 自动记录)
docker compose logs db | grep "duration"

# Redis 内存使用
docker compose exec redis redis-cli INFO memory | grep used_memory_human

# API 响应时间测试
curl -w "\n  DNS: %{time_namelookup}s\n  Connect: %{time_connect}s\n  TLS: %{time_appconnect}s\n  Total: %{time_total}s\n" \
    "https://api.sts2stats.com/v1/stats/bulk?char=IRONCLAD&ver=v0.99.1" -o /dev/null -s
```

### 15.5 磁盘空间不足

```bash
# Docker 清理
docker system prune -a --volumes  # ⚠️ 删除未使用的镜像和卷

# 安全清理 (不删数据卷)
docker system prune -f
docker image prune -a -f

# 清理旧日志
truncate -s 0 /var/log/sts2stats-*.log
```

---

## 16. 成本估算

### 起步阶段 (0-1K 日活, 前 3 个月)

| 项目 | 月成本 |
|------|--------|
| Hetzner CX22 (2C4G) | €4.50 (~$5) |
| 域名 (.com) | ~$1/月 |
| HTTPS (Let's Encrypt) | 免费 |
| **总计** | **~$6/月** |

### 增长阶段 (1K-10K 日活)

| 项目 | 月成本 |
|------|--------|
| Hetzner CX32 (4C8G) | €10 (~$11) |
| 域名 | ~$1 |
| 监控 (Uptime Kuma 自部署) | 免费 |
| **总计** | **~$12/月** |

### 规模阶段 (10K+ 日活)

| 项目 | 月成本 |
|------|--------|
| 应用服务器 2× (4C8G) | ~$30 |
| 托管 PostgreSQL (DigitalOcean) | ~$50 |
| Redis 托管 | ~$15 |
| Cloudflare (免费) | $0 |
| **总计** | **~$100/月** |

---

## 17. 开发排期

### Phase 1: MVP (1-2 周)

- [ ] 购买 VPS + 域名
- [ ] 运行 `setup_server.sh` 部署
- [ ] 用 `seed_test_data.py` 注入测试数据
- [ ] 修改 Mod `ApiBaseUrl` 指向服务器
- [ ] 端到端测试: 游戏 → 打一局 → 验证上传 → 新 Run 验证显示

### Phase 2: 优化 (1 周)

- [ ] 监控缓存命中率，调整预计算间隔
- [ ] 添加 Cloudflare 或 CDN
- [ ] 压力测试 (`wrk` 或 `vegeta`)

### Phase 3: 健壮性 (1 周)

- [ ] 建立 ID 迁移流程
- [ ] 备份恢复演练
- [ ] 离线队列端到端测试
- [ ] 监控告警 (Uptime Kuma)

### Phase 4: 发布 (几天)

- [ ] 编写玩家 README
- [ ] 打包 Mod 到 Steam Workshop
- [ ] 撰写社区帖子 (Reddit / Discord)

---

## 附录 A: 完整 Docker Compose 命令速查

```bash
docker compose up -d               # 启动所有服务
docker compose up -d --build       # 重建并启动
docker compose down                # 停止所有服务
docker compose down -v             # 停止并删除数据
docker compose ps                  # 查看状态
docker compose logs -f api         # 跟踪 API 日志
docker compose restart api         # 重启 API
docker compose exec db psql -U sts2stats -d sts2stats  # 进入数据库
docker compose exec redis redis-cli                     # 进入 Redis
docker compose pull                # 更新基础镜像
```

## 附录 B: 环境变量参考

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `DB_PASSWORD` | changeme | PostgreSQL 密码 |
| `DATABASE_URL` | (见 .env.example) | 完整数据库 DSN |
| `REDIS_URL` | redis://redis:6379/0 | Redis 连接 URL |
| `HOST` | 0.0.0.0 | API 监听地址 |
| `PORT` | 8080 | API 监听端口 |
| `WORKERS` | 1 | uvicorn worker 数 |
| `LOG_LEVEL` | info | 日志级别 |
| `PRECOMPUTE_INTERVAL_MINUTES` | 10 | 预计算间隔 |
| `CACHE_TTL_SECONDS` | 900 | Redis 缓存 TTL |
| `RATE_LIMIT_UPLOAD` | 10/minute | 上传速率限制 |
| `RATE_LIMIT_QUERY` | 60/minute | 查询速率限制 |
| `BLOCKED_MOD_VERSIONS` | (空) | 逗号分隔的封锁版本 |
