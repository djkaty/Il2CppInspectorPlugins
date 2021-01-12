/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector.Plugins.Core
{
    // Plugin definition
    public class StringXorPlugin : ICorePlugin, ILoadPipeline
    {
        public string Id => "string-xor";
        public string Name => "Metadata strings XOR decryptor";
        public string Author => "Il2CppInspector";
        public string Version => "2021.1.0";
        public string Description => "Automatic detection and decryption of XOR-encrypted metadata strings";
        public List<IPluginOption> Options => null;

        // Decrypt XOR-encrypted strings in global-metadata.dat
        public void PostProcessMetadata(Metadata metadata, PluginPostProcessMetadataEventInfo data) {

            // To check for encryption, find every single string start position by scanning all of the definitions
            var stringOffsets = metadata.Images.Select(x => x.nameIndex)
                        .Concat(metadata.Assemblies.Select(x => x.aname.nameIndex))
                        .Concat(metadata.Assemblies.Select(x => x.aname.cultureIndex))
                        .Concat(metadata.Assemblies.Select(x => x.aname.hashValueIndex)) // <=24.3
                        .Concat(metadata.Assemblies.Select(x => x.aname.publicKeyIndex))
                        .Concat(metadata.Events.Select(x => x.nameIndex))
                        .Concat(metadata.Fields.Select(x => x.nameIndex))
                        .Concat(metadata.Methods.Select(x => x.nameIndex))
                        .Concat(metadata.Params.Select(x => x.nameIndex))
                        .Concat(metadata.Properties.Select(x => x.nameIndex))
                        .Concat(metadata.Types.Select(x => x.nameIndex))
                        .Concat(metadata.Types.Select(x => x.namespaceIndex))
                        .Concat(metadata.GenericParameters.Select(x => x.nameIndex))
                        .OrderBy(x => x)
                        .Distinct()
                        .ToList();

            // Now confirm that all the keys are present in the string dictionary
            if (metadata.Header.stringCount == 0 || !stringOffsets.Except(metadata.Strings.Keys).Any())
                return;

            // If they aren't, that means one or more of the null terminators wasn't null, indicating potential encryption
            // Only do this if we need to because it's very slow
            PluginServices.For(this).StatusUpdate("Decrypting strings");

            // There may be zero-padding at the end of the last string since counts seem to be word-aligned
            // Find the true location one byte after the final character of the final string
            var endOfStrings = metadata.Header.stringCount;
            while (metadata.ReadByte(metadata.Header.stringOffset + endOfStrings - 1) == 0)
                endOfStrings--;

            // Start again
            metadata.Strings.Clear();
            metadata.Position = metadata.Header.stringOffset;

            // Read in all of the strings as if they are fixed length rather than null-terminated
            foreach (var offset in stringOffsets.Zip(stringOffsets.Skip(1).Append(endOfStrings), (a, b) => (current: a, next: b))) {
                var encryptedString = metadata.ReadBytes(offset.next - offset.current - 1);

                // The null terminator is the XOR key
                var xorKey = metadata.ReadByte();

                var decryptedString = metadata.Encoding.GetString(encryptedString.Select(b => (byte) (b ^ xorKey)).ToArray());
                metadata.Strings.Add(offset.current, decryptedString);
            }

            // Write changes back in case the user wants to save the metadata file
            metadata.Position = metadata.Header.stringOffset;
            foreach (var str in metadata.Strings.OrderBy(s => s.Key))
                metadata.WriteNullTerminatedString(str.Value);
            metadata.Flush();

            data.IsDataModified = true;
            data.IsStreamModified = true;
        }
    }
}
