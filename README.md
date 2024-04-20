# Masked Enemy Overhaul Fork (v3.3.0)

Based on [MaskedEnemyOverhaul](https://thunderstore.io/c/lethal-company/p/HomelessGinger/MaskedEnemyOverhaul/) by HomelessGinger

+ Removes the mask the mimic wears, so no one can tell its a fake until it is too late.
+ Boosts the spawn rate to be the same as a bracken, so it can now spawn on every moon
+ Masked can set off Landmines
+ Changes the suit the mimic spawns with to a suit worn by a random player in the game. This works with any modded suit as well
+ Supports cosmetics
+ Most settings are configurable in the config file
+ A Zombie Apocalypse???

## Important info:

* Tested on Lethal Company v49 and v50
* (April 13 2024) There's currently a vanilla bug in LC v50 where masked enemies spawned inside cannot harm players outside.
* Nametags on Masked do not work, disabled for now
* This mod works in both solo and multiplayer lobbies. Everyone download it.

## Recommended Mods

+ Skinwalkers by RedbugRedfern. Records and plays the voices of your friends back to you. I also recommend changing the config file for Skinwalkers to only have the masked enemy talk
+ Mirage by qwbarch. Mimics voices like Skinwalkers but synced across clients, players turning to Maskeds on death, lots of feature overlap, and more.
+ StarlancerAIFix by AudioKnight. Fixes lots of enemy AI issues, including Maskeds.

## Zombie Apocalypse (DOESN'T WORK)

+ This is just a fun little part of the mod. It only spawns the masked enemies, with the ability to bump up the enemy spawn rate. Do not use this mode with other mods that affect spawn rates. There is a very good chance they conflict. Turn it on in the config.
+ Spawns may feel off, as of now i use the default vanilla spawning methods (letting the vanilla game decide when to spawn a new batch of enemies). this could feel very slow at times since it is random and tied to in game time.
+ Also has random zombie event, it is very unvanilla so it is off by default. 
+ For the spawn curves, the numbers can go very high. As an example, putting the starting numbers at 500 will spawn several enemies on the ground before you even land. Turn up the end of day numbers and leave the start of day numbers vanilla for a rapid increase of Masked as the day progresses.

## Development

See LC Modding Discord (https://discord.com/invite/lcmod)

Planned changes for this Fork:
* Rework spawning and Zombie Apocalypse
* Fix name tags
* Make everything configurable

## Building

Put referenced DLLs in a new directory ./ReferencesLCv50

* Assembly-CSharp.dll from `Lethal Company_Data/Managed`
* LethalLib.dll
* MoreCompany.dll
* System.Security.dll
* Unity.Netcode.Runtime.dll from `Lethal Company_Data/Managed`
* UnityEngine.UIModule.dll from `Lethal Company_Data/Managed`

build with dotnet. idk, i'm a c++ dev.
