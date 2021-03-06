﻿using System;

using PInvoke.Common.Models;

namespace PInvoke.Common.Generators.CSharp
{
    using Type = Models.Type;

    public enum CSharpPointerMode
    {
        Unsafe,
        IntPtr,
        Types
    }

    public enum CSharpEnumerationMode
    {
        Enumerations,
        Constants
    }

    public abstract class CSharpGenerator<T> : Generator<T>
    {
        public CSharpModifier Modifier { get; set; } = CSharpModifier.None;
        public bool UseFullTypes { get; set; } = false;
        public CSharpPointerMode PointerMode { get; set; } = CSharpPointerMode.Types;
        public CSharpEnumerationMode EnumerationMode { get; set; } = CSharpEnumerationMode.Enumerations;
        public bool PreferStringBuilders { get; set; } = false;

        public CSharpGenerator()
        {
        }
        public CSharpGenerator(GenerationParameters generationParameters)
        {
            Modifier = Enum.TryParse(generationParameters.GetValue<string>("Modifier", ""), out CSharpModifier _modifier) ? _modifier : Modifier;
            UseFullTypes = generationParameters.GetValue<bool>("UseFullTypes", UseFullTypes);
            PointerMode = Enum.TryParse(generationParameters.GetValue<string>("PointerMode", ""), out CSharpPointerMode _pointerMode) ? _pointerMode : PointerMode;
            EnumerationMode = Enum.TryParse(generationParameters.GetValue<string>("EnumerationMode", ""), out CSharpEnumerationMode _enumerationMode) ? _enumerationMode : EnumerationMode;
            PreferStringBuilders = generationParameters.GetValue<bool>("PreferStringBuilders", PreferStringBuilders);
        }

        protected string GetType(ParsedType type)
        {
            return GetTypeInternal(type.Parsed);
        }
        protected string GetFinalType(ParsedType type)
        {
            return type.Raw
                .Replace("CONST ", "")
                .Replace("*", "")
                .Replace("IN ", "")
                .Replace("INOUT ", "")
                .Replace("OUT ", "");
        }

        private string GetTypeInternal(Type type, bool byRef = false)
        {
            switch (type)
            {
                case PointerType pointerType:
                    {
                        if (PointerMode == CSharpPointerMode.Unsafe)
                            return (GetTypeInternal(pointerType.Target) ?? "void") + "*";
                        else if (PointerMode == CSharpPointerMode.IntPtr)
                            return UseFullTypes ? "System.IntPtr" : "IntPtr";
                        else
                        {
                            if (pointerType.Target is StringType)
                                return "string";
                            if (pointerType.Target is PointerType subPointerType && subPointerType.Target is StringType)
                                return "out string";

                            if (pointerType.Target is BasicType basicType && basicType.Type == BasicTypeValues.Void)
                                return UseFullTypes ? "System.IntPtr" : "IntPtr";

                            if (byRef)
                                return GetTypeInternal(pointerType.Target, true);
                            else
                                return "ref " + GetTypeInternal(pointerType.Target, true);
                        }
                    }

                case HandleType handleType:
                    {
                        return UseFullTypes ? "System.IntPtr" : "IntPtr";
                    }

                case ConstType constType:
                    {
                        if (constType.Target is PointerType pointerType && PointerMode == CSharpPointerMode.Types)
                            return GetTypeInternal(pointerType.Target);
                        else
                            return GetTypeInternal(constType.Target);
                    }

                case InType inType:
                    return GetTypeInternal(inType.Target);
                case OutType outType:
                    return GetTypeInternal(outType.Target);
                case InOutType inOutType:
                    return GetTypeInternal(inOutType.Target);

                case BasicType basicType:
                    switch (basicType.Type)
                    {
                        case BasicTypeValues.Boolean: return "bool";
                        case BasicTypeValues.Byte: return "byte";
                        case BasicTypeValues.SByte: return "sbyte";
                        case BasicTypeValues.UShort: return "ushort";
                        case BasicTypeValues.Short: return "short";
                        case BasicTypeValues.UInteger: return "uint";
                        case BasicTypeValues.Integer: return "int";
                        case BasicTypeValues.ULong: return "ulong";
                        case BasicTypeValues.Long: return "long";
                        case BasicTypeValues.Float: return "float";
                        case BasicTypeValues.Double: return "double";
                    }
                    break;

                case UnknownType unknownType:
                    {
                        if (PointerMode == CSharpPointerMode.IntPtr)
                            return UseFullTypes ? "System.IntPtr" : "IntPtr";
                        else
                            return unknownType.Name;
                    }
            }

            return null;
        }
    }
}