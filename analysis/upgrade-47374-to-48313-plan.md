# Upgrade plan: hsdata 47374 -> 48313

Scope: traditional 1v1 card data only. Battlegrounds, Tavern Brawl, and hero skins remain out of scope.

## Diff summary

- Entity count is unchanged: 9156 -> 9156.
- No relevant traditional entities are added or removed.
- The patch contains 18 relevant balance-field changes.

## Data changes

- Replace `SabberStoneCore/resources/Data/CardDefs.xml` with `analysis/CardDefs-48313.xml`.
- Verify changed base stats and costs:
  - `BT_020` Aldor Attendant: 2/2/2 -> 1/1/2.
  - `BT_110` Torrent: 5 -> 4 cost.
  - `BT_114` Shattered Rumbler: 4 -> 5 attack.
  - `BT_138` Bloodboil Brute: 6 -> 5 attack.
  - `BT_188` Shadowjeweler Hanar: 5 -> 4 health.
  - `BT_230` The Lurker Below: 3 -> 5 health.
  - `BT_480` Crimson Sigil Runner: 2 -> 1 attack.
  - `BT_493` Priestess of Fury: 7 -> 5 health.
  - `ULD_720` Bloodsworn Mercenary: 3/3 -> 2/2.

## Effect changes

- `BT_213` Scavenger's Ingenuity and `BT_213e`: hand Beast buff changes from +3/+3 to +2/+2.
- `BT_305` Imprisoned Scrap Imp and `BT_305e`: hand minion buff changes from +2/+2 to +2/+1.
- `BT_711` Blackjack Stunner and `BT_711e`: returned minion cost penalty changes from +2 to +1.

## Tests

- Add a 48313 data-patch test covering changed stats, costs, and text.
- Update existing behavior tests for Scavenger's Ingenuity, Imprisoned Scrap Imp, Blackjack Stunner, and Torrent.
- Run targeted Outland/Uldum tests, then full test suite before committing.
