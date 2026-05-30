# Upgrade plan: hsdata 44582 -> 45310

Scope: traditional 1v1 simulation only. Battlegrounds, Tavern Brawl, and hero skins are ignored.

## Summary

- Source file: `analysis/CardDefs-45310.xml`
- Target file: `SabberStoneCore/resources/Data/CardDefs.xml`
- Added relevant entities: 0
- Removed relevant entities: 0
- Changed relevant fields: 4

## Traditional 1v1 changes

- `BT_601` Skull of Gul'dan: `COST` 5 -> 6.
- `BT_801` Eye Beam: Outcast text changed from "This costs (0)" to "This costs (1)".
- `BT_921` Aldrachi Warblades: `DURABILITY` 3 -> 2.
- `BT_934` Imprisoned Antaen: `COST` 5 -> 6.

## Implementation notes

- Apply `CardDefs-45310.xml` as a full embedded data-source replacement.
- No new card entities or new mechanics are introduced in this build.
- Update Eye Beam's implemented Outcast cost effect to reduce to 1 mana instead of 0.
- Add a data patch test for the four changed fields.
- Update the existing Eye Beam effect test to assert the new Outcast cost.

## Verification

- `dotnet test SabberStoneCoreTest/SabberStoneCoreTest.csproj --filter FullyQualifiedName~OutlandDataPatch45310Test --no-restore`
  - 1 passed.
- `dotnet test SabberStoneCoreTest/SabberStoneCoreTest.csproj --filter FullyQualifiedName~OutlandDemonHunterCardsGenTest --no-restore`
  - 45 passed.
- `dotnet test SabberStoneCoreTest/SabberStoneCoreTest.csproj --filter FullyQualifiedName~Outland --no-restore`
  - 197 passed.
- `dotnet test SabberStoneCoreTest/SabberStoneCoreTest.csproj --no-restore`
  - 1359 passed, 1934 skipped, 0 failed.
