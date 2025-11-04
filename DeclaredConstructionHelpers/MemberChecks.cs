using NStar.MathLib.Extras;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CSharp.NStar;

public static class MemberChecks
{
	public static bool CheckContainer(BlockStack container, Func<BlockStack, bool> check, out BlockStack type)
	{
		if (check(container))
		{
			type = container;
			return true;
		}
		var containerPart = container.ToList().GetSlice();
		BlockStack stack;
		while (containerPart.Any())
		{
			containerPart = containerPart.SkipLast(1);
			if (check(stack = new(containerPart)))
			{
				type = stack;
				return true;
			}
		}
		type = new();
		return false;
	}

	public static bool PropertyExists(NStarType container, String name, [MaybeNullWhen(false)]
		out UserDefinedProperty? property)
	{
		if (UserDefinedProperties.TryGetValue(container.MainType, out var containerProperties)
			&& containerProperties.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		else if (UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
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
		if (CheckContainer(container, UserDefinedProperties.ContainsKey, out matchingContainer)
			&& UserDefinedProperties[matchingContainer].TryGetValue(name, out var value))
		{
			property = value;
			inBase = false;
			return true;
		}
		else if (CheckContainer(container, x => UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
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
		if (UserDefinedProperties.TryGetValue(container, out var containerProperties))
			foreach (var containerProperty in containerProperties)
				result.Add(containerProperty);
		if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
			result.AddRange(GetAllProperties(userDefinedType.BaseType.MainType));
		return result;
	}

	public static bool ConstantExists(NStarType container, String name, [MaybeNullWhen(false)]
		out UserDefinedConstant? constant)
	{
		if (UserDefinedConstants.TryGetValue(container.MainType, out var containerConstants)
			&& containerConstants.TryGetValue(name, out var a))
		{
			constant = a;
			return true;
		}
		else if (UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
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
		if (CheckContainer(container, UserDefinedConstants.ContainsKey, out matchingContainer)
			&& UserDefinedConstants[matchingContainer].TryGetValue(name, out var value))
		{
			constant = value;
			inBase = false;
			return true;
		}
		else if (CheckContainer(container, x => UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && ConstantExists(userDefinedType.BaseType, name, out constant))
		{
			inBase = true;
			return true;
		}
		constant = null;
		inBase = false;
		return false;
	}

	public static bool MethodExists(NStarType container, String name)
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

	public static bool MethodExists(NStarType container, String name, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			functions = [];
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(TypeMapping);
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
			functions.Add(new(name, [], TypeMappingBack(returnNetType, netType.GetGenericArguments(), container.ExtraTypes),
				(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
				new(functionParameterTypes.ToList((x, index) => new ExtendedMethodParameter(TypeMappingBack(x,
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

	public static bool ExtendedMethodExists(BlockStack container, String name, List<NStarType> parameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions, out bool user)
	{
		if (PublicFunctions.TryGetValue(name, out var functionOverload))
		{
			BlockStack? mainType;
			if (functionOverload.ExtraTypes.Contains(functionOverload.ReturnType))
				mainType = FindParameter(functionOverload.ReturnType).MainType;
			else
				mainType = GetBlockStack(functionOverload.ReturnType);
			BranchCollection extraTypes = new(functionOverload.ReturnExtraTypes.ToList(GetTypeAsBranch));
			functions = [new(name, [], new(mainType, extraTypes),
				functionOverload.Attributes, [.. functionOverload.Parameters.Convert((x, index) =>
				new ExtendedMethodParameter(functionOverload.ExtraTypes.Contains(x.Type)
				? parameterTypes[index] : BasicTypeToExtendedType(x.Type, x.ExtraTypes),
				x.Name, x.Attributes, x.DefaultValue))])];
			user = false;
			return true;
		}
		if (!(UserDefinedFunctions.TryGetValue(container, out var methods)
			&& methods.TryGetValue(name, out var overloads)))
		{
			if (BuiltInMemberCollections.ExtendedMethods.TryGetValue(container, out var builtInMethods)
				&& builtInMethods.TryGetValue(name, out var builtInOverloads))
			{
				functions = [.. builtInOverloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0).ToList(x =>
					new UserDefinedMethodOverload(name, x.ArrayParameters, x.ReturnNStarType, x.Attributes, x.Parameters))];
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
				functions[i] = new(functions[i].RealName, [], ReplaceExtraType(functions[i].ReturnNStarType, x.ArrayParameterType.Peek().Name,
					parameterTypes[j]), functions[i].Attributes, [.. functions[i].Parameters.Convert(y =>
				new ExtendedMethodParameter(ReplaceExtraType(y.Type, x.ArrayParameterType.Peek().Name,
				parameterTypes[j]), y.Name, y.Attributes, y.DefaultValue))]);
			}
		}
		user = true;
		return true;
		NStarType FindParameter(String typeName) => parameterTypes[functionOverload.Parameters.FindIndex(x =>
			typeName == x.Type || x.ExtraTypes.Contains(typeName))];
		TreeBranch GetTypeAsBranch(String typeName) => new("type", 0, [])
		{
			Extra
			= functionOverload.ExtraTypes.Contains(typeName)
			? FindParameter(typeName) : new NStarType(GetBlockStack(typeName), [])
		};
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name)
	{
		if (CheckContainer(container, UserDefinedFunctions.ContainsKey, out var matchingContainer) && UserDefinedFunctions[matchingContainer].TryGetValue(name, out var method_overloads))
			return true;
		else if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(userDefinedType.BaseType, name))
				return true;
			else if (UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name))
				return true;
		}
		return false;
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name, List<NStarType> parameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions) =>
		UserDefinedFunctionExists(container, name, parameterTypes, out functions, out _, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, List<NStarType> parameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		if (CheckContainer(container, UserDefinedFunctions.ContainsKey, out matchingContainer)
			&& UserDefinedFunctions[matchingContainer].TryGetValue(name, out var overloads))
		{
			functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
			derived = false;
			return true;
		}
		else if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
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
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedFunctions.ContainsKey, out matchingContainer) && UserDefinedFunctions[matchingContainer].TryGetValue(name, out var overloads))
		{
			functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
			return true;
		}
		functions = null;
		return false;
	}

	public static bool ConstructorsExist(NStarType container, List<NStarType> callParameterTypes, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			constructors = [];
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(TypeMapping);
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
				new(constructorParameterTypes.ToList((x, index) => new ExtendedMethodParameter(TypeMappingBack(x,
				netType.GetGenericArguments(), [.. container.ExtraTypes.SkipWhile(x =>
				x.Value.Name != "type" || x.Value.Extra is not NStarType)]).Wrap(y =>
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

	public static bool UserDefinedConstructorsExist(NStarType container, List<NStarType> parameterTypes, [MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		if (UserDefinedConstructors.TryGetValue(container.MainType, out var temp_constructors)
			&& !(UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
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

	public static bool TypeIsFullySpecified(NStarType type, BlockStack container)
	{
		if (type.MainType.Length == 0 || type.MainType.Peek().BlockType == BlockType.Extra
			&& !ConstantExists(new(new(type.MainType.SkipLast(1)), NoBranches), type.MainType.Peek().Name, out _)
			&& !(type.MainType.Length == 1
			&& UserDefinedConstantExists(container, type.MainType.Peek().Name, out _, out _, out _)))
			return false;
		foreach (var x in type.ExtraTypes)
			if (x.Value.Name == "type" && x.Value.Extra is NStarType InnerNStarType
				&& !TypeIsFullySpecified(InnerNStarType, container))
				return false;
		return true;
	}
}
