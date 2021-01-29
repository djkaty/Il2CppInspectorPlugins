/*
    Copyright 2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    FOR EDUCATIONAL PURPOSES ONLY
    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NoisyCowStudios.Bin2Object;
using Il2CppInspector;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.PluginAPI.V100;
using System.Text;

namespace Loader
{
    public class Plugin : IPlugin, ILoadPipeline {
        public string Id => "guigubahuang";
        public string Name => "Tale of Immortal (鬼谷八荒 / Guigubahuang) Loader";
        public string Author => "Katy";
        public string Version => "1.0";
        public string Description => "Enables loading of Tale of Immortal (鬼谷八荒 / Guigubahuang)\nNOTE: Metadata file is guigubahuang_Data/resources.resource.resdata";

        private PluginOptionChoice<string> presetKey = new PluginOptionChoice<string> {
            Name = "key-select",
            Description = "Preset decryption key for selected version",
            Required = true,
            Value = "0.8.1012",
            Choices = new Dictionary<string, string> {
                ["0.8.1010"] = "0.8.1010",
                ["0.8.1012"] = "0.8.1012",
                ["custom"]   = "Other version"
            }
        };

        private PluginOptionText customKey = new PluginOptionText {
            Name = "key",
            Description = "Metadata decryption key",
            Required = false
        };

        public List<IPluginOption> Options => new List<IPluginOption> { presetKey, customKey };

        // Update list of keys here
        private Dictionary<string, string> PresetKeys = new Dictionary<string, string> {
            ["0.8.1010"]                   = "@F_Gs<>_+**-%322asAS*]!%637473794040990947",
            ["0.8.1012"]                   = "@F_Gs<>_+**-%322asAS*]!%637474491508757063",
        };

        public Plugin() {
            customKey.If = () => presetKey.Value == "custom";
        }

        // This executes as soon as the raw global-metadata.dat has been read from storage,
        // before any attempt is made to analyze its contents
        public void PreProcessMetadata(BinaryObjectStream stream, PluginPreProcessMetadataEventInfo info) {

            stream.Position = 21;

            var key = presetKey.Value == "custom"? customKey.Value : PresetKeys[presetKey.Value];

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Game version or decryption key must be specified");

            var sig = stream.ReadBytes(4);
            sig = sig.Select((b, i) => (byte) (b - key[i % key.Length])).ToArray();
            if (BitConverter.ToUInt32(sig) != Il2CppConstants.MetadataSignature)
                return;

            PluginServices.For(this).StatusUpdate("Decrypting metadata");

            // Subtract key bytes from metadata bytes, skipping the first bytes,
            // and cycling through the key bytes repeatedly
            var bytesToSkipAtStart = 21;
            var length = stream.Length;

            var bytes = stream.ToArray();
            var keyBytes = Encoding.ASCII.GetBytes(key);

            for (int index = 0, pos = bytesToSkipAtStart; pos < length; index = (index + 1) % key.Length, pos++)
                bytes[pos] -= keyBytes[index];

            // We replace the loaded global-metadata.dat with the newly decrypted version,
            // allowing Il2CppInspector to analyze it as normal
            stream.Write(0, bytes.Skip(bytesToSkipAtStart).ToArray());
            
            info.IsStreamModified = true;
        }
    }
}
