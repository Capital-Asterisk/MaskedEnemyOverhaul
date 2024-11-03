using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
//using MaskedEnemyRework.External_Classes;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;

namespace MaskedEnemyRework.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class UltraStupidFix
    {
        [HarmonyPatch("EnableEnemyMesh")]
        [HarmonyPrefix]
        private static void StupidFix(ref EnemyAI __instance)
        {
            if (__instance is MaskedPlayerEnemy masked)
            {
                // No idea why but a random null gets added to meshRenderers every now and then when
                // MoreCompany is installed, related to visibility and toggling the ship door.
                masked.skinnedMeshRenderers = masked.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                masked.meshRenderers        = masked.gameObject.GetComponentsInChildren<MeshRenderer>();
            }
        }

    }
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedVisualRework
    {
        private static HashSet<int> instanceFirstUpdateDone = new HashSet<int>();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void RemoveMask(ref MaskedPlayerEnemy __instance)
        {
            if (Plugin.cfg.RemoveMasks || Plugin.cfg.RevealMasks)
            {
                __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskComedy").gameObject.SetActive(false);
                __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskTragedy").gameObject.SetActive(false);
            }
        }


        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void AssignRandomPlayerToMimic(ref MaskedPlayerEnemy __instance)
        {
            bool firstUpdate = instanceFirstUpdateDone.Add(__instance.GetInstanceID());
            if (    ! firstUpdate                       // Already done assigning player
                 || Plugin.cfg.DontTouchMimickingPlayer // Configured not to assign player
                 || __instance.mimickingPlayer != null) // Don't assign random player to transformed players (only spawned outside/vent)
            {
                return;
            }

            ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            PlayerControllerB[] playerObjects = StartOfRound.Instance.allPlayerScripts; // this spawns max amount of players, not total players (not dynamic)
            int playerCount = StartOfRound.Instance.ClientPlayerList.Count;
            if (playerCount == 0)
            {
                playerCount = 1;
                logger.LogError("Player count was zero");
            }

            if(Plugin.PlayerMimicList.Count <= 1 || Plugin.InitialPlayerCount != playerCount) // remakes list if new player joins
            {
                Plugin.InitialPlayerCount = playerCount;
                Random.State stateBeforeITouchedIt = Random.state; // not sure this is necessary, but since this is a global change i do not want to impact map generation. would be very bad :)
                Random.InitState(1234);
                for(int i = 0; i < 50; i++)
                {
                    Plugin.PlayerMimicList.Add(Random.Range(0, playerCount));
                }
                Random.state = stateBeforeITouchedIt;
            }

            // this chooses the player to mimic
            int randomPlayerIndex = Plugin.PlayerMimicList[Plugin.PlayerMimicIndex % 50] % playerCount;
            Plugin.PlayerMimicIndex += 1;

            __instance.mimickingPlayer = playerObjects[randomPlayerIndex];

            // replace player models with ones found on active Clients in level
            __instance.SetSuit(__instance.mimickingPlayer.currentSuitID);

            // MoreCompany hooks into this. Does nothing otherwise.
            // see https://github.com/notnotnotswipez/MoreCompany/blob/e2d2ec49b204c74652ff6e1e0bf3552f6efc11a2/MoreCompany/CosmeticPatches.cs#L54
            __instance.SetEnemyOutside(__instance.isOutside);
        }

        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        private static void OnDestroy(ref MaskedPlayerEnemy __instance)
        {
            instanceFirstUpdateDone.Remove(__instance.GetInstanceID());
        }

        [HarmonyPatch("SetHandsOutClientRpc")]
        [HarmonyPrefix]
        private static void MaskAndArmsReveal(ref bool setOut, ref MaskedPlayerEnemy __instance)
        {
            GameObject mask = __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskComedy").gameObject;
            if (Plugin.cfg.RevealMasks && !mask.activeSelf && __instance.currentBehaviourStateIndex == 1)
            {
                ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
                IEnumerator fadeMaskCoroutine = FadeInAndOut(mask, true, 1f);
                __instance.StartCoroutine(fadeMaskCoroutine);
            }
            
            if (Plugin.cfg.RemoveZombieArms)
            {
                setOut = false;
            }
            //if (Plugin.ShowMaskedNames)
            //    MaskedNamePatch.SetNameBillboard(__instance);
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        private static void HideRevealedMask(ref MaskedPlayerEnemy __instance)
        {
            if(Plugin.cfg.RevealMasks && __instance.targetPlayer == null)
            {
                GameObject mask = __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskComedy").gameObject;

                if (mask.activeSelf)
                {
                    IEnumerator fadeMaskCoroutine = FadeInAndOut(mask, false, 1f);
                    __instance.StartCoroutine(fadeMaskCoroutine);
                }

            }
        }


        static IEnumerator FadeInAndOut(GameObject mask, bool fadeIn, float duration)
        {
            float counter = 0f;
            float startLoc, endLoc;
            mask.SetActive(true);
            if (fadeIn)
            {
                startLoc = 0.095f;
                endLoc =.215f;
            }
            else
            {
                startLoc = .215f;
                endLoc = 0.095f;
            }

            while (counter < duration)
            {
                counter += Time.deltaTime;
                float loc = Mathf.Lerp(startLoc, endLoc, counter / duration);
                mask.transform.localPosition = new UnityEngine.Vector3 (-.009f, .143f, loc);
                yield return null;
            }

            if(!fadeIn)
            {
                mask.SetActive(false);
            }
        }

    }
}
