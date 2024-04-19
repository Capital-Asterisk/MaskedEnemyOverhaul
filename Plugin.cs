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
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        private static Plugin Instance;
        public static ManualLogSource logger;

        public static List<int> PlayerMimicList;
        public static int PlayerMimicIndex;

        private ConfigEntry<bool> RemoveMasksConfig;
        private ConfigEntry<bool> RevealMasksConfig;
        private ConfigEntry<bool> ShowMaskedNamesConfig;
        private ConfigEntry<bool> RemoveZombieArmsConfig;
        private ConfigEntry<bool> UseSpawnRarityConfig;
        private ConfigEntry<int> SpawnRarityConfig;
        private ConfigEntry<bool> CanSpawnOutsideConfig;
        private ConfigEntry<int> MaxSpawnCountConfig;


        private ConfigEntry<bool> ZombieApocalypeModeConfig;
        private ConfigEntry<bool> UseVanillaSpawnsConfig;
        private ConfigEntry<bool> DontTouchMimickingPlayerConfig;
        private ConfigEntry<int> ZombieApocalypeRandomChanceConfig;
        private ConfigEntry<int> MaxZombiesZombieConfig;
        private ConfigEntry<float> InsideEnemySpawnCurveConfig;
        private ConfigEntry<float> MiddayInsideEnemySpawnCurveConfig;
        private ConfigEntry<float> StartOutsideEnemySpawnCurveConfig;
        private ConfigEntry<float> MidOutsideEnemySpawnCurveConfig;
        private ConfigEntry<float> EndOutsideEnemySpawnCurveConfig;

        public static bool RemoveMasks;
        public static bool RevealMasks;
        public static bool ShowMaskedNames;
        public static bool RemoveZombieArms;
        public static bool UseSpawnRarity;
        public static int SpawnRarity;
        public static bool CanSpawnOutside;
        public static int MaxSpawnCount;


        public static int MaxZombies;
        public static bool ZombieApocalypseMode;
        public static bool UseVanillaSpawns;
        public static bool DontTouchMimickingPlayer;
        public static int RandomChanceZombieApocalypse;
        public static float InsideEnemySpawnCurve;
        public static float MiddayInsideEnemySpawnCurve;
        public static float StartOutsideEnemySpawnCurve;
        public static float MidOutsideEnemySpawnCurve;
        public static float EndOutsideEnemySpawnCurve;

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


            ShowMaskedNamesConfig = Config.Bind<bool>("General", "Show Masked Usernames", false, "Will show username of player being mimicked.");
            RemoveMasksConfig = Config.Bind<bool>("General", "Remove Mask From Masked Enemy", true, "Whether or not the Masked Enemy has a mask on.");
            RevealMasksConfig = Config.Bind<bool>("General", "Reveal Mask When Attacking", false, "The enemy would reveal their mask permanently after trying to attack someone. Mask would be off until the attempt to attack is made");
            RemoveZombieArmsConfig = Config.Bind<bool>("General", "Remove Zombie Arms", true, "Remove the animation where the Masked raise arms like a zombie.");
            UseVanillaSpawnsConfig = Config.Bind<bool>("General", "Use Vanilla Spawns", false, "Ignores anything else in this mod. Only uses the above settings from this config. Will not spawn on all moons. will ignore EVERYTHING in the config below this point.");
            DontTouchMimickingPlayerConfig = Config.Bind<bool>("General", "Dont Touch MaskedPlayerEnemy.mimickingPlayer", false, "Experimental. Give control to other mods (like qwbarch-Mirage) to set which players are impersonated.");



            UseSpawnRarityConfig = Config.Bind<bool>("Spawns", "Use Spawn Rarity", false, "Use custom spawn rate from config. If this is false, the masked spawns at the same rate as the Bracken. If true, will spawn at whatever rarity is given in Spawn Rarity config option");
            SpawnRarityConfig = Config.Bind<int>("Spawns", "Spawn Rarity", 15, "The rarity for the Masked Enemy to spawn. The higher the number, the more likely to spawn. Can go to 1000000000, any higher will break. Use Spawn Rarity must be set to True");
            CanSpawnOutsideConfig = Config.Bind<bool>("Spawns", "Allow Masked To Spawn Outside", false, "Whether the Masked Enemy can spawn outside the building");
            MaxSpawnCountConfig = Config.Bind<int>("Spawns", "Max Number of Masked", 2, "Max Number of possible masked to spawn in one level");



            ZombieApocalypeModeConfig = Config.Bind<bool>("Zombie Apocalypse Mode", "Zombie Apocalypse Mode", false, "Only spawns Masked! Make sure to crank up the Max Spawn Count in this config! Would also recommend bringing a gun (mod), a shovel works fine too though.... This mode does not play nice with other mods that affect spawn rates. Disable those before playing for best results");
            MaxZombiesZombieConfig = Config.Bind<int>("Zombie Apocalypse Mode", "Max Number of Masked in Zombie Apocalypse", 2, "Max Number of possible masked to spawn in Zombie Apocalypse");
            ZombieApocalypeRandomChanceConfig = Config.Bind<int>("Zombie Apocalypse Mode", "Random Zombie Apocalypse Mode", -1, "[Must Be Whole Number] The percent chance from 1 to 100 that a day could contain a zombie apocalypse. Put at -1 to never have the chance arise and don't have Only Spawn Masked turned on");
            InsideEnemySpawnCurveConfig = Config.Bind<float>("Zombie Apocalypse Mode", "StartOfDay Inside Masked Spawn Curve", 0.1f, "Spawn curve for masked inside, start of the day. Crank this way up for immediate action. More info in the readme");
            MiddayInsideEnemySpawnCurveConfig = Config.Bind<float>("Zombie Apocalypse Mode", "Midday Inside Masked Spawn Curve", 500f, "Spawn curve for masked inside, midday.");
            StartOutsideEnemySpawnCurveConfig = Config.Bind<float>("Zombie Apocalypse Mode", "StartOfDay Masked Outside Spawn Curve", -30f, "Spawn curve for outside masked, start of the day.");
            MidOutsideEnemySpawnCurveConfig = Config.Bind<float>("Zombie Apocalypse Mode", "Midday Outside Masked Spawn Curve", -30f, "Spawn curve for outside masked, midday.");
            EndOutsideEnemySpawnCurveConfig = Config.Bind<float>("Zombie Apocalypse Mode", "EOD Outside Masked Spawn Curve", 10f, "Spawn curve for outside masked, end of day");

            RemoveMasks = RemoveMasksConfig.Value;
            ShowMaskedNames = ShowMaskedNamesConfig.Value;
            RevealMasks = RevealMasksConfig.Value;
            UseVanillaSpawns = UseVanillaSpawnsConfig.Value;
            DontTouchMimickingPlayer = DontTouchMimickingPlayerConfig.Value;
            RemoveZombieArms = RemoveZombieArmsConfig.Value;
            UseSpawnRarity = UseSpawnRarityConfig.Value;
            CanSpawnOutside = CanSpawnOutsideConfig.Value;
            MaxSpawnCount = MaxSpawnCountConfig.Value;
            SpawnRarity = SpawnRarityConfig.Value;



            ZombieApocalypseMode = ZombieApocalypeModeConfig.Value;
            MaxZombies = MaxZombiesZombieConfig.Value;
            InsideEnemySpawnCurve = InsideEnemySpawnCurveConfig.Value;
            MiddayInsideEnemySpawnCurve = MiddayInsideEnemySpawnCurveConfig.Value;
            StartOutsideEnemySpawnCurve = StartOutsideEnemySpawnCurveConfig.Value;
            MidOutsideEnemySpawnCurve = MidOutsideEnemySpawnCurveConfig.Value;
            EndOutsideEnemySpawnCurve = EndOutsideEnemySpawnCurveConfig.Value;
            RandomChanceZombieApocalypse = ZombieApocalypeRandomChanceConfig.Value;

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
