# Dungeon System Documentation

## Overview
This comprehensive dungeon system provides a complete procedural dungeon generation solution for Unity with room-based gameplay, camera management, minimap with fog-of-war, and floor progression.

## System Components

### Core Room System
- **Room.cs**: Base room entity with 14×10 interior walkable area, wall boundaries, exit management, and enemy tracking
- **ExitDoor.cs**: Door component that blocks/unblocks player movement through room exits
- **BossRoom.cs**: Special room that spawns teleport portal when cleared
- **ItemRoom.cs**: Room that spawns collectible items when cleared or entered
- **ElevatorRoom.cs**: Special room connected to boss room that teleports player to new level location
- **TeleportPortal.cs**: Portal that teleports player to the next floor

### Dungeon Generation
- **DungeonGenerator.cs**: Generates multi-room floors with proper spacing and exit alignment
- **FloorManager.cs**: Manages dungeon progression across multiple floors and player positioning

### Camera System
- **CameraRoomFollow.cs**: Camera system that locks to current room and pans smoothly between rooms

### Minimap System
- **MinimapManager.cs**: Generates and manages minimap grid with fog-of-war
- **MinimapRoom.cs**: Individual minimap tile with visibility states (hidden/discovered/revealed)
- **RoomMinimapLink.cs**: Links world rooms to minimap tiles and handles fog-of-war updates

## Room Layout Specifications

### Tile Layout
- **Interior Size**: 14 × 10 tiles (walkable area)
- **Total Size**: 16 × 12 tiles (includes 1-tile walls on all sides)
- **Tile Encoding**: 0 = walkable floor, 1 = wall
- **Exits**: Carved through walls only (North, South, East, West)

### Tilemap Coordinate System
- **Room Bounds**: (0,0) to (15,11) in tilemap coordinates
- **Interior Area**: (1,1) to (14,10) - walkable tiles
- **Wall Tiles**: Perimeter at x=0, x=15, y=0, y=11
- **Exit Positions**: 
  - North: (7,11) - removes wall tile for passage
  - South: (7,0) - removes wall tile for passage  
  - East: (15,5) - removes wall tile for passage
  - West: (0,5) - removes wall tile for passage

### Room Types
- **Start Room**: Bottom row placement, only North exit
- **Fighting Rooms**: 6 per floor, random placement with enemies
- **Item Room**: 1 per floor, spawns collectible items
- **Boss Room**: 1 per floor, spawns teleport portal when cleared
- **Elevator Room**: 1 per floor, connected to boss room, teleports to new level at origin

## Setup Instructions

### 1. Scene Setup
1. **Create Dungeon System**:
   - Create empty GameObject named "DungeonSystem"
   - Add the following components:
     - `FloorManager`
     - `DungeonGenerator`
     - `MinimapManager`

*Note: Tilemap system is now automatically created by Room.cs - no manual setup required.*

### 2. Camera Setup
1. Add `CameraRoomFollow` component to your main camera
2. Configure pan speed and room offset for UI

### 3. Room Prefabs
Create prefabs for each room type following these detailed instructions:

#### ⚠️ IMPORTANT: Prefab Creation Process
**Before creating any room prefab, follow these steps to ensure Grid settings are saved:**

1. **Create Empty GameObject**: Name it for your room type (e.g., "StartRoom")
2. **Add Room Component**: Attach `Room.cs` script
3. **Setup Grid for Prefab**: Right-click Room component → **"Setup Complete Prefab"**
   - This creates Grid component with proper 0.4×0.4 cell size
   - Creates Wall_Tilemap and Floor_Tilemap child objects
   - Initializes all necessary components
4. **Configure Room Settings**: Set interior size, exits, tile assets
5. **Save as Prefab**: Grid settings will now be properly saved

#### Common Prefab Issues & Solutions:
- **Grid settings not saved**: Use "Setup Complete Prefab" before creating prefab
- **Missing tilemaps**: "Setup Complete Prefab" creates both Wall_Tilemap and Floor_Tilemap
- **Wrong cell size**: Grid is automatically set to 0.4×0.4×0 for dungeon alignment

