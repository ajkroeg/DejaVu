﻿using System;
using System.Reflection;
using System.Collections.Generic;
using DejaVu.Framework;
using Newtonsoft.Json;

namespace DejaVu
{

    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;

        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.DejaVu";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            
            try
            {
                ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception ex)
            {
                ModInit.modLog?.LogException(ex);
                ModInit.modSettings = new Settings();
            }
            modLog = new Logger(modDir, "DejaVu", modSettings.enableLogging, modSettings.trace);
            //HarmonyInstance.DEBUG = true;
            ModInit.modLog.LogMessage($"Initializing {HarmonyPackage} - Version {typeof(Settings).Assembly.GetName().Version}");
            Util.UtilInstance.Initialize();
            //var harmony = HarmonyInstance.Create(HarmonyPackage);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }
    }

    class Settings
    {
        public bool enableLogging = true;
        public bool trace = true;
        public int killsToSave = 0;
        public bool enableMechBayExport = true;
        public List<string> disallowedComponentTags = new List<string>();
        public List<string> disallowedUnitTags = new List<string>();
        public bool clearMechTags = false;
        public List<string> customMechTags = new List<string>();
    }
}