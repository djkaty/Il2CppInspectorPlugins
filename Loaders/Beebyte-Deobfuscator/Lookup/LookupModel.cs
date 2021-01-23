﻿using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModel
    {
        public List<Translation> Translations = new List<Translation>();

        private LookupMatrix Matrix;
        private Dictionary<LookupType, LookupType> Matches;
        private HashSet<LookupType> MatchedTypes;

        private List<string> CleanTypeNameData;
        public List<string> CleanTypeNames
        {
            get
            {
                return CleanTypeNameData;
            }
        }
        private List<LookupType> CleanTypeData;
        public List<LookupType> CleanTypes
        {
            get
            {
                return CleanTypeData;
            }
        }
        private List<string> ObfTypeNameData;
        public List<string> ObfTypeNames
        {
            get
            {
                return ObfTypeNameData;
            }
        }
        public List<LookupType> ObfTypeData;
        public List<LookupType> ObfTypes
        {
            get
            {
                return ObfTypeData;
            }
        }
        public List<TypeDef> ProcessedMonoTypes = new List<TypeDef>();
        public List<TypeInfo> ProcessedIl2CppTypes = new List<TypeInfo>();
        public Dictionary<TypeDef, LookupType> MonoTypeMatches = new Dictionary<TypeDef, LookupType>();
        public Dictionary<TypeInfo, LookupType> Il2CppTypeMatches = new Dictionary<TypeInfo, LookupType>();

        public string NamingRegex;

        public LookupModel(string namingRegex)
        {
            NamingRegex = namingRegex;
            ProcessedMonoTypes = new List<TypeDef>();
            ProcessedIl2CppTypes = new List<TypeInfo>();
            MonoTypeMatches = new Dictionary<TypeDef, LookupType>();
            Il2CppTypeMatches = new Dictionary<TypeInfo, LookupType>();
        }

        public void Init(LookupModule obfModule, LookupModule cleanModule)
        {
            if (cleanModule.Namespaces.Contains("Beebyte.Obfuscator")) throw new ArgumentException("The application you provided as \"unobfuscated\" has obfuscation detected");
            Matches = new Dictionary<LookupType, LookupType>();
            MatchedTypes = new HashSet<LookupType>();

            CleanTypeData = cleanModule.Types.ToList();
            CleanTypeNameData = new List<string>();
            CleanTypeNameData.AddRange(cleanModule.Types.Select(x => x.Name));

            ObfTypeData = obfModule.Types;
            ObfTypeNameData = new List<string>();
            ObfTypeNameData.AddRange(obfModule.Types.Select(x => x.Name));
            Matrix = new LookupMatrix();

            foreach (LookupType type in obfModule.Types.Where(t => t.ShouldTranslate))
            {
                if (!type.DeclaringType.IsEmpty) continue;

                Matrix.Insert(type);
            }
        }

        public LookupType GetMatchingType(LookupType type, bool checkoffsets)
        {
            LookupType typeInfo = null;

            List<LookupType> types = Matrix.Get(type);

            if (types.Count() == 1 && types[0] != null)
            {
                bool t1 = !MatchedTypes.Contains(types[0]);
                bool t2 = !CleanTypeNames.Contains(types[0].Name);
                if (t1 && t2)
                {
                    MatchedTypes.Add(types[0]);
                    Matches.Add(types[0], type);
                    return types[0];
                }
            }

            float best_score = 0.0f;

            foreach (LookupType t in types)
            {
                if (MatchedTypes.Contains(t) || CleanTypeNames.Contains(t.Name)) continue;

                if (t.Name.Equals(type.Name))
                {
                    typeInfo = t;
                    break;
                }
                float score = 0.0f;

                if (checkoffsets)
                    score = (Helpers.CompareFieldOffsets(t, type, this) + Helpers.CompareFieldTypes(t, type, this)) / 2;
                else
                    score = Helpers.CompareFieldTypes(t, type, this);
                if (score > best_score)
                {
                    best_score = score;
                    typeInfo = t;
                }
            }
            if (typeInfo != null && !MatchedTypes.Contains(typeInfo)) 
            {
                Matches.Add(typeInfo, type);
                MatchedTypes.Add(typeInfo);
            }
            return typeInfo;
        }

        public void TranslateTypes(bool checkoffsets = false)
        {
            foreach (LookupType t in CleanTypes)
            {
                if (!t.DeclaringType.IsEmpty || t.IsEnum) continue;
                LookupType matchingType = GetMatchingType(t, checkoffsets);
                if (matchingType == null) continue;

                if (matchingType.Children.Count() > 0 && t.Children.Count() > 0) LookupTranslators.TranslateChildren(matchingType, t, checkoffsets, this);

                matchingType.Name = t.Name;
            }
            TranslateFields(checkoffsets);
        }

        public void TranslateFields(bool checkoffsets)
        {
            //Translate fields for matched types
            foreach (KeyValuePair<LookupType, LookupType> match in Matches)
            {
                LookupTranslators.TranslateFields(match.Key, match.Value, checkoffsets, this);
            }

            //Translate fields for types with the same name
            foreach (LookupType cleanType in CleanTypes)
            {
                LookupType obfType = null;
                List<LookupType> equalNamedTypes = ObfTypes.Where(t => t.Name == cleanType.Name).ToList();
                if (equalNamedTypes.Count() > 0) obfType = equalNamedTypes[0];
                if (!MatchedTypes.Contains(cleanType) && obfType != null)
                {
                    Translations.Add(new Translation(cleanType.Name, cleanType));
                    LookupTranslators.TranslateFields(obfType, cleanType, checkoffsets, this);
                }
            }
        }
    }
}
