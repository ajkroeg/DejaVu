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
using HBS;
using Localize;
using UnityEngine;

namespace DejaVu.Patches
{

    [HarmonyPatch(typeof(LanceConfiguratorPanel), "ContinueConfirmClicked")]
    public static class LCP_ContinueConfirmClicked
    {
        public static bool Prepare() => ModInit.modSettings.killsToSave >= 0;
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(LanceConfiguratorPanel __instance, LanceLoadoutSlot[] ___loadoutSlots, bool ___mechWarningsCheckResolved)
        {
            ModState.runContinueConfirmClickedPost = false;
            if (___mechWarningsCheckResolved)
            {
                ModState.runContinueConfirmClickedPost = true;
            }
        }

        public static void Postfix(LanceConfiguratorPanel __instance, LanceLoadoutSlot[] ___loadoutSlots)
        {
            bool flag = false;
            foreach (LanceLoadoutSlot lanceLoadoutSlot in ___loadoutSlots)
            {
                if (lanceLoadoutSlot.SelectedMech != null)
                {
                    List<Text> mechFieldableWarnings =
                        MechValidationRules.GetMechFieldableWarnings(__instance.dataManager,
                            lanceLoadoutSlot.SelectedMech.MechDef);
                    if (mechFieldableWarnings.Count > 0)
                    {
                        flag = true;
                    }
                }
            }

            if (!ModState.runContinueConfirmClickedPost && flag)
            {
                ModInit.modLog.LogTrace(
                    $"!runContinueConfirmClickedPost: {!ModState.runContinueConfirmClickedPost} && had mechFieldableWarnings. ContinueConfirmClicked should run again!");
                return;
            }
            
            Util.UtilInstance.GetDataManagerMechDefInventories(UnityGameInstance.BattleTechGame.DataManager);
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            foreach (LanceLoadoutSlot lanceLoadoutSlot in ___loadoutSlots)
            {
                if (lanceLoadoutSlot.SelectedMech == null) continue; 
                if (lanceLoadoutSlot.SelectedMech.MechDef.Inventory.Any(x =>
                    x.Def.ComponentTags.Any(y => ModInit.modSettings.disallowedComponentTags.Contains(y))))
                {
                    ModInit.modLog.LogTrace(
                        $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} had a verboten component, skipping!");
                    continue;
                }

                var inventory = lanceLoadoutSlot.SelectedMech.MechDef.Inventory;
                var comparer = new Util.MechComponentArrayComparer();
                var foundmatches = false;
                inventory = inventory.OrderBy(i => i.ComponentDefID).ToArray();
                foreach (var mechinventory in new List<MechComponentRef[]>(Util.UtilInstance.DejaVuInventories))
                {
                    if (inventory.Length == mechinventory.Length)
                    {
                        ModInit.modLog.LogTrace(
                            $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} inventory length is same as reference mechInventory in DejaVuInventories");
                        var orderedinventory = mechinventory.OrderBy(i => i.ComponentDefID).ToArray();
                        var results = orderedinventory.Except(inventory, comparer);
                        if (!results.Any()) foundmatches = true;
                    }
                }

                foreach (var mechinventory in new List<MechComponentRef[]>(Util.UtilInstance.AllMechInventories))
                {
                    if (inventory.Length == mechinventory.Length)
                    {
                        ModInit.modLog.LogTrace(
                            $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} inventory length is same as reference mechInventory in AllMechInventories");
                        var orderedinventory = mechinventory.OrderBy(i => i.ComponentDefID).ToArray();
                        var results = orderedinventory.Except(inventory, comparer);
                        if (!results.Any()) foundmatches = true;
                    }
                }

                if (!foundmatches)
                {
                    var newGUID = Guid.NewGuid().ToString();

                    MechDef mechDef = new MechDef(lanceLoadoutSlot.SelectedMech.MechDef, newGUID);

                    Util.UtilInstance.DejaVuMechs.Add(mechDef);
                    ModInit.modLog.LogMessage($"Adding {mechDef.Chassis.VariantName} to dejaVuMechs");

                }
                else
                {
                    ModInit.modLog.LogMessage(
                        $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} already exists in dejaVuMechs or AllMechs");
                }
            }
        }
    }

