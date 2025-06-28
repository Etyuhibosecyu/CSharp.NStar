global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Runtime.CompilerServices;
global using G = System.Collections.Generic;
global using static CSharp.NStar.DeclaredConstructionChecks;
global using static CSharp.NStar.DeclaredConstructionMappings;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.IntermediateFunctions;
global using static CSharp.NStar.TypeHelpers;
global using static NStar.Core.Extents;
global using static System.Math;
global using String = NStar.Core.String;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
		if (!TypeExists(containerType, out var netType))
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

	public static bool MethodExists(UniversalType container, String name)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
			return false;
		if (!netType.TryWrap(x => x.GetMethod(name.ToString()), out var method))
			method = netType.GetMethods().Find(x => x.Name == name.ToString());
		if (method == null)
			return false;
		return true;
	}

	public static bool MethodExists(UniversalType container, String name, List<UniversalType> callParameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverload? function)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			function = null;
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(x => TypeMapping(x));
		if (!(netType.TryWrap(x => x.GetMethod(name.ToString(), callParameterNetTypes), out var method) && method != null))
			method = netType.GetMethods().Find(x => IsValidMethod(name, x, callParameterNetTypes));
		if (method == null)
		{
			function = null;
			return false;
		}
		var genericArguments = method.GetGenericArguments();
		var patterns = GetReplacementPatterns(genericArguments, callParameterNetTypes);
		var returnNetType = method.ReturnType;
		var parameters = method.GetParameters();
		var functionParameterTypes = parameters.ToArray(x => x.ParameterType);
		for (var i = 0; i < patterns.Length; i++)
		{
			returnNetType = ReplaceExtraNetType(returnNetType, patterns[i]);
			for (var j = 0; j < functionParameterTypes.Length; j++)
				functionParameterTypes[j] = ReplaceExtraNetType(functionParameterTypes[j], patterns[i]);
		}
		function = new([], TypeMappingBack(returnNetType, netType.GetGenericArguments(), container.ExtraTypes),
			(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
			new(functionParameterTypes.ToList((x, index) => new GeneralMethodParameter(TypeMappingBack(x,
			netType.GetGenericArguments(), container.ExtraTypes), parameters[index].Name ?? "x",
			(parameters[index].IsOptional ? ParameterAttributes.Optional : 0)
			| (parameters[index].ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (parameters[index].IsOut ? ParameterAttributes.Out : 0),
			parameters[index].DefaultValue?.ToString() ?? "null"))));
		return true;
	}

	private static bool IsValidMethod(String name, MethodInfo x, Type[] callParameterNetTypes)
	{
		if (x.Name != name.ToString())
			return false;
		if (CreateVar(x.GetParameters(), out var functionParameters).Length < callParameterNetTypes.Length)
			return false;
		if (!functionParameters.Skip(callParameterNetTypes.Length).All(y => y.IsOptional))
			return false;
		if (x.Name == nameof(name.AddRange) && functionParameters.Length == 1)
		{
			if (functionParameters[0].ParameterType.Name != "List`1")
				return false;
			var genericArguments = functionParameters[0].ParameterType.GetGenericArguments();
			if (genericArguments.Length != 1)
				return false;
			var listType = typeof(List<>).MakeGenericType(genericArguments);
			if (!functionParameters[0].ParameterType.Equals(listType))
				return false;
			return true;
		}
		return (functionParameters, callParameterNetTypes).Combine().All(IsValidParameter);
	}

	private static bool IsValidParameter((ParameterInfo, Type) y)
	{
		var genericArguments = y.Item2.GetGenericArguments();
		Type destType;
		if (y.Item1.ParameterType.IsGenericParameter)
			destType = genericArguments.Length == 0 ? y.Item2 : genericArguments[0];
		else if (y.Item1.ParameterType.IsSZArray)
			destType = (genericArguments.Length == 0 ? y.Item2 : genericArguments[0]).MakeArrayType();
		else if (y.Item1.ParameterType.ContainsGenericParameters)
		{
			if (genericArguments.Length == 0 || typeof(ITuple).IsAssignableFrom(y.Item2))
				genericArguments = [y.Item2];
			destType = y.Item1.ParameterType.GetGenericTypeDefinition().MakeGenericType(genericArguments);
		}
		else
			destType = y.Item1.ParameterType;
		if (destType.IsAssignableFromExt(y.Item2))
			return true;
		return false;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, List<UniversalType> parameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverload? function, out bool user)
	{
		if (PublicFunctionsList.TryGetValue(name, out var functionOverload))
		{
			function = new([], PartialTypeToGeneralType(functionOverload.ExtraTypes.Contains(functionOverload.ReturnType)
				? parameterTypes[functionOverload.Parameters.FindIndex(x =>
				functionOverload.ReturnType == x.Type || x.ExtraTypes.Contains(functionOverload.ReturnType))].ToString()
				: functionOverload.ReturnType, functionOverload.ReturnExtraTypes.ToList(t =>
				functionOverload.ExtraTypes.Contains(t) ? parameterTypes[functionOverload.Parameters.FindIndex(x =>
				t == x.Type || x.ExtraTypes.Contains(t))].ToString() : t)),
				functionOverload.Attributes, [.. functionOverload.Parameters.Convert((x, index) =>
				new GeneralMethodParameter(functionOverload.ExtraTypes.Contains(x.Type)
				? parameterTypes[index] : PartialTypeToGeneralType(x.Type, x.ExtraTypes),
				x.Name, x.Attributes, x.DefaultValue))]);
			user = false;
			return true;
		}
		if (!(UserDefinedFunctionsList.TryGetValue(container, out var methods)
			&& methods.TryGetValue(name, out var method_overloads)))
		{
			if (GeneralMethodsList.TryGetValue(container, out var list) && list.TryGetValue(name, out var overloads))
			{
				function = overloads[0];
				user = false;
				return true;
			}
			function = null;
			user = false;
			return false;
		}
		function = method_overloads[0];
		var arrayParameters = function.Value.ArrayParameters;
		for (var i = 0; i < arrayParameters.Length; i++)
		{
			var x = arrayParameters[i];
			if (!(!x.ArrayParameterPackage && x.ArrayParameterType.Length == 1
				&& x.ArrayParameterType.Peek().BlockType == BlockType.Extra && parameterTypes.Length > i))
				continue;
			function = new([], ReplaceExtraType(function.Value.ReturnUnvType, x.ArrayParameterType.Peek().Name,
				parameterTypes[i]), function.Value.Attributes, [.. function.Value.Parameters.Convert(y =>
				new GeneralMethodParameter(ReplaceExtraType(y.Type, x.ArrayParameterType.Peek().Name,
				parameterTypes[i]), y.Name, y.Attributes, y.DefaultValue))]);
		}
		user = true;
		return true;
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out var matchingContainer) && UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var method_overloads))
			return true;
		else if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(userDefinedType.BaseType, name))
				return true;
			else if (UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name))
				return true;
		}
		return false;
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name, List<UniversalType> parameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverload? function) =>
		UserDefinedFunctionExists(container, name, parameterTypes, out function, out _, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, List<UniversalType> parameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverload? function,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer)
			&& UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var method_overloads))
		{
			function = method_overloads[0];
			derived = false;
			return true;
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(userDefinedType.BaseType, name, parameterTypes, out function))
			{
				derived = true;
				return true;
			}
			else if (UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name, parameterTypes,
				out function, out matchingContainer, out derived))
				return true;
		}
		function = null;
		derived = false;
		return false;
	}

	public static bool UserDefinedNonDerivedFunctionExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out GeneralMethodOverload? function,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer) && UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var method_overloads))
		{
			function = method_overloads[0];
			return true;
		}
		function = null;
		return false;
	}

	public static bool ConstructorsExist(UniversalType container, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
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
			new GeneralMethodParameter(TypeMappingBack(!CreateVar(y.GetCustomAttributes(typeof(ParamArrayAttribute),
			false).Length > 0, out var @params) ? y.ParameterType : y.ParameterType.IsSZArray
			? y.ParameterType.GetElementType() ?? typeof(object) : y.ParameterType.GetGenericArguments()[0],
			netType.GetGenericArguments(), container.ExtraTypes), y.Name ?? "x",
			(y.IsOptional ? ParameterAttributes.Optional : 0)
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
