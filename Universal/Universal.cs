global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.TypeHelpers;
global using static NStar.Core.Extents;
global using static System.Math;
global using String = NStar.Core.String;
using System.Globalization;

namespace CSharp.NStar;
[DebuggerDisplay("{ToString(true)}")]
public struct Universal
{
	private readonly bool Bool;
	private readonly double Number;
	private readonly String String;
	private readonly List<Universal>? NextList;
	private readonly object? Object;
	public UniversalType InnerType { get; set; }
	public UniversalType? OuterType { get; set; }
	public bool Fixed { get; set; }
	public static Universal Infinity => (double)1 / 0;
	public static Universal MinusInfinity => (double)-1 / 0;
	public static Universal Uncertainty => (double)0 / 0;

	private static readonly List<String> ConvertibleTypesList = ["bool", "byte", "short int", "unsigned short int", "char", "int", "unsigned int", "long int", "unsigned long int", "real", "string"];
	private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

	public Universal()
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = null;
		InnerType = NullType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(Universal other)
	{
		if (Fixed)
		{
			var a = other.ToType(InnerType);
			Bool = a.Bool;
			Number = a.Number;
			String = a.String;
			NextList = a.NextList;
			Object = a.Object;
		}
		else
		{
			Bool = other.Bool;
			Number = other.Number;
			String = other.String;
			NextList = other.NextList;
			Object = other.Object;
			InnerType = other.InnerType;
		}
	}

