# LyoMirWorld
传奇世界 C# 版服务端，基于淡抹夕阳 06 年代码，支持 Mac / Linux / Windows 跨平台运行。

**当前分支**: `dev` — 集成所有新增功能

---

## 快速开始

### 环境要求
- .NET 8.0 SDK+
- 配套资源文件（QQ群：1063081017）

### 编译运行

```bash
# Windows / Linux / Mac 通用
dotnet restore LyoMirWorldServer.sln
dotnet build LyoMirWorldServer.sln -c Release

# 启动各服务器（按顺序）
dotnet run --project DBServer/DBServer.csproj
dotnet run --project LoginServer/LoginServer.csproj
dotnet run --project GameServer/GameServer.csproj
```

> Windows 用户推荐使用 [LyoConsole 管理器](https://github.com/AndrewChien/LyoConsole/releases/tag/v1.0.0) 一键启停。

### 客户端配套
- [登录器](https://github.com/AndrewChien/LyoMirWorldLauncher/releases/tag/v1.0.0)
- [登录器配置器](https://github.com/AndrewChien/LyoMirWorldLauncher/releases/tag/v1.0.0)
- 资源文件：QQ群 1063081017

---

## 项目结构

```
LyoMirWorld/
├── GameServer/          # 游戏服务器（核心）
├── LoginServer/         # 登录服务器
├── DBServer/           # 数据库服务器
├── ServerCenter/        # 服务器中心
├── SelectCharServer/    # 角色选择服务器
├── MirCommon/           # 公共库（网络/数据库/数据结构）
├── SqlFiles/            # 数据库SQL脚本
├── data/                # 游戏数据配置
└── LyoMirWorldServer.sln
```

---

## 已实现系统

| 系统 | 文件 | 说明 |
|------|------|------|
| 玩家对象 | HumanPlayer.cs | 核心玩家逻辑 |
| 物品/背包 | ItemSystem.cs | 装备/消耗品/仓库 |
| 技能系统 | SkillSystem.cs | 技能/魔法/Buff |
| 战斗系统 | CombatSystem.cs | 伤害计算/攻击判定 |
| 任务系统 | QuestSystem.cs | 任务/奖励 |
| 交易市场 | MarketManager.cs | 拍卖/交易 |
| 挖矿系统 | MiningSystem.cs | 挖矿/宝石资源 |
| 聊天系统 | ChatSystem.cs | 聊天/社交 |
| 公会系统 | GuildSystem.cs | 行会/攻城 |
| 组队系统 | GroupObject.cs | 组队/配合 |
| 摆摊系统 | StallSystem.cs | 摆摊/经济 |
| 沙城系统 | SandCity.cs | 攻城战 |
| NPC系统 | NPCSystem.cs | NPC对话/商店 |
| 宠物/坐骑 | PetSystem.cs / MountSystem.cs | 宠物/坐骑 |
| 邮件系统 | MailSystem.cs | 玩家邮件/附件 |
| 成就系统 | AchievementSystem.cs | 成就/称号 |
| Buff系统 | BuffSystem.cs | 状态效果 |

---

## 新增功能（dev分支）

### 每日签到系统 ✅
- NPC"福利姐姐"对话触发
- 7天循环奖励递增（3万~30万金币 + 祝福油）
- JSON文件持久化（`Data/SignIn/{角色ID}.json`）
- GM命令：`@SIGNIN`

### 在线挂机系统 ✅
- 自动寻怪 / 攻击 / 掉落拾取
- 药水自动使用（HP/MP）
- 技能自动释放
- 安全区自动停止
- GM命令：`@TRAIN` / `@STOPTRAIN`

### 宝石镶嵌系统 ✅
- 装备打孔（按品质：白1/绿2/蓝3/紫4/橙5/金6孔）
- 6种宝石：攻击 / 防御 / 生命 / 幸运 / 魔法 / 经验
- 属性直接叠加到装备总属性
- GM命令：`@PUNCHHOLE` / `@INLAY` / `@REMOVEGEM`

### VIP会员系统 ✅
- 8级VIP：体验 / 基础 / 中级 / 高级 / 终极 / 至尊 / 钻石 / 钻石
- 每日礼包：金币 + 道具（按等级递增）
- 经验加成：5% ~ 80%
- 爆率加成：5% ~ 100%
- 仓库扩充 / 摆摊栏位增加
- 专属VIP地图（2013-2015）
- 累计充值自动升级（10/50/100/200/500/1000/2000/5000元）
- GM命令：`@SETVIP` / `@VIPGIFT`
- JSON持久化（`Data/Vip/{角色ID}.json`）

---

## 跨平台说明

游戏服务器使用标准 .NET `TcpListener` / `TcpClient`，天然支持 Mac / Linux / Windows。

废弃的 Windows-only IOCP 网络引擎（`IocpNetworkEngine.cs`）已标记为 `[Obsolete]` 并从编译中排除，不影响跨平台运行。

---

## 开发说明

- 长期开发分支：`dev`
- 新功能开发请基于 `dev` 分支进行
- 所有新系统使用 JSON 文件存储（`Data/` 目录），减少数据库依赖
- GM命令配置文件：`data/GameMaster/cmdlist.txt`

### GM命令格式
```
@GIVE <物品ID> [数量]     # 给予物品
@GIVEGOLD <数量>          # 给予金币
@GIVEEXP <数量>           # 给予经验
@TRAIN                    # 开始挂机
@STOPTRAIN                # 停止挂机
@PUNCHHOLE <装备名>       # 打孔
@INLAY <装备> <孔位> <宝石> # 镶嵌
@REMOVEGEM <装备> <孔位>  # 取下宝石
@SETVIP <等级0-8> [天数]  # 设置VIP
@VIPGIFT                 # 领取VIP每日礼包
```

---

## 群服及技术交流

QQ群：1063081017（配套资源文件也在此群）

---

<img src='https://github.com/AndrewChien/Blog/blob/master/source/mirworld01.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/mirworld02.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/mw_magic.jpg'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/mw_monster.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/mw_NPC.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/npc.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/pig.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/street.png'/></br>
<img src='https://github.com/AndrewChien/Blog/blob/master/source/mon.png'/></br>
