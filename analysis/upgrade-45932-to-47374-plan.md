# Upgrade plan: hsdata 45932 -> 47374

Scope: traditional 1v1 Hearthstone simulation only. Battlegrounds, Tavern Brawl, and cosmetic entities are ignored.

## Data source

- Replace `SabberStoneCore/resources/Data/CardDefs.xml` with build `47374`.
- Filtered traditional entity count changes from 9059 to 9156.

## Added entities

- 105 relevant entities are added in the filtered data.
- Most additions are `BTA_*` Ashes of Outland adventure/challenge entities and new canonical core hero power IDs.
- No new ladder-collectible cards are added by this patch.

## Removed entities

- 22 old basic/upgraded hero power entities are removed.
- Old examples: `CS2_102`, `DS1h_292`, `CS1h_001`, `HERO_10p`, `AT_132_WARRIOR`.
- New examples: `HERO_01bp`, `HERO_05bp`, `HERO_09bp`, `HERO_10bp`, `HERO_01bp2`.

## Gameplay changes

- `BT_126` Teron Gorefiend text is now "Destroy all other friendly minions", so Teron must not destroy itself on Battlecry.
- Core hero powers now use the `HERO_*bp` ID family. Runtime mappings, Justicar Trueheart, and random hero power replacement need to use the new IDs.

## Implementation work

- Update the generated data resource to `47374`.
- Move core hero power implementations from removed old IDs to new `HERO_*bp` IDs.
- Add implementations for new upgraded `HERO_*bp2` hero powers.
- Update Demon Hunter hero power/enchantment IDs from `HERO_10p/HERO_10pe` to `HERO_10bp/HERO_10bpe`.
- Update tests for renamed hero power entities and pin the 47374 behavior with `OutlandDataPatch47374Test`.
