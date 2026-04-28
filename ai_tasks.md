# LyoMirWorld 开发任务清单

## 项目目标
- 支持 Mac/Linux/Windows 跨平台运行
- 对接官方传奇世界客户端，补全缺失功能
- 适合单人/群服游玩（非商业开服）
- 在一定范围内进行代码重构

## 现状确认

### 跨平台状态
- **网络层**：已使用 .NET 标准 TcpListener/TcpClient，天然跨平台 ✅
- **IOCP引擎**：`IocpNetworkEngine.cs` 已被 `<Compile Remove>` 排除，不影响生产 ✅
- **无需跨平台改造**，已可直接编译运行于 Mac/Linux

### 已有的完整系统 ✅
| 系统 | 文件 | 说明 |
|------|------|------|
| 玩家对象 | HumanPlayer.cs (3706行) | 核心玩家逻辑 |
| 物品/背包 | ItemSystem.cs (1875行) | 装备/消耗品/仓库 |
| 技能系统 | SkillSystem.cs (1453行) | 技能/魔法/Buff |
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
| 宠物系统 | PetSystem.cs | 宠物召唤/仓库 |
| 坐骑系统 | MountSystem.cs | 骑乘/训练 |
| 邮件系统 | MailSystem.cs | 玩家邮件/附件 |
| 成就系统 | AchievementSystem.cs | 成就/称号 |
| Buff系统 | BuffSystem.cs | 状态效果 |

### 缺失功能（需要新增）❌
| 功能 | 优先级 | 说明 |
|------|--------|------|
| **每日签到** | P1 | DailySignInSystem，每日奖励领取 |
| **在线挂机** | P1 | OnlineTrainingSystem，自动打怪/捡物/技能 |
| **宝石镶嵌** | P2 | GemInlaySystem，装备打孔+宝石镶嵌 |
| **VIP/会员系统** | P3 | VIP福利/特权/每日礼包 |

### 代码重构任务
| 优先级 | 任务 | 文件 | 说明 |
|--------|------|------|------|
| P1 | 拆分 HumanPlayer | HumanPlayer.cs (3706行) | 按职责拆分为 PlayerCore/Inventory/Quest 子组件 |
| P1 | 拆分 HumanPlayerEx | HumanPlayerEx.cs (3484行) | 同上，Ex方法按类别分离 |
| P2 | 清理废弃 IOCP 代码 | IocpNetworkEngine.cs | 删除或标记为 obsolete，清理 DllImport |
| P2 | 重构 ConfigLoader | ConfigLoader.cs (2521行) | 抽象配置结构，分离数据校验 |
| P3 | 统一数据库接口 | DatabaseManager.cs | 完善抽象层，移除硬依赖 |

---

## 任务列表

### Phase 1: 核心缺失功能

- [x] **P1-TASK-001**: 新增 DailySignInSystem（每日签到）✅
  - 签到奖励配置（7天循环递增奖励：金币/经验/祝福油）
  - JSON文件持久化（Data/SignIn/{charId}.json），无DB依赖
  - NPC"福利姐姐"对话触发 ✅
  - 验证：连续签到7天奖励递增，断签重置

- [x] **P1-TASK-002**: 新增 OnlineTrainingSystem（在线挂机）✅
  - 挂机状态管理（开始/暂停/结束）
  - 自动寻怪/攻击/拾取
  - 自动使用药水/技能
  - 挂机范围和安全区检测
  - 客户端协议对接（挂机命令/状态同步）
  - 验证：角色可在无人操作下持续战斗

- [x] **P2-TASK-001**: 新增 GemInlaySystem（宝石镶嵌）✅
  - 打孔/镶嵌/取下完整流程
  - 按装备品质决定最大孔数（白1/绿2/蓝3/紫4/橙5/金6）
  - 6种宝石类型（攻击/防御/生命/幸运/魔法/经验）
  - 装备 ExtraStats 存储孔位，无DB依赖
  - 客户端协议对接
  - 验证：武器可镶嵌多颗宝石，属性正确叠加

- [x] **P3-TASK-001**: 新增 VipSystem（VIP会员）✅
  - VIP等级定义（1-8级：体验/基础/中级/高级/终极/至尊/钻石/钻石）
  - VIP专属福利：每日礼包/经验加成(5%~80%)/爆率加成(5%~100%)/专属地图/仓库扩充
  - VIP状态持久化（Data/Vip/{charId}.json），无DB依赖
  - 累计充值自动升级（10/50/100/200/500/1000/2000/5000元）
  - @SETVIP / @VIPGIFT GM命令
  - 验证：不同VIP等级享受对应特权

### Phase 2: 重构

- [ ] **P1-REFACTOR-001**: 重构 HumanPlayer 超大类
  - 将 3706 行按职责拆分为：
    - `HumanPlayerCore.cs` - 核心属性/状态/移动/复活
    - `HumanPlayerInventory.cs` - 背包/装备/仓库操作
    - `HumanPlayerQuest.cs` - 任务相关逻辑
    - `HumanPlayerSocial.cs` - 好友/师徒/夫妻
  - 保持对外接口不变，确保向后兼容
  - 提交前通过编译验证

- [ ] **P1-REFACTOR-002**: 重构 HumanPlayerEx 超大类
  - 将 3484 行 Ex 方法按类别分离到独立文件
  - 保持和 HumanPlayer 的关联不变
  - 清理重复逻辑

- [ ] **P2-REFACTOR-001**: 清理废弃 IOCP 代码
  - 将 `IocpNetworkEngine.cs` 标记为 `[Obsolete]`
  - 清理 `IocpNetworkTest.cs` 中的测试代码或删除
  - 确保 GameServer 不引用任何 IOCP 类
  - 提交前验证项目编译无警告

- [ ] **P2-REFACTOR-002**: 重构 ConfigLoader
  - 将配置数据结构和配置加载逻辑分离
  - 提取常量定义（魔法数字 → 具名常量）
  - 验证：原有配置读取行为不变

### Phase 3: 收尾与验证

- [ ] **FINAL-001**: 全项目编译验证
  - Windows (.NET 8 SDK)
  - Linux (dotnet build)
  - Mac (dotnet build)
  - 无编译错误，无警告

- [ ] **FINAL-002**: 更新 README.md
  - 跨平台运行说明（dotnet run）
  - 缺失功能说明
  - 项目架构图

---

## 执行约束

- **每次完成任务必须 git commit**，格式：`feat: 完成任务 - {任务名}`
- **重构任务必须保持向后兼容**，客户端协议不变
- **新功能需要客户端协议对接**，先确认协议字段再实现
- **超过 1 小时的任务需要每 30 分钟汇报进度**
- **如果需要新增数据库表，先写 SQL 脚本再实现代码**

---

_Last Updated: 2026-04-28_