	public Universal(bool @bool)
	{
		Bool = @bool;
		Number = 0;
		String = [];
		NextList = null;
		Object = null;
		InnerType = BoolType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(char @char)
	{
		Bool = false;
		Number = @char;
		String = [];
		NextList = null;
		Object = null;
		InnerType = CharType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(double number)
	{
		Bool = false;
		Number = number;
		String = [];
		NextList = null;
		Object = null;
		if (number >= 0 && number <= 255 && Truncate(number) == number)
			InnerType = ByteType;
		else if (Number >= -32768 && number <= 32767 && Truncate(number) == number)
			InnerType = ShortIntType;
		else if (number >= 0 && number <= 65535 && Truncate(number) == number)
			InnerType = UnsignedShortIntType;
		else if (number >= -2147483648 && number <= 2147483647 && Truncate(number) == number)
			InnerType = IntType;
		else
			InnerType = number >= 0 && number <= 4294967295 && Truncate(number) == number ? UnsignedIntType : RealType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(String @string)
	{
		Bool = false;
		Number = 0;
		String = @string;
		NextList = null;
		Object = null;
		InnerType = StringType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(List<Universal> nextList)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = nextList;
		Object = null;
		InnerType = nextList.Length == 0 ? GetListType(NullType) : GetListType(nextList.Skip(1).Progression(nextList[0].InnerType, (x, y) => GetResultType(x, y.InnerType, "default!", "default!")));
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<bool>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = BitListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<byte>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = ByteListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<short>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = ShortIntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<ushort>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = UnsignedShortIntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<char>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = CharListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<int>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = IntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<uint>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = UnsignedIntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<long>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = LongIntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<ulong>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = UnsignedLongIntListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<double>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = RealListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal((IList<bool>, IList<string>) list)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = null;
		Object = list;
		InnerType = StringListType;
		OuterType = null;
		Fixed = false;
	}

	public Universal(object @object, UniversalType type)
	{
		if (@object is Universal unv)
		{
			Bool = unv.Bool;
			Number = unv.Number;
			String = unv.String;
			NextList = unv.NextList;
			Object = unv.Object;
		}
		else
		{
			Bool = false;
			Number = 0;
			String = [];
			NextList = null;
			Object = @object;
		}
		InnerType = type;
		OuterType = null;
		Fixed = false;
	}

	public static Universal Parse(string s)
	{
		string s2;
		if (s.Length == 0)
			throw new FormatException();
		else if (s == "null")
			return new();
		else if (s is "true" or "false")
			return bool.Parse(s);
		else if (s == "Infty")
			return Infinity;
		else if (s == "-Infty")
			return MinusInfinity;
		else if (s == "Uncty")
			return Uncertainty;
		else if (s == "Pi")
			return PI;
		else if (s == "E")
			return E;
		else if (s[0] is not (>= '0' and <= '9' or '+' or '-') && s[^1] is not ('\"' or '\'' or '\\'))
			throw new FormatException();
		else if (s[^1] == 'i')
			return int.Parse(s[..^1], InvariantCulture);
		else if (s[^1] == 'u')
			return uint.Parse(s[..^1], InvariantCulture);
		else if (s[^1] == 'L')
		{
			s2 = s[..^1];
			if (int.TryParse(s2, out var i))
				return (Universal)i;
			else if (uint.TryParse(s2, out var ui))
				return (Universal)ui;
			else if (long.TryParse(s2, out var l))
				return new(l, LongIntType);
			else
				return new(ulong.Parse(s2), UnsignedLongIntType);
		}
		else if (s[^1] == 'r')
		{
			if ((s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
			{
				double n;
				try
				{
					n = int.Parse(s2, InvariantCulture);
				}
				catch
				{
					n = double.Parse(s2, InvariantCulture);
				}
				return ValidateFixing(n, RealType, true);
			}
			else
				throw new FormatException();
		}
		else if (s[0] == '\"' && s[^1] == '\"')
			return ((String)s).RemoveQuotes();
		else if (s[0] == '\'' && s[^1] == '\'')
			return s.Length <= 2 ? (Universal)'\0' : (Universal)((String)s).RemoveQuotes()[0];
		else if (s.Length >= 3 && s[0] == '@' && s[1] == '\"' && s[^1] == '\"')
			return ((String)s)[2..^1].Replace("\"\"", "\"");
		else if (Quotes.IsRawString(s, out var output))
			return output;
		else
		{
			if (int.TryParse(s, NumberStyles.Integer, InvariantCulture, out var i))
				return i;
			else if (long.TryParse(s, NumberStyles.Integer, InvariantCulture, out var l))
				return (Universal)l;
			else
				return ulong.TryParse(s, NumberStyles.Integer, InvariantCulture, out var ul) ? (Universal)ul : (Universal)double.Parse(s, InvariantCulture);
		}
	}

	public static bool TryParse(string s, out Universal result)
	{
		try
		{
			result = Parse(s);
			return true;
		}
		catch
		{
			result = new();
			return false;
		}
	}

	public static Universal TryConstruct(object? element) => element switch
	{
		null => new(),
		bool b => b,
		byte y => y,
		short si => si,
		ushort usi => usi,
		char c => c,
		int i => i,
		uint ui => ui,
		long li => new(li, LongIntType),
		ulong uli => new(uli, UnsignedLongIntType),
		double r => r,
		String s => s,
		_ => new()
	};

	public static Universal And(Universal x, Universal y) => (Universal)(x.ToBool() && y.ToBool());

	public static Universal Or(Universal x, Universal y) => (Universal)(x.ToBool() || y.ToBool());

	public static Universal Xor(Universal x, Universal y) => (Universal)(x.ToBool() != y.ToBool());

	public static Universal Xor(params List<Universal> list)
	{
		if (list is null)
			return new();
		var trueOccurred = false;
		for (var i = 0; i < list.Length; i++)
		{
			if (list[i].ToBool())
			{
				if (trueOccurred)
					return false;
				else
					trueOccurred = true;
			}
		}
		return true;
	}

	public static Universal Eq(Universal x, Universal y)
	{
		var result_type = GetResultType(x.InnerType, y.InnerType, x.ToString(true), y.ToString(true));
		return x.ToType(result_type, x.Fixed) == y.ToType(result_type, y.Fixed);
	}

	public static Universal Neq(Universal x, Universal y)
	{
		var result_type = GetResultType(x.InnerType, y.InnerType, x.ToString(true), y.ToString(true));
		return x.ToType(result_type, x.Fixed) != y.ToType(result_type, y.Fixed);
	}

	public static Universal Goe(Universal x, Universal y) => (Universal)(x.ToReal() >= y.ToReal());

	public static Universal Loe(Universal x, Universal y) => (Universal)(x.ToReal() <= y.ToReal());

	public static Universal Gt(Universal x, Universal y) => (Universal)(x.ToReal() > y.ToReal());

	public static Universal Lt(Universal x, Universal y) => (Universal)(x.ToReal() < y.ToReal());

	// Set flag to true if you want to try to apply this function.
	public static Universal ValidateFixing(Universal value, UniversalType type, bool flag = false)
	{
		Universal a = new(value);
		if (flag)
		{
			a.InnerType = type;
			a.Fixed = true;
		}
		return a;
	}

	public readonly Universal GetElement(int index)
	{
		if (Object is (IList<bool> BoolIsNullList, IList<bool> BoolList))
			return GetElement2(index, BoolIsNullList, BoolList);
		else if (Object is (IList<bool> ByteIsNullList, IList<byte> ByteList))
			return GetElement2(index, ByteIsNullList, ByteList);
		else if (Object is (IList<bool> ShortIntIsNullList, IList<short> ShortIntList))
			return GetElement2(index, ShortIntIsNullList, ShortIntList);
		else if (Object is (IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList))
			return GetElement2(index, UnsignedShortIntIsNullList, UnsignedShortIntList);
		else if (Object is (IList<bool> CharIsNullList, IList<char> CharList))
			return GetElement2(index, CharIsNullList, CharList);
		else if (Object is (IList<bool> IntIsNullList, IList<int> IntList))
			return GetElement2(index, IntIsNullList, IntList);
		else if (Object is (IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList))
			return GetElement2(index, UnsignedIntIsNullList, UnsignedIntList);
		else if (Object is (IList<bool> LongIntIsNullList, IList<long> LongIntList))
			return GetElement2(index, LongIntIsNullList, LongIntList);
		else if (Object is (IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList))
			return GetElement2(index, UnsignedLongIntIsNullList, UnsignedLongIntList);
		else if (Object is (IList<bool> RealIsNullList, IList<double> RealList))
			return GetElement2(index, RealIsNullList, RealList);
		else if (Object is (IList<bool> StringIsNullList, IList<string> StringList))
			return GetElement2(index, StringIsNullList, StringList);
		else if (TypeEqualsToPrimitive(InnerType, "string"))
		{
			var string_ = ToString();
			return index <= 0 || index > string_.Length ? new() : (Universal)string_[index - 1];
		}
		else
		{
			var list = ToList();
			return index <= 0 || index > list.Length ? new() : list[index - 1];
		}
	}

	private static Universal GetElement2<T>(int index, IList<bool> IsNullList, IList<T> MainList) => index <= 0 || index > MainList.Length ? new() : IsNullList[index - 1] ? new() : TryConstruct(MainList[index - 1]);

	public readonly void SetElement(int index, Universal value)
	{
		if (Object is (IList<bool> BoolIsNullList, IList<bool> BoolList))
			SetElement2(index, BoolIsNullList, BoolList, value.ToBool());
		else if (Object is (IList<bool> ByteIsNullList, IList<byte> ByteList))
			SetElement2(index, ByteIsNullList, ByteList, value.ToByte());
		else if (Object is (IList<bool> ShortIntIsNullList, IList<short> ShortIntList))
			SetElement2(index, ShortIntIsNullList, ShortIntList, value.ToShortInt());
		else if (Object is (IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList))
			SetElement2(index, UnsignedShortIntIsNullList, UnsignedShortIntList, value.ToUnsignedShortInt());
		else if (Object is (IList<bool> CharIsNullList, IList<char> CharList))
			SetElement2(index, CharIsNullList, CharList, value.ToChar());
		else if (Object is (IList<bool> IntIsNullList, IList<int> IntList))
			SetElement2(index, IntIsNullList, IntList, value.ToInt());
		else if (Object is (IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList))
			SetElement2(index, UnsignedIntIsNullList, UnsignedIntList, value.ToUnsignedInt());
		else if (Object is (IList<bool> LongIntIsNullList, IList<long> LongIntList))
			SetElement2(index, LongIntIsNullList, LongIntList, value.ToLongInt());
		else if (Object is (IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList))
			SetElement2(index, UnsignedLongIntIsNullList, UnsignedLongIntList, value.ToUnsignedLongInt());
		else if (Object is (IList<bool> RealIsNullList, IList<double> RealList))
			SetElement2(index, RealIsNullList, RealList, value.ToReal());
		else if (Object is (IList<bool> StringIsNullList, IList<String> StringList))
			SetElement2(index, StringIsNullList, StringList, value.ToString());
		else if (TypeEqualsToPrimitive(InnerType, "string"))
			throw new NotSupportedException("Separate characters in the string cannot be set!");
		else
		{
			var list = ToList();
			if (index <= 0 || index > list.Length)
				return;
			else
				list[index - 1] = value;
		}
	}

	private static void SetElement2<T>(int index, IList<bool> IsNullList, IList<T> MainList, T value)
	{
		if (index <= 0 || index > MainList.Length)
			return;
		else
		{
			IsNullList[index - 1] = false;
			MainList[index - 1] = value;
		}
	}

	public readonly int GetLength() => Object switch
	{
		(IList<bool> BoolIsNullList, IList<bool> BoolList) => GetLength2(BoolIsNullList, BoolList),
		(IList<bool> ByteIsNullList, IList<byte> ByteList) => GetLength2(ByteIsNullList, ByteList),
		(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => GetLength2(ShortIntIsNullList, ShortIntList),
		(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) => GetLength2(UnsignedShortIntIsNullList, UnsignedShortIntList),
		(IList<bool> CharIsNullList, IList<char> CharList) => GetLength2(CharIsNullList, CharList),
		(IList<bool> IntIsNullList, IList<int> IntList) => GetLength2(IntIsNullList, IntList),
		(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) => GetLength2(UnsignedIntIsNullList, UnsignedIntList),
		(IList<bool> LongIntIsNullList, IList<long> LongIntList) => GetLength2(LongIntIsNullList, LongIntList),
		(IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList) => GetLength2(UnsignedLongIntIsNullList, UnsignedLongIntList),
		(IList<bool> RealIsNullList, IList<double> RealList) => GetLength2(RealIsNullList, RealList),
		(IList<bool> StringIsNullList, IList<string> StringList) => GetLength2(StringIsNullList, StringList),
		_ => TypeEqualsToPrimitive(InnerType, "string") ? ToString().Length : ToList().Length
	};

	private static int GetLength2<T>(IList<bool> IsNullList, IList<T> MainList) => IsNullList.Length != MainList.Length ? 0 : MainList.Length;

	public readonly bool ToBool()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return false;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => false,
			"bool" => Bool,
			"byte" => !(Number < 1),
			"short int" => !(Number < 1),
			"unsigned short int" => !(Number < 1),
			"char" => !(Number < 1),
			"int" => !(Number < 1),
			"unsigned int" => !(Number < 1),
			"long int" => !(Object is not long li || li < 1),
			"DateTime" => !(Object is not DateTime dt || dt.Ticks < 1),
			"unsigned long int" => !(Object is not ulong uli || uli < 1),
			"real" => !(Number < 1),
			"string" => !(String == ""),
			"list" => !(NextList == null || NextList.Length == 0) && NextList[0].ToBool(),
			_ => false
		};
	}

	public readonly byte ToByte()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (byte)(Bool == false ? 0 : 1),
			"byte" => (byte)Number,
			"short int" => (byte)(Number is < (-255) or > 255 ? 0 : Abs(Number)),
			"unsigned short int" => (byte)(Number is < (-255) or > 255 ? 0 : Number),
			"char" => (byte)(Number is < (-255) or > 255 ? 0 : Number),
			"int" => (byte)(Number is < (-255) or > 255 ? 0 : Abs(Number)),
			"unsigned int" => (byte)(Number is < (-255) or > 255 ? 0 : Number),
			"long int" => (byte)(Object is not long li ? 0 : li is < (-255) or > 255 ? 0 : Abs(li)),
			"DateTime" => (byte)(Object is not DateTime dt ? 0 : dt.Ticks > 255 ? 0 : dt.Ticks),
			"unsigned long int" => (byte)(Object is not ulong uli ? 0 : uli > 255 ? 0 : uli),
			"real" => (byte)(Number is < (-255) or > 255 ? 0 : Floor(Abs(Number))),
			"string" => 0,
			"list" => (byte)(NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToByte()),
			_ => 0
		};
	}

	public readonly short ToShortInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (short)(Bool == false ? 0 : 1),
			"byte" => (short)Number,
			"short int" => (short)Number,
			"unsigned short int" => (short)Number,
			"char" => (short)Number,
			"int" => (short)(Number is < (-32768) or > 32767 ? 0 : Number),
			"unsigned int" => (short)(Number > 32767 ? 0 : Number),
			"long int" => (short)(Object is not long li ? 0 : li is < (-32768) or > 32767 ? 0 : li),
			"DateTime" => (short)(Object is not DateTime dt ? 0 : dt.Ticks > 32767 ? 0 : dt.Ticks),
			"unsigned long int" => (short)(Object is not ulong uli ? 0 : uli > 32767 ? 0 : uli),
			"real" => (short)(Number is < (-32768) or > 32767 ? 0 : Floor(Number)),
			"string" => 0,
			"list" => (short)(NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToShortInt()),
			_ => 0
		};
	}

