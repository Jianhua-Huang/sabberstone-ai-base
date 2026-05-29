# Upgrade plan: hsdata 44222 -> 44582

Scope: traditional 1v1 Hearthstone only. Battlegrounds, Tavern Brawl, and cosmetic-only entities are out of scope.

## Data summary

- Source file: `analysis/CardDefs-44582.xml`
- Target file: `SabberStoneCore/resources/Data/CardDefs.xml`
- Relevant added entities: 0
- Relevant removed entities: 0
- Relevant changed fields: 21

## Implementation notes

- Apply the 44582 XML as a full data-source replacement.
- No new ladder card entities are introduced in this build.
- No new combat mechanics are introduced in this build.
- The main changes are:
  - `AT_037` Living Roots rarity changed from epic to common.
  - `EX1_193` Psychic Conjurer rarity changed from common to free.
  - `EX1_194` Power Infusion rarity changed from common to free.
  - `LOOT_526e` Darkness Awaits is no longer collectible.
  - 17 Galakrond-related Descent of Dragons cards gain tag `676=1`; the current enum table does not name this tag, so tests reference it as `(GameTag)676`.

## Tests

- Add `OutlandDataPatch44582Test` to assert the key data changes.
- Run the targeted 44582 test.
- Run the full `SabberStoneCoreTest` suite.
