# Upgrade plan: hsdata 43246 -> 44222

Scope: only 1v1 traditional Hearthstone simulation. Battlegrounds, Tavern Brawl, hero skins, and solo/prologue-only entities are not implementation targets.

## Structured diff summary

- Added traditional-relevant entities before solo/prologue pruning: 309.
- Added collectible cards: 172.
- Removed entities: 0.
- Changed relevant fields: 139.
- Main content: Ashes of Outland plus Demon Hunter Initiate.

## Required engine/data work

- Add `CardClass.DEMONHUNTER = 14`.
- Add `CardSet.DEMON_HUNTER_INITIATE = 1463`.
- Include Demon Hunter in Standard/Wild class card pools.
- Map `CardClass.DEMONHUNTER` to `HERO_10`.
- Implement base Demon Hunter hero power `HERO_10p` / Demon Claws.
- Update `CardDefs.xml` to build `44222`.

## Immediate gameplay changes covered in this pass

- `CS1_112` Holy Nova: damages enemy minions only, not enemy hero; cost is data-driven to 4.
- `CS1_130` Holy Smite: deals 3 damage and requires a minion target.
- `CS2_004` Power Word: Shield: no longer draws a card; cost is data-driven to 0.

## Follow-up implementation backlog

- Implement Outcast.
- Implement Dormant / awakening for Ashes of Outland minions.
- Implement Prime deathrattle shuffle chains.
- Implement Demon Hunter class cards from Initiate and Ashes of Outland.
- Implement remaining Ashes of Outland class/neutral card powers and focused tests.
