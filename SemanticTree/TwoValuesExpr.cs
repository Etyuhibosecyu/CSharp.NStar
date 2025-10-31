using System.Text;

namespace CSharp.NStar;

internal record class TwoValuesExpr(NStarEntity Value1, NStarEntity Value2, TreeBranch Branch, List<Lexem> Lexems, String Default)
{
	public String Calculate(ref List<String>? errors, ref int i)
	{
		var otherPos = Branch[i].Pos;
		if (Branch[i - 2].Extra is not NStarType UnvType1)
			UnvType1 = NullType;
		if (Branch[i - 1].Extra is not NStarType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && Branch[i - 4].Extra is NStarType PrevUnvType))
			PrevUnvType = NullType;
		var result = Branch[i].Name.ToString() switch
		{
			"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" => TranslateTimeTernaryExpr(ref i),
			":" => TranslateTimeColonExpr(ref i, UnvType2),
			"pow" => TranslateTimePowExpr(errors, i, otherPos),
			"*" => TranslateTimeMulExpr(ref errors, ref i, UnvType1, UnvType2, PrevUnvType),
			"/" => TranslateTimeDivExpr(ref errors, ref i, UnvType1, UnvType2, PrevUnvType),
			"%" => TranslateTimeModExpr(ref errors, ref i, UnvType1, UnvType2, PrevUnvType),
			"+" => TranslateTimePlusExpr(ref errors, ref i, UnvType1, UnvType2, PrevUnvType),
			"-" => TranslateTimeMinusExpr(ref errors, ref i, UnvType1, UnvType2, PrevUnvType),
			">>" => TranslateTimeRightShiftExpr(ref i, UnvType1, UnvType2),
			"<<" => TranslateTimeLeftShiftExpr(ref i, UnvType1, UnvType2),
			"==" => TranslateTimeSingularExpr(i, NStarEntity.Eq(Value1, Value2)),
			">" => TranslateTimeSingularExpr(i, NStarEntity.Gt(Value1, Value2)),
			"<" => TranslateTimeSingularExpr(i, NStarEntity.Lt(Value1, Value2)),
			">=" => TranslateTimeSingularExpr(i, NStarEntity.Goe(Value1, Value2)),
			"<=" => TranslateTimeSingularExpr(i, NStarEntity.Loe(Value1, Value2)),
			"!=" => TranslateTimeSingularExpr(i, NStarEntity.Neq(Value1, Value2)),
			"&&" => TranslateTimeSingularExpr(i, NStarEntity.And(Value1, Value2)),
			"||" => TranslateTimeSingularExpr(i, NStarEntity.Or(Value1, Value2)),
			"^^" => TranslateTimeSingularExpr(i, NStarEntity.Xor(Value1, Value2)),
			_ => TranslateTimeDefaultExpr(ref i, UnvType1, UnvType2),
		};
		Branch.Remove(Min(i - 1, Branch.Length - 2), 2);
		i = Max(i - 3, 0);
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		return result.Length == 0 ? Branch[i].Name : result;
	}

	private String TranslateTimeTernaryExpr(ref int i)
	{
		String result;
		var s = Branch[i].Name;
		if ((s == "?" ? Value1 : s == "?=" ? NStarEntity.Eq(Value1, Value2) : s == "?>" ? NStarEntity.Gt(Value1, Value2) : s == "?<" ? NStarEntity.Lt(Value1, Value2) : s == "?>=" ? NStarEntity.Goe(Value1, Value2) : s == "?<=" ? NStarEntity.Loe(Value1, Value2) : NStarEntity.Neq(Value1, Value2)).ToBool())
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
		result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
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
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		else
		{
			Branch[i].Extra = UnvType2;
			i++;
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimePowExpr(List<String>? errors, int i, int otherPos)
	{
		try
		{
			Branch[Max(i - 3, 0)] = new(((NStarEntity)Pow(Value2.ToReal(), Value1.ToReal())).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		}
		catch
		{
			GenerateMessage(ref errors, 0x400D, otherPos);
			Branch[Max(i - 3, 0)] = new("null", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		return [];
	}

	private String TranslateTimeMulExpr(ref List<String>? errors, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
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
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") && TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errors, 0x4008, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeDivExpr(ref List<String>? errors, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
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
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeModExpr(ref List<String>? errors, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
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
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Name == "*")
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimePlusExpr(ref List<String>? errors, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
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
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		bool isStringLeft = TypeEqualsToPrimitive(UnvType1, "string"), isStringRight = TypeEqualsToPrimitive(UnvType2, "string");
		if (i == 2)
		{
			Branch[Max(i - 3, 0)] = new((Value1 + Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
			return result;
		}
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		if (isStringLeft && isStringRight)
		{
			i++;
			var innerResult = Value1.ToString(true, true).Copy();
			if (TypeEqualsToPrimitive(PrevUnvType, "string"))
				innerResult.AddRange((String)".Copy()");
			result = innerResult.AddRange(".AddRange(").AddRange(Value2.ToString(true, true)).Add(')');
		}
		else if (isStringLeft || isStringRight)
			result = ((String)"((").AddRange(nameof(NStarEntity)).Add(')').AddRange(Value1.ToString(true, true)).Add(' ').AddRange(Branch[i++].Name).Add(' ').AddRange(Value2.ToString(true, true)).AddRange(").ToString()");
		else
			result = i < 2 ? Branch[i][^1].Name : Value1.ToString(true, true).Copy().Add(' ').AddRange(Branch[i++].Name).Add(' ').AddRange(Value2.ToString(true, true));
		return result;
	}

	private String TranslateTimeMinusExpr(ref List<String>? errors, ref int i, NStarType UnvType1, NStarType UnvType2, NStarType PrevUnvType)
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
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, UnvType1.ToString(), UnvType2.ToString());
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
		{
			GenerateMessage(ref errors, 0x4007, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Name == "+")
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
			i++;
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
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
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
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
			result = new String(Branch[i - 2].Name).Add(' ').AddRange(Branch[i].Name).Add(' ').AddRange(Branch[i - 1].Name);
		}
		return result;
	}

	private String TranslateTimeSingularExpr(int i, NStarEntity resultValue) => (Branch[Max(i - 3, 0)] = new(resultValue.ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container)).Name;

	private String TranslateTimeDefaultExpr(ref int i, NStarType UnvType1, NStarType UnvType2)
	{
		Branch[i].Extra = GetResultType(UnvType1, UnvType2, Value1.ToString(true), Value2.ToString(true));
		return new String(Branch[Max(i - 3, 0)].Name).Add(' ').AddRange(Branch[i - 1].Name).Add(' ').AddRange(Branch[i++ - 1].Name);
	}

	private void GenerateMessage(ref List<String>? errors, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, Lexems[pos].LineN, Lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			throw new InvalidOperationException();
	}
}
