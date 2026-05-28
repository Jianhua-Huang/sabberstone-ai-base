# SabberStone 数据节点升级计划：39954 -> 40734

当前升级目标是从 `CardDefs build="39954"` 更新到 `40734`。该节点没有新增或删除卡牌实体，也没有新增可收集卡牌。

## 数据来源

- 旧数据：`analysis/CardDefs-39954.xml`
- 新数据：`analysis/CardDefs-40734.xml`
- 来源：`HearthSim/hsdata` tag `40734`

## 结构化差异结果

本次比对按 `CardID` 解析实体，再比较关键游戏字段，不使用纯文本 diff 作为判断依据。

- 实体总数：`8637 -> 8637`
- 新增实体：`0`
- 删除实体：`0`
- 新增可收集卡：`0`
- 可收集卡字段变化：`1`

## 需要同步的字段变化

### 可收集卡

- `YOD_022` Risky Skipper
  - `CARDRACE`: 空 -> `PIRATE`
  - 影响：这会让它被海盗相关检索、随机池、种族条件正确识别。

### 非可收集衍生物/英雄技能

- `DRG_311a` Spin 'em Up
  - `COST`: 空 -> `1`
- `DRG_311b` Small Repairs
  - `COST`: 空 -> `1`
- `YOD_012ts` Air Raid
  - `RARITY`: `COMMON` -> `RARE`
- `YOD_038t` Sharkbait
  - `RARITY`: `LEGENDARY` -> `INVALID`

### 战场元数据

这些字段属于 Battlegrounds/Bacon 元数据，不影响当前标准对战模拟：

- `TB_BaconShop_HERO_08`
  - 移除 `BACON_HERO_CAN_BE_DRAFTED`
- `BGS_018`
  - `enumID=1421` 的 tag 名称从 `1` 修正为 `BACON_MINION_IS_LEVEL_TWO`
- `BGS_030`
  - `enumID=1421` 的 tag 名称从 `1` 修正为 `BACON_MINION_IS_LEVEL_TWO`

### 文本排版变化

以下实体只有 `CARDTEXT` 排版或本地化文本换行差异，不改变模拟逻辑：

- `CRED_49`
- `TB_RoadToNR_Murgatha`
- `TB_RoadToNR_OrgrimmarGuard`
- `ULDA_BOSS_74h`

## 实施计划

1. 用 `analysis/CardDefs-40734.xml` 替换 `SabberStoneCore/resources/Data/CardDefs.xml`。
2. 新增数据补丁测试，验证：
   - `YOD_022` 被识别为 `Race.PIRATE`。
   - `DRG_311a` 和 `DRG_311b` 的费用为 `1`。
   - `YOD_012ts` 稀有度为 `RARE`。
   - `YOD_038t` 稀有度为 `INVALID`。
3. 战场元数据变更不新增标准对战测试，只通过完整测试确认加载不回归。
4. 跑数据补丁相关测试和完整测试。

## 机制评估

本节点没有新机制，也没有新可收集卡牌，因此不需要新增卡牌效果实现。唯一会影响对战计算的是 `Risky Skipper` 的海盗种族标记，属于数据修正，不需要额外行为代码。
