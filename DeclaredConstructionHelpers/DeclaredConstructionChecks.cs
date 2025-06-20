﻿global using Corlib.NStar;
global using LINQ.NStar;
global using MathLib.NStar;
global using System;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.DeclaredConstructionMappings;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.IntermediateFunctions;
global using static CSharp.NStar.TypeHelpers;
global using static System.Math;
global using String = Corlib.NStar.String;
using System.Diagnostics.CodeAnalysis;

namespace CSharp.NStar;

public static class DeclaredConstructionChecks
{
	public static bool CheckContainer(BlockStack container, Func<BlockStack, bool> check, out BlockStack type)
	{
		if (check(container))
		{
			type = container;
			return true;
		}
		var list = container.ToList().GetSlice();
		BlockStack stack;
		while (list.Any())
		{
			list = list.SkipLast(1);
			if (check(stack = new(list)))
			{
				type = stack;
				return true;
			}
		}
		type = new();
		return false;
	}

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

	public static bool PropertyExists(UniversalType container, String name, [MaybeNullWhen(false)]
		out UserDefinedProperty? property)
	{
		if (UserDefinedPropertiesList.TryGetValue(container.MainType, out var list) && list.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& PropertyExists(userDefinedType.BaseType, name, out property))
			return true;
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var netType)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType)
			|| containerType.Container.Length == 0 && ExplicitlyConnectedNamespacesList.FindIndex(x =>
			ExtraTypesList.TryGetValue((x, containerType.Type), out netType)) >= 0))
		{
			property = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetProperty(name.ToString()), out var netProperty))
			netProperty = netType.GetProperties().Find(x => x.Name == name.ToString());
		if (netProperty == null)
		{
			property = null;
			return false;
		}
		property = new(TypeMappingBack(netProperty.PropertyType, netType.GetGenericArguments(), container.ExtraTypes),
			PropertyAttributes.None, "null");
		return true;
	}

	public static bool UserDefinedPropertyExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out UserDefinedProperty? property, [MaybeNullWhen(false)] out BlockStack matchingContainer,
		[MaybeNullWhen(false)] out bool inBase)
	{
		UserDefinedType userDefinedType = default!;
		if (CheckContainer(container, UserDefinedPropertiesList.ContainsKey, out matchingContainer)
			&& UserDefinedPropertiesList[matchingContainer].TryGetValue(name, out var value))
		{
			property = value;
			inBase = false;
			return true;
		}
		else if (CheckContainer(container, x => UserDefinedTypesList.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && PropertyExists(userDefinedType.BaseType, name, out property))
		{
			inBase = true;
			return true;
		}
		property = null;
		inBase = false;
		return false;
	}

	public static List<G.KeyValuePair<String, UserDefinedProperty>> GetAllProperties(BlockStack container)
	{
		List<G.KeyValuePair<String, UserDefinedProperty>> result = [];
		if (UserDefinedPropertiesList.TryGetValue(container, out var list))
			foreach (var item in list)
				result.Add(item);
		if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType))
			result.AddRange(GetAllProperties(userDefinedType.BaseType.MainType));
		return result;
	}

	public static bool PublicFunctionExists(String name, out (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		if (PublicFunctionsList.TryGetValue(name, out var function2))
		{
			function = function2;
			return true;
		}
		function = null;
		return false;
	}

	public static bool MethodExists(UniversalType container, String name, [MaybeNullWhen(false)]
	out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes,
		GeneralMethodParameters Parameters)? function)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var netType)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out netType)) >= 0))
		{
			function = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetMethod(name.ToString()), out var method))
			method = netType.GetMethods().Find(x => x.Name == name.ToString());
		if (method == null)
		{
			function = null;
			return false;
		}
		function = ([], TypeMappingBack(method.ReturnType, netType.GetGenericArguments(), container.ExtraTypes),
			(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
			new(method.GetParameters().ToList(x => new GeneralMethodParameter(CreateVar(TypeMappingBack(x.ParameterType,
			netType.GetGenericArguments(), container.ExtraTypes), out var UnvType).MainType,
			x.Name ?? "x", UnvType.ExtraTypes,
			(x.IsOptional ? ParameterAttributes.Optional : 0) | (x.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (x.IsOut ? ParameterAttributes.Out : 0), x.DefaultValue?.ToString() ?? "null"))));
		return true;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, [MaybeNullWhen(false)]
	out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes,
		GeneralMethodParameters Parameters)? function, out bool user)
	{
		if (UserDefinedFunctionsList.TryGetValue(container, out var methods) && methods.TryGetValue(name, out var method_overloads))
		{
			function = method_overloads[0];
			user = true;
			return true;
		}
		var index = GeneralMethodsList.IndexOfKey(container);
		if (index != -1)
		{
			var list = GeneralMethodsList.Values[index];
			var index2 = list.IndexOfKey(name);
			if (index2 != -1)
			{
				function = list.Values[index2][0];
				user = false;
				return true;
			}
		}
		function = null;
		user = false;
		return false;
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType,
		FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function) =>
		UserDefinedFunctionExists(container, name, out function, out _, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType,
		FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer) && UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var method_overloads))
		{
			function = method_overloads[0];
			derived = false;
			return true;
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(userDefinedType.BaseType, name, out function))
			{
				derived = true;
				return true;
			}
			else if (UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name, out function, out matchingContainer,
				out derived))
				return true;
		}
		function = null;
		derived = false;
		return false;
	}

	public static bool ConstructorsExist(UniversalType container, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var netType)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out netType)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out netType)) >= 0))
		{
			constructors = null;
			return false;
		}
		var typeConstructors = netType.GetConstructors();
		if (typeConstructors == null)
		{
			constructors = null;
			return false;
		}
		constructors = [.. typeConstructors.ToList(x => ((x.IsAbstract ? ConstructorAttributes.Abstract : 0)
			| (x.IsStatic ? ConstructorAttributes.Static : 0), new GeneralMethodParameters(x.GetParameters().ToList(y =>
			new GeneralMethodParameter(CreateVar(TypeMappingBack(!CreateVar(y.GetCustomAttributes(typeof(ParamArrayAttribute),
			false).Length > 0, out var @params) ? y.ParameterType : y.ParameterType.IsSZArray
			? y.ParameterType.GetElementType() ?? typeof(object) : y.ParameterType.GetGenericArguments()[0],
			netType.GetGenericArguments(), container.ExtraTypes),
			out var UnvType).MainType, y.Name ?? "x", UnvType.ExtraTypes, (y.IsOptional ? ParameterAttributes.Optional : 0)
			| (y.ParameterType.IsByRef ? ParameterAttributes.Ref : 0) | (y.IsOut ? ParameterAttributes.Out : 0)
			| (@params ? ParameterAttributes.Params : 0), y.DefaultValue?.ToString() ?? "null")))))];
		return true;
	}

	public static bool UserDefinedConstructorsExist(UniversalType container, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		if (UserDefinedConstructorsList.TryGetValue(container.MainType, out var temp_constructors)
			&& !(UserDefinedTypesList.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& (userDefinedType.Attributes & (TypeAttributes.Struct | TypeAttributes.Static))
			is not 0 or TypeAttributes.Sealed or TypeAttributes.Struct))
		{
			constructors = [.. temp_constructors, .. ConstructorsExist(userDefinedType.BaseType, out var baseConstructors)
				? baseConstructors : [], .. UserDefinedConstructorsExist(userDefinedType.BaseType, out baseConstructors)
				? baseConstructors : []];
			if (constructors.Length != 0)
				return true;
		}
		constructors = null;
		return false;
	}
}
