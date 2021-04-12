using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using Harmony;
using Newtonsoft.Json;
using static DejaVu.ModInit;

namespace DejaVu.Framework
{
    class Util
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        internal static Util _instance;
        public List<MechDef> dejaVuMechs;

//        public List<MechDef> allMechs;
        public List<MechComponentRef[]> allMechInventories;

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
            dejaVuMechs = new List<MechDef>();
//            allMechs = new List<MechDef>();
            allMechInventories = new List<MechComponentRef[]>();
        }

        internal void InitializeAllMechs(DataManager dm)
        {
            if (allMechInventories.Count == 0)
            {
                foreach (var t in dm.MechDefs)
                {
//                    allMechs.Add(t.Value);
//                    ModInit.modLog.LogTrace($"Added MechDef: {t.Value.Description.UIName} to allMechs");
                    allMechInventories.Add(t.Value.Inventory);
                    ModInit.modLog.LogTrace($"Added MechInventory for {t.Value.Description.UIName} to allMechInventories");
                }
            }
        }


        public class MechComponentArrayComparer : IEqualityComparer<MechComponentRef>
        {
            public bool Equals(MechComponentRef x, MechComponentRef y)
            {

                    if (x.MountedLocation != y.MountedLocation || x.ComponentDefID != y.ComponentDefID || x.ComponentDefType != y.ComponentDefType || x.HardpointSlot != y.HardpointSlot || x.IsFixed != y.IsFixed)
                    {
                        return false;
                    }
                    return true;
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

        internal static void SerializeMech(MechDef def)
        {
            var inventory = def.Inventory;
            var comparer = new MechComponentArrayComparer();
            var foundmatches = false;
            inventory = inventory.OrderBy(i => i.ComponentDefID).ToArray();
            foreach (var mechinventory in new List<MechComponentRef[]>(UtilInstance.allMechInventories))
            {
                if (inventory.Length == mechinventory.Length)
                {
                    ModInit.modLog.LogTrace($"{def.Description.UIName} inventory length is same as reference mechInventory");
                    var orderedinventory = mechinventory.OrderBy(i => i.ComponentDefID).ToArray();
                    var results = mechinventory.Except(inventory, comparer);
                    if (!results.Any()) foundmatches = true;
                }
            }

            if (!foundmatches)
            {
                Util.UtilInstance.allMechInventories.Add(def.Inventory);
                var append = $"-{RandomString(2)}";
                try
                {
                    var chassisID = def.Chassis.Description.Id + append;
                    var variantID = def.Chassis.VariantName + append;
                    var chassisUIName = def.Chassis.Description.UIName + append;
                    ModInit.modLog.LogMessage($"Added {append} to chassisID: {chassisID} and variantName: {variantID}");
                    Traverse.Create(def).Property("Chassis").Property("VariantName").SetValue(variantID);
                    Traverse.Create(def).Property("Chassis").Property("Description").Property("Id").SetValue(chassisID);
                    Traverse.Create(def).Property("Chassis").Property("Description").Property("UIName").SetValue(chassisUIName);
                    ModInit.modLog.LogMessage($"Set def.Chassic.Description.Id to {def.Chassis.Description.Id} and variantname to {def.Chassis.VariantName}");

                    
                    string chassisPath = Path.Combine(modDir, "chassis", $"{def.Chassis.Description.Id}.json");
                    string chassicDir = Path.Combine(modDir, "chassis");
                    Directory.CreateDirectory(chassicDir);
                    var jsonChassisDef = def.Chassis.ToJSON();
                    using (StreamWriter writer = new StreamWriter(chassisPath, false))
                    {
                        writer.Write(jsonChassisDef);
                        writer.Flush();
                    }

                    ModInit.modLog.LogMessage($"Serialized {def.Description.UIName} to .json");

                    var mechID = def.Description.Id + append;
                    var newUIName = def.Description.UIName;
                    if (string.IsNullOrEmpty(newUIName))
                    {
                        newUIName = variantID;
                    }
                    else
                    {
                        newUIName = def.Description.UIName + append;
                    }

                    ModInit.modLog.LogMessage($"Added {append} to mechdefID: {mechID} and UIName: {newUIName}");
                    Traverse.Create(def).Property("ChassisID").SetValue(chassisID);
                    Traverse.Create(def).Property("Description").Property("UIName").SetValue(newUIName);
                    Traverse.Create(def).Property("Description").Property("Id").SetValue(mechID);

                    var mechdefInventoryList = def.Inventory.ToList();
                    foreach (var fixedcomponent in def.Chassis.FixedEquipment)
                    {
                        mechdefInventoryList.RemoveAll(x => x.ComponentDefID == fixedcomponent.ComponentDefID && x.MountedLocation == fixedcomponent.MountedLocation);
                    }

                    var mechdefInventory = mechdefInventoryList.ToArray();
                    def.Chassis = null;

                    Traverse.Create(def).Field("inventory").SetValue(mechdefInventory);

                    foreach (var component in def.Inventory)
                    {
                        component.SetSimGameUID(null);
                        component.SetGuid(null);
                    }
                    ModInit.modLog.LogMessage($"Set def.Description.Id to {def.Description.Id} and def.Description.UIName to {def.Description.UIName}");
                    string mechPath = Path.Combine(modDir, "mech", $"{def.Description.Id}.json");
                    string mechDir = Path.Combine(modDir, "mech");

                    Directory.CreateDirectory(mechDir);
                    var jsonDef = def.ToJSON();
                    using (StreamWriter writer = new StreamWriter(mechPath, false))
                    {
                        writer.Write(jsonDef);
                        writer.Flush();
                    }
                }
                catch (Exception e)
                {
                    modLog?.LogException(e);
                }
            }
        }
    }
}
