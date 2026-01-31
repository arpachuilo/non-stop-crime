# TODO
- UI
  - In-round HUD
  - Tutorial
  - Lobby
  - Main menu art 👉 Ready to go, need to be implemented
  - Win screen
- Gameplay
  - Finalize mask abilities + VFX 👉 Maybe add one or two more masks as a stretch goal
  - Build final level 👉 Floris: I'm pretty pleased with the current state, might add some more greeblies
  - Communicate which zones relate to which masks 👉 Are we still doing this?
  - Spawnpoint asset (tiny house colored in player color)
  - Capture zones should maybe not be instant to capture
  - Owning capture zones should give points over time
  - Capture zones might have uncappable cooldown after being captured
- Sound
  - SFX for abilities
  - Background music
- Graphics/design
  - Replace wall assets with fences 👉 Nice to have tbh
  - Masks are hard to see 👉 Add cylinder with capture zone shader?
  - Player characters are hard to see 👉 Dont shade them, or add light source to them
  - NPC's are not shaded 👉 Make their sprite shaded
- NPC's
  - Reduce jittering
  - Reduce spawnrate
  - Add npc limit (max in scene at any point in time)
- Bugs
  - Projectiles entering a zone captures it for the player who shot it
- Balance
  - Gun is too powerful
    - Reduce fire rate
    - Do not allow projectiles to penetrate walls
  - Timer is too long
- User journey
  - ✅ Launch game 
  - ✅ Go into lobby
  - ✅ 2-4 players join and ready
  - ✅ Game starts if all ready
  - ✅ Players spawn in without a mask in their own corner
  - ✅ Players pick up a mask
  - After pickup, targets become apparent, may have to be spawned
  - Players complete objectives and frustrate others until time limit
  - Game ends, winner announced
  - Game restarts or goes back to lobby

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
