using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;

namespace MaskedEnemyRework.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class MaskedSpawnSettings
    {
        private static Predicate<SpawnableEnemyWithRarity> isMasked    = enemy => enemy.enemyType.enemyName == "Masked";
        private static Predicate<SpawnableEnemyWithRarity> isFlowerman = enemy => enemy.enemyType.enemyName == "Flowerman";

        // int on v49, but float on v50
        private static FieldInfo powerLevelField            = typeof(EnemyType).GetField("PowerLevel");
        private static FieldInfo currentMaxInsidePowerField = typeof(RoundManager).GetField("currentMaxInsidePower");

        public static T StupidGet<T>(object obj, FieldInfo field)
        {
            return (T) Convert.ChangeType(field.GetValue(obj), typeof(T));
        }

        public static bool isZombieApocalypse = false;

        [HarmonyPatch("BeginEnemySpawning")]
        [HarmonyPrefix]
        static void UpdateSpawnRates(ref SelectableLevel ___currentLevel)
        {
            PluginConfig cfg = Plugin.cfg;

            if (cfg.UseVanillaSpawns)
                return;

            ManualLogSource logger = Plugin.logger;
            logger.LogInfo("Starting Round Manager");

            SpawnableEnemyWithRarity maskedEnemy = Plugin.maskedPrefab;
            SpawnableEnemyWithRarity flowerman   = ___currentLevel.Enemies.Find(isFlowerman) ?? Plugin.flowerPrefab;

            isZombieApocalypse = cfg.ZombieApocalypseMode || ( (StartOfRound.Instance.randomMapSeed % 100) < cfg.ZombieApocalypeRandomChance );

            try
            {
                maskedEnemy.enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP = cfg.Health;

                float powerDelta = 0.0f;
                foreach (SpawnableEnemyWithRarity enemy in ___currentLevel.Enemies.FindAll(isMasked))
                {
                    powerDelta -= enemy.enemyType.MaxCount * StupidGet<float>(enemy.enemyType, powerLevelField);
                }
                ___currentLevel.Enemies.RemoveAll(isMasked);
                ___currentLevel.Enemies.Add(maskedEnemy);

                if (cfg.CanSpawnOutside)
                {
                    ___currentLevel.OutsideEnemies.RemoveAll(isMasked);
                    ___currentLevel.OutsideEnemies.Add(maskedEnemy);
                    ___currentLevel.DaytimeEnemies.RemoveAll(isMasked);
                    ___currentLevel.DaytimeEnemies.Add(maskedEnemy);
                }


                float maskedPowerLevel = isZombieApocalypse ? cfg.ZombiePowerLevel : cfg.PowerLevel;

                 // C++ dev tries to figure out C#
                powerLevelField.SetValue(maskedEnemy.enemyType, Convert.ChangeType(maskedPowerLevel, powerLevelField.FieldType));

                maskedEnemy.enemyType.probabilityCurve = flowerman.enemyType.probabilityCurve;
                maskedEnemy.enemyType.isOutsideEnemy   = cfg.CanSpawnOutside;

                if (isZombieApocalypse)
                {
                    logger.LogInfo("ZOMBIE APOCALYPSE");

                    maskedEnemy.enemyType.MaxCount = cfg.MaxZombies;
                    maskedEnemy.rarity = 1000000;

                    //Plugin.RandomChanceZombieApocalypse = -1;

                    if (cfg.UseZombieSpawnCurve)
                    {
                        ___currentLevel.enemySpawnChanceThroughoutDay = new AnimationCurve((Keyframe[])(object)new Keyframe[2]
                        {
                            new Keyframe(0f,   cfg.InsideEnemySpawnCurve),
                            new Keyframe(0.5f, cfg.MiddayInsideEnemySpawnCurve)
                        });
                        ___currentLevel.daytimeEnemySpawnChanceThroughDay = new AnimationCurve((Keyframe[])(object)new Keyframe[2]
                        {
                            new Keyframe(0f,   7f),
                            new Keyframe(0.5f, 7f)
                        });
                        ___currentLevel.outsideEnemySpawnChanceThroughDay = new AnimationCurve((Keyframe[])(object)new Keyframe[3]
                        {
                            new Keyframe(0f,  cfg.StartOutsideEnemySpawnCurve),
                            new Keyframe(20f, cfg.MidOutsideEnemySpawnCurve),
                            new Keyframe(21f, cfg.EndOutsideEnemySpawnCurve)
                        });
                    }
                }
                else
                {
                    logger.LogInfo("no zombies :(");

                    maskedEnemy.enemyType.MaxCount = cfg.MaxSpawnCount;
                    maskedEnemy.rarity = cfg.UseSpawnRarity ? cfg.SpawnRarity : flowerman.rarity;
                }

                powerDelta += maskedEnemy.enemyType.MaxCount * maskedPowerLevel;

                if (cfg.BoostMoonPowerLevel)
                {
                    logger.LogInfo(String.Format("Adjusting power levels: [maxEnemyPowerCount: {0}+{1}, maxDaytimeEnemyPowerCount: {2}+{3}, maxOutsideEnemyPowerCount: {4}+{5}]",
                                                ___currentLevel.maxEnemyPowerCount,        powerDelta,
                                                ___currentLevel.maxDaytimeEnemyPowerCount, powerDelta,
                                                ___currentLevel.maxOutsideEnemyPowerCount, powerDelta));

                    ___currentLevel.maxEnemyPowerCount        += (int)powerDelta;
                    ___currentLevel.maxDaytimeEnemyPowerCount += (int)powerDelta;
                    ___currentLevel.maxOutsideEnemyPowerCount += (int)powerDelta;
                }
            }
            catch (Exception ex)
            {
               logger.LogInfo(ex);
            }
        }

        [HarmonyPatch("AssignRandomEnemyToVent")]
        [HarmonyPrefix]
        static bool ZombieVent(
                EnemyVent           vent,
                float               spawnTime,
                ref RoundManager    __instance,
                ref bool            __result,
                ref SelectableLevel ___currentLevel,
                ref TimeOfDay       ___timeScript,
                ref bool            ___cannotSpawnMoreInsideEnemies,
                ref bool            ___firstTimeSpawningEnemies,
                ref int             ___currentEnemyPower,
                ref int             ___currentHour)
        {
            if (Plugin.cfg.UseVanillaSpawns || !isZombieApocalypse)
            {
                return true; // Do vanilla behaviour instead
            }

            if (___firstTimeSpawningEnemies)
            {
                foreach (SpawnableEnemyWithRarity spawnable in ___currentLevel.Enemies)
                {
                    spawnable.enemyType.numberSpawned = 0;
                }
            }
            ___firstTimeSpawningEnemies = false;

            ManualLogSource logger = Plugin.logger;

            int maskedIndex = ___currentLevel.Enemies.FindIndex(isMasked);

            SpawnableEnemyWithRarity maskedEnemy = ___currentLevel.Enemies[maskedIndex];

            if (maskedIndex == -1)
            {
                logger.LogInfo("No masked found in enemy list?");
                return true; // Do vanilla behaviour instead
            }

            if (maskedEnemy.enemyType.numberSpawned >= maskedEnemy.enemyType.MaxCount)
            {
                __result = false;
                ___cannotSpawnMoreInsideEnemies = true;
                logger.LogInfo("Max masked spawned");
                return false;
            }

            float maskedPowerLevel      = StupidGet<float>(maskedEnemy.enemyType, powerLevelField);
            float currentMaxInsidePower = StupidGet<float>(__instance, currentMaxInsidePowerField);
            float availableInsidePower  = currentMaxInsidePower - ___currentEnemyPower;

            logger.LogInfo("available inside power: " + availableInsidePower);

            if (maskedPowerLevel > availableInsidePower)
            {
                __result = false;
                ___cannotSpawnMoreInsideEnemies = true;
                logger.LogInfo("Max power");
                return false;
            }

            ___currentEnemyPower += (int) maskedPowerLevel;
            vent.enemyType = maskedEnemy.enemyType;
            vent.enemyTypeIndex = maskedIndex;
            vent.occupied = true;
            vent.spawnTime = spawnTime;
            if (___timeScript.hour - ___currentHour > 0)
            {
                logger.LogInfo("Round manager catching up to time yada yada UvU.");
            }
            else
            {
                vent.SyncVentSpawnTimeClientRpc((int)spawnTime, maskedIndex);
            }
            maskedEnemy.enemyType.numberSpawned ++;

            logger.LogInfo("Spawned a masked");

            __result = true;

            return false;
        }
    }
}
