global using Corlib.NStar;
global using System;
global using System.Drawing;
global using G = System.Collections.Generic;
global using static CSharp.NStar.Constructions;
global using static CSharp.NStar.Executions;
global using static System.Math;
global using String = Corlib.NStar.String;
using System.Diagnostics;
using System.Text;

namespace CSharp.NStar;
public struct DelegateParameters
{
	public TreeBranch? Location { get; private set; }
	public object? Function { get; private set; }
	public Universal? ContainerValue { get; private set; }

	public DelegateParameters(TreeBranch? location, (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, Universal? containerValue = null)
	{
		Location = location;
		Function = function;
		ContainerValue = containerValue;
	}

	public DelegateParameters(TreeBranch? location, (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function, Universal? containerValue = null)
	{
		Location = location;
		Function = function;
		ContainerValue = containerValue;
	}
}

public static partial class Executions
{
	private static readonly List<SortedDictionary<String, (int LocalIndex, UniversalType Type, ulong Timestamp)>> variable_mapping = [];
	private static readonly List<VariablesBlock<bool>?> b_variable = [];
	private static readonly List<VariablesBlock<byte>?> y_variable = [];
	private static readonly List<VariablesBlock<short>?> si_variable = [];
	private static readonly List<VariablesBlock<ushort>?> usi_variable = [];
	private static readonly List<VariablesBlock<char>?> c_variable = [];
	private static readonly List<VariablesBlock<int>?> i_variable = [];
	private static readonly List<VariablesBlock<uint>?> ui_variable = [];
	private static readonly List<VariablesBlock<long>?> li_variable = [];
	private static readonly List<VariablesBlock<ulong>?> uli_variable = [];
	private static readonly List<VariablesBlock<double>?> r_variable = [];
	private static readonly List<VariablesBlock<String>?> s_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<bool>)>?> bl_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<byte>)>?> yl_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<short>)>?> sil_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<ushort>)>?> usil_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<char>)>?> cl_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<int>)>?> il_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<uint>)>?> uil_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<long>)>?> lil_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<ulong>)>?> ulil_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<double>)>?> rl_variable = [];
	private static readonly List<VariablesBlock<(IList<bool>, IList<String>)>?> sl_variable = [];
	private static readonly List<List<Universal>?> unv_variable = [];
	private static readonly List<List<String>?> unv_variable_array_names_list = [];

	private static readonly TypeDictionary<SortedDictionary<String, (int LocalIndex, UniversalType Type, ulong Timestamp)>> property_mapping = [];
	private static readonly TypeDictionary<VariablesBlock<bool>> b_property = [];
	private static readonly TypeDictionary<VariablesBlock<byte>> y_property = [];
	private static readonly TypeDictionary<VariablesBlock<short>> si_property = [];
	private static readonly TypeDictionary<VariablesBlock<ushort>> usi_property = [];
	private static readonly TypeDictionary<VariablesBlock<char>> c_property = [];
	private static readonly TypeDictionary<VariablesBlock<int>> i_property = [];
	private static readonly TypeDictionary<VariablesBlock<uint>> ui_property = [];
	private static readonly TypeDictionary<VariablesBlock<long>> li_property = [];
	private static readonly TypeDictionary<VariablesBlock<ulong>> uli_property = [];
	private static readonly TypeDictionary<VariablesBlock<double>> r_property = [];
	private static readonly TypeDictionary<VariablesBlock<String>> s_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<bool>)>> bl_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<byte>)>> yl_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<short>)>> sil_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<ushort>)>> usil_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<char>)>> cl_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<int>)>> il_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<uint>)>> uil_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<long>)>> lil_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<ulong>)>> ulil_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<double>)>> rl_property = [];
	private static readonly TypeDictionary<VariablesBlock<(IList<bool>, IList<String>)>> sl_property = [];
	private static readonly TypeDictionary2<Universal> unv_property = [];
	private static readonly TypeDictionary2<String> unv_property_array_names_list = [];
	public static readonly String[] operators = ["or", "and", "^^", "||", "&&", "==", "!=", ">=", "<=", ">", "<", "^=", "|=", "&=", ">>=", "<<=", "+=", "-=", "*=", "/=", "%=", "pow=", "=", "^", "|", "&", ">>", "<<", "+", "-", "*", "/", "%", "pow", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "!", "~", "++", "--", "!!"];
	public static readonly bool[] areOperatorsInverted = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false];
	public static readonly bool[] areOperatorsAssignment = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true];

	//const String o_function = " Function ";
	private const int oi_bool = 0, oi_compare = 5, oi_assign_bitwise = 11, oi_assign_arith = 16, oi_assign_arrows = 21, oi_assign_clear = 22, oi_bitwise = 23, oi_arith = 28, oi_arrows = 33;
	private static ulong last_timestamp;
	private static readonly Random random = new();
	private static int random_calls;
	private static readonly double random_initializer = DateTime.Now.ToBinary() / 1E+9;

	public static bool LowLevelVariableExists(String name, int rn) => variable_mapping[rn].ContainsKey(name);

	public static bool LowLevelVariableExistsC(String name, int rn) => variable_mapping[rn].ContainsKey(name) || variable_mapping[rn].ContainsKey("@" + name);

	public static int LowLevelVariableIndexGet(String name, int rn) => variable_mapping[rn].IndexOfKey(name);

	public static int LowLevelVariableIndexGetC(String name, int rn)
	{
		var index = variable_mapping[rn].IndexOfKey(name);
		if (index != -1)
			return index;
		else
			return variable_mapping[rn].IndexOfKey("@" + name);
	}

	public static int LowLevelVariableLocalIndexGet(String name, int rn, int prevIndex = -1)
	{
		var index = prevIndex != -1 ? prevIndex : variable_mapping[rn].IndexOfKey(name);
		if (index != -1)
			return variable_mapping[rn].Values[index].LocalIndex;
		else
			return -1;
	}

	public static int LowLevelVariableLocalIndexGetC(String name, int rn, int prevIndex = -1)
	{
		int index, index2;
		index = prevIndex != -1 ? prevIndex : variable_mapping[rn].IndexOfKey(name);
		index2 = index != -1 ? index : variable_mapping[rn].IndexOfKey("@" + name);
		if (index2 != -1)
			return variable_mapping[rn].Values[index2].LocalIndex;
		else
			return -1;
	}

	public static Universal LowLevelVariableCreate(String name, int rn, UniversalType type, Universal value)
	{
		last_timestamp += (ulong)(1 << random.Next(0, 9));
		if (TypeIsPrimitive(type.MainType))
		{
			return type.MainType.Peek().Name.ToString() switch
			{
				"null" => LowLevel_VariableCreateNull(name, rn, type),
				"bool" => LowLevel_VariableCreate2(b_variable, name, type, rn, value.ToBool(), value.IsNull),
				"byte" => LowLevel_VariableCreate2(y_variable, name, type, rn, value.ToByte(), value.IsNull),
				"short int" => LowLevel_VariableCreate2(si_variable, name, type, rn, value.ToShortInt(), value.IsNull),
				"unsigned short int" => LowLevel_VariableCreate2(usi_variable, name, type, rn, value.ToUnsignedShortInt(), value.IsNull),
				"char" => LowLevel_VariableCreate2(c_variable, name, type, rn, value.ToChar(), value.IsNull),
				"int" => LowLevel_VariableCreate2(i_variable, name, type, rn, value.ToInt(), value.IsNull),
				"unsigned int" => LowLevel_VariableCreate2(ui_variable, name, type, rn, value.ToUnsignedInt(), value.IsNull),
				"long int" => LowLevel_VariableCreate2(li_variable, name, type, rn, value.ToLongInt(), value.IsNull),
				"unsigned long int" => LowLevel_VariableCreate2(uli_variable, name, type, rn, value.ToUnsignedLongInt(), value.IsNull),
				"real" => LowLevel_VariableCreate2(r_variable, name, type, rn, value.ToReal(), value.IsNull),
				"string" => LowLevel_VariableCreate2(s_variable, name, type, rn, value.ToString(), value.IsNull),
				"list" when type.ExtraTypes.Length == 1 && type.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(type.ExtraTypes[0].MainType.Type) => type.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => LowLevel_VariableCreate2(bl_variable, name, type, rn, value.ToBoolList(), value.IsNull),
					"byte" => LowLevel_VariableCreate2(yl_variable, name, type, rn, value.ToByteList(), value.IsNull),
					"short int" => LowLevel_VariableCreate2(sil_variable, name, type, rn, value.ToShortIntList(), value.IsNull),
					"unsigned short int" => LowLevel_VariableCreate2(usil_variable, name, type, rn, value.ToUnsignedShortIntList(), value.IsNull),
					"char" => LowLevel_VariableCreate2(cl_variable, name, type, rn, value.ToCharList(), value.IsNull),
					"int" => LowLevel_VariableCreate2(il_variable, name, type, rn, value.ToIntList(), value.IsNull),
					"unsigned int" => LowLevel_VariableCreate2(uil_variable, name, type, rn, value.ToUnsignedIntList(), value.IsNull),
					"long int" => LowLevel_VariableCreate2(lil_variable, name, type, rn, value.ToLongIntList(), value.IsNull),
					"unsigned long int" => LowLevel_VariableCreate2(ulil_variable, name, type, rn, value.ToUnsignedLongIntList(), value.IsNull),
					"real" => LowLevel_VariableCreate2(rl_variable, name, type, rn, value.ToRealList(), value.IsNull),
					"string" => LowLevel_VariableCreate2(sl_variable, name, type, rn, value.ToStringList(), value.IsNull),
					_ => LowLevel_VariableCreateUnv(name, rn, type, value)
				},
				_ => LowLevel_VariableCreateUnv(name, rn, type, value)
			};
		}
		else
			return LowLevel_VariableCreateUnv(name, rn, type, value);
	}

	private static Universal LowLevel_VariableCreate2<T>(List<VariablesBlock<T>?> blocks_list, String name, UniversalType type, int rn, T value, bool is_null)
	{
		if (blocks_list[rn] is null)
			blocks_list[rn] = new(typeof(bool).IsAssignableTo(typeof(T)) ? (new BitList() is IList<T> list_ ? list_ : throw new ArgumentException(null)) : new List<T>(), new BitList());
		var pos = blocks_list[rn]?.Main?.Length ?? 0;
		variable_mapping[rn].Add(name, (pos, type, last_timestamp));
		if (blocks_list is not null && blocks_list[rn] is not null && blocks_list[rn]!.Main is not null)
		{
			Add(ref blocks_list[rn]!.Main!, value);
			Add(ref blocks_list[rn]!.IsNull!, is_null);
		}
		return Universal.TryConstruct(blocks_list![rn]!.IsNull[pos] ? null : blocks_list![rn]!.Main[pos]);
	}

	private static Universal LowLevel_VariableCreateNull(String name, int rn, UniversalType type)
	{
		variable_mapping[rn].Add(name, (-1, type, last_timestamp));
		return Universal.Null;
	}

	private static Universal LowLevel_VariableCreateUnv(String name, int rn, UniversalType type, Universal value)
	{
		var pos = unv_variable[rn]?.Length ?? 0;
		variable_mapping[rn].Add(name, (pos, type, last_timestamp));
		Add(unv_variable, rn, value.ToType(type, true));
		return unv_variable[rn]?[pos] ?? Universal.Null;
	}

	public static Universal LowLevelVariableGet(String name, int rn, int prevIndex = -1, int prevIndex2 = -1)
	{
		int index, index2, index_c, index2_c;
		index = prevIndex != -1 ? prevIndex : LowLevelVariableIndexGet(name, rn);
		index_c = index != -1 ? index : LowLevelVariableIndexGet("@" + name, rn);
		index2 = prevIndex2 != -1 ? prevIndex2 : LowLevelVariableLocalIndexGet(name, rn, index_c);
		index2_c = index2 != -1 ? index2 : LowLevelVariableLocalIndexGet("@" + name, rn, index_c);
		if (index_c == -1 || index2_c == -1)
			return Universal.Null;
		var UnvType = variable_mapping[rn].Values[index_c].Type;
		if (TypeIsPrimitive(UnvType.MainType))
		{
			return UnvType.MainType.Peek().Name.ToString() switch
			{
				"null" => Universal.Null,
				"bool" => LowLevel_VariableGet2(b_variable[rn], UnvType, index2_c),
				"byte" => LowLevel_VariableGet2(y_variable[rn], UnvType, index2_c),
				"short int" => LowLevel_VariableGet2(si_variable[rn], UnvType, index2_c),
				"unsigned short int" => LowLevel_VariableGet2(usi_variable[rn], UnvType, index2_c),
				"char" => LowLevel_VariableGet2(c_variable[rn], UnvType, index2_c),
				"int" => LowLevel_VariableGet2(i_variable[rn], UnvType, index2_c),
				"unsigned int" => LowLevel_VariableGet2(ui_variable[rn], UnvType, index2_c),
				"long int" => LowLevel_VariableGet2(li_variable[rn], UnvType, index2_c),
				"unsigned long int" => LowLevel_VariableGet2(uli_variable[rn], UnvType, index2_c),
				"real" => LowLevel_VariableGet2(r_variable[rn], UnvType, index2_c),
				"string" => LowLevel_VariableGet2(s_variable[rn], UnvType, index2_c),
				"list" when UnvType.ExtraTypes.Length == 1 && UnvType.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(UnvType.ExtraTypes[0].MainType.Type) => UnvType.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => LowLevel_VariableGet2(bl_variable[rn], UnvType, index2_c),
					"byte" => LowLevel_VariableGet2(yl_variable[rn], UnvType, index2_c),
					"short int" => LowLevel_VariableGet2(sil_variable[rn], UnvType, index2_c),
					"unsigned short int" => LowLevel_VariableGet2(usil_variable[rn], UnvType, index2_c),
					"char" => LowLevel_VariableGet2(cl_variable[rn], UnvType, index2_c),
					"int" => LowLevel_VariableGet2(il_variable[rn], UnvType, index2_c),
					"unsigned int" => LowLevel_VariableGet2(uil_variable[rn], UnvType, index2_c),
					"long int" => LowLevel_VariableGet2(lil_variable[rn], UnvType, index2_c),
					"unsigned long int" => LowLevel_VariableGet2(ulil_variable[rn], UnvType, index2_c),
					"real" => LowLevel_VariableGet2(rl_variable[rn], UnvType, index2_c),
					"string" => LowLevel_VariableGet2(sl_variable[rn], UnvType, index2_c),
					_ => unv_variable[rn]?[index2_c] ?? Universal.Null
				},
				_ => unv_variable[rn]?[index2_c] ?? Universal.Null
			};
		}
		else
			return unv_variable[rn]?[index2_c] ?? Universal.Null;
	}

	private static Universal LowLevel_VariableGet2<T>(VariablesBlock<T>? block, UniversalType type, int index2_c) => Universal.ValidateFixing((block?.IsNull?[index2_c] ?? true) ? Universal.Null : Universal.TryConstruct(block!.Main[index2_c]), type, true);

	public static Universal LowLevelVariableSet(String name, int rn, Universal value, int prevIndex = -1, int prevIndex2 = -1)
	{
		int index, index2;
		index = prevIndex != -1 ? prevIndex : LowLevelVariableIndexGet(name, rn);
		index2 = prevIndex2 != -1 ? prevIndex2 : LowLevelVariableLocalIndexGet(name, rn, index);
		if (index == -1 || index2 == -1)
			return Universal.Null;
		var UnvType = variable_mapping[rn].Values[index].Type;
		if (TypeIsPrimitive(UnvType.MainType))
		{
			return UnvType.MainType.Peek().Name.ToString() switch
			{
				"null" => Universal.Null,
				"bool" => LowLevel_VariableSet2(b_variable[rn], value.ToBool(), value.IsNull, index2),
				"byte" => LowLevel_VariableSet2(y_variable[rn], value.ToByte(), value.IsNull, index2),
				"short int" => LowLevel_VariableSet2(si_variable[rn], value.ToShortInt(), value.IsNull, index2),
				"unsigned short int" => LowLevel_VariableSet2(usi_variable[rn], value.ToUnsignedShortInt(), value.IsNull, index2),
				"char" => LowLevel_VariableSet2(c_variable[rn], value.ToChar(), value.IsNull, index2),
				"int" => LowLevel_VariableSet2(i_variable[rn], value.ToInt(), value.IsNull, index2),
				"unsigned int" => LowLevel_VariableSet2(ui_variable[rn], value.ToUnsignedInt(), value.IsNull, index2),
				"long int" => LowLevel_VariableSet2(li_variable[rn], value.ToLongInt(), value.IsNull, index2),
				"unsigned long int" => LowLevel_VariableSet2(uli_variable[rn], value.ToUnsignedLongInt(), value.IsNull, index2),
				"real" => LowLevel_VariableSet2(r_variable[rn], value.ToReal(), value.IsNull, index2),
				"string" => LowLevel_VariableSet2(s_variable[rn], value.ToString(), value.IsNull, index2),
				"list" when UnvType.ExtraTypes.Length == 1 && UnvType.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(UnvType.ExtraTypes[0].MainType.Type) => UnvType.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => LowLevel_VariableSet2(bl_variable[rn], value.ToBoolList(), value.IsNull, index2),
					"byte" => LowLevel_VariableSet2(yl_variable[rn], value.ToByteList(), value.IsNull, index2),
					"short int" => LowLevel_VariableSet2(sil_variable[rn], value.ToShortIntList(), value.IsNull, index2),
					"unsigned short int" => LowLevel_VariableSet2(usil_variable[rn], value.ToUnsignedShortIntList(), value.IsNull, index2),
					"char" => LowLevel_VariableSet2(cl_variable[rn], value.ToCharList(), value.IsNull, index2),
					"int" => LowLevel_VariableSet2(il_variable[rn], value.ToIntList(), value.IsNull, index2),
					"unsigned int" => LowLevel_VariableSet2(uil_variable[rn], value.ToUnsignedIntList(), value.IsNull, index2),
					"long int" => LowLevel_VariableSet2(lil_variable[rn], value.ToLongIntList(), value.IsNull, index2),
					"unsigned long int" => LowLevel_VariableSet2(ulil_variable[rn], value.ToUnsignedLongIntList(), value.IsNull, index2),
					"real" => LowLevel_VariableSet2(rl_variable[rn], value.ToRealList(), value.IsNull, index2),
					"string" => LowLevel_VariableSet2(sl_variable[rn], value.ToStringList(), value.IsNull, index2),
					_ => LowLevel_VariableSetUnv(rn, value, index2)
				},
				_ => LowLevel_VariableSetUnv(rn, value, index2)
			};
		}
		else
			return LowLevel_VariableSetUnv(rn, value, index2);
	}

	private static Universal LowLevel_VariableSet2<T>(VariablesBlock<T>? block, T value, bool is_null, int index)
	{
		if (block is not null && block.Main is not null && block.IsNull is not null)
		{
			block.Main[index] = value;
			block.IsNull[index] = is_null;
		}
		return (block?.IsNull?[index] ?? true) ? Universal.Null : Universal.TryConstruct(block!.Main![index]);
	}

	private static Universal LowLevel_VariableSetUnv(int rn, Universal value, int index2)
	{
		if (unv_variable[rn] is not null)
			unv_variable[rn]![index2] = value.ToType(value.InnerType);
		return unv_variable[rn]?[index2] ?? Universal.Null;
	}

	public static Universal VariableCreate(String name, UniversalType type, Universal value) => LowLevelVariableCreate(name, variable_mapping.Length - 1, type, value);

	public static Universal VariableGet(String name)
	{
		for (var i = variable_mapping.Length - 1; i >= 0; i--)
		{
			var index = LowLevelVariableIndexGetC(name, i);
			if (index == -1)
			{
				continue;
			}
			var index2 = LowLevelVariableLocalIndexGetC(name, i, index);
			if (index2 == -1)
			{
				continue;
			}
			return LowLevelVariableGet(name, i, index, index2);
		}
		return Universal.Null;
	}

	public static Universal VariableSet(String name, Universal value)
	{
		for (var i = variable_mapping.Length - 1; i >= 0; i--)
		{
			var index = LowLevelVariableIndexGet(name, i);
			if (index == -1)
			{
				continue;
			}
			var index2 = LowLevelVariableLocalIndexGet(name, i, index);
			if (index2 == -1)
			{
				continue;
			}
			return LowLevelVariableSet(name, i, value, index, index2);
		}
		return Universal.Null;
	}

	public static void VariableAddRn()
	{
		variable_mapping.Add([]);
		b_variable.Add(null);
		y_variable.Add(null);
		si_variable.Add(null);
		usi_variable.Add(null);
		c_variable.Add(null);
		i_variable.Add(null);
		ui_variable.Add(null);
		li_variable.Add(null);
		uli_variable.Add(null);
		r_variable.Add(null);
		s_variable.Add(null);
		bl_variable.Add(null);
		yl_variable.Add(null);
		sil_variable.Add(null);
		usil_variable.Add(null);
		cl_variable.Add(null);
		il_variable.Add(null);
		uil_variable.Add(null);
		lil_variable.Add(null);
		ulil_variable.Add(null);
		rl_variable.Add(null);
		sl_variable.Add(null);
		unv_variable.Add(null);
		unv_variable_array_names_list.Add(null);
	}

	public static void VariableRemoveRn()
	{
		variable_mapping.RemoveAt(variable_mapping.Length - 1);
		b_variable.RemoveAt(b_variable.Length - 1);
		y_variable.RemoveAt(y_variable.Length - 1);
		si_variable.RemoveAt(si_variable.Length - 1);
		usi_variable.RemoveAt(usi_variable.Length - 1);
		c_variable.RemoveAt(c_variable.Length - 1);
		i_variable.RemoveAt(i_variable.Length - 1);
		ui_variable.RemoveAt(ui_variable.Length - 1);
		li_variable.RemoveAt(li_variable.Length - 1);
		uli_variable.RemoveAt(uli_variable.Length - 1);
		r_variable.RemoveAt(r_variable.Length - 1);
		s_variable.RemoveAt(s_variable.Length - 1);
		bl_variable.RemoveAt(bl_variable.Length - 1);
		yl_variable.RemoveAt(yl_variable.Length - 1);
		sil_variable.RemoveAt(sil_variable.Length - 1);
		usil_variable.RemoveAt(usil_variable.Length - 1);
		cl_variable.RemoveAt(cl_variable.Length - 1);
		il_variable.RemoveAt(il_variable.Length - 1);
		uil_variable.RemoveAt(uil_variable.Length - 1);
		lil_variable.RemoveAt(lil_variable.Length - 1);
		ulil_variable.RemoveAt(ulil_variable.Length - 1);
		rl_variable.RemoveAt(rl_variable.Length - 1);
		sl_variable.RemoveAt(sl_variable.Length - 1);
		unv_variable.RemoveAt(unv_variable.Length - 1);
		unv_variable_array_names_list.RemoveAt(unv_variable_array_names_list.Length - 1);
	}

	public static void ClearVariables()
	{
		variable_mapping.Clear();
		b_variable.Clear();
		y_variable.Clear();
		si_variable.Clear();
		usi_variable.Clear();
		c_variable.Clear();
		i_variable.Clear();
		ui_variable.Clear();
		li_variable.Clear();
		uli_variable.Clear();
		r_variable.Clear();
		s_variable.Clear();
		bl_variable.Clear();
		yl_variable.Clear();
		sil_variable.Clear();
		usil_variable.Clear();
		cl_variable.Clear();
		il_variable.Clear();
		uil_variable.Clear();
		lil_variable.Clear();
		ulil_variable.Clear();
		rl_variable.Clear();
		sl_variable.Clear();
		unv_variable.Clear();
		unv_variable_array_names_list.Clear();
	}

	public static bool PropertyExists(BlockStack containerType, String name) => property_mapping.TryGetValue(containerType, out var dic) && dic.ContainsKey(name);

	public static bool PropertyExistsC(BlockStack containerType, String name) => property_mapping.TryGetValue(containerType, out var dic) && (dic.ContainsKey(name) || dic.ContainsKey("@" + name));

	public static bool PropertyExistsDeep(BlockStack containerType, String name)
	{
		int index = -1, index2 = -1;
		return CheckContainer(containerType, stack => (index = PropertyIndexGet(stack, name)) != -1 && (index2 = PropertyLocalIndexGet(stack, index)) != -1, out var type);
	}

	public static int PropertyIndexGet(BlockStack containerType, String name) => property_mapping.TryGetValue(containerType, out var dic) ? dic.IndexOfKey(name) : -1;

	public static int PropertyIndexGetC(BlockStack containerType, String name)
	{
		if (!property_mapping.TryGetValue(containerType, out var dic))
			return -1;
		var index = dic.IndexOfKey(name);
		if (index != -1)
		{
			return index;
		}
		else
		{
			return dic.IndexOfKey("@" + name);
		}
	}

	public static int PropertyLocalIndexGet(BlockStack containerType, int index)
	{
		if (index != -1)
		{
			return property_mapping[containerType].Values[index].LocalIndex;
		}
		else
		{
			return -1;
		}
	}

	public static Universal PropertyCreate(BlockStack containerType, String name, UniversalType type, Universal value)
	{
		last_timestamp += (ulong)(1 << random.Next(0, 9));
		if (TypeIsPrimitive(type.MainType))
		{
			return type.MainType.Peek().Name.ToString() switch
			{
				"null" => PropertyCreateNull(containerType, name, type),
				"bool" => PropertyCreate2(b_property, containerType, name, type, value.ToBool(), value.IsNull),
				"byte" => PropertyCreate2(y_property, containerType, name, type, value.ToByte(), value.IsNull),
				"short int" => PropertyCreate2(si_property, containerType, name, type, value.ToShortInt(), value.IsNull),
				"unsigned short int" => PropertyCreate2(usi_property, containerType, name, type, value.ToUnsignedShortInt(), value.IsNull),
				"char" => PropertyCreate2(c_property, containerType, name, type, value.ToChar(), value.IsNull),
				"int" => PropertyCreate2(i_property, containerType, name, type, value.ToInt(), value.IsNull),
				"unsigned int" => PropertyCreate2(ui_property, containerType, name, type, value.ToUnsignedInt(), value.IsNull),
				"long int" => PropertyCreate2(li_property, containerType, name, type, value.ToLongInt(), value.IsNull),
				"unsigned long int" => PropertyCreate2(uli_property, containerType, name, type, value.ToUnsignedLongInt(), value.IsNull),
				"real" => PropertyCreate2(r_property, containerType, name, type, value.ToReal(), value.IsNull),
				"string" => PropertyCreate2(s_property, containerType, name, type, value.ToString(), value.IsNull),
				"list" when type.ExtraTypes.Length == 1 && type.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(type.ExtraTypes[0].MainType.Type) => type.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => PropertyCreate2(bl_property, containerType, name, type, value.ToBoolList(), value.IsNull),
					"byte" => PropertyCreate2(yl_property, containerType, name, type, value.ToByteList(), value.IsNull),
					"short int" => PropertyCreate2(sil_property, containerType, name, type, value.ToShortIntList(), value.IsNull),
					"unsigned short int" => PropertyCreate2(usil_property, containerType, name, type, value.ToUnsignedShortIntList(), value.IsNull),
					"char" => PropertyCreate2(cl_property, containerType, name, type, value.ToCharList(), value.IsNull),
					"int" => PropertyCreate2(il_property, containerType, name, type, value.ToIntList(), value.IsNull),
					"unsigned int" => PropertyCreate2(uil_property, containerType, name, type, value.ToUnsignedIntList(), value.IsNull),
					"long int" => PropertyCreate2(lil_property, containerType, name, type, value.ToLongIntList(), value.IsNull),
					"unsigned long int" => PropertyCreate2(ulil_property, containerType, name, type, value.ToUnsignedLongIntList(), value.IsNull),
					"real" => PropertyCreate2(rl_property, containerType, name, type, value.ToRealList(), value.IsNull),
					"string" => PropertyCreate2(sl_property, containerType, name, type, value.ToStringList(), value.IsNull),
					_ => PropertyCreateUnv(containerType, name, type, value)
				},
				_ => PropertyCreateUnv(containerType, name, type, value)
			};
		}
		else
		{
			return PropertyCreateUnv(containerType, name, type, value);
		}
	}

	private static Universal PropertyCreate2<T>(TypeDictionary<VariablesBlock<T>> blocks_group, BlockStack containerType, String name, UniversalType type, T value, bool is_null)
	{
		property_mapping.TryAdd(containerType, []);
		blocks_group.TryAdd(containerType, new(typeof(bool).IsAssignableTo(typeof(T)) ? (new BitList() is IList<T> list_ ? list_ : throw new ArgumentException(null)) : new List<T>(), new BitList()));
		var pos = blocks_group[containerType].Main?.Length ?? 0;
		property_mapping[containerType].Add(name, (pos, type, last_timestamp));
		Add(ref blocks_group[containerType].Main!, value);
		Add(ref blocks_group[containerType].IsNull!, is_null);
		return Universal.TryConstruct(blocks_group[containerType].Main[pos]);
	}

	private static Universal PropertyCreateNull(BlockStack containerType, String name, UniversalType type)
	{
		property_mapping.TryAdd(containerType, []);
		property_mapping[containerType].Add(name, (-1, type, last_timestamp));
		return Universal.Null;
	}

	private static Universal PropertyCreateUnv(BlockStack containerType, String name, UniversalType type, Universal value)
	{
		property_mapping.TryAdd(containerType, []);
		unv_property.TryAdd(containerType, new List<Universal>());
		var pos = unv_property[containerType].Length;
		property_mapping[containerType].Add(name, (pos, type, last_timestamp));
		unv_property[containerType].Add(value.ToType(type, true));
		return unv_property[containerType][pos];
	}

	public static Universal PropertyGet(BlockStack containerType, String name, int index = -1, int index2 = -1)
	{
		if (index == -1)
			index = PropertyIndexGet(containerType, name);
		if (index == -1)
			index = PropertyIndexGet(containerType, "@" + name);
		if (index2 == -1)
			index2 = PropertyLocalIndexGet(containerType, index);
		if (index2 == -1)
			index2 = PropertyLocalIndexGet(containerType, index);
		if (index == -1 || index2 == -1)
			return Universal.Null;
		var UnvType = property_mapping[containerType].Values[index].Type;
		if (TypeIsPrimitive(UnvType.MainType))
		{
			return UnvType.MainType.Peek().Name.ToString() switch
			{
				"null" => Universal.Null,
				"bool" => LowLevel_VariableGet2(b_property[containerType], UnvType, index2),
				"byte" => LowLevel_VariableGet2(y_property[containerType], UnvType, index2),
				"short int" => LowLevel_VariableGet2(si_property[containerType], UnvType, index2),
				"unsigned short int" => LowLevel_VariableGet2(usi_property[containerType], UnvType, index2),
				"char" => LowLevel_VariableGet2(c_property[containerType], UnvType, index2),
				"int" => LowLevel_VariableGet2(i_property[containerType], UnvType, index2),
				"unsigned int" => LowLevel_VariableGet2(ui_property[containerType], UnvType, index2),
				"long int" => LowLevel_VariableGet2(li_property[containerType], UnvType, index2),
				"unsigned long int" => LowLevel_VariableGet2(uli_property[containerType], UnvType, index2),
				"real" => LowLevel_VariableGet2(r_property[containerType], UnvType, index2),
				"string" => LowLevel_VariableGet2(s_property[containerType], UnvType, index2),
				"list" when UnvType.ExtraTypes.Length == 1 && UnvType.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(UnvType.ExtraTypes[0].MainType.Type) => UnvType.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => LowLevel_VariableGet2(bl_property[containerType], UnvType, index2),
					"byte" => LowLevel_VariableGet2(yl_property[containerType], UnvType, index2),
					"short int" => LowLevel_VariableGet2(sil_property[containerType], UnvType, index2),
					"unsigned short int" => LowLevel_VariableGet2(usil_property[containerType], UnvType, index2),
					"char" => LowLevel_VariableGet2(cl_property[containerType], UnvType, index2),
					"int" => LowLevel_VariableGet2(il_property[containerType], UnvType, index2),
					"unsigned int" => LowLevel_VariableGet2(uil_property[containerType], UnvType, index2),
					"long int" => LowLevel_VariableGet2(lil_property[containerType], UnvType, index2),
					"unsigned long int" => LowLevel_VariableGet2(ulil_property[containerType], UnvType, index2),
					"real" => LowLevel_VariableGet2(rl_property[containerType], UnvType, index2),
					"string" => LowLevel_VariableGet2(sl_property[containerType], UnvType, index2),
					_ => unv_property[containerType][index2]
				},
				_ => unv_property[containerType][index2]
			};
		}
		else
			return unv_property[containerType][index2];
	}

	public static Universal PropertyGet(Universal container, String name)
	{
		if (!UserDefinedPropertiesMapping.TryGetValue(container.InnerType.MainType, out var dic))
			return Universal.Null;
		if (!dic.TryGetValue(name, out var index))
			return Universal.Null;
		return container.GetElement(index + 1);
	}

	public static bool PropertyGetDeep(BlockStack containerType, String name, out Universal value)
	{
		int index = -1, index2 = -1;
		if (CheckContainer(containerType, stack => (index = PropertyIndexGetC(stack, name)) != -1 && (index2 = PropertyLocalIndexGet(stack, index)) != -1, out var type))
		{
			value = PropertyGet(type, name, index, index2);
			return true;
		}
		else
		{
			value = Universal.Null;
			return false;
		}
	}

	public static Universal PropertySet(BlockStack containerType, String name, Universal value, int index = -1, int index2 = -1)
	{
		if (index == -1)
			index = PropertyIndexGet(containerType, name);
		if (index2 == -1)
			index2 = PropertyLocalIndexGet(containerType, index);
		if (index == -1 || index2 == -1)
			return Universal.Null;
		var UnvType = property_mapping[containerType].Values[index].Type;
		if (TypeIsPrimitive(UnvType.MainType))
		{
			return UnvType.MainType.Peek().Name.ToString() switch
			{
				"null" => Universal.Null,
				"bool" => LowLevel_VariableSet2(b_property[containerType], value.ToBool(), value.IsNull, index2),
				"byte" => LowLevel_VariableSet2(y_property[containerType], value.ToByte(), value.IsNull, index2),
				"short int" => LowLevel_VariableSet2(si_property[containerType], value.ToShortInt(), value.IsNull, index2),
				"unsigned short int" => LowLevel_VariableSet2(usi_property[containerType], value.ToUnsignedShortInt(), value.IsNull, index2),
				"char" => LowLevel_VariableSet2(c_property[containerType], value.ToChar(), value.IsNull, index2),
				"int" => LowLevel_VariableSet2(i_property[containerType], value.ToInt(), value.IsNull, index2),
				"unsigned int" => LowLevel_VariableSet2(ui_property[containerType], value.ToUnsignedInt(), value.IsNull, index2),
				"long int" => LowLevel_VariableSet2(li_property[containerType], value.ToLongInt(), value.IsNull, index2),
				"unsigned long int" => LowLevel_VariableSet2(uli_property[containerType], value.ToUnsignedLongInt(), value.IsNull, index2),
				"real" => LowLevel_VariableSet2(r_property[containerType], value.ToReal(), value.IsNull, index2),
				"string" => LowLevel_VariableSet2(s_property[containerType], value.ToString(), value.IsNull, index2),
				"list" when UnvType.ExtraTypes.Length == 1 && UnvType.ExtraTypes[0].MainType.IsValue == false && TypeIsPrimitive(UnvType.ExtraTypes[0].MainType.Type) => UnvType.ExtraTypes[0].MainType.Type.Peek().Name.ToString() switch
				{
					"bool" => LowLevel_VariableSet2(bl_property[containerType], value.ToBoolList(), value.IsNull, index2),
					"byte" => LowLevel_VariableSet2(yl_property[containerType], value.ToByteList(), value.IsNull, index2),
					"short int" => LowLevel_VariableSet2(sil_property[containerType], value.ToShortIntList(), value.IsNull, index2),
					"unsigned short int" => LowLevel_VariableSet2(usil_property[containerType], value.ToUnsignedShortIntList(), value.IsNull, index2),
					"char" => LowLevel_VariableSet2(cl_property[containerType], value.ToCharList(), value.IsNull, index2),
					"int" => LowLevel_VariableSet2(il_property[containerType], value.ToIntList(), value.IsNull, index2),
					"unsigned int" => LowLevel_VariableSet2(uil_property[containerType], value.ToUnsignedIntList(), value.IsNull, index2),
					"long int" => LowLevel_VariableSet2(lil_property[containerType], value.ToLongIntList(), value.IsNull, index2),
					"unsigned long int" => LowLevel_VariableSet2(ulil_property[containerType], value.ToUnsignedLongIntList(), value.IsNull, index2),
					"real" => LowLevel_VariableSet2(rl_property[containerType], value.ToRealList(), value.IsNull, index2),
					"string" => LowLevel_VariableSet2(sl_property[containerType], value.ToStringList(), value.IsNull, index2),
					_ => PropertySetUnv(containerType, value, index2)
				},
				_ => PropertySetUnv(containerType, value, index2)
			};
		}
		else
			return PropertySetUnv(containerType, value, index2);
	}

	public static Universal PropertySet(Universal container, String name, Universal value)
	{
		if (!UserDefinedPropertiesMapping.TryGetValue(container.InnerType.MainType, out var dic) || !dic.TryGetValue(name, out var index))
			return Universal.Null;
		container.SetElement(index + 1, value);
		return container;
	}

	private static Universal PropertySetUnv(BlockStack containerType, Universal value, int index2)
	{
		unv_property[containerType][index2] = value.ToType(value.InnerType);
		return unv_property[containerType][index2];
	}

	public static bool PropertySetDeep(BlockStack containerType, String name, Universal value, out Universal value2)
	{
		int index = -1, index2 = -1;
		if (CheckContainer(containerType, stack => (index = PropertyIndexGet(stack, name)) != -1 && (index2 = PropertyLocalIndexGet(stack, index)) != -1, out var type))
		{
			value2 = PropertySet(type, name, value, index, index2);
			return true;
		}
		else
		{
			value2 = Universal.Null;
			return false;
		}
	}

	public static void ClearProperties()
	{
		property_mapping.Clear();
		b_property.Clear();
		si_property.Clear();
		usi_property.Clear();
		i_property.Clear();
		ui_property.Clear();
		li_property.Clear();
		uli_property.Clear();
		r_property.Clear();
		s_property.Clear();
		bl_property.Clear();
		sil_property.Clear();
		usil_property.Clear();
		il_property.Clear();
		uil_property.Clear();
		lil_property.Clear();
		ulil_property.Clear();
		rl_property.Clear();
		sl_property.Clear();
		unv_property.Clear();
		unv_property_array_names_list.Clear();
	}

	public static Universal ExecuteFunction(Universal containerValue, String function, List<Universal> parameters)
	{
		if (function == "ToString")
		{
			return containerValue.ToString();
		}
		Universal parameters2 = parameters;
		if (containerValue.IsNull)
		{
			if (function?.Length == 0)
			{
				return Universal.Null;
			}
			return function?.ToString() switch
			{
				"Chain" => parameters.Any(x => x.IsNull) ? Universal.Null : Chain(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt()).ToList(x => (Universal)x),
				"Choose" => Choose(parameters),
				"FillList" => parameters2.GetElement(2).IsNull ? Universal.Null : FillList(parameters2.GetElement(1), parameters2.GetElement(2).ToInt()),
				"ListWithSingle" => parameters2.GetElement(1).IsNull ? (List<Universal>)[] : [parameters2.GetElement(1)],
				"RGB" => parameters.Any(x => x.IsNull) ? Universal.Null : Color.FromArgb(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt(), parameters2.GetElement(3).ToInt()).ToArgb(),
				"Abs" => parameters2.GetElement(1).IsNull ? Universal.Null : Abs(parameters2.GetElement(1).ToReal()),
				"Ceil" => parameters2.GetElement(1).IsNull ? Universal.Null : Ceiling(parameters2.GetElement(1).ToReal()),
				"Clamp" => parameters2.GetElement(1).IsNull ? Universal.Null : Clamp(parameters2.GetElement(1), parameters2.GetElement(2), parameters2.GetElement(3)),
				"Fibonacci" => parameters2.GetElement(1).IsNull ? Universal.Null : Fibonacci(parameters2.GetElement(1).ToUnsignedInt()),
				"Floor" => parameters2.GetElement(1).IsNull ? Universal.Null : Floor(parameters2.GetElement(1).ToReal()),
				"Frac" => parameters2.GetElement(1).IsNull ? Universal.Null : parameters2.GetElement(1).ToReal() - Truncate(parameters2.GetElement(1).ToReal()),
				"IntRandom" => parameters2.GetElement(1).IsNull ? Universal.Null : IntRandomNumber(parameters2.GetElement(1).ToInt()),
				"IntToReal" => IntToReal(parameters2),
				"Log" => parameters.Any(x => x.IsNull) ? Universal.Null : Log(parameters2.GetElement(2).ToReal(), parameters2.GetElement(1).ToReal()),
				"Max" => Max(parameters2.GetElement(1), parameters2.GetElement(2)),
				"Max3" => Max(Max(parameters2.GetElement(1), parameters2.GetElement(2)), parameters2.GetElement(3)),
				"Mean" => Mean(parameters2.GetElement(1), parameters2.GetElement(2)),
				"Mean3" => Mean(parameters2.GetElement(1), parameters2.GetElement(2), parameters2.GetElement(3)),
				"Min" => Min(parameters2.GetElement(1), parameters2.GetElement(2)),
				"Min3" => Min(Min(parameters2.GetElement(1), parameters2.GetElement(2)), parameters2.GetElement(3)),
				"Random" => parameters2.GetElement(1).IsNull ? Universal.Null : RandomNumber(parameters2.GetElement(1).ToReal()),
				"RealRemainder" => parameters.Any(x => x.IsNull) ? Universal.Null : RealRemainder(parameters2.GetElement(1).ToReal(), parameters2.GetElement(2).ToReal()),
				"Round" => parameters2.GetElement(1).IsNull ? Universal.Null : Round(parameters2.GetElement(1).ToReal()),
				"Sign" => parameters2.GetElement(1).IsNull ? Universal.Null : Sign(parameters2.GetElement(1).ToReal()),
				"Truncate" => parameters2.GetElement(1).IsNull ? Universal.Null : Truncate(parameters2.GetElement(1).ToReal()),
				_ => Universal.Null,
			};
		}
		var ContainerUnvType = containerValue.InnerType;
		if (TypeIsPrimitive(ContainerUnvType.MainType))
		{
			if (ContainerUnvType.ExtraTypes.Length == 0)
			{
				return ContainerUnvType.MainType.Peek().Name.ToString() switch
				{
					"typename" => ExecuteTypenameFunction(containerValue, function, parameters, parameters2),
					"DateTime" => ExecuteDateTimeFunction(containerValue, function, parameters2),
					"string" => ExecuteStringFunction(containerValue, function, parameters2),
					_ => Universal.Null
				};
			}
			else
			{
				if (ContainerUnvType.MainType.Peek().Name == "list")
					return ExecuteListFunction(containerValue, function, parameters2);
				else
					return Universal.Null;
			}
		}
		else
		{
			return Universal.Null;
		}
	}

	private static Universal ExecuteTypenameFunction(Universal container_value, String function, List<Universal> parameters, Universal parameters2)
	{
		if (!(container_value.GetCustomObject() is UniversalType ValueUnvType && TypeIsPrimitive(ValueUnvType.MainType) && ValueUnvType.ExtraTypes.Length == 0))
		{
			return Universal.Null;
		}
		var basic_type2 = ValueUnvType.MainType.Peek().Name;
		if (basic_type2 == "DateTime")
		{
			return function.ToString() switch
			{
				"Compare" => (parameters2.GetElement(1).IsNull || parameters2.GetElement(2).IsNull) ? Universal.Null : DateTime.Compare(parameters2.GetElement(1).ToDateTime(), parameters2.GetElement(2).ToDateTime()),
				"DaysInMonth" => parameters2.GetElement(2).IsNull ? Universal.Null : DateTime.DaysInMonth(parameters2.GetElement(1).IsNull ? 1 : parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt()),
				"FromBinary" => parameters2.GetElement(1).IsNull ? Universal.Null : new(DateTime.FromBinary(parameters2.GetElement(1).ToLongInt()), GetPrimitiveType("DateTime")),
				"IsLeapYear" => parameters2.GetElement(1).IsNull ? Universal.Null : DateTime.IsLeapYear(parameters2.GetElement(1).ToInt()),
				_ => Universal.Null
			};
		}
		else if (basic_type2 == "string")
		{
			return function.ToString() switch
			{
				"Compare" => CompareStrings(parameters2.GetElement(1), parameters2.GetElement(2)),
				"Concat" => parameters.Length == 0 ? Universal.Null : new String(parameters.Filter(x => x.IsNull == false).Convert(x => x.ToString()).JoinIntoSingle()),
				"Join" => JoinStrings(parameters),
				_ => Universal.Null
			};
		}
		else
		{
			return Universal.Null;
		}
	}

	private static Universal ExecuteDateTimeFunction(Universal container_value, String function, Universal parameters2) => function.ToString() switch
	{
		"AddDays" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddDays(parameters2.GetElement(1).ToReal()), GetPrimitiveType("DateTime")),
		"AddHours" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddHours(parameters2.GetElement(1).ToReal()), GetPrimitiveType("DateTime")),
		"AddMilliseconds" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddMilliseconds(parameters2.GetElement(1).ToReal()), GetPrimitiveType("DateTime")),
		"AddMinutes" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddMinutes(parameters2.GetElement(1).ToReal()), GetPrimitiveType("DateTime")),
		"AddMonths" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddMonths(parameters2.GetElement(1).ToInt()), GetPrimitiveType("DateTime")),
		"AddSeconds" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddSeconds(parameters2.GetElement(1).ToReal()), GetPrimitiveType("DateTime")),
		"AddTicks" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddTicks(parameters2.GetElement(1).ToLongInt()), GetPrimitiveType("DateTime")),
		"AddYears" => parameters2.GetElement(1).IsNull ? Universal.Null : new(container_value.ToDateTime().AddYears(parameters2.GetElement(1).ToInt()), GetPrimitiveType("DateTime")),
		"CompareTo" => parameters2.GetElement(1).IsNull ? Universal.Null : Sign(container_value.ToDateTime().CompareTo(parameters2.GetElement(1).ToDateTime())),
		"IsSummertime" => container_value.ToDateTime().IsDaylightSavingTime(),
		"SpecifyKind" => parameters2.GetElement(1).IsNull ? Universal.Null : new(DateTime.SpecifyKind(container_value.ToDateTime(), parameters2.GetElement(1).IsNull ? DateTimeKind.Unspecified : (DateTimeKind)parameters2.GetElement(1).ToInt()), GetPrimitiveType("DateTime")),
		"ToBinary" => new(container_value.ToDateTime().ToBinary(), LongIntType),
		"ToLocalTime" => new(container_value.ToDateTime().ToLocalTime(), GetPrimitiveType("DateTime")),
		"ToUniversalTime" => new(container_value.ToDateTime().ToUniversalTime(), GetPrimitiveType("DateTime")),
		_ => Universal.Null,
	};

	private static Universal ExecuteStringFunction(Universal container_value, String function, Universal parameters2) => function.ToString() switch
	{
		"Contains" => StringContains(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"ContainsAny" => StringContainsAny(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"ContainsAnyExcluding" => StringContainsAnyExcluding(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"Length" => StringCount(container_value, parameters2.GetElement(1), parameters2.GetElement(2), parameters2.GetElement(3)),
		"EndsWith" => StringEndsWith(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"GetAfter" => parameters2.GetElement(1).IsNull ? Universal.Null : container_value.ToString().GetAfter(parameters2.GetElement(1).ToString()),
		"GetBefore" => parameters2.GetElement(1).IsNull ? Universal.Null : container_value.ToString().GetBefore(parameters2.GetElement(1).ToString()),
		"IndexOf" => StringIndexOf(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"IndexOfAny" => StringIndexOfAny(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"IndexOfAnyExcluding" => StringIndexOfAnyExcluding(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"Insert" => (parameters2.GetElement(1).IsNull || parameters2.GetElement(2).IsNull) ? Universal.Null : container_value.ToString().Insert(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToString()),
		"LastIndexOf" => StringLastIndexOf(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"LastIndexOfAny" => StringLastIndexOfAny(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"LastIndexOfAnyExcluding" => StringLastIndexOfAnyExcluding(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"Remove" => StringRemove(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"Replace" => (parameters2.GetElement(1).IsNull || parameters2.GetElement(2).IsNull) ? Universal.Null : container_value.ToString().Replace(parameters2.GetElement(1).ToString(), parameters2.GetElement(2).ToString()),
		"Split" => parameters2.GetElement(1).IsNull ? Universal.Null : Universal.TryConstruct(new List<String>(container_value.ToString().Split(parameters2.GetElement(1).ToString(), StringSplitOptions.None))),
		"StartsWith" => StringStartsWith(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"Substring" => StringSubstring(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"ToCharList" => StringToCharList(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"ToLower" => container_value.ToString().ToLower(),
		"ToUpper" => container_value.ToString().ToUpper(),
		"Trim" => StringTrim(container_value, parameters2.GetElement(1)),
		"TrimEnd" => StringTrim(container_value, parameters2.GetElement(1), 1),
		"TrimStart" => StringTrim(container_value, parameters2.GetElement(1), 2),
		_ => Universal.Null,
	};

	private static Universal ExecuteListFunction(Universal container_value, String function, Universal parameters2) => function.ToString() switch
	{
		"AddRange" => ListAdd(container_value, parameters2.GetElement(1)),
		"Clear" => ListClear(container_value),
		"Contains" => ListContains(container_value, parameters2.GetElement(1)),
		"GetRange" => ListGetRange(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"IndexOf" => ListIndexOf(container_value, parameters2.GetElement(1)),
		"Insert" => ListInsert(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"LastIndexOf" => ListLastIndexOf(container_value, parameters2.GetElement(1)),
		"Remove" => ListRemove(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		"RemoveLast" => ListRemoveLast(container_value),
		"Reverse" => ListReverse(container_value, parameters2.GetElement(1), parameters2.GetElement(2)),
		_ => Universal.Null,
	};

	public static String FunctionMapping(String function) => function?.ToString() switch
	{
		"Abs" => nameof(Abs),
		"Ceil" => nameof(Ceiling),
		"Clamp" => nameof(Clamp),
		"Chain" => ((String)nameof(Executions)).Add('.').AddRange(nameof(Chain)),
		"Choose" => nameof(Choose),
		"Fibonacci" => nameof(Fibonacci),
		"Fill" => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)),
		"Floor" => nameof(Floor),
		"Frac" => nameof(Frac),
		"IntRandom" => nameof(IntRandomNumber),
		"IntToReal" => "(double)",
		"ListWithSingle" => nameof(ListWithSingle),
		"Log" => nameof(Log),
		"Max" or "Max3" => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Max)),
		"Mean" or "Mean3" => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Mean)),
		"Min" or "Min3" => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Min)),
		"Random" => nameof(RandomNumber),
		"RealRemainder" => nameof(RealRemainder),
		"RGB" => nameof(RGB),
		"Round" => nameof(Round),
		"Sign" => nameof(Sign),
		"Truncate" => nameof(Truncate),
		_ => "",
	};

	public static Universal ExecuteTypicalConstructor(UniversalType constructingType, List<Universal> parameters, (ConstructorAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch Location) realization)
	{
		Universal parameters2 = parameters;
		var index = ConstructorsList.IndexOfKey(constructingType.MainType);
		if (index == -1)
		{
			return Universal.Null;
		}
		var constructors = ConstructorsList.Values[index];
		var constructor_index = constructors.IndexOf(realization);
		if (constructor_index == -1)
		{
			return Universal.Null;
		}
		if (TypeIsPrimitive(constructingType.MainType))
		{
			if (constructingType.ExtraTypes.Length == 0)
			{
				var basic_type = constructingType.MainType.Peek().Name;
				if (basic_type == "DateTime")
				{
					return ExecuteDateTimeConstructor(parameters, parameters2, constructor_index);
				}
				else if (basic_type == "string")
				{
					return ExecuteStringConstructor(parameters, parameters2, constructor_index);
				}
				else
				{
					return Universal.Null;
				}
			}
			else
			{
				return Universal.Null;
			}
		}
		else
		{
			return Universal.Null;
		}
	}

	private static Universal ExecuteDateTimeConstructor(List<Universal> parameters, Universal parameters2, int constructor_index) => constructor_index switch
	{
		0 => parameters.Any(x => x.IsNull) ? Universal.Null : new(new DateTime(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt(), parameters2.GetElement(3).ToInt()), GetPrimitiveType("DateTime")),
		1 => parameters.Take(6).Any(x => x.IsNull) ? Universal.Null : new(new DateTime(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt(), parameters2.GetElement(3).ToInt(), parameters2.GetElement(4).ToInt(), parameters2.GetElement(5).ToInt(), parameters2.GetElement(6).ToInt(), parameters2.GetElement(7).IsNull ? DateTimeKind.Unspecified : (DateTimeKind)parameters2.GetElement(7).ToInt()), GetPrimitiveType("DateTime")),
		2 => parameters.Take(7).Any(x => x.IsNull) ? Universal.Null : new(new DateTime(parameters2.GetElement(1).ToInt(), parameters2.GetElement(2).ToInt(), parameters2.GetElement(3).ToInt(), parameters2.GetElement(4).ToInt(), parameters2.GetElement(5).ToInt(), parameters2.GetElement(6).ToInt(), parameters2.GetElement(7).ToInt(), parameters2.GetElement(8).IsNull ? DateTimeKind.Unspecified : (DateTimeKind)parameters2.GetElement(8).ToInt()), GetPrimitiveType("DateTime")),
		3 => parameters2.GetElement(1).IsNull ? Universal.Null : new(new DateTime(parameters2.GetElement(1).ToLongInt(), parameters2.GetElement(2).IsNull ? DateTimeKind.Unspecified : (DateTimeKind)parameters2.GetElement(2).ToInt()), GetPrimitiveType("DateTime")),
		_ => Universal.Null,
	};

	private static Universal ExecuteStringConstructor(List<Universal> parameters, Universal parameters2, int constructor_index) => constructor_index switch
	{
		0 => parameters.Any(x => x.IsNull) ? Universal.Null : new String(parameters2.GetElement(2).ToInt(), parameters2.GetElement(1).ToChar()),
		_ => Universal.Null,
	};

	public static Universal ExecuteDefaultConstructor(UniversalType constructingType, List<Universal> parameters, (ConstructorAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch Location) realization)
	{
		Universal parameters2 = parameters;
		if (!(UserDefinedTypesList.TryGetValue(SplitType(constructingType.MainType), out var type_descr) && type_descr.Decomposition is not null && type_descr.Decomposition.Length != 0 && UserDefinedConstructorsList.TryGetValue(constructingType.MainType, out var constructors)))
		{
			return Universal.Null;
		}
		var constructor_index = constructors.IndexOf(realization);
		if (constructor_index == -1)
		{
			return Universal.Null;
		}
		List<Universal> list = new(type_descr.Decomposition.Length);
		for (var i = 0; i < parameters.Length && i < type_descr.Decomposition.Length; i++)
		{
			list.Add(parameters2.GetElement(i + 1).ToType((type_descr.Decomposition[i].MainType.Type, type_descr.Decomposition[i].ExtraTypes), true));
		}
		for (var i = parameters.Length; i < type_descr.Decomposition.Length; i++)
		{
			list.Add(PropertyGet(constructingType.MainType, type_descr.Decomposition.ElementAt(i).Key + "#Default").ToType((type_descr.Decomposition[i].MainType.Type, type_descr.Decomposition[i].ExtraTypes), true));
		}
		return ((Universal)list).ToType(new UniversalType(constructingType.MainType, constructingType.ExtraTypes), true);
	}

	public static Universal ExecuteProperty(Universal containerValue, String property/*, List<Universal> indexes*/)
	{
		if (containerValue.IsNull)
		{
			return Universal.Null;
		}
		var ContainerUnvType = containerValue.InnerType;
		if (TypeIsPrimitive(ContainerUnvType.MainType))
		{
			if (ContainerUnvType.ExtraTypes.Length == 0)
			{
				var basic_type = ContainerUnvType.MainType.Peek().Name;
				if (basic_type == "typename")
				{
					return ExecuteTypenameProperty(containerValue, property);
				}
				else if (basic_type == "DateTime")
				{
					return ExecuteDateTimeProperty(containerValue, property);
				}
				if (basic_type == "string")
				{
					return ExecuteStringProperty(containerValue, property);
				}
				else
				{
					return Universal.Null;
				}
			}
			else
			{
				var basic_type = ContainerUnvType.MainType.Peek().Name;
				if (basic_type == "list")
				{
					return ExecuteListProperty(containerValue, property);
				}
				else
				{
					return Universal.Null;
				}
			}
		}
		else
		{
			return PropertyGet(containerValue, property);
		}
	}

	private static Universal ExecuteTypenameProperty(Universal container_value, String property/*, List<Universal> indexes*/)
	{
		if (!(container_value.GetCustomObject() is UniversalType ValueUnvType && TypeIsPrimitive(ValueUnvType.MainType) && ValueUnvType.ExtraTypes.Length == 0))
		{
			return Universal.Null;
		}
		var basic_type2 = ValueUnvType.MainType.Peek().Name;
		if (basic_type2 == "DateTime")
		{
			return property.ToString() switch
			{
				"Now" => new(DateTime.Now, GetPrimitiveType("DateTime")),
				"Today" => new(DateTime.Today, GetPrimitiveType("DateTime")),
				"UTCNow" => new(DateTime.UtcNow, GetPrimitiveType("DateTime")),
				_ => Universal.Null
			};
		}
		else
		{
			return Universal.Null;
		}
	}

	private static Universal ExecuteDateTimeProperty(Universal container_value, String property) => property.ToString() switch
	{
		"Date" => new(container_value.ToDateTime().Date, GetPrimitiveType("DateTime")),
		"Day" => container_value.ToDateTime().Day,
		"DayOfWeek" => (int)container_value.ToDateTime().DayOfWeek,
		"DayOfYear" => container_value.ToDateTime().DayOfYear,
		"Hour" => container_value.ToDateTime().Hour,
		"Kind" => (int)container_value.ToDateTime().Kind,
		"Millisecond" => container_value.ToDateTime().Millisecond,
		"Minute" => container_value.ToDateTime().Minute,
		"Month" => container_value.ToDateTime().Month,
		"Second" => container_value.ToDateTime().Second,
		"Ticks" => new(container_value.ToDateTime().Ticks, LongIntType),
		"TimeOfDay" => new(container_value.ToDateTime().TimeOfDay, GetPrimitiveType("DateTime")),
		"Year" => container_value.ToDateTime().Year,
		_ => Universal.Null,
	};

	private static Universal ExecuteStringProperty(Universal container_value, String property) => property.ToString() switch
	{
		"Length" => container_value.ToString().Length,
		_ => Universal.Null,
	};

	private static Universal ExecuteListProperty(Universal container_value, String property) => property.ToString() switch
	{
		"Last" => ListGetLast(container_value),
		"Length" => ListGetLength(container_value),
		_ => Universal.Null,
	};

	public static Universal ExecuteOperator(Universal x, int operatorIndex, Universal y) => operatorIndex switch
	{
		-127 => y,
		oi_bool + 0 or oi_bool + 3 => Universal.Or(x, y),
		oi_bool + 1 or oi_bool + 4 => Universal.And(x, y),
		oi_bool + 2 => Universal.Xor(x, y),
		oi_compare + 0 => Universal.Eq(x, y),
		oi_compare + 1 => Universal.Neq(x, y),
		oi_compare + 2 => Universal.Goe(x, y),
		oi_compare + 3 => Universal.Loe(x, y),
		oi_compare + 4 => Universal.Gt(x, y),
		oi_compare + 5 => Universal.Lt(x, y),
		oi_assign_bitwise + 0 => VariableSet(y.ToString(), VariableGet(y.ToString()) ^ x),
		oi_assign_bitwise + 1 => VariableSet(y.ToString(), VariableGet(y.ToString()) | x),
		oi_assign_bitwise + 2 => VariableSet(y.ToString(), VariableGet(y.ToString()) & x),
		oi_assign_bitwise + 3 => VariableSet(y.ToString(), VariableGet(y.ToString()) >> x.ToInt()),
		oi_assign_bitwise + 4 => VariableSet(y.ToString(), VariableGet(y.ToString()) << x.ToInt()),
		oi_assign_arith + 0 => VariableSet(y.ToString(), VariableGet(y.ToString()) + x),
		oi_assign_arith + 1 => VariableSet(y.ToString(), VariableGet(y.ToString()) - x),
		oi_assign_arith + 2 => VariableSet(y.ToString(), VariableGet(y.ToString()) * x),
		oi_assign_arith + 3 => VariableSet(y.ToString(), VariableGet(y.ToString()) / x),
		oi_assign_arith + 4 => VariableSet(y.ToString(), VariableGet(y.ToString()) % x),
		oi_assign_arrows + 0 => VariableSet(y.ToString(), Pow(VariableGet(y.ToString()).ToReal(), x.ToReal())),
		oi_assign_clear + 0 => VariableSet(y.ToString(), x),
		oi_bitwise + 0 => x ^ y,
		oi_bitwise + 1 => x | y,
		oi_bitwise + 2 => x & y,
		oi_bitwise + 3 => x >> y.ToInt(),
		oi_bitwise + 4 => x << y.ToInt(),
		oi_arith + 0 => x + y,
		oi_arith + 1 => x - y,
		oi_arith + 2 => x * y,
		oi_arith + 3 => x / y,
		oi_arith + 4 => x % y,
		oi_arrows + 0 => (x.IsNull || y.IsNull) ? Universal.Null : Pow(y.ToReal(), x.ToReal()),
		_ => Universal.Null,
	};

	public static Universal ExecuteUnaryOperator(Universal x, String @operator) => @operator.ToString() switch
	{
		"+" => +x,
		"-" => -x,
		"!" => !x,
		"~" => ~x,
		"postfix !" => Factorial(x.ToUnsignedInt()),
		"ln" => x.IsNull ? Universal.Null : Log(x.ToReal()),
		"sin" => x.IsNull ? Universal.Null : Sin(x.ToReal()),
		"cos" => x.IsNull ? Universal.Null : Cos(x.ToReal()),
		"tan" => x.IsNull ? Universal.Null : Tan(x.ToReal()),
		"asin" => x.IsNull ? Universal.Null : Asin(x.ToReal()),
		"acos" => x.IsNull ? Universal.Null : Acos(x.ToReal()),
		"atan" => x.IsNull ? Universal.Null : Atan(x.ToReal()),
		_ => Universal.Null,
	};

	public static Universal ExecuteAssignment(BlockStack container, List<Universal> target, String @operator, Universal value, bool declaration)
	{
		if (!(target.Length == 2 && TypeEqualsToPrimitive(target[1].InnerType, "string")))
			return Universal.Null;
		else if (TypeEqualsToPrimitive(target[0].InnerType, "typename") && target[0].GetCustomObject() is UniversalType UnvType)
		{
			if (declaration)
				return VariableCreate(target[1].ToString(), UnvType, value);
			else
				return PropertySet(UnvType.MainType, target[1].ToString(), (@operator == "=") ? value : ExecuteOperator(PropertyGet(UnvType.MainType, target[1].ToString()), Array.IndexOf(operators, @operator[..^1]), value));
		}
		else if (!target[0].IsNull)
			return PropertySet(target[0], target[1].ToString(), (@operator == "=") ? value : ExecuteOperator(PropertyGet(target[0], target[1].ToString()), Array.IndexOf(operators, @operator[..^1]), value));
		else if (PropertySetDeep(container, target[1].ToString(), value, out var value2))
			return value2;
		else
			return VariableSet(target[1].ToString(), (@operator == "=") ? value : ExecuteOperator(VariableGet(target[1].ToString()), Array.IndexOf(operators, @operator[..^1]), value));
	}

	public static Universal ExecuteUnaryAssignment(BlockStack container, List<Universal> target, String @operator)
	{
		if (target.Length == 2 && TypeEqualsToPrimitive(target[1].InnerType, "string"))
		{
			Func<String, Universal> Input;
			Func<String, Universal, Universal> Output;
			Universal Assign(Func<Universal, Universal> Convert) => ExecuteUnaryConversion(target[1].ToString(), Input, Convert, Output);
			Universal PostfixAssign(Func<Universal, Universal> Convert) => ExecutePostfixUnaryConversion(target[1].ToString(), Input, Convert, Output);
			if (TypeEqualsToPrimitive(target[0].InnerType, "typename") && target[0].GetCustomObject() is UniversalType UnvType)
			{
				Input = x => PropertyGet(UnvType.MainType, x);
				Output = (x, y) => PropertySet(UnvType.MainType, x, y);
			}
			else
			{
				Input = x => PropertyGetDeep(container, x, out var value) ? value : VariableGet(x);
				Output = (x, y) => PropertySetDeep(container, x, y, out var value) ? value : VariableSet(x, y);
			}
			return @operator.ToString() switch
			{
				"++" => Assign(x => x + 1),
				"--" => Assign(x => x - 1),
				"postfix ++" => PostfixAssign(x => x + 1),
				"postfix --" => PostfixAssign(x => x - 1),
				"!!" => Assign(x => !x),
				_ => Universal.Null,
			};
		}
		else
		{
			return Universal.Null;
		}
	}

	private static Universal ExecuteUnaryConversion(String name, Func<String, Universal> Input, Func<Universal, Universal> Convert, Func<String, Universal, Universal> Output) => Output(name, Convert(Input(name)));

	private static Universal ExecutePostfixUnaryConversion(String name, Func<String, Universal> Input, Func<Universal, Universal> Convert, Func<String, Universal, Universal> Output)
	{
		var temp = Input(name);
		Output(name, Convert(temp));
		return temp;
	}

	private static double RandomNumberBase(int calls, double initializer, double max)
	{
		var a = initializer * 5.29848949848415968;
		var b = Abs(a - Floor(a / 100000) * 100000 + Sin(calls / 1.597486513 + 2.5845984) * 45758.479849894 - 489.498489641984);
		var c = Tan((b - Floor(b / 179.999) * 179.999 - 90) * PI / 180);
		var d = Pow(Abs(Sin(Cos(Tan(calls) * 3.0362187913025793 + 0.10320655487900326) * PI - 2.032198747013) * 146283.032478491032657 - 2903.0267951604) + 0.000001, 2.3065479615036587) + Pow(Abs(Log(Abs(Pow(Pow((double)calls * 123 + 64.0657980165456, 2) + Pow(max - 21.970264984615, 2), 0.5) * 648.0654731649 - 47359.03197931073648) + 0.000001)) + 0.000001, 0.60265497063473049);
		var e = Log(Abs(Pow(Abs(Atan((a - Floor(a / 1000) * 1000 - max) / 169.340493) * 1.905676152049703) + 0.000001, 12.206479803657304) - 382.0654987304) + 0.000001);
		var f = Pow(Abs(c * 1573.06546157302 + d / 51065574.32761504 + e * 1031.3248941027032) + 0.000001, 2.30465546897032);
		return RealRemainder(f, max);
	}

	public static List<int> Chain(int start, int end) => new Chain(start, end - start + 1).ToList();

	public static Universal Choose(List<Universal> variants)
	{
		if (variants.Length == 0)
		{
			return Universal.Null;
		}
		else
		{
			return variants[IntRandomNumber(variants.Length) - 1];
		}
	}

	public static T Choose<T>(List<T> variants) => variants.Random();

	public static Universal FillList(Universal value, int length)
	{
		if (TypeIsPrimitive(value.InnerType.MainType))
		{
			var basic_type = value.InnerType.MainType.Peek().Name;
			if (basic_type == "null")
			{
				return new byte[length].ToList(x => Universal.Null);
			}
			else if (basic_type == "bool")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToBool()));
			}
			else if (basic_type == "byte")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToByte()));
			}
			else if (basic_type == "short int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToShortInt()));
			}
			else if (basic_type == "unsigned short int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToUnsignedShortInt()));
			}
			else if (basic_type == "char")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToChar()));
			}
			else if (basic_type == "int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToInt()));
			}
			else if (basic_type == "unsigned int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToUnsignedInt()));
			}
			else if (basic_type == "long int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToLongInt()));
			}
			else if (basic_type == "unsigned long int")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToUnsignedLongInt()));
			}
			else if (basic_type == "real")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToReal()));
			}
			else if (basic_type == "string")
			{
				return (new byte[length].ToList(x => value.IsNull), new byte[length].ToList(x => value.ToString()));
			}
		}
		return new byte[length].ToList(x => value);
	}

	private static Universal Clamp(Universal x, Universal min, Universal max)
	{
		if (x.IsNull)
		{
			return Universal.Null;
		}
		else if (min.IsNull && max.IsNull)
		{
			return x;
		}
		else if (min.IsNull)
		{
			return Math.Min(x.ToReal(), max.ToReal());
		}
		else if (max.IsNull)
		{
			return Math.Max(x.ToReal(), min.ToReal());
		}
		else
		{
			return Math.Max(min.ToReal(), Math.Min(x.ToReal(), max.ToReal()));
		}
	}

	public static double Factorial(uint x)
	{
		if (x <= 1)
			return 1;
		else if (x > 170)
			return (double)1 / 0;
		else
		{
			double n = 1;
			for (var i = 2; i <= x; i++)
			{
				n *= i;
			}
			return n;
		}
	}

	public static double Fibonacci(uint x)
	{
		if (x <= 1)
		{
			return x;
		}
		else if (x > 1476)
		{
			return 0;
		}
		else
		{
			var a = new double[] { 0, 1, 1 };
			for (var i = 2; i <= (int)x - 1; i++)
			{
				a[0] = a[1];
				a[1] = a[2];
				a[2] = a[0] + a[1];
			}
			return a[2];
		}
	}

	public static double Frac(double x) => x - Truncate(x);

	private static int IntRandomNumber(int max)
	{
		var a = (int)Floor(RandomNumberBase(random_calls, random_initializer, max) + 1);
		random_calls++;
		return a;
	}

	private static Universal IntToReal(Universal parameters2)
	{
		var UnvType = parameters2.GetElement(1).InnerType;
		if (TypeIsPrimitive(UnvType.MainType) && UnvType.ExtraTypes.Length == 0)
		{
			var basic_type = UnvType.MainType.Peek().Name;
			if (NumberTypesList.Contains(basic_type))
			{
				return parameters2.GetElement(1).ToType(RealType, true);
			}
			else
			{
				return Universal.Null;
			}
		}
		else
		{
			return Universal.Null;
		}
	}

	public static List<T> ListWithSingle<T>(T item) => new(item);

	public static Universal Max(Universal x, Universal y)
	{
		if (x.IsNull && y.IsNull)
		{
			return Universal.Null;
		}
		else if (x.IsNull)
		{
			return y.ToReal();
		}
		else if (y.IsNull)
		{
			return x.ToReal();
		}
		else
		{
			return Math.Max(x.ToReal(), y.ToReal());
		}
	}

	public static Universal Mean(params Universal[] values)
	{
		double sum = 0;
		var count = 0;
		for (var i = 0; i < values.Length; i++)
		{
			if (values[i].IsNull == false)
			{
				sum += values[i].ToReal();
				count++;
			}
		}
		if (count == 0)
		{
			return Universal.Null;
		}
		else
		{
			return sum / count;
		}
	}

	public static Universal Min(Universal x, Universal y)
	{
		if (x.IsNull && y.IsNull)
		{
			return Universal.Null;
		}
		else if (x.IsNull)
		{
			return y.ToReal();
		}
		else if (y.IsNull)
		{
			return x.ToReal();
		}
		else
		{
			return Math.Min(x.ToReal(), y.ToReal());
		}
	}

	private static double RandomNumber(double max)
	{
		var a = RandomNumberBase(random_calls, random_initializer, max);
		random_calls++;
		return a;
	}

	public static double RealRemainder(double x, double y) => x - Floor(x / y) * y;

	public static int RGB(int r, int g, int b) => Color.FromArgb(r, g, b).ToArgb();

	private static Universal ListOperation<T>(Universal list1, Universal list2, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func, Action<IList<bool>, IList<bool>> output_func1, Action<IList<T>, IList<T>> output_func2)
	{
		(var IsNullList1, var MainList1) = input_func(list1);
		(var IsNullList2, var MainList2) = input_func(list2);
		output_func1(IsNullList1, IsNullList2);
		output_func2(MainList1, MainList2);
		return Universal.TryConstruct((IsNullList1, MainList1));
	}

	private static Universal ListOperation2(Universal list1, Universal list2, Action<IList<Universal>, IList<Universal>> output_func)
	{
		output_func(list1.ToList(), list2.ToList());
		return list1;
	}

	private static Universal ListOperation<T>(Universal list, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func, Action<IList<bool>> output_func1, Action<IList<T>> output_func2)
	{
		(var IsNullList, var MainList) = input_func(list);
		output_func1(IsNullList);
		output_func2(MainList);
		return Universal.TryConstruct((IsNullList, MainList));
	}

	private static Universal ListOperation2(Universal list, Action<IList<Universal>> output_func)
	{
		output_func(list.ToList());
		return list;
	}

	private static Universal ListOperation<T>(Universal list, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func, Func<IList<bool>, IList<bool>> output_func1, Func<IList<T>, IList<T>> output_func2)
	{
		(var IsNullList, var MainList) = input_func(list);
		return Universal.TryConstruct((output_func1(IsNullList), output_func2(MainList)));
	}

	private static Universal ListOperation2(Universal list, Func<IList<Universal>, IList<Universal>> output_func) => Universal.TryConstruct(output_func(list.ToList()));

	private static Universal ListAdd(Universal list1, Universal list2)
	{
		if (list2.IsNull)
		{
			return list1;
		}
		else
		{
			static void ListAdd2<T>(IList<T> list1_, IList<T> list2_)
			{
				if (list1_ is BitList bitList1 && list2_ is BitList bitList2)
					bitList1.AddRange(bitList2);
				else if (list1_ is List<T> list1__ && list2_ is List<T> list2__)
					list1__.AddRange(list2__);
				else
					throw new ArgumentException(null, nameof(list1) + " or " + nameof(list2));
			}
			if (!(list1.InnerType.ExtraTypes.Length == 1 && !list1.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list1.InnerType.ExtraTypes[0].MainType.Type) && list1.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list1, list2.ToType(list1.InnerType, true), ListAdd2);
			}
			var list_type = list1.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list1, list2, x => x.ToBoolList(), ListAdd2, ListAdd2),
				"byte" => ListOperation(list1, list2, x => x.ToByteList(), ListAdd2, ListAdd2),
				"short int" => ListOperation(list1, list2, x => x.ToShortIntList(), ListAdd2, ListAdd2),
				"unsigned short int" => ListOperation(list1, list2, x => x.ToUnsignedShortIntList(), ListAdd2, ListAdd2),
				"char" => ListOperation(list1, list2, x => x.ToCharList(), ListAdd2, ListAdd2),
				"int" => ListOperation(list1, list2, x => x.ToIntList(), ListAdd2, ListAdd2),
				"unsigned int" => ListOperation(list1, list2, x => x.ToUnsignedIntList(), ListAdd2, ListAdd2),
				"long int" => ListOperation(list1, list2, x => x.ToLongIntList(), ListAdd2, ListAdd2),
				"unsigned long int" => ListOperation(list1, list2, x => x.ToUnsignedLongIntList(), ListAdd2, ListAdd2),
				"real" => ListOperation(list1, list2, x => x.ToRealList(), ListAdd2, ListAdd2),
				"string" => ListOperation(list1, list2, x => x.ToStringList(), ListAdd2, ListAdd2),
				_ => ListOperation2(list1, list2.ToType(list1.InnerType, true), ListAdd2),
			};
		}
	}

	private static Universal ListClear(Universal list)
	{
		static void ListClear2<T>(IList<T> list_) => list_.Clear();
		if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
		{
			return ListOperation2(list, ListClear2);
		}
		var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
		return list_type.ToString() switch
		{
			"bool" => ListOperation(list, x => x.ToBoolList(), ListClear2, ListClear2),
			"byte" => ListOperation(list, x => x.ToByteList(), ListClear2, ListClear2),
			"short int" => ListOperation(list, x => x.ToShortIntList(), ListClear2, ListClear2),
			"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListClear2, ListClear2),
			"char" => ListOperation(list, x => x.ToCharList(), ListClear2, ListClear2),
			"int" => ListOperation(list, x => x.ToIntList(), ListClear2, ListClear2),
			"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListClear2, ListClear2),
			"long int" => ListOperation(list, x => x.ToLongIntList(), ListClear2, ListClear2),
			"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListClear2, ListClear2),
			"real" => ListOperation(list, x => x.ToRealList(), ListClear2, ListClear2),
			"string" => ListOperation(list, x => x.ToStringList(), ListClear2, ListClear2),
			_ => ListOperation2(list, ListClear2),
		};
	}

	private static Universal ListContains(Universal list1, Universal list2)
	{
		if (list2.IsNull)
		{
			return false;
		}
		else
		{
			if (!(list1.InnerType.ExtraTypes.Length == 1 && !list1.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list1.InnerType.ExtraTypes[0].MainType.Type) && list1.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListContains3(list1, list2.ToType(list1.InnerType, true));
			}
			var list_type = list1.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListContains2(list1, list2, x => x.ToBoolList()),
				"byte" => ListContains2(list1, list2, x => x.ToByteList()),
				"short int" => ListContains2(list1, list2, x => x.ToShortIntList()),
				"unsigned short int" => ListContains2(list1, list2, x => x.ToUnsignedShortIntList()),
				"char" => ListContains2(list1, list2, x => x.ToCharList()),
				"int" => ListContains2(list1, list2, x => x.ToIntList()),
				"unsigned int" => ListContains2(list1, list2, x => x.ToUnsignedIntList()),
				"long int" => ListContains2(list1, list2, x => x.ToLongIntList()),
				"unsigned long int" => ListContains2(list1, list2, x => x.ToUnsignedLongIntList()),
				"real" => ListContains2(list1, list2, x => x.ToRealList()),
				"string" => ListContains2(list1, list2, x => x.ToStringList()),
				_ => ListContains3(list1, list2.ToType(list1.InnerType, true)),
			};
		}
	}

	private static bool ListContains2<T>(Universal list1, Universal list2, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func) => ListIndexOf2(list1, list2, input_func) != -1;

	private static bool ListContains3(Universal list1, Universal list2) => ListIndexOf3(list1, list2) != -1;

	public static bool ListEndsWith<T>(NList<T> list1, NList<T> list2) where T : unmanaged
	{
		if (list1.Length == 0 || list2.Length == 0 || list1.Length < list2.Length)
		{
			return false;
		}
		var diff = list1.Length - list2.Length;
		for (var i = list2.Length - 1; i >= 0; i--)
		{
			if (!list1[diff + i].Equals(list2[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static Universal ListGetRange(Universal list, Universal index, Universal count)
	{
		if (index.IsNull)
		{
			return list;
		}
		else if (count.IsNull)
		{
			var index2 = index.ToInt();
			IList<T> ListGetRange2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList && bitList.GetRange(index2 - 1, bitList.Length - index2 + 1) is IList<T> result)
					return result;
				else if (list_ is List<T> list__)
					return list__.GetRange(index2 - 1, list__.Length - index2 + 1);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListGetRange2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListGetRange2, ListGetRange2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListGetRange2, ListGetRange2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListGetRange2, ListGetRange2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListGetRange2, ListGetRange2),
				"char" => ListOperation(list, x => x.ToCharList(), ListGetRange2, ListGetRange2),
				"int" => ListOperation(list, x => x.ToIntList(), ListGetRange2, ListGetRange2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListGetRange2, ListGetRange2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListGetRange2, ListGetRange2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListGetRange2, ListGetRange2),
				"real" => ListOperation(list, x => x.ToRealList(), ListGetRange2, ListGetRange2),
				"string" => ListOperation(list, x => x.ToStringList(), ListGetRange2, ListGetRange2),
				_ => ListOperation2(list, ListGetRange2),
			};
		}
		else
		{
			var index2 = index.ToInt();
			var count2 = count.ToInt();
			IList<T> ListGetRange2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList && bitList.GetRange(index2 - 1, count2) is IList<T> result)
					return result;
				else if (list_ is List<T> list__)
					return list__.GetRange(index2 - 1, count2);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListGetRange2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListGetRange2, ListGetRange2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListGetRange2, ListGetRange2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListGetRange2, ListGetRange2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListGetRange2, ListGetRange2),
				"char" => ListOperation(list, x => x.ToCharList(), ListGetRange2, ListGetRange2),
				"int" => ListOperation(list, x => x.ToIntList(), ListGetRange2, ListGetRange2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListGetRange2, ListGetRange2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListGetRange2, ListGetRange2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListGetRange2, ListGetRange2),
				"real" => ListOperation(list, x => x.ToRealList(), ListGetRange2, ListGetRange2),
				"string" => ListOperation(list, x => x.ToStringList(), ListGetRange2, ListGetRange2),
				_ => ListOperation2(list, ListGetRange2),
			};
		}
	}

	private static Universal ListGetLast(Universal list)
	{
		if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
		{
			return ListGetLast3(list);
		}
		var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
		return list_type.ToString() switch
		{
			"bool" => ListGetLast2(list, x => x.ToBoolList()),
			"byte" => ListGetLast2(list, x => x.ToByteList()),
			"short int" => ListGetLast2(list, x => x.ToShortIntList()),
			"unsigned short int" => ListGetLast2(list, x => x.ToUnsignedShortIntList()),
			"char" => ListGetLast2(list, x => x.ToCharList()),
			"int" => ListGetLast2(list, x => x.ToIntList()),
			"unsigned int" => ListGetLast2(list, x => x.ToUnsignedIntList()),
			"long int" => ListGetLast2(list, x => x.ToLongIntList()),
			"unsigned long int" => ListGetLast2(list, x => x.ToUnsignedLongIntList()),
			"real" => ListGetLast2(list, x => x.ToRealList()),
			"string" => ListGetLast2(list, x => x.ToStringList()),
			_ => ListGetLast3(list),
		};
	}

	private static Universal ListGetLast2<T>(Universal list, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func)
	{
		(var IsNullList, var MainList) = input_func(list);
		if (IsNullList.Length == 0 || IsNullList.Length != MainList.Length)
		{
			return Universal.Null;
		}
		else
		{
			return IsNullList[^1] ? Universal.Null : Universal.TryConstruct(MainList[^1]);
		}
	}

	private static Universal ListGetLast3(Universal list)
	{
		var list2 = list.ToList();
		return list2.Length == 0 ? Universal.Null : list2[^1];
	}

	private static Universal ListGetLength(Universal list)
	{
		if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
		{
			return ListGetLength3(list);
		}
		var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
		return list_type.ToString() switch
		{
			"bool" => ListGetLength2(list, x => x.ToBoolList()),
			"byte" => ListGetLength2(list, x => x.ToByteList()),
			"short int" => ListGetLength2(list, x => x.ToShortIntList()),
			"unsigned short int" => ListGetLength2(list, x => x.ToUnsignedShortIntList()),
			"char" => ListGetLength2(list, x => x.ToCharList()),
			"int" => ListGetLength2(list, x => x.ToIntList()),
			"unsigned int" => ListGetLength2(list, x => x.ToUnsignedIntList()),
			"long int" => ListGetLength2(list, x => x.ToLongIntList()),
			"unsigned long int" => ListGetLength2(list, x => x.ToUnsignedLongIntList()),
			"real" => ListGetLength2(list, x => x.ToRealList()),
			"string" => ListGetLength2(list, x => x.ToStringList()),
			_ => ListGetLength3(list),
		};
	}

	private static Universal ListGetLength2<T>(Universal list, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func)
	{
		(var IsNullList, var MainList) = input_func(list);
		if (IsNullList.Length != MainList.Length)
		{
			return 0;
		}
		else
		{
			return IsNullList.Length;
		}
	}

	private static Universal ListGetLength3(Universal list) => list.GetLength();

	private static Universal ListIndexOf(Universal list1, Universal list2)
	{
		if (list2.IsNull)
		{
			return false;
		}
		else
		{
			if (!(list1.InnerType.ExtraTypes.Length == 1 && !list1.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list1.InnerType.ExtraTypes[0].MainType.Type) && list1.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListIndexOf3(list1, list2.ToType(list1.InnerType, true));
			}
			var list_type = list1.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListIndexOf2(list1, list2, x => x.ToBoolList()),
				"byte" => ListIndexOf2(list1, list2, x => x.ToByteList()),
				"short int" => ListIndexOf2(list1, list2, x => x.ToShortIntList()),
				"unsigned short int" => ListIndexOf2(list1, list2, x => x.ToUnsignedShortIntList()),
				"char" => ListIndexOf2(list1, list2, x => x.ToCharList()),
				"int" => ListIndexOf2(list1, list2, x => x.ToIntList()),
				"unsigned int" => ListIndexOf2(list1, list2, x => x.ToUnsignedIntList()),
				"long int" => ListIndexOf2(list1, list2, x => x.ToLongIntList()),
				"unsigned long int" => ListIndexOf2(list1, list2, x => x.ToUnsignedLongIntList()),
				"real" => ListIndexOf2(list1, list2, x => x.ToRealList()),
				"string" => ListIndexOf2(list1, list2, x => x.ToStringList()),
				_ => ListIndexOf3(list1, list2.ToType(list1.InnerType, true)),
			};
		}
	}

	private static int ListIndexOf2<T>(Universal list1, Universal list2, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func)
	{
		(var IsNullList1, var MainList1) = input_func(list1);
		(var IsNullList2, var MainList2) = input_func(list2);
		return ListIndexOfInternal(IsNullList1, MainList1, IsNullList2, MainList2);
	}

	private static int ListIndexOf3(Universal list1, Universal list2) => list1.ToList().IndexOf(list2.ToList());

	private static int ListIndexOfInternal<T>(IList<bool> IsNullList1, IList<T> MainList1, IList<bool> IsNullList2, IList<T> MainList2)
	{
		if (MainList1.Length == 0 || MainList2.Length == 0 || IsNullList1.Length != MainList1.Length || IsNullList2.Length != MainList2.Length)
		{
			return 0;
		}
		var j = 0;
		for (var i = 0; i - j <= MainList1.Length - MainList2.Length; i++)
		{
			if (IsNullList1[i] == false && (MainList1[i]?.Equals(MainList2[j]) ?? false) || IsNullList2[j] == true)
			{
				j++;
				if (j >= MainList2.Length)
				{
					return i - j + 2;
				}
			}
			else if (j != 0)
			{
				i -= j;
				j = 0;
			}
		}
		return -1;
	}

	private static Universal ListInsert(Universal list1, Universal index, Universal list2)
	{
		if (list2.IsNull)
		{
			return list1;
		}
		else if (index.IsNull)
		{
			return ListAdd(list1, list2);
		}
		else
		{
			var index2 = index.ToInt();
			void ListInsert2<T>(IList<T> list1_, IList<T> list2_)
			{
				if (list1_ is BitList bitList1 && list2_ is BitList bitList2)
					bitList1.Insert(index2, bitList2);
				else if (list1_ is List<T> list1__ && list2_ is List<T> list2__)
					list1__.Insert(index2, list2__);
				else
					throw new ArgumentException(null, nameof(list1) + " or " + nameof(list2));
			}
			if (!(list1.InnerType.ExtraTypes.Length == 1 && !list1.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list1.InnerType.ExtraTypes[0].MainType.Type) && list1.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list1, list2.ToType(list1.InnerType, true), ListInsert2);
			}
			var list_type = list1.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list1, list2, x => x.ToBoolList(), ListInsert2, ListInsert2),
				"byte" => ListOperation(list1, list2, x => x.ToByteList(), ListInsert2, ListInsert2),
				"short int" => ListOperation(list1, list2, x => x.ToShortIntList(), ListInsert2, ListInsert2),
				"unsigned short int" => ListOperation(list1, list2, x => x.ToUnsignedShortIntList(), ListInsert2, ListInsert2),
				"char" => ListOperation(list1, list2, x => x.ToCharList(), ListInsert2, ListInsert2),
				"int" => ListOperation(list1, list2, x => x.ToIntList(), ListInsert2, ListInsert2),
				"unsigned int" => ListOperation(list1, list2, x => x.ToUnsignedIntList(), ListInsert2, ListInsert2),
				"long int" => ListOperation(list1, list2, x => x.ToLongIntList(), ListInsert2, ListInsert2),
				"unsigned long int" => ListOperation(list1, list2, x => x.ToUnsignedLongIntList(), ListInsert2, ListInsert2),
				"real" => ListOperation(list1, list2, x => x.ToRealList(), ListInsert2, ListInsert2),
				"string" => ListOperation(list1, list2, x => x.ToStringList(), ListInsert2, ListInsert2),
				_ => ListOperation2(list1, list2.ToType(list1.InnerType, true), ListInsert2),
			};
		}
	}

	private static Universal ListLastIndexOf(Universal list1, Universal list2)
	{
		if (list2.IsNull)
		{
			return false;
		}
		else
		{
			if (!(list1.InnerType.ExtraTypes.Length == 1 && !list1.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list1.InnerType.ExtraTypes[0].MainType.Type) && list1.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListLastIndexOf3(list1, list2.ToType(list1.InnerType, true));
			}
			var list_type = list1.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListLastIndexOf2(list1, list2, x => x.ToBoolList()),
				"byte" => ListLastIndexOf2(list1, list2, x => x.ToByteList()),
				"short int" => ListLastIndexOf2(list1, list2, x => x.ToShortIntList()),
				"unsigned short int" => ListLastIndexOf2(list1, list2, x => x.ToUnsignedShortIntList()),
				"char" => ListLastIndexOf2(list1, list2, x => x.ToCharList()),
				"int" => ListLastIndexOf2(list1, list2, x => x.ToIntList()),
				"unsigned int" => ListLastIndexOf2(list1, list2, x => x.ToUnsignedIntList()),
				"long int" => ListLastIndexOf2(list1, list2, x => x.ToLongIntList()),
				"unsigned long int" => ListLastIndexOf2(list1, list2, x => x.ToUnsignedLongIntList()),
				"real" => ListLastIndexOf2(list1, list2, x => x.ToRealList()),
				"string" => ListLastIndexOf2(list1, list2, x => x.ToStringList()),
				_ => ListLastIndexOf3(list1, list2.ToType(list1.InnerType, true)),
			};
		}
	}

	private static int ListLastIndexOf2<T>(Universal list1, Universal list2, Func<Universal, (IList<bool> IsNullList, IList<T> MainList)> input_func)
	{
		(var IsNullList1, var MainList1) = input_func(list1);
		(var IsNullList2, var MainList2) = input_func(list2);
		return ListLastIndexOfInternal(IsNullList1, MainList1, IsNullList2, MainList2);
	}

	private static int ListLastIndexOf3(Universal list1, Universal list2) => list1.ToList().LastIndexOf(list2.ToList());

	private static int ListLastIndexOfInternal<T>(IList<bool> IsNullList1, IList<T> MainList1, IList<bool> IsNullList2, IList<T> MainList2)
	{
		if (MainList1.Length == 0 || MainList2.Length == 0 || IsNullList1.Length != MainList1.Length || IsNullList2.Length != MainList2.Length)
		{
			return 0;
		}
		var j = MainList2.Length - 1;
		for (var i = MainList1.Length - 1; i - j <= MainList1.Length - MainList2.Length; i--)
		{
			if (IsNullList1[i] == false && (MainList1[i]?.Equals(MainList2[j]) ?? false) || IsNullList2[j] == true)
			{
				j--;
				if (j < 0)
				{
					return i - j;
				}
			}
			else if (j != 0)
			{
				i += MainList2.Length - 1 - j;
				j = MainList2.Length - 1;
			}
		}
		return -1;
	}

	private static Universal ListRemove(Universal list, Universal index, Universal count)
	{
		if (index.IsNull)
		{
			return list;
		}
		else if (count.IsNull)
		{
			var index2 = index.ToInt();
			void ListRemove2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList)
					bitList.RemoveEnd(index2 - 1);
				else if (list_ is List<T> list__)
					list__.RemoveEnd(index2 - 1);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListRemove2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListRemove2, ListRemove2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListRemove2, ListRemove2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListRemove2, ListRemove2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListRemove2, ListRemove2),
				"char" => ListOperation(list, x => x.ToCharList(), ListRemove2, ListRemove2),
				"int" => ListOperation(list, x => x.ToIntList(), ListRemove2, ListRemove2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListRemove2, ListRemove2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListRemove2, ListRemove2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListRemove2, ListRemove2),
				"real" => ListOperation(list, x => x.ToRealList(), ListRemove2, ListRemove2),
				"string" => ListOperation(list, x => x.ToStringList(), ListRemove2, ListRemove2),
				_ => ListOperation2(list, ListRemove2),
			};
		}
		else
		{
			var index2 = index.ToInt();
			var count2 = count.ToInt();
			void ListRemove2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList)
					bitList.Remove(index2 - 1, count2);
				else if (list_ is List<T> list__)
					list__.Remove(index2 - 1, count2);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListRemove2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListRemove2, ListRemove2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListRemove2, ListRemove2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListRemove2, ListRemove2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListRemove2, ListRemove2),
				"char" => ListOperation(list, x => x.ToCharList(), ListRemove2, ListRemove2),
				"int" => ListOperation(list, x => x.ToIntList(), ListRemove2, ListRemove2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListRemove2, ListRemove2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListRemove2, ListRemove2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListRemove2, ListRemove2),
				"real" => ListOperation(list, x => x.ToRealList(), ListRemove2, ListRemove2),
				"string" => ListOperation(list, x => x.ToStringList(), ListRemove2, ListRemove2),
				_ => ListOperation2(list, ListRemove2),
			};
		}
	}

	private static Universal ListRemoveLast(Universal list)
	{
		static void ListRemoveLast2<T>(IList<T> list_) => list_.RemoveAt(list_.Length - 1);
		if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
		{
			return ListOperation2(list, ListRemoveLast2);
		}
		var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
		return list_type.ToString() switch
		{
			"bool" => ListOperation(list, x => x.ToBoolList(), ListRemoveLast2, ListRemoveLast2),
			"byte" => ListOperation(list, x => x.ToByteList(), ListRemoveLast2, ListRemoveLast2),
			"short int" => ListOperation(list, x => x.ToShortIntList(), ListRemoveLast2, ListRemoveLast2),
			"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListRemoveLast2, ListRemoveLast2),
			"char" => ListOperation(list, x => x.ToCharList(), ListRemoveLast2, ListRemoveLast2),
			"int" => ListOperation(list, x => x.ToIntList(), ListRemoveLast2, ListRemoveLast2),
			"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListRemoveLast2, ListRemoveLast2),
			"long int" => ListOperation(list, x => x.ToLongIntList(), ListRemoveLast2, ListRemoveLast2),
			"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListRemoveLast2, ListRemoveLast2),
			"real" => ListOperation(list, x => x.ToRealList(), ListRemoveLast2, ListRemoveLast2),
			"string" => ListOperation(list, x => x.ToStringList(), ListRemoveLast2, ListRemoveLast2),
			_ => ListOperation2(list, ListRemoveLast2),
		};
	}

	private static Universal ListReverse(Universal list, Universal index, Universal count)
	{
		if (index.IsNull)
		{
			return ListReverse(list, 1, Universal.Null);
		}
		else if (count.IsNull)
		{
			var index2 = index.ToInt();
			void ListReverse2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList)
					bitList.Reverse(index2 - 1, bitList.Length - index2 + 1);
				else if (list_ is List<T> list__)
					list__.Reverse(index2 - 1, list__.Length - index2 + 1);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListReverse2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListReverse2, ListReverse2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListReverse2, ListReverse2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListReverse2, ListReverse2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListReverse2, ListReverse2),
				"char" => ListOperation(list, x => x.ToCharList(), ListReverse2, ListReverse2),
				"int" => ListOperation(list, x => x.ToIntList(), ListReverse2, ListReverse2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListReverse2, ListReverse2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListReverse2, ListReverse2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListReverse2, ListReverse2),
				"real" => ListOperation(list, x => x.ToRealList(), ListReverse2, ListReverse2),
				"string" => ListOperation(list, x => x.ToStringList(), ListReverse2, ListReverse2),
				_ => ListOperation2(list, ListReverse2),
			};
		}
		else
		{
			var index2 = index.ToInt();
			var count2 = count.ToInt();
			void ListReverse2<T>(IList<T> list_)
			{
				if (list_ is BitList bitList)
					bitList.Reverse(index2 - 1, count2);
				else if (list_ is List<T> list__)
					list__.Reverse(index2 - 1, count2);
				else
					throw new ArgumentException(null, nameof(list));
			}
			if (!(list.InnerType.ExtraTypes.Length == 1 && !list.InnerType.ExtraTypes[0].MainType.IsValue && TypeIsPrimitive(list.InnerType.ExtraTypes[0].MainType.Type) && list.InnerType.ExtraTypes[0].ExtraTypes.Length == 0))
			{
				return ListOperation2(list, ListReverse2);
			}
			var list_type = list.InnerType.ExtraTypes[0].MainType.Type.Peek().Name;
			return list_type.ToString() switch
			{
				"bool" => ListOperation(list, x => x.ToBoolList(), ListReverse2, ListReverse2),
				"byte" => ListOperation(list, x => x.ToByteList(), ListReverse2, ListReverse2),
				"short int" => ListOperation(list, x => x.ToShortIntList(), ListReverse2, ListReverse2),
				"unsigned short int" => ListOperation(list, x => x.ToUnsignedShortIntList(), ListReverse2, ListReverse2),
				"char" => ListOperation(list, x => x.ToCharList(), ListReverse2, ListReverse2),
				"int" => ListOperation(list, x => x.ToIntList(), ListReverse2, ListReverse2),
				"unsigned int" => ListOperation(list, x => x.ToUnsignedIntList(), ListReverse2, ListReverse2),
				"long int" => ListOperation(list, x => x.ToLongIntList(), ListReverse2, ListReverse2),
				"unsigned long int" => ListOperation(list, x => x.ToUnsignedLongIntList(), ListReverse2, ListReverse2),
				"real" => ListOperation(list, x => x.ToRealList(), ListReverse2, ListReverse2),
				"string" => ListOperation(list, x => x.ToStringList(), ListReverse2, ListReverse2),
				_ => ListOperation2(list, ListReverse2),
			};
		}
	}

	public static bool ListStartsWith<T>(List<T> list1, List<T> list2)
	{
		if (list1.Length == 0 || list2.Length == 0 || list1.Length < list2.Length)
		{
			return false;
		}
		for (var i = 0; i < list2.Length; i++)
			if (!(list1[i]?.Equals(list2[i]) ?? false))
				return false;
		return true;
	}

	public static short CompareStrings(Universal x, Universal y)
	{
		if (x.IsNull && y.IsNull)
		{
			return 0;
		}
		else if (x.IsNull)
		{
			return 1;
		}
		else if (y.IsNull)
		{
			return -1;
		}
		else
		{
			return (short)Sign(string.Compare(x.ToString().ToString(), y.ToString().ToString()));
		}
	}

	public static Universal JoinStrings(List<Universal> parameters)
	{
		if (parameters.Length < 2 || parameters[1].IsNull)
		{
			return Universal.Null;
		}
		else
		{
			var separator = parameters[0].IsNull ? "" : parameters[0].ToString();
			if (parameters.Length < 4 || parameters[2].IsNull || parameters[3].IsNull)
			{
				return String.Join(separator, [.. parameters[1].ToList().Filter(x => x.IsNull == false).Convert(x => x.ToString())]);
			}
			else
			{
				return String.Join(separator, [.. parameters[1].ToList().Filter(x => x.IsNull == false).Convert(x => x.ToString()).GetSlice(parameters[2].ToInt() - 1, parameters[3].ToInt())]);
			}
		}
	}

	public static Universal StringContains(Universal containerString, Universal substring, Universal ignoreCase)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (ignoreCase.IsNull)
		{
			return containerString.ToString().Contains(substring.ToString());
		}
		else
		{
			return containerString.ToString().Contains(substring.ToString(), ignoreCase.ToBool());
		}
	}

	public static Universal StringContainsAny(Universal containerString, Universal substring, Universal ignoreCase)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else
		{
			var ignoreCase2 = ignoreCase.ToBool();
			var s = containerString.ToString();
			var hs = substring.ToString().ToHashSet();
			for (var i = 0; i < s.Length; i++)
			{
				var c = s[i];
				if ((char.IsLetter(c) && ignoreCase2) ? hs.Contains(char.ToLower(c)) || hs.Contains(char.ToUpper(c)) : hs.Contains(c))
				{
					return true;
				}
			}
			return false;
		}
	}

	public static Universal StringContainsAnyExcluding(Universal containerString, Universal substring, Universal ignoreCase)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else
		{
			var ignoreCase2 = ignoreCase.ToBool();
			var s = containerString.ToString();
			var hs = substring.ToString().ToHashSet();
			for (var i = 0; i < s.Length; i++)
			{
				var c = s[i];
				if ((char.IsLetter(c) && ignoreCase2) ? !hs.Contains(char.ToLower(c)) && !hs.Contains(char.ToUpper(c)) : !hs.Contains(c))
				{
					return true;
				}
			}
			return false;
		}
	}

	public static Universal StringCount(Universal containerString, Universal substring, Universal ignoreCase, Universal guaranteed)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else
		{
			bool ignoreCase2 = ignoreCase.ToBool(), guaranteed2 = guaranteed.ToBool();
			String s = containerString.ToString(), s2 = substring.ToString();
			if (s.Length == 0 || s2.Length == 0)
			{
				return 0;
			}
			int count = 0, j = 0;
			for (var i = 0; i - j <= s.Length - s2.Length; i++)
			{
				var c = s2[j];
				if ((char.IsLetter(c) && ignoreCase2) ? s[i] == char.ToLower(c) || s[i] == char.ToUpper(c) : s[i] == c)
				{
					j++;
					Increase(s2, ref count, ref j);
					continue;
				}
				if (guaranteed2 && j != 0)
				{
					i -= j;
					j = 0;
					continue;
				}
				c = s2[0];
				j = j != 0 && ((char.IsLetter(c) && ignoreCase2) ? s[i] == char.ToLower(c) || s[i] == char.ToUpper(c) : s[i] == c) ? 1 : 0;
			}
			return count;
		}
		static void Increase(String s2, ref int count, ref int j)
		{
			if (j >= s2.Length)
			{
				count++;
				j = 0;
			}
		}
	}

	public static Universal StringEndsWith(Universal containerString, Universal substring, Universal ignoreCase)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (ignoreCase.IsNull)
		{
			return containerString.ToString().EndsWith(substring.ToString());
		}
		else
		{
			return containerString.ToString().EndsWith(substring.ToString(), ignoreCase.ToBool());
		}
	}

	public static Universal StringIndexOf(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (startIndex.IsNull)
		{
			return containerString.ToString().IndexOf(substring.ToString());
		}
		else
		{
			return containerString.ToString().IndexOf(substring.ToString(), startIndex.ToInt());
		}
	}

	public static Universal StringIndexOfAny(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (startIndex.IsNull)
		{
			return containerString.ToString().IndexOfAny(substring.ToString());
		}
		else
		{
			return containerString.ToString().IndexOfAny(substring.ToString(), startIndex.ToInt());
		}
	}

	public static Universal StringIndexOfAnyExcluding(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else
		{
			var s = containerString.ToString();
			var hs = substring.ToString().ToHashSet();
			for (var i = startIndex.IsNull ? 0 : startIndex.ToInt(); i < s.Length; i++)
			{
				if (!hs.Contains(s[i]))
				{
					return i;
				}
			}
			return -1;
		}
	}

	public static Universal StringLastIndexOf(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (startIndex.IsNull)
		{
			return containerString.ToString().LastIndexOf(substring.ToString());
		}
		else
		{
			return containerString.ToString().LastIndexOf(substring.ToString(), startIndex.ToInt());
		}
	}

	public static Universal StringLastIndexOfAny(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (startIndex.IsNull)
		{
			return containerString.ToString().LastIndexOfAny(substring.ToString());
		}
		else
		{
			return containerString.ToString().LastIndexOfAny(substring.ToString(), startIndex.ToInt());
		}
	}

	public static Universal StringLastIndexOfAnyExcluding(Universal containerString, Universal substring, Universal startIndex)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else
		{
			var s = containerString.ToString();
			var hs = substring.ToString().ToHashSet();
			for (var i = startIndex.IsNull ? s.Length - 1 : startIndex.ToInt(); i >= 0; i--)
			{
				if (!hs.Contains(s[i]))
				{
					return i;
				}
			}
			return -1;
		}
	}

	public static Universal StringRemove(Universal containerString, Universal startIndex, Universal length)
	{
		if (containerString.IsNull || startIndex.IsNull)
		{
			return Universal.Null;
		}
		else if (length.IsNull)
		{
			return containerString.ToString().RemoveEnd(startIndex.ToInt());
		}
		else
		{
			return containerString.ToString().Remove(startIndex.ToInt(), length.ToInt());
		}
	}

	public static Universal StringStartsWith(Universal containerString, Universal substring, Universal ignoreCase)
	{
		if (containerString.IsNull || substring.IsNull)
		{
			return Universal.Null;
		}
		else if (ignoreCase.IsNull)
		{
			return containerString.ToString().StartsWith(substring.ToString());
		}
		else
		{
			return containerString.ToString().StartsWith(substring.ToString(), ignoreCase.ToBool());
		}
	}

	public static Universal StringSubstring(Universal containerString, Universal startIndex, Universal length)
	{
		if (containerString.IsNull || startIndex.IsNull)
		{
			return Universal.Null;
		}
		else if (length.IsNull)
		{
			return containerString.ToString()[startIndex.ToInt()..];
		}
		else
		{
			return containerString.ToString().GetRange(startIndex.ToInt(), length.ToInt());
		}
	}

	public static Universal StringToCharList(Universal containerString, Universal startIndex, Universal length)
	{
		if (containerString.IsNull)
		{
			return Universal.Null;
		}
		else if (startIndex.IsNull)
		{
			return Universal.TryConstruct(new String(containerString.ToString().AsSpan()));
		}
		else if (length.IsNull)
		{
			return Universal.TryConstruct(new String(containerString.ToString().Skip(startIndex.ToInt())));
		}
		else
		{
			return Universal.TryConstruct(new String(containerString.ToString().Skip(startIndex.ToInt()).Take(length.ToInt())));
		}
	}

	public static Universal StringTrim(Universal containerString, Universal chars, int mode = 0)
	{
		if (containerString.IsNull)
		{
			return Universal.Null;
		}
		else if (chars.IsNull)
		{
			return (mode == 1) ? containerString.ToString().TrimEnd() : (mode == 2) ? containerString.ToString().TrimStart() : containerString.ToString().Trim();
		}
		else
		{
			var (IsNullList, CharList) = chars.ToCharList();
			var chars2 = IsNullList.Combine(CharList, (x, y) => x ? (char)0 : y).ToArray();
			return (mode == 1) ? containerString.ToString().TrimEnd(chars2) : (mode == 2) ? containerString.ToString().TrimStart(chars2) : containerString.ToString().Trim(chars2);
		}
	}

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
		var index = VariablesList.IndexOfKey(container);
		if (index != -1)
		{
			var list = VariablesList.Values[index];
			var index2 = list.IndexOfKey(type);
			if (index2 != -1)
			{
				var a = list.Values[index2];
				return TypeIsPrimitive(a.MainType) && a.MainType.Peek().Name == "typename" && a.ExtraTypes.Length == 0;
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
		useInstead = "";
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
		useInstead = "";
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
		typeEnd = "";
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
		useInstead = "";
		typeEnd = "";
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
		typeEnd = "";
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
		useInstead = "";
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

	public static bool PropertyExists(BlockStack container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property)
	{
		if (UserDefinedPropertiesList.TryGetValue(container, out var list_) && list_.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		var index = PropertiesList.IndexOfKey(container);
		if (index != -1)
		{
			var list = PropertiesList.Values[index];
			var index2 = list.IndexOfKey(name);
			if (index2 != -1)
			{
				property = list.Values[index2];
				return true;
			}
		}
		property = null;
		return false;
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
		var index = PublicFunctionsList.IndexOfKey(name);
		if (index != -1)
		{
			function = PublicFunctionsList.Values[index];
			return true;
		}
		function = null;
		return false;
	}

	public static bool MethodExists(String container, String name, out (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		var index = MethodsList.IndexOfKey(container);
		if (index != -1)
		{
			var list = MethodsList.Values[index];
			var index2 = list.IndexOfKey(name);
			if (index2 != -1)
			{
				function = list.Values[index2];
				return true;
			}
		}
		function = null;
		return false;
	}

	public static bool MethodExists(BlockStack container, String name, out (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function) => MethodExists(String.Join(".", [.. container.ToList().Convert(x => x.Name)]), name, out function);

	public static bool GeneralMethodExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out bool user)
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

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function) => UserDefinedFunctionExists(container, name, out function, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out BlockStack matchingContainer)
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

	public static TreeBranch WrapFunctionWithDelegate((List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters) function, String functionName, int pos, int endPos, BlockStack container, UniversalType? containerType)
	{
		var extra = PartialTypeToGeneralType(function.ReturnType, function.ReturnExtraTypes);
		TreeBranch branch, branch2 = new("Call", pos, endPos, container) { Extra = extra, Elements = { new("kernel", pos, endPos, container) { Extra = new List<object> { "Function " + functionName, containerType.HasValue ? "method" : "public", function } } } };
		if (function.Parameters is MethodParameters parameters)
		{
			branch = new("Parameters", pos, endPos, container);
			foreach (var x in parameters)
			{
				TreeBranch branch3 = new("Parameter", pos, endPos, container) { Elements = { new("type", pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes) }, new(x.Name, pos, endPos, container) } };
				if ((x.Attributes & ParameterAttributes.Optional) != 0)
				{
					branch3.Add(new("optional", pos, endPos, container));
					branch3.Add(new("Expr", pos, endPos, container) { Elements = { new(Universal.TryParse(x.DefaultValue.ToString(), out _) ? x.DefaultValue : "null", pos, endPos, container) } });
				}
				else
				{
					branch3.Add(new("no optional", pos, endPos, container));
				}
				branch.Add(branch3);
				branch2.Add(new("Hypername", pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes), Elements = { new(x.Name, pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes) } } });
			}
		}
		else
		{
			branch = new("no parameters", pos, endPos, container);
		}
		if (containerType.HasValue && branch2.Elements[0].Extra is List<object> list)
			branch2.Elements[0].Extra = list.Append(new Universal(containerType.Value, GetPrimitiveType("typename")));
		TreeBranch branch4 = new("Main", pos, endPos, container) { Elements = { new("return", pos, endPos, container) { Elements = { new("Expr", pos, endPos, container) { Elements = { new("Hypername", pos, endPos, container) { Elements = { new(functionName + " (function)", pos, endPos, container) { Extra = extra }, branch2 }, Extra = extra } }, Extra = extra } } } } };
		return new("Function", pos, endPos, container) { Elements = { new("_", pos, endPos, container), new("type", pos, endPos, container) { Extra = extra }, branch, branch4 } };
	}

	public static TreeBranch WrapFunctionWithDelegate((GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch Location) function, String functionName, int pos, int endPos, BlockStack container, UniversalType? containerType)
	{
		var extra = function.ReturnUnvType;
		TreeBranch branch, branch2 = new("Call", pos, endPos, container) { Extra = extra, Elements = { new("kernel", pos, endPos, container) { Extra = new List<object> { "Function " + functionName, "general", function } } } };
		if (function.Parameters is GeneralMethodParameters parameters)
		{
			branch = new("Parameters", pos, endPos, container);
			foreach (var x in parameters)
			{
				TreeBranch branch3 = new("Parameter", pos, endPos, container) { Elements = { new("type", pos, endPos, container) { Extra = (x.Type, x.ExtraTypes) }, new(x.Name, pos, endPos, container) } };
				if ((x.Attributes & ParameterAttributes.Optional) != 0)
				{
					branch3.Add(new("optional", pos, endPos, container));
					branch3.Add(new("Expr", pos, endPos, container) { Elements = { new(Universal.TryParse(x.DefaultValue.ToString(), out _) ? x.DefaultValue : "null", pos, endPos, container) } });
				}
				else
				{
					branch3.Add(new("no optional", pos, endPos, container));
				}
				branch.Add(branch3);
				branch2.Add(new("Hypername", pos, endPos, container) { Extra = (x.Type, x.ExtraTypes), Elements = { new(x.Name, pos, endPos, container) { Extra = (x.Type, x.ExtraTypes) } } });
			}
		}
		else
		{
			branch = new("no parameters", pos, endPos, container);
		}
		if (containerType.HasValue && branch2.Elements[0].Extra is List<object> list)
			branch2.Elements[0].Extra = list.Append(new Universal(containerType.Value, GetPrimitiveType("typename")));
		TreeBranch branch4 = new("Main", pos, endPos, container) { Elements = { new("return", pos, endPos, container) { Elements = { new("Expr", pos, endPos, container) { Elements = { new("Hypername", pos, endPos, container) { Elements = { new(functionName + " (function)", pos, endPos, container) { Extra = extra }, branch2 }, Extra = extra } }, Extra = extra } } } } };
		return new("Function", pos, endPos, container) { Elements = { new("_", pos, endPos, container), new("type", pos, endPos, container) { Extra = extra }, branch, branch4 } };
	}

	public static bool ConstructorsExist(BlockStack type, out ConstructorOverloads? constructors)
	{
		var index = ConstructorsList.IndexOfKey(type);
		if (index != -1)
		{
			constructors = [.. ConstructorsList.Values[index]];
			if (constructors.Length != 0)
			{
				return true;
			}
		}
		constructors = null;
		return false;
	}

	public static bool UserDefinedConstructorsExist(BlockStack type, out ConstructorOverloads? constructors)
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

	public static UniversalType GetSubtype(UniversalType type, int levels = 1)
	{
		if (levels <= 0)
		{
			return type;
		}
		else if (levels == 1)
		{
			if (TypeIsPrimitive(type.MainType))
			{
				if (type.MainType.Peek().Name == "list")
				{
					return GetListSubtype(type);
				}
				else
				{
					return NullType;
				}
			}
			else
			{
				return NullType;
			}
		}
		else
		{
			var t = type;
			for (var i = 0; i < levels; i++)
			{
				t = GetSubtype(t);
			}
			return t;
		}
	}

	private static UniversalType GetListSubtype(UniversalType type)
	{
		if (type.ExtraTypes.Length == 1)
		{
			return (type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes);
		}
		else if (!(type.ExtraTypes[0].MainType.IsValue && int.TryParse(type.ExtraTypes[0].MainType.Value.ToString(), out var n)))
		{
			return NullType;
		}
		else if (n <= 2)
		{
			return GetListType(type.ExtraTypes[1]);
		}
		else
		{
			return (ListBlockStack, new GeneralExtraTypes { ((TypeOrValue)(n - 1).ToString(), NoGeneralExtraTypes), type.ExtraTypes[1] });
		}
	}

	public static (int Depth, UniversalType LeafType) GetTypeDepthAndLeafType(UniversalType type)
	{
		var Depth = 0;
		var LeafType = type;
		while (true)
		{
			if (TypeEqualsToPrimitive(LeafType, "list", false))
			{
				if (LeafType.ExtraTypes.Length == 1)
				{
					Depth++;
					LeafType = (LeafType.ExtraTypes[0].MainType.Type, LeafType.ExtraTypes[0].ExtraTypes);
				}
				else if (LeafType.ExtraTypes[0].MainType.IsValue && int.TryParse(LeafType.ExtraTypes[0].MainType.Value.ToString(), out var n))
				{
					Depth += n;
					LeafType = (LeafType.ExtraTypes[1].MainType.Type, LeafType.ExtraTypes[1].ExtraTypes);
				}
				else
				{
					Depth++;
					LeafType = (LeafType.ExtraTypes[1].MainType.Type, LeafType.ExtraTypes[1].ExtraTypes);
				}
			}
			else if (LeafType.MainType.Length != 0 && LeafType.MainType.Peek().Type == BlockType.Class && ListTypesList.Contains(LeafType.MainType.Peek().Name))
			{
				Depth++;
				LeafType = (LeafType.ExtraTypes[^1].MainType.Type, LeafType.ExtraTypes[^1].ExtraTypes);
			}
			else
			{
				return (Depth, LeafType);
			}
		}
	}

	public static UniversalType GetResultType(UniversalType type1, UniversalType type2)
	{
		try
		{
			if (TypesAreEqual(type1, type2))
			{
				return type1;
			}
			if (TypeIsPrimitive(type1.MainType) && TypeIsPrimitive(type2.MainType))
			{
				var left_type = type1.MainType.Peek().Name;
				var right_type = type2.MainType.Peek().Name;
				if (type1.ExtraTypes.Length == 0 && type2.ExtraTypes.Length == 0)
				{
					return GetPrimitiveType(GetPrimitiveResultType(left_type, right_type));
				}
				else if (left_type == "list" || right_type == "list")
				{
					return GetListResultType(type1, type2, left_type, right_type);
				}
				else
				{
					return NullType;
				}
			}
			else
			{
				return NullType;
			}
		}
		catch (StackOverflowException)
		{
			return NullType;
		}
	}

	private static String GetPrimitiveResultType(String left_type, String right_type)
	{
		if (left_type == "dynamic" || right_type == "dynamic")
		{
			return "dynamic";
		}
		else if (left_type == "string" || right_type == "string")
		{
			return "string";
		}
		else if (left_type == "long complex" || right_type == "long complex")
		{
			return "long complex";
		}
		else if (left_type == "long real" || right_type == "long real")
		{
			return "long real";
		}
		else if (left_type == "long long" || right_type == "long long")
		{
			if (left_type == "complex" || right_type == "complex")
			{
				return "long complex";
			}
			else if (left_type == "real" || right_type == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (left_type == "unsigned long long" || right_type == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(right_type))
			{
				return "long long";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (left_type == "complex" || right_type == "complex")
		{
			return "complex";
		}
		else if (left_type == "real" || right_type == "real")
		{
			return "real";
		}
		else if (left_type == "unsigned long int" || right_type == "unsigned long int")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(right_type))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (left_type == "TimeSpan" || right_type == "TimeSpan")
		{
			return "TimeSpan";
		}
		else if (left_type == "DateTime" || right_type == "DateTime")
		{
			return "DateTime";
		}
		else if (left_type == "long int" || right_type == "long int")
		{
			return "long int";
		}
		else if (left_type == "long char" || right_type == "long char")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (left_type == "unsigned int" || right_type == "unsigned int")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (left_type == "int" || right_type == "int")
		{
			return "int";
		}
		else if (left_type == "char" || right_type == "char")
		{
			if (left_type == "short int" || right_type == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (left_type == "unsigned short int" || right_type == "unsigned short int")
		{
			if (left_type == "short int" || right_type == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (left_type == "short int" || right_type == "short int")
		{
			return "short int";
		}
		else if (left_type == "short char" || right_type == "short char")
		{
			return "short char";
		}
		else if (left_type == "byte" || right_type == "byte")
		{
			return "byte";
		}
		else if (left_type == "bool" || right_type == "bool")
		{
			return "bool";
		}
		else if (left_type == "BaseClass" || right_type == "BaseClass")
		{
			return "BaseClass";
		}
		else
		{
			return "null";
		}
	}

	public static String GetQuotientType(String leftType, Universal right, String rightType)
	{
		if (leftType == "long real" || rightType == "long real")
		{
			return "long real";
		}
		else if (leftType == "long long" || rightType == "long long")
		{
			if (leftType == "real" || rightType == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (leftType == "unsigned long long" || rightType == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "real" }.Contains(leftType) || new List<String> { "short int", "int", "long int", "real" }.Contains(rightType))
			{
				return "long real";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (leftType == "real" || rightType == "real")
		{
			return "real";
		}
		else if (rightType == "bool")
		{
			return "byte";
		}
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() >= (ulong)1 << 56)
			{
				return "byte";
			}
			else if (right.ToUnsignedLongInt() >= (ulong)1 << 48)
			{
				return "unsigned short int";
			}
			else if (right.ToUnsignedLongInt() >= 4294967296)
			{
				return "unsigned int";
			}
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() >= (long)1 << 48)
			{
				return "short int";
			}
			else if (right.ToLongInt() >= 4294967296)
			{
				return "int";
			}
			else if (rightType == "unsigned long int")
			{
				return "long long";
			}
			else
			{
				return "long int";
			}
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() >= 16777216)
			{
				return "byte";
			}
			else if (right.ToUnsignedInt() >= 65536)
			{
				return "unsigned short int";
			}
			else if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
			{
				return "long int";
			}
			else if (right.ToInt() >= 65536)
			{
				return "short int";
			}
			else
			{
				return "int";
			}
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (leftType == "unsigned short int")
		{
			if (right.ToUnsignedShortInt() >= 256)
			{
				return "byte";
			}
			else if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (leftType == "short int")
		{
			if (rightType == "unsigned short int")
			{
				return "int";
			}
			else
			{
				return "short int";
			}
		}
		else if (leftType == "short char" || rightType == "short char")
		{
			return "short char";
		}
		else if (leftType.ToString() is "byte" or "bool")
		{
			return "byte";
		}
		else
		{
			return "null";
		}
	}

	public static String GetRemainderType(String leftType, Universal right, String rightType)
	{
		if (leftType == "long real" || rightType == "long real")
		{
			return "long real";
		}
		else if (leftType == "long long" || rightType == "long long")
		{
			if (leftType == "real" || rightType == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (leftType == "unsigned long long" || rightType == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "real" }.Contains(leftType) || new List<String> { "short int", "int", "long int", "real" }.Contains(rightType))
			{
				return "long real";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (leftType == "real" || rightType == "real")
		{
			return "real";
		}
		else if (rightType == "bool")
		{
			return "byte";
		}
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() <= 256)
			{
				return "byte";
			}
			else if (right.ToUnsignedLongInt() <= 65536)
			{
				return "unsigned short int";
			}
			else if (right.ToUnsignedLongInt() <= 4294967296)
			{
				return "unsigned int";
			}
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() <= 32768)
			{
				return "short int";
			}
			else if (right.ToLongInt() <= 2147483648)
			{
				return "int";
			}
			else if (rightType == "unsigned long int")
			{
				return "long long";
			}
			else
			{
				return "long int";
			}
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() <= 256)
			{
				return "byte";
			}
			else if (right.ToUnsignedInt() <= 65536)
			{
				return "unsigned short int";
			}
			else if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
			{
				return "long int";
			}
			else if (right.ToInt() <= 32768)
			{
				return "short int";
			}
			else
			{
				return "int";
			}
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (leftType == "unsigned short int")
		{
			if (right.ToUnsignedShortInt() <= 256)
			{
				return "byte";
			}
			else if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (leftType == "short int")
		{
			if (rightType == "unsigned short int")
			{
				return "int";
			}
			else
			{
				return "short int";
			}
		}
		else if (leftType == "short char" || rightType == "short char")
		{
			return "short char";
		}
		else if (leftType.ToString() is "byte" or "bool")
		{
			return "byte";
		}
		else
		{
			return "null";
		}
	}

	private static UniversalType GetListResultType(UniversalType type1, UniversalType type2, String left_type, String right_type)
	{
		if (ListTypesList.Contains(left_type) || ListTypesList.Contains(right_type))
			return GetListType(GetResultType(GetSubtype(type1), GetSubtype(type2)));
		else if (left_type == "list")
			return GetListType(GetResultType(GetSubtype(type1), (right_type == "list") ? GetSubtype(type2) : type2));
		else
			return GetListType(GetResultType(type1, GetSubtype(type2)));
	}

	public static String TypeToString(UniversalType type)
	{
		try
		{
			if (type.MainType.Length == 0)
			{
				return "null";
			}
			var basic_type = String.Join(".", type.MainType.ToArray(x => (x.Type == BlockType.Unnamed) ? "Unnamed(" + x.Name + ")" : x.Name));
			if (type.ExtraTypes.Length == 0)
			{
				return basic_type;
			}
			else if (basic_type == "list")
			{
				return ListTypeToString(type, basic_type);
			}
			else
			{
				return basic_type + "[" + String.Join(", ",
				[
					.. type.ExtraTypes.Convert(x => x.Value.MainType.IsValue ? x.Value.MainType.Value : TypeToString((x.Value.MainType.Type, x.Value.ExtraTypes))),
				]) + "]";
			}
		}
		catch (StackOverflowException)
		{
			return "null";
		}
	}

	private static String ListTypeToString(UniversalType type, String basic_type)
	{
		if (type.ExtraTypes.Length == 1)
		{
			return basic_type + "() " + (type.ExtraTypes[0].MainType.IsValue ? type.ExtraTypes[0].MainType.Value : TypeToString((type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes)));
		}
		else
		{
			return basic_type + "(" + type.ExtraTypes[0].MainType.Value + ") " + (type.ExtraTypes[1].MainType.IsValue ? type.ExtraTypes[1].MainType.Value : TypeToString((type.ExtraTypes[1].MainType.Type, type.ExtraTypes[1].ExtraTypes)));
		}
	}

	public static UniversalType PartialTypeToGeneralType(String mainType, List<String> extraTypes) => (GetPrimitiveBlockStack(mainType), GetGeneralExtraTypes(extraTypes));

	public static GeneralExtraTypes GetGeneralExtraTypes(List<String> partialBlockStack) => new(partialBlockStack.Convert(x => (UniversalTypeOrValue)((TypeOrValue)new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoGeneralExtraTypes)));

	public static bool TypesAreEqual(UniversalType type1, UniversalType type2)
	{
		if (type1.MainType.Length != type2.MainType.Length)
		{
			return false;
		}
		for (var i = 0; i < type1.MainType.Length; i++)
		{
			if (type1.MainType.ElementAt(i).Type != type2.MainType.ElementAt(i).Type || type1.MainType.ElementAt(i).Name != type2.MainType.ElementAt(i).Name)
			{
				return false;
			}
		}
		if (type1.ExtraTypes.Length == 0)
		{
			if (type2.ExtraTypes.Length != 0)
			{
				return false;
			}
			return true;
		}
		if (type1.ExtraTypes.Length != type2.ExtraTypes.Length)
		{
			return false;
		}
		for (var i = 0; i < type1.ExtraTypes.Length; i++)
		{
			if (type1.ExtraTypes[i].MainType.IsValue ? !(type2.ExtraTypes[i].MainType.IsValue && type1.ExtraTypes[i].MainType.Value == type2.ExtraTypes[i].MainType.Value) : (type2.ExtraTypes[i].MainType.IsValue || !TypesAreEqual((type1.ExtraTypes[i].MainType.Type, type1.ExtraTypes[i].ExtraTypes), (type2.ExtraTypes[i].MainType.Type, type2.ExtraTypes[i].ExtraTypes))))
			{
				return false;
			}
		}
		return true;
	}

	public static (BlockStack Container, String Type) SplitType(BlockStack blockStack) => (new(blockStack.ToList().SkipLast(1)), blockStack.Peek().Name);

	public static bool TypesAreCompatible(UniversalType sourceType, UniversalType destinationType, out bool warning)
	{
		warning = false;
		if (TypesAreEqual(sourceType, destinationType))
			return true;
		if (TypeEqualsToPrimitive(sourceType, "null", false))
			return true;
		if (ImplicitConversionsFromAnythingList.Contains(destinationType, new FullTypeEComparer()))
			return true;
		if (TypeEqualsToPrimitive(sourceType, "list", false) && TypeEqualsToPrimitive(destinationType, "list", false))
		{
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			return SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, "string") || SourceDepth <= DestinationDepth && TypesAreCompatible(SourceLeafType, DestinationLeafType, out warning);
		}
		if (new BlockStackEComparer().Equals(sourceType.MainType, FuncBlockStack) && new BlockStackEComparer().Equals(destinationType.MainType, FuncBlockStack))
		{
			try
			{
				var warning2 = false;
				if (!(sourceType.ExtraTypes.Length >= destinationType.ExtraTypes.Length && destinationType.ExtraTypes.Length >= 1 && !sourceType.ExtraTypes[0].MainType.IsValue && !destinationType.ExtraTypes[0].MainType.IsValue && TypesAreCompatible((sourceType.ExtraTypes[0].MainType.Type, sourceType.ExtraTypes[0].ExtraTypes), (destinationType.ExtraTypes[0].MainType.Type, destinationType.ExtraTypes[0].ExtraTypes), out warning)))
					return false;
				if (destinationType.ExtraTypes.Skip(1).Combine(sourceType.ExtraTypes.Skip(1), (x, y) =>
				{
					var warning3 = false;
					var b = !x.Value.MainType.IsValue && !y.Value.MainType.IsValue && TypesAreCompatible((x.Value.MainType.Type, x.Value.ExtraTypes), (y.Value.MainType.Type, y.Value.ExtraTypes), out warning3);
					warning2 |= warning3;
					return b;
				}).All(x => x))
				{
					warning |= warning2;
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (StackOverflowException)
			{
				return false;
			}
		}
		var index = ImplicitConversionsList.IndexOfKey(sourceType.MainType);
		if (index == -1)
		{
			return false;
		}
		var list = ImplicitConversionsList.Values[index];
		if (!list.TryGetValue(sourceType.ExtraTypes, out var list2))
		{
			return false;
		}
		var index2 = list2.FindIndex(x => TypesAreEqual(x.DestType, destinationType));
		if (index2 != -1)
		{
			warning = list2[index2].Warning;
			return true;
		}
		List<(UniversalType Type, bool Warning)> types_list = [(sourceType, false)];
		List<(UniversalType Type, bool Warning)> new_types_list = [(sourceType, false)];
		while (1 == 1)
		{
			List<(UniversalType Type, bool Warning)> new_types2_list = new(16);
			for (var i = 0; i < new_types_list.Length; i++)
			{
				var new_types3_list = GetCompatibleTypes(new_types_list[i], types_list);
				index2 = new_types3_list.FindIndex(x => TypesAreEqual(x.Type, destinationType));
				if (index2 != -1)
				{
					warning = new_types3_list[index2].Warning;
					return true;
				}
				new_types2_list.AddRange(new_types3_list);
			}
			new_types_list = new(new_types2_list);
			types_list.AddRange(new_types2_list);
			if (new_types2_list.Length == 0)
			{
				break;
			}
		}
		return false;
	}

	public static List<(UniversalType Type, bool Warning)> GetCompatibleTypes((UniversalType Type, bool Warning) source, List<(UniversalType Type, bool Warning)> blackList)
	{
		List<(UniversalType Type, bool Warning)> list = new(16);
		list.AddRange(ImplicitConversionsFromAnythingList.Convert(x => (x, source.Warning)).Filter(x => !blackList.Contains(x)));
		var index = ImplicitConversionsList.IndexOfKey(source.Type.MainType);
		if (index != -1)
		{
			var list2 = ImplicitConversionsList.Values[index];
			if (list2.TryGetValue(source.Type.ExtraTypes, out var list3))
				list.AddRange(list3.Convert(x => (x.DestType, x.Warning || source.Warning)).Filter(x => !blackList.Contains(x)));
		}
		return list;
	}
}
