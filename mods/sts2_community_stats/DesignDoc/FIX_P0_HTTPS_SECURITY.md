# PLAN: P0 安全修复 — HTTPS 通信 + 凭据清理

> 日期：2026-04-16
> 触发：准发布版安全审阅发现客户端使用 HTTP 明文通信，服务端模板含默认弱密码

---

## Context

安全审阅发现 3 个 P0 问题：

1. **客户端 config.json 使用 `http://64.176.85.164/v1`（HTTP 明文 + 裸 IP）** — 所有数据传输无加密，绕过 Nginx HTTPS 终止层。开发者表示之前测试时为避免 TLS 握手失败而降级为 HTTP。
2. **docker-compose.yml 中 `DB_PASSWORD` fallback 为 `changeme`** — 未设环境变量时数据库使用弱密码。
3. **server/.env 含占位密码** — 虽已被 .gitignore 排除，但文件存在于磁盘，模板角色不明确。

**部署环境**：
- VPS: Vultr Ubuntu 24.04, IP `64.176.85.164`
- 域名: `statsthespire.duckdns.org`（DuckDNS 免费）
- HTTPS: Let's Encrypt, Nginx 容器终止 TLS
- 部署目录: `/opt/sts2stats/`
- 发布包: `Sts2-mod-decompiled/stats_the_spire/`（无 git/server 文件）

**TLS 握手失败根因分析**：
客户端之前用 `http://64.176.85.164/v1` → Nginx 80 端口 → 301 重定向到 `https://64.176.85.164/v1` → TLS 证书是给 `statsthespire.duckdns.org` 的，**主机名不匹配** → 握手失败。修复方式是客户端直接使用域名 HTTPS 连接，而非 IP。

---

## 修改文件清单

| # | 文件 | 变更 | 位置 |
|---|------|------|------|
| 1 | `mods/sts2_community_stats/config.json` | HTTP IP → HTTPS 域名 | 开发源 |
| 2 | `stats_the_spire/config.json` | 同上 | 发布包 |
| 3 | `mods/sts2_community_stats/src/Config/ModConfig.cs` | 默认 URL 更新 + HTTPS scheme 校验 | 客户端代码 |
| 4 | `server/docker-compose.yml` | 移除 changeme fallback | 服务端 |
| 5 | `server/nginx/default.conf` | 模板域名更新为 DuckDNS | 服务端模板 |
| 6 | `server/scripts/setup_server.sh` | 默认域名/邮箱更新 | 部署脚本 |
| 7 | `server/.env` → `server/.env.example` | 重命名为模板，明确占位符 | 服务端 |

---

## 详细改动

### 1. config.json（开发源 + 发布包）

两个文件同步修改：

```json
{
  "api_base_url": "https://statsthespire.duckdns.org/v1",
  "query_timeout_ms": 10000,
  "upload_timeout_ms": 30000
}
```

路径：
- `mods/sts2_community_stats/config.json`
- `stats_the_spire/config.json`

### 2. ModConfig.cs — 默认 URL + HTTPS 强制

`src/Config/ModConfig.cs:13`：

```csharp
// 旧
public static string ApiBaseUrl { get; set; } = "https://api.sts2stats.example.com/v1";

// 新
public static string ApiBaseUrl { get; set; } = "https://statsthespire.duckdns.org/v1";
```

在 `LoadOverrides()` 中解析 `api_base_url` 后增加 scheme 校验（约 `ModConfig.cs:87`）：

```csharp
if (root.TryGetProperty("api_base_url", out var url))
{
    var rawUrl = url.GetString() ?? ApiBaseUrl;
    // 强制 HTTPS：拒绝 HTTP scheme，防止中间人攻击
    if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
        Safe.Warn($"[ModConfig] Insecure HTTP URL rejected: {rawUrl}, upgrading to HTTPS");
        rawUrl = "https://" + rawUrl[7..];
    }
    ApiBaseUrl = rawUrl;
}
```

**设计考量**：不是直接拒绝 HTTP 不加载，而是自动升级为 HTTPS + 打 warn 日志。这样：
- 如果用户手动编辑 config.json 误用 HTTP，自动修正而非静默失败
- 开发者本地测试可以通过查看日志发现被升级
- 不会因为旧 config 文件导致 mod 无法连接服务器

### 3. docker-compose.yml — 移除弱密码 fallback

`server/docker-compose.yml:9`：

```yaml
# 旧
DATABASE_URL: "postgresql://sts2stats:${DB_PASSWORD:-changeme}@db:5432/sts2stats"
# 新
DATABASE_URL: "postgresql://sts2stats:${DB_PASSWORD:?DB_PASSWORD must be set in .env}@db:5432/sts2stats"
```

`server/docker-compose.yml:35`：

```yaml
# 旧
POSTGRES_PASSWORD: "${DB_PASSWORD:-changeme}"
# 新
POSTGRES_PASSWORD: "${DB_PASSWORD:?DB_PASSWORD must be set in .env}"
```

