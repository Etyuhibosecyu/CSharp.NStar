using Mpir.NET;

namespace CSharp.NStar;

public record struct UserDefinedType(GeneralArrayParameters ArrayParameters, TypeAttributes Attributes, NStarType BaseType, BranchCollection Decomposition);
public record struct UserDefinedProperty(NStarType UnvType, PropertyAttributes Attributes, String DefaultValue);
public record struct UserDefinedConstant(NStarType UnvType, ConstantAttributes Attributes, TreeBranch DefaultValue);
public record struct GeneralArrayParameter(bool ArrayParameterPackage, BranchCollection ArrayParameterRestrictions, BlockStack ArrayParameterType, String ArrayParameterName);
public record struct MethodParameter(String Type, String Name, List<String> ExtraTypes, ParameterAttributes Attributes, String DefaultValue);
public record struct GeneralMethodParameter(NStarType Type, String Name, ParameterAttributes Attributes, String DefaultValue);
public record struct FunctionOverload(List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters);
public record struct GeneralMethodOverload(GeneralArrayParameters ArrayParameters, NStarType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters);

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
public class GeneralTypes(G.IComparer<(BlockStack Container, String Type)> comparer) : SortedDictionary<(BlockStack Container, String Type), (GeneralArrayParameters ArrayParameters, TypeAttributes Attributes)>(comparer)
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
public class TypeProperties : SortedDictionary<String, (NStarType UnvType, PropertyAttributes Attributes)>
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
public class GeneralArrayParameters : List<GeneralArrayParameter>
{
}
public class GeneralMethodParameters : List<GeneralMethodParameter>
{
	public GeneralMethodParameters() : base()
	{
	}

	public GeneralMethodParameters(G.IEnumerable<GeneralMethodParameter> collection) : base(collection)
	{
	}
}
public class GeneralMethodOverloads : List<GeneralMethodOverload>
{
}
public class GeneralMethods : SortedDictionary<String, GeneralMethodOverloads>
{
}
public class UserDefinedMethods : Dictionary<String, GeneralMethodOverloads>
{
}
public class ConstructorOverloads : List<(ConstructorAttributes Attributes, GeneralMethodParameters Parameters)>
{
	public ConstructorOverloads() : base() { }
	public ConstructorOverloads(G.IEnumerable<(ConstructorAttributes Attributes, GeneralMethodParameters Parameters)> collection) : base(collection) { }
}
public class UnaryOperatorOverloads : List<(bool Postfix, NStarType ReturnUnvType, NStarType OpdUnvType)>
{
}
public class UnaryOperatorClasses(G.IComparer<BlockStack> comparer) : SortedDictionary<BlockStack, UnaryOperatorOverloads>(comparer)
{
}
public class BinaryOperatorOverloads : List<(NStarType ReturnUnvType, NStarType LeftOpdUnvType, NStarType RightOpdUnvType)>
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
public class OutdatedMethodOverloads : List<(GeneralMethodParameters Parameters, String UseInstead)>
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
