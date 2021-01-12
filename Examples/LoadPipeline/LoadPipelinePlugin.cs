/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

/*
 * This plugin demonstrates where the various load pipeline hooks are called and how to use them
 * 
 * TIP: This plugin should work with any Unity application. Step through it line by line with the debugger!
 * 
 * TIP: The API surface is large and we give only small examples here. Use IntelliSense to discover useful methods and properties
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInspector;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;
using NoisyCowStudios.Bin2Object;

namespace Loader
{
    // Define your plugin class, implementing IPlugin plus interfaces for any hooks you wish to use
    public class Plugin : IPlugin, ILoadPipeline
    {
        // Set the details of the plugin here
        public string Id => "load-pipeline";
        public string Name => "Example: Load Pipeline";
        public string Author => "Katy";
        public string Version => "1.0";
        public string Description => "Demonstrates the load pipeline process";

        // No options
        public List<IPluginOption> Options => null;

        public void LoadPipelineStarting(PluginLoadPipelineStartingEventInfo info) {
            // Perform per load initialization here
        }

        // See: Bin2Object/BinaryObjectStream.cs
        public void PreProcessMetadata(BinaryObjectStream stream, PluginPreProcessMetadataEventInfo info) {

            // Example: check if metadata has correct signature
            if (stream.ReadUInt32() != Il2CppConstants.MetadataSignature)
                Console.Error.WriteLine("Metadata signature is invalid");

            // The file is loaded into memory so you can make edits without affecting the file

            // See PreProcessImage below for information about using BinaryObjectStream for de-obfuscation

            // Set info.IsStreamModified if you change the stream contents
        }

        // See: IL2CPP/Metadata.cs
        public void GetStrings(Metadata metadata, PluginGetStringsEventInfo data) {
            // NOTE: Metadata derives from BinaryObjectStream

            // Everything is available in Metadata except Strings and StringLiterals

            // See PostProcessMetadata below

            // Set data.Strings to a complete list of indexes and strings
            // and set data.IsDataModified
        }

        // See: IL2CPP/Metadata.cs
        public void GetStringLiterals(Metadata metadata, PluginGetStringLiteralsEventInfo data) {
            // NOTE: Metadata derives from BinaryObjectStream

            // Everything is available in Metadata except StringLiterals

            // See PostProcessMetadata below

            // Set data.Strings to a complete list of indexes and strings
            // and set data.IsDataModified
        }

        // See: IL2CPP/Metadata.cs
        // See: IL2CPP/MetadataClasses.cs
        public void PostProcessMetadata(Metadata metadata, PluginPostProcessMetadataEventInfo data) {
            // NOTE: Metadata derives from BinaryObjectStream

            // Everything is available in Metadata

            // This is a direct parsing of the global-metadata.dat file
            // containing Il2Cpp*[] arrays

            // Assemblies, Images, Types, Methods, Params, Fields, etc. etc.
            // See Il2CppInspector.Common/IL2CPP/Metadata.cs for a complete list

            // Example: list all assemblies in metadata
            /*  Assembly 0 = mscorlib
                Assembly 1 = System
                Assembly 2 = Mono.Security
                Assembly 3 = System.Core
                Assembly 4 = System.Xml
                Assembly 5 = UnityEngine .... */
            foreach (var asm in metadata.Assemblies)
                Console.WriteLine($"Assembly {asm.imageIndex} = {metadata.Strings[asm.aname.nameIndex]}");

            // Set data.IsDataModified if you change the metadata contents
        }

        // Example for PreProcessImage
        private class ObjectInStream
        {
            int a;
            short b;
        }

        private class ObjectInMemory
        {
            short b;
            int a;
        }

        // See: Bin2Object/BinaryObjectStream.cs
        public void PreProcessImage(BinaryObjectStream stream, PluginPreProcessImageEventInfo data) {
            // This is called once when the user selects an application binary

            // Example: try to read 64-bit ELF header
            try {
                var header = stream.ReadObject<elf_header<ulong>>();

                // Check for magic bytes
                if ((Elf) header.m_dwFormat == Elf.ELFMAG) {

                    // Do something with ELF file
                    Console.WriteLine("ELF image found");
                }
            } catch { }

            // The file is loaded into memory so you can make edits without affecting the file

            // BinaryObjectStream provides many useful methods and properties
            // Set endianness with Endianness property
            // Set text encoding with Encoding property

            // Map reading of one primitive type to another (for both reading and writing):
            // This will cause all uints in the stream to be converted to ulongs when read with ReadObject<uint>()
            // This allows you to use one execution path to read 32-bit and 64-bit files
            stream.AddPrimitiveMapping(typeof(ulong), typeof(uint));

            // Map reading of one object to another (for both reading and writing):
            // This will cause all fields that exist in ObjectInMemory to be populated with
            // the correspondingly named fields in ObjectInStream
            // This is useful if items have been re-ordered via obfuscation
            stream.AddObjectMapping(typeof(ObjectInMemory), typeof(ObjectInStream));

            // With the above mapping, this will read an ObjectInStream and translate it to an ObjectInMemory
            try {
                var obj = stream.ReadObject<ObjectInMemory>();
            } catch { }

            // Mappings will be persisted for the entire lifetime of the stream,
            // so you can use them to alter the order in which Il2CppInspector reads fields in structs

            // Clear mappings:
            stream.Reader.PrimitiveMappings.Clear();
            stream.Reader.ObjectMappings.Clear();
            stream.Writer.PrimitiveMappings.Clear();
            stream.Writer.PrimitiveMappings.Clear();

            // Other useful methods:
            // ReadObject<T>, ReadArray<T>, ReadNullTerminatedString, ReadFixedLengthString
            // + equivalent write methods

            // You can also use a number of attributes:
            // ArrayLengthAttribute, StringAttribute, VersionAttribute, SkipWhenReadingAttribute

            // Set data.IsStreamModified if you change the stream contents
        }

        // See: FileFormatStreams/FileFormatStream.cs
        // See: FileFormatStreams/*Reader.cs
        // See: FileFormatStreams/Export.cs
        // See: FileFormatStreams/Section.cs
        // See: FileFormatStreams/Symbol.cs
        public void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo data) where T : FileFormatStream<T> {
            // This is called once FOR EACH binary image
            // Regular ELF, PE, MachO files will result in a single call
            // Binaries containing sub-images (eg. Fat MachO, multi-architecture/split APKs) will result in
            // one call for the complete image and one call for each sub-image

            // FileFormatStream<T> derives from BinaryObjectReader and implements IFileFormatStream

            // Example: ignore Fat MachO files (wait for a sub-image)
            if (stream is UBReader)
                return;

            // Example: select an image extracted from a Fat MachO file
            if (stream is MachOReader32) {

            }
            if (stream is MachOReader64) {

            }

            // Useful properties:
            // Length, Numimages, DefaultFilename, IsModified, Images[], Position, Format, Arch, Bits,
            // GlobalOffset, ImageBase etc.

            // Example: check if file is ARM64
            var isArm64 = stream.Arch == "ARM64";

            // Example: check if file is 64-bit PE file
            var isPE64 = stream.Format == "PE32+";

            // Useful methods:
            // GetSymbolTable(), GetFunctionTable(), GetExports(), GetSections()
            // MapVATR() (maps a virtual address to a file offset), MapFileOffsetToVA() (the opposite) etc.

            // Example: get all sections in file
            /*  Section 00004370 0000000000008370 __text
                Section 01f48058 0000000001f4c058 __picsymbolstub4
                Section 01f4aba8 0000000001f4eba8 __stub_helper
                Section 01f4cbe8 0000000001f50be8 __gcc_except_tab ... */
            if (stream.TryGetSections(out var sections)) {
                foreach (var section in sections)
                    Console.WriteLine($"Section {section.ImageStart:x8} {section.VirtualStart:x16} {section.Name}");
            }

            // You can use ReadWord and ReadWordArray to read a uint or ulong depending on whether the file is 32 or 64-bit

            // You can use ReadMappedX versions of all the Read functions to read from a virtual address mapped to the file

            // Example: Read 16 bytes from wherever virtual address 0x12345678 maps to in the file:
            try {
                var bytes = stream.ReadMappedBytes(0x12345678, 16);
            } catch { }

            // ReadMappedObjectPointerArray reads a list of VA pointers from the specified mapped VA,
            // then reads all of the objects

            // Example: VA 0x4000000 maps to 0x2000 in the file and contains 3 pointers, 0x5000010, 0x5000020, 0x5000440
            // which are the VAs of three 'ObjectInMemory' objects:
            try {
                var objs = stream.ReadMappedObjectPointerArray<ObjectInMemory>(0x4000000, 3);
            } catch { }

            // Set data.IsStreamModified if you change the stream contents
        }

        // See: IL2CPP/Il2CppBinary.cs
        public void PreProcessBinary(Il2CppBinary binary, PluginPreProcessBinaryEventInfo data) {
            // This is called once per found IL2CPP binary
            // after Il2CppCodeRegistration and Il2CppMetadataRegistration have been located and read
            // into binary.CodeRegistration and binary.MetadataRegistration,
            // but they have not been validated and no other loading or analysis has been performed

            // Example: check that the found structs make sense
            if (binary.CodeRegistration.interopDataCount > 0x1000) {
                // This is very unlikely
            }

            // You can acquire the underlying IFileFormatStream (BinaryObjectStream) with binary.Image
            var underlyingStream = binary.Image;

            // Set data.IsDataModified if you modify the Il2CppBinary
            // Set data.IsStreamModified if you modify the stream contents
        }

        // See: IL2CPP/Il2CppBinary.cs
        // See: IL2CPP/Il2CppBinaryClasses.cs
        public void PostProcessBinary(Il2CppBinary binary, PluginPostProcessBinaryEventInfo data) {
            // This is called once per IL2CPP binary after it has been fully loaded

            // This is a direct parsing of the structs in the binary file
            // with some items re-arranged into eg. Dictionary collections for simplified access

            // CodeRegistration, MetadataRegistration, CodeGenModulePointers,
            // FieldOffsetPointers, MethodSpecs, GenericInstances, TypeReferences etc. etc.

            // See Il2CppInspector.Common/IL2CPP/Il2CppBinary.cs for a complete list

            // Note that some items are only valid for certain IL2CPP versions
            // Use the Image.Version property to check the IL2CPP version

            // Example: list all CodeGenModules
            /*  Module mscorlib.dll has 11863 methods
                Module Mono.Security.dll has 421 methods
                Module System.Xml.dll has 1 methods
                Module System.dll has 3623 methods ... */
            if (binary.Image.Version >= 24.2) {
                foreach (var module in binary.Modules)
                    Console.WriteLine($"Module {module.Key} has {module.Value.methodPointerCount} methods");
            }

            // Set data.IsDataModified if you modify the Il2CppBinary
            // Set data.IsStreamModified if you modify the stream contents
        }

        // See: IL2CPP/Il2CppInspector.cs
        public void PostProcessPackage(Il2CppInspector.Il2CppInspector package, PluginPostProcessPackageEventInfo data) {
            // This is called once per IL2CPP binary after it has been merged with global-metadata.dat
            // and all of the data linked together

            // It contains surrogate properties to everything in Metadata and Il2CppBinary
            // plus all calculated default field values (FieldDefaultValue), default parameter values (ParameterDefaultValue),
            // field offsets (FieldOffsets), custom attribute generators (CustomAttributeGenerators),
            // function addresses (FunctionAddresses) and metadata usages (MetadataUsages) etc.

            // Set data.IsDataModified if you modify the Il2CppInspector
        }

        // See: IL2CPP/Il2CppInspector.cs
        public void LoadPipelineEnding(List<Il2CppInspector.Il2CppInspector> packages, PluginLoadPipelineEndingEventInfo info) {
            // Perform per load finalization and teardown here

            // The 'packages' argument contains every Il2CppInspector metadata+binary package generated from the input files
            // eg. a Fat MachO with 32-bit and 64-bit images will generate two Il2CppInspectors

            // If you want to load additional IL2CPP applications for comparative analysis, this is a good place to do it

            // Example (store these in your plugin class; we use [0] to select the first image):
            // (Note: this will cause your load pipeline functions to execute again except for the one it is called from;
            // be sure to check for this and ignore load tasks you're not interested in. If you want the calling function
            // to be executed recursively, add the [Reentrant] attribute to the method declaration)
            try {
                var anotherIl2cpp = Il2CppInspector.Il2CppInspector.LoadFromFile("some-libil2cpp.so", "some-global-metadata.dat")[0];
                var anotherTypeModel = new TypeModel(anotherIl2cpp);
            } catch { }
        }

        // See: Reflection/TypeModel.cs
        // See: Reflection/*.cs
        public void PostProcessTypeModel(TypeModel model, PluginPostProcessTypeModelEventInfo data) {
            // This is only called if the user generates a .NET type model from an Il2CppInspector package
            // The bundled CLI and GUI do this automatically as soon as loading completes for each package

            // The .NET type model gives you a feature-complete .NET Reflection-style API to IL2CPP applications

            // You can access the underlying components:
            var package = model.Package;
            var binary = model.Package.Binary;
            var binaryImage = model.Package.BinaryImage;
            var metadata = model.Package.Metadata;

            // Useful properties:
            // Assemblies, Namespaces, TypesByDefinitionIndex, TypesByReferenceIndex, GenericParameterTypes,
            // GenericMethods, TypesByFullName, Types, MethodsByDefinitionIndex, MethodInvokers,
            // AttributesByIndices, CustomAttributeGenerators, CustomAttributeGeneratorsByAddress etc.

            // See Il2CppInspector.Common/Reflection/TypeModel.cs for a complete list

            // The model is designed to be used with Linq queries for maximum flexibility

            // Useful methods as entry points to drill down: GetAssembly(string), GetType(string)

            // Example: get main game assembly
            var asmGame = model.GetAssembly("Assembly-CSharp.dll");

            // Example: get Vector3
            var typeVector3 = model.GetType("UnityEngine.Vector3");

            // Example: get all C# method definitions in the application
            var methodsAll = model.Types.SelectMany(t => t.DeclaredMethods).ToList();

            // Example: get all methods from a type including inherited methods
            var methodsForType = typeVector3.GetAllMethods();

            /*  Methods for UnityEngine.Vector3:
                Vector3 RotateTowards(Vector3, Vector3, Single, Single)
                Void INTERNAL_CALL_RotateTowards(Vector3 ByRef, Vector3 ByRef, Single, Single, Vector3 ByRef)
                Vector3 Lerp(Vector3, Vector3, Single)
                Vector3 MoveTowards(Vector3, Vector3, Single) ... */
            Console.WriteLine($"Methods for {typeVector3.FullName}:");
            foreach (var method in methodsForType)
                Console.WriteLine(method.ToString());

            // Example: get all properties called "Id"
            var propIds = model.Types.SelectMany(t => t.DeclaredProperties.Where(p => p.Name == "Id")).ToList();

            /*  Types that have an Id property:
                System.Diagnostics.Process
                Mono.Security.Protocol.Tls.ClientSessionInfo
                System.Xml.Schema.XmlSchema
                System.Xml.Schema.XmlSchemaAnnotated
                System.Xml.Schema.XmlSchemaAnnotation ...
             */
            Console.WriteLine($"Types that have an Id property:");
            foreach (var prop in propIds)
                Console.WriteLine(prop.DeclaringType.FullName);

            // The type model implements many of the standard .NET Reflection APIs for IL2CPP:
            // https://docs.microsoft.com/en-us/dotnet/api/system.reflection?view=net-5.0

            // Assembly, ConstructorInfo, CustomAttributeData, EventInfo, FieldInfo, MemberInfo,
            // MethodBase, MethodInfo, ParameterInfo, PropertyInfo, TypeInfo
            // + IL2CPP-specific types MethodInvoker, Scope, TypeRef, MetadataUsage

            // Filtering on specific criteria:
            // Find this method overload: public static Vector3 Scale(Vector3 a, Vector3 b);
            var scale = typeVector3.GetMethods("Scale")
                .Single(m => m.DeclaredParameters.Count == 2
                            && m.DeclaredParameters[0].ParameterType == typeVector3
                            && m.DeclaredParameters[1].ParameterType == typeVector3
                            && m.ReturnType == typeVector3
                            && m.IsStatic
                            && m.IsPublic);

            // You can get the machine code of a method as a byte[] for signature scanning or disassembly etc.
            var scaleCode = scale.GetMethodBody();

            // Set data.IsDataModified if you modify the TypeModel
        }

        // See: Model/AppModel.cs
        // See: Model/*.cs
        // See: Cpp/CppTypeCollection.cs
        // See: Cpp/CppType.cs
        // See: Cpp/CppField.cs
        // See: Il2CppTests/TestAppModelQueries.cs
        // See: Il2CppTests/CppTypeDeclarations.cs
        public void PostProcessAppModel(AppModel appModel, PluginPostProcessAppModelEventInfo data) {
            // This is only called if the user generates a C++ application model from a .NET type model
            // The bundled CLI and GUI do this automatically if it is required for the selected outputs

            // The application model provides a C-oriented model of the IL2CPP application;
            // lower level than using the .NET type model but higher level than the IL2CPP structures

            // Note that you should change the .NET type model, NOT the application model, if you want
            // changes to propagate to all outputs because all outputs including the application model
            // are derived from the .NET type model

            // Changing the application model will only change outputs that depend on it, eg.
            // C++ scaffolding project, IDA script output etc.

            // You can access the underlying components:
            var typeModel = appModel.TypeModel;
            var package = appModel.Package;
            var binary = appModel.Package.Binary;
            var binaryImage = appModel.Package.BinaryImage;
            var metadata = appModel.Package.Metadata;

            // AppModels are targeted towards a specific compiler type and a specific version of Unity
            // SourceCompiler, TargetCompiler, UnityVersion, UnityHeaders

            // AppModel provides a composite mapping of .NET methods to C++ functions,
            // and .NET types to C++ types using MultiKeyDictionary

            var vector3 = typeModel.GetType("UnityEngine.Vector3");

            // See: Model/AppType.cs
            // CppType, CppValueType, Type, TypeClassAddress, TypeRefPtrAddress, Name etc.
            var appType = appModel.Types[vector3];

            // See: Model/AppMethod.cs
            // CppFnPtrType, Method, MethodInfoPtrAddress, MethodCodeAddress etc.
            var appMethod = appModel.Methods[vector3.GetMethod("Scale")];

            // Example: Get the C++ struct for Vector3
            var v3struct = appType.CppValueType; // null for .NET reference types

            /*  Vector3 C++ struct:
                struct Vector3 {
                    float x;
                    float y;
                    float z;
                }; */
            Console.WriteLine("Vector3 C++ struct:");
            Console.WriteLine(v3struct.ToString());

            // Example: Get the C++ class for Vector3, derived from Il2CppObject
            var v3class = appType.CppType;

            /*  Vector3 C++ class:
                struct Vector3__Boxed {
                    struct Vector3__Class *klass;
                    MonitorData *monitor;
                    struct Vector3 fields;
                }; */
            Console.WriteLine("Vector3 C++ class:");
            Console.WriteLine(v3class.ToString());

            // Example: Get the C++ function pointer type for a method
            var scaleFnPtrType = appMethod.CppFnPtrType;

            /* Vector3.Scale C++ function: Vector3 Vector3_Scale(void * this, Vector3 a, Vector3 b, MethodInfo * method) */
            Console.WriteLine("Vector3.Scale C++ function: " + scaleFnPtrType.ToSignatureString());

            // You can also retrieve composite types from the same dictionaries
            // via their C++ class types or function pointer types, if you want to
            // go back to the .NET type model
            var appType2 = appModel.Types[v3class];
            var appMethod2 = appModel.Methods[scaleFnPtrType];

            if (appType != appType2 || appMethod != appMethod2)
                throw new Exception("This will never happen");

            // Get detailed information about a type
            // We can use a type directly from appModel.Types, or inspect a method:
            var scaleReturnType = scaleFnPtrType.ReturnType;
            var argType = scaleFnPtrType.Arguments[1].Type as CppComplexType; // Note: Arguments[0] == this pointer

            /* Vector3: 12 bytes; 0-byte aligned */
            Console.WriteLine($"{argType.Name}: {argType.SizeBytes} bytes; {argType.AlignmentBytes}-byte aligned");

            // Get every field in a class or struct
            /*  x, offset 00 bytes, length 4 bytes, type float
                y, offset 04 bytes, length 4 bytes, type float
                z, offset 08 bytes, length 4 bytes, type float */
            foreach (var field in argType)
                Console.WriteLine($"{field.Name}, offset {field.OffsetBytes:x2} bytes, length {field.SizeBytes} bytes, type {field.Type.Name}");

            // Many other ways to access fields
            var fieldByByteOffset = argType[4][0]; // will get Vector3.y field
            var fieldByName = argType["y"];

            if (fieldByName != fieldByByteOffset)
                throw new Exception("This will never happen");

            // Get any C++ type in the AppModel by name
            var typeDefType = appModel.CppTypeCollection.GetComplexType("Il2CppTypeDefinition");

            // You can also add types and methods to the AppModel
            // Examples are not shown here; browse AppModel.cs and CppTypeCollection.cs for details

            // You can generate an address map from the AppModel (Note: slow)
            // This is an IDictionary<ulong, object> which maps every address in an IL2CPP binary to its contents
            // as elements from the AppModel, for example function addresses will yield an AppMethod allowing you
            // to access both the C++ and .NET methods for the same function

            // This feature is currently highly experimental
            // See: Model/AddressMap.cs for example contents
            var map = appModel.GetAddressMap();

            // Set data.IsDataModified if you modify the AppModel
        }
    }
}
