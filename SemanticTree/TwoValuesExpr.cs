using System.Text;

namespace CSharp.NStar;

internal record class TwoValuesExpr(NStarEntity Value1, NStarEntity Value2, TreeBranch Branch,
	List<Lexem> Lexems, String Default)
{
	public String Calculate(ref List<String>? errors, ref int i)
	{
		var otherPos = Branch[i].Pos;
		if (Branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (Branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (!(i >= 4 && Branch[i - 4].Extra is NStarType PrevNStarType))
			PrevNStarType = NullType;
		var op = Branch[i].Name.ToString();
		var result = op switch
		{
			"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" => TranslateTimeTernaryExpr(ref i),
			":" => TranslateTimeColonExpr(ref i, RightNStarType),
			"pow" => TranslateTimePowExpr(errors, i, otherPos, LeftNStarType, RightNStarType),
			"*" => TranslateTimeMulExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"/" => TranslateTimeDivExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"%" => TranslateTimeModExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"+" => TranslateTimePlusExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"-" => TranslateTimeMinusExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"<<<" => TranslateTimeCyclicShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			">>>" => TranslateTimeUnsignedRightShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			"<<" => TranslateTimeLeftShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			">>" => TranslateTimeRightShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			"==" => TranslateTimeSingularExpr(i, NStarEntity.Eq(Value1, Value2)),
			">" => TranslateTimeSingularExpr(i, NStarEntity.Gt(Value1, Value2)),
			"<" => TranslateTimeSingularExpr(i, NStarEntity.Lt(Value1, Value2)),
			">=" => TranslateTimeSingularExpr(i, NStarEntity.Goe(Value1, Value2)),
			"<=" => TranslateTimeSingularExpr(i, NStarEntity.Loe(Value1, Value2)),
			"!=" => TranslateTimeSingularExpr(i, NStarEntity.Neq(Value1, Value2)),
			"&&" => TranslateTimeSingularExpr(i, NStarEntity.And(Value1, Value2)),
			"||" => TranslateTimeSingularExpr(i, NStarEntity.Or(Value1, Value2)),
			"^^" => TranslateTimeSingularExpr(i, NStarEntity.Xor(Value1, Value2)),
			_ => TranslateTimeDefaultExpr(ref i, LeftNStarType, RightNStarType),
		};
		Branch.Remove(Min(i - 1, Branch.Length - 2), 2);
		i = Max(i - 3, 0);
		Branch[i].Extra = op is "<<<" or ">>>" or "<<" or ">>" ? LeftNStarType
			: GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
		return result.Length == 0 ? Branch[i].Name : result;
	}

	private String TranslateTimeTernaryExpr(ref int i)
	{
		String result;
		var s = Branch[i].Name;
		var conditionValue = s.AsSpan() switch
		{
			"?" => Value1,
			"?=" => NStarEntity.Eq(Value1, Value2),
			"?>" => NStarEntity.Gt(Value1, Value2),
			"?<" => NStarEntity.Lt(Value1, Value2),
			"?>=" => NStarEntity.Goe(Value1, Value2),
			"?<=" => NStarEntity.Loe(Value1, Value2),
			_ => NStarEntity.Neq(Value1, Value2)
		};
		if (conditionValue.ToBool())
		{
			Branch[Max(i - 3, 0)] = new((s == "?" ? Value2 : Value1).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
			Branch.RemoveEnd(i - 1);
		}
		else if (i + 2 >= Branch.Length)
		{
			Branch[Max(i - 3, 0)] = new(NullString, Branch.Pos, Branch.EndPos, Branch.Container) { Extra = NullType };
			Branch.RemoveEnd(i - 1);
		}
		else
		{
			Branch[Max(i - 3, 0)] = Branch[i + 1];
			Branch.Remove(i - 1, 4);
		}
		i--;
		result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		return result;
	}

	private String TranslateTimeColonExpr(ref int i, NStarType RightNStarType)
	{
		String result;
		if (i + 2 >= Branch.Length)
		{
			var i2 = i;
			Branch[i].Extra = Branch.Elements.Filter((_, index) => index == i2 - 1 || index % 4 == 1)
				.Convert(x => x.Extra is NStarType ElemType ? ElemType : NullType)
				.Progression((x, y) => GetResultType(x, y, DefaultNull, DefaultNull));
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		else
		{
			Branch[i].Extra = RightNStarType;
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimePowExpr(List<String>? errors, int i, int otherPos,
		NStarType LeftNStarType, NStarType RightNStarType)
	{
		try
		{
			if (TypeEqualsToPrimitive(LeftNStarType, LongLongTypeName)
				&& (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _)
				|| warning))
			{
				GenerateMessage(ref errors, 0x4006, otherPos, Branch[i].Name, LeftNStarType, RightNStarType);
				Branch[i].Extra = NullType;
				return DefaultNull;
			}
			Branch[Max(i - 3, 0)] = new(((NStarEntity)Pow(Value2.ToReal(), Value1.ToReal())).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		}
		catch
		{
			GenerateMessage(ref errors, 0x400D, otherPos);
			Branch[Max(i - 3, 0)] = new(NullString, Branch.Pos, Branch.EndPos, Branch.Container);
		}
		return [];
	}

	private String TranslateTimeMulExpr(ref List<String>? errors, ref int i, NStarType LeftNStarType, NStarType RightNStarType,
		NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is NullString
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is NullString
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, StringTypeName) && TypeEqualsToPrimitive(RightNStarType, StringTypeName))
		{
			GenerateMessage(ref errors, 0x4008, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, StringTypeName) && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeDivExpr(ref List<String>? errors, ref int i, NStarType LeftNStarType, NStarType RightNStarType,
		NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is NullString
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is ByteTypeName
			or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, StringTypeName) || TypeEqualsToPrimitive(RightNStarType, StringTypeName))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(LeftNStarType, RealTypeName) && !TypeEqualsToPrimitive(RightNStarType, RealTypeName)
			&& !TypeEqualsToPrimitive(LeftNStarType, DecimalTypeName) && !TypeEqualsToPrimitive(RightNStarType, DecimalTypeName)
			&& Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new(DefaultNull, Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, StringTypeName) && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeModExpr(ref List<String>? errors, ref int i, NStarType LeftNStarType, NStarType RightNStarType,
		NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is NullString
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is ByteTypeName
			or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, StringTypeName) || TypeEqualsToPrimitive(RightNStarType, StringTypeName))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(LeftNStarType, RealTypeName) && !TypeEqualsToPrimitive(RightNStarType, RealTypeName)
			&& !TypeEqualsToPrimitive(LeftNStarType, DecimalTypeName) && !TypeEqualsToPrimitive(RightNStarType, DecimalTypeName)
			&& Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new(DefaultNull, Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, StringTypeName) && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimePlusExpr(ref List<String>? errors, ref int i, NStarType LeftNStarType, NStarType RightNStarType,
		NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, StringTypeName);
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, StringTypeName);
		if (i == 2)
		{
			Branch[Max(i - 3, 0)] = new((Value1 + Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
			return result;
		}
		Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
		if (isStringLeft && isStringRight)
		{
			i++;
			var innerResult = Value1.ToString(true, true).Copy();
			if (TypeEqualsToPrimitive(PrevNStarType, StringTypeName))
				innerResult.AddRange((String)".Copy()");
			result = innerResult.AddRange(".AddRange(").AddRange(Value2.ToString(true, true)).Add(')');
		}
		else if (isStringLeft || isStringRight)
		{
			result = ((String)"((").AddRange(nameof(NStarEntity)).Add(')').AddRange(Value1.ToString(true, true)).Add(' ');
			result.AddRange(Branch[i++].Name).Add(' ').AddRange(Value2.ToString(true, true)).AddRange(").ToString()");
		}
		else
		{
			result = i < 2 ? Branch[i][^1].Name : Value1.ToString(true, true).Copy().Add(' ');
			result.AddRange(Branch[i++].Name).Add(' ').AddRange(Value2.ToString(true, true));
		}
		return result;
	}

	private String TranslateTimeMinusExpr(ref List<String>? errors, ref int i,
		NStarType LeftNStarType, NStarType RightNStarType, NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			or ByteTypeName or "short char" or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or "long char" or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or UnsignedLongLongTypeName
			or RealTypeName or DecimalTypeName or LongRealTypeName or ComplexTypeName or LongComplexTypeName or StringTypeName))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, StringTypeName) || TypeEqualsToPrimitive(RightNStarType, StringTypeName))
		{
			GenerateMessage(ref errors, 0x4007, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, StringTypeName) && Branch[i - 2].Name == "+")
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeUnsignedRightShiftExpr(ref List<String>? errors, ref int i,
		NStarType LeftNStarType, NStarType RightNStarType)
	{
		String result = [];
		if (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _) || warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		else if (!TypesAreCompatible(LeftNStarType, LongLongType, out warning, Value1.ToString(true, true), out _, out _)
			|| warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4083, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >>> Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = LeftNStarType;
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeCyclicShiftExpr(ref List<String>? errors, ref int i,
		NStarType LeftNStarType, NStarType RightNStarType)
	{
		String result = [];
		if (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _) || warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		else if (!TypesAreCompatible(LeftNStarType, LongLongType, out warning, Value1.ToString(true, true), out _, out _)
			|| warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4083, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new(Value1.CyclicShift(Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = LeftNStarType;
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeRightShiftExpr(ref List<String>? errors, ref int i,
		NStarType LeftNStarType, NStarType RightNStarType)
	{
		String result = [];
		if (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _) || warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >> Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = LeftNStarType;
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeLeftShiftExpr(ref List<String>? errors, ref int i,
		NStarType LeftNStarType, NStarType RightNStarType)
	{
		String result = [];
		if (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _) || warning)
		{
			var otherPos = Branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, Branch[i].Name);
			Branch[i].Extra = NullType;
			return DefaultNull;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 << Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = LeftNStarType;
			i++;
			result = Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeSingularExpr(int i, NStarEntity resultValue) =>
		(Branch[Max(i - 3, 0)] = new(resultValue.ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container)).Name;

	private String TranslateTimeDefaultExpr(ref int i, NStarType LeftNStarType, NStarType RightNStarType)
	{
		Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
		return Branch[i - 2].Name.Copy().Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
	}

	private void GenerateMessage(ref List<String>? errors, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, Lexems[pos].LineN, Lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			throw new InvalidOperationException();
	}
}
