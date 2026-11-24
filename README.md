# Syntactic Wizardry — Vintage Story 0.02

Spatial Shape development milestone. Target: Vintage Story 1.22.x, .NET 10. Minecraft reference: Syntactic Wizardry v0.3.98. The project archive version is 0.02; modinfo uses semantic version 0.0.2.

## Build on Linux
Install the .NET 10 SDK and Vintage Story 1.22.x. Set `export VINTAGE_STORY=/absolute/path/to/vintagestory` (directory containing VintagestoryAPI.dll). Run `./build.sh`. Copy `dist/vs_syntactic_wizardry_0.02-mod.zip` to the game's Mods directory. Run core tests with `dotnet run --project tests/CoreTests.csproj`.

## Test workflow
In a creative test world, obtain Wizardry Wand from creative inventory. Hold it, press J, select a cell and add Target or Damage. The starter plan is Target (Entities) → Damage (Physical, Potence 1). Save to the wand, close the dialog, aim at a creature, right-click. Confirm health changes, Mana cost, cooldown, misses and wall occlusion. Restart and confirm the wand retains its spell. Test two players and PvP disabled. Try malformed/invalid plans and low Mana. No survival crafting recipe yet.

## Scope and departures
- 5x5 grammar, stable sparse properties, registered definitions, generic execution context and shape resolution. Target, Sphere, Box, Cone, Circle, Triangle, Floating and Damage. Modifier registration exists but execution is deferred. This first validator restricts one Shape per row and separates Shape/Effect rows; not full Minecraft grammar parity.
- Target range 64, entity/block/all modes, inward/outward; raycast adapter uses server-side Vintage Story selections. Entity impact currently uses entity position rather than exact intersection. Generic voxel overlap adapter is preliminary and intended for small sets.
- Damage Physical only, Potence 1–10; maps to blunt damage with native armor handling. Target costs 3, Damage costs 3 per Potence, wand multiplier .75, cooldown 1 second. Mana cap 50. Initial Mana 50 and passive regeneration .25/s are temporary testing defaults; Minecraft starts at zero and has progression-based regeneration. Casting experience, robe modifiers, damage kinds and research are deferred.
- One serialized Written Spell embedded in a wand, not yet a separate transferable Written Spell item or three-slot focus. Save/load/edit use the held wand; revision checks guard against stale edits on the same item but do not uniquely bind an editor session to a particular wand. Client feedback via chat; sound path and in-game behavior need verification. No particles yet. No hold/channel interruption needed for instant spells; death, active item and cooldown checked server-side.
- Player Mana stored in watched attributes; save/reconnect/death behavior requires engine testing. No explicit death persistence hook yet.
- No Luminal Bridge. No other phases implemented.

## Validation status
No .NET SDK or Vintage Story assemblies were available in the authoring container. API signatures were consulted against public documentation, but compilation and runtime verification are pending. Core tests supplied but not executed. The source should be treated as a first integration candidate, not a finished release.

## v0.02 changes
Sphere/hemisphere, Box and Cone port their v0.3.98 voxel rules and charge one Mana per canonical voxel. Circle (filled disk), Triangle (filled upright plane), and Floating (free point 1–32 blocks forward, radial force frame, cost 3) are provisional legacy components absent from the v0.3.98 registry. Floating does not stop at obstacles; area effects can reach through terrain. Root area Shapes originate one horizontal block forward of the eye. Box uses horizontal inherited facing unless a surface frame is available. Existing spells are compatible. One Shape per row remains. No new rendering or particles. Large Shapes may exceed the test Mana cap. In-game geometry and multiplayer behavior still require validation.
