global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Runtime.CompilerServices;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.NStarUtilityFunctions;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static CSharp.NStar.TypeMappings;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;

namespace CSharp.NStar;

public static class TypeChecks
{
	public static bool ExtraTypeExists(BlockStack container, String typeName)
	{
		if (VariablesList.TryGetValue(container, out var containerVariables)
			&& containerVariables.TryGetValue(typeName, out var variableName))
			return TypeIsPrimitive(variableName.MainType) && variableName.MainType.Peek().Name == "typename"
				&& variableName.ExtraTypes.Length == 0;
		if (UserDefinedPropertiesList.TryGetValue(container, out var containerProperties)
			&& containerProperties.TryGetValue(typeName, out var a))
			return TypeIsPrimitive(a.UnvType.MainType) && a.UnvType.MainType.Peek().Name == "typename"
				&& a.UnvType.ExtraTypes.Length == 0;
		return false;
	}

	public static bool IsNotImplementedNamespace(String @namespace)
	{
		if (NotImplementedNamespacesList.Contains(@namespace))
			return true;
		return false;
	}

	public static bool IsOutdatedNamespace(String @namespace, out String useInstead)
	{
		if (OutdatedNamespacesList.TryGetValue(@namespace, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedNamespace(String @namespace)
	{
		if (ReservedNamespacesList.Contains(@namespace))
			return true;
		return false;
	}

	public static bool IsNotImplementedType(String @namespace, String typeName)
	{
		if (NotImplementedTypesList.Contains((@namespace, typeName)))
			return true;
		return false;
	}

	public static bool IsOutdatedType(String @namespace, String typeName, out String useInstead)
	{
		if (OutdatedTypesList.TryGetValue((@namespace, typeName), out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String typeName)
	{
		if (ReservedTypesList.Contains((@namespace, typeName)))
			return true;
		return false;
	}

	public static bool IsNotImplementedEndOfIdentifier(String identifier, out String wrongEnd)
	{
		foreach (var typeEnd in NotImplementedTypeEndsList)
		{
			if (identifier.EndsWith(typeEnd))
			{
				wrongEnd = typeEnd;
				return true;
			}
		}
		wrongEnd = [];
		return false;
	}

	public static bool IsOutdatedEndOfIdentifier(String identifier, out String wrongEnd, out String useInstead)
	{
		foreach (var typeEnd in OutdatedTypeEndsList)
		{
			if (identifier.EndsWith(typeEnd.Key))
			{
				useInstead = typeEnd.Value;
				wrongEnd = typeEnd.Key;
				return true;
			}
		}
		useInstead = [];
		wrongEnd = [];
		return false;
	}

	public static bool IsReservedEndOfIdentifier(String identifier, out String wrongEnd)
	{
		foreach (var typeEnd in ReservedTypeEndsList)
		{
			if (identifier.EndsWith(typeEnd))
			{
				wrongEnd = typeEnd;
				return true;
			}
		}
		wrongEnd = [];
		return false;
	}

	public static bool IsNotImplementedMember(BlockStack type, String member)
	{
		if (NotImplementedMembersList.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member))
			return true;
		return false;
	}

	public static bool IsOutdatedMember(BlockStack type, String member, out String useInstead)
	{
		if (OutdatedMembersList.TryGetValue(type, out var containerMembers)
			&& containerMembers.TryGetValue(member, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedMember(BlockStack type, String member)
	{
		if (ReservedMembersList.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member))
			return true;
		return false;
	}

	public static bool TypeExists((BlockStack Container, String Type) containerType, [MaybeNullWhen(false)] out Type netType)
	{
		if (PrimitiveTypesList.TryGetValue(containerType.Type, out netType))
			return true;
		if (ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType))
			return true;
		Type? preservedNetType = null;
		if (containerType.Container.Length != 0)
			return false;
		if (ExplicitlyConnectedNamespacesList.FindIndex(x =>
			ExtraTypesList.TryGetValue((x, containerType.Type), out preservedNetType)) < 0)
			return false;
		if (preservedNetType == null)
			return false;
		netType = preservedNetType;
		return true;
	}
}
