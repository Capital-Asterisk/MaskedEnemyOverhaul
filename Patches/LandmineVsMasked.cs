using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;

namespace MaskedEnemyRework.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandmineVsMasked
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        static void OnTriggerEnter(Collider other, Landmine __instance, ref bool ___hasExploded, ref float ___pressMineDebounceTimer)
        {
            if (___hasExploded || ___pressMineDebounceTimer > 0.0f)
            {
                return;
            }

            if (other.CompareTag("Player") && other.name.StartsWith("Masked"))
            {
                ___pressMineDebounceTimer = 0.5f;
                __instance.PressMineServerRpc();
            }
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPostfix]
        static void OnTriggerExit(Collider other, Landmine __instance, ref bool ___hasExploded, ref bool ___mineActivated)
        {
            if (___hasExploded || !___mineActivated)
            {
                return;
            }

            if (other.CompareTag("Player") && other.name.StartsWith("Masked"))
            {
                typeof(Landmine).GetMethod("TriggerMineOnLocalClientByExiting", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            }
        }
    }
}
