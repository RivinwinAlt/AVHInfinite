using System;
using System.Configuration;

namespace AVHInfiniteMod
{
    public class ModConfig
    {
        private bool initialized;
        private Configuration configFile;
        private KeyValueConfigurationCollection appSettings;
        private ModConfig() { }

        // Singleton access through Config.Instance
        private static ModConfig instance = null;
        public static ModConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModConfig();
                }
                return instance;
            }
        }

        private bool Save()
        {
            if (!CheckValid()) return false;
            try
            {
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool CheckValid()
        {
            if (configFile == null) configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configFile == null) return false;

            if (appSettings == null) appSettings = configFile.AppSettings.Settings;
            return initialized = appSettings != null;
        }

        public bool CheckValid(string key)
        {
            if (configFile == null) configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configFile == null) return false;

            if (appSettings == null) appSettings = configFile.AppSettings.Settings;
            initialized = appSettings != null;
            if (!initialized) return false;

            return appSettings[key] != null;
        }


        // READ Functions
        public bool Read(string key, out string value)
        {
            value = null;
            if (!CheckValid(key)) return false;
            value = appSettings[key].Value;
            return true;
        }

        public bool Read(string key, out bool value)
        {
            value = false;
            if (!CheckValid(key)) return false;
            return bool.TryParse(appSettings[key].Value, out value);
        }

        public bool Read(string key, out int value)
        {
            value = 0;
            if (!CheckValid(key)) return false;
            return Int32.TryParse(appSettings[key].Value, out value);
        }

        public bool Read(string key, out float value)
        {
            value = 0.0f;
            if (!CheckValid(key)) return false;
            return float.TryParse(appSettings[key].Value, out value);
        }

        public bool Read(string key, out double value)
        {
            value = 0.0d;
            if (!CheckValid(key)) return false;
            return double.TryParse(appSettings[key].Value, out value);
        }


        // WRITE Functions
        public bool Write(string key, string value)
        {
            if (CheckValid(key))
            {
                appSettings[key].Value = value;
                return Save();
            }
            else if (initialized)
            {
                appSettings.Add(key, value);
                return Save();
            }
            return false;
        }

        public bool Write(string key, bool value)
        {
            return Write(key, value.ToString());
        }

        public bool Write(string key, int value)
        {
            return Write(key, value.ToString());
        }

        public bool Write(string key, float value)
        {
            return Write(key, value.ToString());
        }

        public bool Write(string key, double value)
        {
            return Write(key, value.ToString());
        }
    }
}
