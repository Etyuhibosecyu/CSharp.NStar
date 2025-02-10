using System.Text;
using static CSharp.NStar.SemanticTree;

namespace CSharp.NStar;

internal record class ExprTwoValues(Universal Value1, Universal Value2, TreeBranch Branch, List<Lexem> Lexems)
{
	public String Calculate(ref List<String>? errorsList, ref int i)
	{
		var otherPos = Branch[i].Pos;
		if (Branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (Branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && Branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		var result = Branch[i].Info.ToString() switch
		{
			"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" => ExprTranslateTimeTernary(ref i),
			":" => ExprTranslateTimeColon(ref i, UnvType2),
			"pow" => ExprTranslateTimePow(errorsList, i, otherPos),
			"*" => ExprTranslateTimeMul(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"/" => ExprTranslateTimeDiv(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"%" => ExprTranslateTimeMod(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			"+" => ExprTranslateTimePlus(ref i, UnvType1, UnvType2, PrevUnvType),
			"-" => ExprTranslateTimeMinus(ref errorsList, ref i, UnvType1, UnvType2, PrevUnvType),
			">>" => ExprTranslateTimeRightShift(ref i, UnvType1, UnvType2),
			"<<" => ExprTranslateTimeLeftShift(ref i, UnvType1, UnvType2),
			"==" => ExprTranslateTimeSingular(i, Universal.Eq(Value1, Value2)),
			">" => ExprTranslateTimeSingular(i, Universal.Gt(Value1, Value2)),
			"<" => ExprTranslateTimeSingular(i, Universal.Lt(Value1, Value2)),
			">=" => ExprTranslateTimeSingular(i, Universal.Goe(Value1, Value2)),
			"<=" => ExprTranslateTimeSingular(i, Universal.Loe(Value1, Value2)),
			"!=" => ExprTranslateTimeSingular(i, Universal.Neq(Value1, Value2)),
			"&&" => ExprTranslateTimeSingular(i, Universal.And(Value1, Value2)),
			"||" => ExprTranslateTimeSingular(i, Universal.Or(Value1, Value2)),
			"^^" => ExprTranslateTimeSingular(i, Universal.Xor(Value1, Value2)),
			_ => ExprTranslateTimeDefault(ref i, UnvType1, UnvType2),
		};
		Branch.Remove(i - 1, 2);
		i = Max(i - 3, 0);
		Branch[i].Extra = GetResultType(UnvType1, UnvType2);
		return result.Length == 0 ? Branch[i].Info : result;
	}

	private String ExprTranslateTimeTernary(ref int i)
	{
		String result;
		var s = Branch[i].Info;
		if ((s == "?" ? Value1 : s == "?=" ? Universal.Eq(Value1, Value2) : s == "?>" ? Universal.Gt(Value1, Value2) : s == "?<" ? Universal.Lt(Value1, Value2) : s == "?>=" ? Universal.Goe(Value1, Value2) : s == "?<=" ? Universal.Loe(Value1, Value2) : Universal.Neq(Value1, Value2)).ToBool())
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

	private String ExprTranslateTimeColon(ref int i, UniversalType UnvType2)
	{
		String result;
		if (i + 2 >= Branch.Length)
		{
			var i2 = i;
			Branch[i].Extra = Branch.Elements.Filter((_, index) => index == i2 - 1 || index % 4 == 1).Convert(x => x.Extra is UniversalType ElemType ? ElemType : NullType).Progression(GetResultType);
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

	private String ExprTranslateTimePow(List<String>? errorsList, int i, int otherPos)
	{
		try
		{
			Branch[Max(i - 3, 0)] = new(((Universal)Math.Pow(Value2.ToReal(), Value1.ToReal())).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		}
		catch
		{
			Add(ref errorsList, "Error in line " + Lexems[otherPos].LineN.ToString() + " at position " + Lexems[otherPos].Pos.ToString() + ": cannot compute this expression");
			Branch[Max(i - 3, 0)] = new("null", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		return [];
	}

	private String ExprTranslateTimeMul(ref List<String>? errorsList, ref int i, UniversalType UnvType1, UniversalType UnvType2, UniversalType PrevUnvType)
	{
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") && TypeEqualsToPrimitive(UnvType2, "string"))
			Add(ref errorsList, "Warning in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": the string cannot be multiplied by string; one of them can be converted to number but this is not recommended and can cause data loss");
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 * Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimeDiv(ref List<String>? errorsList, ref int i, UniversalType UnvType1, UniversalType UnvType2, UniversalType PrevUnvType)
	{
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
			Add(ref errorsList, "Warning in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss");
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			Add(ref errorsList, "Error in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": division by integer zero is forbidden");
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 / Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimeMod(ref List<String>? errorsList, ref int i, UniversalType UnvType1, UniversalType UnvType2, UniversalType PrevUnvType)
	{
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
			Add(ref errorsList, "Warning in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss");
		if (!TypeEqualsToPrimitive(UnvType1, "real") && !TypeEqualsToPrimitive(UnvType2, "real") && Value2 == 0)
		{
			Add(ref errorsList, "Error in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": division by integer zero is forbidden");
			Branch[Max(i - 3, 0)] = new("default!", Branch.Pos, Branch.EndPos, Branch.Container);
		}
		else if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "*")
			Branch[Max(i - 3, 0)] = new((Value1 % Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimePlus(ref int i, UniversalType UnvType1, UniversalType UnvType2, UniversalType PrevUnvType)
	{
		String result = [];
		bool isString1 = TypeEqualsToPrimitive(UnvType1, "string"), isString2 = TypeEqualsToPrimitive(UnvType2, "string");
		if (i == 2)
		{
			Branch[Max(i - 3, 0)] = new((Value1 + Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
			return result;
		}
		Branch[i].Extra = GetResultType(UnvType1, UnvType2);
		if (isString1 && isString2)
		{
			i++;
			var result2 = Value1.ToString(true, true).Copy();
			if (TypeEqualsToPrimitive(PrevUnvType, "string"))
				result2.AddRange((String)".Copy()");
			result = result2.AddRange(".AddRange(").AddRange(Value2.ToString(true, true)).Add(')');
		}
		else if (isString1 || isString2)
			result = ((String)"((").AddRange(nameof(Universal)).Add(')').AddRange(Value1.ToString(true, true)).Add(' ').AddRange(Branch[i++].Info).Add(' ').AddRange(Value2.ToString(true, true)).AddRange(").ToString()");
		else
			result = i < 2 ? Branch[i][^1].Info : Value1.ToString(true, true).Copy().Add(' ').AddRange(Branch[i++].Info).Add(' ').AddRange(Value2.ToString(true, true));
		return result;
	}

	private String ExprTranslateTimeMinus(ref List<String>? errorsList, ref int i, UniversalType UnvType1, UniversalType UnvType2, UniversalType PrevUnvType)
	{
		String result = [];
		if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
			Add(ref errorsList, "Warning in line " + Lexems[Branch[i].Pos].LineN.ToString() + " at position " + Lexems[Branch[i].Pos].Pos.ToString() + ": the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss");
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && Branch[i - 2].Info == "+")
			Branch[Max(i - 3, 0)] = new((Value1 - Value2).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimeRightShift(ref int i, UniversalType UnvType1, UniversalType UnvType2)
	{
		String result = [];
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 >> Value2.ToInt()).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimeLeftShift(ref int i, UniversalType UnvType1, UniversalType UnvType2)
	{
		String result = [];
		if (i == 2)
			Branch[Max(i - 3, 0)] = new((Value1 << Value2.ToInt()).ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container);
		else
		{
			Branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			result = new String(Branch[i - 2].Info).Add(' ').AddRange(Branch[i].Info).Add(' ').AddRange(Branch[i - 1].Info);
		}
		return result;
	}

	private String ExprTranslateTimeSingular(int i, Universal resultValue) => (Branch[Max(i - 3, 0)] = new(resultValue.ToString(true, true), Branch.Pos, Branch.EndPos, Branch.Container)).Info;

	private String ExprTranslateTimeDefault(ref int i, UniversalType UnvType1, UniversalType UnvType2)
	{
		Branch[i].Extra = GetResultType(UnvType1, UnvType2);
		return new String(Branch[Max(i - 3, 0)].Info).Add(' ').AddRange(Branch[i - 1].Info).Add(' ').AddRange(Branch[i++ - 1].Info);
	}
}
