// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Barbatos.Pallas.Architecture.Tests.FloatingPoint;

/// <summary>
/// Decodes a metadata signature to a single answer: does it mention a floating-point type anywhere, including
/// inside generic arguments, arrays, by-refs and pointers?
/// </summary>
internal sealed class FloatingPointSignatureProvider : ISignatureTypeProvider<bool, object?>
{
    public static readonly FloatingPointSignatureProvider Instance = new();

    private FloatingPointSignatureProvider()
    {
    }

    /// <summary>
    /// Binary floating-point types. <see cref="decimal"/> is deliberately not one of them: it is Pallas's exact
    /// arithmetic type (docs/PRECISION.md).
    /// </summary>
    public static bool IsFloatingPointType(string ns, string name)
    {
        return (ns, name) is ("System", "Double") or ("System", "Single") or ("System", "Half")
            or ("System", "MathF") or ("System.Numerics", "Complex");
    }

    public static bool EntityIsFloatingPoint(MetadataReader metadata, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
                TypeReference reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                return IsFloatingPointType(metadata.GetString(reference.Namespace), metadata.GetString(reference.Name));
            case HandleKind.TypeDefinition:
                TypeDefinition definition = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
                return IsFloatingPointType(metadata.GetString(definition.Namespace), metadata.GetString(definition.Name));
            case HandleKind.TypeSpecification:
                return metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(Instance, null);
            default:
                return false;
        }
    }

    public bool GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        return typeCode is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
    }

    public bool GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        return EntityIsFloatingPoint(reader, handle);
    }

    public bool GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        return EntityIsFloatingPoint(reader, handle);
    }

    public bool GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }

    public bool GetGenericInstantiation(bool genericType, ImmutableArray<bool> typeArguments)
    {
        return genericType || typeArguments.Contains(true);
    }

    public bool GetFunctionPointerType(MethodSignature<bool> signature)
    {
        return signature.ReturnType || signature.ParameterTypes.Contains(true);
    }

    public bool GetSZArrayType(bool elementType) => elementType;

    public bool GetArrayType(bool elementType, ArrayShape shape) => elementType;

    public bool GetByReferenceType(bool elementType) => elementType;

    public bool GetPointerType(bool elementType) => elementType;

    public bool GetPinnedType(bool elementType) => elementType;

    public bool GetModifiedType(bool modifier, bool unmodifiedType, bool isRequired) => unmodifiedType;

    public bool GetGenericMethodParameter(object? genericContext, int index) => false;

    public bool GetGenericTypeParameter(object? genericContext, int index) => false;
}
