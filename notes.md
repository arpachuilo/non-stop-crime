# TODO
- UI
  - In-round HUD 👉 Pretty good already, might need some scaling for 4K screens and general polish @ Floris
  - Tutorial 👉 Throw some text up in the lobby @ Floris
- Gameplay
  - Finalize mask abilities + VFX 👉 Maybe add one or two more masks as a stretch goal
  - Build final level 👉 Floris: I'm pretty pleased with the current state, might add some more greeblies
  - Communicate which zones relate to which masks 👉 Are we still doing this?
  - Spawnpoint asset (tiny house colored in player color)
- Sound
  - SFX for abilities
  - Background music
- NPC's
  - Reduce jittering 👉 don't allow instant direction changes
  - Add npc limit 👉 max in scene at any point in time
  - Add faces
- Balance
  - Gun is too powerful
    - Reduce fire rate
    - Do not allow projectiles to penetrate walls
  - Spawn invulnerability?
- User journey aka The Horizontal Vertical Slice
  - ✅ Launch game 
  - ✅ Go into lobby
  - ✅ 2-4 players join and ready 👉 Let's verify 3 and 4 player modes
  - ✅ Game starts if all ready
  - ✅ Players spawn in without a mask in their own corner
  - ✅ Players pick up a mask
  - After pickup, targets become apparent, may have to be spawned 👉 All players can go to all targets, do we want this?
  - Players complete objectives and frustrate others until time limit 👉 This works really well right now, only the gun is a little OP
  - Game ends, winner announced 👉 Just needs a better win screen
  - Game restarts or goes back to lobby 👉 Add buttons to the win screen
  
# Stretch goals
- More than 4 masks

# Graphics
- 2.5D sprite-based graphics
- Urban cityscape environment

# Gameplay
- Local multiplayer with 2-4 player support
- Each player controls a single character 
- Players acquire a mask that does two things:
  1. Grants a unique (combat) ability
  2. Allows access to a (pre-set) number of objectives
- The core gameplay loop consists of walking to objectives and completing them (presumably just by standing next to them) and frustrating the 
attempts of other players at achieving their objectives

## Victory conditions
- The game ends after a fixed time limit (e.g., 10 minutes)
- The player with the most points at the end of the time limit wins

## Mask Abilities
- All players start without a mask
- Masks spawn randomly on the map
- Each mask grants a unique ability
- (Possible) mask types:
  - ✅ Arson
  - ✅ Knife
  - ✅ Gun
  - ✅ Bomb
  - Beefy
  - Prank
  - Fast
  - Stealth
  - Trap
  - Theft

# Tutorializing
- Instructions on lobby screen

# UI
- TBD

# Playtests/feedback
- None
