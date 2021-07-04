using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
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
            foreach (var lanceLoadoutSlot in ___loadoutSlots)
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

            foreach (LanceLoadoutSlot lanceLoadoutSlot in ___loadoutSlots)
            {
                if (lanceLoadoutSlot.SelectedMech == null) continue; 

                if (lanceLoadoutSlot.SelectedMech.MechDef.MechTags.Any(x => ModInit.modSettings.disallowedUnitTags.Contains(x)))
                {
                    ModInit.modLog.LogTrace(
                        $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} had a verboten unit tag, skipping!");
                    continue;
                }

                if (lanceLoadoutSlot.SelectedMech.ChassisDef.ChassisTags.Any(x => ModInit.modSettings.disallowedUnitTags.Contains(x)))
                {
                    ModInit.modLog.LogTrace(
                        $"{lanceLoadoutSlot.SelectedMech.MechDef.Chassis.VariantName} had a verboten unit tag, skipping!");
                    continue;
                }

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
            string text = __instance.GetCantSaveErrorString().ToString();
            if (!string.IsNullOrEmpty(text)) return;
            string text2 = __instance.GetNonFieldableErrorString().ToString();
            if (!string.IsNullOrEmpty(text2)) return;

            Util.UtilInstance.GetDataManagerMechDefInventories(UnityGameInstance.BattleTechGame.DataManager);

            if (__instance.activeMechDef.MechTags.Any(x => ModInit.modSettings.disallowedUnitTags.Contains(x)))
            {
                ModInit.modLog.LogTrace(
                    $"{__instance.activeMechDef.Chassis.VariantName} had a verboten unit tag, skipping!");
                return;
            }

            if (__instance.activeMechDef.Chassis.ChassisTags.Any(x => ModInit.modSettings.disallowedUnitTags.Contains(x)))
            {
                ModInit.modLog.LogTrace(
                    $"{__instance.activeMechDef.Chassis.VariantName} had a verboten unit tag, skipping!");
                return;
            }

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
                GenericPopupBuilder.Create("MechDef Already Exists!", "An exported MechDef with this same  loadout already exists, ignoring.").AddButton("Okay").CancelOnEscape().AddFader(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill).Render();
                ModInit.modLog.LogMessage(
                    $"{__instance.activeMechDef.Chassis.VariantName} already exists in dejaVuMechs or AllMechs");
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CreateMechPart")]
    public static class Contract_CreateMechPart
    {
        public static bool Prepare() => !Util.detectCS();

        public static void Postfix(Contract __instance, SimGameConstants sc, MechDef m, ref SalvageDef __result)
        {
            var salvageDef = new SalvageDef
            {
                Type = SalvageDef.SalvageType.MECH_PART,
                ComponentType = ComponentType.MechPart,
                Count = 1,
                Weight = sc.Salvage.DefaultMechPartWeight
            };
            var description = m.Description;
            var mechID = m.Chassis.Description.Id.Replace("chassisdef", "mechdef");
            ModInit.modLog.LogMessage(
                $"Vanilla Salvage: Overwriting custom {m.Description.Id} with parent ID. ChassisID: {m.Description.Id}, parent MechID: {mechID}");

            var gotParentMech = __instance.DataManager.MechDefs.TryGet(mechID, out var parenMechDef);
            if (!gotParentMech)
            {
                ModInit.modLog.LogMessage(
                    $"Vanilla Salvage: Couldn't get parent mech with ID {mechID}, something fucked. Reverting to 'custom' mechdef with ID {description.Id} and crossing fingies.");
                var description2 = new DescriptionDef(description.Id,
                    $"{description.Name} {sc.Story.DefaultMechPartName}", description.Details, description.Icon, description.Cost, description.Rarity, description.Purchasable, description.Manufacturer, description.Model, description.UIName);
                salvageDef.Description = description2;
                salvageDef.RewardID = __instance.GenerateRewardUID();
                __result = salvageDef;
                return;
            }
            var description3 = new DescriptionDef(parenMechDef.Description);
            salvageDef.Description = description3;
            salvageDef.RewardID = __instance.GenerateRewardUID();
            __result = salvageDef;
        }
    }
}
