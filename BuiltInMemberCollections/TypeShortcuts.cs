using Mpir.NET;

namespace CSharp.NStar;

public record struct UserDefinedType(ExtendedArrayParameters ArrayParameters, TypeAttributes Attributes, NStarType BaseType, BranchCollection Decomposition);
public record struct UserDefinedProperty(NStarType NStarType, PropertyAttributes Attributes, String DefaultValue);
public record struct UserDefinedConstant(NStarType NStarType, ConstantAttributes Attributes, TreeBranch DefaultValue);
public record struct ExtendedArrayParameter(bool ArrayParameterPackage, BranchCollection ArrayParameterRestrictions, BlockStack ArrayParameterType, String ArrayParameterName);
public record struct MethodParameter(String Type, String Name, List<String> ExtraTypes, ParameterAttributes Attributes, String DefaultValue);
public record struct ExtendedMethodParameter(NStarType Type, String Name, ParameterAttributes Attributes, String DefaultValue);
public record struct FunctionOverload(List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters);
public record struct ExtendedMethodOverload(ExtendedArrayParameters ArrayParameters, NStarType ReturnNStarType, FunctionAttributes Attributes, ExtendedMethodParameters Parameters);
public record struct UserDefinedMethodOverload(String RealName, ExtendedArrayParameters ArrayParameters, NStarType ReturnNStarType, FunctionAttributes Attributes, ExtendedMethodParameters Parameters);

public sealed class VariablesBlock<T>(IList<T> main, IList<bool> isNull)
{
	public IList<T> Main = main;
	public IList<bool> IsNull = isNull;
}

public class TypeSortedList<T> : SortedDictionary<BlockStack, T>
{
	public TypeSortedList() : base(new BlockStackComparer())
	{
	}
}
public class TypeDictionary<T> : Dictionary<BlockStack, T>
{
	public TypeDictionary() : base(new BlockStackEComparer())
	{
	}
}
public class TypeDictionary2<T> : Dictionary<BlockStack, IList<T>>
{
	public TypeDictionary2() : base(new BlockStackEComparer())
	{
	}
}
public class LexemGroup : List<(BlockStack Container, String Name, int Start, int End)>
{
}
public class BlocksToJump : List<(BlockStack Container, String Type, String Name, int Start, int End)>
{
}
public class ParameterValues : List<(BlockStack Container, String Name, int ParameterIndex, int Start, int End)>
{
}
public class ExtendedTypesCollection(G.IComparer<(BlockStack Container, String Type)> comparer) : SortedDictionary<(BlockStack Container, String Type), (ExtendedArrayParameters ArrayParameters, TypeAttributes Attributes)>(comparer)
{
}
public class TypeVariables : SortedDictionary<String, NStarType>
{
	public TypeVariables() : base()
	{
	}

	public TypeVariables(G.IDictionary<String, NStarType> dictionary) : base(dictionary)
	{
	}
}
public class TypeProperties : SortedDictionary<String, (NStarType NStarType, PropertyAttributes Attributes)>
{
}
public class UserDefinedTypeProperties : Dictionary<String, UserDefinedProperty>
{
}
public class TypeIndexers : SortedDictionary<String, (BlockStack IndexType, BlockStack Type, List<String> ExtraTypes, PropertyAttributes Attributes)>
{
}
public class MethodParameters : List<MethodParameter>
{
	public MethodParameters() : base() { }
	public MethodParameters(G.IEnumerable<MethodParameter> parameters) : base(parameters) { }
}
public class FunctionsList : SortedDictionary<String, FunctionOverload>
{
}
public class ExtendedArrayParameters : List<ExtendedArrayParameter>
{
}
public class ExtendedMethodParameters : List<ExtendedMethodParameter>
{
	public ExtendedMethodParameters() : base()
	{
	}

	public ExtendedMethodParameters(G.IEnumerable<ExtendedMethodParameter> collection) : base(collection)
	{
	}
}
public class ExtendedMethodOverloads : List<ExtendedMethodOverload>
{
}
public class ExtendedMethods : SortedDictionary<String, ExtendedMethodOverloads>
{
}
public class UserDefinedMethodOverloads : List<UserDefinedMethodOverload>
{
}
public class UserDefinedMethods : Dictionary<String, UserDefinedMethodOverloads>
{
}
public class ConstructorOverloads : List<(ConstructorAttributes Attributes, ExtendedMethodParameters Parameters)>
{
	public ConstructorOverloads() : base() { }
	public ConstructorOverloads(G.IEnumerable<(ConstructorAttributes Attributes, ExtendedMethodParameters Parameters)> collection) : base(collection) { }
}
public class UnaryOperatorOverloads : List<(bool Postfix, NStarType ReturnNStarType, NStarType OpdNStarType)>
{
}
public class UnaryOperatorClasses(G.IComparer<BlockStack> comparer) : SortedDictionary<BlockStack, UnaryOperatorOverloads>(comparer)
{
}
public class BinaryOperatorOverloads : List<(NStarType ReturnNStarType, NStarType LeftOpdNStarType, NStarType RightOpdNStarType)>
{
}
public class BinaryOperatorClasses(G.IComparer<BlockStack> comparer) : SortedDictionary<BlockStack, BinaryOperatorOverloads>(comparer)
{
}
public class DestTypes : List<(NStarType DestType, bool Warning)>
{
}
public class ImplicitConversions : Dictionary<BranchCollection, DestTypes>
{
	public ImplicitConversions() : base(new BranchCollectionEComparer())
	{
	}
}
public class OutdatedMethodOverloads : List<(ExtendedMethodParameters Parameters, String UseInstead)>
{
}
public class OutdatedMethods : SortedDictionary<String, OutdatedMethodOverloads>
{
}

public interface IClass { }

public abstract class TImitator
{
	public abstract MpzT Equivalent { get; }
}

public class TZeroImitator : TImitator
{
	public override MpzT Equivalent { get; } = 0;
}

public class TOneImitator : TImitator
{
	public override MpzT Equivalent { get; } = 1;
}

public class TNextImitator<T> : TImitator where T : TImitator, new()
{
	private static readonly T _underlying = new();
	public override MpzT Equivalent { get; } = _underlying.Equivalent + 1;
}

public class TDoubleImitator<T> : TImitator where T : TImitator, new()
{
	private static readonly T _underlying = new();
	public override MpzT Equivalent { get; } = _underlying.Equivalent << 1;
}

public class THexImitator<T> : TImitator where T : TImitator, new()
{
	private static readonly T _underlying = new();
	public override MpzT Equivalent { get; } = _underlying.Equivalent << 4;
}

public class TByteImitator<T> : TImitator where T : TImitator, new()
{
	private static readonly T _underlying = new();
	public override MpzT Equivalent { get; } = _underlying.Equivalent << 8;
}
