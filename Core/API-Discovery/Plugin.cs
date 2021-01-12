/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector.Plugins.Core
{
    // Plugin definition
    public class APIDiscoveryPlugin : ICorePlugin, ILoadPipeline
    {
        public string Id => "api-discovery";
        public string Name => "IL2CPP API Discovery";
        public string Author => "Il2CppInspector";
        public string Version => "2021.1.0";
        public string Description => "Automatic detection and decryption of encrypted IL2CPP API export names";
        public List<IPluginOption> Options => null;

        // Handle ROT name encryption found in some binaries
        public void PreProcessBinary(Il2CppBinary binary, PluginPreProcessBinaryEventInfo data) {
            // Get all exports
            var exports = binary.Image.GetExports()?.ToList();
            if (exports == null)
                return;

            // Try every ROT possibility (except 0 - these will already be added to APIExports)
            var exportRgx = new Regex(@"^_+");

            for (var rotKey = 1; rotKey <= 25; rotKey++) {
                var possibleExports = exports.Select(e => new {
                    Name = string.Join("", e.Name.Select(x => (char) (x >= 'a' && x <= 'z'? (x - 'a' + rotKey) % 26 + 'a' : x))),
                    VirtualAddress = e.VirtualAddress
                }).ToList();

                var foundExports = possibleExports
                    .Where(e => (e.Name.StartsWith("il2cpp_") || e.Name.StartsWith("_il2cpp_") || e.Name.StartsWith("__il2cpp_"))
                        && !e.Name.Contains("il2cpp_z_"))
                    .Select(e => e);

                if (foundExports.Any() && !data.IsDataModified) {
                    PluginServices.For(this).StatusUpdate("Decrypting API export names");
                    data.IsDataModified = true;
                }

                foreach (var export in foundExports)
                    if (binary.Image.TryMapVATR(export.VirtualAddress, out _))
                        binary.APIExports.Add(exportRgx.Replace(export.Name, ""), export.VirtualAddress);
            }
        }
    }
}
