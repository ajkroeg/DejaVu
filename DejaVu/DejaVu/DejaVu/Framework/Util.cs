using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using static DejaVu.ModInit;
using HBS.Collections;
using Newtonsoft.Json.Linq;
using HBS.Util;

namespace DejaVu.Framework
{
    internal static class ModState
    {
        public static bool runContinueConfirmClickedPost;
    }

    internal class Util
    {

        private static Random random = new Random();

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static Util _instance;

        public List<MechDef> DejaVuMechs;
        public List<MechComponentRef[]> AllMechInventories;
        public List<MechComponentRef[]> DejaVuInventories;
        public static Util UtilInstance
        {
            get
            {
                if (_instance == null) _instance = new Util();
                return _instance;
            }
        }

        internal void Initialize()
        {
            DejaVuMechs = new List<MechDef>();
            AllMechInventories = new List<MechComponentRef[]>();
            DejaVuInventories = new List<MechComponentRef[]>();
        }

        internal void GetDataManagerMechDefInventories(DataManager dm)
        {
            if (AllMechInventories.Count == 0)
            {
                foreach (var t in dm.MechDefs)
                {
                    t.Value.Refresh();
                    AllMechInventories.Add(t.Value.Inventory);
                    ModInit.modLog.LogTrace($"Added MechInventory for {t.Value.Description.UIName} to allMechInventories");
                }
            }
        }
        
        public class MechComponentArrayComparer : IEqualityComparer<MechComponentRef>
        {
            public bool Equals(MechComponentRef x, MechComponentRef y)
            {
                return x?.MountedLocation == y?.MountedLocation && x?.ComponentDefID == y?.ComponentDefID && x?.ComponentDefType == y?.ComponentDefType && x?.HardpointSlot == y?.HardpointSlot && x?.IsFixed == y?.IsFixed;
            }
            public int GetHashCode(MechComponentRef obj)
            {
                return obj.MountedLocation.GetHashCode() * 17
                     + obj.ComponentDefID.GetHashCode() * 17
                     + obj.ComponentDefType.GetHashCode() * 17
                     + obj.HardpointSlot.GetHashCode() * 17
                     + obj.IsFixed.GetHashCode() * 17;
            }
        }

        internal static bool detectCS()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var result = false;
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("CustomSalvage"))
                {
                    result = true;
                }
            }
            return result;
        }

        internal static void SerializeMech(MechDef def)
        {
            var append = $"-{RandomString(2)}";
            Util.UtilInstance.DejaVuInventories.Add(def.Inventory);
            ModInit.modLog.LogMessage($"Added MechInventory for {def.Description.UIName} to DejaVuInventories");
            try
            {
                def.DataManager = null;
                def.SetGuid(null);
                foreach (var component in def.Inventory)
                {
                    component.SetSimGameUID(null);
                    component.SetGuid(null);
                    component.DataManager = null;
                }

                foreach (var component in def.Chassis.FixedEquipment)
                {
                    component.SetSimGameUID(null);
                    component.SetGuid(null);
                    component.DataManager = null;
                }

//                ProcessChassis(def, append); // old, unused chassis processing ; method  in CU_CC_Util, requires CU and CC references - pones CSalv rework should take care of this

// TO DO - Make MechDef reference original ChassisDef (that way don't need to fuck with CustomUnits or CustomComponents).  ALSO patch vanilla Contract.CreateMechPart so SalvageDef pulls base MechDef NOT custom MechDef - finished?

                
                var variantID = def.Chassis.VariantName + append;
//                var chassisID = def.Chassis.Description.Id + append;

                ModInit.modLog.LogMessage($"Processing MechDef sans ChassisDef: {def.Description.Id}");

                var mechID = def.Description.Id + append;
                var newUIName = def.Description.UIName;
                ModInit.modLog.LogMessage($"Added {append} to mechdefID: {mechID} and UIName: {newUIName}");

                if (string.IsNullOrEmpty(newUIName))
                {
                    newUIName = variantID;
                }
                else
                {
                    newUIName = def.Description.UIName + append;
                }

                var mechdefInventoryList = new List<MechComponentRef>(def.Inventory.ToList());
                foreach (var fixedcomponent in def.Chassis.FixedEquipment)
                {
                    mechdefInventoryList.RemoveAll(x => x.ComponentDefID == fixedcomponent.ComponentDefID && x.MountedLocation == fixedcomponent.MountedLocation);
                }

                var mechdefInventory = mechdefInventoryList.ToArray();

                var mechdefInventoryJSON = JSONSerializationUtility.ToJSON(mechdefInventory);
                var mechdefInventoryJA = JArray.Parse(mechdefInventoryJSON);

                var jsonMechDefString = def.ToJSON();
                var jsonMechDef = JObject.Parse(jsonMechDefString);

                if (modSettings.clearMechTags)
                {
                    var MechTags = new TagSet(modSettings.customMechTags).ToJSON();
                    var MechTagsJO = JObject.Parse(MechTags);
                    jsonMechDef["MechTags"] = MechTagsJO;
                }

                jsonMechDef["Chassis"].Parent.Remove();
//                jsonMechDef["ChassisID"] = chassisID;
                jsonMechDef["Description"]["UIName"] = newUIName;
                jsonMechDef["Description"]["Id"] = mechID;
                jsonMechDef["inventory"] = mechdefInventoryJA;
                ModInit.modLog.LogMessage($"Set def.ChassisID to {jsonMechDef["ChassisID"]}, def.Description.Id to {jsonMechDef["Description"]["Id"]} and def.Description.UIName to {jsonMechDef["Description"]["UIName"]}");

                var jsonMechDefJSON = jsonMechDef.ToString();
                string mechPath = Path.Combine(modDir, "mech", $"{jsonMechDef["Description"]["Id"]}.json");
                string mechDir = Path.Combine(modDir, "mech");

                File.Delete(mechPath);

                Directory.CreateDirectory(mechDir);
                using (StreamWriter writer = new StreamWriter(mechPath, false))
                {
                    writer.Write(jsonMechDefJSON);
                    writer.Flush();
                }
                ModInit.modLog.LogMessage($"Serialized {jsonMechDef["Description"]["Id"]} mechDef to .json");
            }
            catch (Exception e)
            {
                modLog?.LogException(e);
            }
        }
    }
}