	public readonly ushort ToUnsignedShortInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (ushort)(Bool == false ? 0 : 1),
			"byte" => (ushort)Number,
			"short int" => (ushort)Number,
			"unsigned short int" => (ushort)Number,
			"char" => (ushort)Number,
			"int" => (ushort)(Number is < (-65535) or > 65535 ? 0 : Abs(Number)),
			"unsigned int" => (ushort)(Number is < (-65535) or > 65535 ? 0 : Number),
			"long int" => (ushort)(Object is not long li ? 0 : li is < (-65535) or > 65535 ? 0 : Abs(li)),
			"DateTime" => (ushort)(Object is not DateTime dt ? 0 : dt.Ticks > 65535 ? 0 : dt.Ticks),
			"unsigned long int" => (ushort)(Object is not ulong uli ? 0 : uli > 65535 ? 0 : uli),
			"real" => (ushort)(Number is < (-65535) or > 65535 ? 0 : Floor(Abs(Number))),
			"string" => 0,
			"list" => (ushort)(NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedShortInt()),
			_ => 0
		};
	}

	public readonly char ToChar()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return '\0';
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => '\0',
			"bool" => (char)(Bool == false ? 0 : 1),
			"byte" => (char)Number,
			"short int" => (char)Number,
			"unsigned short int" => (char)Number,
			"char" => (char)Number,
			"int" => (char)(Number is < (-65535) or > 65535 ? 0 : Abs(Number)),
			"unsigned int" => (char)(Number is < (-65535) or > 65535 ? 0 : Number),
			"long int" => (char)(Object is not long li ? 0 : li is < (-65535) or > 65535 ? 0 : Abs(li)),
			"DateTime" => (char)(Object is not DateTime dt ? 0 : dt.Ticks > 65535 ? 0 : dt.Ticks),
			"unsigned long int" => (char)(Object is not ulong uli ? 0 : uli > 65535 ? 0 : uli),
			"real" => (char)(Number is < (-65535) or > 65535 ? 0 : Floor(Abs(Number))),
			"string" => (char)0,
			"list" => (char)(NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToChar()),
			_ => '\0'
		};
	}

	public readonly int ToInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => Bool == false ? 0 : 1,
			"byte" => (int)Number,
			"short int" => (int)Number,
			"unsigned short int" => (int)Number,
			"char" => (int)Number,
			"int" => (int)Number,
			"unsigned int" => (int)Number,
			"long int" => (int)(Object is not long li ? 0 : li is < (-2147483648) or > 2147483647 ? 0 : li),
			"DateTime" => (int)(Object is not DateTime dt ? 0 : dt.Ticks > 2147483647 ? 0 : dt.Ticks),
			"unsigned long int" => (int)(Object is not ulong uli ? 0 : uli > 2147483647 ? 0 : uli),
			"real" => (int)(Number is < (-2147483648) or > 2147483647 ? 0 : Floor(Number)),
			"string" => 0,
			"list" => NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToInt(),
			_ => 0
		};
	}

	public readonly uint ToUnsignedInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (uint)(Bool == false ? 0 : 1),
			"byte" => (uint)Number,
			"short int" => (uint)Abs(Number),
			"unsigned short int" => (uint)Number,
			"char" => (uint)Number,
			"int" => (uint)Number,
			"unsigned int" => (uint)Number,
			"long int" => (uint)(Object is not long li ? 0 : li is < (-4294967295) or > 4294967295 ? 0 : Abs(li)),
			"DateTime" => (uint)(Object is not DateTime dt ? 0 : dt.Ticks > 4294967295 ? 0 : dt.Ticks),
			"unsigned long int" => (uint)(Object is not ulong uli ? 0 : uli > 4294967295 ? 0 : uli),
			"real" => (uint)(Number is < (-4294967295) or > 4294967295 ? 0 : Floor(Abs(Number))),
			"string" => 0,
			"list" => NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedInt(),
			_ => 0
		};
	}

	public readonly long ToLongInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (Bool == false) ? 0 : 1,
			"byte" => (long)Number,
			"short int" => (long)Number,
			"unsigned short int" => (long)Number,
			"char" => (long)Number,
			"int" => (long)Number,
			"unsigned int" => (long)Number,
			"long int" => (Object is not long li) ? 0 : li,
			"DateTime" => (Object is not DateTime dt) ? 0 : dt.Ticks,
			"unsigned long int" => (long)((Object is not ulong uli) ? 0 : uli),
			"real" => (long)((Number is < (-(double)9223372036854775808) or > 9223372036854775807) ? 0 : Floor(Number)),
			"string" => 0,
			"list" => (NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToLongInt(),
			_ => 0
		};
	}

	public readonly DateTime ToDateTime() => TypeEqualsToPrimitive(InnerType, "DateTime") ? (Object is not DateTime dt) ? new(0) : dt : new(ToLongInt());

	public readonly ulong ToUnsignedLongInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => (ulong)(Bool == false ? 0 : 1),
			"byte" => (ulong)Number,
			"short int" => (ulong)Abs(Number),
			"unsigned short int" => (ulong)Number,
			"char" => (ulong)Number,
			"int" => (ulong)Number,
			"unsigned int" => (ulong)Number,
			"long int" => (ulong)(Object is not long li ? 0 : Abs(li)),
			"DateTime" => (ulong)(Object is not DateTime dt ? 0 : dt.Ticks),
			"unsigned long int" => Object is not ulong uli ? 0 : uli,
			"real" => (ulong)(Number is < (-(double)18446744073709551615) or > 18446744073709551615 ? 0 : Floor(Abs(Number))),
			"string" => 0,
			"list" => NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedLongInt(),
			_ => 0
		};
	}

	public readonly double ToReal()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basic_type = InnerType.MainType?.Peek().Name.ToString() ?? "null";
		return basic_type switch
		{
			"null" => 0,
			"bool" => Bool == false ? 0 : 1,
			"byte" => Number,
			"short int" => Number,
			"unsigned short int" => Number,
			"char" => Number,
			"int" => Number,
			"unsigned int" => Number,
			"long int" => Object is not long li ? 0 : li,
			"DateTime" => Object is not DateTime dt ? 0 : dt.Ticks,
			"unsigned long int" => Object is not ulong uli ? 0 : uli,
			"real" => Number,
			"string" => 0,
			"list" => (double)(NextList == null || NextList.Length == 0 ? 0 : NextList[0].ToReal()),
			_ => 0
		};
	}

	public readonly String ToString(bool takeIntoQuotes = false, bool addCasting = false)
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return addCasting ? "default!" : "null";
			else if (basic_type == "bool")
				return (Bool == false) ? "false" : "true";
			else if (basic_type == "byte")
				return ((byte)Number).ToString(InvariantCulture);
			else if (basic_type == "short int")
				return ((short)Number).ToString(InvariantCulture);
			else if (basic_type == "unsigned short int")
				return ((ushort)Number).ToString(InvariantCulture);
			else if (basic_type == "char")
			{
				return takeIntoQuotes ? "'" + (char)Number switch
				{
					'\0' => @"\0",
					'\a' => @"\a",
					'\b' => @"\b",
					'\f' => @"\f",
					'\n' => @"\n",
					'\r' => @"\r",
					'\t' => @"\t",
					'\v' => @"\v",
					'\'' => @"\'",
					'\"' => @"\q",
					'\\' => @"\!",
					_ => (char)Number,
				} + "'" : "" + (char)Number;
			}
			else if (basic_type == "int")
				return ((int)Number).ToString(InvariantCulture);
			else if (basic_type == "unsigned int")
				return ((uint)Number).ToString(InvariantCulture);
			else if (basic_type == "long int")
				return Object == null ? "" : Object is long li ? li.ToString() : "0";
			else if (basic_type == "DateTime")
				return Object == null ? "" : Object is DateTime dt ? dt.ToString() : new DateTime(0).ToString();
			else if (basic_type == "unsigned long int")
				return Object == null ? "" : Object is ulong uli ? uli.ToString() : "0";
			else if (basic_type == "real")
			{
				return Number switch
				{
					(double)1 / 0 => addCasting ? "((double)1 / 0)" : "Infty",
					(double)-1 / 0 => addCasting ? "((double)-1 / 0)" : "-Infty",
					(double)0 / 0 => addCasting ? "((double)0 / 0)" : "Uncty",
					_ => Number.ToString(InvariantCulture)
				};
			}
			else if (basic_type == "typename")
				return Object == null ? "" : Object is UniversalType UnvType ? UnvType.ToString() : NullType.ToString();
			else if (basic_type == "string")
			{
				if (!takeIntoQuotes)
					return String;
				else if (addCasting)
					return ((String)"((").AddRange(nameof(String)).Add(')').AddRange(String.TakeIntoQuotes(true)).Add(')');
				else
					return String.TakeIntoQuotes();
			}
			else if (basic_type == "list")
			{
				return Object switch
				{
					(IList<bool> BoolIsNullList, IList<bool> BoolList) => ListToString(BoolIsNullList, BoolList),
					(IList<bool> ByteIsNullList, IList<byte> ByteList) => ListToString(ByteIsNullList, ByteList),
					(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => ListToString(ShortIntIsNullList, ShortIntList),
					(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) => ListToString(UnsignedShortIntIsNullList, UnsignedShortIntList),
					(IList<bool> CharIsNullList, IList<char> CharList) => ListToString(CharIsNullList, CharList),
					(IList<bool> IntIsNullList, IList<int> IntList) => ListToString(IntIsNullList, IntList),
					(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) => ListToString(UnsignedIntIsNullList, UnsignedIntList),
					(IList<bool> RealIsNullList, IList<double> RealList) => ListToString(RealIsNullList, RealList),
					(IList<bool> StringIsNullList, IList<string> StringList) => ListToString(StringIsNullList, StringList),
					_ => ListToString()
				};
			}
			else if (basic_type == "tuple")
				return ListToString();
		}
		else if (InnerType.MainType.Length != 0 && UserDefinedTypesList.TryGetValue(SplitType(InnerType.MainType), out var type2) && type2.Decomposition != null && type2.Decomposition.Length != 0)
			return ListToString(InnerType.MainType.Peek().Name.ToString());
		return takeIntoQuotes ? "Unknown Object" : "";
	}

	private readonly string ListToString(string type_name = "")
	{
		var list = ToList();
		if (list.Length == 0)
			return (type_name == "" ? "ListWithSingle" : "new " + type_name) + "(null)";
		else if (list.Length == 1)
			return (type_name == "" ? "ListWithSingle" : "new " + type_name) + "(" + list[0].ToString(true) + ")";
		String output = new(list.Length * 4 + 2) { '(' };
		if (type_name != "")
			output.Insert(0, "new " + type_name);
		output.AddRange(list[0].ToString(true));
		for (var i = 1; i <= list.Length - 1; i++)
		{
			output.AddRange(", ");
			output.AddRange(list[i].ToString(true));
		}
		output.Add(')');
		return new([.. output]);
	}

	private static string ListToString<T>(IList<bool> IsNullList, IList<T> MainList)
	{
		if (MainList.Length == 0)
			return "ListWithSingle(null)";
		else if (MainList.Length == 1)
			return "ListWithSingle(" + (IsNullList[0] ? new() : TryConstruct(MainList[0])).ToString(true) + ")";
		String output = new(MainList.Length * 4 + 2) { '(' };
		output.AddRange((IsNullList[0] ? new() : TryConstruct(MainList[0])).ToString(true));
		for (var i = 1; i <= MainList.Length - 1; i++)
		{
			output.AddRange(", ");
			output.AddRange((IsNullList[i] ? new() : TryConstruct(MainList[i])).ToString(true));
		}
		output.Add(')');
		return new([.. output]);
	}

	public static Universal PerformOperation<T>(Universal x, Universal y, Func<Universal, T> Input, Func<T, T, Universal> Output, String leftType, String rightType, String inputType) => ValidateFixing(Output(Input(x), Input(y)), GetPrimitiveType(inputType), x.Fixed && leftType == inputType || y.Fixed && rightType == inputType);

	public static Universal PerformOperation<T>(T x, T y, Func<T, T, Universal> Process, String leftType, String rightType, String inputType) => ValidateFixing(Process(x, y), GetPrimitiveType(inputType), leftType == inputType || rightType == inputType);

	public readonly List<Universal> ToList()
	{
		if (TypeIsPrimitive(InnerType.MainType) && InnerType.MainType.Peek().Name != "list" && InnerType.MainType.Peek().Name != "tuple")
			return [this];
		else
		{
			return Object switch
			{
				(IList<bool> BoolIsNullList, IList<bool> BoolList) => ToList2(BoolIsNullList, BoolList),
				(IList<bool> ByteIsNullList, IList<byte> ByteList) => ToList2(ByteIsNullList, ByteList),
				(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => ToList2(ShortIntIsNullList, ShortIntList),
				(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) => ToList2(UnsignedShortIntIsNullList, UnsignedShortIntList),
				(IList<bool> CharIsNullList, IList<char> CharList) => ToList2(CharIsNullList, CharList),
				(IList<bool> IntIsNullList, IList<int> IntList) => ToList2(IntIsNullList, IntList),
				(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) => ToList2(UnsignedIntIsNullList, UnsignedIntList),
				(IList<bool> LongIntIsNullList, IList<long> LongIntList) => ToList2(LongIntIsNullList, LongIntList),
				(IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList) => ToList2(UnsignedLongIntIsNullList, UnsignedLongIntList),
				(IList<bool> RealIsNullList, IList<double> RealList) => ToList2(RealIsNullList, RealList),
				_ => Object is (IList<bool> StringIsNullList, IList<string> StringList) ? ToList2(StringIsNullList, StringList) : NextList ?? []
			};
		}
	}

	private static List<Universal> ToList2<T>(IList<bool> IsNullList, IList<T> MainList)
	{
		List<Universal> output = new(MainList.Length);
		for (var i = 0; i < MainList.Length; i++)
			output.Add(IsNullList[i] ? new() : TryConstruct(MainList[i]));
		return output;
	}

	public Universal ToType(UniversalType type, bool fix = false)
	{
		try
		{
			Universal a;
			if (TypeIsPrimitive(type.MainType))
				a = ToPrimitiveType(type, fix);
			else if (TypesAreEqual(type, InnerType))
				a = this;
			else if (type.MainType.Length != 0 && UserDefinedTypesList.TryGetValue(SplitType(type.MainType), out var type_descr) && type_descr.Decomposition != null && type_descr.Decomposition.Length != 0)
				a = ToTupleType(type_descr.Decomposition);
			else
				a = new();
			if (!TypeEqualsToPrimitive(type, "universal"))
				a.InnerType = type;
			if (fix)
				a.Fixed = true;
			return a;
		}
		catch (StackOverflowException)
		{
			return new();
		}
	}

	private Universal ToPrimitiveType(UniversalType type, bool fix)
	{
		var basic_type = type.MainType.Peek().Name;
		if (basic_type == "null")
			return new();
		else if (basic_type == "universal")
			return this;
		else if (basic_type == "bool")
			return ToBool();
		else if (basic_type == "byte")
			return ToByte();
		else if (basic_type == "short int")
			return ToShortInt();
		else if (basic_type == "unsigned short int")
			return ToUnsignedShortInt();
		else if (basic_type == "char")
			return ToChar();
		else if (basic_type == "int")
			return ToInt();
		else if (basic_type == "unsigned int")
			return ToUnsignedInt();
		else if (basic_type == "long int")
			return new(ToLongInt(), LongIntType);
		else if (basic_type == "DateTime")
			return new(ToDateTime(), GetPrimitiveType("DateTime"));
		else if (basic_type == "unsigned long int")
			return new(ToUnsignedLongInt(), UnsignedLongIntType);
		else if (basic_type == "real")
			return ToReal();
		else if (basic_type == "typename")
			return TypeIsPrimitive(InnerType.MainType) && InnerType.MainType.Peek().Name == "typename" ? this : new();
		else if (basic_type == "string")
			return ToString();
		else if (basic_type == "list")
		{
			(var LeftDepth, var LeftLeafType) = GetTypeDepthAndLeafType(type);
			(var RightDepth, var RightLeafType) = GetTypeDepthAndLeafType(InnerType);
			return ToFullListType(type, LeftDepth, LeftLeafType, RightDepth, RightLeafType, fix);
		}
		else return basic_type == "tuple" ? ToTupleType(type.ExtraTypes) : new();
	}

	private Universal ToFullListType(UniversalType type, int LeftDepth, UniversalType LeftLeafType, int RightDepth, UniversalType RightLeafType, bool fix = false)
	{
		if (LeftDepth == 0)
			return ToType(LeftLeafType, fix);
		else if (LeftDepth > RightDepth)
		{
			var types_list = new UniversalType[LeftDepth - RightDepth + 1];
			types_list[0] = type;
			for (var i = 0; i < LeftDepth - RightDepth; i++)
				types_list[i + 1] = GetSubtype(types_list[i]);
			var element = (RightDepth == 0) ? ToType(types_list[LeftDepth - RightDepth], true) : ToFullListType(types_list[LeftDepth - RightDepth], RightDepth, LeftLeafType, RightDepth, RightLeafType, true);
			for (var i = LeftDepth - RightDepth - 1; i >= 0; i--)
				element = ValidateFixing(new List<Universal> { element }, types_list[i], i > 0 || fix);
			return element;
		}
		else if (LeftDepth == RightDepth || TypeEqualsToPrimitive(LeftLeafType, "string"))
		{
			var old_list = ToList();
			List<Universal> new_list = new(old_list.Length);
			for (var i = 0; i < old_list.Length; i++)
				new_list.Add(old_list[i].ToFullListType(GetSubtype(type), LeftDepth - 1, LeftLeafType, RightDepth - 1, RightLeafType, true));
			return ValidateFixing(new_list, type, fix);
		}
		else
		{
			var element = this;
			for (var i = 0; i < RightDepth - LeftDepth; i++)
				element = element.GetElement(1);
			return element.ToFullListType(type, LeftDepth, LeftLeafType, LeftDepth, RightLeafType, fix);
		}
	}

	private readonly Universal ToTupleType(GeneralExtraTypes type_parts)
	{
		var count = 0;
		NList<int> numbers = [];
		for (var i = 0; i < type_parts.Length; i++)
		{
			if (i >= 1 && type_parts[i].MainType.IsValue && int.TryParse(type_parts[i].MainType.Value.ToString(), out var number) && type_parts[i].ExtraTypes.Length == 0)
			{
				count += number - 1;
				numbers.Add(number - 1);
			}
			else
				count++;
		}
		numbers.Add(1);
		var old_list = ToList();
		List<Universal> new_list = [.. new Universal[count]];
		int tpos = 0, tpos2, npos = 0;
		for (var i = 0; i < count && i < old_list.Length; i++)
		{
			if (tpos >= 1 && type_parts[tpos].MainType.IsValue && int.TryParse(type_parts[tpos].MainType.Value.ToString(), out _) && type_parts[tpos].ExtraTypes.Length == 0)
			{
				tpos2 = tpos - 1;
				numbers[npos]--;
			}
			else
				tpos2 = tpos;
			new_list[i] = old_list[i].ToType((type_parts[tpos2].MainType.Type, type_parts[tpos2].ExtraTypes), true);
			if (tpos2 == tpos || numbers[npos] == 0)
				tpos++;
			if (numbers[npos] == 0)
				npos++;
		}
		for (var i = old_list.Length; i < count; i++)
			new_list[i] = new();
		return new_list;
	}

	public static String GetQuotientType(String leftType, Universal right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() >= (ulong)1 << 56)
				return "byte";
			else if (right.ToUnsignedLongInt() >= (ulong)1 << 48)
				return "unsigned short int";
			else if (right.ToUnsignedLongInt() >= 4294967296)
				return "unsigned int";
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
				return "long long";
			else
				return "unsigned long int";
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() >= (long)1 << 48)
				return "short int";
			else if (right.ToLongInt() >= 4294967296)
				return "int";
			else if (rightType == "unsigned long int")
				return "long long";
			else
				return "long int";
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
				return "long int";
			else
				return "long char";
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() >= 16777216)
				return "byte";
			else if (right.ToUnsignedInt() >= 65536)
				return "unsigned short int";
			else if (rightType.ToString() is "short int" or "int")
				return "long int";
			else
				return "unsigned int";
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
				return "long int";
			else if (right.ToInt() >= 65536)
				return "short int";
			else
				return "int";
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
				return "int";
			else
				return "char";
		}
		else if (leftType == "unsigned short int")
			return ValidateUnsignedShortIntType(right.ToUnsignedShortInt() >= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	public static String GetRemainderType(String leftType, Universal right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() <= 256)
				return "byte";
			else if (right.ToUnsignedLongInt() <= 65536)
				return "unsigned short int";
			else if (right.ToUnsignedLongInt() <= 4294967296)
				return "unsigned int";
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
				return "long long";
			else
				return "unsigned long int";
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() <= 32768)
				return "short int";
			else if (right.ToLongInt() <= 2147483648)
				return "int";
			else if (rightType == "unsigned long int")
				return "long long";
			else
				return "long int";
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
				return "long int";
			else
				return "long char";
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() <= 256)
				return "byte";
			else if (right.ToUnsignedInt() <= 65536)
				return "unsigned short int";
			else if (rightType.ToString() is "short int" or "int")
				return "long int";
			else
				return "unsigned int";
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
				return "long int";
			else if (right.ToInt() <= 32768)
				return "short int";
			else
				return "int";
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
				return "int";
			else
				return "char";
		}
		else if (leftType == "unsigned short int")
			return ValidateUnsignedShortIntType(right.ToUnsignedShortInt() <= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	private static String? ValidateRealType(String leftType, String rightType)
	{
		if (leftType == "long real" || rightType == "long real")
			return "long real";
		else if (leftType == "long long" || rightType == "long long")
		{
			if (leftType == "real" || rightType == "real")
				return "long real";
			else
				return "long long";
		}
		else if (leftType == "unsigned long long" || rightType == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "real" }.Contains(leftType) || new List<String> { "short int", "int", "long int", "real" }.Contains(rightType))
				return "long real";
			else
				return "unsigned long long";
		}
		else if (leftType == "real" || rightType == "real")
			return "real";
		else if (rightType == "bool")
			return "byte";
		return null;
	}

	private static String ValidateUnsignedShortIntType(bool condition, String rightType)
	{
		if (condition)
			return "byte";
		else if (rightType == "short int")
			return "int";
		else
			return "unsigned short int";
	}

	private static String ValidatePostUSIType(String leftType, String rightType)
	{
		if (leftType == "short int")
		{
			if (rightType == "unsigned short int")
				return "int";
			else
				return "short int";
		}
		else if (leftType == "short char" || rightType == "short char")
			return "short char";
		else if (leftType.ToString() is "byte" or "bool")
			return "byte";
		else
			return "null";
	}

	public override readonly bool Equals(object? obj) => obj != null
