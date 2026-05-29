# Upgrade plan: hsdata 42174 -> 43246

Scope: only 1v1 traditional Hearthstone simulation. Battlegrounds (`CardSet.BATTLEGROUNDS`, `BGS_*`, `TB_Bacon*`), Tavern Brawl (`CardSet.TB`, `FB_Champs_*`), and cosmetic hero skins are ignored.

## Structured diff summary

- Added entities: 19 total, 2 traditional ladder-relevant.
- Removed entities: 0.
- Changed fields: 54 total.
- Traditional ladder-relevant additions:
  - `BT_255` Kael'thas Sunstrider / 凯尔萨斯·逐日者.
  - `BT_255e` Sunstrider enchantment / 逐日者附魔.

## Traditional gameplay changes

- `BT_255` Kael'thas Sunstrider: implement "Every third spell you cast each turn costs (0)."
- `BT_255e`: set spell cost to 0, remove when the discounted spell is played or at turn end.
- `CFM_020` Raza the Chained: hero power cost effect changes from `(1)` to `(0)`.
- `DAL_433` Sludge Slurper: attack changes from 1 to 2.
- `LOOT_080`, `LOOT_080t2`, `LOOT_080t3`: cost changes from 6 to 5.
- `LOOT_539` Spiteful Summoner: cost changes from 7 to 6.
- `OG_211` Call of the Wild: cost changes from 9 to 8.
- `DRG_099t1` Decimation: remove `ImmuneToSpellpower`.
- `EX1_534` Savannah Highmane: `TECH_LEVEL` changes from 5 to 4; this is metadata only.

## Implementation steps

1. Replace `CardDefs.xml` with hsdata build `43246`.
2. Add `CardSet.BLACK_TEMPLE = 1414` and include it in Standard/Wild card pools.
3. Add `OutlandCardsGen` and register it in `CardDefs`.
4. Update Raza's existing enchant implementation to set hero power cost to 0.
5. Add focused tests for new data, filtered scope, Raza, and Kael'thas.
