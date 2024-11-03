using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MaskedEnemyRework.Patches;
using System;
using System.Collections.Generic;
//using System.Security.Policy;
using Unity.Netcode;

namespace MaskedEnemyRework
{
    public class PluginConfig
    {
        public bool  RemoveMasks                    = Cfg<bool>( "General", "Remove Mask From Masked Enemy", true, "Whether or not the Masked Enemy has a mask on.");
        public bool  RevealMasks                    = Cfg<bool>( "General", "Reveal Mask When Attacking",   false, "The enemy would reveal their mask permanently after trying to attack someone. Mask would be off until the attempt to attack is made");
        public bool  RemoveZombieArms               = Cfg<bool>( "General", "Remove Zombie Arms", true, "Remove the animation where the Masked raise arms like a zombie.");
        public bool  TriggerMines                   = Cfg<bool>( "General", "Masked Trigger Mines", true, "Masked go KABOOM when walking over a mine.");
        public int   Health                         = Cfg<int> ( "General", "Masked Health", 4, "Number of shovel hits required to kill a Masked.");
        public bool  UseVanillaSpawns               = Cfg<bool>( "General", "Use Vanilla Spawns", false, "Disables all spawning rules from this mod. Only uses the above settings from this config. Will not spawn on all moons. will ignore EVERYTHING in the config below this point.");
        public bool  DontTouchMimickingPlayer       = Cfg<bool>( "General", "Dont Touch MaskedPlayerEnemy.mimickingPlayer", false, "Experimental. Give control to other mods (like qwbarch-Mirage) to set which players are impersonated.");
        public bool  UseStupidFix                   = Cfg<bool>( "General", "Fix Invisible Masked Enemy Bug", true, "Stupid workaround for some interaction with MoreCompany that make Masked invisible when opening/closing the ship door.");
        public bool  ShowMaskedNames                = Cfg<bool>( "General", "Show Masked Usernames", false, "[UNUSED FOR NOW] Will show username of player being mimicked.");

        public bool  UseSpawnRarity                 = Cfg<bool>( "Spawns", "Use Spawn Rarity", false, "Use custom spawn rate from config. If this is false, the masked spawns at the same rate as the Bracken. If true, will spawn at whatever rarity is given in Spawn Rarity config option");
        public int   SpawnRarity                    = Cfg<int>(  "Spawns", "Spawn Rarity", 15, "The rarity for the Masked Enemy to spawn. The higher the number, the more likely to spawn. Can go to 1000000000, any higher will break. Use Spawn Rarity must be set to True");
        public bool  CanSpawnOutside                = Cfg<bool>( "Spawns", "Allow Masked To Spawn Outside", false, "Whether the Masked Enemy can spawn outside the building");
        public int   MaxSpawnCount                  = Cfg<int>(  "Spawns", "Max Number of Masked", 3, "Vents will stop spawning Masked when this limit is hit. Masked can still spawn through other means, like players getting possessed.");
        public float PowerLevel                     = Cfg<float>("Spawns", "Masked Power Level", 1.0f, "How much of the moon's Power Level each Masked consumes. Higher = Less entities");
        public bool  BoostMoonPowerLevel            = Cfg<bool>( "Spawns", "Boost Moon Power Level", false, "Increase moon indoor max power level by (Max Masked * Masked Power Level). Allows more Masked and other monsters to spawn. Original MEO behavior.");

        public bool  ZombieApocalypseMode           = Cfg<bool>( "Zombie Apocalypse Mode", "Always Zombie Apocalypse", false, "Only spawns Masked! Make sure to crank up the Max Spawn Count in this config! Would also recommend bringing a gun (mod), a shovel works fine too though.... This mode does not play nice with other mods that affect spawn rates. Disable those before playing for best results");
        public int   ZombieApocalypeRandomChance    = Cfg<int>(  "Zombie Apocalypse Mode", "Random Zombie Apocalypse", -1, "[Must Be Whole Number] The percent chance from 1 to 100 that a day could contain a zombie apocalypse. Put at -1 to never have the chance arise and don't have Only Spawn Masked turned on");
        public int   MaxZombies                     = Cfg<int>(  "Zombie Apocalypse Mode", "Max Zombies", 6, "Max Masked for Zombie Apocalypse. Vents will stop spawning Masked when this limit is hit.");
        public float ZombiePowerLevel               = Cfg<float>("Zombie Apocalypse Mode", "Zombie Power Level", 2.0f, "Masked power level during Zombie Apocalypse. Higher = Less Zombies. This can limit max zombies by moon difficulty, even if it's lower than what the 'Max Zombies' option allows. Set to 0 to use Max Zombies for all moons. Moon Indoor Power Levels for reference: [Experimentation: 4, Offense: 12, Titan: 18]");

        public bool  UseZombieSpawnCurve            = Cfg<bool>( "Zombie Apocalypse Mode", "Use Spawn Curves", false, "[BUGGED: This likely permanently modifies the level spawning options until the game is restarted] Edit level spawn curves during a Zombie Apocalypse; options below. Original MEO behavior.");
        public float InsideEnemySpawnCurve          = Cfg<float>("Zombie Apocalypse Mode", "StartOfDay Inside Masked Spawn Curve", 0.1f, "Spawn curve for masked inside, start of the day. Crank this way up for immediate action. More info in the readme");
        public float MiddayInsideEnemySpawnCurve    = Cfg<float>("Zombie Apocalypse Mode", "Midday Inside Masked Spawn Curve", 500f, "Spawn curve for masked inside, midday.");
        public float StartOutsideEnemySpawnCurve    = Cfg<float>("Zombie Apocalypse Mode", "StartOfDay Masked Outside Spawn Curve", -30f, "Spawn curve for outside masked, start of the day.");
        public float MidOutsideEnemySpawnCurve      = Cfg<float>("Zombie Apocalypse Mode", "Midday Outside Masked Spawn Curve", -30f, "Spawn curve for outside masked, midday.");
        public float EndOutsideEnemySpawnCurve      = Cfg<float>("Zombie Apocalypse Mode", "EOD Outside Masked Spawn Curve", 10f, "Spawn curve for outside masked, end of day");

        public static List<ConfigEntryBase> entries = new List<ConfigEntryBase>();

        public static T Cfg<T>(string category, string name, T defaultVal, string description)
        {
            ConfigEntry<T> entry = Plugin.Instance.Config.Bind<T>(category, name, defaultVal, description);
            entries.Add(entry);
            return entry.Value;
        }
    }


    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        public static Plugin Instance;
        public static ManualLogSource logger;
        public static PluginConfig cfg;

        public static List<int> PlayerMimicList;
        public static int PlayerMimicIndex;
        public static int InitialPlayerCount;
        public static SpawnableEnemyWithRarity maskedPrefab;
        public static SpawnableEnemyWithRarity flowerPrefab;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            PlayerMimicList = new List<int> { };
            PlayerMimicIndex = 0;
            InitialPlayerCount = 0;

            cfg = new PluginConfig();

            logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! Woohoo!");

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(GetMaskedPrefabForLaterUse));
            harmony.PatchAll(typeof(MaskedVisualRework));
            harmony.PatchAll(typeof(MaskedSpawnSettings));

            if (cfg.UseStupidFix)
            {
                harmony.PatchAll(typeof(UltraStupidFix));
            }

            if (cfg.TriggerMines)
            {
                harmony.PatchAll(typeof(LandmineVsMasked));
            }
        }
    }
}
