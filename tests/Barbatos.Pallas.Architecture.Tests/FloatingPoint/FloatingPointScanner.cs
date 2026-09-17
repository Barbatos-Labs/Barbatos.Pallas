// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Barbatos.Pallas.Architecture.Tests.FloatingPoint;

/// <summary>
/// One place in a compiled assembly where binary floating point is used.
/// </summary>
/// <param name="Location">The type or member, e.g. <c>Namespace.Type::Method</c>.</param>
/// <param name="Kind">What was found: Field, Parameter, ReturnType, Local, Opcode, MemberReference, TypeToken, BaseType, Attribute.</param>
/// <param name="Detail">The opcode or referenced member, when there is one.</param>
internal sealed record FloatingPointUsage(string Location, string Kind, string Detail)
{
    public override string ToString()
    {
        return Detail.Length == 0 ? $"{Location} [{Kind}]" : $"{Location} [{Kind}: {Detail}]";
    }
}

/// <summary>
/// Finds every use of binary floating point - double, float, Half, MathF or System.Numerics.Complex - in a compiled
/// assembly, by reading metadata signatures and walking IL.
/// </summary>
/// <remarks>
/// Pallas computes exact decimal arithmetic with <c>decimal</c> and allows <c>double</c> only where docs/PRECISION.md
/// says so; this scanner is what confines it. BannedApiAnalyzers was measured to catch member use
/// (<c>Math.Sqrt(double)</c>, <c>MathF</c>, <c>double.Parse</c>, conversions) but NOT declarations: a <c>double</c>
/// field, parameter or local and a literal such as <c>1.5</c> all compiled without a diagnostic. Metadata and IL see all
/// of them, whatever the source looked like. <c>decimal</c> and the <c>decimal</c> and integer overloads of System.Math
/// (<c>Math.Round(decimal, int, MidpointRounding)</c>, <c>Math.Abs(long)</c>) are not reported.
/// </remarks>
internal static class FloatingPointScanner
{
    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    private static readonly HashSet<short> FloatingPointOpCodes =
    [
        OpCodes.Ldc_R4.Value,
        OpCodes.Ldc_R8.Value,
        OpCodes.Conv_R4.Value,
        OpCodes.Conv_R8.Value,
        OpCodes.Conv_R_Un.Value,
        OpCodes.Ldelem_R4.Value,
        OpCodes.Ldelem_R8.Value,
        OpCodes.Stelem_R4.Value,
        OpCodes.Stelem_R8.Value,
        OpCodes.Ldind_R4.Value,
        OpCodes.Ldind_R8.Value,
        OpCodes.Stind_R4.Value,
        OpCodes.Stind_R8.Value,
    ];

    public static IReadOnlyList<FloatingPointUsage> Scan(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        List<FloatingPointUsage> usages = [];

        foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
        {
            TypeDefinition type = metadata.GetTypeDefinition(typeHandle);
            string typeName = FullName(metadata, typeHandle);

            if (!type.BaseType.IsNil && FloatingPointSignatureProvider.EntityIsFloatingPoint(metadata, type.BaseType))
            {
                usages.Add(new(typeName, "BaseType", DescribeEntity(metadata, type.BaseType)));
            }

            foreach (InterfaceImplementationHandle interfaceHandle in type.GetInterfaceImplementations())
            {
                EntityHandle implemented = metadata.GetInterfaceImplementation(interfaceHandle).Interface;
                if (FloatingPointSignatureProvider.EntityIsFloatingPoint(metadata, implemented))
                {
                    usages.Add(new(typeName, "BaseType", DescribeEntity(metadata, implemented)));
                }
            }

            foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
            {
                FieldDefinition field = metadata.GetFieldDefinition(fieldHandle);
                if (field.DecodeSignature(FloatingPointSignatureProvider.Instance, null))
                {
                    usages.Add(new($"{typeName}.{metadata.GetString(field.Name)}", "Field", ""));
                }
            }

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                ScanMethod(peReader, metadata, typeName, methodHandle, usages);
            }
        }

