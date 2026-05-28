global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.TypeConverters;
global using static System.Math;
global using String = NStar.Core.String;
using NStar.Mpir;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace CSharp.NStar;

[DebuggerDisplay("{ToString(true)}")]
public struct NStarEntity
{
	private readonly bool Bool;
	private readonly double Number;
	private readonly String String;
	private readonly List<NStarEntity>? NextList;
	private readonly object? Object;
	public NStarType InnerType { get; set; }
	public NStarType? OuterType { get; set; }
	public bool Fixed { get; set; }
	public static NStarEntity Infinity => 1d / 0;
	public static NStarEntity MinusInfinity => -1d / 0;
	public static NStarEntity Uncertainty => 0d / 0;

	private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

	public NStarEntity()
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

	public NStarEntity(NStarEntity other)
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

	public NStarEntity(bool @bool)
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

	public NStarEntity(char @char)
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

	public NStarEntity(double number)
	{
		Bool = false;
		Number = number;
		String = [];
		NextList = null;
		Object = null;
		if (number >= 0 && number <= 255 && Truncate(number).Equals(number))
			InnerType = ByteType;
		else if (Number >= -32768 && number <= 32767 && Truncate(number).Equals(number))
			InnerType = ShortIntType;
		else if (number >= 0 && number <= 65535 && Truncate(number).Equals(number))
			InnerType = UnsignedShortIntType;
		else if (number >= -2147483648 && number <= 2147483647 && Truncate(number).Equals(number))
			InnerType = IntType;
		else
			InnerType = number >= 0 && number <= 4294967295 && Truncate(number).Equals(number) ? UnsignedIntType : RealType;
		OuterType = null;
		Fixed = false;
	}

	public NStarEntity(String @string)
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

	public NStarEntity(List<NStarEntity> nextList)
	{
		Bool = false;
		Number = 0;
		String = [];
		NextList = nextList;
		Object = null;
		if (nextList.Length == 0)
			InnerType = GetListType(NullType);
		else
			InnerType = GetListType(nextList.Skip(1)
				.Progression(nextList[0].InnerType, (x, y) => GetResultType(x, y.InnerType, DefaultNull, DefaultNull)));
		OuterType = null;
		Fixed = false;
	}

	public NStarEntity(object @object, NStarType type)
	{
		if (@object is NStarEntity unv)
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

	public static NStarEntity Parse(string s)
	{
		string s2;
		if (s.Length == 0)
			throw new FormatException();
		else if (s == NullString)
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
		else if (s[^1] == 'n')
			return int.Parse(s[..^1], InvariantCulture);
		else if (s[^1] == 'u')
			return uint.Parse(s[..^1], InvariantCulture);
		else if (s[^1] == 'L')
		{
			s2 = s[..^1];
			var @double = false;
			if (s2.EndsWith('L'))
			{
				@double = true;
				s2 = s2[..^1];
			}
			var unsigned = false;
			if (s2.EndsWith('u'))
			{
				unsigned = true;
				s2 = s2[..^1];
			}
			if (!unsigned && int.TryParse(s2, out var i))
				return (NStarEntity)i;
			else if (uint.TryParse(s2, out var ui))
				return (NStarEntity)ui;
			else if (!unsigned && long.TryParse(s2, out var l))
				return new(l, LongIntType);
			else if (@double && ulong.TryParse(s2, out var ul))
				return new(ul, UnsignedLongIntType);
			else if (unsigned)
				return new(MpuT.Parse(s2), UnsignedLongLongType);
			else
				return new(MpzT.Parse(s2), LongLongType);
		}
		else if (s[^1] == 'r')
		{
			s2 = s[..^1];
			if (!s2.All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				throw new FormatException();
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
		else if (s[^1] == 'm')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				throw new FormatException();
			decimal n;
			try
			{
				n = int.Parse(s2, InvariantCulture);
			}
			catch
			{
				n = decimal.Parse(s2, InvariantCulture);
			}
			return ValidateFixing(new(n, DecimalType), DecimalType, true);
		}
		else if (s[^1] == 'c')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				throw new FormatException();
			return new(new Complex(double.Parse(s2), 0), ComplexType);
		}
		else if (s[^1] == 'i')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				throw new FormatException();
			return new(new Complex(0, double.Parse(s2)), ComplexType);
		}
		else if (s[0] == '\"' && s[^1] == '\"')
			return ((String)s).RemoveQuotes();
		else if (s[0] == '\'' && s[^1] == '\'')
			return s.Length <= 2 ? (NStarEntity)'\0' : (NStarEntity)((String)s).RemoveQuotes()[0];
		else if (s.Length >= 3 && s[0] == '@' && s[1] == '\"' && s[^1] == '\"')
			return ((String)s)[2..^1].Replace("\"\"", "\"");
		else if (Quotes.IsRawString(s, out var output))
			return output;
		else
		{
			if (int.TryParse(s, NumberStyles.Integer, InvariantCulture, out var i))
				return i;
			else if (long.TryParse(s, NumberStyles.Integer, InvariantCulture, out var l))
				return (NStarEntity)l;
			else if (ulong.TryParse(s, NumberStyles.Integer, InvariantCulture, out var ul))
				return (NStarEntity)ul;
			else
				return (NStarEntity)double.Parse(s, InvariantCulture);
		}
	}

