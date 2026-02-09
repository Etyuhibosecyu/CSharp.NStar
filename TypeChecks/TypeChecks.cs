global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using String = NStar.Core.String;

namespace CSharp.NStar;

public static class TypeChecks
{
	public static bool CheckContainer(BlockStack container, Func<BlockStack, bool> check, out BlockStack matchingContainer)
	{
		if (check(container))
		{
			matchingContainer = container;
			return true;
		}
		var containerPart = container.ToList().GetSlice();
		BlockStack stack;
		while (containerPart.Any())
		{
			containerPart = containerPart.SkipLast(1);
			if (check(stack = new(containerPart)))
			{
				matchingContainer = stack;
				return true;
			}
		}
		matchingContainer = new();
		return false;
	}

	public static bool ExtraTypeExists(BlockStack container, String typeName, out bool @class)
	{
		@class = false;
		if (container.Length != 0 && UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType)
			&& userDefinedType.Restrictions.Exists(x => x.Name == typeName))
			return true;
		if (UserDefinedConstants.TryGetValue(container, out var containerConstants)
			&& containerConstants.TryGetValue(typeName, out var constant))
		{
			if (TypeIsPrimitive(constant.NStarType.MainType) && constant.NStarType.MainType.TryPeek(out var block)
				&& block.Name == "typename" && constant.NStarType.ExtraTypes.Length == 0)
				return true;
			if (constant.NStarType.MainType.Equals(DictionaryBlockStack) && constant.NStarType.ExtraTypes.Length == 2
				&& constant.NStarType.ExtraTypes[1].Name == "type"
				&& constant.NStarType.ExtraTypes[1].Extra is NStarType ValueNStarType
				&& ValueNStarType.MainType.TryPeek(out block) && block.BlockType == BlockType.Other && block.Name == "Class")
			{
				@class = true;
				return true;
			}
		}
		if (Variables.TryGetValue(container, out var containerVariables)
			&& containerVariables.TryGetValue(typeName, out var variableName))
			return TypeIsPrimitive(variableName.MainType) && variableName.MainType.Peek().Name == "typename";
		if (UserDefinedProperties.TryGetValue(container, out var containerProperties)
			&& containerProperties.TryGetValue(typeName, out var a))
			return TypeIsPrimitive(a.NStarType.MainType) && a.NStarType.MainType.Peek().Name == "typename"
				&& a.NStarType.ExtraTypes.Length == 0;
		return false;
	}

	public static bool IsEqualOrDerived(NStarType derived, NStarType @base)
	{
		if (derived.Equals(@base) || @base.Equals(ObjectType))
			return true;
		String foundName = default!;
		if (@base.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
			&& CheckContainer(@base.MainType, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
			&& containerTempTypes.Find(x => x.Name == block.Name) is var found && (foundName = found.Name) != null, out _)
			&& derived.MainType.TryPeek(out block) && block.BlockType == BlockType.Extra
			&& CheckContainer(derived.MainType, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
			&& containerTempTypes.Any(x => x.Name == block.Name && Variables.TryGetValue(stack, out var containerVariables)
			&& containerVariables.TryGetValue(x.Name, out var VariableNStarType)
			&& VariableNStarType.MainType.Equals(RecursiveBlockStack) && VariableNStarType.ExtraTypes.Length == 1
			&& VariableNStarType.ExtraTypes[0].Name == "type"
			&& VariableNStarType.ExtraTypes[0].Extra is NStarType BaseNStarType
			&& BaseNStarType.Equals(@base)), out _))
			return true;
		if (IsEqualOrDerivedNetType(derived, @base))
			return true;
		var type = derived;
		while (!type.Equals(NullType))
		{
			if (!UserDefinedTypes.TryGetValue(SplitType(derived.MainType), out var userDefinedType))
				return false;
			type = userDefinedType.BaseType;
			if (type.MainType.Equals(@base.MainType))
				return true;
		}
		return false;
	}

	private static bool IsEqualOrDerivedNetType(NStarType sourceType, NStarType destinationType) =>
		sourceType.MainType.TryPeek(out var sourceBlock)
		&& (PrimitiveTypes.TryGetValue(sourceBlock.Name, out var sourceNetType)
		|| ExtraTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
		sourceBlock.Name), out sourceNetType)
		|| ImportedTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
		sourceBlock.Name), out sourceNetType))
		&& sourceNetType.GetGenericArguments().Length == 0
		&& destinationType.MainType.TryPeek(out var destinationBlock)
		&& (PrimitiveTypes.TryGetValue(destinationBlock.Name, out var destinationNetType)
		|| ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		destinationBlock.Name), out destinationNetType)
		|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		destinationBlock.Name), out destinationNetType))
		&& (destinationNetType.GetGenericArguments().Length == 0
		&& destinationNetType.IsAssignableFrom(sourceNetType)
		|| destinationNetType.GetGenericArguments().Length == 1
		&& destinationNetType.GetGenericArguments()[0].Name is "T" or "TSelf"
		&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
		&& genericType.IsAssignableFrom(sourceNetType))
		|| ExplicitlyConnectedNamespaces.FindIndex(x => (ExtraTypes.TryGetValue((x,
		sourceType.MainType.TryPeek(out var sourceBlock) ? sourceBlock.Name : ""), out var sourceNetType)
		|| ImportedTypes.TryGetValue((x,
		sourceType.MainType.TryPeek(out sourceBlock) ? sourceBlock.Name : ""), out sourceNetType))
		&& sourceNetType.GetGenericArguments().Length == 0
		&& (ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		sourceType.MainType.TryPeek(out var destinationBlock) ? destinationBlock.Name : ""), out var destinationNetType)
		|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		sourceType.MainType.TryPeek(out destinationBlock) ? destinationBlock.Name : ""), out destinationNetType))
		&& (destinationNetType.GetGenericArguments().Length == 0
		&& destinationNetType.IsAssignableFrom(sourceNetType)
		|| destinationNetType.GetGenericArguments()[0].Name is "T" or "TSelf"
		&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
		&& genericType.IsAssignableFrom(sourceNetType))) >= 0
		|| destinationType.MainType.TryPeek(out destinationBlock)
		&& (PrimitiveTypes.TryGetValue(destinationBlock.Name, out destinationNetType)
		|| ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		destinationBlock.Name), out destinationNetType)
		|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
		destinationBlock.Name), out destinationNetType))
		&& destinationNetType.GetGenericArguments().Length == 1
		&& destinationNetType.GetGenericArguments()[0].Name is "T"
		&& destinationType.ExtraTypes.Length == 1 && destinationType.ExtraTypes[0].Name == "type"
		&& destinationType.ExtraTypes[0].Extra is NStarType ClosureNStarType && ClosureNStarType.Equals(sourceType);

	public static bool IsNotImplementedNamespace(String @namespace)
	{
		if (NotImplementedNamespaces.Contains(@namespace))
			return true;
		return false;
	}

	public static bool IsOutdatedNamespace(String @namespace, out String? useInstead)
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

	public static bool IsOutdatedType(String @namespace, String typeName, out String? useInstead)
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

	public static bool IsOutdatedMember(BlockStack type, String member, out String? useInstead)
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
		if (ExtraTypes.TryGetValue((containerType.Container.ToString(), containerType.Type), out netType)
			|| ImportedTypes.TryGetValue((containerType.Container.ToString(), containerType.Type), out netType))
			return true;
		if (Interfaces.TryGetValue((containerType.Container.ToString(), containerType.Type), out var @interface))
		{
			netType = @interface.DotNetType;
			return true;
		}
		if (ExtendedTypes.TryGetValue((containerType.Container, containerType.Type), out var extendedType))
		{
			netType = containerType.Type.ToString() switch
			{
				nameof(Action) => typeof(Action),
				nameof(Func<>) => typeof(Func<>),
				_ => throw new InvalidOperationException(),
			};
			return true;
		}
		Type? preservedNetType = null;
		if (containerType.Container.Length != 0)
			return false;
		if (ExplicitlyConnectedNamespaces
			.FindIndex(x => ExtraTypes.TryGetValue((x, containerType.Type), out preservedNetType)) < 0
			&& ExplicitlyConnectedNamespaces
			.FindIndex(x => ImportedTypes.TryGetValue((x, containerType.Type), out preservedNetType)) < 0)
			return false;
		if (preservedNetType == null)
			return false;
		netType = preservedNetType;
		return true;
	}
}
