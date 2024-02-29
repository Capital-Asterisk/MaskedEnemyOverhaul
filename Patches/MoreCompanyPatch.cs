using BepInEx.Bootstrap;
using GameNetcodeStuff;
using HarmonyLib;
//using MaskedEnemyRework.External_Classes;
using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MaskedEnemyRework.Patches
{
    internal class MoreCompanyPatch
    {
        public static void ApplyCosmetics(MaskedPlayerEnemy masked)
        {

            if (!MoreCompany.MainClass.showCosmetics || MoreCompany.MainClass.playerIdsAndCosmetics.Count == 0)
                return;

            Transform cosmeticRoot = masked.transform.Find("ScavengerModel").Find("metarig");
            CosmeticApplication cosmeticApplication = cosmeticRoot.GetComponent<CosmeticApplication>();
            if (cosmeticApplication)
            {
                cosmeticApplication.ClearCosmetics();
                GameObject.Destroy(cosmeticApplication);
                masked.skinnedMeshRenderers = masked.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                masked.meshRenderers = masked.gameObject.GetComponentsInChildren<MeshRenderer>();

            }


            List<string> playerCosmetics = MoreCompany.MainClass.playerIdsAndCosmetics[(int)masked.mimickingPlayer.playerClientId];
            cosmeticApplication = cosmeticRoot.gameObject.AddComponent<CosmeticApplication>();
            foreach (var cosmetic in playerCosmetics)
            {
                cosmeticApplication.ApplyCosmetic(cosmetic, true);
            }

            foreach (var cosmetic in cosmeticApplication.spawnedCosmetics)
            {
                cosmetic.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
            }
        }
    }
}
