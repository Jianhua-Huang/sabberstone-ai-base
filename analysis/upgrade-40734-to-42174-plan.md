# SabberStone 数据节点升级计划：40734 -> 42174

当前升级目标是从 `CardDefs build="40734"` 更新到 `42174`。该节点仍属于 2020 年早期版本段，主要同步 Battlegrounds 龙体系数据，不是标准构筑大版本。

## 数据来源

- 旧数据：`analysis/CardDefs-40734.xml`
- 新数据：`analysis/CardDefs-42174.xml`
- 来源：`HearthSim/hsdata` tag `42174`

## 结构化差异结果

本次比对按 `CardID` 解析实体，再比较关键游戏字段，不使用纯文本 diff 作为判断依据。

- 实体总数：`8637 -> 8703`
- 新增实体：`68`
- 删除实体：`2`
- 新增可收集卡：`1`
- 可收集卡字段变化：`0`

## 新增数据范围

### Battlegrounds

新增内容集中在 `CardSet.BATTLEGROUNDS`：

- 新增龙体系随从及金色版本，例如：
  - `BGS_019` Red Whelp
  - `BGS_033` Hangry Dragon
  - `BGS_036` Razorgore, the Untamed
  - `BGS_040` Nadina the Red
  - `BGS_041` Kalecgos, Arcane Aspect
  - `BGS_043` Murozond
- 新增龙相关英雄和英雄技能，例如：
  - `TB_BaconShop_HERO_02` Galakrond
  - `TB_BaconShop_HERO_52` Deathwing
  - `TB_BaconShop_HERO_53` Ysera
  - `TB_BaconShop_HP_011` Galakrond's Greed

### Tavern Brawl

- 新增 `FB_Champs_ULD_169` Mogu Fleshshaper
  - 这是赛事/乱斗用可收集实体，不属于标准构筑卡池。
- 新增 `FB_Champs_DAL_736` Archivist Elysiana。

## 删除数据

删除两个 Battlegrounds 旧英雄技能实体：

- `TB_BaconShop_HP_038` Bananarama
- `TB_BaconShop_HP_045` Power Up!

## 既有字段变化

主要是 Battlegrounds 平衡和文本：

- `BGS_021` Mama Bear
  - `ATK`: `4 -> 5`
  - `HEALTH`: `4 -> 5`
  - 文本从 `+4/+4` 调整为 `+5/+5`
- `TB_BaconUps_090` Mama Bear
  - `ATK`: `8 -> 10`
  - `HEALTH`: `8 -> 10`
  - 文本从 `+8/+8` 调整为 `+10/+10`
- `TB_BaconShop_HP_010` Boon of Light
  - `COST`: `4 -> 3`
- `TB_BaconShop_HP_046` Gonna Be Rich!
  - 原实体从 `Gatling Wand` 改为 `Gonna Be Rich!`
  - `COST`: `2 -> 4`
- `TB_BaconShopTechUp06_Button` Tavern Tier 6
  - `COST`: `11 -> 10`

## 实施计划

1. 用 `analysis/CardDefs-42174.xml` 替换 `SabberStoneCore/resources/Data/CardDefs.xml`。
2. 新增数据补丁测试，验证：
   - build `42174` 的新增 Battlegrounds 龙体系实体可以加载。
   - `Mama Bear` 和金色 `Mama Bear` 的攻血修正已生效。
   - 关键英雄技能费用修正已生效。
   - 被删除的旧 Battlegrounds 英雄技能不再存在。
3. 跑数据补丁相关测试和完整测试。

## 机制评估

本节点没有新增标准构筑机制，也没有标准构筑新卡牌效果要实现。Battlegrounds 逻辑不属于当前 SabberStone 标准对战模拟重点，因此本次只同步数据并加加载/字段测试。
