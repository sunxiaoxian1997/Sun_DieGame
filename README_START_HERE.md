# 《我死后成为了关卡机关》Codex + Unity 实施教程

## 目标

本教程用于指导 Codex 从零实现一个可验证的 Unity 灰盒原型。

第一阶段只完成以下闭环：

1. 玩家可以移动和跳跃。
2. 玩家触碰尖刺后死亡。
3. 死亡位置生成一具具有物理碰撞的普通尸体。
4. 新玩家在出生点复活。
5. 尸体不会随着复活消失。
6. 尸体可以压住压力开关。
7. 压力开关打开门。
8. 新玩家通过门抵达出口。
9. 生命耗尽后关卡失败。
10. 重新开始时清除本轮尸体并恢复机关。

第一阶段明确不做：

- 火尸、冰尸、电尸、毒尸。
- 正式美术、动画、音频和剧情。
- 背包、技能树、存档、关卡选择。
- Roguelike、程序化关卡和联网。
- 复杂角色状态机。
- 第三方框架。
- 正式性能优化。

---

# 第一部分：准备开发环境

## 1. 创建 Unity 项目

建议：

- 使用你当前稳定安装的 Unity 版本，不要在原型阶段迁移版本。
- 项目模板选择 2D Core。
- 工作项目名可暂定为 `CorpseMechanismPrototype`。
- 项目路径避免中文、空格和过长目录。

示例：

```text
D:\UnityProjects\CorpseMechanismPrototype
```

## 2. Unity 项目设置

进入 Unity：

### Version Control

```text
Edit
→ Project Settings
→ Version Control
→ Mode: Visible Meta Files
```

### Asset Serialization

```text
Edit
→ Project Settings
→ Editor
→ Asset Serialization
→ Mode: Force Text
```

### 输入系统

第一阶段可由 Codex判断项目当前使用的是：

- Input System Package；
- 或旧版 Input Manager。

不要在同一个任务中同时迁移输入系统和开发角色控制器。若新项目默认已经使用 Input System，则继续使用 Input System。

## 3. 初始化 Git

在项目根目录执行：

```powershell
git init
git add .
git commit -m "chore: initialize Unity prototype"
```

在任何 Codex 大任务前创建 Git 检查点。

## 4. 打开 Codex

推荐优先使用：

- VS Code + Codex 扩展；
- 或在项目根目录运行 Codex CLI。

Codex 的工作目录必须是 Unity 项目根目录，即同时包含：

```text
Assets/
Packages/
ProjectSettings/
AGENTS.md
```

---

# 第二部分：导入本启动包

将本压缩包中的内容复制到 Unity 项目根目录。

复制后应得到：

```text
CorpseMechanismPrototype/
├── AGENTS.md
├── README_START_HERE.md
├── Docs/
├── Prompts/
├── Tools/
├── Assets/
├── Packages/
└── ProjectSettings/
```

然后提交：

```powershell
git add .
git commit -m "docs: add Codex project guidance and implementation plan"
```

---

# 第三部分：配置 Unity 命令行路径

在 PowerShell 中设置本次终端会话使用的 Unity Editor 路径：

```powershell
$env:UNITY_EDITOR_PATH = "C:\Program Files\Unity\Hub\Editor\<你的Unity版本>\Editor\Unity.exe"
```

验证：

```powershell
& $env:UNITY_EDITOR_PATH -version
```

如需长期保存，可在 Windows 环境变量中创建：

```text
UNITY_EDITOR_PATH
```

注意：

- Unity Batch Mode 运行时，不要让同一个项目同时在 Unity Editor 中保持打开状态。
- 执行自动测试前先关闭 Unity，或者复制一个独立工作树用于测试。

---

# 第四部分：Codex 工作规则

每个任务都使用以下顺序：

```text
1. 让 Codex 读取 AGENTS.md 和 Docs。
2. 让 Codex 先检查现状。
3. 对复杂任务先输出计划。
4. 计划确认后再实施。
5. Codex 修改代码。
6. Codex运行编译、测试或验证命令。
7. Codex总结改动文件、验证结果和已知问题。
8. 运行 /review 检查未提交变更。
9. 你在 Unity 中实际试玩。
10. 通过后提交 Git。
```

任务提示词必须包含：