        foreach (CustomAttributeHandle attributeHandle in metadata.CustomAttributes)
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(attributeHandle);
            if (MemberIsFloatingPoint(metadata, attribute.Constructor))
            {
                usages.Add(new("(custom attribute)", "Attribute", DescribeEntity(metadata, attribute.Constructor)));
            }
        }

        return usages;
    }

    private static void ScanMethod(PEReader peReader, MetadataReader metadata, string typeName, MethodDefinitionHandle methodHandle, List<FloatingPointUsage> usages)
    {
        MethodDefinition method = metadata.GetMethodDefinition(methodHandle);
        string location = $"{typeName}::{metadata.GetString(method.Name)}";

        MethodSignature<bool> signature = method.DecodeSignature(FloatingPointSignatureProvider.Instance, null);
        if (signature.ReturnType)
        {
            usages.Add(new(location, "ReturnType", ""));
        }

        if (signature.ParameterTypes.Contains(true))
        {
            usages.Add(new(location, "Parameter", ""));
        }

        if (method.RelativeVirtualAddress == 0)
        {
            return;
        }

        MethodBodyBlock body = peReader.GetMethodBody(method.RelativeVirtualAddress);
        if (!body.LocalSignature.IsNil)
        {
            ImmutableArray<bool> locals = metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(FloatingPointSignatureProvider.Instance, null);
            if (locals.Contains(true))
            {
                usages.Add(new(location, "Local", ""));
            }
        }

        ScanIl(metadata, body.GetILReader(), location, usages);
    }

    private static void ScanIl(MetadataReader metadata, BlobReader il, string location, List<FloatingPointUsage> usages)
    {
        while (il.RemainingBytes > 0)
        {
            byte first = il.ReadByte();
            short value = first == 0xFE ? unchecked((short)(0xFE00 | il.ReadByte())) : first;
            OpCode opCode = OpCodesByValue[value];

            if (FloatingPointOpCodes.Contains(value))
            {
                usages.Add(new(location, "Opcode", opCode.Name!));
            }

            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    il.Offset += 1;
                    break;
                case OperandType.InlineVar:
                    il.Offset += 2;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.ShortInlineR:
                    il.Offset += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    il.Offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    int targets = il.ReadInt32();
                    il.Offset += 4 * targets;
                    break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    EntityHandle token = MetadataTokens.EntityHandle(il.ReadInt32());
                    if (MemberIsFloatingPoint(metadata, token))
                    {
                        string kind = token.Kind is HandleKind.TypeReference or HandleKind.TypeSpecification or HandleKind.TypeDefinition ? "TypeToken" : "MemberReference";
                        usages.Add(new(location, kind, DescribeEntity(metadata, token)));
                    }

                    break;
                default:
                    throw new NotSupportedException($"Unexpected IL operand type {opCode.OperandType} in {location}.");
            }
        }
    }

    private static bool MemberIsFloatingPoint(MetadataReader metadata, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference reference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                if (FloatingPointSignatureProvider.EntityIsFloatingPoint(metadata, reference.Parent))
                {
                    return true;
                }

                if (reference.GetKind() == MemberReferenceKind.Method)
                {
                    MethodSignature<bool> signature = reference.DecodeMethodSignature(FloatingPointSignatureProvider.Instance, null);
                    return signature.ReturnType || signature.ParameterTypes.Contains(true);
                }

                return reference.DecodeFieldSignature(FloatingPointSignatureProvider.Instance, null);

            case HandleKind.MethodSpecification:
                MethodSpecification specification = metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                return specification.DecodeSignature(FloatingPointSignatureProvider.Instance, null).Contains(true)
                    || MemberIsFloatingPoint(metadata, specification.Method);

            case HandleKind.TypeReference:
            case HandleKind.TypeSpecification:
            case HandleKind.TypeDefinition:
                return FloatingPointSignatureProvider.EntityIsFloatingPoint(metadata, handle);

            default:
                // Method and field definitions of this assembly are scanned where they are declared.
                return false;
        }
    }

    private static string DescribeEntity(MetadataReader metadata, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeReference => TypeReferenceName(metadata, (TypeReferenceHandle)handle),
            HandleKind.TypeDefinition => FullName(metadata, (TypeDefinitionHandle)handle),
            HandleKind.TypeSpecification => "generic instantiation over a floating-point type",
            HandleKind.MemberReference => DescribeMemberReference(metadata, (MemberReferenceHandle)handle),
            HandleKind.MethodSpecification => DescribeEntity(metadata, metadata.GetMethodSpecification((MethodSpecificationHandle)handle).Method),
            _ => handle.Kind.ToString(),
        };
    }

    private static string DescribeMemberReference(MetadataReader metadata, MemberReferenceHandle handle)
    {
        MemberReference reference = metadata.GetMemberReference(handle);
        string parent = reference.Parent.Kind switch
        {
            HandleKind.TypeReference => TypeReferenceName(metadata, (TypeReferenceHandle)reference.Parent),
            HandleKind.TypeDefinition => FullName(metadata, (TypeDefinitionHandle)reference.Parent),
            _ => "?",
        };

        return $"{parent}::{metadata.GetString(reference.Name)}";
    }

    private static string TypeReferenceName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        TypeReference reference = metadata.GetTypeReference(handle);
        string ns = metadata.GetString(reference.Namespace);
        return ns.Length == 0 ? metadata.GetString(reference.Name) : $"{ns}.{metadata.GetString(reference.Name)}";
    }

    private static string FullName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        TypeDefinition type = metadata.GetTypeDefinition(handle);
        string name = metadata.GetString(type.Name);
        TypeDefinitionHandle declaring = type.GetDeclaringType();
        if (!declaring.IsNil)
        {
            return $"{FullName(metadata, declaring)}+{name}";
        }

        string ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : $"{ns}.{name}";
    }
}
