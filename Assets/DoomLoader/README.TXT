Load Doom wads directly into Unity.
(currently Doom1 shareware wad)

Builds meshes for linedefs and sectors.
Constructs textures directly from WAD.
Creates Unity gameobjects for things.
Has option to override materials and textures.

How to use:
Download Doom1 shareware wad from the internet and place it into Assets/StreamingAssets

You can download shareware wad from:
https://doomwiki.org/wiki/DOOM1.WAD

Open up the test_scene.scene and hit play to load the first map
Change the "Autoload Map" parameter in the WadLoader component to load different map "E1M1" "E1M2" "E1M3"...
You can walk around using arrow keys and open doors with spacebar.


Version notes:

v1 - Initial release

v2 - Removed Vector2D struct and replaced all references with Unity's Vector2
   - Added first person weapon, created transparent billboard material and shader
   - Made mouselook and WSAD movement 
   - Implemented sounds for doors, weapon, enemies and linedefs
   - You can now shoot enemies and barrels dead
   - Created GameManager class and moved stuff from WadLoader into there
   - Removed the shader vertex transformation and went with classic gameobject rotation instead
   - Enemies now have the 5way sprite system
   - Added my AxMath library into the project
   - Mesher now has grid of triangle lists for quick querying for sectors
   - Removed my own ambient light parameter, the shaders now use Unity's Sky Ambient Light
   - Shaders in general are more cleaned up and parameter names are standardized
   - Thing's facing is now a separate quaternion decoupled from the object transform rotation
   - Homogenized linedef and sector controllers so they can be more easiely shared
   - All E1M1 and E1M2 linedefs are now done
   - Added a method to swap switch textures in TextureLoader
   - Made a fix for the sector 7 bug in E1M3
   - You can now unload maps and thus move from one map to another
   - Added a ground stick method to player object for smoother elevator movement

v3 - Went back to rotate sprites in shaders, billboards now keep y = up. Projectiles rotate omnidirectionally
   - Fixed AxMath.CartesianLineF to work when direction was true compass direction
   - Monsters and things are now in separate layers, as well as local player
   - Made it so you can no longer shoot yourself, don't know if this is a good thing
   - Added sectors and linedefs to the fast lookup cache, similar to triangles
   - Created TheGrid class for the caches, breath and pathfinding
   - Made AI class and heatmap calculation based on the grid data
   - Dynamic meshes now add Rigidbody component to avoid any problems or performance loss
   - Items can now be picked up. There's a PlayerInfo script that holds player inventory.
   - Keycard doors now require the correct key
   - Created hud to show player items
   - Added pain and pickup overlay to color the edges of the screen for a short time
   - Player plays sounds when poking, getting hurt or picking up items
   - Pokeable is now boolean to make player do the sound if the poke failed
   - There's a boolean to allow pokeable for monsters
   - Changed GameManager to refer to local player and made triggers to query for PlayerThing instead of localplayer
   - Player is now a monster-type thing, as well as demons
   - Made GameManager to skip frames after loading a map to stabilize Time.deltaTime 
   - Things now has unified gravity handling
   - Sectors have a property of being dynamic
   - Player casts breath that is used for making noise and generic pathfinding
   - Made physics collision matrix to optimize collisions and to disallow medikits to activate lifts
   - GameManager now handles loading of wads and maps
   - Zombies can now use common lifts and doors... monkaS
   - Monsters now have assigneable behavior that controls the intelligence
   - Renamed fivewaysprite to monstercontroller
   - Monster controller now refer to a common animation frame struct that holds the sprites for different rotations
   - Mesher, MapLoader, WadLoader and AI are now static classes
   - Damageable interface now has method for impules to make stuff be pushed by physical force. Only monsters use this atm
   - Shotgun and explosions now apply physical force to damageable objects

v4 - Added rest of the map linedefs from Doom shareware campaign
   - Optimized linedef and sector controllers to use common formats and reduced ticking
   - Fixed a lot of the linedef behavior in fringe/niche cases, looking at you E1M7 pillar of doom
   - There is now PlayerDamageReceive variable in GameManager to make the game easier (doesn't affect nukage)
   - Presynthesized music
   - Added Baron of Hell
   - Made a bytereader class to reduce repetition of code when reading raw wad data
   - Flipped the V coordinate of floors and ceilings, they should be correct now (E1M8 teleporter)
   - There is a modified OBJ exporter script that takes material names from textures used correctly
   - Fixed oneshot door keycards being in wrong order
   - There are couple hardcoded parts to fix bugs in maps or to enable old hardcoded behavior (eg. sector tag 666)
   - Commented a lot of the code
   - You can now fully play shareware Doom1 inside Unity!

   Missing Features:
   Menus
   Save/Load
   Minimap
   Powerups
   Monster infighting (monsters can damage each other, but they don't aggro)
   Gibbing

   Known bugs:
   Teleporters can place player too close to walls/ledges
   Walking between things might push you on top of them
   AI sometimes walks off the ledges unintentionally
   Monsters are unable to walk through certain areas, such as a keycard door that is open. This is very noticeable with the pink demons, but applies to all. The reason for this is that heatmap doesn't get updated, or the original calculations were not good enough.