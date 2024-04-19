using BepInEx;
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
        public bool  ShowMaskedNames                = Cfg<bool>("General", "Show Masked Usernames", false, "Will show username of player being mimicked.");
        public bool  RemoveMasks                    = Cfg<bool>("General", "Remove Mask From Masked Enemy", true, "Whether or not the Masked Enemy has a mask on.");
        public bool  RevealMasks                    = Cfg<bool>("General", "Reveal Mask When Attacking", false, "The enemy would reveal their mask permanently after trying to attack someone. Mask would be off until the attempt to attack is made");
        public bool  RemoveZombieArms               = Cfg<bool>("General", "Remove Zombie Arms", true, "Remove the animation where the Masked raise arms like a zombie.");
        public bool  UseVanillaSpawns               = Cfg<bool>("General", "Use Vanilla Spawns", false, "Ignores anything else in this mod. Only uses the above settings from this config. Will not spawn on all moons. will ignore EVERYTHING in the config below this point.");
        public bool  DontTouchMimickingPlayer       = Cfg<bool>("General", "Dont Touch MaskedPlayerEnemy.mimickingPlayer", false, "Experimental. Give control to other mods (like qwbarch-Mirage) to set which players are impersonated.");

        public bool  UseSpawnRarity                 = Cfg<bool>("Spawns", "Use Spawn Rarity", false, "Use custom spawn rate from config. If this is false, the masked spawns at the same rate as the Bracken. If true, will spawn at whatever rarity is given in Spawn Rarity config option");
        public int   SpawnRarity                    = Cfg<int>( "Spawns", "Spawn Rarity", 15, "The rarity for the Masked Enemy to spawn. The higher the number, the more likely to spawn. Can go to 1000000000, any higher will break. Use Spawn Rarity must be set to True");
        public bool  CanSpawnOutside                = Cfg<bool>("Spawns", "Allow Masked To Spawn Outside", false, "Whether the Masked Enemy can spawn outside the building");
        public int   MaxSpawnCount                  = Cfg<int>( "Spawns", "Max Number of Masked", 2, "Max Number of possible masked to spawn in one level");

        public bool  ZombieApocalypseMode           = Cfg<bool>( "Zombie Apocalypse Mode", "Zombie Apocalypse Mode", false, "Only spawns Masked! Make sure to crank up the Max Spawn Count in this config! Would also recommend bringing a gun (mod), a shovel works fine too though.... This mode does not play nice with other mods that affect spawn rates. Disable those before playing for best results");
        public int   MaxZombies                     = Cfg<int>(  "Zombie Apocalypse Mode", "Max Number of Masked in Zombie Apocalypse", 2, "Max Number of possible masked to spawn in Zombie Apocalypse");
        public int   ZombieApocalypeRandomChance    = Cfg<int>(  "Zombie Apocalypse Mode", "Random Zombie Apocalypse Mode", -1, "[Must Be Whole Number] The percent chance from 1 to 100 that a day could contain a zombie apocalypse. Put at -1 to never have the chance arise and don't have Only Spawn Masked turned on");
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

        public static List<int> PlayerMimicList;
        public static int PlayerMimicIndex;

        public static PluginConfig cfg;

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
            harmony.PatchAll(typeof(LandmineVsMasked));
        }
    }
}
