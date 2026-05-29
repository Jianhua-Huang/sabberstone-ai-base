# Outland 44222 implementation status

Scope: collectible cards introduced by hsdata `43246 -> 44222`, traditional 1v1 simulation only.

## Current status

- New collectible cards in scope: 172
- Implemented collectible ids from this diff in code: 150
- Effect-focused Outland tests added so far: 176
- Remaining collectible cards: 22

## Completed so far

### Demon Hunter

- `BT_035` Chaos Strike
- `BT_036` Coordinated Strike
- `BT_142` Shadowhoof Slayer
- `BT_173` Command the Illidari
- `BT_175` Twin Slice / `BT_175t` Second Slice
- `BT_187` Kayn Sunfury
- `BT_235` Chaos Nova
- `BT_271` Flamereaper
- `BT_321` Netherwalker
- `BT_351` Battlefiend
- `BT_352` Satyr Overseer
- `BT_354` Blade Dance
- `BT_355` Wrathscale Naga
- `BT_407` Ur'zul Horror
- `BT_416` Raging Felscreamer
- `BT_423` Ashtongue Battlelord
- `BT_427` Feast of Souls
- `BT_430` Warglaives of Azzinoth
- `BT_480` Crimson Sigil Runner
- `BT_486` Pit Commander
- `BT_487` Hulking Overfiend
- `BT_488` Soul Split
- `BT_490` Consume Magic
- `BT_491` Spectral Sight
- `BT_493` Priestess of Fury
- `BT_495` Glaivebound Adept
- `BT_496` Furious Felfin
- `BT_509` Fel Summoner
- `BT_510` Wrathspike Brute
- `BT_512` Inner Demon
- `BT_514` Immolation Aura
- `BT_601` Skull of Gul'dan
- `BT_740` Soul Cleave
- `BT_752` Blur
- `BT_753` Mana Burn
- `BT_761` Coilfang Warlord
- `BT_801` Eye Beam
- `BT_814` Illidari Felblade
- `BT_921` Aldrachi Warblades
- `BT_922` Umberwing
- `BT_937` Altruis the Outcast

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
- `BT_131` Ysiel Windsinger
- `BT_132` Ironbark
- `BT_133` Marsh Hydra
- `BT_134` Bogbeam
- `BT_135` Glowfly Swarm
- `BT_136` Archspore Msshi'fn

### Hunter

- `BT_163` Nagrand Slam
- `BT_201` Augmented Porcupine
- `BT_202` Helboar
- `BT_203` Pack Tactics
- `BT_205` Scrap Shot
- `BT_210` Zixor, Apex Predator / `BT_210t` Zixor Prime
- `BT_213` Scavenger's Ingenuity
- `BT_214` Beastmaster Leoroxx

### Mage

- `BT_002` Incanter's Flow
- `BT_003` Netherwind Portal
- `BT_014` Starscryer
- `BT_021` Font of Power
- `BT_022` Apexis Smuggler
- `BT_028` Astromancer Solarian
- `BT_072` Deep Freeze
- `BT_291` Apexis Blast

### Paladin

- `BT_011` Libram of Justice
- `BT_018` Underlight Angling Rod
- `BT_019` Murgur Murgurgle / `BT_019t` Murgurgle Prime
- `BT_020` Aldor Attendant
- `BT_024` Libram of Hope
- `BT_025` Libram of Wisdom
- `BT_026` Aldor Truthseeker
- `BT_292` Hand of A'dal

### Priest

- `BT_197` Reliquary of Souls / `BT_197t` Reliquary Prime
- `BT_198` Soul Mirror
- `BT_252` Renew
- `BT_253` Psyche Split
- `BT_254` Sethekk Veilweaver
- `BT_256` Dragonmaw Overseer
- `BT_257` Apotheosis
- `BT_262` Dragonmaw Sentinel
- `BT_341` Skeletal Dragon
- `EX1_193` Psychic Conjurer
- `EX1_194` Power Infusion
- `EX1_195` Kul Tiran Chaplain
- `EX1_196` Scarlet Subjugator
- `EX1_197` Shadow Word: Ruin
- `EX1_198` Natalie Seline

### Rogue

- `BT_042` Bamboozle
- `BT_188` Shadowjeweler Hanar
- `BT_707` Ambush
- `BT_709` Dirty Tricks
- `BT_711` Blackjack Stunner
- `BT_701` Spymistress
- `BT_702` Ashtongue Slayer
- `BT_703` Cursed Vagrant
- `BT_710` Greyheart Sage
- `BT_713` Akama / `BT_713t` Akama Prime

### Shaman

- `BT_100` Serpentshrine Portal
- `BT_101` Vivid Spores
- `BT_102` Boggspine Knuckles
- `BT_106` Bogstrok Clacker
- `BT_109` Lady Vashj
- `BT_110` Torrent
- `BT_113` Totemic Reflection
- `BT_114` Shattered Rumbler
- `BT_115` Marshspawn
- `BT_230` The Lurker Below

### Warlock

- `BT_196` Keli'dan the Breaker
- `BT_199` Unstable Felbolt
- `BT_300` Hand of Gul'dan
- `BT_301` Nightshade Matron
- `BT_302` Dark Portal
- `BT_304` Enhanced Dreadlord
- `BT_306` Shadow Council
- `BT_307` Darkglare
- `BT_309` Kanrethad Ebonlocke

### Neutral

- `BT_008` Rustsworn Initiate
- `BT_010` Felfin Navigator
- `BT_155` Scrapyard Colossus
- `BT_159` Terrorguard Escapee
- `BT_160` Rustsworn Cultist
- `BT_190` Replicat-o-tron
- `BT_714` Frozen Shadoweaver
- `BT_715` Bonechewer Brawler
- `BT_716` Bonechewer Vanguard
- `BT_717` Burrowing Scorpid
- `BT_720` Ruststeed Raider
- `BT_721` Blistering Rot
- `BT_722` Guardian Augmerchant
- `BT_723` Rocket Augmerchant
- `BT_724` Ethereal Augmerchant
- `BT_726` Dragonmaw Sky Stalker
- `BT_727` Soulbound Ashtongue
- `BT_728` Disguised Wanderer
- `BT_729` Waste Warden
- `BT_730` Overconfident Orc
- `BT_731` Infectious Sporeling
- `BT_732` Scavenging Shivarra
- `BT_733` Mo'arg Artificer
- `BT_734` Supreme Abyssal

## Verification

- Targeted first batch passed: 100 passed.
- Targeted second batch passed: 35 passed.
- Outland filter passed after this pass: 176 passed, 0 failed.
- Full suite no-build run currently has a pre-existing order/random-sensitive failure outside Outland:
  `VentureCoMercenary_CS2_227` failed in the full run but passed immediately when isolated.

## Remaining high-priority mechanics

- Dormant minions and delayed wake-up effects.
- Strict previous-turn spell tracking; current implementation supports these cards but still needs to be tightened from "recent/game spell" compatibility to exact previous-turn semantics.
- Discover and choice flows for cards that require custom pools.
- Hero and hero power replacement with limited-use counters.
- Global replacement/prevention effects such as `Bulwark of Azzinoth`.
- Remaining complex 44222 cards: 22 collectible ids.
