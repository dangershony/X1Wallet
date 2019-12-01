using System;
using System.IO;
using Obsidian.Features.X1Wallet.Tools;

namespace Obsidian.Features.X1Wallet.NodeUI
{
    public sealed class NodeRunnerSettings
    {
        const string RunnerDir = "X1Runner";
        static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RunnerDir);
        static readonly string SettingsFile = Path.Combine(SettingsFolder, "runnersettings.json");

        static readonly string DefaultUiPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs",
                "x1_wallet", "X1.exe");

        public string Arguments { get; set; }

        public string UiPath { get; set; }

        public static NodeRunnerSettings LoadSettings()
        {

            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);

            if (!File.Exists(SettingsFile))
            {
                NodeRunnerSettings defaults = CreateDefaultSettings();
                SaveSettings(defaults);
            }

            try
            {
                var file = File.ReadAllText(SettingsFile);
                return Serializer.Deserialize<NodeRunnerSettings>(file);
            }
            catch (Exception)
            {
                File.Delete(SettingsFile);
                NodeRunnerSettings defaults = CreateDefaultSettings();
                SaveSettings(defaults);
                return defaults;
            }
        }

        public static NodeRunnerSettings SaveSettings(NodeRunnerSettings settings)
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);

            if (settings.UiPath != null)
                settings.UiPath = settings.UiPath.Trim();
            else settings.UiPath = "";

            if (settings.Arguments != null)
                settings.Arguments = settings.Arguments.Trim();
            else settings.Arguments = "";

            var serialized = Serializer.Serialize(settings);
            File.WriteAllText(SettingsFile, serialized);
            return settings;
        }

        public static NodeRunnerSettings ResetSettings()
        {
            return SaveSettings(CreateDefaultSettings());
        }

        static NodeRunnerSettings CreateDefaultSettings()
        {
            var defaults = new NodeRunnerSettings
            {
                Arguments = "",
                UiPath = DefaultUiPath
            };
            return defaults;
        }
    }
}
