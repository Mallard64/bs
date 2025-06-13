# 🏛️ Lobby System Setup Guide

## Step 1: Update Your Player Prefab

### 1.1 Add LobbyPlayerAdapter Component
1. Open your existing player prefab (the one with MouseShooting and PlayerMovement)
2. In the Inspector, click "Add Component"
3. Search for "LobbyPlayerAdapter" and add it
4. Configure the settings:
   - **Is In Lobby**: Check this for lobby mode
   - **Lobby Move Speed**: 3 (slower than combat)
   - **Lobby Run Speed Multiplier**: 1.5
   - **Interaction Range**: 2
   - **Interactable Layer Mask**: Default (or create a "Interactable" layer)

### 1.2 Setup Lobby UI (Optional)
1. Create a Canvas as child of your player prefab
2. Add UI elements:
   - **Interaction Prompt**: TextMeshPro for "Press E to..." messages
   - **Player Name Text**: TextMeshPro to show player name above character
3. Assign these UI elements to the LobbyPlayerAdapter component

## Step 2: Create the Lobby Scene

### 2.1 Create New Scene
1. File → New Scene
2. Save as "Lobby.unity"
3. Add basic environment (ground, walls, decorations)

### 2.2 Setup LobbyManager
1. Create empty GameObject named "LobbyManager"
2. Add LobbyManager component
3. Configure spawn points:
   - Create empty GameObjects positioned around the lobby
   - Assign them to the "Player Spawn Points" array

### 2.3 Setup Leaderboard
1. Create Canvas for UI
2. Create Panel for leaderboard (initially disabled)
3. Inside panel, create ScrollView with:
   - Content area for leaderboard entries
4. Create LeaderboardEntry prefab:
   - Panel with TextMeshPro components for rank, name, stats
5. Assign to LobbyManager's leaderboard fields

## Step 3: Add Portal Objects

### 3.1 Create Boss Portal
1. Create GameObject named "BossPortal"
2. Add Portal component
3. Configure:
   - **Target Scene**: "Boss"
   - **Portal Name**: "Boss Arena"
   - **Min Players Required**: 1
   - **Max Players Allowed**: 4
4. Add visual elements:
   - SpriteRenderer with portal sprite
   - ParticleSystem for portal effects
   - Collider2D (IsTrigger = true) for detection
   - AudioSource for sound effects

### 3.2 Create Knockout Portal
1. Duplicate Boss Portal
2. Rename to "KnockoutPortal"
3. Configure:
   - **Target Scene**: "Knockout"
   - **Portal Name**: "PvP Arena"
4. Position in different location

### 3.3 Portal UI Setup
1. Create Canvas above each portal
2. Add TextMeshPro for portal info
3. Add UI elements for player count
4. Assign to Portal's UI fields

## Step 4: Add Interactive Features

### 4.1 Weapon Display
1. Create GameObject named "WeaponDisplay"
2. Add WeaponDisplay component
3. Configure:
   - **Weapon Prefabs**: Assign your weapon prefabs array
   - **Display Point**: Transform where weapon appears
   - **Rotation Speed**: 45
   - **Switch Interval**: 5 seconds
4. Add visual effects:
   - ParticleSystem for display effects
   - Light for illumination
   - AudioSource for switch sounds
5. Create UI Canvas with weapon info display

### 4.2 Training Dummy
1. Create GameObject named "TrainingDummy"
2. Add TrainingDummy component
3. Configure:
   - **Max Health**: 1000
   - **Reset Time**: 5 seconds
   - **Show Damage Numbers**: true
   - **Track DPS**: true
4. Add components:
   - SpriteRenderer with dummy sprite
   - Collider2D for hit detection
   - Animator for hit animations
   - ParticleSystem for hit effects
   - AudioSource for hit sounds
5. Create damage text prefab (TextMeshPro that fades out)
6. Create UI Canvas for health/DPS display

### 4.3 Notification System
1. Create GameObject named "NotificationSystem"
2. Add NotificationSystem component
3. Create notification prefab:
   - Panel with background
   - TextMeshPro for message
   - Image for notification icon
   - Add NotificationUI component
4. Setup notification parent (UI Canvas area)

## Step 5: Configure Layers and Physics

### 5.1 Create Layers
1. Edit → Project Settings → Tags and Layers
2. Create layers:
   - "Interactable" (for portals, weapon display, etc.)
   - "UI" (for UI elements)
   - "Environment" (for lobby decorations)

### 5.2 Setup Collision Matrix
1. Edit → Project Settings → Physics2D
2. Configure layer interactions:
   - Players should collide with Environment
   - Players should trigger Interactable
   - Bullets should not collide in lobby

## Step 6: Scene Integration

### 6.1 Network Manager Setup
1. Find your NetworkManager
2. In "Offline Scene" set to your main menu
3. In "Online Scene" set to "Lobby"
4. Make sure your player prefab is registered

### 6.2 Scene Loading Setup
1. Ensure Boss.unity and Knockout.unity scenes exist
2. Add them to Build Settings (File → Build Settings)
3. Note their build indices for scene loading

## Step 7: Testing

### 7.1 Lobby Testing
1. Start play mode
2. Test movement (WASD, Shift to run)
3. Test interactions (E key on portals, weapon display)
4. Test leaderboard (Tab key)
5. Test portal teleportation

### 7.2 Multiplayer Testing
1. Build and run multiple instances
2. Test lobby spawning with multiple players
3. Test portal requirements (min/max players)
4. Test leaderboard synchronization

## Common Issues & Solutions

### Issue: Player falls through floor
**Solution**: Ensure lobby ground has Collider2D

### Issue: Interactions don't work
**Solution**: 
- Check Interactable layer mask on LobbyPlayerAdapter
- Ensure interactable objects have correct layer
- Verify Collider2D has IsTrigger = true

### Issue: Portal doesn't teleport
**Solution**:
- Check target scene name matches exactly
- Ensure scenes are in Build Settings
- Check min/max player requirements

### Issue: UI doesn't show
**Solution**:
- Check Canvas render mode (Screen Space - Overlay)
- Verify UI assignments on components
- Check Canvas sorting order

### Issue: Network sync problems
**Solution**:
- Ensure all lobby objects have NetworkIdentity if needed
- Check SyncVar updates
- Verify ClientRpc calls

## Example Lobby Layout

```
Lobby Scene Structure:
├── Environment/
│   ├── Ground (Collider2D)
│   ├── Walls (Collider2D)
│   └── Decorations
├── LobbyManager
├── Spawn Points/
│   ├── SpawnPoint1
│   ├── SpawnPoint2
│   └── SpawnPoint3
├── Portals/
│   ├── BossPortal (Portal component, UI Canvas)
│   └── KnockoutPortal (Portal component, UI Canvas)
├── Features/
│   ├── WeaponDisplay (WeaponDisplay component, UI Canvas)
│   └── TrainingDummy (TrainingDummy component, UI Canvas)
├── UI/
│   ├── NotificationSystem
│   └── LeaderboardCanvas
└── Audio/
    ├── MusicPlayer
    └── AmbientSounds
```

This setup will give you a fully functional lobby system that works with your existing player prefab!