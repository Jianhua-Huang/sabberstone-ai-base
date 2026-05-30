# Upgrade plan: hsdata 45310 -> 45932

Scope: traditional 1v1 Hearthstone simulation only. Battlegrounds, Tavern Brawl, and cosmetic entities are ignored.

## Data source

- Replace `SabberStoneCore/resources/Data/CardDefs.xml` with build `45932`.
- Entity count changes from 9057 to 9059 in the filtered traditional scope.

## Added entities

- `CS1_130_Puzzle` - Holy Smite, non-collectible puzzle spell.
- `CS2_004_Puzzle` - Power Word: Shield, non-collectible puzzle spell.

These are puzzle-only entities and do not add new ladder-legal collectible cards.

## Balance and gameplay changes

- `BT_011` Libram of Justice: cost 6 -> 5.
- `BT_230` The Lurker Below: gains Beast race.
- `BT_255` Kael'thas Sunstrider: cost 6 -> 7.
- `BT_351` Battlefiend: attack 2 -> 1.
- `BT_495` Glaivebound Adept: attack 7 -> 6.
- `BT_937` Altruis the Outcast: cost 3 -> 4, attack 3 -> 4.
- `DRG_071` Bad Luck Albatross: cost 3 -> 4.
- `NEW1_003` Sacrificial Pact: target changes from any Demon to friendly Demon only.
- `UNG_028` Open the Waygate: quest progress 6 -> 8.
- `UNG_832` Bloodbloom: cost 2 -> 4.
- `YOD_032` Frenzied Felwing: health 3 -> 2.

## Implementation work

- Update the generated data resource to `45932`.
- Add `REQ_FRIENDLY_TARGET` to `Sacrificial Pact`, because this text change affects legal combat targets.
- Update behavior tests for `Sacrificial Pact`, `Battlefiend`, and `Open the Waygate`.
- Add a focused `OutlandDataPatch45932Test` to pin all 45932 traditional balance fields and the two puzzle-only entities.
