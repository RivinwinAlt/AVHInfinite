using MelonLoader;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace AVHInfiniteMod
{
    public class ModConfig
    {
        public string modFolder, configFilePath;

        public const string defaultSettings =
            "AVH Infinite Rounds Mod by Rivinwin\n" +
            "Mod Version: 1.0\n" +
            "Config Version: 1.0\n";

        public ModConfig(string fileName)
        {
            modFolder = $"{Environment.CurrentDirectory}\\Mods\\{fileName}";
            configFilePath = modFolder + "\\settings.txt";

            if (!Directory.Exists(modFolder)) Directory.CreateDirectory("Mods\\" + fileName);

            if (!File.Exists(configFilePath))  File.WriteAllText(configFilePath, defaultSettings);
            if (!File.Exists(modFolder + "\\infiniterounds.assets")) File.WriteAllBytes(modFolder + "\\infiniterounds.assets", AVHInfiniteMod.Properties.Resources.infiniterounds);
        }

        private int ReadInt(string key)
        {
            int value = -1;

            foreach (string line in File.ReadLines(configFilePath))
            {
                if (line.Contains(key))
                {
                    if (!int.TryParse(line.Substring(line.IndexOf("=") + 1).Trim(), out value))
                    {
                        //config value is corrupted, throw error
                    }
                }
            }

            return value;
        }

        private float ReadFloat(string key)
        {
            float value = -1;

            foreach (string line in File.ReadLines(configFilePath))
            {
                if (line.Contains(key))
                {
                    if (!float.TryParse(line.Substring(line.IndexOf("=") + 1).Trim(), out value))
                    {
                        //config value is corrupted, throw error
                    }
                }
            }

            return value;
        }
    }
}