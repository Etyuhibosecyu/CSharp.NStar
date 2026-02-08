global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib.Extras;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Reflection;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CSharp.NStar;

public static class MemberChecks
{
	public static bool PropertyExists(NStarType container, String name, bool @static, [MaybeNullWhen(false)]
		out UserDefinedProperty? property)
	{
		if (UserDefinedProperties.TryGetValue(container.MainType, out var containerProperties)
			&& containerProperties.TryGetValue(name, out var a))
		{
			property = a;
			return ProcessProperty(container, ref property);
		}
		else if (UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& PropertyExists(userDefinedType.BaseType, name, @static, out property))
			return ProcessProperty(container, ref property);
		var containerType = SplitType(container.MainType);
		if (!TypeExists(containerType, out var netType))
		{
			property = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetProperty(name.ToString(),
			(@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public), out var netProperty))
			netProperty = netType.GetProperties((@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public)
				.Find(x => x.Name == name.ToString());
		if (netProperty != null)
		{
			property = new(TypeMappingBack(netProperty.PropertyType, netType.GetGenericArguments(), container.ExtraTypes),
				@static ? PropertyAttributes.Static : PropertyAttributes.None, "null");
			return true;
		}
		if (!netType.TryWrap(x => x.GetField(name.ToString(),
			(@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public), out var netField))
			netField = netType.GetFields((@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public)
				.Find(x => x.Name == name.ToString());
		if (netField != null)
		{
			property = new(TypeMappingBack(netField.FieldType, netType.GetGenericArguments(), container.ExtraTypes),
				@static ? PropertyAttributes.Static : PropertyAttributes.None, "null");
			return true;
		}
		if (@static)
		{
			property = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetEvent(name.ToString()), out var netEvent))
			netEvent = netType.GetEvents().Find(x => x.Name == name.ToString());
		if (netEvent != null)
		{
			var handlerType = netEvent.EventHandlerType;
			if (handlerType != null)
			{
				property = new(TypeMappingBack(handlerType, netType.GetGenericArguments(), container.ExtraTypes),
					PropertyAttributes.None, "null");
				return true;
			}
		}
		property = null;
		return false;
	}

	public static bool UserDefinedPropertyExists(BlockStack container, String name, bool @static,
		[MaybeNullWhen(false)] out UserDefinedProperty? property, [MaybeNullWhen(false)] out BlockStack matchingContainer,
		out bool inBase, out BlockStack actualContainer)
	{
		UserDefinedType userDefinedType = default!;
		if (CheckContainer(container, UserDefinedProperties.ContainsKey, out matchingContainer)
			&& UserDefinedProperties[matchingContainer].TryGetValue(name, out var value))
		{
			property = value;
			inBase = false;
			actualContainer = matchingContainer;
			return true;
		}
		else if (CheckContainer(container, x => UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && PropertyExists(userDefinedType.BaseType, name, @static, out property))
		{
			inBase = true;
			actualContainer = userDefinedType.BaseType.MainType;
			return true;
		}
		property = null;
		inBase = false;
		actualContainer = default!;
		return false;
	}

	private static bool ProcessProperty(NStarType container, ref UserDefinedProperty? property)
	{
		Debug.Assert(property != null);
		(BlockStack Container, String Type) matchingType = default!;
		if (!CheckContainer(container.MainType, x => UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = UserDefinedTypes[matchingType].Restrictions;
		if (restrictions.Length == 0)
			return true;
		var sourceTypes = restrictions.ToList(x => new NStarType(new(new Block(BlockType.Extra, x.Name, 1)), NoBranches));
		var destinationTypes = container.ExtraTypes
			.ToList(x => x.Value.Name == "type" && x.Value.Extra is NStarType NStarType ? NStarType : NullType)
			.AddRange(container.ExtraTypes.Length == 1 && container.ExtraTypes[0].Name == "List"
			? container.ExtraTypes[0].Elements
			.Convert(x => x.Name == "type" && x.Extra is NStarType NStarType ? NStarType
			: x.Name == "Hypername" && x.Length == 1 && x[0].Name == "type" && x[0].Extra is NStarType NStarType2
			? NStarType2 : NullType) : []).FilterInPlace(x => !x.Equals(NullType));
		var patterns = GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
			destinationTypes, sourceTypes)
			.AddRange(GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
			sourceTypes, destinationTypes));
		var returnType = property.Value.NStarType;
		for (var j = 0; j < patterns.Length; j++)
		{
			for (var k = 0; k < sourceTypes.Length; k++)
				returnType = ReplaceExtraType(returnType, patterns[j]);
		}
		property = new(returnType, property.Value.Attributes, property.Value.DefaultValue);
		return true;
	}

	public static List<G.KeyValuePair<String, UserDefinedProperty>> GetAllProperties(BlockStack container)
	{
		List<G.KeyValuePair<String, UserDefinedProperty>> result = [];
		if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
			result.AddRange(GetAllProperties(userDefinedType.BaseType.MainType));
		if (UserDefinedProperties.TryGetValue(container, out var containerProperties))
			foreach (var containerProperty in containerProperties)
				result.Add(containerProperty);
		return result;
	}

	public static bool ConstantExists(NStarType container, String name,
		[MaybeNullWhen(false)] out UserDefinedConstant? constant)
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
			.Find(x => x.IsInitOnly && x.Name == name.ToString());
		if (netProperty == null)
		{
			constant = null;
			return false;
		}
		constant = new(TypeMappingBack(netProperty.FieldType, netType.GetGenericArguments(), container.ExtraTypes),
			ConstantAttributes.None, new(netProperty.GetValue(null)?.ToString() ?? "null", 0, []));
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
			out matchingContainer))
		{
			if (ConstantExists(userDefinedType.BaseType, name, out constant))
			{
				inBase = true;
				return true;
			}
		}
		constant = null;
		inBase = false;
		return false;
	}

	public static bool UserDefinedPolymorphTypeExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		UserDefinedType userDefinedType = default!;
		if (CheckContainer(container, x => UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer))
		{
			var foundIndex = userDefinedType.Restrictions
				.FindIndex(x => x.RestrictionType.MainType.Equals(RecursiveBlockStack) && x.Name == name);
			if (foundIndex >= 0)
				return true;
		}
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
		if (container.ExtraTypes.Length == 0)
		{
			if (netType == typeof(Task<>))
				netType = typeof(Task);
			else if (netType == typeof(ValueTask<>))
				netType = typeof(ValueTask);
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
		var noArrayFunction = false;
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
			var goodIndex = parameters.FindIndex(x => (x.ParameterType.Name.Contains(nameof(List<>))
				|| x.ParameterType.Name.Contains(nameof(G.IEnumerable<>)))
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)));
			var badIndex = parameters.FindIndex(x => x.ParameterType.IsSZArray
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)) || x.ParameterType.Name.Contains("Span"));
			if (goodIndex >= 0 && badIndex < 0)
				noArrayFunction = true;
			else if (noArrayFunction && badIndex >= 0)
				continue;
			functions.Add(new(name, [], TypeMappingBack(returnNetType, netType.GetGenericArguments(), container.ExtraTypes),
				(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0)
				| (method.ReturnType.FullName != null
				&& (method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task")
				|| method.ReturnType.FullName.StartsWith("System.Threading.Tasks.ValueTask")) ? FunctionAttributes.Async : 0),
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

	public static bool ExtendedMethodExists(BlockStack container, String name, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions, out bool user)
	{
		if (PublicFunctions.TryGetValue(name, out var functionOverload))
		{
			BlockStack mainType;
			if (functionOverload.ExtraTypes.Contains(functionOverload.ReturnType))
				mainType = FindParameter(functionOverload.ReturnType).MainType;
			else
				mainType = GetBlockStack(functionOverload.ReturnType);
			BranchCollection extraTypes = new(functionOverload.ReturnExtraTypes.ToList(GetTypeAsBranch));
			NStarType ReturnNStarType = (mainType, extraTypes);
			ExtendedMethodParameters parameters = [.. functionOverload.Parameters.Convert((x, index) =>
			{
				NStarType NStarType;
				if (functionOverload.ExtraTypes.Contains(x.Type))
					NStarType = FindParameter(x.Type);
				else
					NStarType = new(GetBlockStack(x.Type), new(x.ExtraTypes.Convert(GetTypeAsBranch)));
				return new ExtendedMethodParameter(NStarType, x.Name, x.Attributes, x.DefaultValue);
			})];
			var functionParameterTypes = parameters.ToList((x, index) => index == callParameterTypes.Length - 1
				&& (x.Attributes & ParameterAttributes.Params) == ParameterAttributes.Params ? GetListType(x.Type) : x.Type);
			if (parameters.Length != 0 && (parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callParameterTypes.Length > functionParameterTypes.Length)
			{
				functionParameterTypes.RemoveAt(^1);
				functionParameterTypes.AddSeries(parameters[^1].Type,
					callParameterTypes.Length - functionParameterTypes.Length);
			}
			var patterns = GetNStarReplacementPatterns(functionOverload.ExtraTypes, callParameterTypes, functionParameterTypes)
				.AddRange(GetNStarReplacementPatterns(functionOverload.ExtraTypes, functionParameterTypes, callParameterTypes))
				.FilterInPlace(x => !x.TypeToInsert.ExtraTypes.Values
				.Any(y => y.Name == "type" && y.Extra is NStarType NStarType && NStarType.MainType.TryPeek(out var block)
				&& block.Name.Equals(x.ExtraType)));
			for (var j = 0; j < patterns.Length; j++)
			{
				ReturnNStarType = ReplaceExtraType(ReturnNStarType, patterns[j]);
				for (var k = 0; k < functionParameterTypes.Length; k++)
					functionParameterTypes[k] = ReplaceExtraType(functionParameterTypes[k], patterns[j]);
				parameters[j] = new(functionParameterTypes[j], parameters[j].Name, parameters[j].Attributes,
					parameters[j].DefaultValue);
			}
			functions = [new(name, [], ReturnNStarType, functionOverload.Attributes, parameters)];
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
					new UserDefinedMethodOverload(name, x.Restrictions, x.ReturnNStarType, x.Attributes, x.Parameters))];
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
			var arrayParameters = functions[i].Restrictions;
			for (var j = 0; j < arrayParameters.Length; j++)
			{
				var x = arrayParameters[j];
				if (!(!x.Package && x.RestrictionType.ExtraTypes.Length == 0
					&& x.RestrictionType.MainType.Length == 1
					&& x.RestrictionType.MainType.Peek().BlockType == BlockType.Extra && callParameterTypes.Length > j))
					continue;
				functions[i] = new(functions[i].RealName, [], ReplaceExtraType(functions[i].ReturnNStarType,
					(x.RestrictionType.MainType.Peek().Name, callParameterTypes[j])), functions[i].Attributes,
					[.. functions[i].Parameters.Convert(y => new ExtendedMethodParameter(ReplaceExtraType(y.Type,
					(x.RestrictionType.MainType.Peek().Name, callParameterTypes[j])), y.Name, y.Attributes, y.DefaultValue))]);
			}
		}
		user = true;
		return true;
		NStarType FindParameter(String typeName)
		{
			var foundIndex = functionOverload.Parameters.FindIndex(x => typeName == x.Type || x.ExtraTypes.Contains(typeName));
			return foundIndex != callParameterTypes.Length - 1
				|| (functionOverload.Parameters[foundIndex].Attributes & ParameterAttributes.Params)
				!= ParameterAttributes.Params ? callParameterTypes[foundIndex]
				: GetSubtype(callParameterTypes[foundIndex]) is var subtype && !subtype.Equals(NullType)
				? subtype : callParameterTypes[foundIndex];
		}

		TreeBranch GetTypeAsBranch(String typeName) => new("type", 0, [])
		{
			Extra = functionOverload.ExtraTypes.Contains(typeName)
			? FindParameter(typeName) : new NStarType(GetBlockStack(typeName), [])
		};
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name)
	{
		if (CheckContainer(container, UserDefinedFunctions.ContainsKey, out var matchingContainer)
			&& UserDefinedFunctions[matchingContainer].TryGetValue(name, out var method_overloads))
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

	public static bool UserDefinedFunctionExists(NStarType container, String name, List<NStarType> parameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions) =>
		UserDefinedFunctionExists(container, name, parameterTypes, out functions, out _, out _);

	public static bool UserDefinedFunctionExists(NStarType container, String name, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		var mainType = container.MainType;
		if (!(CheckContainer(mainType, UserDefinedFunctions.ContainsKey, out matchingContainer)
			&& UserDefinedFunctions[matchingContainer].TryGetValue(name, out functions)))
		{
			if (UserDefinedTypes.TryGetValue(SplitType(mainType), out var userDefinedType))
			{
				if (MethodExists(userDefinedType.BaseType, name, callParameterTypes, out functions))
				{
					derived = true;
					return true;
				}
				else if (UserDefinedFunctionExists(userDefinedType.BaseType, name, callParameterTypes,
					out functions, out matchingContainer, out derived))
					return ProcessUserDefinedMethod(container, callParameterTypes, functions);
			}
			functions = null;
			derived = false;
			return false;
		}
		functions = [.. functions.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		derived = false;
		return ProcessUserDefinedMethod(container, callParameterTypes, functions);
	}

	private static bool ProcessUserDefinedMethod(NStarType container, List<NStarType> callParameterTypes,
		UserDefinedMethodOverloads functions)
	{
		var mainType = container.MainType;
		(BlockStack Container, String Type) matchingType = default!;
		if (!CheckContainer(mainType, x => UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = UserDefinedTypes[matchingType].Restrictions;
		if (restrictions.Length == 0)
			return true;
		for (var i = 0; i < functions.Length; i++)
		{
			var function = functions[i];
			var ReturnNStarType = function.ReturnNStarType;
			ExtendedMethodParameters parameters = [.. function.Parameters.Convert(x =>
				new ExtendedMethodParameter(x.Type, x.Name, x.Attributes, x.DefaultValue))];
			callParameterTypes = callParameterTypes.Copy();
			var extraTypes = container.ExtraTypes.Values;
			if (extraTypes.Count == 1 && extraTypes.First() is var extraType && extraType.Name == "List")
				extraTypes = extraType.Elements;
			foreach (var x in extraTypes)
			{
				TreeBranch branch;
				if (x.Name == "Hypername" && x.Length == 1)
					branch = x[0];
				else
					branch = x;
				if (branch.Name == "type" && branch.Extra is NStarType NStarType)
					callParameterTypes.Add(NStarType);
			}
			var functionParameterTypes = parameters.ToList(x => x.Type)
				.Concat(restrictions.ToList(x => new NStarType(new(new Block(BlockType.Extra, x.Name, 1)), NoBranches)));
			var patterns = GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
				callParameterTypes, functionParameterTypes)
				.AddRange(GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
				functionParameterTypes, callParameterTypes));
			for (var j = 0; j < patterns.Length; j++)
			{
				ReturnNStarType = ReplaceExtraType(ReturnNStarType, patterns[j]);
				for (var k = 0; k < parameters.Length; k++)
				{
					functionParameterTypes[k] = ReplaceExtraType(functionParameterTypes[k], patterns[j]);
					parameters[k] = new(functionParameterTypes[k], parameters[k].Name, parameters[k].Attributes,
						parameters[k].DefaultValue);
				}
			}
			functions[i] = new(function.RealName, function.Restrictions, ReturnNStarType, function.Attributes, parameters);
		}
		return true;
	}

	public static bool UserDefinedNonDerivedFunctionExists(BlockStack container, String name,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		if (!(CheckContainer(container, UserDefinedFunctions.ContainsKey, out matchingContainer)
			&& UserDefinedFunctions[matchingContainer].TryGetValue(name, out var overloads)))
		{
			functions = null;
			return false;
		}
		functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		return true;
	}

	public static ListHashSet<(String ExtraType, NStarType TypeToInsert)>
		GetNStarReplacementPatterns(List<String> genericArguments, List<NStarType> callParameterTypes,
		List<NStarType> functionParameterTypes)
	{
		var length = Min(callParameterTypes.Length, functionParameterTypes.Length);
		ListHashSet<(String ExtraType, NStarType TypeToInsert)> result = [];
		for (var i = 0; i < genericArguments.Length; i++)
		{
			var genericArgument = genericArguments[i];
			for (var j = 0; j < length; j++)
			{
				var callParameterType = callParameterTypes[j];
				var functionParameterType = functionParameterTypes[j];
				if (TypeIsFullySpecified(callParameterType, []))
					continue;
				if (callParameterType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
					&& block.Name == genericArgument)
				{
					result.Add((genericArgument, functionParameterType));
					continue;
				}
				result.AddRange(GetNStarReplacementPatterns(genericArguments,
					callParameterType.ExtraTypes.ToList(x => x.Value.Name == "type" && x.Value.Extra is NStarType NStarType
					? NStarType : NullType),
					functionParameterType.ExtraTypes.ToList(x => x.Value.Name == "type" && x.Value.Extra is NStarType NStarType
					? NStarType : NullType)));
			}
		}
		return result;
	}

	public static bool ConstructorsExist(NStarType container, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out ConstructorOverloads constructors)
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
		var noArrayConstructor = false;
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
			var goodIndex = parameters.FindIndex(x => (x.ParameterType.Name.Contains(nameof(List<>))
				|| x.ParameterType.Name.Contains(nameof(G.IEnumerable<>)))
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)));
			var badIndex = parameters.FindIndex(x => x.ParameterType.IsSZArray
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)) || x.ParameterType.Name.Contains("Span"));
			if (goodIndex >= 0 && badIndex < 0)
				noArrayConstructor = true;
			else if (noArrayConstructor && badIndex >= 0)
				continue;
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
				parameters[index].DefaultValue?.ToString() ?? "null"))), []));
		}
		return true;
	}

	public static bool UserDefinedConstructorsExist(NStarType container, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var mainType = container.MainType;
		if (!UserDefinedConstructors.TryGetValue(container.MainType, out var temp_constructors)
			|| UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& (userDefinedType.Attributes & (TypeAttributes.Struct | TypeAttributes.Static))
			is not (0 or TypeAttributes.Sealed or TypeAttributes.Struct))
		{
			constructors = null;
			return false;
		}
		constructors = [.. temp_constructors,
			.. ConstructorsExist(userDefinedType.BaseType, callParameterTypes, out var baseConstructors)
			? baseConstructors : [],
			.. UserDefinedConstructorsExist(userDefinedType.BaseType, callParameterTypes, out baseConstructors)
			? baseConstructors : []];
		(BlockStack Container, String Type) matchingType = default!;
		if (!CheckContainer(mainType, x => UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = UserDefinedTypes[matchingType].Restrictions;
		if (restrictions.Length == 0)
			return true;
		for (var i = 0; i < constructors.Length; i++)
		{
			var (Attributes, Parameters, UnsetRequiredProperties) = constructors[i];
			ExtendedMethodParameters parameters = [.. Parameters.Convert(x =>
				new ExtendedMethodParameter(x.Type, x.Name, x.Attributes, x.DefaultValue))];
			var constructorParameterTypes = parameters.ToList(x => x.Type);
			var patterns = GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
				callParameterTypes, constructorParameterTypes)
				.AddRange(GetNStarReplacementPatterns(restrictions.ToList(x => x.Name),
				constructorParameterTypes, callParameterTypes));
			for (var j = 0; j < patterns.Length; j++)
			{
				for (var k = 0; k < constructorParameterTypes.Length; k++)
				{
					constructorParameterTypes[k] = ReplaceExtraType(constructorParameterTypes[k], patterns[j]);
					parameters[k] = new(constructorParameterTypes[k], parameters[k].Name, parameters[k].Attributes,
						parameters[k].DefaultValue);
				}
			}
			constructors[i] = new(Attributes, parameters, UnsetRequiredProperties);
		}
		return true;
	}

	public static bool TypeIsFullySpecified(NStarType type, BlockStack container)
	{
		BlockStack partialContainer;
		String name;
		if (type.MainType.Length == 0 || type.MainType.Peek().BlockType == BlockType.Extra
			&& !(UserDefinedPolymorphTypeExists(partialContainer = new(type.MainType.SkipLast(1)),
			name = type.MainType.Peek().Name, out _) || ConstantExists(new(partialContainer, NoBranches), name, out _)
			|| type.MainType.Length == 1
			&& UserDefinedConstantExists(container, type.MainType.Peek().Name, out _, out _, out _)))
			return false;
		foreach (var x in type.ExtraTypes)
			if (x.Value.Name == "type" && x.Value.Extra is NStarType InnerNStarType
				&& !TypeIsFullySpecified(InnerNStarType, container))
				return false;
		return true;
	}
}
