using System.Text;

namespace CSharp.NStar;

internal record class TwoValuesExpr(NStarObject Value1, NStarObject Value2, TreeBranch Branch, List<Lexem> Lexems, String Default)
{
	public String Calculate(ref List<String>? errorsList, ref int i)
	{
		var otherPos = Branch[i].Pos;
		if (Branch[i - 2].Extra is not NStarType UnvType1)
			UnvType1 = NullType;
		if (Branch[i - 1].Extra is not NStarType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && Branch[i - 4].Extra is NStarType PrevUnvType))
			PrevUnvType = NullType;
		var result = Branch[i].Info.ToString() switch
		{
			"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" => TranslateTimeTernaryExpr(ref i),
			":" => TranslateTimeColonExpr(ref i, UnvType2),
			"pow" => TranslateTimePowExpr(errorsList, i, otherPos),
			"*" => TranslateTimeMulExpr(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"/" => TranslateTimeDivExpr(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"%" => TranslateTimeModExpr(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"+" => TranslateTimePlusExpr(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"-" => TranslateTimeMinusExpr(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			">>" => TranslateTimeRightShiftExpr(ref i, UnvType1, UnvType2),
			"<<" => TranslateTimeLeftShiftExpr(ref i, UnvType1, UnvType2),
			"==" => TranslateTimeSingularExpr(i, NStarObject.Eq(Value1, Value2)),
			">" => TranslateTimeSingularExpr(i, NStarObject.Gt(Value1, Value2)),
			"<" => TranslateTimeSingularExpr(i, NStarObject.Lt(Value1, Value2)),
			">=" => TranslateTimeSingularExpr(i, NStarObject.Goe(Value1, Value2)),
			"<=" => TranslateTimeSingularExpr(i, NStarObject.Loe(Value1, Value2)),
			"!=" => TranslateTimeSingularExpr(i, NStarObject.Neq(Value1, Value2)),
			"&&" => TranslateTimeSingularExpr(i, NStarObject.And(Value1, Value2)),
			"||" => TranslateTimeSingularExpr(i, NStarObject.Or(Value1, Value2)),
			"^^" => TranslateTimeSingularExpr(i, NStarObject.Xor(Value1, Value2)),
			_ => TranslateTimeDefaultExpr(ref i, UnvType1, UnvType2),
		};
		Branch.Remove(Min(i - 1, Branch.Length - 2), 2);
		i = Max(i - 3, 0);
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		return result.Length == 0 ? Branch[i].Info : result;
	}

	private String TranslateTimeTernaryExpr(ref int i)
	{
		String result;
		var s = Branch[i].Info;
		if ((s == "?" ? Value1 : s == "?=" ? NStarObject.Eq(Value1, Value2) : s == "?>" ? NStarObject.Gt(Value1, Value2) : s == "?<" ? NStarObject.Lt(Value1, Value2) : s == "?>=" ? NStarObject.Goe(Value1, Value2) : s == "?<=" ? NStarObject.Loe(Value1, Value2) : NStarObject.Neq(Value1, Value2)).ToBool())
		{
			Branch[Max(i - 3, 0)] = new((s == "?" ? Value2 : Value1).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
			Branch.RemoveEnd(i - 1);
		}
		else if (i + 2 >= Branch.Length)
		{
			Branch[Max(i - 3, 0)] = new("null", Branch.Pos, Branch.EndPos, Branch.Container) { Extra = NullType };
			Branch.RemoveEnd(i - 1);
		}
		else
		{
			Branch[Max(i - 3, 0)] = Branch[i + 1];
			Branch.Remove(i - 1, 4);
		}
		i--;
		result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		return result;
	}

	private String TranslateTimeColonExpr(ref int i, NStarType UnvType2)
	{
		String result;
		if (i + 2 >= Branch.Length)
		{
			var i2 = i;
			Branch[i].Extra = Branch.Elements.Filter((_, index) => index == i2 - 1 || index % 4 == 1).Convert(x => x.Extra is NStarType ElemType ? ElemType : NullType).Progression((x, y) => GetResultType(x, y, "default!", "default!"));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		else
		{
			Branch[i].Extra = UnvType2;
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimePowExpr(List<String>? errorsList, int i, int otherPos)
	{
		try
		{
			Branch[Max(i - 3, 0)] = new(((NStarObject)Pow(Value2.ToReal(), Value1.ToReal())).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		}
		catch
		{
			GenerateMessage(ref errorsList, 0x400D, otherPos);
			Branch[Max(i - 3, 0)] = new("null", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		return [];
	}

	private String TranslateTimeMulExpr(ref List<String>? errorsList, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
	{
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errorsList, 0x4006, Branch[i].Pos, Branch[i].Info, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") && TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errorsList, 0x4008, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimeDivExpr(ref List<String>? errorsList, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
	{
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errorsList, 0x4006, Branch[i].Pos, Branch[i].Info, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errorsList, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			GenerateMessage(ref errorsList, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimeModExpr(ref List<String>? errorsList, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
	{
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errorsList, 0x4006, Branch[i].Pos, Branch[i].Info, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errorsList, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			GenerateMessage(ref errorsList, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimePlusExpr(ref List<String>? errorsList, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
	{
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errorsList, 0x4006, Branch[i].Pos, Branch[i].Info, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		bool isString1 = TypeEqualsToPrimitive(UnvType1, "string"), isString2 = TypeEqualsToPrimitive(UnvType2, "string");
		if (i == 2)
		{
			Branch[Max(i - 3, 0)] = new((Value1 + Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
			return result;
		}
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		if (isString1 && isString2)
		{
			i++;
			var result2 = Value1.ToString(true, true).Copy();
			if (TypeEqualsToPrimitive(PrevUnvType, "string"))
				result2.AddRange((String)".Copy()");
			result = result2.AddRange(".AddRange(").AddRange(Value2.ToString(true, true)).Add(')');
		}
		else if (isString1 || isString2)
			result = ((String)"((").AddRange(nameof(NStarObject)).Add(')').AddRange(Value1.ToString(true, true)).Add(' ').AddRange(Branch[i++].Info).Add(' ').AddRange(Value2.ToString(true, true)).AddRange(").ToString()");
		else
			result = i < 2 ? Branch[i][^1].Info : Value1.ToString(true, true).Copy().Add(' ').AddRange(Branch[i++].Info).Add(' ').AddRange(Value2.ToString(true, true));
		return result;
	}

	private String TranslateTimeMinusExpr(ref List<String>? errorsList, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
	{
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errorsList, 0x4006, Branch[i].Pos, Branch[i].Info, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errorsList, 0x4007, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "+")
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimeRightShiftExpr(ref int i, NStarType UnvType1, NStarType UnvType2)
	{
		String result = [];
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >> Value2.ToInt()).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimeLeftShiftExpr(ref int i, NStarType UnvType1, NStarType UnvType2)
	{
		String result = [];
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 << Value2.ToInt()).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String TranslateTimeSingularExpr(int i, NStarObject resultValue) => (Branch[Max(i - 3, 0)] = new(resultValue.ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container)).Info;

	private String TranslateTimeDefaultExpr(ref int i, NStarType UnvType1, NStarType UnvType2)
	{
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		return new String(Branch[Max(i - 3, 0)].Info).Add(' ').AddRange(Branch[i - 1].Info).Add(' ').AddRange(Branch[i++ - 1].Info);
	}

	private void GenerateMessage(ref List<String>? errorsList, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errorsList, code, Lexems[pos].LineN, Lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			throw new InvalidOperationException();
	}
}
