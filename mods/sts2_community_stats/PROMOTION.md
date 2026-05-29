  Description / 描述

  - In-Run Displays: potion drop chance, card rarity odds, ? room encounter rates, shop price preview.
  - Intent Graph: enemy AI state machine on hover.
  - Combat Contribution Chart (F8): per-card/relic/potion breakdown of damage, defense, draw, and energy with full
  indirect attribution. Real-time updates during combat.
  - Community Stats: card pick/win rates on rewards, relic win rates with delta, event choice rates, map danger
  overlay, shop buy rates — aggregated from all subscribed players.
  - Personal Career Stats: win rate trends, death causes, per-Act deck/path statistics, ancient relic pick rates, boss
  damage taken.
  - Run History Enhancement: view single-run stats and replay past combat contribution charts.
  - Compendium Integration: card library and relic collection show personal + community data side by side.
  - Settings Panel (F9): upload toggle, ascension/WR/character filters, data branch (Release/Beta), language switch.
  - Internet required for community data. Local features work offline.

  - 局内信息显示：药水掉落、卡牌稀有度、?房遭遇概率、商店价格预览。
  - 怪物意图状态机：悬停查看敌人 AI 状态转移图。
  - 战斗贡献统计 (F8)：每张卡/遗物/药水的伤害/防御/抽牌/能量贡献，间接效果完整归因。战斗中实时更新。
  - 社区统计：选卡选取率/胜率、遗物胜率/浮动、事件选择率、地图危险度、商店购买率 —— 全体订阅者数据聚合。
  - 个人生涯统计：胜率趋势、死因排行、每幕卡组与路线数据、先古遗物选取率、Boss 战损。
  - Run History 增强：历史记录中查看本局统计，回看过往战斗贡献图表。
  - 百科大全集成：卡牌图书馆/遗物收藏显示个人 + 社区双列对比。
  - 设置面板 (F9)：上传开关、进阶/胜率/角色筛选、数据分支（正式版/Beta）、语言切换。
  - 社区数据需联网。本地功能可离线使用。

  Installation / 安装方式
  Installation: Extract the archive. Place the stats_the_spire folder into your game's mods/ directory. Launch Slay the Spire 2. The mod loads automatically.
  安装：解压后将 stats_the_spire 文件夹放入游戏 mods/ 目录，启动游戏即可。

  Main Features / 主要特点

  In-Run Displays (InfoMod)
  - Potion drop chance tracker on the top bar. Hover for elite odds and multi-combat cumulative probability.
  - Card rarity drop odds for your next combat reward.
  - Unknown room encounter probability preview on hover. Relic modifiers factored in.
  - Shop price preview with relic-adjusted costs, color-coded by what you can afford with current gold.
  局内信息显示
  - 顶部栏药水掉落追踪。悬停查看精英战概率与多场累计概率。
  - 下场战斗卡牌奖励，不同稀有度掉落率。
  - 悬停 ? 房查看遭遇概率（含遗物修正）。
  - 商店价格预览（含遗物折扣修正、金币是否够用颜色区分）。
  - Map nodes: death rate and average damage on visited encounters. | 地图节点：已走过遭遇战的死亡率与平均伤害。

  Intent Graph
  - Hover any monster to see its full intent state machine.
  - Shows possible actions, probabilities, and consecutive-use limits.
  - Yellow arrows for normal transitions, red for conditional branches. Ascension 9+ shows Deadly variants.
  怪物意图状态机
  - 悬停怪物查看完整意图状态机。
  - 显示可能行动、概率、连续使用次数上限。
  - 黄色箭头常规转移，红色箭头条件分支。进阶 9+ 显示致命变体。

  Combat Contribution Chart (hotkey: F8)
  - Auto-pops after each combat. Damage, Defense, Card Draw, Energy, Stars, Healing per source.
  - Real-time mode: open during combat to see live contribution data update as you play cards.
  - Strength/Vulnerable/Poison/Orb/Doom all traced back to the original applicator.
  - Defense pipeline: Block (FIFO) → Dexterity → Weak mitigation → Intangible cap → Buffer layers — each credited to its source.
  - Layer consumption: Vulnerable/Weak first-applied-first-credited per turn; Poison ticks split by remaining stacks; Doom FIFO detonation per enemy.
  - Upgrade bonuses (Armaments upgrading Strike) shown as orange segments. Sub-bars for generated/transformed cards.
  - Run Summary tab with separate Healing section. Mid-run save/quit persistence.
  战斗贡献统计 (F8)
  - 每场战斗后自动弹出。伤害、防御、抽牌、能量、辉星、治疗按来源分列。
  - 实时模式：战斗中打开面板，随打牌即时刷新贡献数据。
  - 力量/易伤/中毒/充能球/灾厄 全部归因到最初施加源。
  - 防御链：格挡(FIFO) → 敏捷 → 虚弱减伤 → 无实体上限 → 缓冲层，逐层归因。
  - 层数消耗：易伤/虚弱每回合归因于队列最前层；中毒按剩余层数比例分配；灾厄按敌人 FIFO 引爆。
  - 升级增量（如武装升级打击）以橙色段显示。生成/变形卡以子条缩进显示。
  - 本局汇总标签页含治疗分类。中途存档退出后数据不丢失。

  Community Stats
  - Card reward screen: pick rate and win rate per card, color-coded.
  - Event options: selection rate per option.
  - Relic hover and compendium: community win rate and win rate delta.
  - Shop: community buy rates on cards and relics.
  - Card removal / upgrade screens: community removal and upgrade rates.
  - All community data filterable by ascension range, player win rate, character, and game version (F9).
  社区统计
  - 选卡界面：每张卡选取率与胜率，颜色编码。
  - 事件选项：每个选项的选择率。
  - 遗物悬停与百科大全：社区胜率与胜率浮动。
  - 商店：卡牌与遗物社区购买率。
  - 删牌/升级界面：社区删除率与升级率。
  - 全部社区数据可按进阶范围、玩家胜率、角色、游戏版本筛选（F9）。

  Personal Career Stats (Compendium → Character Data)
  - Win rate summary with adjustable ascension/character filter and rolling trend (last 10/50/100/all).
  - Death cause ranking with encounter icons.
  - Per-Act deck building (obtained/bought/removed/upgraded) and path statistics (monster/elite/?room/shop/rest).
  - Ancient relic pick rates grouped by Elder, with pool-by-pool breakdown.
  - Boss damage taken across all acts.
  - Hover any stat for community comparison tooltip.
  个人生涯统计（百科大全 → 角色数据）
  - 胜率汇总（进阶/角色筛选，10/50/100/全部局胜率趋势）。
  - 死因排行（含遭遇图标）。
  - 每幕卡组构筑（获取/购买/删除/升级）与路线统计（小怪/精英/?房/商店/火堆）。
  - 先古遗物选取率按 Elder 分组、逐池展示。
  - 全幕 Boss 战损。
  - 悬停任意统计项查看社区对比数据。

  Run History Enhancement
  - Single-run stats popup: deck building, path stats, ancient picks, boss damage.
  - "View Contribution Chart" button to replay that run's combat contributions.
  历史记录增强
  - 本局统计弹窗：卡组构筑、路线统计、先古遗物选取、Boss 战损。
  - "查看贡献图表"按钮，回看过往战斗贡献。

  Compendium Personal Stats
  - Card Library: personal + community pick rate, win rate, upgrade rate, removal rate, buy rate per card.
  - Relic Collection: personal + community win rate and win rate delta.
  百科大全个人统计
  - 卡牌图书馆：每张卡的个人 + 社区选取率、胜率、升级率、删除率、购买率。
  - 遗物收藏：个人 + 社区胜率与胜率浮动。

  Settings Panel (F9)
  - Upload toggle: opt out of data upload.
  - Ascension range filter, min player win rate filter, character filter.
  - Game version filter, data branch (Release/Beta).
  - Feature toggles: independently enable/disable each sub-feature.
  - Language: 中文 / English.
  设置面板 (F9)
  - 上传开关：可选择不上传数据。
  - 进阶范围筛选、最低玩家胜率筛选、角色筛选。
  - 游戏版本筛选、数据分支（正式版/Beta）。
  - 功能开关：独立启用/禁用各子功能。
  - 语言切换（中文/English）。

  Data Upload & Privacy
  - Uploads one payload per completed run (win or loss). Disable anytime in F9 settings.
  - Uploaded: character, ascension, win/loss, floor, card choices, event choices, shop purchases, combat
  contributions, final deck/relics.
  - NOT uploaded: Steam ID, personal info, system data.
  - Offline queue: saves uploads locally when server is unreachable, retries on next launch.
  数据上传与隐私
  - 每局结束后上传一次（胜利或失败）。可在 F9 设置中随时关闭。
  - 上传内容：角色、进阶、胜负、楼层、选牌记录、事件选择、商店购买、战斗贡献、最终卡组/遗物。
  - 不上传：Steam ID、个人身份信息、系统数据。
  - 离线队列：服务器不可达时本地保存，下次启动自动重传。

  Requirements / 依赖
  - No other mods required.
  - Internet connection for community data. Local features work offline.
  - 无需其他 Mod。
  - 社区数据需联网。本地功能可离线使用。

  Shout Outs
  感谢所有参与测试和提供支持的人。
  联机支持：联机时可正常加载，仅统计本地玩家贡献数据，但未经足量测试。
  仓库：https://github.com/reddots1453/STS2mod-Stats_the_Spire
  Thanks to everyone who helped test and provided support.
  Multiplayer: loads normally and tracks only the local player's contribution data, but has not been thoroughly tested.
  Repository: https://github.com/reddots1453/STS2mod-Stats_the_Spire
