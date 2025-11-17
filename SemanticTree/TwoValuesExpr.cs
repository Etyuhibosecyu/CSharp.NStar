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
		var result = Branch[i].Name.ToString() switch
		{
			"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" => TranslateTimeTernaryExpr(ref i),
			":" => TranslateTimeColonExpr(ref i, RightNStarType),
			"pow" => TranslateTimePowExpr(errors, i, otherPos, LeftNStarType, RightNStarType),
			"*" => TranslateTimeMulExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"/" => TranslateTimeDivExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"%" => TranslateTimeModExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"+" => TranslateTimePlusExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			"-" => TranslateTimeMinusExpr(ref errors, ref i, LeftNStarType, RightNStarType, PrevNStarType),
			">>>" => TranslateTimeUnsignedRightShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			">>" => TranslateTimeRightShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
			"<<" => TranslateTimeLeftShiftExpr(ref errors, ref i, LeftNStarType, RightNStarType),
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
		Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
		return result.Length == 0 ? Branch[i].Name : result;
	}

	private String TranslateTimeTernaryExpr(ref int i)
	{
		String result;
		var s = Branch[i].Name;
		if ((s == "?" ? Value1 : s == "?=" ? NStarEntity.Eq(Value1, Value2) : s == "?>" ? NStarEntity.Gt(Value1, Value2)
			: s == "?<" ? NStarEntity.Lt(Value1, Value2) : s == "?>=" ? NStarEntity.Goe(Value1, Value2)
			: s == "?<=" ? NStarEntity.Loe(Value1, Value2) : NStarEntity.Neq(Value1, Value2)).ToBool())
		{
			Branch[Max(i - 3, 0)] = new((s == "?" ? Value2 : Value1).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
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
				.Progression((x, y) => GetResultType(x, y, "default!", "default!"));
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
			if (TypeEqualsToPrimitive(LeftNStarType, "long long")
				&& (!TypesAreCompatible(RightNStarType, IntType, out var warning, Value2.ToString(true, true), out _, out _)
				|| warning))
			{
				GenerateMessage(ref errors, 0x4006, otherPos, Branch[i].Name, LeftNStarType, RightNStarType);
				Branch[i].Extra = NullType;
				return "default!";
			}
			Branch[Max(i - 3, 0)] = new(((NStarEntity)Pow(Value2.ToReal(), Value1.ToReal())).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		}
		catch
		{
			GenerateMessage(ref errors, 0x400D, otherPos);
			Branch[Max(i - 3, 0)] = new("null", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		return [];
	}

	private String TranslateTimeMulExpr(ref List<String>? errors, ref int i, NStarType LeftNStarType, NStarType RightNStarType,
		NStarType PrevNStarType)
	{
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, "string") && TypeEqualsToPrimitive(RightNStarType, "string"))
		{
			GenerateMessage(ref errors, 0x4008, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, "string") && Branch[i - 2].Name == "*")
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
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, "string") || TypeEqualsToPrimitive(RightNStarType, "string"))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(LeftNStarType, "real") && !TypeEqualsToPrimitive(RightNStarType, "real") && Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, "string") && Branch[i - 2].Name == "*")
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
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, "string") || TypeEqualsToPrimitive(RightNStarType, "string"))
		{
			GenerateMessage(ref errors, 0x4009, Branch[i].Pos);
			return Default;
		}
		if (!TypeEqualsToPrimitive(LeftNStarType, "real") && !TypeEqualsToPrimitive(RightNStarType, "real") && Value2 == 0)
		{
			GenerateMessage(ref errors, 0x4004, Branch[i].Pos);
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, "string") && Branch[i - 2].Name == "*")
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
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, "string");
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, "string");
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
			if (TypeEqualsToPrimitive(PrevNStarType, "string"))
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
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			GenerateMessage(ref errors, 0x4006, Branch[i].Pos, Branch[i].Name, LeftNStarType, RightNStarType);
			return Default;
		}
		String result = [];
		if (TypeEqualsToPrimitive(LeftNStarType, "string") || TypeEqualsToPrimitive(RightNStarType, "string"))
		{
			GenerateMessage(ref errors, 0x4007, Branch[i].Pos);
			return Default;
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevNStarType, "string") && Branch[i - 2].Name == "+")
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
			return "default!";
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >>> Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
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
			return "default!";
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >> Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
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
			return "default!";
		}
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 << Value2.ToInt()).ToString(true, true),
				Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, Value1.ToString(true), Value2.ToString(true));
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
		return Branch[Max(i - 3, 0)].Name.Copy().Add(' ').AddRange(Branch[i - 1].Name).Add(' ').AddRange(Branch[i++ - 1].Name);
	}

	private void GenerateMessage(ref List<String>? errors, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, Lexems[pos].LineN, Lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			throw new InvalidOperationException();
	}
}
