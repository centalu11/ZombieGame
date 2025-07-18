# Quick Cooperative Multiplayer Setup

## What We Built
- **NetworkPlayerController** - Handles networked players
- **NetworkZombieAI** - Zombies that detect multiple players  
- **SimpleNetworkManager** - Basic connection UI for testing

## Setup Steps

### 1. Setup Player Prefab
Take your existing player prefab (e.g., `PlayerArmature.prefab`):

1. **Add Network Components:**
   - Add `NetworkObject` component
   - Add `NetworkTransform` component
   - Add `NetworkPlayerController` script

2. **Configure NetworkObject:**
   - Check "Allow Scene Objects" if placing in scene
   - Set "Scene Id" if needed

### 2. Setup Zombie Prefab
Take your existing zombie prefab:

1. **Add Network Components:**
   - Add `NetworkObject` component 
   - Add `NetworkTransform` component
   - Add `NetworkZombieAI` script

2. **Configure NetworkZombieAI:**
   - Set detection range (e.g., 10)
   - Set attack range (e.g., 2)
   - Assign animation clips from your zombie animations

### 3. Setup Main Scene
In your main gameplay scene:

1. **Add Connection Manager:**
   - Create empty GameObject, name it "NetworkConnectionManager"
   - Add `SimpleNetworkManager` script

2. **Place Players and Zombies:**
   - Drag networked player prefabs into scene
   - Drag networked zombie prefabs into scene
   - Make sure NetworkScene is loaded (your SceneLoader should handle this)

### 4. Testing

**Single Machine Test:**
1. Build the project 
2. Run one instance → Click "Start Host"
3. Run another instance → Click "Connect as Client"
4. Both players should appear, zombies should detect both

**Movement Test:**
- Each player controls their own character
- Other players appear as different colored characters
- Zombies chase the closest player
- Input only works for your own player

## Debug Info
- Players show their ID in top-right corner
- Zombies show state and player count above them
- Connection status in top-left corner

## What Should Happen
✅ Player 1 can move around with WASD  
✅ Player 2 appears and moves when controlled by second client  
✅ Zombies detect and chase both players  
✅ Zombies switch targets to closest player  
✅ Players are different colors  

## Quick Fixes
- **No player spawning:** Make sure NetworkObject is on player prefab
- **No zombie detection:** Check zombie has NetworkZombieAI script
- **Input not working:** Only the owner should have input enabled
- **No connection:** Make sure NetworkScene contains NetworkManager

That's it! Your cooperative zombie game should work now. 