`${VAR:?message}` — 变量未设置时 shell 报错并退出，容器启动失败，强制运维者正确配置。

### 4. nginx/default.conf — 模板域名更新

将所有 `api.sts2stats.com` 替换为 `statsthespire.duckdns.org`（4 处）：

- L22: `server_name`
- L36: `server_name`
- L40: `ssl_certificate` 路径
- L41: `ssl_certificate_key` 路径

**注意**：setup_server.sh 有 `sed -i "s/api.sts2stats.com/${DOMAIN}/g"` 自动替换，但模板本身应反映实际域名，避免直接使用模板时遗漏 sed 步骤。

### 5. setup_server.sh — 默认参数更新

`server/scripts/setup_server.sh:10-11`：

```bash
# 旧
DOMAIN="${1:-api.sts2stats.com}"
EMAIL="${2:-admin@sts2stats.com}"

# 新
DOMAIN="${1:-statsthespire.duckdns.org}"
EMAIL="${2:-reddotswilson@gmail.com}"
```

### 6. .env → .env.example

将 `server/.env` 重命名为 `server/.env.example`，内容不变（已经是占位符）。
.gitignore 中 `server/.env` 已被排除。

---

## 服务端部署步骤

修改完本地文件后，需要同步到 VPS 并重启：

```bash
# 1. 同步修改后的配置到 VPS
scp server/docker-compose.yml root@64.176.85.164:/opt/sts2stats/docker-compose.yml
scp server/nginx/default.conf root@64.176.85.164:/opt/sts2stats/nginx/default.conf

# 2. SSH 进入 VPS，重启受影响容器
ssh root@64.176.85.164
cd /opt/sts2stats

# 3. 验证 .env 中 DB_PASSWORD 已设置（新 docker-compose 会在缺失时报错）
grep DB_PASSWORD .env

# 4. 验证 Let's Encrypt 证书存在且域名匹配
ls /etc/letsencrypt/live/statsthespire.duckdns.org/
# 应看到 fullchain.pem, privkey.pem

# 5. 重启 nginx 容器（加载新 default.conf）
docker compose restart nginx

# 6. 健康检查
curl -sf https://statsthespire.duckdns.org/health
# 预期: {"status":"healthy","db":"ok","redis":"ok"}
```

---

## 验证

### 本地验证
```bash
cd Sts2-mod-decompiled/mods/sts2_community_stats && dotnet build
# 预期: Build succeeded, 0 errors
```

### 远程连通性验证
```bash
# HTTPS GET 测试
curl -v https://statsthespire.duckdns.org/health
# 预期: TLS 握手成功，返回 {"status":"healthy",...}

# HTTPS POST 测试（模拟上传）
curl -v -X POST https://statsthespire.duckdns.org/v1/runs \
  -H "Content-Type: application/json" \
  -H "X-Mod-Version: 2.0.0" \
  -d '{"mod_version":"test","game_version":"test","character":"IRONCLAD","ascension":0,"win":false,"num_players":1,"floor_reached":1}'
# 预期: 200 或 422（验证失败），而非连接错误

# HTTP 重定向测试
curl -v http://statsthespire.duckdns.org/health
# 预期: 301 → https://statsthespire.duckdns.org/health

# 旧 IP 直连测试（确认不再可用于 HTTP 明文）
curl -v http://64.176.85.164/v1/runs
# 预期: 301 → https://64.176.85.164/ → TLS 失败（预期行为，防止 IP 直连）
```

### 游戏内验证
1. 启动游戏 → Mod 加载 → Godot 日志中应看到 HTTPS URL（非 HTTP）
2. 打一局 → 结束时上传成功通知
3. F9 面板 → 社区数据正常加载

---

## 风险

| 风险 | 等级 | 缓解 |
|------|------|------|
| VPS 上证书路径不匹配 | 中 | 步骤 4 先验证证书存在；如缺失，需在 VPS 上运行 `certbot certonly --webroot -w /var/www/certbot -d statsthespire.duckdns.org` |
| docker-compose 的 `${DB_PASSWORD:?...}` 导致现有容器启动失败 | 低 | 步骤 3 先验证 .env 存在且含 DB_PASSWORD |
| 已分发的旧测试包仍使用 HTTP IP | 低 | 旧包的 HTTP 请求会被 Nginx 301 重定向到 HTTPS，但域名不匹配导致失败 — 迫使测试者更新包 |

---

## 状态

- [x] 客户端 config.json 修改（开发源 + 发布包）
- [x] ModConfig.cs 默认 URL + HTTPS 强制
- [x] docker-compose.yml 移除弱密码 fallback
- [x] nginx/default.conf 模板域名更新
- [x] setup_server.sh 默认参数更新
- [x] .env → .env.example 重命名
- [x] dotnet build 验证（0 errors, 7 pre-existing warnings）
- [ ] 服务端部署 + 连通性测试（需 SSH 到 VPS）
