# SabberStone 数据节点升级计划：39282 -> 39954

## 结论

当前升级目标是从 `CardDefs build="39282"` 更新到 `39954`。该节点已经不是单纯平衡补丁，而是引入了 **迦拉克隆的觉醒（Galakrond's Awakening）** 数据。

结构化 diff 结果：

- 新增实体（Added Entities）：334
- 删除实体（Removed Entities）：0
- 字段变动实体（Changed Entities）：82
- 新增可收藏卡（Collectible Cards）：35

## 新增可收藏卡

本节点新增 35 张 `YOD_*` 可收藏卡，均属于 `CARD_SET=1403`。

代表性卡牌：

- `YOD_009`：神奇的雷诺（The Amazing Reno）
- `YOD_042`：莱登之拳（The Fist of Ra-den）
- `YOD_041`：风暴之眼（Eye of the Storm）
- `YOD_043`：鳞甲领主（Scalelord）
- `YOD_022`：冒进的艇长（Risky Skipper）
- `YOD_036`：腐巢幼龙（Rotnest Drake）

## 实现范围

本次只完成数据层升级：

- `SabberStoneCore/resources/Data/CardDefs.xml` 替换为 build `39954`
- 新增数据测试，确认 35 张 `YOD_*` 可收藏卡进入卡库
- 验证关键卡的费用/类型字段

暂不在本节点实现 35 张新卡的完整 `PowerTask`。这些卡后续应按卡牌语义逐张补齐，并配套机制/卡牌测试。

## 风险

新增实体会影响随机池、发现池、默认填充卡组。当前完整测试已经通过，但后续实现 `YOD_*` 卡牌效果时，仍应避免测试依赖随机池具体顺序。

## 验证

已通过：

```powershell
dotnet build SabberStone.sln -c Debug --no-restore
dotnet test SabberStoneCoreTest\SabberStoneCoreTest.csproj -c Debug --no-restore
```

测试结果：

- 1125 passed
- 1934 skipped
- 0 failed
