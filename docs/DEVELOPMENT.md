# Developing the prototype

Use Unity 6000.3.24f1. Stop Play mode before changing saved scene values; many Inspector edits made during Play are discarded when it stops.

## Where to edit SHIFT

- **01 ENVIRONMENT:** platforms and walls; change positions to explore routes.
- **02 MECHANISMS:** stations, panels, pistons, gates, bridge and exit. Piston `Gates` and `Bridges` lists define connections.
- **03 ROBOT:** movement and light/heavy jump settings on `ShiftRobot`; child objects hold the appearance.
- **04 BACKDROP:** non-colliding decoration.
- **05 ROOM RULES:** `ShiftRoom` references, exit, state flow and next scene.

The reusable components live in `Assets/Scripts/ShiftGame`. Materials and the starting robot prefab are in `Assets/ShiftPrototype`. Earlier magnet components remain in `Assets/Scripts/MagneticPrototype`.

## Preserve your own iterations

Duplicate a scene under a new name before trying a layout. Update its room's next-scene value and Build Profiles scene list when adding a playable level. Keep the original 05/06 scene names if you want to run the included tests unchanged: they deliberately target those baseline scenes.

**Portfolio → SHIFT → Generate two robot levels overwrites the baseline 05/06 scenes.** It is a reconstruction tool, not the normal way to save an edited level. Save customized scenes under different names before using it. **Build Windows game** builds the saved 05/06 scenes without regenerating them.

The Polar Relay generator similarly rebuilds its own 03/04 baseline. Existing 01/02 movement exercises are retained.

## A useful first iteration

Choose a single sign, platform or return-route cue in the second level. Save a baseline screenshot and scene copy. Observe a new player before explaining the solution. Change that one item, then record whether a later test supports keeping it. Record actual evidence and the assistance used; do not claim an untested layout is clearer.