#### 3.1 Start Room Prefab
1. **Create Empty GameObject**: Name it "StartRoom"
2. **Add Room Component**: Attach `Room.cs` script
3. **Setup Prefab Structure**: Right-click Room → **"Setup Complete Prefab"**
4. **Configure Room Settings**:
   - Interior Size: (14, 10)
   - Grid Position: Will be set by generator
5. **Assign Tile Assets**:
   - Floor Tile: Assign floor tile asset (walkable areas)
   - Wall Tile: Assign wall tile asset (barriers)
   - Door Tile: Assign door tile asset (tile block 2 for locked exits)
6. **Add RoomMinimapLink**: Set Room Type to "Start"
7. **Save as Prefab**

*Note: Room.cs now automatically creates Grid, Tilemap, TilemapRenderer, and TilemapCollider2D components and generates tiles based on room layout.*

#### 3.2 Fight Room Prefab
1. **Create Empty GameObject**: Name it "FightRoom"
2. **Add Room Component**: Attach `Room.cs` script
3. **Configure Room Settings**: Same as Start Room
4. **Add BoxCollider2D**: Same trigger setup as Start Room
5. **Assign Tile Assets**: Same as Start Room
   - Floor Tile, Wall Tile, Door Tile (tile block 2)
6. **Add Enemy Spawn Points**:
   - Create child GameObjects as spawn points
   - Place enemy prefabs as children (they'll be found automatically)
   - Ensure enemies have `Enemy.cs` with OnDeath event
7. **Add Visual Elements**: Floor, walls, combat decorations
8. **Add RoomMinimapLink**: Set Room Type to "Fight"
9. **Save as Prefab**

#### 3.3 Item Room Prefab
1. **Create Empty GameObject**: Name it "ItemRoom"
2. **Add ItemRoom Component**: Attach `ItemRoom.cs` script (inherits from Room)
3. **Configure ItemRoom Settings**:
   - Interior Size: (14, 10)
   - Spawn On Room Clear: ✓ (recommended)
   - Spawn On Entry: ☐ (optional)
4. **Add BoxCollider2D**: Same trigger setup
5. **Create Exit Doors**: Same as Fight Room (all four directions)
5. **Assign Tile Assets**: Same as other rooms
6. **Configure Item System**:
   - **Item Spawn Point**: Create child GameObject "ItemSpawn"
   - **Item Prefabs Array**: Assign item prefabs in inspector
   - Items should have `ItemCollectable.cs` component
7. **Optional Enemy Setup**: Add enemies if room should be cleared first
8. **Add Visual Elements**: Floor, walls, treasure room decorations
9. **Add RoomMinimapLink**: Set Room Type to "Item"
10. **Save as Prefab**

#### 3.4 Boss Room Prefab
1. **Create Empty GameObject**: Name it "BossRoom"
2. **Add BossRoom Component**: Attach `BossRoom.cs` script
3. **Configure BossRoom Settings**: Same room setup as others
4. **Add BoxCollider2D**: Same trigger setup
5. **Assign Tile Assets**: Same as other rooms
6. **Configure Teleport Portal System**:
   - **Portal Spawn Point**: Create child GameObject "PortalSpawn"
   - **Teleport Portal Prefab**: Assign in inspector (see Portal Prefab section)
7. **Add Boss Enemy**:
   - Place boss enemy prefab as child
   - Ensure boss has `Enemy.cs` with OnDeath event
   - Boss should be significantly stronger than regular enemies
8. **Add Visual Elements**: Floor, walls, boss arena decorations
9. **Add RoomMinimapLink**: Set Room Type to "Boss"
10. **Save as Prefab**

#### 3.5 Elevator Room Prefab
1. **Create Empty GameObject**: Name it "ElevatorRoom"
2. **Add ElevatorRoom Component**: Attach `ElevatorRoom.cs` script
3. **Configure ElevatorRoom Settings**:
   - Requires Boss Cleared: ✓
   - Interior Size: (14, 10)
4. **Add BoxCollider2D**: Same trigger setup
5. **Assign Tile Assets**: Same as other rooms
6. **Configure Elevator System**:
   - **Elevator Spawn Point**: Create child GameObject "ElevatorSpawn"
   - **Elevator Prefab**: Assign in inspector (see Elevator Prefab section)
7. **Optional Enemies**: Usually no enemies, but can add if desired
8. **Add Visual Elements**: Floor, walls, elevator shaft decorations
9. **Add RoomMinimapLink**: Set Room Type to "Elevator"
10. **Save as Prefab**

### 4. Supporting Prefabs

#### 4.1 Tile Assets Setup
1. **Floor Tile**: Create or assign tile asset for walkable areas
2. **Wall Tile**: Create or assign tile asset for walls/barriers
3. **Door Tile (Block 2)**: Create or assign tile asset for locked doors
   - This tile should have collision to block player movement
   - Visual should clearly indicate a locked/closed door
   - Will be placed automatically at exit positions when room is locked

#### 4.2 Teleport Portal Prefab
1. **Create Empty GameObject**: Name it "TeleportPortal"
2. **Add TeleportPortal Component**: Attach `TeleportPortal.cs` script
3. **Configure Portal Settings**:
   - Activation Delay: 1.0
   - Is Active: ✓
   - Detection Radius: 2.0
   - Player Layer: Default
4. **Add BoxCollider2D**:
   - Is Trigger: ✓
   - Size: (2, 2)
5. **Add Visual Effects**:
   - **Portal Effect**: Child GameObject with particle system
   - **Portal Sprites**: Animated portal sprites
6. **Optional Components**:
   - Animator for portal animations
   - AudioSource for portal sounds
   - Light component for glow effect
7. **Save as Prefab**

#### 4.3 Elevator Prefab
1. **Create Empty GameObject**: Name it "Elevator"
2. **Add Elevator Component**: Attach `Elevator.cs` script (from ElevatorRoom.cs file)
3. **Configure Elevator Settings**:
   - Activation Delay: 1.0
   - Is Active: ✓
   - Detection Radius: 2.0
   - Player Layer: Default
4. **Add BoxCollider2D**:
   - Is Trigger: ✓
   - Size: (3, 3) - larger than portal
5. **Add Visual Effects**:
   - **Elevator Effect**: Particle system or animated sprites
   - **Elevator Platform**: Visual platform sprite
   - **Elevator Shaft**: Background shaft visuals
6. **Optional Components**:
   - Animator for elevator animations ("Activate" trigger, "IsActive" bool)
   - AudioSource for elevator sounds
   - Light component for illumination
7. **Save as Prefab**

#### 4.4 Item Prefabs
1. **Create Empty GameObject**: Name it after item type (e.g., "HealthPotion")
2. **Add ItemCollectable Component**: Attach `ItemCollectable.cs` script
3. **Configure Item Properties**:
   - Item Name: "Health Potion"
   - Item Description: "Restores 50 health"
   - Value: 50
4. **Add BoxCollider2D**:
   - Is Trigger: ✓
   - Size: (1, 1)
5. **Add Visual Components**:
   - SpriteRenderer with item icon
   - Optional particle effects
   - Optional floating animation
6. **Add Item Icon**: Assign sprite for UI/inventory display
7. **Save as Prefab** for each item type

#### 4.5 Minimap Tile Prefab
1. **Create UI GameObject**: Name it "MinimapTile"
2. **Add RectTransform**: Size (32, 32)
3. **Add Image Components**:
   - **Background**: Main Image component (tile background)
   - **Room Icon**: Child GameObject with Image (room type icon)
   - **Fog Overlay**: Child GameObject with Image (fog sprite)
4. **Add MinimapRoom Component**: Attach `MinimapRoom.cs` script
5. **Configure Visual States**:
   - Hidden Icon Color: (1, 1, 1, 0) - transparent
   - Discovered Icon Color: (1, 1, 1, 0.5) - dimmed
   - Revealed Icon Color: (1, 1, 1, 1) - full opacity
   - Fog Visible Color: (0, 0, 0, 0.8) - dark
   - Fog Hidden Color: (0, 0, 0, 0) - transparent
6. **Setup Hierarchy**:
   ```
   MinimapTile (Image - background)
   ├── RoomIcon (Image - room type icon)
   └── FogOverlay (Image - fog sprite)
   ```
7. **Save as Prefab**

### 5. DungeonGenerator Component Assignment
In the DungeonGenerator component, assign all room prefabs:
1. **Start Room Prefab**: Drag StartRoom prefab
2. **Fight Room Prefab**: Drag FightRoom prefab  
3. **Item Room Prefab**: Drag ItemRoom prefab
4. **Boss Room Prefab**: Drag BossRoom prefab
5. **Elevator Room Prefab**: Drag ElevatorRoom prefab

### 6. Minimap UI Setup
1. **Create Canvas**: Add Canvas component to scene
2. **Create Minimap Panel**: 
   - Add UI Panel as child of Canvas
   - Name it "MinimapPanel"
   - Set anchor to top-right corner
   - Set size (e.g., 200×150)
3. **Create Container**:
   - Add empty GameObject "MinimapContainer" as child of panel
   - Add RectTransform component
   - This will hold generated minimap tiles
4. **Assign to MinimapManager**:
   - Minimap Container: Drag MinimapContainer
   - Minimap Tile Prefab: Drag MinimapTile prefab
   - Room Icons: Assign sprites for each room type
   - Fog Sprite: Assign fog overlay sprite

### 7. Room Component Configuration
For each room prefab, ensure proper component setup:

#### Room.cs Configuration:
- **Interior Size**: Always (14, 10)
- **Floor Tile**: Assign tile asset for walkable areas
- **Wall Tile**: Assign tile asset for walls
- **Door Tile**: Assign tile asset for locked doors (tile block 2)
- **Grid/Tilemap**: Automatically created and configured
- **Tilemap Generation**: Happens automatically in Start()

#### Exit Door System (Tile-Based):
- **Locked State**: Door tile (block 2) placed at exit positions
- **Unlocked State**: Floor tile placed at exit positions
- **Exit Positions**: 
  - North: (7, 11) - center of north wall
  - South: (7, 0) - center of south wall
  - East: (15, 5) - center of east wall
  - West: (0, 5) - center of west wall
- **Automatic Management**: Room handles door tile placement/removal

*Note: No separate door GameObjects needed - uses tilemap tiles directly*

#### RoomMinimapLink.cs Configuration:
- **Linked Room**: Auto-assigned (same GameObject)
- **Room Type**: Set appropriate type (Start/Fight/Item/Boss/Elevator)
- **Minimap Manager**: Will be found automatically

## Configuration

### DungeonGenerator Settings
```csharp
public int gridRows = 3;          // Number of rows in dungeon grid
public int gridCols = 4;          // Number of columns in dungeon grid
public float roomSpacing = 6.4f;  // Spacing between rooms (16 tiles × 0.4 cell size)
public int fightRoomCount = 6;    // Number of fighting rooms per floor
public int elevatorRoomCount = 1;  // Number of elevator rooms per floor
```

**Important**: For tilemap systems with 0.4×0.4 cell size, set `roomSpacing` to 6.4 (16 tiles × 0.4) to ensure proper room alignment without gaps or overlaps.

### Camera Settings
```csharp
public float panSpeed = 2f;              // Speed of camera panning
public Vector2 roomOffset = Vector2.zero; // Offset for UI elements
public Vector2 roomSize = new Vector2(14, 10); // Interior room size
```

### Minimap Settings
```csharp
public Vector2 tileSize = new Vector2(32, 32);     // Size of minimap tiles
public Vector2 tileSpacing = new Vector2(4, 4);    // Spacing between tiles
```

## Gameplay Flow

### Room Interaction
1. **Player Entry**: Triggers room lock, camera pan, minimap update
2. **Combat**: Player fights enemies while doors are locked
3. **Room Clear**: All enemies defeated → doors unlock
4. **Boss Clear**: Teleport portal spawns → floor progression available

### Fog-of-War States
- **Hidden**: Icon off, fog on
- **Discovered**: Icon dimmed, fog off (adjacent to visited rooms)
- **Revealed**: Icon on, fog off (current player room)

### Floor Progression
1. Player enters boss room → doors lock
2. Player defeats boss → teleport portal spawns + elevator room activates
3. Player can use portal → advances to next floor OR use elevator → recreates level at origin
4. New floor generates → player moves to new start room

### Elevator System
1. Elevator room is placed adjacent to boss room
2. Elevator activates when boss is defeated
3. Using elevator recreates the current level at position (0,0)
4. Provides alternative to floor progression for exploration or repositioning

## Events System

### Room Events
```csharp
room.OnPlayerEntered += HandlePlayerEntered;
room.OnPlayerExited += HandlePlayerExited;
room.OnRoomCleared += HandleRoomCleared;
```

### Floor Manager Events
```csharp
floorManager.OnFloorChanged += HandleFloorChanged;
floorManager.OnNewFloorGenerated += HandleNewFloor;
```

## Enemy Integration

Enemies must implement the death event for room clearing:
```csharp
public System.Action<Enemy> OnDeath;

public void TakeDamage(int damage)
{
    health -= damage;
    if (health <= 0)
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
```

## Debug Features

### Editor Debug Methods
- `Room.DebugSetState()`: Manually set room state
- `DungeonGenerator.OnDrawGizmos()`: Visualize room grid
- `FloorManager.DebugNextFloor()`: Skip to next floor
- `MinimapRoom.DebugSetState()`: Test minimap visibility states

### Console Commands
All major systems provide detailed Debug.Log output for tracking state changes.

## Performance Considerations

- Room enemy detection uses cached lists updated on Start()
- Minimap uses object pooling for tile reuse
- Camera panning uses Time.deltaTime for frame-rate independence
- Event subscriptions are properly cleaned up in OnDestroy()

## Extension Points

### Custom Room Types
Inherit from `Room` class and override:
- `CheckRoomClearCondition()`: Custom victory conditions
- `EnterRoom()`/`ExitRoom()`: Special entry/exit effects
- `MarkCleared()`: Custom clear behavior

### Custom Items
Implement `ItemCollectable` interface or extend the provided component for special item behaviors.

### Custom Camera Behaviors
Extend `CameraRoomFollow` for special camera effects like screen shake, zoom changes, or tracking multiple targets.

## Troubleshooting

### Common Issues
1. **Rooms not connecting**: Check exit door assignments and DungeonGenerator room spacing
2. **Camera not following**: Ensure CameraRoomFollow is subscribed to room events via FloorManager
3. **Minimap not updating**: Verify RoomMinimapLink components are on room prefabs
4. **Enemies not clearing rooms**: Ensure Enemy.OnDeath event is properly implemented
5. **Elevator not spawning**: Check that elevator prefab is assigned and boss room exists
6. **Elevator not adjacent to boss**: Ensure grid has space around boss room for elevator placement

### Validation
Each component includes validation methods and detailed error logging to help identify setup issues.

## Prefab Creation Checklist

### ✅ Room Prefab Checklist:
- [ ] Room script attached (Room/ItemRoom/BossRoom/ElevatorRoom)
- [ ] BoxCollider2D added and configured as trigger
- [ ] Interior size set to (14, 10)
- [ ] All exit doors created and assigned
- [ ] RoomMinimapLink component added with correct room type
- [ ] Visual elements (floor, walls) added
- [ ] Enemy spawn points or special items configured

### ✅ Tile Assets Checklist:
- [ ] Floor tile asset created/assigned
- [ ] Wall tile asset created/assigned  
- [ ] Door tile (block 2) asset created/assigned
- [ ] Door tile has proper collision setup
- [ ] All tiles assigned to room prefabs

### ✅ Special Prefab Checklist:
- [ ] **Portal**: TeleportPortal script, trigger collider, visual effects
- [ ] **Elevator**: Elevator script, trigger collider, visual effects  
- [ ] **Items**: ItemCollectable script, trigger collider, sprites
- [ ] **Minimap Tile**: MinimapRoom script, proper UI hierarchy

### 🚨 Common Issues:
1. **Doors not blocking**: Ensure door tile (block 2) has proper collision setup
2. **Room not detecting player**: Check BoxCollider2D is set as trigger and sized correctly
3. **Items not collecting**: Verify ItemCollectable script and trigger collider
4. **Minimap not showing**: Check RoomMinimapLink room type assignment
5. **Portal/Elevator not working**: Ensure proper layer mask and detection radius
6. **Door tiles not appearing**: Check that door tile asset is assigned to Room component
7. **Tilemap spacing issues**: Ensure roomSpacing matches room total width (6.4 for 0.4×0.4 cells)
8. **Tile collision problems**: Verify TilemapCollider2D is properly configured

This system provides a complete foundation for dungeon-based gameplay with room progression, minimap navigation, and floor advancement mechanics.