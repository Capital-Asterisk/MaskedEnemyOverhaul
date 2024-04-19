using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using HarmonyLib.Tools;
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
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedVisualRework
    {
        private static IEnumerator coroutine;

        [HarmonyPatch("Start")]
        [HarmonyBefore(new string[] { "AdvancedCompany" })]
        [HarmonyPostfix]
        static void ReformVisuals(ref MaskedPlayerEnemy __instance)
        {
            ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            if (!Plugin.cfg.DontTouchMimickingPlayer)
            {
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

                if(__instance.mimickingPlayer == null)
                {
                    __instance.mimickingPlayer = playerObjects[randomPlayerIndex];
                }

                // replace player models with ones found on active Clients in level
                __instance.SetSuit(__instance.mimickingPlayer.currentSuitID);
            }

            // remove mask
            if (Plugin.cfg.RemoveMasks || Plugin.cfg.RevealMasks)
            {
                __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskComedy").gameObject.SetActive(false);
                __instance.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004/HeadMaskTragedy").gameObject.SetActive(false);
            }

            if (Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany") && !Chainloader.PluginInfos.ContainsKey("com.potatoepet.AdvancedCompany"))
            {
                MoreCompanyPatch.ApplyCosmetics(__instance);
            }
            //if (Plugin.ShowMaskedNames)
            //    MaskedNamePatch.SetNameBillboard(__instance);
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

        [HarmonyPatch("SetEnemyOutside")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.LowerThanNormal)]
        private static void HideCosmeticsIfMarked(ref MaskedPlayerEnemy __instance)
        {
            if (Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany") && !Chainloader.PluginInfos.ContainsKey("com.potatoepet.AdvancedCompany"))
            {
                MoreCompanyPatch.ApplyCosmetics(__instance);
            }
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

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void UpdateMaskName(ref MaskedPlayerEnemy __instance)
        {
            //if (Plugin.ShowMaskedNames)
            //    MaskedNamePatch.UpdateNameBillboard(__instance);
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
