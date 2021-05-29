using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using CustomComponents;
using CustomUnits;
using static DejaVu.ModInit;
using HBS.Collections;
using HBS.Util;
using Newtonsoft.Json.Linq;

namespace DejaVu.Framework
{
    internal class CU_CC_Util
    {
        internal static void ProcessChassis(MechDef def, string append)
        {
            ModInit.modLog.LogMessage($"Processing ChassisDef: {def.Chassis.Description.Id}");

                var customParts = new UnitCustomInfo();
                if (VehicleCustomInfoHelper.vehicleChasissInfosDb.ContainsKey(def.ChassisID))
                {
                    customParts = VehicleCustomInfoHelper.vehicleChasissInfosDb[def.ChassisID];
                }

                var chassisCustoms = Database.GetCustoms<ICustom>(def.Chassis).ToList();
                var chassisCustomsDict = new Dictionary<string, ICustom>();
                for (int i = 0; i < chassisCustoms.ToList().Count; i++)
                {
                    chassisCustomsDict.Add(chassisCustoms[i].GetType().Name, chassisCustoms[i]);
                    ModInit.modLog.LogMessage($"Added {chassisCustoms[i].GetType().Name} to chassisCustomsDict");
                }

                var customPartsJS = JSONSerializationUtility.ToJSON(customParts);
                var chassisCustomsJS = JSONSerializationUtility.ToJSON(chassisCustomsDict);


                var variantID = def.Chassis.VariantName + append;
                var chassisID = def.Chassis.Description.Id + append;
                var chassisUIName = def.Chassis.Description.UIName + append;

                var jsonChassisDefString = def.Chassis.ToJSON();
                var jsonChassisDef = JObject.Parse(jsonChassisDefString);

                var chassisCustomsJA = JArray.Parse(chassisCustomsJS);
                var customPartsJO = JObject.Parse(customPartsJS);
                
                jsonChassisDef.Add("Custom", chassisCustomsJA);
                jsonChassisDef.Add("CustomParts", customPartsJO);

                if (modSettings.clearMechTags)
                {
                    var ChassisTags = new TagSet(modSettings.customChassisTags).ToJSON();
                    var ChassisTagsJO = JObject.Parse(ChassisTags);
                    jsonChassisDef["ChassisTags"] = ChassisTagsJO;
                }

                ModInit.modLog.LogMessage($"Added {append} to chassisID: {chassisID}, variantName: {variantID}, and UIName: {chassisUIName}");
                jsonChassisDef["VariantName"] = variantID;
                jsonChassisDef["Description"]["Id"] = chassisID;
                jsonChassisDef["Description"]["UIName"] = chassisUIName;
                ModInit.modLog.LogMessage($"Set def.VariantName to {jsonChassisDef["VariantName"]}, def.Chassis.Description.Id to {jsonChassisDef["Description"]["Id"]} and def.Chassis.Description.UIName to {jsonChassisDef["Description"]["UIName"]}");

                var jsonChassisDefJSON = jsonChassisDef.ToString();

                string chassisPath = Path.Combine(modDir, "chassis", $"{jsonChassisDef["Description"]["Id"]}.json");
                string chassicDir = Path.Combine(modDir, "chassis");
                Directory.CreateDirectory(chassicDir);

                using (StreamWriter writer = new StreamWriter(chassisPath, false))
                {
                    writer.Write(jsonChassisDefJSON);
                    writer.Flush();
                }
                ModInit.modLog.LogMessage($"Serialized {jsonChassisDef["Description"]["Id"]} chassisDef to .json");
        }
    }
}