- Goal：本任务要完成什么。
- Context：相关目录、规则和现状。
- Constraints：禁止事项与架构约束。
- Done when：可验证的完成条件。

一次只给 Codex 一个清晰任务。不要直接要求“完成整个游戏”。

---

# 第五部分：第一阶段任务顺序

严格按以下顺序推进。

## 任务 0：项目审计，只制定计划

使用：

```text
Prompts/00_PROJECT_AUDIT.md
```

此任务禁止 Codex 修改文件。

完成标准：

- Codex识别 Unity 版本。
- Codex识别输入系统。
- Codex识别当前 Package。
- Codex识别项目是否能编译。
- Codex给出第一阶段实施计划。
- Codex列出风险和需要创建的文件。

## 任务 1：工程基础设施

使用：

```text
Prompts/01_INFRASTRUCTURE.md
```

预期生成：

```text
Assets/_Game/
├── Runtime/
│   ├── Core/
│   ├── Player/
│   ├── Death/
│   ├── Corpse/
│   ├── Interaction/
│   ├── Level/
│   └── UI/
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
├── Editor/
└── Scenes/
```

同时建立：

- Assembly Definition；
- ProjectAutomation；
- 命令行编译验证；
- Edit Mode 和 Play Mode 测试入口；
- Windows Development Build；
- 日志输出目录。

## 任务 2：角色移动

使用：

```text
Prompts/02_PLAYER_MOVEMENT.md
```

只实现：

- 左右移动；
- 跳跃；
- 地面检测；
- 基础重力；
- 输入开关；
- 调试 Gizmo；
- 参数可配置。

不要实现：

- 冲刺；
- 二段跳；
- 攀爬；
- 移动平台适配；
- 动画状态机；
- 土狼时间和跳跃缓冲，除非第一轮试玩确认需要。

## 任务 3：死亡与复活

使用：

```text
Prompts/03_DEATH_RESPAWN.md
```

只实现：

- `IDamageSource`；
- `DeathType.Normal`；
- 玩家死亡事件；
- 玩家输入和物理禁用；
- 在死亡地点记录快照；
- 延迟复活；
- 生命数量；
- 关卡失败；
- 重开关卡。

## 任务 4：普通尸体

使用：

```text
Prompts/04_CORPSE_SYSTEM.md
```

只实现普通尸体：

- 保留死亡位置；
- 具有 Rigidbody2D 和 Collider2D；
- 可被玩家踩踏；
- 可以压住开关；
- 不自动销毁；
- 不与新玩家身份混淆；
- 关卡重开时统一清理。

## 任务 5：压力开关与门

使用：

```text
Prompts/05_PRESSURE_DOOR.md
```

只实现：

- `IWeightedObject`；
- 玩家和尸体都提供重量；
- 开关统计区域内有效重量；
- 门根据开关状态打开和关闭；
- 不直接判断对象名称或 Tag；
- 不让开关直接查找 Player 单例。

## 任务 6：第一个完整灰盒关卡

使用：

```text
Prompts/06_FIRST_LEVEL.md
```

目标关卡：

```text
出生点
→ 玩家跨过小障碍
→ 主动死在压力开关上
→ 普通尸体持续压住开关
→ 新玩家复活
→ 门保持开启
→ 新玩家通过门
→ 到达出口
```

生命上限建议为 3。

---

# 第六部分：测试策略

## Edit Mode 测试

适合验证纯规则：

- 生命扣减；
- 死亡类型映射；
- 尸体定义；
- 重量累计；
- 压力开关阈值；
- 门状态逻辑；
- 重开时状态清理。

## Play Mode 测试

适合验证场景行为：

- 玩家触碰尖刺后死亡；
- 尸体生成位置接近死亡位置；
- 玩家在出生点复活；
- 尸体保持在场景；
- 尸体压住开关后门打开；
- 移走尸体后门关闭；
- 生命耗尽后进入失败状态。

## 人工试玩

自动测试不能判断：

- 移动是否顺手；
- 跳跃是否舒服；
- 死亡反馈是否清楚；
- 尸体是否容易意外滚走；
- 玩家是否理解要主动死亡；
- 解谜是否有“原来如此”的感受。

每完成一个功能，必须在 Unity 中人工试玩。

---

# 第七部分：Git 工作流

每个功能单独分支：

```powershell
git switch -c feat/player-movement
```

