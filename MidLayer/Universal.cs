using System.Diagnostics;
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
		InnerType = nextList.Length == 0 ? GetListType(NullType) : GetListType(nextList.Skip(1).Progression(nextList[0].InnerType, (x, y) => GetResultType(x, y.InnerType)));
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
		InnerType = BoolListType;
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
		else if ((uint)(s[0] - '0') > 9 && s[^1] is not ('\"' or '\'' or '\\'))
			throw new FormatException();
		else if (s[^1] == 'i')
			return int.Parse(s[..^1], EnUsCulture);
		else if (s[^1] == 'L')
		{
			s2 = s[..^1];
			if (int.TryParse(s2, out var i))
				return (Universal)i;
			else
				return long.TryParse(s2, out var l) ? new(l, LongIntType) : new(ulong.Parse(s2), UnsignedLongIntType);
		}
		else if (s[^1] == 'r')
		{
			if ((s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
			{
				double n;
				try
				{
					n = int.Parse(s2, EnUsCulture);
				}
				catch
				{
					n = double.Parse(s2, EnUsCulture);
				}
				return ValidateFixing(n, RealType, true);
			}
			else
				throw new FormatException();
		}
		else if (s[0] == '\"' && s[^1] == '\"')
			return ((String)s).RemoveQuotes();
		else if (s[0] == '\'' && s[^1] == '\'')
		{
			return s.Length <= 2 ? (Universal)'\0' : (Universal)((String)s).RemoveQuotes()[0];
		}
		else if (s.Length >= 3 && s[0] == '@' && s[1] == '\"' && s[^1] == '\"')
			return ((String)s)[2..^1].Replace("\"\"", "\"");
		else if (Constructions.IsRawString(s, out var output))
			return output;
		else
		{
			if (int.TryParse(s, NumberStyles.Integer, EnUsCulture, out var i))
				return i;
			else if (long.TryParse(s, NumberStyles.Integer, EnUsCulture, out var l))
				return (Universal)l;
			else
				return ulong.TryParse(s, NumberStyles.Integer, EnUsCulture, out var ul) ? (Universal)ul : (Universal)double.Parse(s, EnUsCulture);
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
		(IList<bool> BoolIsNullList, IList<bool> BoolList) => (BoolIsNullList, BoolList),
		(IList<bool> ByteIsNullList, IList<byte> ByteList) => (ByteIsNullList, ByteList),
		(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => (ShortIntIsNullList, ShortIntList),
		(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) => (UnsignedShortIntIsNullList, UnsignedShortIntList),
		(IList<bool> CharIsNullList, IList<char> CharList) => (CharIsNullList, CharList),
		(IList<bool> IntIsNullList, IList<int> IntList) => (IntIsNullList, IntList),
		(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) => (UnsignedIntIsNullList, UnsignedIntList),
		(IList<bool> LongIntIsNullList, IList<long> LongIntList) => (LongIntIsNullList, LongIntList),
		(IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList) => (UnsignedLongIntIsNullList, UnsignedLongIntList),
		(IList<bool> RealIsNullList, IList<double> RealList) => (RealIsNullList, RealList),
		(IList<bool> StringIsNullList, IList<String> StringList) => (StringIsNullList, StringList),
		IList<bool> BoolList2 => (new BitList(BoolList2.Length, false), BoolList2),
		IList<byte> ByteList2 => (new BitList(ByteList2.Length, false), ByteList2),
		IList<short> ShortIntList2 => (new BitList(ShortIntList2.Length, false), ShortIntList2),
		IList<ushort> UnsignedShortIntList2 => (new BitList(UnsignedShortIntList2.Length, false), UnsignedShortIntList2),
		IList<char> CharList2 => (new BitList(CharList2.Length, false), CharList2),
		IList<int> IntList2 => (new BitList(IntList2.Length, false), IntList2),
		IList<uint> UnsignedIntList2 => (new BitList(UnsignedIntList2.Length, false), UnsignedIntList2),
		IList<long> LongIntList2 => (new BitList(LongIntList2.Length, false), LongIntList2),
		IList<ulong> UnsignedLongIntList2 => (new BitList(UnsignedLongIntList2.Length, false), UnsignedLongIntList2),
		IList<double> RealList2 => (Universal)(new BitList(RealList2.Length, false), RealList2),
		_ => element is IList<String> StringList2 ? (Universal)(new BitList(StringList2.Length, false), StringList2) : new()
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
		var result_type = GetResultType(x.InnerType, y.InnerType);
		return x.ToType(result_type, x.Fixed) == y.ToType(result_type, y.Fixed);
	}

	public static Universal Neq(Universal x, Universal y)
	{
		var result_type = GetResultType(x.InnerType, y.InnerType);
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
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return false;
			else if (basic_type == "bool")
				return Bool;
			else if (basic_type == "byte")
				return !(Number < 1);
			else if (basic_type == "short int")
				return !(Number < 1);
			else if (basic_type == "unsigned short int")
				return !(Number < 1);
			else if (basic_type == "char")
				return !(Number < 1);
			else if (basic_type == "int")
				return !(Number < 1);
			else if (basic_type == "unsigned int")
				return !(Number < 1);
			else if (basic_type == "long int")
				return !(Object is not long li || li < 1);
			else if (basic_type == "DateTime")
				return !(Object is not DateTime dt || dt.Ticks < 1);
			else if (basic_type == "unsigned long int")
				return !(Object is not ulong uli || uli < 1);
			else if (basic_type == "real")
				return !(Number < 1);
			else if (basic_type == "string")
				return !(String == "");
			else if (basic_type == "list")
				return !(NextList == null || NextList.Length == 0) && NextList[0].ToBool();
		}
		return false;
	}

	public readonly byte ToByte()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (byte)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (byte)Number;
			else if (basic_type == "short int")
				return (byte)((Number is < (-255) or > 255) ? 0 : Abs(Number));
			else if (basic_type == "unsigned short int")
				return (byte)((Number is < (-255) or > 255) ? 0 : Number);
			else if (basic_type == "char")
				return (byte)((Number is < (-255) or > 255) ? 0 : Number);
			else if (basic_type == "int")
				return (byte)((Number is < (-255) or > 255) ? 0 : Abs(Number));
			else if (basic_type == "unsigned int")
				return (byte)((Number is < (-255) or > 255) ? 0 : Number);
			else if (basic_type == "long int")
				return (byte)((Object is not long li) ? 0 : (li is < (-255) or > 255) ? 0 : Abs(li));
			else if (basic_type == "DateTime")
				return (byte)((Object is not DateTime dt) ? 0 : (dt.Ticks > 255) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (byte)((Object is not ulong uli) ? 0 : (uli > 255) ? 0 : uli);
			else if (basic_type == "real")
				return (byte)((Number is < (-255) or > 255) ? 0 : Floor(Abs(Number)));
			else if (basic_type == "string")
				return (byte)((String == "") ? 0 : byte.TryParse(String.ToString(), out var a) ? a : 0);
			else if (basic_type == "list")
				return (byte)((NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToByte());
		}
		return 0;
	}

	public readonly short ToShortInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (short)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (short)Number;
			else if (basic_type == "short int")
				return (short)Number;
			else if (basic_type == "unsigned short int")
				return (short)Number;
			else if (basic_type == "char")
				return (short)Number;
			else if (basic_type == "int")
				return (short)((Number is < (-32768) or > 32767) ? 0 : Number);
			else if (basic_type == "unsigned int")
				return (short)((Number > 32767) ? 0 : Number);
			else if (basic_type == "long int")
				return (short)((Object is not long li) ? 0 : (li is < (-32768) or > 32767) ? 0 : li);
			else if (basic_type == "DateTime")
				return (short)((Object is not DateTime dt) ? 0 : (dt.Ticks > 32767) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (short)((Object is not ulong uli) ? 0 : (uli > 32767) ? 0 : uli);
			else if (basic_type == "real")
				return (short)((Number is < (-32768) or > 32767) ? 0 : Floor(Number));
			else if (basic_type == "string")
				return (short)((String == "") ? 0 : short.TryParse(String.ToString(), out var a) ? a : 0);
			else if (basic_type == "list")
				return (short)((NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToShortInt());
		}
		return 0;
	}

	public readonly ushort ToUnsignedShortInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (ushort)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (ushort)Number;
			else if (basic_type == "short int")
				return (ushort)Number;
			else if (basic_type == "unsigned short int")
				return (ushort)Number;
			else if (basic_type == "char")
				return (ushort)Number;
			else if (basic_type == "int")
				return (ushort)((Number is < (-65535) or > 65535) ? 0 : Abs(Number));
			else if (basic_type == "unsigned int")
				return (ushort)((Number is < (-65535) or > 65535) ? 0 : Number);
			else if (basic_type == "long int")
				return (ushort)((Object is not long li) ? 0 : (li is < (-65535) or > 65535) ? 0 : Abs(li));
			else if (basic_type == "DateTime")
				return (ushort)((Object is not DateTime dt) ? 0 : (dt.Ticks > 65535) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (ushort)((Object is not ulong uli) ? 0 : (uli > 65535) ? 0 : uli);
			else if (basic_type == "real")
				return (ushort)((Number is < (-65535) or > 65535) ? 0 : Floor(Abs(Number)));
			else if (basic_type == "string")
				return (ushort)((String == "") ? 0 : ushort.TryParse(String.ToString(), out var a) ? a : 0);
			else if (basic_type == "list")
				return (ushort)((NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToUnsignedShortInt());
		}
		return 0;
	}

	public readonly char ToChar()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return '\0';
			else if (basic_type == "bool")
				return (char)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (char)Number;
			else if (basic_type == "short int")
				return (char)Number;
			else if (basic_type == "unsigned short int")
				return (char)Number;
			else if (basic_type == "char")
				return (char)Number;
			else if (basic_type == "int")
				return (char)((Number is < (-65535) or > 65535) ? 0 : Abs(Number));
			else if (basic_type == "unsigned int")
				return (char)((Number is < (-65535) or > 65535) ? 0 : Number);
			else if (basic_type == "long int")
				return (char)((Object is not long li) ? 0 : (li is < (-65535) or > 65535) ? 0 : Abs(li));
			else if (basic_type == "DateTime")
				return (char)((Object is not DateTime dt) ? 0 : (dt.Ticks > 65535) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (char)((Object is not ulong uli) ? 0 : (uli > 65535) ? 0 : uli);
			else if (basic_type == "real")
				return (char)((Number is < (-65535) or > 65535) ? 0 : Floor(Abs(Number)));
			else if (basic_type == "string")
				return (char)((String == "") ? 0 : char.TryParse(String.ToString(), out var a) ? a : 0);
			else if (basic_type == "list")
				return (char)((NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToChar());
		}
		return '\0';
	}

	public readonly int ToInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (Bool == false) ? 0 : 1;
			else if (basic_type == "byte")
				return (int)Number;
			else if (basic_type == "short int")
				return (int)Number;
			else if (basic_type == "unsigned short int")
				return (int)Number;
			else if (basic_type == "char")
				return (int)Number;
			else if (basic_type == "int")
				return (int)Number;
			else if (basic_type == "unsigned int")
				return (int)Number;
			else if (basic_type == "long int")
				return (int)((Object is not long li) ? 0 : (li is < (-2147483648) or > 2147483647) ? 0 : li);
			else if (basic_type == "DateTime")
				return (int)((Object is not DateTime dt) ? 0 : (dt.Ticks > 2147483647) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (int)((Object is not ulong uli) ? 0 : (uli > 2147483647) ? 0 : uli);
			else if (basic_type == "real")
				return (int)((Number is < (-2147483648) or > 2147483647) ? 0 : Floor(Number));
			else if (basic_type == "string")
				return (String == "") ? 0 : int.TryParse(String.ToString(), out var a) ? a : 0;
			else if (basic_type == "list")
				return (NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToInt();
		}
		return 0;
	}

	public readonly uint ToUnsignedInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (uint)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (uint)Number;
			else if (basic_type == "short int")
				return (uint)Abs(Number);
			else if (basic_type == "unsigned short int")
				return (uint)Number;
			else if (basic_type == "char")
				return (uint)Number;
			else if (basic_type == "int")
				return (uint)Number;
			else if (basic_type == "unsigned int")
				return (uint)Number;
			else if (basic_type == "long int")
				return (uint)((Object is not long li) ? 0 : (li is < (-4294967295) or > 4294967295) ? 0 : Abs(li));
			else if (basic_type == "DateTime")
				return (uint)((Object is not DateTime dt) ? 0 : (dt.Ticks > 4294967295) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (uint)((Object is not ulong uli) ? 0 : (uli > 4294967295) ? 0 : uli);
			else if (basic_type == "real")
				return (uint)((Number is < (-4294967295) or > 4294967295) ? 0 : Floor(Abs(Number)));
			else if (basic_type == "string")
				return (String == "") ? 0 : uint.TryParse(String.ToString(), out var a) ? a : 0;
			else if (basic_type == "list")
				return (NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToUnsignedInt();
		}
		return 0;
	}

	public readonly long ToLongInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (Bool == false) ? 0 : 1;
			else if (basic_type == "byte")
				return (long)Number;
			else if (basic_type == "short int")
				return (long)Number;
			else if (basic_type == "unsigned short int")
				return (long)Number;
			else if (basic_type == "char")
				return (long)Number;
			else if (basic_type == "int")
				return (long)Number;
			else if (basic_type == "unsigned int")
				return (long)Number;
			else if (basic_type == "long int")
				return (Object is not long li) ? 0 : li;
			else if (basic_type == "DateTime")
				return (Object is not DateTime dt) ? 0 : dt.Ticks;
			else if (basic_type == "unsigned long int")
				return (long)((Object is not ulong uli) ? 0 : uli);
			else if (basic_type == "real")
				return (long)((Number is < (-(double)9223372036854775808) or > 9223372036854775807) ? 0 : Floor(Number));
			else if (basic_type == "string")
				return (String == "") ? 0 : long.TryParse(String.ToString(), out var a) ? a : 0;
			else if (basic_type == "list")
				return (NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToLongInt();
		}
		return 0;
	}

	public readonly DateTime ToDateTime() => TypeEqualsToPrimitive(InnerType, "DateTime") ? (Object is not DateTime dt) ? new(0) : dt : new(ToLongInt());

	public readonly ulong ToUnsignedLongInt()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (ulong)((Bool == false) ? 0 : 1);
			else if (basic_type == "byte")
				return (ulong)Number;
			else if (basic_type == "short int")
				return (ulong)Abs(Number);
			else if (basic_type == "unsigned short int")
				return (ulong)Number;
			else if (basic_type == "char")
				return (ulong)Number;
			else if (basic_type == "int")
				return (ulong)Number;
			else if (basic_type == "unsigned int")
				return (ulong)Number;
			else if (basic_type == "long int")
				return (ulong)((Object is not long li) ? 0 : Abs(li));
			else if (basic_type == "DateTime")
				return (ulong)((Object is not DateTime dt) ? 0 : dt.Ticks);
			else if (basic_type == "unsigned long int")
				return (Object is not ulong uli) ? 0 : uli;
			else if (basic_type == "real")
				return (ulong)((Number is < (-(double)18446744073709551615) or > 18446744073709551615) ? 0 : Floor(Abs(Number)));
			else if (basic_type == "string")
				return (String == "") ? 0 : ulong.TryParse(String.ToString(), out var a) ? a : 0;
			else if (basic_type == "list")
				return (NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToUnsignedLongInt();
		}
		return 0;
	}

	public readonly double ToReal()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basic_type = InnerType.MainType?.Peek().Name ?? "null";
			if (basic_type == "null")
				return 0;
			else if (basic_type == "bool")
				return (Bool == false) ? 0 : 1;
			else if (basic_type == "byte")
				return (double)Number;
			else if (basic_type == "short int")
				return (double)Number;
			else if (basic_type == "unsigned short int")
				return (double)Number;
			else if (basic_type == "char")
				return (double)Number;
			else if (basic_type == "int")
				return (double)Number;
			else if (basic_type == "unsigned int")
				return (double)Number;
			else if (basic_type == "long int")
				return (Object is not long li) ? 0 : li;
			else if (basic_type == "DateTime")
				return (Object is not DateTime dt) ? 0 : dt.Ticks;
			else if (basic_type == "unsigned long int")
				return (Object is not ulong uli) ? 0 : uli;
			else if (basic_type == "real")
				return (double)Number;
			else if (basic_type == "string")
				return (double)((String == "") ? 0 : double.TryParse(String.ToString(), out var a) ? a : 0);
			else if (basic_type == "list")
				return (double)((NextList == null || NextList.Length == 0) ? 0 : NextList[0].ToReal());
		}
		return 0;
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
				return ((byte)Number).ToString(EnUsCulture);
			else if (basic_type == "short int")
				return ((short)Number).ToString(EnUsCulture);
			else if (basic_type == "unsigned short int")
				return ((ushort)Number).ToString(EnUsCulture);
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
				return ((int)Number).ToString(EnUsCulture);
			else if (basic_type == "unsigned int")
				return ((uint)Number).ToString(EnUsCulture);
			else if (basic_type == "long int")
			{
				return Object == null ? "" : Object is long li ? li.ToString() : "0";
			}
			else if (basic_type == "DateTime")
			{
				return Object == null ? "" : Object is DateTime dt ? dt.ToString() : new DateTime(0).ToString();
			}
			else if (basic_type == "unsigned long int")
			{
				return Object == null ? "" : Object is ulong uli ? uli.ToString() : "0";
			}
			else if (basic_type == "real")
			{
				return Number switch
				{
					(double)1 / 0 => addCasting ? "((double)1 / 0)" : "Infty",
					(double)-1 / 0 => addCasting ? "((double)-1 / 0)" : "-Infty",
					(double)0 / 0 => addCasting ? "((double)0 / 0)" : "Uncty",
					_ => Number.ToString(EnUsCulture)
				};
			}
			else if (basic_type == "typename")
			{
				return Object == null ? "" : Object is UniversalType UnvType ? UnvType.ToString() : NullType.ToString();
			}
			else if (basic_type == "string")
			{
				if (String == "")
					return (String)(takeIntoQuotes ? "\"\"" : "");
				else if (!takeIntoQuotes)
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

	public readonly DelegateParameters? ToDelegate()
	{
		if (!new BlockStackEComparer().Equals(InnerType.MainType, FuncBlockStack))
			return null;
		else
			return Object is DelegateParameters delegateParameters ? delegateParameters : null;
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

	public readonly (IList<bool> IsNullList, IList<bool> MainList) ToBoolList()
	{
		if (Object is (IList<bool> IsNullList, IList<bool> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		BitList output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToBool());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<byte> MainList) ToByteList()
	{
		if (Object is (IList<bool> IsNullList, IList<byte> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<byte> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToByte());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<short> MainList) ToShortIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<short> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<short> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToShortInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<ushort> MainList) ToUnsignedShortIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<ushort> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<ushort> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToUnsignedShortInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<char> MainList) ToCharList()
	{
		if (Object is (IList<bool> IsNullList, IList<char> MainList))
			return (IsNullList, MainList);
		if (TypeEqualsToPrimitive(InnerType, "string"))
		{
			var string_ = ToString();
			return (new BitList(string_.Length), new String(string_.AsSpan()));
		}
		var list = ToList();
		BitList is_null = new(list.Length);
		String output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToChar());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<int> MainList) ToIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<int> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		NList<int> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<uint> MainList) ToUnsignedIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<uint> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<uint> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToUnsignedInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<long> MainList) ToLongIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<long> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<long> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToLongInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<ulong> MainList) ToUnsignedLongIntList()
	{
		if (Object is (IList<bool> IsNullList, IList<ulong> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<ulong> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToUnsignedLongInt());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<double> MainList) ToRealList()
	{
		if (Object is (IList<bool> IsNullList, IList<double> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<double> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToReal());
		}
		return (is_null, output);
	}

	public readonly (IList<bool> IsNullList, IList<String> MainList) ToStringList()
	{
		if (Object is (IList<bool> IsNullList, IList<String> MainList))
			return (IsNullList, MainList);
		var list = ToList();
		BitList is_null = new(list.Length);
		List<String> output = new(list.Length);
		for (var i = 0; i < list.Length; i++)
		{
			is_null.Add(false);
			output.Add(list[i].ToString());
		}
		return (is_null, output);
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
		{
			return TypeIsPrimitive(InnerType.MainType) && InnerType.MainType.Peek().Name == "typename" ? this : new();
		}
		else if (basic_type == "string")
			return ToString();
		else if (basic_type == "list")
		{
			if (ToListType(type, out var a))
				return a;
			(var LeftDepth, var LeftLeafType) = GetTypeDepthAndLeafType(type);
			(var RightDepth, var RightLeafType) = GetTypeDepthAndLeafType(InnerType);
			return ToFullListType(type, LeftDepth, LeftLeafType, RightDepth, RightLeafType, fix);
		}
		else return basic_type == "tuple" ? ToTupleType(type.ExtraTypes) : new();
	}

	private readonly bool ToListType(UniversalType type, out Universal a)
	{
		if (type.ExtraTypes.Length == 1 && TypeIsPrimitive(type.ExtraTypes[0].MainType.Type) && type.ExtraTypes[0].ExtraTypes.Length == 0)
		{
			var basic_type2 = type.ExtraTypes[0].MainType.Type.Peek().Name;
			if (basic_type2 == "bool")
			{
				a = ToBoolList();
				return true;
			}
			else if (basic_type2 == "byte")
			{
				a = ToByteList();
				return true;
			}
			else if (basic_type2 == "short int")
			{
				a = ToShortIntList();
				return true;
			}
			else if (basic_type2 == "unsigned short int")
			{
				a = ToUnsignedShortIntList();
				return true;
			}
			else if (basic_type2 == "char")
			{
				a = ToCharList();
				return true;
			}
			else if (basic_type2 == "int")
			{
				a = ToIntList();
				return true;
			}
			else if (basic_type2 == "unsigned int")
			{
				a = ToUnsignedIntList();
				return true;
			}
			else if (basic_type2 == "long int")
			{
				a = ToLongIntList();
				return true;
			}
			else if (basic_type2 == "unsigned long int")
			{
				a = ToUnsignedLongIntList();
				return true;
			}
			else if (basic_type2 == "real")
			{
				a = ToRealList();
				return true;
			}
			else if (basic_type2 == "string")
			{
				a = ToStringList();
				return true;
			}
		}
		a = new();
		return false;
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
		List<Universal> new_list = new(new Universal[count]);
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

	public static implicit operator Universal((IList<bool>, IList<bool>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<byte>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<short>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<ushort>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<char>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<int>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<uint>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<long>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<ulong>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<double>) x) => new(x);

	public static implicit operator Universal((IList<bool>, IList<String>) x) => new(x);

	public static Universal operator +(Universal x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basic_type = x.InnerType.MainType.Peek().Name;
			if (new List<String> { "short int", "unsigned short int", "int", "unsigned int", "real" }.Contains(basic_type))
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
				else
					return basic_type == "short int" ? ValidateFixing(+x.ToShortInt(), ShortIntType, x.Fixed) : new();
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
			if (new List<String> { "short int", "unsigned short int", "int", "unsigned int", "real" }.Contains(basic_type))
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
				else
					return basic_type == "short int" ? ValidateFixing(-x.ToShortInt(), ShortIntType, x.Fixed) : new();
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
				else
					return basic_type == "short int" ? ValidateFixing(~x.ToShortInt(), ShortIntType, x.Fixed) : new();
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
