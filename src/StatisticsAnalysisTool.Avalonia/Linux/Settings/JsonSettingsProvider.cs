// Settings.Default is an ApplicationSettingsBase. Its stock LocalFileSettingsProvider cannot write on Linux
// (no ClientConfigurationHost stream, Upgrade() throws), so Settings is given this JSON-backed provider through a
// partial declaration (the linked Settings.Designer.cs stays untouched). Only user-scoped values that differ from
// their declared default are stored, as strings, in AppDataPaths.UserSettingsFile. I/O failures while reading
// or writing are logged and never thrown.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text.Json;
using Serilog;
using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Properties
{
    [SettingsProvider(typeof(StatisticsAnalysisTool.Linux.Settings.JsonSettingsProvider))]
    internal sealed partial class Settings
    {
    }
}

namespace StatisticsAnalysisTool.Linux.Settings
{
    internal sealed class JsonSettingsProvider : SettingsProvider
    {
        private static readonly object FileLock = new();

        public override string ApplicationName
        {
            get => "StatisticsAnalysisTool";
            set { }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(string.IsNullOrEmpty(name) ? nameof(JsonSettingsProvider) : name, config);
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            var stored = Read();
            var values = new SettingsPropertyValueCollection();

            foreach (SettingsProperty property in properties)
            {
                var value = new SettingsPropertyValue(property) { IsDirty = false };
                if (IsUserScoped(property) && stored.TryGetValue(property.Name, out var serialized))
                {
                    value.SerializedValue = serialized;
                    value.Deserialized = false;
                }

                values.Add(value);
            }

            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            lock (FileLock)
            {
                var stored = Read();
                foreach (SettingsPropertyValue value in values)
                {
                    if (!IsUserScoped(value.Property) || value.SerializedValue is not string serialized)
                    {
                        continue;
                    }

                    stored[value.Name] = serialized;
                }

                var file = AppDataPaths.UserSettingsFile;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                    var temp = file + ".tmp";
                    File.WriteAllText(temp, JsonSerializer.Serialize(stored, new JsonSerializerOptions { WriteIndented = true }));
                    File.Move(temp, file, true);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Log.Warning(e, "Could not write {file}, the setting changes are not persisted", file);
                }
            }
        }

        private static bool IsUserScoped(SettingsProperty property)
        {
            return property.Attributes[typeof(UserScopedSettingAttribute)] is UserScopedSettingAttribute;
        }

        private static Dictionary<string, string> Read()
        {
            lock (FileLock)
            {
                try
                {
                    var file = AppDataPaths.UserSettingsFile;
                    if (File.Exists(file))
                    {
                        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? new Dictionary<string, string>();
                    }
                }
                catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
                {
                    Log.Warning(e, "Could not read {file}, using setting defaults", AppDataPaths.UserSettingsFile);
                }

                return new Dictionary<string, string>();
            }
        }
    }
}
