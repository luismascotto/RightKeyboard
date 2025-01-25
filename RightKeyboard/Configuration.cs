using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RightKeyboard
{
    public class Configuration
    {
        public Dictionary<IntPtr, Layout> LanguageMappings { get; } = new Dictionary<IntPtr, Layout>();
        public Dictionary<string, IntPtr> LanguageMappingsNamed { get; } = new Dictionary<string, IntPtr>();

        public static Configuration LoadConfiguration(KeyboardDevicesCollection devices)
        {
            var configuration = new Configuration();
            var languageMappings = configuration.LanguageMappings;
            var languageMappingsNamed = configuration.LanguageMappingsNamed;

            string configFilePath = GetConfigFilePath();
            if (File.Exists(configFilePath))
            {
                using (TextReader input = File.OpenText(configFilePath))
                {
                    var layouts = Layout.EnumerateLayouts().ToDictionary(k => k.Identifier, v => v);

                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        string[] parts = line.Split('=');
                        Debug.Assert(parts.Length == 2);

                        string deviceName = parts[0];
                        var layoutId = new IntPtr(int.Parse(parts[1], NumberStyles.HexNumber));

                        if (devices.TryGetByName(deviceName, out var deviceHandle)
                            && layouts.TryGetValue(layoutId, out var layout))
                        {
                            languageMappings.Add(deviceHandle, layout);
                            languageMappingsNamed.Add(deviceName, layoutId);
                        }
                    }
                }
            }

            return configuration;
        }

        public void Save(KeyboardDevicesCollection devices)
        {
            string configFilePath = GetConfigFilePath();
            using (TextWriter output = File.CreateText(configFilePath))
            {
                foreach (var device in devices)
                {
                    if (LanguageMappings.TryGetValue(device.Handle, out var layout))
                    {
                        output.WriteLine("{0}={1:X8}", device.Name, layout.Identifier.ToInt32());
                    }
                }
            }
        }

        private static string GetConfigFilePath()
        {
            string configFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard");
            if (!Directory.Exists(configFileDir))
            {
                Directory.CreateDirectory(configFileDir);
            }

            return Path.Combine(configFileDir, "config.txt");
        }
        private static string GetDebugFilePath()
        {
            string configFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard");
            if (!Directory.Exists(configFileDir))
            {
                Directory.CreateDirectory(configFileDir);
            }

            return Path.Combine(configFileDir, "debug.txt");
        }

        internal void AppendDebugCurrentDevice(IntPtr hCurrentDevice, KeyboardDevicesCollection devices, KeyboardDevicesCollection newDevices)
        {
            string configFilePath = GetDebugFilePath();
            using (TextWriter output = File.AppendText(configFilePath))
            {
                output.WriteLine("---");
                output.WriteLine("Ptr nao encontrado {0}", hCurrentDevice);
                if (devices.Any())
                {
                    output.WriteLine("Mapeados:");

                    foreach (var device in devices)
                    {
                        if (LanguageMappings.TryGetValue(device.Handle, out var layout))
                        {
                            output.WriteLine("Handle:{0}\t\tLayout:{1:X8}\t\tName:{2}", device.Handle, layout.Identifier.ToInt32(), device.Name);
                        }
                    }
                }
                if (newDevices.Any())
                {
                    output.WriteLine();
                    output.WriteLine("Disponíveis:");
                    foreach (var device in newDevices)
                    {
                        output.WriteLine("Handle:{0}\t\tName:{1}", device.Handle, device.Name);
                    }
                }
                output.WriteLine("---");
                output.WriteLine();
            }
        }
    }
}
