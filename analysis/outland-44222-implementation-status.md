# Outland 44222 implementation status

Scope: collectible cards introduced by hsdata `43246 -> 44222`, traditional 1v1 simulation only.

## Current status

- New collectible cards in scope: 172
- Implemented card ids in code: 40
- Effect-focused tests added so far: 38
- Remaining collectible cards: 132

## Completed in this pass

### Demon Hunter

- `BT_035` Chaos Strike
- `BT_036` Coordinated Strike
- `BT_142` Shadowhoof Slayer
- `BT_173` Command the Illidari
- `BT_175` Twin Slice / `BT_175t` Second Slice
- `BT_235` Chaos Nova
- `BT_351` Battlefiend
- `BT_352` Satyr Overseer
- `BT_354` Blade Dance
- `BT_355` Wrathscale Naga
- `BT_407` Ur'zul Horror
- `BT_427` Feast of Souls
- `BT_480` Crimson Sigil Runner
- `BT_486` Pit Commander
- `BT_488` Soul Split
- `BT_490` Consume Magic
- `BT_491` Spectral Sight
- `BT_493` Priestess of Fury
- `BT_495` Glaivebound Adept
- `BT_496` Furious Felfin
- `BT_509` Fel Summoner
- `BT_512` Inner Demon
- `BT_514` Immolation Aura
- `BT_601` Skull of Gul'dan
- `BT_740` Soul Cleave
- `BT_752` Blur
- `BT_761` Coilfang Warlord
- `BT_801` Eye Beam
- `BT_814` Illidari Felblade
- `BT_922` Umberwing
- `BT_937` Altruis the Outcast

### Priest

- `EX1_193` Psychic Conjurer
- `EX1_194` Power Infusion
- `EX1_195` Kul Tiran Chaplain
- `EX1_196` Scarlet Subjugator
- `EX1_197` Shadow Word: Ruin
- `EX1_198` Natalie Seline

## Verification

- Targeted tests: `OutlandDemonHunterCardsGenTest` passed: 32 passed.
- Clone consistency: `CloneSameSame` passed.
- Full test suite passed: 1226 passed, 1934 skipped, 0 failed.

## Remaining high-priority mechanics

- Remaining Outcast cards not yet implemented outside the seven covered here.
- Dormant minions and delayed wake-up effects.
- Prime deathrattle shuffle and Prime token behavior.
- Discover and choice flows for cards that require user choice.
- Hero power replacement with limited use counters.
- Attack rule extensions such as ignoring Taunt and repeat attacks.
