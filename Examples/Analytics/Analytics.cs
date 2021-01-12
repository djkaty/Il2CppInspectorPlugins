/*
    Copyright 2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

/*
 * This is an example plugin for Il2CppInspector
 * 
 * This plugin shows how to generate statistical data from an IL2CPP binary that can be analyzed in Excel,
 * how to output files and how to use a 3rd party nuget package in a plugin
 * 
 * NOTE: Be sure to copy Aspose.Cells.dll and System.Drawing.Common.dll into the plugin output folder
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aspose.Cells;
using Aspose.Cells.Charts;
using Aspose.Cells.Tables;
using Il2CppInspector;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.PluginAPI.V100;

namespace Analytics
{
    // Define your plugin class, implementing IPlugin plus interfaces for any hooks you wish to use
    public class AnalyticsPlugin : IPlugin, ILoadPipeline
    {
        // The ID that will be used to load the plugin from the command-line
        public string Id => "analytics";

        // The full name of the plugin
        public string Name => "Analytics Generator";

        // The nickname of the plugin author
        public string Author => "Katy"; 

        // The version of the plugin. Always use version numbers in increasing lexical order when you create a new version.
        public string Version => "1.0";

        // A description of what the plugin does
        public string Description => "Generates spreadsheets containing statistical data from an IL2CPP binary";

        // Our options
        private PluginOptionText sectionName = new PluginOptionText {
            Name = "section",
            Description = "Section to analyze",
            Value = ".rodata",
            Required = true
        };

        private PluginOptionFilePath outputPath = new PluginOptionFilePath {
            Name = "output",
            Description = "Output file (CSV or XLSX)",
            Value = "analytics.csv",
            Required = true,
            AllowedExtensions = new Dictionary<string, string> {
                ["csv"] = "CSV (Comma delimited)",
                ["xlsx"] = "Excel Workbook"
            }
        };

        // Let Il2CppInspector know about all of the options we have created
        public List<IPluginOption> Options => new List<IPluginOption> { sectionName, outputPath };

        // Generate analytics from binary image
        public void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo info) where T : FileFormatStream<T> {

            // Report to the user what is happening
            PluginServices.For(this).StatusUpdate("Generating analytics");

            // Get the section we would like to investigate
            Section section;
            try {
                section = stream.GetSections().Single(s => s.Name == sectionName.Value);
            }
            // Not all binaries have a section with this name, or any sections at all
            catch {
                return;
            }

            // Get contents of section
            var bytes = stream.ReadBytes(section.ImageStart, (int) (section.ImageEnd - section.ImageStart));

            // Produce frequency graph of bytes
            // This will produce a dictionary where the key is the byte value and the value is the number of times it occurred
            var freq = bytes.GroupBy(b => b).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => (double) g.Count() * 100 / bytes.Length);

            // Write as CSV file
            if (Path.GetExtension(outputPath.Value) == ".csv") {
                var csv = new StringBuilder();
                csv.AppendLine("Byte,Count");
                csv.Append(string.Join(Environment.NewLine, freq.Select(f => f.Key + "," + f.Value)));

                File.WriteAllText(outputPath.Value, csv.ToString());
                return;
            }

            // Write as XLSX file (using nuget package Aspose.Cells)
            var wb = new Workbook();
            var sheet = wb.Worksheets[0];

            // Add headers
            sheet.Cells["A1"].PutValue("Byte");
            sheet.Cells["B1"].PutValue("Count");

            // Create number format style
            var style = new CellsFactory().CreateStyle();
            style.Custom = "@"; // Text for hex bytes column

            // Add data
            for (var row = 0; row < freq.Count; row++) {
                sheet.Cells["A" + (row + 2)].PutValue($"{row:X2}");
                sheet.Cells["A" + (row + 2)].SetStyle(style, true);
                sheet.Cells["B" + (row + 2)].PutValue(freq[(byte) row]);
            }

            // Create table
            var list = sheet.ListObjects[sheet.ListObjects.Add("A1", "B257", hasHeaders: true)];
            list.TableStyleType = TableStyleType.TableStyleMedium6;

            // Create chart
            var chart = sheet.Charts[sheet.Charts.Add(ChartType.Column, 3, 3, 40, 26)];
            chart.Title.Text = "Frequency graph of data bytes";
            chart.Style = 3;
            chart.ValueAxis.IsLogarithmic = true;
            chart.ValueAxis.CrossAt = 0.001;
            chart.ValueAxis.IsAutomaticMaxValue = false;
            chart.ValueAxis.MaxValue = 100;
            chart.ValueAxis.LogBase = 2;

            // Set chart data
            chart.SetChartDataRange("A1:B257", isVertical: true);

            chart.NSeries.CategoryData = "A2:A257";
            chart.NSeries[0].GapWidth = 0; // 'Count' data series is at index 0 now that category has been set
            chart.ShowLegend = false;

            // Save workbook
            wb.Save(outputPath.Value, SaveFormat.Xlsx);
        }
    }
}
