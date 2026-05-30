# Upgrade 48313 to 49534

## Scope

- Target build: 49534 / Hearthstone 17.4.0.
- Previous applied build: 48313 / Hearthstone 17.2.1.
- Build 48705 has no effective card data changes, so this upgrade applies the next major data change directly.
- No new collectible ladder cards are added in this step.
- Newly relevant 1v1 entities are the Trial by Felfire adventure/challenge `BTA_*` cards, hero powers, tokens, and enchantments.

## Data Changes

- Replaced `SabberStoneCore/resources/Data/CardDefs.xml` with `analysis/CardDefs-49534.xml`.
- Added 57 new Trial by Felfire entities from the 48705 to 49534 diff.
- Ignored Battlegrounds-only and Tavern Brawl-only implications for simulator behavior, per project scope.

## Implementation Plan

- Register Trial by Felfire powers from `OutlandCardsGen.AddAll`.
- Implement player-side `BTA_*` combat effects:
  - deck discover hero power;
  - conditional Demon Hunter attack hero power;
  - Outcast battlecries and summon effects;
  - Rusted Legion hand corruption, cost reduction, periodic damage/heal, draw and summon effects.
- Implement representative boss-side combat effects:
  - active hero powers such as destroy/deathrattle trigger, damage/heal, summon, bounce, and draw;
  - passive effects that can be represented by current triggers and auras;
  - enchantments with explicit effects where localized text cannot be parsed safely.
- Keep adventure-only progress counters, such as escape-turn bookkeeping, as data-loaded entities unless they affect normal 1v1 board resolution.

## Tests

- Add `OutlandDataPatch49534Test`.
- Assert every newly added `BTA_*` entity loads.
- Add effect tests for each implemented gameplay family rather than only card existence:
  - discover and hero attack modification;
  - Outcast minion effects;
  - corruption enchantments;
  - cost reduction, damage, summon, draw, deathrattle, and start-turn effects;
  - boss hero powers and representative passives/spells.