&& obj is Universal m && ToBool() == m.ToBool() && ToReal() == m.ToReal() && ToString() == m.ToString();

	public override readonly int GetHashCode()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var s = InnerType.MainType.Peek().Name;
			if (s == "null")
				return 0;
			else if (s == "bool")
				return Bool.GetHashCode();
			else if (s.ToString() is "byte" or "short int" or "unsigned short int" or "int" or "unsigned int" or "real")
				return Number.GetHashCode();
			else if (s == "char")
				return ((char)Number).GetHashCode();
			else if (s == "long int" && Object is long li)
				return li.GetHashCode();
			else if (s == "DateTime" && Object is DateTime dt)
				return dt.GetHashCode();
			else if (s == "unsigned long int" && Object is ulong uli)
				return uli.GetHashCode();
			else if (s == "string")
				return String.GetHashCode();
			else if (s == "list")
				return (NextList == null || NextList.Length == 0) ? 0 : NextList.Progression(0, (x, y) => x ^ y.GetHashCode());
		}
		return 0;
	}

	public static implicit operator Universal(bool x) => new(x);

	public static implicit operator Universal(char x) => new(x);

	public static implicit operator Universal(double x) => new(x);

	public static implicit operator Universal(string x) => new((String)x);

	public static implicit operator Universal(String x) => new(x);

	public static implicit operator Universal(List<Universal> x) => new(x);

	public static Universal operator +(Universal x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basic_type = x.InnerType.MainType.Peek().Name;
			if (new List<String> { "byte", "short int", "unsigned short int", "int", "unsigned int", "real" }.Contains(basic_type))
			{
				if (basic_type == "real")
					return ValidateFixing(+x.ToReal(), RealType, x.Fixed);
				else if (basic_type == "unsigned long int")
					return ValidateFixing(new(+x.ToUnsignedLongInt(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
				else if (basic_type == "long int")
					return ValidateFixing(new(+x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
				else if (basic_type == "unsigned int")
					return ValidateFixing(+x.ToUnsignedInt(), UnsignedIntType, x.Fixed);
				else if (basic_type == "int")
					return ValidateFixing(+x.ToInt(), IntType, x.Fixed);
				else if (basic_type == "unsigned short int")
					return ValidateFixing(+x.ToUnsignedShortInt(), UnsignedShortIntType, x.Fixed);
				else if (basic_type == "short int")
					return ValidateFixing(+x.ToShortInt(), ShortIntType, x.Fixed);
				else if (basic_type == "byte")
					return ValidateFixing(+x.ToByte(), ByteType, x.Fixed);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	public static Universal operator -(Universal x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basic_type = x.InnerType.MainType.Peek().Name;
			if (new List<String> { "byte", "short int", "unsigned short int", "int", "unsigned int", "real" }.Contains(basic_type))
			{
				if (basic_type == "real")
					return ValidateFixing(-x.ToReal(), RealType, x.Fixed);
				else if (basic_type == "unsigned long int")
					return ValidateFixing(new(-x.ToLongInt(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
				else if (basic_type == "long int")
					return ValidateFixing(new(-x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
				else if (basic_type == "unsigned int")
					return ValidateFixing(-x.ToInt(), UnsignedIntType, x.Fixed);
				else if (basic_type == "int")
					return ValidateFixing(-x.ToInt(), IntType, x.Fixed);
				else if (basic_type == "unsigned short int")
					return ValidateFixing(-x.ToShortInt(), UnsignedShortIntType, x.Fixed);
				else if (basic_type == "short int")
					return ValidateFixing(-x.ToShortInt(), ShortIntType, x.Fixed);
				else if (basic_type == "byte")
					return ValidateFixing(-x.ToByte(), ByteType, x.Fixed);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	public static Universal operator !(Universal x) => ValidateFixing(!x.ToBool(), BoolType, x.Fixed && TypeIsPrimitive(x.InnerType.MainType) && x.InnerType.MainType.Peek().Name == "bool");

	public static Universal operator ~(Universal x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basic_type = x.InnerType.MainType.Peek().Name;
			if (new List<String> { "short int", "unsigned short int", "int", "unsigned int" }.Contains(basic_type))
			{
				if (basic_type == "unsigned long int")
					return ValidateFixing(new(~x.ToUnsignedLongInt(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
				else if (basic_type == "long int")
					return ValidateFixing(new(~x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
				else if (basic_type == "unsigned int")
					return ValidateFixing(~x.ToUnsignedInt(), UnsignedIntType, x.Fixed);
				else if (basic_type == "int")
					return ValidateFixing(~x.ToInt(), IntType, x.Fixed);
				else if (basic_type == "unsigned short int")
					return ValidateFixing(~x.ToUnsignedShortInt(), UnsignedShortIntType, x.Fixed);
				else if (basic_type == "short int")
					return ValidateFixing(~x.ToShortInt(), ShortIntType, x.Fixed);
				else if (basic_type == "byte")
					return ValidateFixing(~x.ToByte(), ByteType, x.Fixed);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	public static Universal operator +(Universal left, Universal right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType) && TypeIsPrimitive(right.InnerType.MainType))
		{
			var left_type = left.InnerType.MainType.Peek().Name;
			var right_type = right.InnerType.MainType.Peek().Name;
			if (left_type == "null")
				left_type = right_type;
			else if (right_type == "null")
				right_type = left_type;
			if (ConvertibleTypesList.Contains(left_type) && ConvertibleTypesList.Contains(right_type))
			{
				string t;
				if (left_type == (t = "string") || right_type == t)
					return PerformOperation(left, right, x => x.ToString(), (x, y) => x.Concat(y), left_type, right_type, t);
				else if (left_type == (t = "real") || right_type == t)
					return PerformOperation(left, right, x => x.ToReal(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "unsigned long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedLongInt(), (x, y) => new(x + y, UnsignedLongIntType), left_type, right_type, t);
				else if (left_type == (t = "long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToLongInt(), (x, y) => new(x + y, LongIntType), left_type, right_type, t);
				else if (left_type == (t = "unsigned int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "int") || right_type == t)
					return PerformOperation(left, right, x => x.ToInt(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "char") || right_type == t)
					return PerformOperation(left, right, x => x.ToChar(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "unsigned short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x + y, left_type, right_type, t);
				else if (left_type == (t = "byte") || left_type == "bool" || right_type == t || right_type == "bool")
					return PerformOperation(left, right, x => x.ToByte(), (x, y) => x + y, left_type, right_type, t);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	public static Universal operator -(Universal left, Universal right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType) && TypeIsPrimitive(right.InnerType.MainType))
		{
			var left_type = left.InnerType.MainType.Peek().Name;
			var right_type = right.InnerType.MainType.Peek().Name;
			if (left_type == "null")
				left_type = right_type;
			else if (right_type == "null")
				right_type = left_type;
			if (ConvertibleTypesList.Contains(left_type) && ConvertibleTypesList.Contains(right_type))
			{
				string t;
				if (left_type == "string" || right_type == "string")
					return StringSubtract(left, right, left_type, right_type);
				else if (left_type == (t = "real") || right_type == t)
					return PerformOperation(left, right, x => x.ToReal(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "unsigned long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedLongInt(), (x, y) => new(x - y, UnsignedLongIntType), left_type, right_type, t);
				else if (left_type == (t = "long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToLongInt(), (x, y) => new(x - y, LongIntType), left_type, right_type, t);
				else if (left_type == (t = "unsigned int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "int") || right_type == t)
					return PerformOperation(left, right, x => x.ToInt(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "char") || right_type == t)
					return PerformOperation(left, right, x => x.ToChar(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "unsigned short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x - y, left_type, right_type, t);
				else if (left_type == (t = "byte") || left_type == "bool" || right_type == t || right_type == "bool")
					return PerformOperation(left, right, x => x.ToByte(), (x, y) => x - y, left_type, right_type, t);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	private static Universal StringSubtract(Universal left, Universal right, String left_type, String right_type)
	{
		if (byte.TryParse(left.ToString().ToString(), out var left_byte) && byte.TryParse(right.ToString().ToString(), out var right_byte))
			return PerformOperation(left_byte, right_byte, (x, y) => x - y, left_type, right_type, "byte");
		else if (short.TryParse(left.ToString().ToString(), out var left_short_int) && short.TryParse(right.ToString().ToString(), out var right_short_int))
			return PerformOperation(left_short_int, right_short_int, (x, y) => x - y, left_type, right_type, "short int");
		else if (ushort.TryParse(left.ToString().ToString(), out var left_unsigned_short_int) && ushort.TryParse(right.ToString().ToString(), out var right_unsigned_short_int))
			return PerformOperation(left_unsigned_short_int, right_unsigned_short_int, (x, y) => x - y, left_type, right_type, "unsigned short int");
		else if (int.TryParse(left.ToString().ToString(), out var left_int) && int.TryParse(right.ToString().ToString(), out var right_int))
			return PerformOperation(left_int, right_int, (x, y) => x - y, left_type, right_type, "int");
		else if (uint.TryParse(left.ToString().ToString(), out var left_unsigned_int) && uint.TryParse(right.ToString().ToString(), out var right_unsigned_int))
			return PerformOperation(left_unsigned_int, right_unsigned_int, (x, y) => x - y, left_type, right_type, "unsigned int");
		else if (long.TryParse(left.ToString().ToString(), out var left_long_int) && long.TryParse(right.ToString().ToString(), out var right_long_int))
			return PerformOperation(left_long_int, right_long_int, (x, y) => new(x - y, LongIntType), left_type, right_type, "long int");
		else if (ulong.TryParse(left.ToString().ToString(), out var left_unsigned_long_int) && ulong.TryParse(right.ToString().ToString(), out var right_unsigned_long_int))
			return PerformOperation(left_unsigned_long_int, right_unsigned_long_int, (x, y) => new(x - y, UnsignedLongIntType), left_type, right_type, "unsigned long int");
		else if (double.TryParse(left.ToString().ToString(), out var left_real) && double.TryParse(right.ToString().ToString(), out var right_real))
			return PerformOperation(left_real, right_real, (x, y) => x - y, left_type, right_type, "real");
		else
			return new();
	}

	public static Universal operator *(Universal left, Universal right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType) && TypeIsPrimitive(right.InnerType.MainType))
		{
			var left_type = left.InnerType.MainType.Peek().Name;
			var right_type = right.InnerType.MainType.Peek().Name;
			if (left_type == "null")
				left_type = right_type;
			else if (right_type == "null")
				right_type = left_type;
			if (ConvertibleTypesList.Contains(left_type) && ConvertibleTypesList.Contains(right_type))
			{
				string t;
				if (left_type == "string")
					return StringMultiply(left, right, right_type);
				else if (right_type == "string")
					return new String(RedStarLinq.Fill(right.ToString(), Max((int)left.ToUnsignedInt(), 0)).JoinIntoSingle());
				else if (left_type == (t = "real") || right_type == t)
					return PerformOperation(left, right, x => x.ToReal(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "unsigned long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedLongInt(), (x, y) => new(x * y, UnsignedLongIntType), left_type, right_type, t);
				else if (left_type == (t = "long int") || right_type == t)
					return PerformOperation(left, right, x => x.ToLongInt(), (x, y) => new(x * y, LongIntType), left_type, right_type, t);
				else if (left_type == (t = "unsigned int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "int") || right_type == t)
					return PerformOperation(left, right, x => x.ToInt(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "char") || right_type == t)
					return PerformOperation(left, right, x => x.ToChar(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "unsigned short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "short int") || right_type == t)
					return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x * y, left_type, right_type, t);
				else if (left_type == (t = "byte") || left_type == "bool" || right_type == t || right_type == "bool")
					return PerformOperation(left, right, x => x.ToByte(), (x, y) => x * y, left_type, right_type, t);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	private static Universal StringMultiply(Universal left, Universal right, String right_type)
	{
		if (right_type == "string" && uint.TryParse(right.ToString().ToString(), out _) == false)
		{
			if (uint.TryParse(left.ToString().ToString(), out _) == false)
				return new();
			else
				return (Universal)new String(RedStarLinq.Fill(right.ToString(), Max((int)left.ToUnsignedInt(), 0)).JoinIntoSingle());
		}
		else
			return (Universal)new String(RedStarLinq.Fill(left.ToString(), Max((int)right.ToUnsignedInt(), 0)).JoinIntoSingle());
	}

	public static Universal operator /(Universal left, Universal right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType) && TypeIsPrimitive(right.InnerType.MainType))
		{
			var left_type = left.InnerType.MainType.Peek().Name;
			var right_type = right.InnerType.MainType.Peek().Name;
			if (left_type == "null")
				left_type = right_type;
			else if (right_type == "null")
				right_type = left_type;
			if (ConvertibleTypesList.Contains(left_type) && ConvertibleTypesList.Contains(right_type))
			{
				var t = GetQuotientType(left_type, right, right_type);
				if (left_type == "string" || right_type == "string")
					return StringDivide(left, right, left_type, right_type);
				else if (left_type == "real" || right_type == "real")
					return PerformOperation(left, right, x => x.ToReal(), (x, y) => x / y, left_type, right_type, t);
				else if (right == 0)
					return new();
				else if (left_type == "unsigned long int" || right_type == "unsigned long int")
					return PerformOperation(left, right, x => x.ToUnsignedLongInt(), (x, y) => new(x / y, UnsignedLongIntType), left_type, right_type, t);
				else if (left_type == "long int" || right_type == "long int")
					return PerformOperation(left, right, x => x.ToLongInt(), (x, y) => new(x / y, LongIntType), left_type, right_type, t);
				else if (left_type == "unsigned int" || right_type == "unsigned int")
					return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x / y, left_type, right_type, t);
				else if (left_type == "int" || right_type == "int")
					return PerformOperation(left, right, x => x.ToInt(), (x, y) => x / y, left_type, right_type, t);
				else if (left_type == "char" || right_type == "char")
					return PerformOperation(left, right, x => x.ToChar(), (x, y) => x / y, left_type, right_type, t);
				else if (left_type == "unsigned short int" || right_type == "unsigned short int")
					return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x / y, left_type, right_type, t);
				else if (left_type == "short int" || right_type == "short int")
					return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x / y, left_type, right_type, t);
				else if (left_type == (t = "byte") || left_type == "bool" || right_type == t || right_type == "bool")
					return PerformOperation(left, right, x => x.ToByte(), (x, y) => x / y, left_type, right_type, t);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	private static Universal StringDivide(Universal left, Universal right, String left_type, String right_type)
	{
		var t = GetQuotientType(left_type, right, right_type);
		if (short.TryParse(left.ToString().ToString(), out var left_short_int) && short.TryParse(right.ToString().ToString(), out var right_short_int))
			return PerformOperation(left_short_int, right_short_int, (x, y) => x / y, left_type, right_type, t);
		else if (ushort.TryParse(left.ToString().ToString(), out var left_unsigned_short_int) && ushort.TryParse(right.ToString().ToString(), out var right_unsigned_short_int))
			return PerformOperation(left_unsigned_short_int, right_unsigned_short_int, (x, y) => x / y, left_type, right_type, t);
		else if (int.TryParse(left.ToString().ToString(), out var left_int) && int.TryParse(right.ToString().ToString(), out var right_int))
			return PerformOperation(left_int, right_int, (x, y) => x / y, left_type, right_type, t);
		else if (uint.TryParse(left.ToString().ToString(), out var left_unsigned_int) && uint.TryParse(right.ToString().ToString(), out var right_unsigned_int))
			return PerformOperation(left_unsigned_int, right_unsigned_int, (x, y) => x / y, left_type, right_type, t);
		else if (long.TryParse(left.ToString().ToString(), out var left_long_int) && long.TryParse(right.ToString().ToString(), out var right_long_int))
			return PerformOperation(left_long_int, right_long_int, (x, y) => new(x / y, LongIntType), left_type, right_type, t);
		else if (ulong.TryParse(left.ToString().ToString(), out var left_unsigned_long_int) && ulong.TryParse(right.ToString().ToString(), out var right_unsigned_long_int))
			return PerformOperation(left_unsigned_long_int, right_unsigned_long_int, (x, y) => new(x / y, UnsignedLongIntType), left_type, right_type, t);
		else if (double.TryParse(left.ToString().ToString(), out var left_real) && double.TryParse(right.ToString().ToString(), out var right_real))
			return PerformOperation(left_real, right_real, (x, y) => x / y, left_type, right_type, t);
		else
			return new();
	}

	public static Universal operator %(Universal left, Universal right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType) && TypeIsPrimitive(right.InnerType.MainType))
		{
			var left_type = left.InnerType.MainType.Peek().Name;
			var right_type = right.InnerType.MainType.Peek().Name;
			if (left_type == "null")
				left_type = right_type;
			else if (right_type == "null")
				right_type = left_type;
			if (ConvertibleTypesList.Contains(left_type) && ConvertibleTypesList.Contains(right_type))
			{
				var t = GetRemainderType(left_type, right, right_type);
				if (right.ToReal() == 0)
					return new();
				else if (left_type == "string" || right_type == "string")
					return StringMod(left, right, left_type, right_type);
				else if (left_type == "real" || right_type == "real")
					return PerformOperation(left, right, x => x.ToReal(), (x, y) => x - Floor(x / y) * y, left_type, right_type, t);
				else if (left_type == "unsigned long int" || right_type == "unsigned long int")
					return PerformOperation(left, right, x => x.ToUnsignedLongInt(), (x, y) => new(x - x / y * y, UnsignedLongIntType), left_type, right_type, t);
				else if (left_type == "long int" || right_type == "long int")
					return PerformOperation(left, right, x => x.ToLongInt(), (x, y) => new(x - x / y * y, LongIntType), left_type, right_type, t);
				else if (left_type == "unsigned int" || right_type == "unsigned int")
					return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x - x / y * y, left_type, right_type, t);
				else if (left_type == "int" || right_type == "int")
					return PerformOperation(left, right, x => x.ToInt(), (x, y) => x - x / y * y, left_type, right_type, t);
				else if (left_type == "char" || right_type == "char")
					return PerformOperation(left, right, x => x.ToChar(), (x, y) => x - x / y * y, left_type, right_type, t);
				else if (left_type == "unsigned short int" || right_type == "unsigned short int")
					return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x - x / y * y, left_type, right_type, t);
				else if (left_type == "short int" || right_type == "short int")
					return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x - x / y * y, left_type, right_type, t);
				else if (left_type == "byte" || left_type == "bool" || right_type == "byte" || right_type == "bool")
					return PerformOperation(left, right, x => x.ToByte(), (x, y) => x - x / y * y, left_type, right_type, t);
				else
					return new();
			}
			else
				return new();
		}
		else
			return new();
	}

	private static Universal StringMod(Universal left, Universal right, String left_type, String right_type)
	{
		var t = GetRemainderType(left_type, right, right_type);
		if (short.TryParse(left.ToString().ToString(), out var left_short_int) && short.TryParse(right.ToString().ToString(), out var right_short_int))
			return PerformOperation(left_short_int, right_short_int, (x, y) => x - x / y * y, left_type, right_type, t);
		else if (ushort.TryParse(left.ToString().ToString(), out var left_unsigned_short_int) && ushort.TryParse(right.ToString().ToString(), out var right_unsigned_short_int))
			return PerformOperation(left_unsigned_short_int, right_unsigned_short_int, (x, y) => x - x / y * y, left_type, right_type, t);
		else if (int.TryParse(left.ToString().ToString(), out var left_int) && int.TryParse(right.ToString().ToString(), out var right_int))
			return PerformOperation(left_int, right_int, (x, y) => x - x / y * y, left_type, right_type, t);
		else if (uint.TryParse(left.ToString().ToString(), out var left_unsigned_int) && uint.TryParse(right.ToString().ToString(), out var right_unsigned_int))
			return PerformOperation(left_unsigned_int, right_unsigned_int, (x, y) => x - x / y * y, left_type, right_type, t);
		else if (long.TryParse(left.ToString().ToString(), out var left_long_int) && long.TryParse(right.ToString().ToString(), out var right_long_int))
			return PerformOperation(left_long_int, right_long_int, (x, y) => new(x - x / y * y, LongIntType), left_type, right_type, t);
		else if (ulong.TryParse(left.ToString().ToString(), out var left_unsigned_long_int) && ulong.TryParse(right.ToString().ToString(), out var right_unsigned_long_int))
			return PerformOperation(left_unsigned_long_int, right_unsigned_long_int, (x, y) => new(x - x / y * y, UnsignedLongIntType), left_type, right_type, t);
		else if (double.TryParse(left.ToString().ToString(), out var left_real) && double.TryParse(right.ToString().ToString(), out var right_real))
			return PerformOperation(left_real, right_real, (x, y) => x - Floor(x / y) * y, left_type, right_type, t);
		else
			return new();
	}

	public static Universal operator &(Universal left, Universal right) => left.ToInt() & right.ToInt();

	public static Universal operator |(Universal left, Universal right) => left.ToInt() | right.ToInt();

	public static Universal operator ^(Universal left, Universal right) => left.ToInt() ^ right.ToInt();

	public static Universal operator >>(Universal left, int right) => left.ToInt() >> right;

	public static Universal operator <<(Universal left, int right) => left.ToInt() << right;

	public static bool operator ==(Universal left, Universal right) =>
		left.ToBool() == right.ToBool() && left.ToReal() == right.ToReal() && left.ToString() == right.ToString();

	public static bool operator !=(Universal left, Universal right) => !(left == right);
}