	public static bool TryParse(string s, [MaybeNullWhen(false)] out NStarEntity result)
	{
		result = default;
		string s2;
		if (s.Length == 0)
			return false;
		else if (s == NullString)
			result = new();
		else if (s is "true" or "false")
			result = s == "true";
		else if (s == "Infty")
			result = Infinity;
		else if (s == "-Infty")
			result = MinusInfinity;
		else if (s == "Uncty")
			result = Uncertainty;
		else if (s == "Pi")
			result = PI;
		else if (s == "E")
			result = E;
		else if (s[0] is not (>= '0' and <= '9' or '+' or '-') && s[^1] is not ('\"' or '\'' or '\\'))
			return false;
		else if (s[^1] == 'n')
		{
			if (int.TryParse(s[..^1], InvariantCulture, out var i))
				result = i;
			else
				return false;
		}
		else if (s[^1] == 'u')
		{
			if (uint.TryParse(s[..^1], InvariantCulture, out var ui))
				result = ui;
			else
				return false;
		}
		else if (s[^1] == 'L')
		{
			s2 = s[..^1];
			var @double = false;
			if (s2.EndsWith('L'))
			{
				@double = true;
				s2 = s2[..^1];
			}
			if (s2.EndsWith('u'))
			{
				s2 = s2[..^1];
				if (!@double && ulong.TryParse(s2, out var ul))
					result = new(ul, UnsignedLongIntType);
				else if (MpuT.TryParse(s2, out var ull))
					result = new(ull, UnsignedLongLongType);
				else
					return false;
				return true;
			}
			else
			{
				if (!@double && long.TryParse(s2, out var l))
					result = new(l, LongIntType);
				else if (MpzT.TryParse(s2, out var ll))
					result = new(ll, LongLongType);
				else
					return false;
				return true;
			}
		}
		else if (s[^1] == 'r')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			double n;
			if (int.TryParse(s2, InvariantCulture, out var i))
				n = i;
			else if (!double.TryParse(s2, InvariantCulture, out n))
				return false;
			result = ValidateFixing(n, RealType, true);
		}
		else if (s[^1] == 'r')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			decimal n;
			if (int.TryParse(s2, InvariantCulture, out var i))
				n = i;
			else if (!decimal.TryParse(s2, InvariantCulture, out n))
				return false;
			result = ValidateFixing(new(n, DecimalType), DecimalType, true);
		}
		else if (s[^1] == 'c')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (!double.TryParse(s2, InvariantCulture, out var n))
				return false;
			result = new(new Complex(n, 0), ComplexType);
		}
		else if (s[^1] == 'i')
		{
			if (!(s2 = s[..^1]).All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (!double.TryParse(s2, InvariantCulture, out var n))
				return false;
			result = new(new Complex(0, n), ComplexType);
		}
		else if (s[0] == '\"' && s[^1] == '\"')
			result = ((String)s).RemoveQuotes();
		else if (s[0] == '\'' && s[^1] == '\'')
			result = s.Length <= 2 ? (NStarEntity)'\0' : (NStarEntity)((String)s).RemoveQuotes()[0];
		else if (s.Length >= 3 && s[0] == '@' && s[1] == '\"' && s[^1] == '\"')
			result = ((String)s)[2..^1].Replace("\"\"", "\"");
		else if (Quotes.IsRawString(s, out var output))
			result = output;
		else if (int.TryParse(s, NumberStyles.Integer, InvariantCulture, out var i))
			result = i;
		else if (long.TryParse(s, NumberStyles.Integer, InvariantCulture, out var l))
			result = (NStarEntity)l;
		else if (ulong.TryParse(s, NumberStyles.Integer, InvariantCulture, out var ul))
			result = (NStarEntity)ul;
		else if (double.TryParse(s, NumberStyles.Float, InvariantCulture, out var d))
			result = d;
		else
			return false;
		return true;
	}

	public static NStarEntity TryConstruct(object? element) => element switch
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

	public static NStarEntity And(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToBool() && y.ToBool());

	public static NStarEntity Or(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToBool() || y.ToBool());

	public static NStarEntity Xor(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToBool() != y.ToBool());

	public static NStarEntity Eq(NStarEntity x, NStarEntity y)
	{
		var resultType = GetResultType(x.InnerType, y.InnerType, x.ToString(true), y.ToString(true));
		return x.ToType(resultType, x.Fixed) == y.ToType(resultType, y.Fixed);
	}

	public static NStarEntity Neq(NStarEntity x, NStarEntity y)
	{
		var resultType = GetResultType(x.InnerType, y.InnerType, x.ToString(true), y.ToString(true));
		return x.ToType(resultType, x.Fixed) != y.ToType(resultType, y.Fixed);
	}

	public static NStarEntity Goe(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToReal() >= y.ToReal());

	public static NStarEntity Loe(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToReal() <= y.ToReal());

	public static NStarEntity Gt(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToReal() > y.ToReal());

	public static NStarEntity Lt(NStarEntity x, NStarEntity y) => (NStarEntity)(x.ToReal() < y.ToReal());

	public readonly NStarEntity CyclicShift(int shiftAmount)
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basicType = InnerType.MainType.Peek().Name;
			if (basicType == LongLongTypeName)
				return ValidateFixing(new(ToLongLong() << shiftAmount, LongLongType), LongLongType, Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(ToUnsignedLongLong() << shiftAmount, UnsignedLongLongType),
					UnsignedLongLongType, Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(ToReal() * Pow(2, shiftAmount), RealType, Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(ToUnsignedLongInt() is var uli
					? uli << (int)unchecked((uint)shiftAmount % (sizeof(ulong) * 8))
					| uli >>> (int)unchecked((uint)-shiftAmount % (sizeof(ulong) * 8)) : 0, UnsignedLongIntType),
					UnsignedLongIntType, Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(ToLongInt() is var li
					? li << (int)unchecked((uint)shiftAmount % (sizeof(long) * 8))
					| li >>> (int)unchecked((uint)-shiftAmount % (sizeof(long) * 8)) : 0, LongIntType), LongIntType, Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(ToUnsignedInt() is var ui
					? ui << (int)unchecked((uint)shiftAmount % (sizeof(uint) * 8))
					| ui >>> (int)unchecked((uint)-shiftAmount % (sizeof(uint) * 8)) : 0, UnsignedIntType, Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(ToInt() is var i
					? i << (int)unchecked((uint)shiftAmount % (sizeof(int) * 8))
					| i >>> (int)unchecked((uint)-shiftAmount % (sizeof(int) * 8)) : 0, IntType, Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing((ushort)(ToUnsignedShortInt() is var usi
					? usi << (int)unchecked((uint)shiftAmount % (sizeof(ushort) * 8))
					| usi >>> (int)unchecked((uint)-shiftAmount % (sizeof(ushort) * 8)) : 0), UnsignedShortIntType, Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing((short)(ToShortInt() is var si
					? si << (int)unchecked((uint)shiftAmount % (sizeof(short) * 8))
					| si >>> (int)unchecked((uint)-shiftAmount % (sizeof(short) * 8)) : 0), ShortIntType, Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing((byte)(ToByte() is var y
					? y << (int)unchecked((uint)shiftAmount % (sizeof(byte) * 8))
					| y >>> (int)unchecked((uint)-shiftAmount % (sizeof(byte) * 8)) : 0), ByteType, Fixed);
			else
				return new();
		}
		else
			return new();
	}

	// Set flag to true if you want to try to apply this function.
	public static NStarEntity ValidateFixing(NStarEntity source, NStarType type, bool doFixing = false)
	{
		NStarEntity a = new(source);
		if (doFixing)
		{
			a.InnerType = type;
			a.Fixed = true;
		}
		return a;
	}

	public readonly NStarEntity GetElement(int index)
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
		else if (TypeEqualsToPrimitive(InnerType, StringTypeName))
		{
			var @string = ToString();
			return index <= 0 || index > @string.Length ? new() : (NStarEntity)@string[index - 1];
		}
		else
		{
			var convertedToList = ToList();
			return index <= 0 || index > convertedToList.Length ? new() : convertedToList[index - 1];
		}
	}

	private static NStarEntity GetElement2<T>(int index, IList<bool> IsNullList, IList<T> MainList) =>
		index <= 0 || index > MainList.Length ? new() : IsNullList[index - 1] ? new() : TryConstruct(MainList[index - 1]);

	public readonly bool ToBool()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return false;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => false,
			BoolTypeName => Bool,
			ByteTypeName => !(Number < 1),
			ShortIntTypeName => !(Number < 1),
			UnsignedShortIntTypeName => !(Number < 1),
			CharTypeName => !(Number < 1),
			IntTypeName => !(Number < 1),
			UnsignedIntTypeName => !(Number < 1),
			LongIntTypeName => !(Object is not long li || li < 1),
			nameof(DateTime) => !(Object is not DateTime dt || dt.Ticks < 1),
			UnsignedLongIntTypeName => !(Object is not ulong uli || uli < 1),
			RealTypeName => Number >= 1,
			DecimalTypeName => !(Object is not decimal m || m is < 1),
			StringTypeName => String != "",
			"list" => !(NextList is null || NextList.Length == 0) && NextList[0].ToBool(),
			_ => false
		};
	}

	public readonly byte ToByte()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (byte)(!Bool ? 0 : 1),
			ByteTypeName => (byte)Number,
			ShortIntTypeName => (byte)(Number is < -255 or > 255 ? 0 : Abs(Number)),
			UnsignedShortIntTypeName => (byte)(Number is < -255 or > 255 ? 0 : Number),
			CharTypeName => (byte)(Number is < -255 or > 255 ? 0 : Number),
			IntTypeName => (byte)(Number is < -255 or > 255 ? 0 : Abs(Number)),
			UnsignedIntTypeName => (byte)(Number is < -255 or > 255 ? 0 : Number),
			LongIntTypeName => (byte)(Object is not long li ? 0 : li is < -255 or > 255 ? 0 : Abs(li)),
			nameof(DateTime) => (byte)(Object is not DateTime dt ? 0 : dt.Ticks > 255 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (byte)(Object is not ulong uli ? 0 : uli > 255 ? 0 : uli),
			UnsignedLongLongTypeName => (byte)(Object is not MpuT ull ? 0 : ull > 255 ? 0 : ull.Abs()),
			LongLongTypeName => (byte)(Object is not MpzT ll ? 0 : ll < -255 || ll > 255 ? 0 : ll.Abs()),
			RealTypeName => (byte)(Number is < -255 or > 255 ? 0 : Truncate(Abs(Number))),
			DecimalTypeName => (byte)(Object is not decimal m || m is < -255 or > 255 ? 0 : Truncate(Abs(m))),
			StringTypeName => 0,
			"list" => (byte)(NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToByte()),
			_ => 0
		};
	}

	public readonly short ToShortInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (short)(!Bool ? 0 : 1),
			ByteTypeName => (short)Number,
			ShortIntTypeName => (short)Number,
			UnsignedShortIntTypeName => (short)Number,
			CharTypeName => (short)Number,
			IntTypeName => (short)(Number is < -32768 or > 32767 ? 0 : Number),
			UnsignedIntTypeName => (short)(Number > 32767 ? 0 : Number),
			LongIntTypeName => (short)(Object is not long li ? 0 : li is < -32768 or > 32767 ? 0 : li),
			nameof(DateTime) => (short)(Object is not DateTime dt ? 0 : dt.Ticks > 32767 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (short)(Object is not ulong uli ? 0 : uli > 32767 ? 0 : uli),
			UnsignedLongLongTypeName => (short)(Object is not MpuT ull ? 0 : ull > 32767 ? 0 : ull),
			LongLongTypeName => (short)(Object is not MpzT ll ? 0 : ll < -32768 || ll > 32767 ? 0 : ll),
			RealTypeName => (short)(Number is < -32768 or > 32767 ? 0 : Truncate(Number)),
			DecimalTypeName => (short)(Object is not decimal m || m is < -32768 or > 32767 ? 0 : Truncate(m)),
			StringTypeName => 0,
			"list" => (short)(NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToShortInt()),
			_ => 0
		};
	}

	public readonly ushort ToUnsignedShortInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (ushort)(!Bool ? 0 : 1),
			ByteTypeName => (ushort)Number,
			ShortIntTypeName => (ushort)Number,
			UnsignedShortIntTypeName => (ushort)Number,
			CharTypeName => (ushort)Number,
			IntTypeName => (ushort)(Number is < -65535 or > 65535 ? 0 : Abs(Number)),
			UnsignedIntTypeName => (ushort)(Number is < -65535 or > 65535 ? 0 : Number),
			LongIntTypeName => (ushort)(Object is not long li ? 0 : li is < -65535 or > 65535 ? 0 : Abs(li)),
			nameof(DateTime) => (ushort)(Object is not DateTime dt ? 0 : dt.Ticks > 65535 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (ushort)(Object is not ulong uli ? 0 : uli > 65535 ? 0 : uli),
			UnsignedLongLongTypeName => (ushort)(Object is not MpuT ull ? 0 : ull > 65535 ? 0 : ull.Abs()),
			LongLongTypeName => (ushort)(Object is not MpzT ll ? 0 : ll < -65535 || ll > 65535 ? 0 : ll.Abs()),
			RealTypeName => (ushort)(Number is < -65535 or > 65535 ? 0 : Truncate(Abs(Number))),
			DecimalTypeName => (ushort)(Object is not decimal m || m is < -65535 or > 65535 ? 0 : Truncate(Abs(m))),
			StringTypeName => 0,
			"list" => (ushort)(NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedShortInt()),
			_ => 0
		};
	}

	public readonly char ToChar()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return '\0';
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => '\0',
			BoolTypeName => (char)(!Bool ? 0 : 1),
			ByteTypeName => (char)Number,
			ShortIntTypeName => (char)Number,
			UnsignedShortIntTypeName => (char)Number,
			CharTypeName => (char)Number,
			IntTypeName => (char)(Number is < -65535 or > 65535 ? 0 : Abs(Number)),
			UnsignedIntTypeName => (char)(Number is < -65535 or > 65535 ? 0 : Number),
			LongIntTypeName => (char)(Object is not long li ? 0 : li is < -65535 or > 65535 ? 0 : Abs(li)),
			nameof(DateTime) => (char)(Object is not DateTime dt ? 0 : dt.Ticks > 65535 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (char)(Object is not ulong uli ? 0 : uli > 65535 ? 0 : uli),
			UnsignedLongLongTypeName => (char)(ushort)(Object is not MpuT ull ? 0 : ull > 65535 ? 0 : ull.Abs()),
			LongLongTypeName => (char)(ushort)(Object is not MpzT ll ? 0 : ll < -65535 || ll > 65535 ? 0 : ll.Abs()),
			RealTypeName => (char)(Number is < -65535 or > 65535 ? 0 : Truncate(Abs(Number))),
			DecimalTypeName => (char)(Object is not decimal m || m is < -65535 or > 65535 ? 0 : Truncate(Abs(m))),
			StringTypeName => (char)0,
			"list" => (char)(NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToChar()),
			_ => '\0'
		};
	}

	public readonly int ToInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => !Bool ? 0 : 1,
			ByteTypeName => (int)Number,
			ShortIntTypeName => (int)Number,
			UnsignedShortIntTypeName => (int)Number,
			CharTypeName => (int)Number,
			IntTypeName => (int)Number,
			UnsignedIntTypeName => (int)Number,
			LongIntTypeName => (int)(Object is not long li ? 0 : li is < -2147483648 or > 2147483647 ? 0 : li),
			nameof(DateTime) => (int)(Object is not DateTime dt ? 0 : dt.Ticks > 2147483647 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (int)(Object is not ulong uli ? 0 : uli > 2147483647 ? 0 : uli),
			UnsignedLongLongTypeName => (int)(Object is not MpuT ull ? 0 : ull > 2147483647 ? 0 : ull),
			LongLongTypeName => (int)(Object is not MpzT ll ? 0 : ll < -2147483648 || ll > 2147483647 ? 0 : ll),
			RealTypeName => (int)(Number is < -2147483648 or > 2147483647 ? 0 : Truncate(Number)),
			DecimalTypeName => (int)(Object is not decimal m || m is < -2147483648 or > 2147483647 ? 0 : Truncate(m)),
			StringTypeName => 0,
			"list" => NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToInt(),
			_ => 0
		};
	}

	public readonly uint ToUnsignedInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (uint)(!Bool ? 0 : 1),
			ByteTypeName => (uint)Number,
			ShortIntTypeName => (uint)Abs(Number),
			UnsignedShortIntTypeName => (uint)Number,
			CharTypeName => (uint)Number,
			IntTypeName => (uint)Number,
			UnsignedIntTypeName => (uint)Number,
			LongIntTypeName => (uint)(Object is not long li ? 0 : li is < -4294967295 or > 4294967295 ? 0 : Abs(li)),
			nameof(DateTime) => (uint)(Object is not DateTime dt ? 0 : dt.Ticks > 4294967295 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (uint)(Object is not ulong uli ? 0 : uli > 4294967295 ? 0 : uli),
			UnsignedLongLongTypeName => (uint)(Object is not MpuT ull ? 0 : ull > 4294967295 ? 0 : ull),
			LongLongTypeName => (uint)(Object is not MpzT ll ? 0 : ll < -4294967295 || ll > 4294967295 ? 0 : ll.Abs()),
			RealTypeName => (uint)(Number is < -4294967295 or > 4294967295 ? 0 : Truncate(Abs(Number))),
			DecimalTypeName => (uint)(Object is not decimal m || m is < -4294967295 or > 4294967295 ? 0 : Truncate(Abs(m))),
			StringTypeName => 0,
			"list" => NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedInt(),
			_ => 0
		};
	}

	public readonly long ToLongInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (!Bool) ? 0 : 1,
			ByteTypeName => (long)Number,
			ShortIntTypeName => (long)Number,
			UnsignedShortIntTypeName => (long)Number,
			CharTypeName => (long)Number,
			IntTypeName => (long)Number,
			UnsignedIntTypeName => (long)Number,
			LongIntTypeName => (Object is not long li) ? 0 : li,
			nameof(DateTime) => (Object is not DateTime dt) ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => (long)((Object is not ulong uli) ? 0 : uli),
			UnsignedLongLongTypeName => (uint)(Object is not MpuT ull ? 0 : ull > 9223372036854775807 ? 0 : ull),
			LongLongTypeName => (long)(Object is not MpzT ll ? 0 : ll < -9223372036854775808 || ll > 9223372036854775807 ? 0 : ll),
			RealTypeName => (long)((Number is < -(double)9223372036854775808 or > 9223372036854775807) ? 0 : Truncate(Number)),
			DecimalTypeName => (long)(Object is not decimal m || m is < -(decimal)9223372036854775808 or > 9223372036854775807 ? 0 : Truncate(m)),
			StringTypeName => 0,
			"list" => (NextList is null || NextList.Length == 0) ? 0 : NextList[0].ToLongInt(),
			_ => 0
		};
	}

	public readonly DateTime ToDateTime() =>
		TypeEqualsToPrimitive(InnerType, nameof(DateTime)) ? (Object is not DateTime dt) ? new(0) : dt : new(ToLongInt());

	public readonly ulong ToUnsignedLongInt()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (ulong)(!Bool ? 0 : 1),
			ByteTypeName => (ulong)Number,
			ShortIntTypeName => (ulong)Abs(Number),
			UnsignedShortIntTypeName => (ulong)Number,
			CharTypeName => (ulong)Number,
			IntTypeName => (ulong)Number,
			UnsignedIntTypeName => (ulong)Number,
			LongIntTypeName => (ulong)(Object is not long li ? 0 : Abs(li)),
			nameof(DateTime) => (ulong)(Object is not DateTime dt ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => Object is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (ulong)(Object is not MpuT ull ? 0 : ull > 18446744073709551615 ? 0 : ull.Abs()),
			LongLongTypeName => (ulong)(Object is not MpzT ll ? 0 : ll < -(MpzT)18446744073709551615 || ll > 18446744073709551615 ? 0 : ll.Abs()),
			RealTypeName => (ulong)(Number is < -(double)18446744073709551615 or > 18446744073709551615 ? 0 : Truncate(Abs(Number))),
			DecimalTypeName => (ulong)(Object is not decimal m || m is < -(decimal)18446744073709551615 or > 18446744073709551615 ? 0 : Truncate(Abs(m))),
			StringTypeName => 0,
			"list" => NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToUnsignedLongInt(),
			_ => 0
		};
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
	}

	public readonly MpuT ToUnsignedLongLong()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (!Bool) ? 0 : 1,
			ByteTypeName => (MpuT)Number,
			ShortIntTypeName => (MpuT)Number,
			UnsignedShortIntTypeName => (MpuT)Number,
			CharTypeName => (MpuT)Number,
			IntTypeName => (MpuT)Number,
			UnsignedIntTypeName => (MpuT)Number,
			LongIntTypeName => (MpuT)((Object is not long li) ? 0 : li),
			nameof(DateTime) => (Object is not DateTime dt) ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => (MpuT)((Object is not ulong uli) ? 0 : uli),
			UnsignedLongLongTypeName => Object is not MpuT ull ? 0 : ull,
			LongLongTypeName => (MpuT)(Object is not MpzT ll ? 0 : ll),
			RealTypeName => (MpuT)Truncate(Number),
			DecimalTypeName => (MpuT)(Object is not decimal m ? 0 : Truncate(m)),
			StringTypeName => 0,
			"list" => (NextList is null || NextList.Length == 0) ? 0 : NextList[0].ToUnsignedLongLong(),
			_ => 0
		};
	}

	public readonly MpzT ToLongLong()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => (!Bool) ? 0 : 1,
			ByteTypeName => (MpzT)Number,
			ShortIntTypeName => (MpzT)Number,
			UnsignedShortIntTypeName => (MpzT)Number,
			CharTypeName => (MpzT)Number,
			IntTypeName => (MpzT)Number,
			UnsignedIntTypeName => (MpzT)Number,
			LongIntTypeName => (MpzT)((Object is not long li) ? 0 : li),
			nameof(DateTime) => (Object is not DateTime dt) ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => (MpzT)((Object is not ulong uli) ? 0 : uli),
			UnsignedLongLongTypeName => (MpzT)((Object is not MpuT ull) ? 0 : ull),
			LongLongTypeName => Object is not MpzT ll ? 0 : ll,
			RealTypeName => (MpzT)Truncate(Number),
			DecimalTypeName => (MpzT)(Object is not decimal m ? 0 : Truncate(m)),
			StringTypeName => 0,
			"list" => (NextList is null || NextList.Length == 0) ? 0 : NextList[0].ToLongLong(),
			_ => 0
		};
	}

	public readonly double ToReal()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => !Bool ? 0 : 1,
			ByteTypeName => Number,
			ShortIntTypeName => Number,
			UnsignedShortIntTypeName => Number,
			CharTypeName => Number,
			IntTypeName => Number,
			UnsignedIntTypeName => Number,
			LongIntTypeName => Object is not long li ? 0 : li,
			nameof(DateTime) => Object is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => Object is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (double)(Object is not MpuT ull ? 0 : ull),
			LongLongTypeName => (double)(Object is not MpzT ll ? 0 : ll),
			RealTypeName => Number,
			StringTypeName => 0,
			"list" => (double)(NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToReal()),
			_ => 0
		};
	}

	public readonly decimal ToDecimal()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => !Bool ? 0 : 1,
			ByteTypeName => (decimal)Number,
			ShortIntTypeName => (decimal)Number,
			UnsignedShortIntTypeName => (decimal)Number,
			CharTypeName => (decimal)Number,
			IntTypeName => (decimal)Number,
			UnsignedIntTypeName => (decimal)Number,
			LongIntTypeName => Object is not long li ? 0 : li,
			nameof(DateTime) => Object is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => Object is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (decimal)(Object is not MpuT ull ? 0 : ull),
			LongLongTypeName => (decimal)(Object is not MpzT ll ? 0 : ll),
			RealTypeName => (decimal)Number,
			DecimalTypeName => Object is not decimal m ? 0 : m,
			StringTypeName => 0,
			"list" => NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToDecimal(),
			_ => 0
		};
	}

	public readonly Complex ToComplex()
	{
		if (!TypeIsPrimitive(InnerType.MainType))
			return 0;
		var basicType = InnerType.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => !Bool ? 0 : 1,
			ByteTypeName => Number,
			ShortIntTypeName => Number,
			UnsignedShortIntTypeName => Number,
			CharTypeName => Number,
			IntTypeName => Number,
			UnsignedIntTypeName => Number,
			LongIntTypeName => Object is not long li ? 0 : li,
			nameof(DateTime) => Object is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => Object is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (Complex)(double)(Object is not MpuT ull ? 0 : ull),
			LongLongTypeName => (Complex)(double)(Object is not MpzT ll ? 0 : ll),
			RealTypeName => Number,
			DecimalTypeName => (double)(Object is not decimal m ? 0 : m),
			ComplexTypeName => Object is not Complex c ? 0 : c,
			StringTypeName => 0,
			"list" => NextList is null || NextList.Length == 0 ? 0 : NextList[0].ToComplex(),
			_ => 0
		};
	}

	public readonly String ToString(bool takeIntoQuotes = false, bool addCasting = false)
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var basicType = InnerType.MainType.Peek().Name ?? NullString;
			switch (basicType.ToString())
			{
				case NullString:
				return addCasting ? DefaultNull : NullString;
				case BoolTypeName:
				return (!Bool) ? "false" : "true";
				case ByteTypeName:
				return ((byte)Number).ToString(InvariantCulture);
				case ShortIntTypeName:
				return ((short)Number).ToString(InvariantCulture);
				case UnsignedShortIntTypeName:
				return ((ushort)Number).ToString(InvariantCulture);
				case CharTypeName:
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
				case IntTypeName:
				return ((int)Number).ToString(InvariantCulture);
				case UnsignedIntTypeName:
				return ((uint)Number).ToString(InvariantCulture);
				case LongIntTypeName:
				return Object is null ? "" : Object is long li ? li.ToString() : "0";
				case nameof(DateTime):
				return Object is null ? "" : Object is DateTime dt ? dt.ToString() : new DateTime(0).ToString();
				case UnsignedLongIntTypeName:
				return Object is null ? "" : Object is ulong uli ? uli.ToString() : "0";
				case UnsignedLongLongTypeName when Object is MpuT ull:
				if (!addCasting)
					return ull.ToString();
				if (ull <= ulong.MaxValue)
					return "new " + nameof(MpuT) + '(' + ull.ToString() + ')';
				return "new " + nameof(MpuT) + "(\"" + ull.ToString() + "\")";
				case LongLongTypeName when Object is MpzT ll:
				if (!addCasting)
					return ll.ToString();
#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
				if (ll >= long.MinValue && ll <= ulong.MaxValue)
					return "new " + nameof(MpzT) + '(' + ll.ToString() + ')';
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
				return "new " + nameof(MpzT) + "(\"" + ll.ToString() + "\")";
				case RealTypeName:
				return Number switch
				{
					1d / 0 => addCasting ? "(1d / 0)" : "Infty",
					-1d / 0 => addCasting ? "(-1d / 0)" : "-Infty",
					0d / 0 => addCasting ? "(0d / 0)" : "Uncty",
					-0d => "0",
					_ => Number.ToString(InvariantCulture)
				};
				case ComplexTypeName when Object is Complex c:
				return (addCasting ? "new Complex(" : "") + c.Real switch
				{
					1d / 0 => addCasting ? "(1d / 0)" : "Infty",
					-1d / 0 => addCasting ? "(-1d / 0)" : "-Infty",
					0d / 0 => addCasting ? "(0d / 0)" : "Uncty",
					-0d => "0",
					_ => c.Real.ToString(InvariantCulture)
				} + (addCasting ? ", " : c.Imaginary is 0d / 0 or >= 0 ? "+" : "") + c.Imaginary switch
				{
					1d / 0 => addCasting ? "(1d / 0)" : "Infty",
					-1d / 0 => addCasting ? "(-1d / 0)" : "-Infty",
					0d / 0 => addCasting ? "(0d / 0)" : "Uncty",
					-0d => "0",
					_ => c.Imaginary.ToString(InvariantCulture)
				} + (addCasting ? ")" : "i");
				case RecursiveTypeName:
				return Object is null ? "" : Object is NStarType NStarType ? NStarType.ToString() : NullType.ToString();
				case StringTypeName:
				if (!takeIntoQuotes)
					return String;
				else if (addCasting)
					return ((String)"((").AddRange(nameof(String)).Add(')').AddRange(String.TakeIntoQuotes(true)).Add(')');
				else
					return String.TakeIntoQuotes();
				case "list":
				if (basicType == "tuple")
					return ListToString();
				return takeIntoQuotes ? "Unknown Object" : "";
			}
			return Object switch
			{
				(IList<bool> BoolIsNullList, IList<bool> BoolList) => ListToString(BoolIsNullList, BoolList),
				(IList<bool> ByteIsNullList, IList<byte> ByteList) => ListToString(ByteIsNullList, ByteList),
				(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => ListToString(ShortIntIsNullList, ShortIntList),
				(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) =>
					ListToString(UnsignedShortIntIsNullList, UnsignedShortIntList),
				(IList<bool> CharIsNullList, IList<char> CharList) => ListToString(CharIsNullList, CharList),
				(IList<bool> IntIsNullList, IList<int> IntList) => ListToString(IntIsNullList, IntList),
				(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) =>
					ListToString(UnsignedIntIsNullList, UnsignedIntList),
				(IList<bool> RealIsNullList, IList<double> RealList) => ListToString(RealIsNullList, RealList),
				(IList<bool> StringIsNullList, IList<string> StringList) => ListToString(StringIsNullList, StringList),
				_ => ListToString()
			};
		}
		else if (InnerType.MainType.Length != 0
			&& UserDefinedTypes.TryGetValue(SplitType(InnerType.MainType), out var userDefinedType)
			&& userDefinedType.Decomposition != null && userDefinedType.Decomposition.Length != 0)
			return ListToString(InnerType.MainType.Peek().Name.ToString());
		return takeIntoQuotes ? "Unknown Object" : "";
	}

	private readonly string ListToString(string typeName = "")
	{
		var convertedToList = ToList();
		if (convertedToList.Length == 0)
			return (typeName == "" ? "ListWithSingle" : "new " + typeName) + "(null)";
		else if (convertedToList.Length == 1)
			return (typeName == "" ? "ListWithSingle" : "new " + typeName) + "(" + convertedToList[0].ToString(true) + ")";
		String output = new(convertedToList.Length * 4 + 2) { '(' };
		if (typeName != "")
			output.Insert(0, "new " + typeName);
		output.AddRange(convertedToList[0].ToString(true));
		for (var i = 1; i <= convertedToList.Length - 1; i++)
		{
			output.AddRange(", ");
			output.AddRange(convertedToList[i].ToString(true));
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

	public static NStarEntity PerformOperation<T>(NStarEntity x, NStarEntity y, Func<NStarEntity, T> Input,
		Func<T, T, NStarEntity> Output, String leftType, String rightType, String inputType) =>
		ValidateFixing(Output(Input(x), Input(y)), GetPrimitiveType(inputType),
		x.Fixed && leftType == inputType || y.Fixed && rightType == inputType);

	public static NStarEntity PerformOperation<T>(T x, T y, Func<T, T, NStarEntity> Process,
		String leftType, String rightType, String inputType) =>
		ValidateFixing(Process(x, y), GetPrimitiveType(inputType), leftType == inputType || rightType == inputType);

	public readonly List<NStarEntity> ToList()
	{
		if (TypeIsPrimitive(InnerType.MainType) && InnerType.MainType.Peek().Name != "list"
			&& InnerType.MainType.Peek().Name != "tuple")
			return [this];
		return Object switch
		{
			(IList<bool> BoolIsNullList, IList<bool> BoolList) => ToList2(BoolIsNullList, BoolList),
			(IList<bool> ByteIsNullList, IList<byte> ByteList) => ToList2(ByteIsNullList, ByteList),
			(IList<bool> ShortIntIsNullList, IList<short> ShortIntList) => ToList2(ShortIntIsNullList, ShortIntList),
			(IList<bool> UnsignedShortIntIsNullList, IList<ushort> UnsignedShortIntList) =>
				ToList2(UnsignedShortIntIsNullList, UnsignedShortIntList),
			(IList<bool> CharIsNullList, IList<char> CharList) => ToList2(CharIsNullList, CharList),
			(IList<bool> IntIsNullList, IList<int> IntList) => ToList2(IntIsNullList, IntList),
			(IList<bool> UnsignedIntIsNullList, IList<uint> UnsignedIntList) =>
				ToList2(UnsignedIntIsNullList, UnsignedIntList),
			(IList<bool> LongIntIsNullList, IList<long> LongIntList) => ToList2(LongIntIsNullList, LongIntList),
			(IList<bool> UnsignedLongIntIsNullList, IList<ulong> UnsignedLongIntList) =>
				ToList2(UnsignedLongIntIsNullList, UnsignedLongIntList),
			(IList<bool> RealIsNullList, IList<double> RealList) => ToList2(RealIsNullList, RealList),
			(IList<bool> StringIsNullList, IList<string> StringList) => ToList2(StringIsNullList, StringList),
			_ => NextList ?? []
		};
	}

	private static List<NStarEntity> ToList2<T>(IList<bool> IsNullList, IList<T> MainList)
	{
		List<NStarEntity> output = new(MainList.Length);
		for (var i = 0; i < MainList.Length; i++)
			output.Add(IsNullList[i] ? new() : TryConstruct(MainList[i]));
		return output;
	}

	public NStarEntity ToType(NStarType type, bool fix = false)
	{
		try
		{
			NStarEntity a;
			if (TypeIsPrimitive(type.MainType))
				a = ToPrimitiveType(type, fix);
			else if (type.Equals(InnerType))
				a = this;
			else if (type.MainType.Length != 0 && UserDefinedTypes.TryGetValue(SplitType(type.MainType), out var typeDescr)
				&& typeDescr.Decomposition != null && typeDescr.Decomposition.Length != 0)
				a = ToTupleType(typeDescr.Decomposition);
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

	private NStarEntity ToPrimitiveType(NStarType type, bool fix)
	{
		var basicType = type.MainType.Peek().Name;
		if (basicType == NullString)
			return new();
		else if (basicType == "universal")
			return this;
		else if (basicType == BoolTypeName)
			return ToBool();
		else if (basicType == ByteTypeName)
			return ToByte();
		else if (basicType == ShortIntTypeName)
			return ToShortInt();
		else if (basicType == UnsignedShortIntTypeName)
			return ToUnsignedShortInt();
		else if (basicType == CharTypeName)
			return ToChar();
		else if (basicType == IntTypeName)
			return ToInt();
		else if (basicType == UnsignedIntTypeName)
			return ToUnsignedInt();
		else if (basicType == LongIntTypeName)
			return new(ToLongInt(), LongIntType);
		else if (basicType == nameof(DateTime))
			return new(ToDateTime(), GetPrimitiveType(nameof(DateTime)));
		else if (basicType == UnsignedLongIntTypeName)
			return new(ToUnsignedLongInt(), UnsignedLongIntType);
		else if (basicType == UnsignedLongLongTypeName)
			return new(ToUnsignedLongLong(), UnsignedLongLongType);
		else if (basicType == LongLongTypeName)
			return new(ToLongLong(), LongLongType);
		else if (basicType == RealTypeName)
			return ToReal();
		else if (basicType == ComplexTypeName)
			return ToComplex();
		else if (basicType == RecursiveTypeName)
			return TypeIsPrimitive(InnerType.MainType) && InnerType.MainType.Peek().Name == RecursiveTypeName ? this : new();
		else if (basicType == StringTypeName)
			return ToString();
		else if (basicType == "list")
		{
			(var LeftDepth, var LeftLeafType) = GetTypeDepthAndLeafType(type);
			(var RightDepth, var RightLeafType) = GetTypeDepthAndLeafType(InnerType);
			return ToFullListType(type, LeftDepth, LeftLeafType, RightDepth, RightLeafType, fix);
		}
		else return basicType == "tuple" ? ToTupleType(type.ExtraTypes) : new();
	}

	private NStarEntity ToFullListType(NStarType type, int LeftDepth, NStarType LeftLeafType,
		int RightDepth, NStarType RightLeafType, bool fix = false)
	{
		if (LeftDepth == 0)
			return ToType(LeftLeafType, fix);
		else if (LeftDepth > RightDepth)
		{
			var typesList = new NStarType[LeftDepth - RightDepth + 1];
			typesList[0] = type;
			for (var i = 0; i < LeftDepth - RightDepth; i++)
				typesList[i + 1] = GetSubtype(typesList[i]);
			NStarEntity element;
			if (RightDepth == 0)
				element = ToType(typesList[LeftDepth - RightDepth], true);
			else
				element = ToFullListType(typesList[LeftDepth - RightDepth], RightDepth,
					LeftLeafType, RightDepth, RightLeafType, true);
			for (var i = LeftDepth - RightDepth - 1; i >= 0; i--)
				element = ValidateFixing(new List<NStarEntity> { element }, typesList[i], i > 0 || fix);
			return element;
		}
		else if (LeftDepth == RightDepth || TypeEqualsToPrimitive(LeftLeafType, StringTypeName))
		{
			var oldList = ToList();
			List<NStarEntity> newList = new(oldList.Length);
			for (var i = 0; i < oldList.Length; i++)
				newList.Add(oldList[i].ToFullListType(GetSubtype(type), LeftDepth - 1, LeftLeafType,
					RightDepth - 1, RightLeafType, true));
			return ValidateFixing(newList, type, fix);
		}
		else
		{
			var element = this;
			for (var i = 0; i < RightDepth - LeftDepth; i++)
				element = element.GetElement(1);
			return element.ToFullListType(type, LeftDepth, LeftLeafType, LeftDepth, RightLeafType, fix);
		}
	}

	private readonly NStarEntity ToTupleType(BranchCollection typeParts)
	{
		var count = 0;
		List<int> numbers = [];
		for (var i = 0; i < typeParts.Length; i++)
		{
			if (i >= 1 && typeParts[i].Name != "type" && int.TryParse(typeParts[i].Name.ToString(), out var number))
			{
				count += number - 1;
				numbers.Add(number - 1);
			}
			else
				count++;
		}
		numbers.Add(1);
		var oldList = ToList();
		List<NStarEntity> newList = [.. new NStarEntity[count]];
		int tpos = 0, tpos2, npos = 0;
		for (var i = 0; i < count && i < oldList.Length; i++)
		{
			if (tpos >= 1 && typeParts[tpos].Name != "type" && int.TryParse(typeParts[tpos].Name.ToString(), out _))
			{
				tpos2 = tpos - 1;
				numbers[npos]--;
			}
			else
				tpos2 = tpos;
			if (typeParts[tpos2].Name != "type" || typeParts[tpos2].Extra is not NStarType InnerNStarType)
				continue;
			newList[i] = oldList[i].ToType(InnerNStarType, true);
			if (tpos2 == tpos || numbers[npos] == 0)
				tpos++;
			if (numbers[npos] == 0)
				npos++;
		}
		for (var i = oldList.Length; i < count; i++)
			newList[i] = new();
		return newList;
	}

	public static String GetQuotientType(String leftType, NStarEntity right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		string t;
		if (leftType == LongLongTypeName)
			return LongLongTypeName;
		else if (leftType == UnsignedLongLongTypeName)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongLongTypeName;
		}
		else if (leftType == UnsignedLongIntTypeName)
		{
			if (right.ToUnsignedLongInt() >= 1uL << 56)
				return ByteTypeName;
			else if (right.ToUnsignedLongInt() >= 1uL << 48)
				return UnsignedShortIntTypeName;
			else if (right.ToUnsignedLongInt() >= 4294967296)
				return UnsignedIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongIntTypeName;
		}
		else if (leftType == LongIntTypeName)
		{
			if (right.ToLongInt() >= 1L << 48)
				return ShortIntTypeName;
			else if (right.ToLongInt() >= 4294967296)
				return IntTypeName;
			else if (rightType == UnsignedLongIntTypeName)
				return LongLongTypeName;
			else
				return LongIntTypeName;
		}
		else if (leftType == (t = "long char") || rightType == t)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return "long char";
		}
		else if (leftType == UnsignedIntTypeName)
		{
			if (right.ToUnsignedInt() >= 16777216)
				return ByteTypeName;
			else if (right.ToUnsignedInt() >= 65536)
				return UnsignedShortIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return UnsignedIntTypeName;
		}
		else if (leftType == IntTypeName)
		{
			if (rightType == UnsignedIntTypeName)
				return LongIntTypeName;
			else if (right.ToInt() >= 65536)
				return ShortIntTypeName;
			else
				return IntTypeName;
		}
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return CharTypeName;
		}
		else if (leftType == UnsignedShortIntTypeName)
			return ValidateUnsignedShortIntType(right.ToUnsignedShortInt() >= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	public static String GetRemainderType(String leftType, NStarEntity right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		string t;
		if (leftType == LongLongTypeName)
		{
			if (right.ToLongLong() <= 32768)
				return ShortIntTypeName;
			else if (right.ToLongLong() <= 2147483648)
				return IntTypeName;
			else if (right.ToLongLong() <= 9223372036854775808)
				return LongIntTypeName;
			else
				return LongLongTypeName;
		}
		else if (leftType == UnsignedLongLongTypeName)
		{
			if (right.ToUnsignedLongLong() <= 256)
				return ByteTypeName;
			else if (right.ToUnsignedLongLong() <= 65536)
				return UnsignedShortIntTypeName;
			else if (right.ToUnsignedLongLong() <= 4294967296)
				return UnsignedIntTypeName;
			else if (right.ToUnsignedLongLong() <= MpuT.One << 64)
				return UnsignedLongIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongLongTypeName;
		}
		else if (leftType == UnsignedLongIntTypeName)
		{
			if (right.ToUnsignedLongInt() <= 256)
				return ByteTypeName;
			else if (right.ToUnsignedLongInt() <= 65536)
				return UnsignedShortIntTypeName;
			else if (right.ToUnsignedLongInt() <= 4294967296)
				return UnsignedIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongIntTypeName;
		}
		else if (leftType == LongIntTypeName)
		{
			if (right.ToLongInt() <= 32768)
				return ShortIntTypeName;
			else if (right.ToLongInt() <= 2147483648)
				return IntTypeName;
			else if (rightType == UnsignedLongIntTypeName)
				return LongLongTypeName;
			else
				return LongIntTypeName;
		}
		else if (leftType == (t = "long char") || rightType == t)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return "long char";
		}
		else if (leftType == UnsignedIntTypeName)
		{
			if (right.ToUnsignedInt() <= 256)
				return ByteTypeName;
			else if (right.ToUnsignedInt() <= 65536)
				return UnsignedShortIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return UnsignedIntTypeName;
		}
		else if (leftType == IntTypeName)
		{
			if (rightType == UnsignedIntTypeName)
				return LongIntTypeName;
			else if (right.ToInt() <= 32768)
				return ShortIntTypeName;
			else
				return IntTypeName;
		}
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return CharTypeName;
		}
		else if (leftType == UnsignedShortIntTypeName)
			return ValidateUnsignedShortIntType(right.ToUnsignedShortInt() <= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	private static String? ValidateRealType(String leftType, String rightType)
	{
		string t;
		if (leftType == (t = LongComplexTypeName) || rightType == t || leftType == (t = LongRealTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongLongTypeName) || rightType == t)
		{
			if (leftType == (t = RealTypeName) || rightType == t)
				return LongRealTypeName;
			else
				return null;
		}
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or RealTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or RealTypeName)
				return LongRealTypeName;
			else
				return null;
		}
		else if (leftType == (t = ComplexTypeName) || rightType == t || leftType == (t = RealTypeName) || rightType == t)
			return t;
		else if (rightType == BoolTypeName)
			return ByteTypeName;
		return null;
	}

	private static String ValidateUnsignedShortIntType(bool condition, String rightType)
	{
		if (condition)
			return ByteTypeName;
		else if (rightType == ShortIntTypeName)
			return IntTypeName;
		else
			return UnsignedShortIntTypeName;
	}

	private static String ValidatePostUSIType(String leftType, String rightType)
	{
		string t;
		if (leftType == ShortIntTypeName)
		{
			if (rightType == UnsignedShortIntTypeName)
				return IntTypeName;
			else
				return ShortIntTypeName;
		}
		else if (leftType == (t = "short char") || rightType == t)
			return "short char";
		else if (leftType.AsSpan() is ByteTypeName or BoolTypeName)
			return ByteTypeName;
		else
			return NullString;
	}

	public override readonly bool Equals(object? obj) => obj != null
		&& obj is NStarEntity m && ToBool() == m.ToBool() && ToReal() == m.ToReal() && ToString() == m.ToString();

	public override readonly int GetHashCode()
	{
		if (TypeIsPrimitive(InnerType.MainType))
		{
			var s = InnerType.MainType.Peek().Name;
			if (s == NullString)
				return 0;
			else if (s == BoolTypeName)
				return Bool.GetHashCode();
			else if (s.AsSpan() is ByteTypeName or ShortIntTypeName or UnsignedShortIntTypeName or IntTypeName or UnsignedIntTypeName or RealTypeName)
				return Number.GetHashCode();
			else if (s == CharTypeName)
				return ((char)Number).GetHashCode();
			else if (s == LongIntTypeName && Object is long li)
				return li.GetHashCode();
			else if (s == nameof(DateTime) && Object is DateTime dt)
				return dt.GetHashCode();
			else if (s == UnsignedLongIntTypeName && Object is ulong uli)
				return uli.GetHashCode();
			else if (s == UnsignedLongLongTypeName && Object is ulong ull)
				return ull.GetHashCode();
			else if (s == LongLongTypeName && Object is MpzT ll)
				return ll.GetHashCode();
			else if (s == ComplexTypeName && Object is Complex c)
				return c.GetHashCode();
			else if (s == StringTypeName)
				return String.GetHashCode();
			else if (s == "list")
				return (NextList is null || NextList.Length == 0) ? 0 : NextList.Progression(0, (x, y) => x ^ y.GetHashCode());
		}
		return 0;
	}

	public static implicit operator NStarEntity(bool x) => new(x);

	public static implicit operator NStarEntity(ushort x) => new(x);

	public static implicit operator NStarEntity(char x) => new(x);

	public static implicit operator NStarEntity(int x) => new(x);

	public static implicit operator NStarEntity(uint x) => new(x);

	public static implicit operator NStarEntity(long x) => new(x, LongIntType);

	public static implicit operator NStarEntity(ulong x) => new(x, UnsignedLongIntType);

	public static implicit operator NStarEntity(MpzT x) => new(x, LongLongType);

	public static implicit operator NStarEntity(MpuT x) => new(x, UnsignedLongLongType);

	public static implicit operator NStarEntity(double x) => new(x);

	public static implicit operator NStarEntity(Complex x) => new(x, ComplexType);

	public static implicit operator NStarEntity(string x) => new((String)x);

	public static implicit operator NStarEntity(String x) => new(x);

	public static implicit operator NStarEntity(List<NStarEntity> x) => new(x);

	public static NStarEntity operator +(NStarEntity x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basicType = x.InnerType.MainType.Peek().Name;
			if (basicType == ComplexTypeName)
				return ValidateFixing(new(+x.ToComplex(), ComplexType), ComplexType, x.Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(+x.ToReal(), RealType, x.Fixed);
			else if (basicType == LongLongTypeName)
				return ValidateFixing(new(+x.ToLongLong(), LongLongType), LongLongType, x.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(+x.ToUnsignedLongLong(), UnsignedLongLongType), UnsignedLongLongType, x.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(+x.ToUnsignedLongInt(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(+x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(+x.ToUnsignedInt(), UnsignedIntType, x.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(+x.ToInt(), IntType, x.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(+x.ToUnsignedShortInt(), UnsignedShortIntType, x.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(+x.ToShortInt(), ShortIntType, x.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(+x.ToByte(), ByteType, x.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static NStarEntity operator -(NStarEntity x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basicType = x.InnerType.MainType.Peek().Name;
			if (basicType == ComplexTypeName)
				return ValidateFixing(new(-x.ToComplex(), ComplexType), ComplexType, x.Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(-x.ToReal(), RealType, x.Fixed);
			else if (basicType == LongLongTypeName)
				return ValidateFixing(new(-x.ToLongLong(), LongLongType), LongLongType, x.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(-x.ToLongLong(), UnsignedLongLongType), UnsignedLongLongType, x.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(-x.ToLongLong(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(-x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(-x.ToLongInt(), UnsignedIntType, x.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(-x.ToInt(), IntType, x.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(-x.ToShortInt(), UnsignedShortIntType, x.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(-x.ToShortInt(), ShortIntType, x.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(-x.ToByte(), ByteType, x.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static NStarEntity operator !(NStarEntity x) =>
		ValidateFixing(!x.ToBool(), BoolType,
		x.Fixed && TypeIsPrimitive(x.InnerType.MainType) && x.InnerType.MainType.Peek().Name == BoolTypeName);

	public static NStarEntity operator ~(NStarEntity x)
	{
		if (TypeIsPrimitive(x.InnerType.MainType))
		{
			var basicType = x.InnerType.MainType.Peek().Name;
			if (basicType == LongLongTypeName)
				return ValidateFixing(new(~x.ToLongLong(), LongLongType), LongLongType, x.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(~x.ToUnsignedLongLong(), UnsignedLongLongType), UnsignedLongLongType, x.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(~x.ToUnsignedLongInt(), UnsignedLongIntType), UnsignedLongIntType, x.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(~x.ToLongInt(), LongIntType), LongIntType, x.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(~x.ToUnsignedInt(), UnsignedIntType, x.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(~x.ToInt(), IntType, x.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(~x.ToUnsignedShortInt(), UnsignedShortIntType, x.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(~x.ToShortInt(), ShortIntType, x.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(~x.ToByte(), ByteType, x.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static NStarEntity operator +(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		string t;
		if (leftType == (t = StringTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToString(), (x, y) => x.Concat(y), leftType, rightType, t);
		else if (leftType == (t = ComplexTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToComplex(),
				(x, y) => new(x + y, ComplexType), leftType, rightType, t);
		else if (leftType == (t = RealTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x + y, LongLongType), leftType, rightType, t);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x + y, UnsignedLongLongType), leftType, rightType, t);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x + y, UnsignedLongIntType), leftType, rightType, t);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x + y, LongIntType), leftType, rightType, t);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x + y, leftType, rightType, t);
		else if (leftType == (t = ByteTypeName) || leftType == BoolTypeName || rightType == t || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(), (x, y) => x + y, leftType, rightType, t);
		else
			return new();
	}

	public static NStarEntity operator -(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		string t;
		if (leftType == (t = StringTypeName) || rightType == t)
			return StringSubtract(left, right, leftType, rightType);
		else if (leftType == (t = ComplexTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToComplex(),
				(x, y) => new(x - y, ComplexType), leftType, rightType, t);
		else if (leftType == (t = RealTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x - y, LongLongType), leftType, rightType, t);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x - y, UnsignedLongLongType), leftType, rightType, t);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x - y, UnsignedLongIntType), leftType, rightType, t);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x - y, LongIntType), leftType, rightType, t);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x - y, leftType, rightType, t);
		else if (leftType == (t = ByteTypeName) || leftType == BoolTypeName || rightType == t || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(), (x, y) => x - y, leftType, rightType, t);
		else
			return new();
	}

	private static NStarEntity StringSubtract(NStarEntity left, NStarEntity right, String leftType, String rightType)
	{
		if (byte.TryParse(left.ToString().ToString(), out var leftByte)
			&& byte.TryParse(right.ToString().ToString(), out var rightByte))
			return PerformOperation(leftByte, rightByte, (x, y) => x - y, leftType, rightType, ByteTypeName);
		else if (short.TryParse(left.ToString().ToString(), out var leftShortInt)
			&& short.TryParse(right.ToString().ToString(), out var rightShortInt))
			return PerformOperation(leftShortInt, rightShortInt, (x, y) => x - y, leftType, rightType, ShortIntTypeName);
		else if (ushort.TryParse(left.ToString().ToString(), out var leftUnsignedShortInt)
			&& ushort.TryParse(right.ToString().ToString(), out var rightUnsignedShortInt))
			return PerformOperation(leftUnsignedShortInt, rightUnsignedShortInt,
				(x, y) => x - y, leftType, rightType, UnsignedShortIntTypeName);
		else if (int.TryParse(left.ToString().ToString(), out var leftInt) &&
			int.TryParse(right.ToString().ToString(), out var rightInt))
			return PerformOperation(leftInt, rightInt, (x, y) => x - y, leftType, rightType, IntTypeName);
		else if (uint.TryParse(left.ToString().ToString(), out var leftUnsignedInt) &&
			uint.TryParse(right.ToString().ToString(), out var rightUnsignedInt))
			return PerformOperation(leftUnsignedInt, rightUnsignedInt,
				(x, y) => x - y, leftType, rightType, UnsignedIntTypeName);
		else if (long.TryParse(left.ToString().ToString(), out var leftLongInt)
			&& long.TryParse(right.ToString().ToString(), out var rightLongInt))
			return PerformOperation(leftLongInt, rightLongInt,
				(x, y) => new(x - y, LongIntType), leftType, rightType, LongIntTypeName);
		else if (ulong.TryParse(left.ToString().ToString(), out var leftUnsignedLongInt) &&
			ulong.TryParse(right.ToString().ToString(), out var rightUnsignedLongInt))
			return PerformOperation(leftUnsignedLongInt, rightUnsignedLongInt,
				(x, y) => new(x - y, UnsignedLongIntType), leftType, rightType, UnsignedLongIntTypeName);
		else if (MpuT.TryParse(left.ToString().ToString(), out var leftUnsignedLongLong) &&
			MpuT.TryParse(right.ToString().ToString(), out var rightUnsignedLongLong))
			return PerformOperation(leftUnsignedLongLong, rightUnsignedLongLong,
				(x, y) => new(x - y, UnsignedLongLongType), leftType, rightType, UnsignedLongLongTypeName);
		else if (MpzT.TryParse(left.ToString().ToString(), out var leftLongLong)
			&& MpzT.TryParse(right.ToString().ToString(), out var rightLongLong))
			return PerformOperation(leftLongLong, rightLongLong,
				(x, y) => new(x - y, LongLongType), leftType, rightType, LongLongTypeName);
		else if (double.TryParse(left.ToString().ToString(), out var leftReal)
			&& double.TryParse(right.ToString().ToString(), out var rightReal))
			return PerformOperation(leftReal, rightReal, (x, y) => x - y, leftType, rightType, RealTypeName);
		else
			return new();
	}

	public static NStarEntity operator *(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		string t;
		if (leftType == StringTypeName)
			return StringMultiply(left, right, rightType);
		else if (rightType == StringTypeName)
			return new String(RedStarLinq.Fill(right.ToString(), Max((int)left.ToUnsignedInt(), 0)).JoinIntoSingle());
		else if (leftType == (t = ComplexTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(), (x, y) => x * y, leftType, rightType, t);
		else if (leftType == (t = RealTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(), (x, y) => x * y, leftType, rightType, t);
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x * y, LongLongType), leftType, rightType, t);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
		{
			if (leftType == LongIntTypeName || rightType == LongIntTypeName
				|| leftType == IntTypeName || rightType == IntTypeName
				|| leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return PerformOperation(left, right, x => x.ToLongLong(),
					(x, y) => new(x * y, LongLongType), leftType, rightType, t);
			else
				return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
					(x, y) => new(x * y, UnsignedLongLongType), leftType, rightType, t);
		}
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
		{
			if (leftType == LongIntTypeName || rightType == LongIntTypeName
				|| leftType == IntTypeName || rightType == IntTypeName
				|| leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return PerformOperation(left, right, x => x.ToLongLong(),
					(x, y) => new(x * y, LongLongType), leftType, rightType, t);
			else
				return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
					(x, y) => new(x * y, UnsignedLongIntType), leftType, rightType, t);
		}
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x * y, LongIntType), leftType, rightType, t);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
		{
			if (leftType == IntTypeName || rightType == IntTypeName
				|| leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return PerformOperation(left, right, x => x.ToLongInt(),
					(x, y) => new(x * y, LongIntType), leftType, rightType, t);
			else
				return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x * y, leftType, rightType, t);
		}
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(), (x, y) => x * y, leftType, rightType, t);
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return PerformOperation(left, right, x => x.ToInt(),
					(x, y) => new(x * y, IntType), leftType, rightType, t);
			else
				return PerformOperation(left, right, x => x.ToChar(), (x, y) => x * y, leftType, rightType, t);
		}
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
		{
			if (leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return PerformOperation(left, right, x => x.ToInt(),
					(x, y) => new(x * y, IntType), leftType, rightType, t);
			else
				return PerformOperation(left, right, x => x.ToUnsignedShortInt(), (x, y) => x * y, leftType, rightType, t);
		}
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x * y, leftType, rightType, t);
		else if (leftType == (t = ByteTypeName) || leftType == BoolTypeName || rightType == t || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(), (x, y) => x * y, leftType, rightType, t);
		else
			return new();
	}

	private static NStarEntity StringMultiply(NStarEntity left, NStarEntity right, String rightType)
	{
		if (rightType != StringTypeName || uint.TryParse(right.ToString().ToString(), out _))
			return (NStarEntity)new String(RedStarLinq.Fill(left.ToString(), Max((int)right.ToUnsignedInt(), 0))
				.JoinIntoSingle());
		if (!uint.TryParse(left.ToString().ToString(), out _))
			return new();
		else
			return (NStarEntity)new String(RedStarLinq.Fill(right.ToString(), Max((int)left.ToUnsignedInt(), 0))
				.JoinIntoSingle());
	}

	public static NStarEntity operator /(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		var quotientType = GetQuotientType(leftType, right, rightType);
		string t;
		if (leftType == (t = StringTypeName) || rightType == t)
			return StringDivide(left, right, leftType, rightType);
		else if (leftType == (t = ComplexTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToComplex(),
				(x, y) => new(x / y, ComplexType), leftType, rightType, quotientType);
		else if (leftType == (t = RealTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(), (x, y) => x / y, leftType, rightType, quotientType);
		else if (right == 0)
			return new();
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x / y, LongLongType), leftType, rightType, quotientType);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x / y, UnsignedLongLongType), leftType, rightType, quotientType);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x / y, UnsignedLongIntType), leftType, rightType, quotientType);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x / y, LongIntType), leftType, rightType, quotientType);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(), (x, y) => x / y, leftType, rightType, quotientType);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(), (x, y) => x / y, leftType, rightType, quotientType);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(), (x, y) => x / y, leftType, rightType, quotientType);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(),
				(x, y) => x / y, leftType, rightType, quotientType);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(), (x, y) => x / y, leftType, rightType, quotientType);
		else if (leftType == (quotientType = ByteTypeName) || leftType == BoolTypeName || rightType == quotientType || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(), (x, y) => x / y, leftType, rightType, quotientType);
		else
			return new();
	}

	private static NStarEntity StringDivide(NStarEntity left, NStarEntity right, String leftType, String rightType)
	{
		var t = GetQuotientType(leftType, right, rightType);
		if (short.TryParse(left.ToString().ToString(), out var leftShortInt)
			&& short.TryParse(right.ToString().ToString(), out var rightShortInt))
			return PerformOperation(leftShortInt, rightShortInt, (x, y) => x / y, leftType, rightType, t);
		else if (ushort.TryParse(left.ToString().ToString(), out var leftUnsignedShortInt)
			&& ushort.TryParse(right.ToString().ToString(), out var rightUnsignedShortInt))
			return PerformOperation(leftUnsignedShortInt, rightUnsignedShortInt,
				(x, y) => x / y, leftType, rightType, t);
		else if (int.TryParse(left.ToString().ToString(), out var leftInt)
			&& int.TryParse(right.ToString().ToString(), out var rightInt))
			return PerformOperation(leftInt, rightInt, (x, y) => x / y, leftType, rightType, t);
		else if (uint.TryParse(left.ToString().ToString(), out var leftUnsignedInt)
			&& uint.TryParse(right.ToString().ToString(), out var rightUnsignedInt))
			return PerformOperation(leftUnsignedInt, rightUnsignedInt, (x, y) => x / y, leftType, rightType, t);
		else if (long.TryParse(left.ToString().ToString(), out var leftLongInt)
			&& long.TryParse(right.ToString().ToString(), out var rightLongInt))
			return PerformOperation(leftLongInt, rightLongInt, (x, y) => new(x / y, LongIntType), leftType, rightType, t);
		else if (ulong.TryParse(left.ToString().ToString(), out var leftUnsignedLongInt)
			&& ulong.TryParse(right.ToString().ToString(), out var rightUnsignedLongInt))
			return PerformOperation(leftUnsignedLongInt, rightUnsignedLongInt,
				(x, y) => new(x / y, UnsignedLongIntType), leftType, rightType, t);
		else if (MpuT.TryParse(left.ToString().ToString(), out var leftUnsignedLongLong)
			&& MpuT.TryParse(right.ToString().ToString(), out var rightUnsignedLongLong))
			return PerformOperation(leftUnsignedLongLong, rightUnsignedLongLong,
				(x, y) => new(x / y, UnsignedLongLongType), leftType, rightType, t);
		else if (MpzT.TryParse(left.ToString().ToString(), out var leftLongLong)
			&& MpzT.TryParse(right.ToString().ToString(), out var rightLongLong))
			return PerformOperation(leftLongLong, rightLongLong, (x, y) => new(x / y, LongLongType), leftType, rightType, t);
		else if (double.TryParse(left.ToString().ToString(), out var leftReal)
			&& double.TryParse(right.ToString().ToString(), out var rightReal))
			return PerformOperation(leftReal, rightReal, (x, y) => x / y, leftType, rightType, t);
		else
			return new();
	}

	public static NStarEntity operator %(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		var remainderType = GetRemainderType(leftType, right, rightType);
		string t;
		if (right.ToReal() == 0)
			return new();
		else if (leftType == (t = StringTypeName) || rightType == t)
			return StringMod(left, right, leftType, rightType);
		else if (leftType == (t = RealTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToReal(),
				(x, y) => x - Truncate(x / y) * y, leftType, rightType, remainderType);
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x - x / y * y, LongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x - x / y * y, UnsignedLongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x - x / y * y, UnsignedLongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x - x / y * y, LongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else if (leftType == ByteTypeName || leftType == BoolTypeName || rightType == ByteTypeName || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(),
				(x, y) => x - x / y * y, leftType, rightType, remainderType);
		else
			return new();
	}

	private static NStarEntity StringMod(NStarEntity left, NStarEntity right, String leftType, String rightType)
	{
		var t = GetRemainderType(leftType, right, rightType);
		if (short.TryParse(left.ToString().ToString(), out var leftShortInt)
			&& short.TryParse(right.ToString().ToString(), out var rightShortInt))
			return PerformOperation(leftShortInt, rightShortInt, (x, y) => x - x / y * y, leftType, rightType, t);
		else if (ushort.TryParse(left.ToString().ToString(), out var leftUnsignedShortInt)
			&& ushort.TryParse(right.ToString().ToString(), out var rightUnsignedShortInt))
			return PerformOperation(leftUnsignedShortInt, rightUnsignedShortInt,
				(x, y) => x - x / y * y, leftType, rightType, t);
		else if (int.TryParse(left.ToString().ToString(), out var leftInt)
			&& int.TryParse(right.ToString().ToString(), out var rightInt))
			return PerformOperation(leftInt, rightInt, (x, y) => x - x / y * y, leftType, rightType, t);
		else if (uint.TryParse(left.ToString().ToString(), out var leftUnsignedInt)
			&& uint.TryParse(right.ToString().ToString(), out var rightUnsignedInt))
			return PerformOperation(leftUnsignedInt, rightUnsignedInt, (x, y) => x - x / y * y, leftType, rightType, t);
		else if (long.TryParse(left.ToString().ToString(), out var leftLongInt)
			&& long.TryParse(right.ToString().ToString(), out var rightLongInt))
			return PerformOperation(leftLongInt, rightLongInt,
				(x, y) => new(x - x / y * y, LongIntType), leftType, rightType, t);
		else if (ulong.TryParse(left.ToString().ToString(), out var leftUnsignedLongInt)
			&& ulong.TryParse(right.ToString().ToString(), out var rightUnsignedLongInt))
			return PerformOperation(leftUnsignedLongInt, rightUnsignedLongInt,
				(x, y) => new(x - x / y * y, UnsignedLongIntType), leftType, rightType, t);
		else if (MpuT.TryParse(left.ToString().ToString(), out var leftUnsignedLongLong)
			&& MpuT.TryParse(right.ToString().ToString(), out var rightUnsignedLongLong))
			return PerformOperation(leftUnsignedLongLong, rightUnsignedLongLong,
				(x, y) => new(x - x / y * y, UnsignedLongLongType), leftType, rightType, t);
		else if (MpzT.TryParse(left.ToString().ToString(), out var leftLongLong)
			&& MpzT.TryParse(right.ToString().ToString(), out var rightLongLong))
			return PerformOperation(leftLongLong, rightLongLong,
				(x, y) => new(x - x / y * y, LongLongType), leftType, rightType, t);
		else if (double.TryParse(left.ToString().ToString(), out var leftReal)
			&& double.TryParse(right.ToString().ToString(), out var rightReal))
			return PerformOperation(leftReal, rightReal, (x, y) => x - Truncate(x / y) * y, leftType, rightType, t);
		else
			return new();
	}

	public static NStarEntity operator &(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		var remainderType = GetRemainderType(leftType, right, rightType);
		string t;
		if (right.ToLongLong() == 0)
			return new();
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x & y, LongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x & y, UnsignedLongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x & y, UnsignedLongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x & y, LongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else if (leftType == ByteTypeName || leftType == BoolTypeName || rightType == ByteTypeName || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(),
				(x, y) => x & y, leftType, rightType, remainderType);
		else
			return new();
	}

	public static NStarEntity operator |(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		var remainderType = GetRemainderType(leftType, right, rightType);
		string t;
		if (right.ToLongLong() == 0)
			return new();
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x | y, LongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x | y, UnsignedLongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x | y, UnsignedLongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x | y, LongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else if (leftType == ByteTypeName || leftType == BoolTypeName || rightType == ByteTypeName || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(),
				(x, y) => x | y, leftType, rightType, remainderType);
		else
			return new();
	}

	public static NStarEntity operator ^(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.InnerType.MainType) || !TypeIsPrimitive(right.InnerType.MainType))
			return new();
		var leftType = left.InnerType.MainType.Peek().Name;
		var rightType = right.InnerType.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		var remainderType = GetRemainderType(leftType, right, rightType);
		string t;
		if (right.ToLongLong() == 0)
			return new();
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongLong(),
				(x, y) => new(x ^ y, LongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongLong(),
				(x, y) => new(x ^ y, UnsignedLongLongType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedLongInt(),
				(x, y) => new(x ^ y, UnsignedLongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToLongInt(),
				(x, y) => new(x ^ y, LongIntType), leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedInt(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else if (leftType == (t = IntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToInt(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else if (leftType == (t = CharTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToChar(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToUnsignedShortInt(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return PerformOperation(left, right, x => x.ToShortInt(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else if (leftType == ByteTypeName || leftType == BoolTypeName || rightType == ByteTypeName || rightType == BoolTypeName)
			return PerformOperation(left, right, x => x.ToByte(),
				(x, y) => x ^ y, leftType, rightType, remainderType);
		else
			return new();
	}

	public static NStarEntity operator >>>(NStarEntity left, int right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType))
		{
			var basicType = left.InnerType.MainType.Peek().Name;
			if (basicType == LongLongTypeName)
				return ValidateFixing(new(left.ToLongLong() >>> right, LongLongType), LongLongType, left.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(left.ToUnsignedLongLong() >>> right, UnsignedLongLongType),
					UnsignedLongLongType, left.Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(left.ToReal() * Pow(2, -right), RealType, left.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(left.ToUnsignedLongInt() >>> right, UnsignedLongIntType),
					UnsignedLongIntType, left.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(left.ToLongInt() >>> right, LongIntType), LongIntType, left.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(left.ToUnsignedInt() >>> right, UnsignedIntType, left.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(left.ToInt() >>> right, IntType, left.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(left.ToUnsignedShortInt() >>> right, UnsignedShortIntType, left.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(left.ToShortInt() >>> right, ShortIntType, left.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(left.ToByte() >>> right, ByteType, left.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static NStarEntity operator >>(NStarEntity left, int right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType))
		{
			var basicType = left.InnerType.MainType.Peek().Name;
			if (basicType == LongLongTypeName)
				return ValidateFixing(new(left.ToLongLong() >> right, LongLongType), LongLongType, left.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(left.ToUnsignedLongLong() >> right, UnsignedLongLongType),
					UnsignedLongLongType, left.Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(left.ToReal() * Pow(2, -right), RealType, left.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(left.ToUnsignedLongInt() >> right, UnsignedLongIntType),
					UnsignedLongIntType, left.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(left.ToLongInt() >> right, LongIntType), LongIntType, left.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(left.ToUnsignedInt() >> right, UnsignedIntType, left.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(left.ToInt() >> right, IntType, left.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(left.ToUnsignedShortInt() >> right, UnsignedShortIntType, left.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(left.ToShortInt() >> right, ShortIntType, left.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(left.ToByte() >> right, ByteType, left.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static NStarEntity operator <<(NStarEntity left, int right)
	{
		if (TypeIsPrimitive(left.InnerType.MainType))
		{
			var basicType = left.InnerType.MainType.Peek().Name;
			if (basicType == LongLongTypeName)
				return ValidateFixing(new(left.ToLongLong() << right, LongLongType), LongLongType, left.Fixed);
			else if (basicType == UnsignedLongLongTypeName)
				return ValidateFixing(new(left.ToUnsignedLongLong() << right, UnsignedLongLongType),
					UnsignedLongLongType, left.Fixed);
			else if (basicType == RealTypeName)
				return ValidateFixing(left.ToReal() * Pow(2, right), RealType, left.Fixed);
			else if (basicType == UnsignedLongIntTypeName)
				return ValidateFixing(new(left.ToUnsignedLongInt() << right, UnsignedLongIntType),
					UnsignedLongIntType, left.Fixed);
			else if (basicType == LongIntTypeName)
				return ValidateFixing(new(left.ToLongInt() << right, LongIntType), LongIntType, left.Fixed);
			else if (basicType == UnsignedIntTypeName)
				return ValidateFixing(left.ToUnsignedInt() << right, UnsignedIntType, left.Fixed);
			else if (basicType == IntTypeName)
				return ValidateFixing(left.ToInt() << right, IntType, left.Fixed);
			else if (basicType == UnsignedShortIntTypeName)
				return ValidateFixing(left.ToUnsignedShortInt() << right, UnsignedShortIntType, left.Fixed);
			else if (basicType == ShortIntTypeName)
				return ValidateFixing(left.ToShortInt() << right, ShortIntType, left.Fixed);
			else if (basicType == ByteTypeName)
				return ValidateFixing(left.ToByte() << right, ByteType, left.Fixed);
			else
				return new();
		}
		else
			return new();
	}

	public static bool operator ==(NStarEntity left, NStarEntity right) =>
		left.ToBool() == right.ToBool() && left.ToReal() == right.ToReal() && left.ToString() == right.ToString();

	public static bool operator !=(NStarEntity left, NStarEntity right) => !(left == right);
}
