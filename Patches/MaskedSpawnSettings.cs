using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace MaskedEnemyRework.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class MaskedSpawnSettings
    {

        [HarmonyPatch("BeginEnemySpawning")]
        [HarmonyPrefix]
        static void UpdateSpawnRates(ref SelectableLevel ___currentLevel)
        {
            if (Plugin.UseVanillaSpawns)
                return;

            ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            logger.LogInfo("Starting Round Manager");

            // PowerLevel is int on v49, but float on v50
            FieldInfo powerLevelField = typeof(EnemyType).GetField("PowerLevel");

            Predicate<SpawnableEnemyWithRarity> isMasked    = enemy => enemy.enemyType.enemyName == "Masked";
            Predicate<SpawnableEnemyWithRarity> isFlowerman = enemy => enemy.enemyType.enemyName == "Flowerman";

            try
            {
                SpawnableEnemyWithRarity maskedEnemy = Plugin.maskedPrefab;
                SpawnableEnemyWithRarity flowerman   = ___currentLevel.Enemies.Find(isFlowerman) ?? Plugin.flowerPrefab;

                float powerDelta = 0.0f;
                foreach (SpawnableEnemyWithRarity enemy in ___currentLevel.Enemies.FindAll(isMasked))
                {
                    powerDelta -= enemy.enemyType.MaxCount * (float) powerLevelField.GetValue(enemy.enemyType);
                }
                ___currentLevel.Enemies.RemoveAll(isMasked);
                ___currentLevel.Enemies.Add(maskedEnemy);

                if (Plugin.CanSpawnOutside)
                {
                    ___currentLevel.OutsideEnemies.RemoveAll(isMasked);
                    ___currentLevel.OutsideEnemies.Add(maskedEnemy);
                    ___currentLevel.DaytimeEnemies.RemoveAll(isMasked);
                    ___currentLevel.DaytimeEnemies.Add(maskedEnemy);
                }

                // might spawn too frequently, we will see.
                const float maskedPowerLevel = 1.0f;

                // C++ dev tries to figure out C#
                powerLevelField.SetValue(maskedEnemy.enemyType, Convert.ChangeType(maskedPowerLevel, powerLevelField.FieldType));

                maskedEnemy.enemyType.probabilityCurve = flowerman.enemyType.probabilityCurve;
                maskedEnemy.enemyType.isOutsideEnemy   = Plugin.CanSpawnOutside;

                bool zombieApocalypse = Plugin.ZombieApocalypseMode;
                zombieApocalypse |= (StartOfRound.Instance.randomMapSeed % 100) < Plugin.RandomChanceZombieApocalypse;

                if (zombieApocalypse)
                {
                    logger.LogInfo("ZOMBIE APOCALYPSE");

                    maskedEnemy.enemyType.MaxCount = Plugin.MaxZombies;
                    maskedEnemy.rarity = 1000000;

                    Plugin.RandomChanceZombieApocalypse = -1;

                    ___currentLevel.enemySpawnChanceThroughoutDay = new AnimationCurve((Keyframe[])(object)new Keyframe[2]
                    {
                        new Keyframe(0f,   Plugin.InsideEnemySpawnCurve),
                        new Keyframe(0.5f, Plugin.MiddayInsideEnemySpawnCurve)
                    });
                    ___currentLevel.daytimeEnemySpawnChanceThroughDay = new AnimationCurve((Keyframe[])(object)new Keyframe[2]
                    {
                        new Keyframe(0f,   7f),
                        new Keyframe(0.5f, 7f)
                    });
                    ___currentLevel.outsideEnemySpawnChanceThroughDay = new AnimationCurve((Keyframe[])(object)new Keyframe[3]
                    {
                        new Keyframe(0f,  Plugin.StartOutsideEnemySpawnCurve),
                        new Keyframe(20f, Plugin.MidOutsideEnemySpawnCurve),
                        new Keyframe(21f, Plugin.EndOutsideEnemySpawnCurve)
                    });
                }
                else
                {
                    logger.LogInfo("no zombies :(");

                    maskedEnemy.enemyType.MaxCount = Plugin.MaxSpawnCount;
                    maskedEnemy.rarity = Plugin.UseSpawnRarity ? Plugin.SpawnRarity : flowerman.rarity;
                }

                powerDelta += maskedEnemy.enemyType.MaxCount * maskedPowerLevel;

                logger.LogInfo(String.Format("Adjusting power levels: [maxEnemyPowerCount: {0}+{1}, maxDaytimeEnemyPowerCount: {2}+{3}, maxOutsideEnemyPowerCount: {4}+{5}]",
                                             ___currentLevel.maxEnemyPowerCount,        powerDelta,
                                             ___currentLevel.maxDaytimeEnemyPowerCount, powerDelta,
                                             ___currentLevel.maxOutsideEnemyPowerCount, powerDelta));

                ___currentLevel.maxEnemyPowerCount        += (int)powerDelta;
                ___currentLevel.maxDaytimeEnemyPowerCount += (int)powerDelta;
                ___currentLevel.maxOutsideEnemyPowerCount += (int)powerDelta;
            }
            catch (Exception ex)
            {
               logger.LogInfo(ex);
            }
        }
    }
}
