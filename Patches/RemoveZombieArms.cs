using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaskedEnemyRework.Patches
{
    internal class RemoveZombieArms
    {
        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetHandsOutClientRpc))]
        [HarmonyPrefix]
        private static void RemoveArms(ref bool setOut)
        {
            if (Plugin.cfg.RemoveZombieArms == true)
            {
                setOut = false;

            }
        }
    }
}
