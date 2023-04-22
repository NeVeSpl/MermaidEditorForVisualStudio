using System;
using System.IO;
using System.Text.Json;


namespace MermaidEditor.Configuration
{
    internal static class ConfigurationManager
    {
        private static Configuration configuration = new Configuration();
        public static readonly string UserDataPath;
        public static readonly string ConfigurationFilePath;


        public static Configuration Configuration
        {
            get => configuration;
            set => configuration = value;
        }


        static ConfigurationManager()
        {
              UserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mermaid.EditorForVisualStudio");
              ConfigurationFilePath = Path.Combine(UserDataPath, "Configuration.json");
        }


        public static void Load()
        {
            if (File.Exists(ConfigurationFilePath))
            {
                string jsonString = File.ReadAllText(ConfigurationFilePath);
                configuration = JsonSerializer.Deserialize<Configuration>(jsonString);
            }
        }
        public static void Save()
        {
            Directory.CreateDirectory(UserDataPath);
            string jsonString = JsonSerializer.Serialize(configuration);
            File.WriteAllText(ConfigurationFilePath, jsonString);
        }
    }
}