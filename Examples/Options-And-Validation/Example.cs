/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

/*
 * This plugin demonstrates how to declare optioons, perform validation on them,
 * and how to receive a notification when they have changed
 */

/*
 * Expected output:
 * 
 * .\Il2CppInspector.exe --plugins options-example
 *
 * Usage for plugin 'options-example':
 * 
 *   --text                 Text option
 *   --test2                Required. (Default: starting value) Another text option
 *   -n                     (Default: 5) Some number
 *   --hex                  (Default: 0x0) Hexadecimal value
 *   -b                     (Default: false) Boolean option
 *   --path-to-some-file    Required. Path to external file
 *   --choice1              Required. (Default: second-item) List of choices
 *   --choice2              Required. (Default: third-item) Another list of choices
 */

using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInspector.PluginAPI.V100;

namespace Loader
{
    // Define your plugin class, implementing IPlugin plus interfaces for any hooks you wish to use
    public class Plugin : IPlugin
    {
        // Set the details of the plugin here
        public string Id => "options-example";
        public string Name => "Options & Validation Example";
        public string Author => "Katy";
        public string Version => "1.0";
        public string Description => "Demonstrates how to handle options and validation";

        // First, define your options

        // Basic (text) option
        private PluginOptionText text = new PluginOptionText {
            // The name of the option at the command-line ("--text")
            // If you supply a single character, eg. "t", single-dash syntax is used ("-t")
            Name = "text",

            // The description of the option for CLI help and GUI options editor
            Description = "Text option"
        };

        // Basic text option with additional settings
        private PluginOptionText text2 = new PluginOptionText {
            Name = "test2",
            Description = "Another text option", 

            // If Required = true, the CLI and GUI will force the user to set a value
            Required = true,

            // You can optionally supply a default value for an option
            Value = "starting value",

            // You can optionally supply a validation function for an option
            // The validation function receives the proposed new option value
            // This must always return true to accept or throw an exception to reject
            // The exception text will be shown as an error message in the CLI and GUI
            Validate = text => text.Length == 5? true : throw new ArgumentException("Text must be 5 characters")
        };

        // A numeric option (GUI: text box)
        // You can specify the type: ulong, long, uint, int, ushort, short, byte
        // The CLI and GUI will convert the user's entry to a the correct numeric type
        private PluginOptionNumber<long> number = new PluginOptionNumber<long> {
            Name = "n",
            Description = "Some number",
            Value = 5,
            
            // Only accept numbers between 1 and 10
            Validate = number => number >= 1 && number <= 10? true : throw new ArgumentException("Number must be between 1 and 10")
        };

        // A numeric option using hexadecimal entry (GUI: number-validated text box)
        // The CLI and GUI will conver the hexadecimal string to the correct numeric type
        // Hexadecimal numbers can be optionally preceded with '0x'
        private PluginOptionNumber<short> hexNumber = new PluginOptionNumber<short> {
            Name = "hex",
            Description = "Hexadecimal value",
            
            // To make a number hexadecimal, specify the Hex style
            Style = PluginOptionNumberStyle.Hex
        };

        // Boolean option (GUI: checkbox)
        // Boolean options can be specified at the CLI with a standalone switch, eg. "-b"
        // Boolean options are always required regardless of the Required property
        private PluginOptionBoolean boolean = new PluginOptionBoolean {
            Name = "b",
            Description = "Boolean option"
        };

        // File path option (GUI: file selection dialog)
        // The internal validator will check the pathname is valid
        private PluginOptionFilePath filePath = new PluginOptionFilePath {
            Name = "path-to-some-file",
            Description = "Path to external file",
            Required = true,

            // Set this to ensure the user selects a file that exists
            MustExist = true, 

            // Set this to ensure the user selects a file that doesn't exist (usually for saving)
            MustNotExist = false,

            // Set this to ensure the user selects a folder rather than a file
            IsFolder = false,

            // Example validation, forces the user to select a file ending in ".dll"
            Validate = path => path.ToLower().EndsWith(".dll")? true : throw new FileNotFoundException($"You must supply a DLL file", path)
        };

        // List of choices (GUI: list of radio buttons)
        // Each option has an internal value and a display name shown to the user
        // The internal value is specified at the CLI eg. "--choice1 first-item"
        // and also used when manipulating options in code
        // The display value is shown in the configuration dialog in the GUI
        private PluginOptionChoice<string> choice1 = new PluginOptionChoice<string> {
            Name = "choice1",
            Description = "List of choices",
            Required = true,
            Value = "second-item",

            Choices = new Dictionary<string, string> {
                ["first-item"]   = "First item",
                ["second-item"]  = "Second item",
                ["third-item"]   = "Third item"
            },

            // To make a set of choices display as a list of radio buttons in the GUI, specify the List style
            Style = PluginOptionChoiceStyle.List
        };

        // List of choices (GUI: drop-down box)
        // Exactly the same as above, but set the style as Dropdown
        private PluginOptionChoice<string> choice2 = new PluginOptionChoice<string> {
            Name = "choice2",
            Description = "Another list of choices",
            Required = true,
            Value = "third-item",

            Choices = new Dictionary<string, string> {
                ["first-item"]   = "First item",
                ["second-item"]  = "Second item",
                ["third-item"]   = "Third item"
            },

            // Dropdown is the default option so you can omit this
            // Style = PluginOptionChoiceStyle.Dropdown 
        };

        // Make the options available to Il2CppInspector
        // The order determines the order options appear in the CLI help and GUI dialog box
        public List<IPluginOption> Options => new List<IPluginOption> { text, text2, number, hexNumber, boolean, filePath, choice1, choice2 };

        // You can add conditions to enable or disable options in the GUI based on the settings of other options.
        // These must be initialized in the parameterless constructor
        public Plugin() {

            // Here we only enable the first set of choices if the boolean option is ticked
            choice1.If = () => boolean.Value;

            // We can also use the If property from another option to chain conditions
            choice2.If = () => !choice1.If();
        }

        // You can optionally implement OptionsChanged
        // This event fires when the user changes the options (supplied at the CLI, via the GUI dialog box or in code),
        // after they have all been validated without errors
        // It does not fire when the plugin first loads
        // Do not perform long-running operations here
        // The default action is to do nothing
        // The PluginOptionsChangedEventInfo parameter is reserved for future use
        public void OptionsChanged(PluginOptionsChangedEventInfo info) {

            // Perform validation using the Validate property of each option

            // Unset values will have their .NET default values (eg. 0 for value types)
            // Unset reference types eg. strings will be null

            // Use the Value property to access an option's current value
            Console.WriteLine(text.Value);

            // If there is an additional problem, you can throw an exception here
            // and it will be displayed in the CLI and GUI
            if (false) {
                throw new InvalidOperationException("There was a problem with the options");
            }
        }
    }
}
