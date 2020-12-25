/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

/*
 * This is an example plugin for Il2CppInspector
 * 
 * This plugin shows how to set up a project and create a basic plugin object
 * The example code performs 'ROT' decryption on all of the string literals in an IL2CPP application
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInspector;
using Il2CppInspector.PluginAPI.V100;

namespace Example
{
    // Define your plugin class, implementing IPlugin plus interfaces for any hooks you wish to use
    public class ExamplePlugin : IPlugin, ILoadPipeline
    {
        // The ID that will be used to load the plugin from the command-line, ie. --plugins "string-rot"
        public string Id => "string-rot";

        // The full name of the plugin
        public string Name => "String literal ROT decryptor";

        // The nickname of the plugin author
        public string Author => "Katy"; 

        // The version of the plugin. Always use version numbers in increasing lexical order when you create a new version.
        public string Version => "1.0";

        // A description of what the plugin does
        public string Description => "Performs ROT decryption on all of the string literals in an IL2CPP binary";

        // Our options
        // An option is created via the PluginOption classes in Il2CppInspector/Plugins/API/Vxxx/IPluginOption.cs

        // The number of characters to rotate by. On the command line, specified as "--key x"
        private PluginOptionNumber<int> rotKey = new PluginOptionNumber<int> { Name = "key", Description = "Number of characters to rotate by", Value = 1 };

        // Let Il2CppInspector know about all of the options we have created
        public List<IPluginOption> Options => new List<IPluginOption> { rotKey };

        // This implements IPostProcessMetadata
        // This hook is executed after global-metadata.dat is fully loaded
        public void PostProcessMetadata(Metadata metadata, PluginPostProcessMetadataEventInfo info) {
            // This displays a progress update for our plugin in the CLI or GUI
            PluginServices.For(this).StatusUpdate("Decrypting strings");

            // Go through every string literal (string[] metadata.StringLiterals) and ROT each string
            for (var i = 0; i < metadata.StringLiterals.Length; i++)
                metadata.StringLiterals[i] = string.Join("", metadata.StringLiterals[i].Select(x => (char) (x >= 'a' && x <= 'z' ? (x - 'a' + rotKey.Value) % 26 + 'a' : x)));

            // Report back that we modified the metadata
            // Note: we do not set info.FullyProcessed in order to allow other plugins to do further processing
            info.IsDataModified = true;
        }
    }
}
