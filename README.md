# 《我死后成为了关卡机关》

一个基于 Unity 2D 的灰盒解谜原型：玩家死亡后会在原地留下一具具有物理碰撞的普通尸体，尸体可以压住压力开关、顶开门，成为通关的关键机关。

## 游戏概念

玩家需要主动利用自己的「死亡」来破解关卡：

1. 玩家可以移动和跳跃。
2. 玩家触碰尖刺后死亡。
3. 死亡位置生成一具具有物理碰撞的普通尸体。
4. 新玩家在出生点复活，但尸体不会消失。
5. 尸体可以压住压力开关，开关打开门。
6. 新玩家通过门抵达出口即通关。
7. 生命耗尽后关卡失败；重开后尸体、门、开关与生命全部复位。

## 第一阶段功能范围

**已实现 / 计划实现（灰盒原型）：**

- 左右移动、跳跃、地面检测、基础重力。
- 尖刺致死、`IDamageSource`、玩家死亡事件。
- 普通尸体系统：保留死亡位置、带 `Rigidbody2D` 与 `Collider2D`、可被踩踏、可压开关、不自动销毁。
- 压力开关与门：`IWeightedObject`，玩家与尸体都提供重量，门按开关状态开合。
- 第一个完整灰盒关卡（生命上限 3）。
- 死亡到复活循环、关卡失败、重开关卡。

**第一阶段明确不做：** 火/冰/电/毒尸、正式美术与音频、背包与技能树、存档与关卡选择、Roguelike 与联网、复杂状态机、性能优化。

## 技术栈

- 引擎：Unity `2022.3.62f3`（2D Core 模板）
- 语言：C#
- 输入：Input System Package（沿用项目默认）
- 序列化：`Force Text` + `Visible Meta Files`
- 测试：Edit Mode / Play Mode 测试

## 快速开始

```powershell
# 克隆仓库
gh repo clone sunxiaoxian1997/Sun_DieGame
# 用 Unity Hub 打开项目根目录，等待 Library 生成后即可在 Editor 中试玩
```

打开后建议确认：

- `Edit → Project Settings → Version Control → Mode: Visible Meta Files`
- `Edit → Project Settings → Editor → Asset Serialization → Mode: Force Text`

## 操作方式

| 操作 | 按键 |
| --- | --- |
| 移动 | A / D 或 ← / → |
| 跳跃 | Space / W |
| 主动死亡（测试用） | 触碰尖刺即可触发 |

## 工程结构

```text
Sun_DieGame/
├── Assets/_Game/        # 运行时代码、测试、场景
├── Docs/                # 设计文档
├── Prompts/             # Codex 任务提示词
├── Tools/               # 命令行/构建工具
├── AGENTS.md
├── README.md
└── README_START_HERE.md # 完整实施教程与开发规范
```

## 开发流程

详细的从零实施教程、任务拆分与 Git 工作流见 [`README_START_HERE.md`](./README_START_HERE.md)。

功能开发约定：

- 每个功能使用独立分支（如 `feat/player-movement`）。
- 提交前运行 Edit Mode / Play Mode 测试，并在 Unity 中人工试玩。
- 完成后发起 Pull Request 到 `main`，合并前保证工作区干净。

## License

本仓库当前未包含许可证文件。如需开源，请补充合适的 LICENSE。
