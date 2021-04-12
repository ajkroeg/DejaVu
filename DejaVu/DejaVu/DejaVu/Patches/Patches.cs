using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using DejaVu.Framework;
using Harmony;

namespace DejaVu.Patches
{

    [HarmonyPatch(typeof(Mech), "AddToTeam")]
    public static class Mech_AddToTeam_Patch
    {
        public static void Postfix(Mech __instance, Team team)
        {
            if (__instance.MechDef.Inventory.Any(x =>
                x.Def.ComponentTags.Any(y => ModInit.modSettings.dissallowedComponentTags.Contains(y)))) return;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
            var sim = UnityGameInstance.BattleTechGame.Simulation;
                
            var p = __instance.pilot;
            if (team.IsLocalPlayer && (sim.PilotRoster.Any(x=>x.Callsign == p.Callsign) || p.IsPlayerCharacter))
            {
                Util.UtilInstance.dejaVuMechs.Add(__instance.MechDef);
                ModInit.modLog.LogMessage($"Adding {__instance.MechDef.Description.UIName} to dejaVuMechs");
                return;
            }
        }
    }

    [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInPilotData")]
    public static class FillInPilotData_Patch
    {
        // Token: 0x0600004B RID: 75 RVA: 0x0000CB54 File Offset: 0x0000AD54
        public static void Postfix(AAR_UnitStatusWidget __instance, UnitResult ___UnitData)
        {
            if (___UnitData.pilot.MechsKilled + ___UnitData.pilot.OthersKilled < ModInit.modSettings.killsToSave)
            {
                var mech = ___UnitData.mech;
                Util.UtilInstance.dejaVuMechs.RemoveAll(x =>
                    x.Description.UIName == mech.Description.UIName && x.Description.Id == mech.Description.Id &&
                    x.Description.Name == mech.Description.Name);
                ModInit.modLog.LogMessage($"Mech {mech.Description.UIName} got < {ModInit.modSettings.killsToSave} kills, removing from dejavu.");
            }
        }
    }


        [HarmonyPatch(typeof(SimGameState), "Dehydrate",
        new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
    public static class SGS_Dehydrate_Patch
    {
        public static void Prefix(SimGameState __instance)
        {
            Util.UtilInstance.InitializeAllMechs(__instance.DataManager);
            foreach (var mech in Util.UtilInstance.dejaVuMechs)
            {
                Util.SerializeMech(mech);
            }

            Util.UtilInstance.dejaVuMechs = new List<MechDef>();
            ModInit.modLog.LogMessage($"Resetting Util.UtilInstance.dejaVuMechs");
        }
    }
}
