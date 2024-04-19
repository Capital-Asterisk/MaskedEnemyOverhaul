using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MaskedEnemyRework.Patches
{
    [HarmonyPatch]
    internal class GetMaskedPrefabForLaterUse
    {
        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        static void SavesPrefabForLaterUse(ref SelectableLevel[] ___moonsCatalogueList)
        {
            ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            foreach(var moon in ___moonsCatalogueList) // finds enemy type to add to 
            {
                foreach(SpawnableEnemyWithRarity enemy in moon.Enemies)
                {
                    if(enemy.enemyType.enemyName == "Masked")
                    {
                        logger.LogInfo("Found Masked!");
                        Plugin.maskedPrefab = enemy;
                    } else if(enemy.enemyType.enemyName == "Flowerman")
                    {
                        Plugin.flowerPrefab = enemy;
                        logger.LogInfo("Found Flowerman!");

                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPrefix]
        static void LookingAtMasked(ref PlayerControllerB __instance)
        {
            if (!Plugin.cfg.ShowMaskedNames)
                return;

            Ray ray = new(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            LayerMask layerMask = 524288;
            if (!__instance.isFreeCamera && Physics.Raycast(ray, out var hitInfo, 5f, layerMask))
            {
                EnemyAICollisionDetect hitEnemy = hitInfo.collider.gameObject.GetComponent<EnemyAICollisionDetect>();
                MaskedPlayerEnemy masked = null;
                if (hitEnemy)
                {
                    masked = hitEnemy.mainScript.gameObject.GetComponent<MaskedPlayerEnemy>();
                }
                //if (masked != null)
                //{
                //    MaskedNamePatch.ToggleName(masked, true);
                //}
            }
        }
    }
}
