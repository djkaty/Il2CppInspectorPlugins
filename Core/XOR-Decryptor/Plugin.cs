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
    public class XorPlugin : ICorePlugin, ILoadPipeline
    {
        public string Id => "xor";
        public string Name => "Binary file XOR decryptor";
        public string Author => "Il2CppInspector";
        public string Version => "2021.1.0";
        public string Description => "Automatic detection and decryption of XOR-encrypted binary files";
        public List<IPluginOption> Options => null;

        private Dictionary<string, Section> sections;
        private IFileFormatStream stream;
        private ElfReader32 elf32;
        private ElfReader64 elf64;

        private bool HasDynamicEntry(Elf dt) => ((object) elf32?.GetDynamicEntry(dt) ?? elf64?.GetDynamicEntry(dt)) != null;

        // Detect and defeat various kinds of XOR encryption
        public void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo data) where T : FileFormatStream<T> {
            if (stream is ElfReader32 stream32)
                elf32 = stream32;
            else if (stream is ElfReader64 stream64)
                elf64 = stream64;
            else
                return;

            PluginServices.For(this).StatusUpdate("Detecting encryption");

            this.stream = stream;
            sections = stream.GetSections().GroupBy(s => s.Name).ToDictionary(s => s.Key, s => s.First());

            if (HasDynamicEntry(Elf.DT_INIT) && sections.ContainsKey(".rodata")) {
                // Use the data section to determine some possible keys
                // If the data section uses striped encryption, bucketing the whole section will not give the correct key
                var roDataBytes = stream.ReadBytes(sections[".rodata"].ImageStart, sections[".rodata"].ImageLength);
                var xorKeyCandidateStriped = roDataBytes.Take(1024).GroupBy(b => b).OrderByDescending(f => f.Count()).First().Key;
                var xorKeyCandidateFull = roDataBytes.GroupBy(b => b).OrderByDescending(f => f.Count()).First().Key;

                // Select test nibbles and values for ARM instructions depending on architecture (ARMv7 / AArch64)
                var testValues = new Dictionary<int, (int, int, int, int)> {
                    [32] = (8, 28, 0x0, 0xE),
                    [64] = (4, 28, 0xE, 0xF)
                };

                var (armNibbleB, armNibbleT, armValueB, armValueT) = testValues[stream.Bits];

                var instructionsToTest = 256;

                // This gives us an idea of whether the code might be encrypted
                var textFirstDWords = stream.ReadArray<uint>(sections[".text"].ImageStart, instructionsToTest);
                var bottom = textFirstDWords.Select(w => (w >> armNibbleB) & 0xF).GroupBy(n => n).OrderByDescending(f => f.Count()).First().Key;
                var top = textFirstDWords.Select(w => w >> armNibbleT).GroupBy(n => n).OrderByDescending(f => f.Count()).First().Key;
                var xorKeyCandidateFromCode = (byte) (((top ^ armValueT) << 4) | (bottom ^ armValueB));

                // If the first part of the data section is encrypted, proceed
                if (xorKeyCandidateStriped != 0x00) {

                    // Some files may use a striped encryption whereby alternate blocks are encrypted and un-encrypted
                    // The first part of each section is always encrypted.

                    // We refer to issue #96 where the code uses striped encryption in 4KB blocks
                    // We perform heuristics for block of size blockSize below
                    const int blockSize = 0x100;
                    const int maxBrokenRun = 2;
                    const int minMultiplierInValid = 6;
                    const int minTotalValidInBucket = 0x10;

                    // Take all of the instructions from the code section starting on a VA block boundary and determine which are valid
                    var startSkip = 0;
                    if (sections[".text"].VirtualStart % blockSize != 0)
                        startSkip = (int) (blockSize - sections[".text"].VirtualStart % blockSize);

                    var insts = stream.ReadArray<uint>(sections[".text"].ImageStart + startSkip, (sections[".text"].ImageLength - startSkip) / 4);
                    var instsValid = insts.Select(i => stream.Bits == 32? isCommonARMv7(i) : isCommonARMv8A(i)).ToList();

                    // Use RLE to produce frequency distribution of number of consecutive valid and invalid instructions,
                    // allowing for maxBrokenRun breaks in valid instructions in a row before considering a run to have ended
                    var freqValid = new SortedDictionary<int, int>();
                    var runLength = 0;
                    var brokenRun = 0;
                    foreach (var i in instsValid) {
                        if (i) {
                            runLength = runLength + brokenRun + 1;
                            brokenRun = 0;
                        } else if (runLength > 0) {
                            brokenRun++;

                            if (brokenRun > maxBrokenRun) {
                                if (freqValid.ContainsKey(runLength))
                                    freqValid[runLength]++;
                                else
                                    freqValid[runLength] = 1;
                                runLength = 0;
                            }
                        }
                    }

                    // Create a histogram of how often each range of valid instruction counts occurred
                    // The uses of 4 refer to the size of an ARM instruction
                    var histValid = freqValid.GroupBy(f => f.Key - (f.Key % (blockSize / 4)))
                                        .Select(f => new {
                                            Key = f.Key * 4,
                                            Value = f.Sum(x => x.Value)
                                        }).ToDictionary(x => x.Key, x => x.Value);

                    // Find first point in the histogram where the number of valid instructions suddenly spikes
                    var stripeSize = (uint) histValid.Zip(histValid.Skip(1), (p,c) => (p,c))
                                        .FirstOrDefault(x => x.c.Value >= x.p.Value * minMultiplierInValid && x.c.Value >= minTotalValidInBucket).c.Key;

                    // Select the key

                    // If more than one key candidates are the same, select the most common candidate
                    var keys = new [] { xorKeyCandidateFromCode, xorKeyCandidateStriped, xorKeyCandidateFull };
                    var bestKey = keys.GroupBy(k => k).OrderByDescending(k => k.Count()).First();
                    var xorKey = bestKey.Key;

                    // Otherwise choose according to striped/full encryption
                    if (bestKey.Count() == 1) {
                        xorKey = keys.OrderByDescending(k => textFirstDWords.Select(w => w ^ (k << 24) ^ (k << 16) ^ (k << 8) ^ k)
                                        .Count(w => stream.Bits == 32 ? isCommonARMv7((uint) w) : isCommonARMv8A((uint) w))).First();
                    }

                    PluginServices.For(this).StatusUpdate($"Decrypting (key: 0x{xorKey:X2}, stripe size: 0x{stripeSize:X4})");

                    xorSection(".text", xorKey, stripeSize);
                    xorSection(".rodata", xorKey, stripeSize);

                    // Notify that stream has been modified
                    data.IsStreamModified = true;
                }
            }

            // Detect more sophisticated packing
            // We have seen several examples (eg. #14 and #26) where most of the file is zeroed
            // and packed data is found in the latter third. So far these files always have zeroed .rodata sections
            if (sections.ContainsKey(".rodata")) {
                var rodataBytes = stream.ReadBytes(sections[".rodata"].ImageStart, sections[".rodata"].ImageLength);
                if (rodataBytes.All(b => b == 0x00))
                    throw new InvalidOperationException("This IL2CPP binary is packed in a way not currently supported by Il2CppInspector and cannot be loaded");
            }
        }

        // https://developer.arm.com/documentation/ddi0406/cb/Application-Level-Architecture/ARM-Instruction-Set-Encoding/ARM-instruction-set-encoding
        private bool isCommonARMv7(uint inst) {
            var cond = inst >> 28; // We'll allow 0x1111 (for BL/BLX), AL, EQ, NE, GE, LT, GT, LE only

            if (cond != 0b1111 && cond != 0b1110 && cond != 0b0000 && cond != 0b0001 && cond != 0b1010 && cond != 0b1011 && cond != 0b1100 && cond != 0b1101)
                return false;

            var op1  = (inst >> 25) & 7;

            // Disallow media instructions
            var op   = (inst >> 4) & 1;
            if (op1 == 0b011 && op == 1)
                return false;

            // Disallow co-processor instructions
            if (op1 == 0b110 || op1 == 0b111)
                return false;

            // Disallow 0b1111 cond except for BL and BLX
            if (cond == 0b1111) {
                var op1_1 = (inst >> 20) & 0b11111111;

                if ((op1_1 >> 5) != 0b101)
                    return false;
            }

            // Disallow MSR and other miscellaneous
            if (op == 1) {
                var op1_1 = (inst >> 20) & 0b11111;
                var op2 = (inst >> 4) & 0b1111;

                if (op1_1 == 0b10010 || op1_1 == 0b10110 || op1_1 == 0b10000 || op1_1 == 0b10100)
                    return false;

                // Disallow synchronization primitives
                if ((op1_1 >> 4) == 1)
                    return false;
            }

            // Probably a common instruction
            return true;
        }

        // https://montcs.bloomu.edu/Information/ARMv8/ARMv8-A_Architecture_Reference_Manual_(Issue_A.a).pdf
        private bool isCommonARMv8A(uint inst) {
            var op = (inst >> 24) & 0b11111;

            // Disallow unexpected, SIMD and FP
            if ((op >> 3) == 0 || (op >> 1) == 0b0111 || (op >> 1) == 0b1111)
                return false;

            // Disallow exception generation and system instructions
            if ((inst >> 24) == 0b11010100 || (inst >> 22) == 0b1101010100)
                return false;

            // Disallow bitfield and extract
            if (op == 0b10011)
                return false;

            // Disallow conditional compare and data processing
            if ((op >> 1) == 0b1101)
                return false;

            return true;
        }

        private void xorRange(int offset, int length, byte xorValue) {
            var bytes = stream.ReadBytes(offset, length);
            bytes = bytes.Select(b => (byte) (b ^ xorValue)).ToArray();
            stream.Write(offset, bytes);
        }

        private void xorSection(string sectionName, byte xorValue, uint stripeSize) {
            var section = sections[sectionName];

            // First part up to stripe size boundary is always encrypted, first full block is always encrypted
            var start = (int) section.ImageStart;
            var length = section.ImageLength;

            // Non-striped
            if (stripeSize == 0) {
                xorRange(start, length, xorValue);
                return;
            }

            // Striped
            // The first block's length is the distance to the boundary to the first stripe size + one stripe
            var firstBlockLength = stripeSize;
            if (start % stripeSize != 0)
                firstBlockLength += stripeSize - (uint) (start % stripeSize);

            xorRange(start, (int) firstBlockLength, xorValue);

            // Step forward two stripe sizes at a time, decrypting the first and ignoring the second
            for (var pos = start + firstBlockLength + stripeSize; pos < start + length; pos += stripeSize * 2) {
                var size = Math.Min(stripeSize, start + length - pos);
                xorRange((int) pos, (int) size, xorValue);
            }
        }
    }
}
