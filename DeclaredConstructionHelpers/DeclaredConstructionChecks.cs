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
using NStar.MathLib.Extras;
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

	public static bool ConstantExists(UniversalType container, String name, [MaybeNullWhen(false)]
		out UserDefinedConstant? constant)
	{
		if (UserDefinedConstantsList.TryGetValue(container.MainType, out var list) && list.TryGetValue(name, out var a))
		{
			constant = a;
			return true;
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& ConstantExists(userDefinedType.BaseType, name, out constant))
			return true;
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			constant = null;
			return false;
		}
		var netProperty = netType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
			.Find(x => x.IsLiteral && x.IsInitOnly && x.Name == name.ToString());
		if (netProperty == null)
		{
			constant = null;
			return false;
		}
		constant = new(TypeMappingBack(netProperty.FieldType, netType.GetGenericArguments(), container.ExtraTypes),
			ConstantAttributes.None, new("null", 0, []));
		return true;
	}

	public static bool UserDefinedConstantExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out UserDefinedConstant? constant, [MaybeNullWhen(false)] out BlockStack matchingContainer,
		[MaybeNullWhen(false)] out bool inBase)
	{
		UserDefinedType userDefinedType = default!;
		if (CheckContainer(container, UserDefinedConstantsList.ContainsKey, out matchingContainer)
			&& UserDefinedConstantsList[matchingContainer].TryGetValue(name, out var value))
		{
			constant = value;
			inBase = false;
			return true;
		}
		else if (CheckContainer(container, x => UserDefinedTypesList.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && ConstantExists(userDefinedType.BaseType, name, out constant))
		{
			inBase = true;
			return true;
		}
		constant = null;
		inBase = false;
		return false;
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
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			functions = [];
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(x => TypeMapping(x));
		var validity = int.MinValue;
		var methods = netType.GetMethods().FindAllMax(x =>
		{
			var currentValidity = GetMethodValidity(name, x, callParameterNetTypes);
			if (currentValidity > validity)
				validity = currentValidity;
			return currentValidity;
		});
		functions = [];
		if (validity < 0)
			return false;
		foreach (var method in methods)
		{
			if (Attribute.IsDefined(method, typeof(ObsoleteAttribute)))
				continue;
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
			functions.Add(new([], TypeMappingBack(returnNetType, netType.GetGenericArguments(), container.ExtraTypes),
				(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
				new(functionParameterTypes.ToList((x, index) => new GeneralMethodParameter(TypeMappingBack(x,
				netType.GetGenericArguments(), container.ExtraTypes), parameters[index].Name ?? "x",
				(parameters[index].IsOptional ? ParameterAttributes.Optional : 0)
				| (parameters[index].ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
				| (parameters[index].IsOut ? ParameterAttributes.Out : 0)
				| (Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? ParameterAttributes.Params : 0),
				parameters[index].DefaultValue?.ToString() ?? "null")))));
		}
		return true;
	}

	private static int GetMethodValidity(String? name, MethodBase x, Type[] callParameterNetTypes)
	{
		if (name != null && x.Name != name.ToString())
			return int.MinValue;
		var obsolete = x.GetCustomAttribute<ObsoleteAttribute>(false);
		if (obsolete != null && obsolete.IsError)
			return 0;
		if (CreateVar(x.GetParameters(), out var functionParameters).Length < callParameterNetTypes.Length)
			return 0;
		if (!functionParameters.Skip(callParameterNetTypes.Length).All(y => y.IsOptional))
			return 0;
		if (x.Name == nameof(name.AddRange) && functionParameters.Length == 1)
		{
			if (functionParameters[0].ParameterType.Name != "List`1")
				return 0;
			var genericArguments = functionParameters[0].ParameterType.GetGenericArguments();
			if (genericArguments.Length != 1)
				return 0;
			var listType = typeof(List<>).MakeGenericType(genericArguments);
			if (!functionParameters[0].ParameterType.Equals(listType))
				return 0;
			return functionParameters.Length;
		}
		var index = (functionParameters, callParameterNetTypes).Combine().FindIndex(x => !IsValidParameter(x));
		return index >= 0 ? index : functionParameters.Length;
	}

	private static bool IsValidParameter((ParameterInfo, Type) x)
	{
		var genericArguments = x.Item2.GetGenericArguments();
		Type destType;
		if (x.Item1.ParameterType.IsGenericParameter)
		{
			if (genericArguments.Length != 0)
				destType = genericArguments[0];
			else if (x.Item2 == typeof(void))
				return true;
			else
				destType = x.Item2;
		}
		else if (x.Item1.ParameterType.IsSZArray)
		{
			if (genericArguments.Length != 0)
				destType = genericArguments[0].MakeArrayType();
			else if (x.Item2 == typeof(void))
				return true;
			else
				destType = x.Item2.MakeArrayType();
		}
		else if (x.Item1.ParameterType.ContainsGenericParameters)
		{
			if (x.Item2 == typeof(void))
				return true;
			else if (genericArguments.Length == 0 || typeof(ITuple).IsAssignableFrom(x.Item2))
				genericArguments = [x.Item2];
			if (x.Item1.ParameterType.GetGenericArguments().Length != genericArguments.Length)
				return false;
			destType = x.Item1.ParameterType.GetGenericTypeDefinition().MakeGenericType(genericArguments);
		}
		else
			destType = x.Item1.ParameterType;
		if (x.Item2 == typeof(void))
			return true;
		if (destType.IsAssignableFromExt(x.Item2))
			return true;
		return false;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, List<UniversalType> parameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions, out bool user)
	{
		if (PublicFunctionsList.TryGetValue(name, out var functionOverload))
		{
			BlockStack? mainType;
			if (functionOverload.ExtraTypes.Contains(functionOverload.ReturnType))
				mainType = FindParameter(functionOverload.ReturnType).MainType;
			else
				mainType = GetBlockStack(functionOverload.ReturnType);
			GeneralExtraTypes extraTypes = new(functionOverload.ReturnExtraTypes.ToList(GetUniversalType));
			functions = [new([], new(mainType, extraTypes),
				functionOverload.Attributes, [.. functionOverload.Parameters.Convert((x, index) =>
				new GeneralMethodParameter(functionOverload.ExtraTypes.Contains(x.Type)
				? parameterTypes[index] : PartialTypeToGeneralType(x.Type, x.ExtraTypes),
				x.Name, x.Attributes, x.DefaultValue))])];
			user = false;
			return true;
		}
		if (!(UserDefinedFunctionsList.TryGetValue(container, out var methods)
			&& methods.TryGetValue(name, out var overloads)))
		{
			if (GeneralMethodsList.TryGetValue(container, out var list) && list.TryGetValue(name, out overloads))
			{
				functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
				user = false;
				return true;
			}
			functions = null;
			user = false;
			return false;
		}
		functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		for (var i = 0; i < functions.Length; i++)
		{
			var arrayParameters = functions[i].ArrayParameters;
			for (var j = 0; j < arrayParameters.Length; j++)
			{
				var x = arrayParameters[j];
				if (!(!x.ArrayParameterPackage && x.ArrayParameterType.Length == 1
					&& x.ArrayParameterType.Peek().BlockType == BlockType.Extra && parameterTypes.Length > j))
					continue;
				functions[i] = new([], ReplaceExtraType(functions[i].ReturnUnvType, x.ArrayParameterType.Peek().Name,
					parameterTypes[j]), functions[i].Attributes, [.. functions[i].Parameters.Convert(y =>
				new GeneralMethodParameter(ReplaceExtraType(y.Type, x.ArrayParameterType.Peek().Name,
				parameterTypes[j]), y.Name, y.Attributes, y.DefaultValue))]);
			}
		}
		user = true;
		return true;
		UniversalType FindParameter(String typeName) => parameterTypes[functionOverload.Parameters.FindIndex(x =>
			typeName == x.Type || x.ExtraTypes.Contains(typeName))];
		TreeBranch GetUniversalType(String typeName) => new("type", 0, []) { Extra
			= functionOverload.ExtraTypes.Contains(typeName)
			? FindParameter(typeName) : new UniversalType(GetBlockStack(typeName), []) };
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
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions) =>
		UserDefinedFunctionExists(container, name, parameterTypes, out functions, out _, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, List<UniversalType> parameterTypes,
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer)
			&& UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var overloads))
		{
			functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
			derived = false;
			return true;
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(userDefinedType.BaseType, name, parameterTypes, out functions))
			{
				derived = true;
				return true;
			}
			else if (UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name, parameterTypes,
				out functions, out matchingContainer, out derived))
				return true;
		}
		functions = null;
		derived = false;
		return false;
	}

	public static bool UserDefinedNonDerivedFunctionExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer) && UserDefinedFunctionsList[matchingContainer].TryGetValue(name, out var overloads))
		{
			functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
			return true;
		}
		functions = null;
		return false;
	}

	public static bool ConstructorsExist(UniversalType container, List<UniversalType> callParameterTypes, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			constructors = [];
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(x => TypeMapping(x));
		var validity = int.MinValue;
		var methods = netType.GetConstructors().FindAllMax(x =>
		{
			var currentValidity = GetMethodValidity(null, x, callParameterNetTypes);
			if (currentValidity > validity)
				validity = currentValidity;
			return currentValidity;
		});
		constructors = [];
		if (validity < 0)
			return false;
		foreach (var method in methods)
		{
			var genericArguments = netType.GetGenericArguments();
			var patterns = GetReplacementPatterns(genericArguments, callParameterNetTypes);
			var parameters = method.GetParameters();
			var constructorParameterTypes = parameters.ToArray(x => x.ParameterType);
			for (var i = 0; i < patterns.Length; i++)
			{
				for (var j = 0; j < constructorParameterTypes.Length; j++)
					constructorParameterTypes[j] = ReplaceExtraNetType(constructorParameterTypes[j], patterns[i]);
			}
			constructors.Add(new((method.IsAbstract ? ConstructorAttributes.Abstract : 0)
				| (method.IsStatic ? ConstructorAttributes.Static : 0),
				new(constructorParameterTypes.ToList((x, index) => new GeneralMethodParameter(TypeMappingBack(x,
				netType.GetGenericArguments(), [.. container.ExtraTypes.SkipWhile(x =>
				x.Value.Info != "type" || x.Value.Extra is not UniversalType)]).Wrap(y =>
				Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? GetSubtype(y) : y),
				parameters[index].Name ?? "x",
				(parameters[index].IsOptional ? ParameterAttributes.Optional : 0)
				| (parameters[index].ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
				| (parameters[index].IsOut ? ParameterAttributes.Out : 0)
				| (Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? ParameterAttributes.Params : 0),
				parameters[index].DefaultValue?.ToString() ?? "null")))));
		}
		return true;
	}

	public static bool UserDefinedConstructorsExist(UniversalType container, List<UniversalType> parameterTypes, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		if (UserDefinedConstructorsList.TryGetValue(container.MainType, out var temp_constructors)
			&& !(UserDefinedTypesList.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& (userDefinedType.Attributes & (TypeAttributes.Struct | TypeAttributes.Static))
			is not (0 or TypeAttributes.Sealed or TypeAttributes.Struct)))
		{
			constructors = [.. temp_constructors, .. ConstructorsExist(userDefinedType.BaseType, parameterTypes, out var baseConstructors)
				? baseConstructors : [], .. UserDefinedConstructorsExist(userDefinedType.BaseType, parameterTypes, out baseConstructors)
				? baseConstructors : []];
			if (constructors.Length != 0)
				return true;
		}
		constructors = null;
		return false;
	}
}