    [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInPilotData")]
    public static class FillInPilotData_Patch
    {
        public static bool Prepare() => ModInit.modSettings.killsToSave >= 0;
        public static void Postfix(AAR_UnitStatusWidget __instance, UnitResult ___UnitData)
        {
            if (___UnitData.pilot.MechsKilled + ___UnitData.pilot.OthersKilled < ModInit.modSettings.killsToSave)
            {
                var mech = ___UnitData.mech;
                Util.UtilInstance.DejaVuMechs.RemoveAll(x =>
                    x.Description.UIName == mech.Description.UIName && x.Description.Id == mech.Description.Id &&
                    x.Description.Name == mech.Description.Name);
                ModInit.modLog.LogMessage($"Mech {mech.Chassis.VariantName} got < {ModInit.modSettings.killsToSave} kills, removing from dejavu.");
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    public static class SGS_ResolveCompleteContract_Patch
    {
        public static bool Prepare() => ModInit.modSettings.killsToSave >= 0;
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(SimGameState __instance)
        {
            foreach (var mech in Util.UtilInstance.DejaVuMechs)
            {
                Util.SerializeMech(mech);
            }
        }
    }

    [HarmonyPatch(typeof(MechLabPanel), "OnConfirmClicked")]
    public static class MechLabPanel_OnConfirmClicked
    {
        public static bool Prepare() => ModInit.modSettings.enableMechBayExport;

        public static void Postfix(MechLabPanel __instance, MechLabMechInfoWidget ___mechInfoWidget)
        {
            var hk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!hk) return;

            if (!___mechInfoWidget.IsNameValid()) return;
            string text = __instance.GetCantSaveErrorString().ToString(true);
            if (!string.IsNullOrEmpty(text)) return;
            string text2 = __instance.GetNonFieldableErrorString().ToString(true);
            if (!string.IsNullOrEmpty(text2)) return;

            Util.UtilInstance.GetDataManagerMechDefInventories(UnityGameInstance.BattleTechGame.DataManager);

            if (__instance.activeMechDef.Inventory.Any(x =>
                x.Def.ComponentTags.Any(y => ModInit.modSettings.disallowedComponentTags.Contains(y))))
            {
                ModInit.modLog.LogTrace(
                    $"{__instance.activeMechDef.Chassis.VariantName} had a verboten component, skipping!");
                return;
            }

            var inventory = __instance.activeMechDef.Inventory;
            var comparer = new Util.MechComponentArrayComparer();
            var foundmatches = false;
            inventory = inventory.OrderBy(i => i.ComponentDefID).ToArray();
            foreach (var mechinventory in new List<MechComponentRef[]>(Util.UtilInstance.DejaVuInventories))
            {
                if (inventory.Length == mechinventory.Length)
                {
                    ModInit.modLog.LogTrace(
                        $"{__instance.activeMechDef.Chassis.VariantName} inventory length is same as reference mechInventory in DejaVuInventories");
                    var orderedinventory = mechinventory.OrderBy(i => i.ComponentDefID).ToArray();
                    var results = orderedinventory.Except(inventory, comparer);
                    if (!results.Any()) foundmatches = true;
                }
            }

            foreach (var mechinventory in new List<MechComponentRef[]>(Util.UtilInstance.AllMechInventories))
            {
                if (inventory.Length == mechinventory.Length)
                {
                    ModInit.modLog.LogTrace(
                        $"{__instance.activeMechDef.Chassis.VariantName} inventory length is same as reference mechInventory in AllMechInventories");
                    var orderedinventory = mechinventory.OrderBy(i => i.ComponentDefID).ToArray();
                    var results = orderedinventory.Except(inventory, comparer);
                    if (!results.Any()) foundmatches = true;
                }
            }

            if (!foundmatches)
            {
                var newGUID = Guid.NewGuid().ToString();

                MechDef mechDef = new MechDef(__instance.activeMechDef, newGUID);

                //Util.UtilInstance.DejaVuMechs.Add(mechDef);
                Util.SerializeMech(mechDef);
                ModInit.modLog.LogMessage($"Adding {mechDef.Chassis.VariantName} to dejaVuMechs and Serializing");

            }
            else
            {
                GenericPopupBuilder.Create("MechDef Already Exists!", "An exported MechDef with this same  loadout already exists, ignoring.").AddButton("Okay", null, true, null).CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                ModInit.modLog.LogMessage(
                    $"{__instance.activeMechDef.Chassis.VariantName} already exists in dejaVuMechs or AllMechs");
            }
        }
    }
}
