global using System;
global using System.Diagnostics.CodeAnalysis;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using String = NStar.Core.String;

namespace CSharp.NStar;

public static class TypeChecks
{
	public static bool ExtraTypeExists(BlockStack container, String typeName)
	{
		if (UserDefinedConstants.TryGetValue(container, out var containerConstants)
			&& containerConstants.TryGetValue(typeName, out var constantType))
			return TypeIsPrimitive(constantType.NStarType.MainType) && constantType.NStarType.MainType.Peek().Name == "typename"
				&& constantType.NStarType.ExtraTypes.Length == 0;
		if (Variables.TryGetValue(container, out var containerVariables)
			&& containerVariables.TryGetValue(typeName, out var variableName))
			return TypeIsPrimitive(variableName.MainType) && variableName.MainType.Peek().Name == "typename"
				&& variableName.ExtraTypes.Length == 0;
		if (UserDefinedProperties.TryGetValue(container, out var containerProperties)
			&& containerProperties.TryGetValue(typeName, out var a))
			return TypeIsPrimitive(a.NStarType.MainType) && a.NStarType.MainType.Peek().Name == "typename"
				&& a.NStarType.ExtraTypes.Length == 0;
		return false;
	}

	public static bool IsNotImplementedNamespace(String @namespace)
	{
		if (NotImplementedNamespaces.Contains(@namespace))
			return true;
		return false;
	}

	public static bool IsOutdatedNamespace(String @namespace, out String useInstead)
	{
		if (OutdatedNamespaces.TryGetValue(@namespace, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedNamespace(String @namespace)
	{
		if (ReservedNamespaces.Contains(@namespace))
			return true;
		return false;
	}

	public static bool IsNotImplementedType(String @namespace, String typeName)
	{
		if (NotImplementedTypes.Contains((@namespace, typeName)))
			return true;
		return false;
	}

	public static bool IsOutdatedType(String @namespace, String typeName, out String useInstead)
	{
		if (OutdatedTypes.TryGetValue((@namespace, typeName), out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String typeName)
	{
		if (ReservedTypes.Contains((@namespace, typeName)))
			return true;
		return false;
	}

	public static bool IsNotImplementedEndOfIdentifier(String identifier, out String wrongEnd)
	{
		foreach (var typeEnd in NotImplementedTypeEnds)
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
		foreach (var typeEnd in OutdatedTypeEnds)
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
		foreach (var typeEnd in ReservedTypeEnds)
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
		if (NotImplementedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member))
			return true;
		return false;
	}

	public static bool IsOutdatedMember(BlockStack type, String member, out String useInstead)
	{
		if (OutdatedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.TryGetValue(member, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedMember(BlockStack type, String member)
	{
		if (ReservedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member))
			return true;
		return false;
	}

	public static bool TypeExists((BlockStack Container, String Type) containerType, [MaybeNullWhen(false)] out Type netType)
	{
		if (PrimitiveTypes.TryGetValue(containerType.Type, out netType))
			return true;
		if (ExtraTypes.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType))
			return true;
		Type? preservedNetType = null;
		if (containerType.Container.Length != 0)
			return false;
		if (ExplicitlyConnectedNamespaces.FindIndex(x =>
			ExtraTypes.TryGetValue((x, containerType.Type), out preservedNetType)) < 0)
			return false;
		if (preservedNetType == null)
			return false;
		netType = preservedNetType;
		return true;
	}
}
