# SabberStone 数据节点升级计划：38377 -> 39282

## 结论

当前 SabberStone 本地卡牌数据是 `CardDefs build="38377"`。它的后一个 HearthSim/hsdata 公开数据节点是 `39282`，对应炉石版本 **16.0.8.39282**，时间为 **2020-01-09**。

这一步不是新扩展包，也没有新增卡牌。它是一次平衡补丁，主要内容是：

- 新增卡牌（Added Cards）：0
- 删除卡牌（Removed Cards）：0
- 字段发生变化的卡牌/实体（Changed Entities）：19
- 标准/狂野构筑相关变动：7 张实体
- 酒馆战棋相关变动：12 张实体

数据来源：

- 旧数据：`SabberStoneCore/resources/Data/CardDefs.xml`，build `38377`
- 新数据：`analysis/CardDefs-39282.xml`，来自 `HearthSim/hsdata` tag `39282`
- 补丁说明参考：[Hearthstone Wiki - Patch 16.0.8.39282](https://hearthstone.wiki.gg/wiki/Card_changes#Patch_16.0.8.39282)
- 版本表参考：[Hearthstone Wiki - Patches](https://hearthstone.wiki.gg/wiki/Patches)

## 全量变动清单

### 构筑模式相关

| CardID | 中文说明 | English | 变动字段 | 旧值 | 新值 | SabberStone 处理方式 |
|---|---|---|---|---|---|---|
| `DRG_019` | 破灭之裔 | Scion of Ruin | `COST` | 3 | 4 | 更新 `CardDefs.xml` 即可，逻辑暂未实现 |
| `DRG_025` | 海盗之锚 | Ancharrr | `DURABILITY` | 3 | 2 | 更新 `CardDefs.xml` 即可，逻辑暂未实现 |
| `DRG_031` | 死金药剂师 | Necrium Apothecary | `COST` | 4 | 5 | 更新 `CardDefs.xml` 即可，逻辑暂未实现 |
| `DRG_089` | 红龙女王阿莱克丝塔萨 | Dragonqueen Alexstrasza | `CARDTEXT` | add 2 random Dragons | add 2 other random Dragons | 更新文本；后续实现时必须排除自身 |
| `DRG_217` | 巨龙的兽群 | Dragon's Pack | `CARDTEXT` | 触发后 +3/+3 | 触发后 +2/+2 | 更新文本；后续实现时按 +2/+2 |
| `DRG_217e` | 迦拉克隆之力 | Galakrond's Power | `CARDTEXT` | +3/+3 | +2/+2 | 更新文本；若实现附魔，数值必须改为 +2/+2 |
| `DRG_248` | 霜之祈咒 | Invocation of Frost | `COST` | 1 | 2 | 更新 `CardDefs.xml` 即可，逻辑暂未实现 |
| `DRG_250` | 邪魔仪式 | Fiendish Rites | `COST` | 3 | 4 | 更新 `CardDefs.xml` 即可，逻辑暂未实现 |

注意：这里列出 8 个实体，但其中 `DRG_217e` 是 `DRG_217` 的附魔实体，不是独立可收藏卡。按可收藏卡看，是 7 张构筑卡被削弱。

### 酒馆战棋相关

SabberStone 当前主要目标是标准/狂野战斗模拟，酒馆战棋可以暂时不进入第一阶段实现范围。不过 `CardDefs.xml` 数据升级会一并带入这些字段变化。

| CardID | 中文说明 | English | 变动字段 | 旧值 | 新值 | 处理方式 |
|---|---|---|---|---|---|---|
| `BGS_002` | 灵魂杂耍者 | Soul Juggler | `CARDTEXT` | Whenever... deal 3 damage | After... deal 3 damage | 仅文本变化 |
| `TB_BaconShop_HERO_18` | 帕奇斯 | Patches the Pirate | `BACON_HERO_CAN_BE_DRAFTED` | 无 | 1 | 酒馆战棋字段，暂不实现 |
| `TB_BaconShop_HERO_39` | 金字塔 | Pyramad | `BACON_HERO_CAN_BE_DRAFTED` | 无 | 1 | 酒馆战棋字段，暂不实现 |
| `TB_BaconShop_HP_022` | 吵吵 | Burbling | `COST` | 2 | 1 | 酒馆战棋英雄技能 |
| `TB_BaconShop_HP_027` | 开火！ | Fire the Cannons! | `CARDTEXT` | 造成 3 点伤害 | 造成 4 点伤害 | 酒馆战棋英雄技能 |
| `TB_BaconShop_HP_028` | 时光酒馆 | Temporal Tavern | `COST` | 2 | 1 | 酒馆战棋英雄技能 |
| `TB_BaconShop_HP_028` | 时光酒馆 | Temporal Tavern | `CARDTEXT` | Add a minion... | Include a minion... | 文本语义调整 |
| `TB_BaconShop_HP_037a` | 蜡质战队 | Wax Warband | `CARDTEXT` | +1 Health | +2 Attack | 酒馆战棋英雄技能 |
| `TB_BaconShop_HP_037te` | 上蜡 | Waxed | `CARDTEXT` | Increased Health | Increased Attack | 酒馆战棋附魔 |
| `TB_BaconShop_HP_040` | 添砖加瓦 | Brick by Brick | `CARDTEXT` | +2 Health | +3 Health | 酒馆战棋英雄技能 |
| `TB_BaconShop_HP_040e` | 建造 | Built Up | `CARDTEXT` | +2 Health | +3 Health | 酒馆战棋附魔 |
| `TB_BaconUps_075` | 金色灵魂杂耍者 | Soul Juggler | `COST` | 2 | 3 | 酒馆战棋金色随从 |
| `TB_BaconUps_075` | 金色灵魂杂耍者 | Soul Juggler | `CARDTEXT` | deal 6 damage | deal 3 damage twice | 酒馆战棋逻辑变化 |

## 对 SabberStone 的影响判断

### 只需要数据升级的内容

以下字段由 `CardDefs.xml` 驱动，理论上替换数据文件后即可生效：

- 费用（`COST`）
- 武器耐久（`DURABILITY`）
- 英文卡牌文本（`CARDTEXT`）
- 酒馆战棋专用草案字段（`BACON_HERO_CAN_BE_DRAFTED`）

因此，`DRG_019`、`DRG_025`、`DRG_031`、`DRG_248`、`DRG_250` 这些数值改动不需要先改核心引擎。

### 需要后续逻辑实现时注意的内容

`DRG_089`、`DRG_217`、`DRG_217e` 是文本/效果语义变化。当前 SabberStone 中这些牌在 `DragonsCardsGen.cs` 里仍是 TODO，没有完整实现。因此本次迁移不需要立即修逻辑，但以后实现这些牌时必须按 39282 规则：

- **红龙女王阿莱克丝塔萨（Dragonqueen Alexstrasza）**：随机龙池必须排除自身，即 `other random Dragons`。
- **巨龙的兽群（Dragon's Pack）**：祈求两次后的强化从 `+3/+3` 改为 `+2/+2`。
- **迦拉克隆之力（Galakrond's Power）**：附魔文本和实际附魔值应为 `+2/+2`。

## 迁移计划

### 第 1 步：数据文件升级

用 `analysis/CardDefs-39282.xml` 替换：

```text
SabberStoneCore/resources/Data/CardDefs.xml
```

同时保留一份旧数据快照，方便回滚和后续 diff：

```text
SabberStoneCore/resources/Data/CardDefs-38377.xml
```

可选：如果项目里已有旧快照命名规则，也可以沿用 `CardDefs-36393.xml` 这种模式，新增 `CardDefs-38377.xml`。

### 第 2 步：同步生成代码注释

`DragonsCardsGen.cs` 和 `DragonsCardsGenTest.cs` 里的注释仍显示旧费用，例如：

- `DRG_019` 仍写 `COST:3`
- `DRG_031` 仍写 `COST:4`
- `DRG_248` 仍写 `COST:1`
- `DRG_250` 仍写 `COST:3`

这些注释不一定影响运行，但会误导后续补卡。建议同步改掉。

### 第 3 步：补最小测试

新增或调整一个数据一致性测试，验证以下字段已经变成 39282：

- `DRG_019` cost = 4
- `DRG_025` durability = 2
- `DRG_031` cost = 5
- `DRG_248` cost = 2
- `DRG_250` cost = 4

这一步不要求实现所有卡牌效果，只验证数据层升级正确。

### 第 4 步：标注未实现逻辑风险

对 `DRG_089`、`DRG_217`、`DRG_217e` 保留 TODO，但 TODO 应明确写 39282 规则，避免后续按旧文本实现。

### 第 5 步：跑现有测试

执行：

```powershell
dotnet build SabberStone.sln -c Debug --no-restore
dotnet test SabberStoneCoreTest\SabberStoneCoreTest.csproj -c Debug --no-restore --verbosity quiet
```

预期：如果只是数据升级，主要风险来自已有测试对旧费用/旧文本有硬编码断言。当前这些卡对应测试多为 skipped TODO，风险较低。

## 是否进入新机制补齐

本节点 `38377 -> 39282` 不引入新机制，不需要补：

- 恶魔猎手（Demon Hunter）
- 流放（Outcast）
- 休眠（Dormant）
- 至尊/终极形态牌（Prime）
- 法术迸发（Spellburst）
- 腐蚀（Corrupt）

这些要从后续 2020 数据节点继续推进。下一轮应比较：

```text
39282 -> 39954
```

如果 `39954` 仍然只是小补丁，则继续顺推，直到遇到第一个包含新卡/新机制的 build。
