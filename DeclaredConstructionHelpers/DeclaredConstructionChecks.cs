global using Corlib.NStar;
global using System;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.DeclaredConstructionMappings;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.IntermediateFunctions;
global using static CSharp.NStar.TypeHelpers;
global using static System.Math;
global using String = Corlib.NStar.String;
using ILGPU.Util;

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

	public static bool ExtraTypeExists(BlockStack container, String type)
	{
		if (VariablesList.TryGetValue(container, out var list))
		{
			if (list.TryGetValue(type, out var type2))
			{
				return TypeIsPrimitive(type2.MainType) && type2.MainType.Peek().Name == "typename" && type2.ExtraTypes.Length == 0;
			}
			else
			{
				return false;
			}
		}
		if (UserDefinedPropertiesList.TryGetValue(container, out var list_))
		{
			if (list_.TryGetValue(type, out var a))
			{
				return TypeIsPrimitive(a.UnvType.MainType) && a.UnvType.MainType.Peek().Name == "typename" && a.UnvType.ExtraTypes.Length == 0;
			}
			else
			{
				return false;
			}
		}
		return false;
	}

	public static bool IsNotImplementedNamespace(String @namespace)
	{
		if (NotImplementedNamespacesList.Contains(@namespace))
		{
			return true;
		}
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
		{
			return true;
		}
		return false;
	}

	public static bool IsNotImplementedType(String @namespace, String type)
	{
		if (NotImplementedTypesList.Contains((@namespace, type)))
		{
			return true;
		}
		return false;
	}

	public static bool IsOutdatedType(String @namespace, String type, out String useInstead)
	{
		var index = OutdatedTypesList.IndexOfKey((@namespace, type));
		if (index != -1)
		{
			useInstead = OutdatedTypesList.Values[index];
			return true;
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String type)
	{
		if (ReservedTypesList.Contains((@namespace, type)))
		{
			return true;
		}
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
			{
				return true;
			}
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
			{
				return true;
			}
		}
		return false;
	}

	public static bool PropertyExists(UniversalType container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property)
	{
		if (UserDefinedPropertiesList.TryGetValue(container.MainType, out var list) && list.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			property = null;
			return false;
		}
		if (!type.TryWrap(x => x.GetProperty(name.ToString()), out var prop))
			prop = type.GetProperties().Find(x => x.Name == name.ToString());
		if (prop == null)
		{
			property = null;
			return false;
		}
		property = (TypeMappingBack(prop.PropertyType, type.GetGenericArguments(), container.ExtraTypes), PropertyAttributes.None);
		return true;
	}

	public static bool UserDefinedPropertyExists(BlockStack container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property, out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedPropertiesList.ContainsKey, out matchingContainer))
		{
			var list = UserDefinedPropertiesList[matchingContainer];
			if (list.TryGetValue(name, out var a))
			{
				property = a;
				return true;
			}
		}
		property = null;
		return false;
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

	public static bool MethodExists(UniversalType container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			function = null;
			return false;
		}
		if (!type.TryWrap(x => x.GetMethod(name.ToString()), out var method))
			method = type.GetMethods().Find(x => x.Name == name.ToString());
		if (method == null)
		{
			function = null;
			return false;
		}
		function = ([], TypeMappingBack(method.ReturnType, type.GetGenericArguments(), container.ExtraTypes),
			(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
			new(method.GetParameters().ToList(x => new GeneralMethodParameter(CreateVar(TypeMappingBack(x.ParameterType,
			type.GetGenericArguments(), container.ExtraTypes), out var UnvType).MainType,
			x.Name ?? "x", UnvType.ExtraTypes,
			(x.IsOptional ? ParameterAttributes.Optional : 0) | (x.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (x.IsOut ? ParameterAttributes.Out : 0), x.DefaultValue?.ToString() ?? "null"))));
		return true;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function, out bool user)
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

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function) => UserDefinedFunctionExists(container, name, out function, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function, out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer))
		{
			var list = UserDefinedFunctionsList[matchingContainer];
			if (list.TryGetValue(name, out var method_overloads))
			{
				function = method_overloads[0];
				return true;
			}
		}
		function = null;
		return false;
	}

	public static bool ConstructorsExist(UniversalType container, out GeneralConstructorOverloads? constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			constructors = null;
			return false;
		}
		var typeConstructors = type.GetConstructors();
		if (typeConstructors == null)
		{
			constructors = null;
			return false;
		}
		constructors = new(typeConstructors.ToList(x => ((x.IsAbstract ? ConstructorAttributes.Abstract : 0)
		| (x.IsStatic ? ConstructorAttributes.Static : 0), new GeneralMethodParameters(x.GetParameters().ToList(y =>
			new GeneralMethodParameter(CreateVar(TypeMappingBack(y.ParameterType,
			type.GetGenericArguments(), container.ExtraTypes),
			out var UnvType).MainType, y.Name ?? "x", UnvType.ExtraTypes, (y.IsOptional ? ParameterAttributes.Optional : 0)
			| (y.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (y.IsOut ? ParameterAttributes.Out : 0), y.DefaultValue?.ToString() ?? "null"))))));
		return true;
	}

	public static bool UserDefinedConstructorsExist(BlockStack type, out GeneralConstructorOverloads? constructors)
	{
		if (UserDefinedConstructorsList.TryGetValue(type, out var temp_constructors))
		{
			constructors = [.. temp_constructors];
			if (constructors.Length != 0)
			{
				return true;
			}
		}
		constructors = null;
		return false;
	}
}