完成后：

```powershell
git status
git diff
git add .
git commit -m "feat: add basic 2D player movement"
```

推荐分支：

```text
feat/project-infrastructure
feat/player-movement
feat/death-respawn
feat/corpse-system
feat/pressure-door
feat/level-001
```

### 创建 Pull Request

功能分支验证通过后，推送并发起 PR：

```powershell
git push -u origin feat/player-movement
gh pr create --base main --title "feat: add basic 2D player movement" --body "实现左右移动、跳跃与地面检测。"
```

PR 描述应说明目标、改动文件和验证方式。合并前确保：

- Edit Mode / Play Mode 测试通过；
- `/review` 无遗留问题；
- Git 工作区干净。

Codex完成任务后，先执行：

```text
/review
```

要求它检查：

- 空引用风险；
- Unity生命周期使用错误；
- 物理更新位置；
- 事件未注销；
- 场景重载残留；
- 不必要的单例；
- 测试遗漏；
- Editor API 泄漏到 Runtime；
- YAML 资源引用风险。

---

# 第八部分：故障处理

## Unity 编译失败

把以下内容完整交给 Codex：

- Console 第一条错误；
- 完整堆栈；
- 相关脚本；
- 最近一次变更；
- Unity 版本；
- 复现步骤。

提示词格式：

```text
修复以下 Unity 编译错误。先找到根因，不要通过删除功能或注释测试绕过。
完成后运行编译验证和相关测试。
错误：
<粘贴完整错误>
```

## Batch Mode 失败

检查：

1. Unity项目是否仍在 Editor 中打开。
2. `UNITY_EDITOR_PATH` 是否正确。
3. `-logFile` 指向的日志。
4. 是否存在编译错误。
5. ProjectAutomation 方法是否为 public static。
6. Editor 代码是否位于 Editor 文件夹或 Editor Assembly 中。

## Codex 修改范围过大

立即要求：

```text
停止继续扩展。请撤销与本任务验收标准无关的改动，只保留实现当前目标所必需的最小改动。列出被撤销和保留的文件。
```

## Codex 擅自增加机制

在 AGENTS.md 中追加：

```text
- Do not introduce a new gameplay mechanic unless the current task explicitly requires it.
```

---

# 第九部分：第一阶段验收标准

只有全部满足后，才进入火尸系统。

## 功能

- [ ] 玩家可以稳定移动和跳跃。
- [ ] 玩家触碰尖刺后死亡。
- [ ] 死亡地点生成普通尸体。
- [ ] 玩家在出生点复活。
- [ ] 尸体在复活后继续存在。
- [ ] 玩家可以踩在尸体上。
- [ ] 尸体可以压住压力开关。
- [ ] 压力开关能打开门。
- [ ] 新玩家可以通过门到达出口。
- [ ] 生命耗尽后失败。
- [ ] 重开后尸体、门、开关和生命全部恢复。

## 工程

- [ ] Runtime 与 Editor 程序集分离。
- [ ] Edit Mode 测试通过。
- [ ] Play Mode 测试通过。
- [ ] Windows Development Build 成功。
- [ ] 关键参数在 Inspector 可配置。
- [ ] 没有依赖对象名称查找。
- [ ] 没有不必要的全局单例。
- [ ] 没有第三方运行时依赖。
- [ ] Git 工作区干净。

## 体验

- [ ] 玩家可以理解死亡产生尸体。
- [ ] 尸体不会因物理抖动破坏谜题。
- [ ] 死亡到复活等待时间不过长。
- [ ] 玩家能明确看到生命数量。
- [ ] 门和开关反馈清晰。
- [ ] 第一次主动死亡具有明显意义。

---

# 第十部分：现在立即执行

1. 创建或打开 Unity 2D 项目。
2. 配置 Visible Meta Files 和 Force Text。
3. 初始化 Git 并提交。
4. 将本启动包复制到项目根目录。
5. 打开 VS Code 和 Codex。
6. 将 `Prompts/00_PROJECT_AUDIT.md` 完整发送给 Codex。
7. 不允许它修改文件，只接收审计和计划。
8. 检查计划是否严格限定在第一阶段。
9. 再执行 `Prompts/01_INFRASTRUCTURE.md`。
10. 每完成一个任务都先 `/review`，再进入 Unity 试玩和提交。
