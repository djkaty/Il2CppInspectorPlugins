# Il2CppInspector Plugins Repository

This is the official repository of plugins for [Il2CppInspector](https://github.com/djkaty/Il2CppInspector)

### Current plugins

#### Loaders

These plugins allow the processing of IL2CPP workloads not directly supported by Il2CppInspector.

* **miHoYo** - Honkai Impact and Genshin Impact _(**NOTE**: Requires UnityPlayer.dll from the corresponding PC version of the game, even if you are inspecting a mobile version)_

#### Examples

These plugins are intended as tutorial samples for plugin writers.

* **StringLiterals-ROT** shows how to setup a plugin project and perform ROT decryption on all string literals
* **Options-And-Validation** shows how to declare options, perform validation and receive option change notifications
* **LoadPipeline** shows all of the available hooks in Il2CppInspector's load pipeline and how to use them
* **Analytics** shows how to use a 3rd party nuget package and output data to files, producing a frequency graph of the chosen section in the input binary

### Installing plugins

[Download all current plugins as a bundle](https://github.com/djkaty/Il2CppInspectorPlugins/releases)

You can also use the `get-plugins.ps1` or `get-plugins.sh` scripts supplied with Il2CppInspector to fetch the current plugins.

Place plugins in a folder called `plugins` which should be created in the same location as `Il2CppInspector.exe`.

Use `--plugins` at the command line or click *Manage Plugins...* in the GUI to configure your plugins.

Learn more in the [Using Plugins](https://github.com/djkaty/Il2CppInspector#using-plugins) section of the Il2CppInspector README.

### Issue reports

**ONLY** use the issue tracker to report bugs in plugins.

**DO NOT** use the issue tracker to request plugins, request features for existing plugins, ask for help with plugins or report bugs in Il2CppInspector. These issues will be ignored. The plugin architecture exists to help you create new functionality, but we do not provide official support or take requests.

To report bugs in Il2CppInspector or its handling of plugins, use the [Il2CppInspector issue tracker](https://github.com/djkaty/Il2CppInspector/issues).

If you need plugin options, hooks or access to data that is not currently supported, or other plugin API features, feel free to suggest them on the [Il2CppInspector issue tracker](https://github.com/djkaty/Il2CppInspector/issues)!

### Creating plugins

See the [Il2CppInspector Plugin Development Wiki](https://github.com/djkaty/Il2CppInspector/wiki/Plugins%3A-Getting-Started) for information about how to create plugins.

### Submitting plugins

To submit a plugin:

* Clone the repo
* Create a folder in the appropriate category (`Examples` or `Loaders`) and place your plugin there
* Remove any local file references from the `.csproj` file
* Submit a PR with the new or update plugin as commits on `Il2CppInspectorPlugins`

#### Submission requirements

If you plugin operates on a specific application, you must attach the related files to the PR so that we can test it, but do not include them in commits.

Plugin submissions should include source code only.

Plugin submissions should not include files from 3rd party commercial applications. If your plugin requires a 3rd party DLL or other file to function, create an option which asks the user to supply it. Dependencies such as nuget packages and open-source code from github are permitted.

#### One plugin, one purpose

Plugins are designed to be chained. Do not include multiple unrelated functions in your plugin. Create separate plugins for each task.

#### Code style

Your plugin should as closely as possible follow the code style demonstrated in the existing plugins. Comment your code clearly so that its functionality can be understood by others.

Use descriptive, grammatically correct and correctly capitalized names and descriptions for your plugin and their options (if English is not your first language, we will help you).

#### Plugin updates

Submit updates to your plugins as PRs as above.

#### Licensing

Submissions that are accepted will be licensed under AGPLv3.

We may change, split or merge plugins over time to keep the codebase clean and logical, or incorporate new features added to Il2CppInspector.
