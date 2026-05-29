# Outland 44222 implementation status

Scope: collectible cards introduced by hsdata `43246 -> 44222`, traditional 1v1 simulation only.

## Current status

- New collectible cards in scope: 172
- Implemented collectible ids from this diff in code: 84
- Effect-focused tests added so far: 85
- Remaining collectible cards: 88

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
- `BT_187` Kayn Sunfury
- `BT_271` Flamereaper
- `BT_423` Ashtongue Battlelord
- `BT_430` Warglaives of Azzinoth
- `BT_487` Hulking Overfiend
- `BT_510` Wrathspike Brute
- `BT_753` Mana Burn
- `BT_921` Aldrachi Warblades

### Warrior

- `BT_117` Bladestorm
- `BT_120` Warmaul Challenger
- `BT_123` Kargath Bladefist
- `BT_124` Corsair Cache
- `BT_138` Bloodboil Brute
- `BT_140` Bonechewer Raider
- `BT_233` Sword and Board
- `BT_249` Scrap Golem

### Druid

- `BT_128` Fungal Fortunes
- `BT_129` Germination
- `BT_130` Overgrowth
- `BT_132` Ironbark
- `BT_133` Marsh Hydra
- `BT_134` Bogbeam
- `BT_135` Glowfly Swarm
- `BT_136` Archspore Msshi'fn

### Hunter

- `BT_163` Nagrand Slam
- `BT_201` Augmented Porcupine
- `BT_202` Helboar
- `BT_205` Scrap Shot
- `BT_210` Zixor, Apex Predator / `BT_210t` Zixor Prime
- `BT_213` Scavenger's Ingenuity
- `BT_214` Beastmaster Leoroxx

### Mage

- `BT_002` Incanter's Flow
- `BT_014` Starscryer
- `BT_028` Astromancer Solarian
- `BT_072` Deep Freeze
- `BT_291` Apexis Blast

### Priest

- `EX1_193` Psychic Conjurer
- `EX1_194` Power Infusion
- `EX1_195` Kul Tiran Chaplain
- `EX1_196` Scarlet Subjugator
- `EX1_197` Shadow Word: Ruin
- `EX1_198` Natalie Seline

### Neutral

- `BT_008` Rustsworn Initiate
- `BT_010` Felfin Navigator
- `BT_714` Frozen Shadoweaver
- `BT_715` Bonechewer Brawler
- `BT_716` Bonechewer Vanguard
- `BT_720` Ruststeed Raider
- `BT_722` Guardian Augmerchant
- `BT_723` Rocket Augmerchant
- `BT_724` Ethereal Augmerchant
- `BT_727` Soulbound Ashtongue
- `BT_730` Overconfident Orc

## Verification

- Targeted Outland filter passed: 111 passed.
- Full test suite passed on second run: 1273 passed, 1934 skipped, 0 failed.
- Note: one first full run failed `GrimyGadgeteer_CFM_754`; the test passed when isolated and the immediate no-build full rerun passed. Treat as pre-existing order/random sensitivity unless it repeats.

## Remaining high-priority mechanics

- Dormant minions and delayed wake-up effects.
- Discover and choice flows for cards that require user choice.
- Secrets introduced in this patch.
- Hero power replacement with limited use counters.
- Global replacement/prevention effects such as `Bulwark of Azzinoth`.
- Remaining class sets: Paladin, Rogue, Shaman, Warlock, newer Priest cards, and remaining neutral cards.
