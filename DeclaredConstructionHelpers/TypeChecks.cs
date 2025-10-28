global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Runtime.CompilerServices;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.TypeMappings;
global using static CSharp.NStar.NStarBuiltInFunctions;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;

namespace CSharp.NStar;

public static class TypeChecks
{
	public static bool ExtraTypeExists(BlockStack container, String typeName)
	{
		if (VariablesList.TryGetValue(container, out var list))
		{
			if (list.TryGetValue(typeName, out var type2))
				return TypeIsPrimitive(type2.MainType) && type2.MainType.Peek().Name == "typename" && type2.ExtraTypes.Length == 0;
			else
				return false;
		}
		if (UserDefinedPropertiesList.TryGetValue(container, out var list_))
		{
			if (list_.TryGetValue(typeName, out var a))
				return TypeIsPrimitive(a.UnvType.MainType) && a.UnvType.MainType.Peek().Name == "typename" && a.UnvType.ExtraTypes.Length == 0;
			else
				return false;
		}
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
		var index = OutdatedNamespacesList.IndexOfKey(@namespace);
		if (index != -1)
		{
			useInstead = OutdatedNamespacesList.Values[index];
			return true;
		}
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
		var index = OutdatedTypesList.IndexOfKey((@namespace, typeName));
		if (index != -1)
		{
			useInstead = OutdatedTypesList.Values[index];
			return true;
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String typeName)
	{
		if (ReservedTypesList.Contains((@namespace, typeName)))
			return true;
		return false;
	}

	public static bool IsNotImplementedEndOfIdentifier(String identifier, out String typeEnd)
	{
		foreach (var te in NotImplementedTypeEndsList)
		{
			if (identifier.EndsWith(te))
			{
				typeEnd = te;
				return true;
			}
		}
		typeEnd = [];
		return false;
	}

	public static bool IsOutdatedEndOfIdentifier(String identifier, out String useInstead, out String typeEnd)
	{
		foreach (var te in OutdatedTypeEndsList)
		{
			if (identifier.EndsWith(te.Key))
			{
				useInstead = te.Value;
				typeEnd = te.Key;
				return true;
			}
		}
		useInstead = [];
		typeEnd = [];
		return false;
	}

	public static bool IsReservedEndOfIdentifier(String identifier, out String typeEnd)
	{
		foreach (var te in ReservedTypeEndsList)
		{
			if (identifier.EndsWith(te))
			{
				typeEnd = te;
				return true;
			}
		}
		typeEnd = [];
		return false;
	}

	public static bool IsNotImplementedMember(BlockStack type, String member)
	{
		var index = NotImplementedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			if (NotImplementedMembersList.Values[index].Contains(member))
				return true;
		}
		return false;
	}

	public static bool IsOutdatedMember(BlockStack type, String member, out String useInstead)
	{
		var index = OutdatedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			var list = OutdatedMembersList.Values[index];
			var index2 = list.IndexOfKey(member);
			if (index2 != -1)
			{
				useInstead = list.Values[index2];
				return true;
			}
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedMember(BlockStack type, String member)
	{
		var index = ReservedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			if (ReservedMembersList.Values[index].Contains(member))
				return true;
		}
		return false;
	}

	public static bool TypeExists((BlockStack Container, String Type) containerType, out Type netType)
	{
		if (PrimitiveTypesList.TryGetValue(containerType.Type, out netType))
			return true;
		if (ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType))
			return true;
		Type? netType2 = null;
		if (containerType.Container.Length != 0)
			return false;
		if (ExplicitlyConnectedNamespacesList.FindIndex(x =>
			ExtraTypesList.TryGetValue((x, containerType.Type), out netType2)) < 0)
			return false;
		if (netType2 == null)
			return false;
		netType = netType2;
		return true;
	}
}
