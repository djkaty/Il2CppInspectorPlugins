/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    FOR EDUCATIONAL PURPOSES ONLY
    All rights reserved.
*/

// Custom type mappings for miHoYo workloads

using NoisyCowStudios.Bin2Object;
using Il2CppInspector;

namespace Loader
{
    // miHoYo re-arranges the order of fields in some metadata types.
    // With these types, we tell Il2CppInspector in which order to read the fields from the file.
    // Il2CppInspector will copy any fields with matching names in these types to the real,
    // correctly ordered types once they have been loaded.
    
    // Field names that don't match are ignored.

    // The [SkipWhenReading] attribute allows you to set a fixed value for a field in code,
    // instead of reading it from the file.

    // To read blocks of data and ignore them, you can use placeholder fields (see unk* below).
    // To skip over a larger block, use [ArrayLength(FixedSize = ...)] public byte[] foo;
    // This will read the specified number of bytes and throw them away.

    public class Il2CppGlobalMetadataHeader
    {
        [SkipWhenReading]
        public uint signature = Il2CppConstants.MetadataSignature;

        [SkipWhenReading]
        public int version = 24;

        [ArrayLength(FixedSize = 0x28)]
        public byte[] unk;

        public int genericContainersOffset; // Il2CppGenericContainer
        public int genericContainersCount;
        public int nestedTypesOffset; // TypeDefinitionIndex
        public int nestedTypesCount;
        public int interfacesOffset; // TypeIndex
        public int interfacesCount;
        public int vtableMethodsOffset; // EncodedMethodIndex
        public int vtableMethodsCount;
        public int interfaceOffsetsOffset; // Il2CppInterfaceOffsetPair
        public int interfaceOffsetsCount;
        public int typeDefinitionsOffset; // Il2CppTypeDefinition
        public int typeDefinitionsCount;

        public int rgctxEntriesOffset; // Il2CppRGCTXDefinition
        public int rgctxEntriesCount;

        public int unk1;
        public int unk2;
        public int unk3;
        public int unk4;

        public int imagesOffset; // Il2CppImageDefinition
        public int imagesCount;
        public int assembliesOffset; // Il2CppAssemblyDefinition
        public int assembliesCount;

        public int fieldsOffset; // Il2CppFieldDefinition
        public int fieldsCount;
        public int genericParametersOffset; // Il2CppGenericParameter
        public int genericParametersCount;

        public int fieldAndParameterDefaultValueDataOffset; // uint8_t
        public int fieldAndParameterDefaultValueDataCount;

        public int fieldMarshaledSizesOffset; // Il2CppFieldMarshaledSize
        public int fieldMarshaledSizesCount;
        public int referencedAssembliesOffset; // int32_t
        public int referencedAssembliesCount;

        public int attributesInfoOffset; // Il2CppCustomAttributeTypeRange
        public int attributesInfoCount;
        public int attributeTypesOffset; // TypeIndex
        public int attributeTypesCount;

        public int unresolvedVirtualCallParameterTypesOffset; // TypeIndex
        public int unresolvedVirtualCallParameterTypesCount;
        public int unresolvedVirtualCallParameterRangesOffset; // Il2CppRange
        public int unresolvedVirtualCallParameterRangesCount;

        public int windowsRuntimeTypeNamesOffset; // Il2CppWindowsRuntimeTypeNamePair
        public int windowsRuntimeTypeNamesSize;
        public int exportedTypeDefinitionsOffset; // TypeDefinitionIndex
        public int exportedTypeDefinitionsCount;

        public int unk5;
        public int unk6;

        public int parametersOffset; // Il2CppParameterDefinition
        public int parametersCount;

        public int genericParameterConstraintsOffset; // TypeIndex
        public int genericParameterConstraintsCount;

        public int unk7;
        public int unk8;

        public int metadataUsagePairsOffset; // Il2CppMetadataUsagePair
        public int metadataUsagePairsCount;

        public int unk9;
        public int unk10;
        public int unk11;
        public int unk12;

        public int fieldRefsOffset; // Il2CppFieldRef
        public int fieldRefsCount;

        public int eventsOffset; // Il2CppEventDefinition
        public int eventsCount;
        public int propertiesOffset; // Il2CppPropertyDefinition
        public int propertiesCount;
        public int methodsOffset; // Il2CppMethodDefinition
        public int methodsCount;

        public int parameterDefaultValuesOffset; // Il2CppParameterDefaultValue
        public int parameterDefaultValuesCount;

        public int fieldDefaultValuesOffset; // Il2CppFieldDefaultValue
        public int fieldDefaultValuesCount;

        public int unk13;
        public int unk14;
        public int unk15;
        public int unk16;

        public int metadataUsageListsOffset; // Il2CppMetadataUsageList
        public int metadataUsageListsCount;
    }

    public class Il2CppTypeDefinition
    {
        public int nameIndex;
        public int namespaceIndex;
        public int customAttributeIndex;
        public int byvalTypeIndex;
        public int byrefTypeIndex;

        public int declaringTypeIndex;
        public int parentIndex;
        public int elementTypeIndex;

        public int rgctxStartIndex;
        public int rgctxCount;

        public int genericContainerIndex;

        public uint flags;

        public int fieldStart;
        public int propertyStart;
        public int methodStart;
        public int eventStart;
        public int nestedTypesStart;
        public int interfacesStart;
        public int interfaceOffsetsStart;
        public int vtableStart;

        public ushort event_count;
        public ushort method_count;
        public ushort property_count;
        public ushort field_count;
        public ushort vtable_count;
        public ushort interfaces_count;
        public ushort interface_offsets_count;
        public ushort nested_type_count;

        public uint bitfield;
        public uint token;
    }

    public class Il2CppMethodDefinition
    {
        public int returnType;
        public int declaringType;
        public int unk1;
        public int nameIndex;
        public int parameterStart;
        public int genericContainerIndex;
        public int customAttributeIndex;
        public int reversePInvokeWrapperIndex;
        public int unk2;
        public int methodIndex;
        public int invokerIndex;
        public int rgctxCount;
        public int rgctxStartIndex;
        public ushort parameterCount;
        public ushort flags;
        public ushort slot;
        public ushort iflags;
        public uint token;
    }

    public class Il2CppFieldDefinition
    {
        public int customAttributeIndex;
        public int typeIndex;
        public int nameIndex;
        public uint token;
    }

    public class Il2CppPropertyDefinition
    {
        public int customAttributeIndex;
        public int nameIndex;
        public int unk1;
        public uint token;
        public uint attrs;
        public int unk2;
        public int set;
        public int get;
    }
}