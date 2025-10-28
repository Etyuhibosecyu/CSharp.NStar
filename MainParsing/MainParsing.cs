using System.Text.RegularExpressions;

namespace CSharp.NStar;

public partial class MainParsing : LexemStream
{
	private int prevPos, start, end;
	private String task = [], prevTask = [];
	private TreeBranch? treeBranch;
	private object? extra;
	private BlockStack container = new();
	private bool success, prevSuccess;
	private int blocksToJumpPos, registeredTypesPos, parameterListsPos;
	private readonly ParameterValues parameterValues = [];
	private readonly NList<int> _PosStack = new(16, 0);
	private readonly NList<int> _StartStack = new(16, 0);
	private readonly NList<int> _EndStack = new(16, -1);
	private readonly List<String> _TaskStack = new(16, nameof(Main));
	private readonly List<List<String>> _ErLStack = new(16, [], []);
	private readonly List<TreeBranch?> _TBStack = new(16, null, null);
	private readonly List<object?> _ExtraStack = new(16) { null };
	private readonly List<BlockStack> _ContainerStack = new(16) { new() };
	private readonly BitList _SuccessStack = new(16) { false, false };
	private readonly NList<int> _BTJPStack = new(16, 0), _RTPStack = new(16, 0), _PLPStack = new(16, 0);
	private int typeDepth;
	private readonly List<String> collectionTypes = new(16) { "" };
	private readonly List<GeneralArrayParameters> typeChainTemplate = new(16);
	private readonly NList<int> tpos = new(16);
	private int globalUnnamedIndex = 1;
	private int _Stackpos;

	private static readonly Dictionary<String, (String Next, String TreeLabel, List<String> Operators)> operatorsMapping = new()
	{
		{ "Members", ("Member", "Members", []) }, { "Expr", ("List", "Expr", []) }, { "List", ("LambdaExpr", "List", []) },
		{ "LambdaExpr", ("AssignedExpr", "Expr", []) }, { "AssignedExpr", ("QuestionExpr", "Expr", []) },
		{ "QuestionExpr", ("XorExpr", "Expr", []) }, { "XorExpr", ("OrExpr", "xorList", []) },
		{ "OrExpr", ("AndExpr", "Expr", new("or")) }, { "AndExpr", ("Xor2Expr", "Expr", new("and")) },
		{ "Xor2Expr", ("Or2Expr", "Expr", new("^^")) }, { "Or2Expr", ("And2Expr", "Expr", new("||")) },
		{ "And2Expr", ("EquatedExpr", "Expr", new("&&")) }, { "EquatedExpr", ("ComparedExpr", "Expr", []) },
		{ "ComparedExpr", ("BitwiseXorExpr", "Expr", new(">", "<", ">=", "<=")) },
		{ "BitwiseXorExpr", ("BitwiseOrExpr", "Expr", new("^")) },
		{ "BitwiseOrExpr", ("BitwiseAndExpr", "Expr", new("|")) },
		{ "BitwiseAndExpr", ("BitwiseShiftExpr", "Expr", new("&")) },
		{ "BitwiseShiftExpr", ("PMExpr", "Expr", new(">>", "<<")) }, { "PMExpr", ("MulDivExpr", "Expr", new("+", "-")) },
		{ "MulDivExpr", ("PowExpr", "Expr", new("*", "/", "%")) }, { "PowExpr", ("TetraExpr", "Expr", new("pow")) },
		{ "TetraExpr", ("RangeExpr", "Expr", []) }, { "RangeExpr", (nameof(UnaryExpr), "Expr", []) }, { "PrefixExpr", ("PostfixExpr", "Expr", []) }
	};
	private static readonly G.Dictionary<String, dynamic> attributesMapping = new()
	{
		{ "ref", ParameterAttributes.Ref }, { "out", ParameterAttributes.Out },
		{ "params", ParameterAttributes.Params }, { "closed", PropertyAttributes.Closed },
		{ "protected", PropertyAttributes.Protected }, { "internal", PropertyAttributes.Internal },
		{ "public", PropertyAttributes.None }
	};

	public MainParsing(LexemStream lexemStream, bool wreckOccurred) : base(lexemStream)
	{
		this.wreckOccurred = wreckOccurred;
		_ErLStack[0] = errorsList ?? [];
		_EndStack[0] = lexems.Length;
	}

	internal (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) MainParse()
	{
		try
		{
			static void RemoveLast<T>(IList<T> list) => list.RemoveAt(list.Length - 1);
			while (_Stackpos >= 0)
			{
				pos = _PosStack[_Stackpos];
				start = _StartStack[_Stackpos];
				end = _EndStack[_Stackpos];
				task = _TaskStack[_Stackpos];
				errorsList = _ErLStack[_Stackpos + 1];
				treeBranch = _TBStack[_Stackpos + 1];
				extra = _ExtraStack[_Stackpos];
				container = _ContainerStack[_Stackpos];
				success = _SuccessStack[_Stackpos + 1];
				blocksToJumpPos = _BTJPStack[_Stackpos];
				registeredTypesPos = _RTPStack[_Stackpos];
				parameterListsPos = _PLPStack[_Stackpos];
				if (MainParseAction())
					continue;
				if (_Stackpos >= 1 && _SuccessStack[_Stackpos])
				{
					_PosStack[_Stackpos - 1] = pos;
					_BTJPStack[_Stackpos - 1] = blocksToJumpPos;
					_RTPStack[_Stackpos - 1] = registeredTypesPos;
					_PLPStack[_Stackpos - 1] = parameterListsPos;
				}
				prevPos = pos;
				prevTask = task;
				prevSuccess = success;
				RemoveLast(_PosStack);
				RemoveLast(_StartStack);
				RemoveLast(_EndStack);
				RemoveLast(_TaskStack);
				RemoveLast(_ErLStack);
				RemoveLast(_TBStack);
				RemoveLast(_ExtraStack);
				RemoveLast(_ContainerStack);
				RemoveLast(_SuccessStack);
				RemoveLast(_BTJPStack);
				RemoveLast(_RTPStack);
				RemoveLast(_PLPStack);
				_Stackpos--;
			}
			(errorsList ??= []).AddRange(_ErLStack[0] ?? []);
			if (_SuccessStack[0])
				return (lexems, input, _TBStack[0] ?? new(nameof(Main), 0, []), errorsList, wreckOccurred);
			else
			{
				(errorsList ??= []).Add(GetWreckPosPrefix(0xF001, ^1) + ": main parsing failed because of internal error");
				return RaiseWreck();
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			var pos2 = _Stackpos >= 0 ? _PosStack[_Stackpos] : lexems.Length - 1;
			var pos3 = pos2 >= lexems.Length ? pos2 - 1 : pos2;
			(errorsList ??= []).Add(GetWreckPosPrefix(0xF000, pos3) + ": compilation failed because of internal compiler error\r\n");
			return RaiseWreck();
		}
	}

	private String GetWreckPosPrefix(ushort code, Index pos) => "Technical wreck " + Convert.ToString(code, 16).ToUpper().PadLeft(4, '0')
		+ "in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString();

	private (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) RaiseWreck() => EmptySyntaxTree();

	private delegate bool ParseAction();

	private bool MainParseAction()
	{
		(String Next, String TreeLabel, List<String> Operators) taskWithoutIndex;
		var task = this.task.ToString();
		return task switch
		{
			"Parameters" => IncreaseStack(nameof(Parameter), currentTask: "Parameters2",
				applyCurrentTask: true, currentExtra: new List<object>()),
			"Members" or "Expr" or "List" or "LambdaExpr" or "AssignedExpr" or "QuestionExpr" or "XorExpr"
				or "OrExpr" or "AndExpr" or "Xor2Expr" or "Or2Expr" or "And2Expr" or "EquatedExpr" or "ComparedExpr"
				or "BitwiseXorExpr" or "BitwiseOrExpr" or "BitwiseAndExpr" or "BitwiseShiftExpr" or "PMExpr" or "MulDivExpr"
				or "PowExpr" or "TetraExpr" or "RangeExpr" or "PrefixExpr" =>
				IncreaseStack(operatorsMapping[this.task].Next, currentTask: this.task + "2", applyCurrentTask: true,
				currentBranch: new(operatorsMapping[this.task].TreeLabel, pos, pos + 1, container), assignCurrentBranch: true),
			"Member" => IncreaseStack(nameof(Property), currentTask: "Member2", applyCurrentTask: true, currentExtra: new List<object>()),
			"ActionChain2" => ActionChain2_3_4(nameof(Cycle), "ActionChain3"),
			"ActionChain3" => ActionChain2_3_4(nameof(SpecialAction), "ActionChain4"),
			"ActionChain4" => ActionChain2_3_4(nameof(Return), "ActionChain5"),
			"OrExpr2" or "AndExpr2" or "Xor2Expr2" or "Or2Expr2" or "And2Expr2" or "ComparedExpr2" or "BitwiseXorExpr2" or "BitwiseOrExpr2" or "BitwiseAndExpr2" or "BitwiseShiftExpr2" or "PMExpr2" or "MulDivExpr2" => LeftAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next, taskWithoutIndex.Operators),
			"PowExpr2" => RightAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next, taskWithoutIndex.Operators),
			"PostfixExpr" => IncreaseStack(nameof(Hypername), currentTask: "PostfixExpr3", applyCurrentTask: true, currentBranch: new("Expr", pos, pos + 1, container), assignCurrentBranch: true),
			"PostfixExpr2" => PostfixExpr2_3(nameof(Type), "PostfixExpr3"),
			"PostfixExpr3" => PostfixExpr2_3(nameof(BasicExpr), "PostfixExpr4"),
			"Indexes" => IncreaseStack("LambdaExpr", currentTask: "Indexes2", applyCurrentTask: true, currentBranch: new("Indexes", pos, pos + 1, container), assignCurrentBranch: true),
			_ => MainParseDelegate(task)(),
		};
	}

	private ParseAction MainParseDelegate(string task) => task switch
	{
		nameof(Main) => Main,
		nameof(Main2) => Main2,
		nameof(Main3) => Main3,
		nameof(Main4) => Main4,
		nameof(Main5) => Main5,
		"Main}" => MainClosing,
		nameof(Namespace) => Namespace,
		"Namespace}" => NamespaceClosing,
		nameof(Class) => Class,
		nameof(Class2) => Class2,
		"Class}" => ClassClosing,
		nameof(Function) => Function,
		nameof(Function2) => Function2,
		nameof(Function3) => Function3,
		"Function}" => FunctionClosing,
		nameof(Constructor) => Constructor,
		nameof(Constructor2) => Constructor2,
		"Constructor}" => ConstructorClosing,
		"Parameters2" or "Parameters3" => Parameters2_3,
		nameof(Parameters4) => Parameters4,
		nameof(Parameter) => Parameter,
		nameof(Parameter2) => Parameter2,
		nameof(Parameter3) => Parameter3,
		nameof(ClassMain) => ClassMain,
		nameof(ClassMain2) => ClassMain2,
		nameof(ClassMain3) => ClassMain3,
		nameof(Members2) => Members2,
		nameof(Member2) => Member2,
		nameof(Member3) => Member3,
		nameof(Member4) => Member4,
		nameof(Property) => Property,
		nameof(Property2) => Property2,
		nameof(Property3) => Property3,
		nameof(ActionChain) => ActionChain,
		nameof(ActionChain5) => ActionChain5,
		nameof(ActionChain6) => ActionChain6,
		nameof(Condition) => Condition,
		"Condition2" or "WhileRepeat2" or "For3" => Condition2_WhileRepeat2_For3,
		nameof(Cycle) => Cycle,
		nameof(Cycle2) => Cycle2,
		nameof(Cycle3) => Cycle3,
		nameof(WhileRepeat) => WhileRepeat,
		nameof(For) => For,
		nameof(For2) => For2,
		nameof(SpecialAction) => SpecialAction,
		nameof(Return) => Return,
		nameof(Return2) => Return2,
		nameof(Expr2) => Expr2,
		nameof(List2) => List2,
		nameof(LambdaExpr2) => LambdaExpr2,
		"LambdaExpr3" or "LambdaExpr4" => LambdaExpr3_4,
		nameof(AssignedExpr2) => AssignedExpr2,
		nameof(QuestionExpr2) => QuestionExpr2,
		nameof(QuestionExpr3) => QuestionExpr3,
		nameof(XorExpr2) => XorExpr2,
		nameof(EquatedExpr2) => EquatedExpr2,
		nameof(EquatedExpr3) => EquatedExpr3,
		"TetraExpr2" or "UnaryExpr4" or "PostfixExpr4" => TetraExpr2_UnaryExpr4_PostfixExpr4,
		nameof(RangeExpr2) => RangeExpr2,
		nameof(RangeExpr3) => RangeExpr3,
		nameof(UnaryExpr) => UnaryExpr,
		nameof(UnaryExpr2) => UnaryExpr2,
		nameof(UnaryExpr3) => UnaryExpr3,
		nameof(PrefixExpr2) => PrefixExpr2,
		nameof(Hypername) or "HypernameNotCall" => Hypername,
		nameof(HypernameNew) => HypernameNew,
		"HypernameConstType" or "HypernameNotCallConstType" => HypernameConstType,
		"HypernameType" or "HypernameNotCallType" => HypernameType,
		nameof(HypernameBasicExpr) or "HypernameNotCallBasicExpr" => HypernameBasicExpr,
		nameof(HypernameCall) => HypernameCall,
		nameof(HypernameIndexes) or "HypernameNotCallIndexes" => HypernameIndexes,
		"HypernameClosing" or "HypernameNotCallClosing" or "BasicExpr4" => HypernameClosing_BasicExpr4,
		nameof(Indexes2) => Indexes2,
		nameof(Type) or nameof(TypeConstraints.BaseClassOrInterface) or nameof(TypeConstraints.NotAbstract) => Type,
		nameof(IdentifierType2) => IdentifierType2,
		nameof(TypeListFail) => TypeListFail,
		nameof(TypeList) => TypeList,
		nameof(TypeList2) => TypeList2,
		nameof(TypeChain) => TypeChain,
		nameof(TypeChain2) => TypeChain2,
		nameof(TypeChainIteration) => TypeChainIteration,
		nameof(TypeChainIteration2) => TypeChainIteration2,
		nameof(TupleType) => TupleType,
		nameof(TupleType2) => TupleType2,
		nameof(BasicExpr) => BasicExpr,
		nameof(BasicExpr2) => BasicExpr2,
		_ => Default,
	};

	private bool IncreaseStack(String newTask, String? currentTask = null, int pos_ = -1, bool applyPos = false, bool applyCurrentTask = false, int start_ = -1, int end_ = -1, List<String>? erl = null, bool applyCurrentErl = false, TreeBranch? currentBranch = null, bool addCurrentBranch = false, bool assignCurrentBranch = false, TreeBranch? newBranch = null, object? currentExtra = null, object? newExtra = null, BlockStack? container_ = null, int btjp = -1, int rtp = -1, int plp = -1)
	{
		static void CheckNoValue(ref int var, int defaultValue)
		{
			if (var == -1)
				var = defaultValue;
		}
		erl ??= [];
		CheckNoValue(ref pos_, pos);
		CheckNoValue(ref start_, start);
		CheckNoValue(ref end_, end);
		currentBranch ??= treeBranch;
		container_ ??= container;
		CheckNoValue(ref btjp, blocksToJumpPos);
		CheckNoValue(ref rtp, registeredTypesPos);
		CheckNoValue(ref plp, parameterListsPos);
		if (applyPos)
			_PosStack[_Stackpos] = pos_;
		if (applyCurrentTask && currentTask != null)
			_TaskStack[_Stackpos] = currentTask;
		if (applyCurrentErl)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_ErLStack[_Stackpos + 1] = [];
		if (addCurrentBranch)
			_TBStack[_Stackpos]?.Add(currentBranch ?? TreeBranch.DoNotAdd());
		else if (assignCurrentBranch)
			_TBStack[_Stackpos] = currentBranch;
		_TBStack[_Stackpos + 1] = null;
		if (currentExtra != null)
			_ExtraStack[_Stackpos] = currentExtra;
		_Stackpos++;
		_PosStack.Add(pos_);
		_StartStack.Add(start_);
		_EndStack.Add(end_);
		_TaskStack.Add(newTask);
		_ErLStack.Add(erl);
		_TBStack.Add(newBranch);
		_ExtraStack.Add(newExtra);
		_ContainerStack.Add(container_);
		_SuccessStack.Add(false);
		_BTJPStack.Add(btjp);
		_RTPStack.Add(rtp);
		_PLPStack.Add(plp);
		return true;
	}

	private bool Main()
	{
		SkipSemicolonsAndNewLines();
		if (pos >= end)
			return Default();
		else if (IsCurrentLexemOther("}"))
			return Default();
		else if (CheckBlockToJump(nameof(Namespace)))
			return IncreaseStack(nameof(Namespace), currentTask: nameof(Main5), applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Class)))
			return IncreaseStack(nameof(Class), currentTask: nameof(Main5), applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Function)))
			return IncreaseStack(nameof(Function), currentTask: nameof(Main5), applyPos: true, applyCurrentTask: true);
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: "Main}", pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed, "#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: nameof(Main5), applyPos: true, applyCurrentTask: true);
	}

	private bool Main2() => success ? NewMainTask() : IncreaseStack(nameof(Class), currentTask: nameof(Main3), applyCurrentTask: true);

	private bool Main3() => success ? NewMainTask() : IncreaseStack(nameof(Function), currentTask: nameof(Main4), applyCurrentTask: true);

	private bool Main4()
	{
		if (success)
			return NewMainTask();
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: "Main}", pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed, "#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: nameof(Main5), applyCurrentTask: true);
	}

	private bool Main5()
	{
		if (!success)
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return Default();
		}
		_TaskStack[_Stackpos] = nameof(Main);
		TransformErrorMessage();
		if (treeBranch != null && !(treeBranch.Info == "" && treeBranch.Length == 0))
		{
			if (_TBStack[_Stackpos] == null)
				_TBStack[_Stackpos] = new(nameof(Main), treeBranch.Info.ToString() is nameof(Main) or nameof(ActionChain) ? treeBranch.Elements : [treeBranch], container);
			else if (_TBStack[_Stackpos]!.Info == nameof(Main) && treeBranch.Info.ToString() is nameof(Class) or nameof(Function) or nameof(Constructor))
				_TBStack[_Stackpos]!.Add(treeBranch);
			else
			{
				_TBStack[_Stackpos]!.Info = nameof(Main);
				_TBStack[_Stackpos]!.AddRange(treeBranch.Elements);
			}
		}
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool MainClosing()
	{
		SkipSemicolonsAndNewLines();
		if (IsCurrentLexemOther("}"))
		{
			_PosStack[_Stackpos] = pos + 1;
			return NewMainTask();
		}
		else
			return EndWithError(0x2004, pos, false);
	}

	private bool GoDownWithPos()
	{
		_PosStack[_Stackpos] = pos;
		_TaskStack[_Stackpos] = nameof(Main4);
		TransformErrorMessage2();
		return true;
	}

	private bool NewMainTask()
	{
		_TaskStack[_Stackpos] = nameof(Main);
		TransformErrorMessage();
		if ((_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0) && (treeBranch == null || treeBranch.Length <= 1 && treeBranch[0].Info == nameof(Main)))
			_TBStack[_Stackpos] = treeBranch;
		else if (_TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length >= 1 && new List<String> { "if", "else", "else if", "while", "repeat", "for", "loop" }.Contains(_TBStack[_Stackpos]?[^1].Info ?? "") && treeBranch == null)
			AppendBranch(nameof(Main), new(nameof(Main), pos - 1, container));
		else
			AppendBranch(nameof(Main));
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool Namespace()
	{
		if (CheckBlockToJump(nameof(Namespace)))
		{
			pos = blocksToJump[blocksToJumpPos].End;
			_TBStack[_Stackpos] = new("Namespace " + blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container);
			return CheckOpeningBracketAndAddTask(nameof(Main), "Namespace}", BlockType.Namespace);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool NamespaceClosing()
	{
		if (IsClosingFigureBracket())
			return EndWithAdding(true);
		else
		{
			var pos2 = pos;
			CloseBracket(ref pos, "}", ref errorsList, true, end);
			GenerateMessage(0x2004, pos2, true);
			return EndWithAdding(false);
		}
	}

	private bool Class()
	{
		if (!CheckBlockToJump(nameof(Class)))
			return _SuccessStack[_Stackpos] = false;
		pos = blocksToJump[blocksToJumpPos].End;
		_TBStack[_Stackpos] = new(nameof(Class), new TreeBranch(blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container), container);
		if (CheckClassSubordination())
			return CheckColonAndAddTask(nameof(TypeConstraints.BaseClassOrInterface), nameof(Class2), BlockType.Class);
		else
			return CheckOpeningBracketAndAddTask(nameof(ClassMain), "Class}", BlockType.Class);
	}

	private bool Class2()
	{
		void CheckSuccess()
		{
			if (!(success && extra is UniversalType UnvType))
			{
				_TBStack[_Stackpos]?.Add(new TreeBranch("type", registeredTypes[registeredTypesPos].Start, registeredTypes[registeredTypesPos].End, container) { Extra = NullType });
				return;
			}
			var t = UserDefinedTypesList[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)];
			t.BaseType = UnvType;
			UserDefinedTypesList[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		CheckSuccess();
		TransformErrorMessage2();
		pos = registeredTypes[registeredTypesPos].End;
		return CheckOpeningBracketAndAddTask(nameof(ClassMain), "Class}", BlockType.Class, registeredTypes[registeredTypesPos++].Name);
	}

	private bool ClassClosing()
	{
		if (IsClosingFigureBracket())
			return EndWithAdding(true);
		else
		{
			var pos2 = pos;
			CloseBracket(ref pos, "}", ref errorsList, true, end);
			GenerateMessage(0x2004, pos2, true);
			return EndWithAdding(false);
		}
	}

	private bool Function()
	{
		if (CheckBlockToJump(nameof(Function)))
		{
			if (registeredTypesPos < registeredTypes.Length && blocksToJump[blocksToJumpPos].Start >= pos && blocksToJump[blocksToJumpPos].End <= end)
				return IncreaseStack(nameof(Type), currentTask: nameof(Function2), pos_: blocksToJump[blocksToJumpPos].Start, applyCurrentTask: true, start_: blocksToJump[blocksToJumpPos].Start, end_: blocksToJump[blocksToJumpPos].End, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes), rtp: registeredTypesPos + 1);
			else
			{
				_TaskStack[_Stackpos] = nameof(Function2);
				return true;
			}
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Function2()
	{
		void CheckSuccess()
		{
			if (!success || extra is not UniversalType UnvType)
			{
				_TBStack[_Stackpos] = new(nameof(Function), [new(blocksToJump[blocksToJumpPos].Name, blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container), new("type", blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container) { Extra = NullType }], container);
				return;
			}
			var container2 = blocksToJump[blocksToJumpPos].Container;
			var name = blocksToJump[blocksToJumpPos].Name;
			var start = blocksToJump[blocksToJumpPos].Start;
			var index = UserDefinedFunctionIndexesList[container2][start];
			var t = UserDefinedFunctionsList[container2][name][index];
			t.ReturnUnvType = UnvType;
			UserDefinedFunctionsList[container2][name][index] = t;
			_TBStack[_Stackpos] = new(nameof(Function), [new(name, start, blocksToJump[blocksToJumpPos].End, container), treeBranch ?? TreeBranch.DoNotAdd()], container);
		}
		if (CheckBlockToJump2(nameof(Function)))
		{
			CheckSuccess();
			TransformErrorMessage2();
			if (parameterListsPos < parameterLists.Length && parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start && parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
				return IncreaseStack("Parameters", currentTask: nameof(Function3), pos_: parameterLists[parameterListsPos].Start, applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start, end_: parameterLists[parameterListsPos].End, currentExtra: new GeneralMethodParameters(), plp: parameterListsPos + 1);
			else
			{
				_TaskStack[_Stackpos] = nameof(Function3);
				_ExtraStack[_Stackpos] = new GeneralMethodParameters();
				return true;
			}
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Function3()
	{
		void CheckSuccess()
		{
			if (!success || extra is not GeneralMethodParameters parameters)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1, blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			var container2 = blocksToJump[blocksToJumpPos].Container;
			var name = blocksToJump[blocksToJumpPos].Name;
			var start = blocksToJump[blocksToJumpPos].Start;
			var index = UserDefinedFunctionIndexesList[container2][start];
			var t = UserDefinedFunctionsList[container2][name][index];
			if (UserDefinedFunctionsList[container2][name].Take(index).Exists(x => x.Parameters.Length == parameters.Length
				&& x.Parameters.Combine(parameters).All(y => TypesAreEqual(y.Item1.Type, y.Item2.Type)
				&& (y.Item1.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)) == 0
				&& (y.Item2.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)) == 0)))
			{
				GenerateMessage(0x2032, pos, false, name, lexems[start].LineN, lexems[start].Pos);
				t.Attributes |= FunctionAttributes.Wrong;
				UserDefinedFunctionsList[container2][name][index] = t;
				return;
			}
			if (parameters.Length == 0)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1, blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			t.Parameters = parameters;
			UserDefinedFunctionsList[container2][name][index] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			return;
		}
		if (CheckBlockToJump2(nameof(Function)))
		{
			CheckSuccess();
			TransformErrorMessage2();
			pos = blocksToJump[blocksToJumpPos].End;
			if (success && (UserDefinedFunctionsList[blocksToJump[blocksToJumpPos].Container]
				[blocksToJump[blocksToJumpPos].Name][0].Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
			{
				SkipSemicolonsAndNewLines();
				blocksToJumpPos++;
				return EndWithAdding(true);
			}
			else
				return CheckOpeningBracketAndAddTask(nameof(Main), "Function}", BlockType.Function);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool FunctionClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(0x2004, pos, false);

	private bool Constructor()
	{
		if (CheckBlockToJump2(nameof(Constructor)))
		{
			_TBStack[_Stackpos] = new(nameof(Constructor), blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container);
			TransformErrorMessage2();
			if (parameterListsPos < parameterLists.Length && parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start && parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
				return IncreaseStack("Parameters", currentTask: nameof(Constructor2), pos_: parameterLists[parameterListsPos].Start, applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start, end_: parameterLists[parameterListsPos].End, currentExtra: new GeneralMethodParameters(), plp: parameterListsPos + 1);
			else
			{
				_TaskStack[_Stackpos] = nameof(Constructor2);
				_ExtraStack[_Stackpos] = new GeneralMethodParameters();
				return true;
			}
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Constructor2()
	{
		void CheckSuccess()
		{
			if (!success || extra is not GeneralMethodParameters parameters || parameters.Length == 0)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1, blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			var container2 = blocksToJump[blocksToJumpPos].Container;
			var index = UserDefinedConstructorIndexesList[container2][blocksToJump[blocksToJumpPos].Start];
			var t = UserDefinedConstructorsList[container2][index];
			t.Parameters = parameters;
			UserDefinedConstructorsList[container2][index] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		if (CheckBlockToJump2(nameof(Constructor)))
		{
			CheckSuccess();
			TransformErrorMessage2();
			pos = blocksToJump[blocksToJumpPos].End;
			return CheckOpeningBracketAndAddTask(nameof(Main), "Constructor}", BlockType.Constructor);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool ConstructorClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(0x2004, pos, false);

	private bool CheckBlockToJump(String string_) => CheckBlockToJump3() && blocksToJump[blocksToJumpPos].Start >= pos && !lexems.GetRange(pos, blocksToJump[blocksToJumpPos].Start - pos).ToHashSet().Convert(X => (X.Type, X.String)).Intersect([(LexemType.Other, ";"), (LexemType.Other, "\r\n"), (LexemType.Other, "{"), (LexemType.Other, "}")]).Any() && blocksToJump[blocksToJumpPos].Type == string_;

	private bool CheckBlockToJump2(String string_) => CheckBlockToJump3() && blocksToJump[blocksToJumpPos].Type == string_;

	private bool CheckBlockToJump3() => blocksToJumpPos < blocksToJump.Length && lexems[blocksToJump[blocksToJumpPos].Start].LineN == lexems[pos].LineN;

	private bool CheckClassSubordination() => registeredTypesPos < registeredTypes.Length && lexems[registeredTypes[registeredTypesPos].Start].LineN == lexems[pos].LineN;

	private bool IsClosingFigureBracket()
	{
		SkipSemicolonsAndNewLines();
		if (IsCurrentLexemOther("}"))
		{
			pos++;
			return true;
		}
		return false;
	}

	private bool CheckColonAndAddTask(String newTask, String closingString, BlockType blockType)
	{
		if (IsCurrentLexemOperator(":"))
			return IncreaseStack(newTask, currentTask: closingString, pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(blockType, blocksToJump[blocksToJumpPos].Name, 1))));
		else
			return EndWithError(0x2006, pos, false);
	}

	private bool CheckOpeningBracketAndAddTask(String newTask, String closingString, BlockType blockType, String? name = null)
	{
		if (IsCurrentLexemOther("{"))
			return IncreaseStack(newTask, currentTask: closingString, pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(blockType, name ?? blocksToJump[blocksToJumpPos].Name, 1))), btjp: blocksToJumpPos + 1);
		else
			return EndWithError(0x2003, pos, false);
	}

	private bool Parameters2_3()
	{
		if (pos >= end)
			return ParametersEnd();
		else if (IsCurrentLexemOperator(","))
		{
			TransformErrorMessageAndAppendBranch("Parameters");
			_TBStack[_Stackpos + 1] = null;
			AddParameter();
			return IncreaseStack(nameof(Parameter), pos_: pos + 1, applyPos: true, currentExtra: new List<object>());
		}
		else
			return EndWithError(0x2019, pos, false);
	}

	private bool Parameters4()
	{
		if (pos >= end)
			return ParametersEnd();
		else if (IsCurrentLexemOperator(","))
			return EndWithError(0x201A, pos, false);
		else
			return EndWithError(0x2019, pos, false);
	}

	private bool ParametersEnd()
	{
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		AppendBranch("Parameters");
		AddParameter();
		return Default();
	}

	private void AddParameter()
	{
		try
		{
			var parameters = (GeneralMethodParameters?)_ExtraStack[_Stackpos - 1];
			parameters?.Add((GeneralMethodParameter?)extra ?? new());
			_ExtraStack[_Stackpos - 1] = parameters;
		}
		catch
		{
		}
	}

	private bool Parameter()
	{
		if (IsParameterModifier())
		{
			if (lexems[pos].String == "params")
				_TaskStack[_Stackpos - 1] = nameof(Parameters4);
			AddPropertyAttribute(attributesMapping[lexems[pos].String], nameof(Parameter));
		}
		else
			AddPropertyAttribute(ParameterAttributes.None, nameof(Parameter), false);
		while (IsParameterModifier())
		{
			pos++;
			GenerateMessage(0x2020, pos - 1, false);
		}
		return IncreaseStack(nameof(Type), currentTask: nameof(Parameter2), pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes));
	}

	private bool Parameter2()
	{
		if (_TBStack[_Stackpos] != null && _ExtraStack[_Stackpos - 1] is List<object> list && list[0] is ParameterAttributes attributes)
			_TBStack[_Stackpos]!.Extra = attributes;
		if (!AddExtraAndIdentifier())
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(new("", pos, pos + 1, container));
			CheckParameters3(true, true);
			return Default();
		}
		if (IsCurrentLexemOperator("="))
		{
			if (_TaskStack[_Stackpos - 1] != nameof(Parameters4))
				_TaskStack[_Stackpos - 1] = "Parameters3";
			try
			{
				CreateObjectList(out var l);
				l![0] = (ParameterAttributes)l[0] | ParameterAttributes.Optional;
				l.Add(pos + 1);
				_ExtraStack[_Stackpos - 1] = l;
			}
			catch
			{
			}
			return IncreaseStack("Expr", currentTask: nameof(Parameter3), pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentBranch: new("optional", pos, pos + 1, container), addCurrentBranch: true);
		}
		else
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			CheckParameters3();
			return Default();
		}
	}

	private bool Parameter3()
	{
		if (success)
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		else
			_TBStack[_Stackpos]?.Add(new("null", pos, pos + 1, container));
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		try
		{
			CreateObjectList(out var l);
			var UnvType = (UniversalType)l![1];
			_ExtraStack[_Stackpos - 1] = new GeneralMethodParameter(UnvType, (String)l[2], (ParameterAttributes)l[0], "null");
			var index = VariablesList.IndexOfKey(container);
			if (index == -1)
			{
				VariablesList.Add(container, []);
				index = VariablesList.IndexOfKey(container);
			}
			var list = VariablesList.Values[index];
			list.Add((String)l[2], UnvType);
			parameterValues.Add((parameterLists[parameterListsPos - 1].Container, parameterLists[parameterListsPos - 1].Name, ((GeneralMethodParameters?)_ExtraStack[_Stackpos - 2] ?? []).Length + 1, (int)l[3], pos));
		}
		catch
		{
		}
		return Default();
	}

	private bool IsParameterModifier() => IsLexemKeyword(lexems[pos], ["ref", "out", "params"]);

	private void CheckParameters3(bool expectIdentifier = false, bool skipParameterName = false)
	{
		if (_TaskStack[_Stackpos - 1] == "Parameters3")
		{
			GenerateMessage(0x201B, pos, false, expectIdentifier);
			_TBStack[_Stackpos]?.Add(new("null", pos, pos + 1, container));
		}
		else
			_TBStack[_Stackpos]?.Add(new("no optional", pos, pos + 1, container));
		try
		{
			CreateObjectList(out var l);
			var UnvType = (UniversalType)l![1];
			_ExtraStack[_Stackpos - 1] = new GeneralMethodParameter(UnvType, skipParameterName ? "" : (String)l[2], (ParameterAttributes)l[0], _TBStack[_Stackpos]?[^1].Info ?? "no optional");
		}
		catch
		{
		}
	}

	private bool ClassMain()
	{
		SkipSemicolonsAndNewLines();
		return IncreaseStack(nameof(Class), currentTask: nameof(ClassMain2), pos_: pos, applyPos: true, applyCurrentTask: true, currentBranch: new(nameof(ClassMain), pos, pos + 1, container), assignCurrentBranch: true);
	}

	private bool ClassMain2()
	{
		if (success)
		{
			SkipSemicolonsAndNewLines();
			return IncreaseStack(nameof(Class), pos_: pos, applyPos: true, applyCurrentErl: true, addCurrentBranch: true);
		}
		else
		{
			SkipSemicolonsAndNewLines();
			return IncreaseStack("Members", currentTask: nameof(ClassMain3), pos_: pos, applyPos: true, applyCurrentTask: true);
		}
	}

	private bool ClassMain3()
	{
		if (success && treeBranch != null && treeBranch.Length != 0)
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		return Default();
	}

	private bool Members2()
	{
		if (success)
		{
			SkipSemicolonsAndNewLines();
			if (treeBranch == null || treeBranch.Length == 0)
				_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return IncreaseStack("Member", pos_: pos, applyPos: true, currentBranch: treeBranch, addCurrentBranch: true);
		}
		else
		{
			PropertiesAction();
			return Default();
		}
	}

	private void PropertiesAction()
	{
		if (!UserDefinedConstructorsList.TryGetValue(container, out var list))
		{
			UserDefinedConstructorsList.Add(container, []);
			list = UserDefinedConstructorsList[container];
		}
		list.Insert(0, (ConstructorAttributes.Multiconst, []));
		var increment = 1;
		if (UserDefinedPropertiesList.TryGetValue(container, out var propertiesList) && propertiesList.Length != 0 && UserDefinedPropertiesOrder.TryGetValue(container, out var propertiesOrder) && propertiesOrder.Length != 0)
			list.Insert(1, (ConstructorAttributes.Multiconst, []));
		if (UserDefinedTypesList.TryGetValue(SplitType(container), out var userDefinedType)
			&& CreateVar(GetAllProperties(userDefinedType.BaseType.MainType), out var baseProperties).Length != 0)
			foreach (var property in baseProperties)
				if (property.Value.Attributes is PropertyAttributes.None or PropertyAttributes.Internal)
					list[1].Parameters.Add(new(property.Value.UnvType, property.Key, ParameterAttributes.Optional, "null"));
		if (UserDefinedPropertiesList.TryGetValue(container, out propertiesList) && propertiesList.Length != 0 && UserDefinedPropertiesOrder.TryGetValue(container, out propertiesOrder) && propertiesOrder.Length != 0)
		{
			foreach (var propertyName in propertiesOrder)
			{
				if (propertiesList.TryGetValue(propertyName, out var property)/* && property.Attributes is PropertyAttributes.None or PropertyAttributes.Internal*/)
					list[1].Parameters.Add(new(property.UnvType, propertyName, ParameterAttributes.Optional, "null"));
			}
			increment++;
		}
		if (UserDefinedConstructorIndexesList.TryGetValue(container, out var list2))
		{
			for (var i = 0; i < list2.Length; i++)
				list2[list2.Keys[i]] += increment;
		}
	}

	private bool Member2()
	{
		if (success)
			return EndWithAssigning(true);
		else
			return IncreaseStack(nameof(Function), currentTask: nameof(Member3), applyCurrentTask: true, currentExtra: new List<object>());
	}

	private bool Member3() => success ? EndWithAssigning(true) : IncreaseStack(nameof(Constructor), currentTask: nameof(Member4), applyCurrentTask: true);

	private bool Member4()
	{
		if (success)
			return EndWithAssigning(true);
		else
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool Property()
	{
		var oldPos = pos;
		if (IsLexemKeyword(lexems[pos], ["closed", "protected", "internal", "public"]))
		{
			AddPropertyAttribute(attributesMapping[lexems[pos].String], nameof(Property));
			if (lexems[pos].String == "public")
				GenerateMessage(0x202F, pos - 1, false);
		}
		else
			AddPropertyAttribute(PropertyAttributes.None, nameof(Property), false);
		var value = (IsCurrentLexemKeyword("static") ? 2 : 0) + ((UserDefinedTypesList[SplitType(container)].Attributes
			& TypeAttributes.Static) == TypeAttributes.Static ? 1 : 0);
		if (value == 3)
			GenerateMessage(0x8000, pos, false);
		if (value >= 1)
			AddPropertyAttribute2(PropertyAttributes.Static);
		if (value >= 2)
			pos++;
		if (IsCurrentLexemKeyword("const"))
		{
			if (value >= 2)
				GenerateMessage(0x800B, pos - 1, false);
			_TBStack[_Stackpos]!.Info = "Constant";
			AddPropertyAttribute2(PropertyAttributes.Static | PropertyAttributes.Const);
			pos++;
		}
		if (IsCurrentLexemKeyword(nameof(Constructor)))
		{
			pos = _PosStack[_Stackpos] = oldPos;
			_TaskStack[_Stackpos] = nameof(Constructor);
			_TaskStack[_Stackpos - 1] = nameof(Member4);
			_ExtraStack[_Stackpos - 1] = null;
			return true;
		}
		return IncreaseStack(nameof(Type), currentTask: nameof(Property2), pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes));
	}

	private bool Property2()
	{
		bool CheckIdentifier(String string_)
		{
			if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == string_)
			{
				pos++;
				return true;
			}
			else
				return EndWithError(0x2008, pos, false, "\"" + string_ + "\"");
		}
		if (IsCurrentLexemKeyword(nameof(Function)))
			return _SuccessStack[_Stackpos] = false;
		CreateObjectList(out var l);
		if (!AddExtraAndIdentifier())
			return EndWithError(0x2001, pos, false);
		if (IsCurrentLexemOther("{"))
		{
			pos++;
			if (((PropertyAttributes)l![0] & PropertyAttributes.Const) == PropertyAttributes.Const)
			{
				GenerateMessage(0x203C, pos - 1, false);
				CloseBracket(ref pos, "}", ref errorsList, false);
				for (; !IsLexemOther(lexems[pos++], ";");) ;
				return EndWithEmpty();
			}
		}
		else
			goto l0;
		if (!CheckIdentifier("get"))
			return false;
		if (IsCurrentLexemOperator(","))
			pos++;
		else
		{
			AddPropertyAttribute2(PropertyAttributes.NoSet);
			goto l0;
		}
		if (!CheckIdentifier("set") || !CheckLexemAndEndWithError("}"))
			return false;
		l0:
		if (IsCurrentLexemOperator("="))
			return IncreaseStack("Expr", currentTask: nameof(Property3), pos_: pos + 1, applyPos: true, applyCurrentTask: true);
		else
		{
			if (((PropertyAttributes)l![0] & PropertyAttributes.Const) == PropertyAttributes.Const)
			{
				GenerateMessage(0x203D, pos, false);
				for (; !IsLexemOther(lexems[pos++], ";");) ;
				return EndWithEmpty();
			}
			if (!CheckLexemAndEndWithError(";", true))
				return false;
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(new("null", pos - 1, container));
			return AddUserDefinedProperty();
		}
	}

	private bool Property3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		CreateObjectList(out var l);
		if (IsCurrentLexemOther(";"))
			pos++;
		else
			return EndWithError(0x2002, pos, false);
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		if (((PropertyAttributes)l![0] & PropertyAttributes.Const) == PropertyAttributes.Const)
			return AddUserDefinedConstant();
		return AddUserDefinedProperty();
	}

	private void AddPropertyAttribute(dynamic atrribute, String treeString, bool increasePos = true)
	{
		if (increasePos)
			pos++;
		_TBStack[_Stackpos] = new(treeString, pos - 1, pos, container);
		try
		{
			_ExtraStack[_Stackpos - 1] = ((List<object>?)_ExtraStack[_Stackpos - 1] ?? []).Append((object)atrribute);
		}
		catch
		{
		}
	}

	private bool AddExtraAndIdentifier()
	{
		if (success)
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		else
			_TBStack[_Stackpos]?.Add(new("type", pos, pos + 1, container) { Extra = NullType });
		if (_ExtraStack[_Stackpos - 1] is List<object> list)
			list.Add((success ? (UniversalType?)extra : null) ?? NullType);
		if (lexems[pos].Type == LexemType.Identifier)
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			if (_ExtraStack[_Stackpos - 1] is List<object> list2)
				list2.Add(lexems[pos - 1].String);
			return true;
		}
		return false;
	}

	private void AddPropertyAttribute2(PropertyAttributes attribute)
	{
		try
		{
			CreateObjectList(out var l);
			l![0] = (PropertyAttributes)l[0] | attribute;
			_ExtraStack[_Stackpos - 1] = l;
		}
		catch
		{
		}
	}

	private bool AddUserDefinedProperty()
	{
		try
		{
			CreateObjectList(out var l);
			var UnvType = (UniversalType)l![1];
			if (!UserDefinedPropertiesList.TryGetValue(container, out var list))
			{
				UserDefinedPropertiesList.Add(container, []);
				list = UserDefinedPropertiesList[container];
			}
			var attributes = (PropertyAttributes)l[0];
			var name = (String)l[2];
			list.Add(name, new(UnvType, attributes, treeBranch == null ? "" : treeBranch.Length == 0
				&& Universal.TryParse(treeBranch.Info.ToString(), out var value) ? value.ToString(true)
				: treeBranch.Info == "Expr" && treeBranch.Length == 1 && treeBranch[0].Length == 0
				&& Universal.TryParse(treeBranch[0].Info.ToString(), out value) ? value.ToString(true) : ""));
			var t = UserDefinedTypesList[SplitType(container)];
			if ((attributes & PropertyAttributes.Static) == 0)
			{
				t.Decomposition ??= [];
				t.Decomposition.Add(name, new("type", pos, container) { Extra = UnvType });
				UserDefinedTypesList[SplitType(container)] = t;
				if (!UserDefinedPropertiesMapping.TryGetValue(container, out var dic))
				{
					UserDefinedPropertiesMapping.Add(container, []);
					dic = UserDefinedPropertiesMapping[container];
				}
				dic.Add(name, dic.Length);
			}
			if (!UserDefinedPropertiesOrder.TryGetValue(container, out var list2))
			{
				UserDefinedPropertiesOrder.Add(container, []);
				list2 = UserDefinedPropertiesOrder[container];
			}
			list2.Add(name);
		}
		catch
		{
		}
		return Default();
	}

	private bool AddUserDefinedConstant()
	{
		try
		{
			CreateObjectList(out var l);
			var UnvType = (UniversalType)l![1];
			if (!UserDefinedConstantsList.TryGetValue(container, out var list))
			{
				UserDefinedConstantsList.Add(container, []);
				list = UserDefinedConstantsList[container];
			}
			var attributes = (ConstantAttributes)l[0];
			var name = (String)l[2];
			if (treeBranch == null)
				list.Add(name, new(UnvType, attributes, new("null", 0, [])));
			else if (treeBranch.Length == 0 && Universal.TryParse(treeBranch.Info.ToString(), out var value))
				list.Add(name, new(UnvType, attributes, new(value.ToString(true), treeBranch.Pos, treeBranch.Container)
				{
					Extra = value.InnerType
				}));
			else if (treeBranch.Info == "Expr" && treeBranch.Length == 1 && treeBranch[0].Length == 0
				&& Universal.TryParse(treeBranch[0].Info.ToString(), out value))
				list.Add(name, new(UnvType, attributes, new(value.ToString(true), treeBranch.Pos, treeBranch.Container)
				{
					Extra = value.InnerType
				}));
			else
				list.Add(name, new(UnvType, attributes, treeBranch));
		}
		catch
		{
		}
		return Default();
	}

	private bool CheckLexemAndEndWithError(String string_, bool addQuotes = false)
	{
		if (IsLexemOther(lexems[pos], string_))
		{
			pos++;
			return true;
		}
		else
			return EndWithError(0x2008, pos, false, addQuotes ? "\"" + string_ + "\"" : string_);
	}

	private bool ActionChain()
	{
		SkipSemicolonsAndNewLines();
		if (pos >= end)
			return Default();
		else if (IsCurrentLexemOther("{") || IsCurrentLexemOther("}"))
			return Default();
		if (CheckBlockToJump(nameof(Function)) && registeredTypesPos < registeredTypes.Length
			&& blocksToJump[blocksToJumpPos].Start >= pos && blocksToJump[blocksToJumpPos].End <= end)
			return Default();
		if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "goto")
		{
			var pos2 = pos + 1;
			if (lexems[pos2].Type == LexemType.Identifier)
			{
				GenerateMessage(0x2048, pos, true);
				return EndActionChain();
			}
		}
		return IncreaseStack(CreateVar(lexems[pos].Type == LexemType.Keyword ? lexems[pos].String.ToString() switch
		{
			"if" or "else" => nameof(Condition),
			"loop" or "repeat" or "while" or "for" => nameof(Cycle),
			"continue" or "break" => nameof(SpecialAction),
			"return" => nameof(Return),
			"null" when pos + 1 < end && IsLexemKeyword(lexems[pos + 1], [nameof(Function), "Operator", "Extent"]) => nameof(Main),
			_ => "Expr",
		} : "Expr", out var newTask), currentTask: newTask == "Expr" ? nameof(ActionChain6) : nameof(ActionChain5), applyCurrentTask: true);
	}

	private bool ActionChain2_3_4(String newTask, String currentTask)
	{
		if (!success)
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
		_TaskStack[_Stackpos] = nameof(ActionChain);
		TransformErrorMessage();
		if (treeBranch != null && (treeBranch.Info != "" || treeBranch.Length != 0))
			AppendBranch(nameof(ActionChain));
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool ActionChain5()
	{
		if (!success)
			return IncreaseStack("Expr", currentTask: nameof(ActionChain6), pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new List<object>());
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		if (treeBranch != null && treeBranch.Length != 0)
		{
			AppendBranch(nameof(ActionChain));
			return Default();
		}
		else
			return EndActionChain();
	}

	private bool ActionChain6()
	{
		if (success && treeBranch != null && treeBranch.Length != 0 && !ValidateEndOrLexem(";", true))
		{
			_PosStack[_Stackpos] = pos;
			return ChangeTaskAndAppendBranch(nameof(ActionChain));
		}
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return Default();
		}
		else if (IsLexemKeyword(lexems[pos], ["switch", "case", "delete"]))
			GenerateMessage(0x201E, pos, false, lexems[pos].String);
		else if (!IsLexemOther(lexems[pos], StopLexemsList))
			GenerateMessage(0x2007, pos, false);
		return EndActionChain();
	}

	private bool EndActionChain()
	{
		if (pos >= 1 && IsLexemKeyword(lexems[pos - 1], ["else", "loop"]) || pos >= 2 && IsLexemKeyword(lexems[pos - 2], ["continue", "break"]) && IsLexemOther(lexems[pos - 1], ";"))
		{
			_PosStack[_Stackpos] = pos;
			_TaskStack[_Stackpos] = nameof(ActionChain);
			AppendBranch(nameof(ActionChain), treeBranch!);
			return true;
		}
		while (!IsLexemOther(lexems[pos], StopLexemsList))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
		}
		if (!IsLexemOther(lexems[pos], ["{", "}"]))
			pos++;
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		return Default();
	}

	private bool ChangeTaskAndAppendBranch(String newTask)
	{
		_TaskStack[_Stackpos] = newTask;
		TransformErrorMessageAndAppendBranch(newTask);
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool Condition()
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (IsCurrentLexemKeyword("if"))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
			else if (IsCurrentLexemOperator("!"))
			{
				pos++;
				_TBStack[_Stackpos] = new("if!", pos - 2, pos, container);
			}
			else
				_TBStack[_Stackpos] = new("if", pos - 1, pos, container);
		}
		else if (IsCurrentLexemKeyword("else"))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
			else if (IsCurrentLexemKeyword("if"))
			{
				pos++;
				if (pos >= end)
				{
					GenerateUnexpectedEndError();
					return EndWithEmpty();
				}
				else if (IsCurrentLexemOperator("!"))
				{
					pos++;
					_TBStack[_Stackpos] = new("else if!", pos - 2, pos, container);
				}
				else
					_TBStack[_Stackpos] = new("else if", pos - 1, pos, container);
			}
			else
			{
				_TBStack[_Stackpos] = new("else", pos - 1, pos, container);
				_ErLStack[_Stackpos].AddRange(errorsList ?? []);
				return Default();
			}
		}
		else
			return EndWithError(0x200F, pos, false);
		if (ValidateEndOrLexem("("))
			return EndWithEmpty();
		else
			return IncreaseStack("Expr", currentTask: "Condition2", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool Condition2_WhileRepeat2_For3()
	{
		if (success && treeBranch != null)
		{
			if (pos >= end)
			{
				GenerateUnexpectedEndError(true);
				return EndWithEmpty();
			}
			else if (IsCurrentLexemOther(")"))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(treeBranch);
			}
			else
			{
				GenerateMessage(0x200B, pos, true);
				return EndWithEmpty();
			}
		}
		else
		{
			GenerateMessage(0x200E, pos, true);
			return EndWithEmpty();
		}
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		if (pos < end && IsCurrentLexemOther(";") && lexems[pos].LineN == lexems[pos - 1].LineN)
			GenerateMessage(0x8001, pos, false);
		return Default();
	}

	private bool ValidateEndOrLexem(String string_, bool addQuotes = false)
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return true;
		}
		else if (IsLexemOther(lexems[pos], string_))
		{
			pos++;
			return false;
		}
		else
		{
			GenerateMessage(0x2008, pos, true, addQuotes ? "\"" + string_ + "\"" : string_);
			return true;
		}
	}

	private bool Cycle()
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (IsCurrentLexemKeyword("loop"))
		{
			pos++;
			_TBStack[_Stackpos] = new("loop", pos - 1, pos, container);
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return Default();
		}
		return lexems[pos].Type == LexemType.Keyword ? lexems[pos].String.ToString() switch
		{
			"while" or "repeat" => IncreaseStack(nameof(WhileRepeat), currentTask: nameof(Cycle3), pos_: pos, applyPos: true, applyCurrentTask: true),
			"for" => IncreaseStack(nameof(For), currentTask: nameof(Cycle3), pos_: pos, applyPos: true, applyCurrentTask: true),
			_ => EndWithError(0x2010, pos, false),
		} : EndWithError(0x2010, pos, false);
	}

	private bool Cycle2() => success ? EndWithAssigning(true) : IncreaseStack(nameof(For), currentTask: nameof(Cycle3), applyCurrentTask: true);

	private bool Cycle3() => success ? EndWithAssigning(true) : EndWithError(0x2010, pos, false);

	private bool WhileRepeat()
	{
		if (IsLexemKeyword(lexems[pos], "while"))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
			else if (IsCurrentLexemOperator("!"))
			{
				pos++;
				_TBStack[_Stackpos] = new("while!", pos - 2, pos, container);
			}
			else
				_TBStack[_Stackpos] = new("while", pos - 1, pos, container);
		}
		else if (IsLexemKeyword(lexems[pos], "repeat"))
		{
			pos++;
			_TBStack[_Stackpos] = new("repeat", pos - 1, pos, container);
		}
		else
			return _SuccessStack[_Stackpos] = false;
		if (ValidateEndOrLexem("("))
			return EndWithEmpty();
		else
			return IncreaseStack("Expr", currentTask: "WhileRepeat2", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool For()
	{
		if (IsLexemKeyword(lexems[pos], ["for", "foreach"]))
		{
			pos++;
			_TBStack[_Stackpos] = new("for", pos - 1, pos, container);
		}
		else
			return _SuccessStack[_Stackpos] = false;
		if (ValidateEndOrLexem("("))
			return EndWithEmpty();
		else
			return IncreaseStack(nameof(Type), currentTask: nameof(For2), pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool For2()
	{
		if (!success)
			return EndWithEmpty(true);
		if (pos >= end)
		{
			GenerateUnexpectedEndError(true);
			return EndWithEmpty();
		}
		else if (lexems[pos].Type == LexemType.Identifier)
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new("Declaration", [treeBranch ?? TreeBranch.DoNotAdd(), new(lexems[pos - 1].String, pos - 1, pos, container)], container));
		}
		else
		{
			GenerateMessage(0x2001, pos, true);
			return EndWithEmpty();
		}
		if (pos >= end)
		{
			GenerateUnexpectedEndError(true);
			return EndWithEmpty();
		}
		else if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "in")
			pos++;
		else
		{
			GenerateMessage(0x2013, pos, true);
			return EndWithEmpty();
		}
		return IncreaseStack("Expr", currentTask: "For3", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool SpecialAction()
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (lexems[pos].Type == LexemType.Keyword && (lexems[pos].String == "continue" || lexems[pos].String == "break"))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError(true);
				return EndWithEmpty();
			}
			else if (IsCurrentLexemOther(";"))
			{
				pos++;
				_TBStack[_Stackpos] = new(lexems[pos - 2].String, pos - 2, pos, container);
			}
			else
			{
				GenerateMessage(0x2002, pos, true);
				return EndWithEmpty();
			}
			return Default();
		}
		else
			return EndWithError(0x2011, pos, false);
	}

	private bool Return()
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (IsCurrentLexemKeyword("return"))
		{
			pos++;
			_TBStack[_Stackpos] = new("return", pos - 1, pos, container);
		}
		else
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (IsCurrentLexemOther(";"))
		{
			GenerateMessage(0x8002, pos, false);
			pos++;
			_TBStack[_Stackpos]?.Add(new("null", pos - 1, container));
			return Default();
		}
		return IncreaseStack("Expr", currentTask: nameof(Return2), pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool Return2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther(";"))
		{
			pos++;
			return EndWithAdding(true);
		}
		else
		{
			GenerateMessage(0x2002, pos, true);
			return EndWithEmpty();
		}
	}

	private bool Expr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator("CombineWith"))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new("CombineWith", pos - 1, pos, container));
		}
		else
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		return IncreaseStack("List", pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool List2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator(","))
		{
			if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall) || _TaskStack[_Stackpos - 1] == nameof(Expr2) && _TaskStack[_Stackpos - 2] == nameof(BasicExpr2))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			}
			else
			{
				_TBStack[_Stackpos] = treeBranch;
				return Default();
			}
		}
		else
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall) || _TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length != 0 && _TaskStack[_Stackpos - 1] == nameof(Expr2) && _TaskStack[_Stackpos - 2] == nameof(BasicExpr2))
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			else
				_TBStack[_Stackpos] = treeBranch;
			return Default();
		}
		return IncreaseStack("LambdaExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool LambdaExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsCurrentLexemOperator("=>"))
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		pos++;
		if (IsCurrentLexemOther("{"))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			return IncreaseStack(nameof(Main), currentTask: "LambdaExpr4",
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
		return IncreaseStack("AssignedExpr", currentTask: "LambdaExpr3",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool LambdaExpr3_4()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (task == "LambdaExpr4" ^ IsCurrentLexemOther("}"))
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		if (task == "LambdaExpr4")
			pos++;
		var treeBranch = _TBStack[_Stackpos];
		if (treeBranch != null)
			treeBranch.Info = "Lambda";
		if (treeBranch != null && treeBranch.Length == 1
			&& treeBranch[0].Info.ToString() is "Assignment" or "DeclarationAssignment")
		{
			var assignmentBranch = treeBranch[0];
			treeBranch.RemoveAt(0);
			assignmentBranch[0] = new("Lambda", [assignmentBranch[0], this.treeBranch], assignmentBranch[0].Container);
			_TBStack[_Stackpos] = assignmentBranch;
			return Default();
		}
		return EndWithAddingOrAssigning(true, treeBranch?.Length ?? 0);
	}

	private bool AssignedExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsLexemOperator(lexems[pos], AssignmentOperatorsList))
		{
			if (treeBranch != null && (treeBranch.Info == "Declaration" || treeBranch.Info == nameof(Hypername)))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(0, [treeBranch, new(lexems[pos - 1].String, pos - 1, pos, container)]);
				_TBStack[_Stackpos]!.Info = treeBranch.Info == "Declaration" ? "DeclarationAssignment" : "Assignment";
			}
			else
				goto l0;
		}
		else
			return EndWithAddingOrAssigning(true, 0);
		return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
	l0:
		GenerateMessage(0x201D, pos, false);
		_TBStack[_Stackpos]?.Insert(0, new TreeBranch("null", pos, pos + 1, container));
		while (!IsLexemOther(lexems[pos], StopLexemsList))
			pos++;
		return Default();
	}

	private bool TetraExpr2_UnaryExpr4_PostfixExpr4() =>
		success ? EndWithAssigning(true) : (_SuccessStack[_Stackpos] = false);

	private bool QuestionExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsLexemOperator(lexems[pos], TernaryOperatorsList))
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		pos++;
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
		_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
		return IncreaseStack("XorExpr", currentTask: nameof(QuestionExpr3), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool QuestionExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsCurrentLexemOperator(":"))
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		pos++;
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
		_TBStack[_Stackpos]?.Add(new(":", pos - 1, pos, container));
		return IncreaseStack("QuestionExpr", currentTask: nameof(QuestionExpr2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool XorExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator("xor"))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		else
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0)
				_TBStack[_Stackpos] = treeBranch;
			else
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			return Default();
		}
		return IncreaseStack("OrExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool EquatedExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator("is"))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new("is", pos - 1, pos, container));
			if (pos < end && IsCurrentLexemKeyword("null"))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), (TreeBranch)new("null", pos - 1, container));
				return Default();
			}
			else
				return IncreaseStack(nameof(Type), currentTask: nameof(EquatedExpr3), applyCurrentTask: true, pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else if (pos < end && IsLexemOperator(lexems[pos], ["==", "!="]))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
		}
		else
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		return IncreaseStack("ComparedExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool EquatedExpr3()
	{
		if (success)
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else
			{
				_ErLStack[_Stackpos].AddRange(errorsList ?? []);
				return Default();
			}
		}
		else
		{
			GenerateMessage(0x201C, pos, false);
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), (TreeBranch)new("null", pos, pos + 1, container));
			return Default();
		}
	}

	private bool LeftAssociativeOperatorExpr2(String newTask, List<String> operatorsList)
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && lexems[pos].Type == LexemType.Operator && operatorsList.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
		}
		else
		{
			if (treeBranch != null && treeBranch.Info.ToString() is "0" or "0i" or "0u" or "0L" or "0uL" or "\"0\"" && _TBStack[_Stackpos] != null && _TBStack[_Stackpos]!.Length != 0 && _TBStack[_Stackpos]![^1].Info.ToString() is "/" or "%")
				GenerateMessage(0x201F, pos, true);
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		}
		return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool RightAssociativeOperatorExpr2(String newTask, List<String> operatorsList)
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && lexems[pos].Type == LexemType.Operator && operatorsList.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(0, [treeBranch ?? TreeBranch.DoNotAdd(), new(lexems[pos - 1].String, pos - 1, pos, container)]);
		}
		else
			return EndWithAddingOrAssigning(true, 0);
		return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool RangeExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsCurrentLexemOperator(".."))
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		if (treeBranch == null)
			treeBranch = new TreeBranch("1", pos, container);
		pos++;
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch);
		_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
		return IncreaseStack(nameof(UnaryExpr), currentTask: nameof(RangeExpr3),
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool RangeExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (treeBranch == null)
		{
			treeBranch = new(nameof(Index), [new("1", pos, container), new("^", pos, container)], container);
			errorsList = null;
		}
		_TBStack[_Stackpos]!.Info = nameof(Range);
		return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
	}

	private bool UnaryExpr()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator && new List<String> { "+", "-", "!", "~" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			isPrefix = true;
		}
		return IncreaseStack(nameof(UnaryExpr2), currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr2()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator && new List<String> { "ln", "#", "$" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			return IncreaseStack(nameof(UnaryExpr2), currentTask: nameof(UnaryExpr3), pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		else if (lexems[pos].Type == LexemType.Operator && new List<String> { "++", "--", "^", "sin", "cos", "tan", "asin", "acos", "atan" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			if (lexems[pos - 1].String == "^")
				_TBStack[_Stackpos]!.Info = "Index";
			isPrefix = true;
		}
		return IncreaseStack("PrefixExpr", currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
		return Default();
	}

	private bool PrefixExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsLexemOperator(lexems[pos], ["++", "--", "!", "!!"]))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String == "!!" ? "!!" : "postfix " + lexems[pos - 1].String, pos - 1, pos, container));
		}
		else
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		_PosStack[_Stackpos] = pos;
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		return Default();
	}

	private bool PostfixExpr2_3(String newTask, String currentTask)
	{
		if (!success)
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
		if (treeBranch != null && treeBranch.Info == nameof(Hypername) && treeBranch.Length == 1
			&& (!WordRegex().IsMatch(treeBranch[0].Info.ToString())
			|| new List<String> { "_", "true", "false", "this", "base", "null", "Infty", "Uncty", "Pi", "E", "List" }
			.Contains(treeBranch[0].Info) || treeBranch[0].Length != 0))
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			_TBStack[_Stackpos] = treeBranch[0];
			return Default();
		}
		else
			return EndWithAssigning(true);
	}

	private bool Hypername()
	{
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		if (IsLexemKeyword(lexems[pos], ["ref", "out"]))
		{
			pos++;
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			return IncreaseStack(nameof(Type), currentTask: task == "HypernameNotCall" ? "HypernameNotCallType" : "HypernameType", applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		if (IsCurrentLexemKeyword("new"))
		{
			if (task == "HypernameNotCall" || _TaskStack[_Stackpos - 1] == "HypernameClosing" || _TaskStack[_Stackpos - 1] == "HypernameNotCallClosing")
				return EndWithError(0x2030, pos, true);
			pos++;
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			if (IsCurrentLexemOther("("))
			{
				_TBStack[_Stackpos] ??= new("Hypername", pos - 1, pos, container);
				_TBStack[_Stackpos]?.Add(new("new", pos - 1, pos, container));
				pos++;
				_TBStack[_Stackpos]?.Add(new("ConstructorCall", pos - 1, pos, container));
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: success);
			}
			return IncreaseStack(nameof(TypeConstraints.NotAbstract), currentTask: "HypernameNew", applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		else if (IsCurrentLexemKeyword("const"))
		{
			pos++;
			return IncreaseStack(nameof(Type), currentTask: task == "HypernameNotCall" ? "HypernameNotCallConstType" : "HypernameConstType", applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		return IncreaseStack(nameof(Type), currentTask: task == "HypernameNotCall" ? "HypernameNotCallType" : "HypernameType", applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
	}

	private bool HypernameNew()
	{
		if (success)
		{
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]![^1].Info = "new type";
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOther("("))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(new("ConstructorCall", pos - 1, pos, container));
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: success);
			}
			else
				return EndWithError(0x200A, pos, true);
		}
		else
		{
			if (errorsList != null)
				_ErLStack[_Stackpos].AddRange(errorsList);
			return Default();
		}
	}

	private bool HypernameConstType()
	{
		if (!success || extra is not UniversalType UnvType)
			return EndWithError(0x2001, pos, true);
		_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		if (lexems[pos].Type != LexemType.Identifier)
			return EndWithError(0x2001, pos, true);
		pos++;
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		AppendBranch("Declaration", new(lexems[pos - 1].String, pos - 1, pos, container) { Extra = UnvType });
		if (!IsCurrentLexemOperator("="))
		{
			GenerateMessage(0x203D, pos, true);
			return EndWithEmpty();
		}
		return HypernameDeclaration(true);
	}

	private bool HypernameType()
	{
		if (success)
		{
			if (extra is not UniversalType UnvType)
				return EndWithError(0x2001, pos, true);
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			if (lexems[pos].Type == LexemType.Identifier)
			{
				pos++;
				_ErLStack[_Stackpos].AddRange(errorsList ?? []);
				AppendBranch("Declaration", new(lexems[pos - 1].String, pos - 1, pos, container) { Extra = UnvType });
				return HypernameDeclaration();
			}
			if (IsCurrentLexemOperator("."))
				return IncreaseStack(task == "HypernameNotCallType" ? "HypernameNotCall" : nameof(Hypername), currentTask: task == "HypernameNotCallType" ? "HypernameNotCallClosing" : "HypernameClosing", pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentBranch: new(".", pos, container), addCurrentBranch: true);
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			if (TypeIsPrimitive(UnvType.MainType) && UnvType.ExtraTypes.Length == 0)
			{
				var newBranch = UserDefinedConstantExists(container, UnvType.MainType.Peek().Name,
					out var constant, out _, out _) && constant.HasValue && constant.Value.DefaultValue != null
					? constant.Value.DefaultValue.Info == "Expr" && constant.Value.DefaultValue.Length == 1
				? new TreeBranch("Expr", constant.Value.DefaultValue, container) : constant.Value.DefaultValue
					: new(UnvType.MainType.Peek().Name, treeBranch?.Pos ?? -1, treeBranch?.Container ?? []);
				_TBStack[_Stackpos]![0] = newBranch;
			}

			return HypernameBracketsAndDot();
		}
		errorsList?.Clear();
		var @ref = false;
		if (IsLexemKeyword(lexems[pos], ["ref", "out"]))
		{
			pos++;
			@ref = true;
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
		}
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			pos++;
			var newBranch = UserDefinedConstantExists(container, lexems[pos - 1].String, out var constant, out _, out _)
				&& constant.HasValue && constant.Value.DefaultValue != null
				? constant.Value.DefaultValue.Info == "Expr" && constant.Value.DefaultValue.Length == 1
				? new TreeBranch("Expr", constant.Value.DefaultValue, container) : constant.Value.DefaultValue
				: new(lexems[pos - 1].String, pos - 1, pos, container);
			AppendBranch(nameof(Hypername), !@ref ? newBranch : new(lexems[pos - 2].String, newBranch, container));
			return HypernameBracketsAndDot();
		}
		if (_TaskStack[_Stackpos - 1].ToString() is not "HypernameClosing" and not "HypernameNotCallClosing")
			return IncreaseStack(nameof(BasicExpr), currentTask: nameof(HypernameBasicExpr), applyCurrentTask: true);
		return EndWithError(0x2001, pos, true);
	}

	private bool HypernameBasicExpr()
	{
		if (success)
			AppendBranch(nameof(Hypername));
		else
		{
			_TBStack[_Stackpos] = null;
			return IsCurrentLexemOther(")") ? Default() : EndWithError(0x2012, pos, true);
		}
		return HypernameBracketsAndDot();
	}

	private bool HypernameCall()
	{
		if (success)
			_TBStack[_Stackpos]?[^1].AddRange(treeBranch?.Elements ?? []);
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther(")"))
			pos++;
		else
			return EndWithError(0x200B, pos, true);
		return HypernameBracketsAndDot();
	}

	private bool HypernameIndexes()
	{
		if (success)
		{
			if (treeBranch != null && treeBranch.Info == "Indexes" && treeBranch.Length != 0)
				_TBStack[_Stackpos]?[^1].AddRange(treeBranch.Elements);
		}
		else
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther("]"))
			pos++;
		else
			return EndWithError(0x200D, pos, true);
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther("("))
		{
			if (task != "HypernameNotCallIndexes")
			{
				pos++;
				_TBStack[_Stackpos]?.Add(new("Call", pos - 1, pos, container));
			}
			else
				return _SuccessStack[_Stackpos] = false;
		}
		else
			goto l0;
		return IncreaseStack("List", currentTask: task == "HypernameNotCallIndexes" ? "HypernameNotCallCall" : nameof(HypernameCall), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	l0:
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOperator("."))
			return IncreaseStack(task == "HypernameNotCallIndexes" ? "HypernameNotCall" : nameof(Hypername), currentTask: task == "HypernameNotCallIndexes" ? "HypernameNotCallClosing" : "HypernameClosing", pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentBranch: new(".", pos, container), addCurrentBranch: true);
		else
		{
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return Default();
		}
	}

	private bool HypernameClosing_BasicExpr4() => success ? pos >= end ? (_SuccessStack[_Stackpos] = false) : EndWithAdding(true) : (_SuccessStack[_Stackpos] = false);

	private bool HypernameDeclaration(bool @const = false)
	{
		if (extra is not UniversalType UnvType)
			return Default();
		if (@const)
		{
			var index = UserDefinedConstantsList.IndexOfKey(container);
			if (index == -1)
			{
				UserDefinedConstantsList.Add(container, []);
				index = UserDefinedConstantsList.IndexOfKey(container);
			}
			var list = UserDefinedConstantsList.Values[index];
			list[lexems[pos - 1].String] = new(UnvType, ConstantAttributes.None, null!);
			return Default();
		}
		else
		{
			var index = VariablesList.IndexOfKey(container);
			if (index == -1)
			{
				VariablesList.Add(container, []);
				index = VariablesList.IndexOfKey(container);
			}
			var list = VariablesList.Values[index];
			list[lexems[pos - 1].String] = UnvType;
			return Default();
		}
	}

	private bool HypernameBracketsAndDot()
	{
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther("("))
		{
			if (!task.StartsWith("HypernameNotCall"))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(new("Call", pos - 1, pos, container));
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: success);
			}
			else
				return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther("["))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new("Indexes", pos - 1, pos, container));
			return IncreaseStack("Indexes", currentTask: task.StartsWith("HypernameNotCall") ? "HypernameNotCallIndexes" : nameof(HypernameIndexes), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: success);
		}
		else if (IsCurrentLexemOperator("."))
			return IncreaseStack(task.StartsWith("HypernameNotCall") ? "HypernameNotCall" : nameof(Hypername), currentTask: task.StartsWith("HypernameNotCall") ? "HypernameNotCallClosing" : "HypernameClosing", pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: success, currentBranch: new(".", pos, container), addCurrentBranch: true);
		else
		{
			if (success)
				_ErLStack[_Stackpos].AddRange(errorsList ?? []);
			return Default();
		}
	}

	private bool Indexes2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOperator(","))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		else
			return EndWithAdding(true);
		return IncreaseStack("LambdaExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool Type()
	{
		var constraints = task.ToString() switch
		{
			nameof(TypeConstraints.BaseClassOrInterface) => TypeConstraints.BaseClassOrInterface,
			nameof(TypeConstraints.NotAbstract) => TypeConstraints.NotAbstract,
			_ => TypeConstraints.None,
		};
		UniversalType UnvType;
		if (pos >= end)
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemKeyword("null"))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			pos++;
			return TypeSingularTuple(UnvType);
		}
		else if (lexems[pos].Type == LexemType.Identifier)
			return IdentifierType(constraints);
		else if (!IsCurrentLexemOther("("))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x2014, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (constraints == TypeConstraints.None)
			return TupleType();
		else
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x2015, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool IdentifierType(TypeConstraints constraints = TypeConstraints.None)
	{
		String s, namespace_ = [], outerClass = [];
		var mainContainer = this.container;
		BlockStack container = new(), innerContainer, innerUserDefinedContainer;
		UniversalType UnvType;
	l0:
		s = lexems[pos].String;
		if (PrimitiveType(constraints, s) is bool b)
			return b;
		if (NamespacesList.Contains(namespace_ == "" ? s : namespace_ + "." + s) || UserDefinedNamespacesList.Contains(namespace_ == "" ? s : namespace_ + "." + s))
		{
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				goto end;
			}
			else if (IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Namespace, s, 1));
				namespace_ = namespace_ == "" ? s : namespace_ + "." + s;
				_PosStack[_Stackpos] = ++pos;
				goto l0;
			}
			else
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateMessage(0x2020, pos, false);
			}
		}
		else if (container.Length == 0 && PrimitiveTypesList.ContainsKey(s))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				goto end;
			}
			if (typeDepth != 0 && s == "var")
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2022, pos, false);
			}
			else
			{
				_PosStack[_Stackpos] = ++pos;
				UnvType = (new(container.ToList().Append(new(BlockType.Primitive, s, 1))), NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
		}
		else if (ExtraTypesList.TryGetValue((namespace_, s), out var netType) || namespace_ == ""
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, s), out netType)) >= 0)
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& (!netType.IsClass || netType.IsSealed))
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				goto end;
			}
			if (constraints == TypeConstraints.NotAbstract && netType.IsAbstract)
			{
				UnvType = (new(container.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				GenerateMessage(0x2023, pos, false, UnvType.ToString());
			}
			var pos2 = pos;
			_PosStack[_Stackpos] = ++pos;
			if (netType.GetGenericArguments().Length == 0)
			{
				UnvType = (new(container.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				goto end;
			}
			else if (IsCurrentLexemOther("["))
				_PosStack[_Stackpos] = ++pos;
			else
			{
				UnvType = NullType;
				GenerateMessage(0x200C, pos, false);
				return TypeSingularTuple(UnvType);
			}
			GeneralArrayParameters template = [];
			GeneralExtraTypes typeParts = [];
			for (var i = 0; i < netType.GetGenericArguments().Length; i++)
				template.Add(new(false, NoGeneralExtraTypes, new([new(BlockType.Primitive, "typename", 1)]), ""));
			typeChainTemplate.Add(template);
			collectionTypes.Add("associativeArray");
			return IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentExtra: (container, s, typeParts));
		}
		else if (GeneralTypesList.TryGetValue((innerContainer = container, s), out var value) || namespace_ == ""
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => GeneralTypesList.TryGetValue((innerContainer = new(x.Split('.').Convert(x =>
			new Block(BlockType.Namespace, x, 1))), s), out value)) >= 0)
		{
			var pos2 = pos;
			var (ArrayParameters, Attributes) = value;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(Attributes))
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				goto end;
			}
			if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static)
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateMessage(0x2024, pos, false, UnvType.ToString());
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not (0 or TypeAttributes.Sealed
				or TypeAttributes.Struct))
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateMessage(0x2023, pos, false, UnvType.ToString());
			}
			_PosStack[_Stackpos] = ++pos;
			if (ArrayParameters.Length == 0)
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				goto end;
			}
			else if (IsCurrentLexemOther("["))
				_PosStack[_Stackpos] = ++pos;
			else
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x200C, pos, false);
				goto end;
			}
			GeneralExtraTypes typeParts = [];
			typeChainTemplate.Add(ArrayParameters);
			collectionTypes.Add("associativeArray");
			return IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentExtra: (innerContainer, s, typeParts));
		}
		else if (UserDefinedTypesList.TryGetValue((innerUserDefinedContainer = container, s), out var value2)
			|| container.Length == 0 && CheckContainer(mainContainer, stack =>
			UserDefinedTypesList.TryGetValue((stack, s), out value2), out innerUserDefinedContainer))
		{
			var pos2 = pos;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(value2.Attributes) && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				goto end;
			}
			if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static
				&& !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateMessage(0x2024, pos, false, UnvType.ToString());
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not (0 or TypeAttributes.Sealed
				or TypeAttributes.Struct) && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateMessage(0x2023, pos, false, UnvType.ToString());
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Class, s, 1));
				outerClass = outerClass == "" ? s : outerClass + "." + s;
				_PosStack[_Stackpos] = ++pos;
				goto l0;
			}
			else
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
		}
		else if (ExtraTypeExists(container, s) || container.Length == 0
			&& CheckContainer(mainContainer, stack => ExtraTypeExists(stack, s), out innerContainer))
		{
			var pos2 = pos;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				goto end;
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2025, pos, false);
			}
			else
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Extra, s, 1))), NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
		}
		else if (IsNotImplementedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s])) || IsNotImplementedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x202A, pos, false, s);
		}
		else if (IsNotImplementedEndOfIdentifier(s, out var s2))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x202B, pos, false, s2);
		}
		else if (IsOutdatedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]), out var useInstead))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x202C, pos, false, s, useInstead);
		}
		else if (IsOutdatedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s, out useInstead))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x202D, pos, false, s, useInstead);
		}
		else if (IsOutdatedEndOfIdentifier(s, out useInstead, out s2))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x202E, pos, false, s2, useInstead);
		}
		else if (IsReservedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s])) || IsReservedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x203A, pos, false, s);
		}
		else if (IsReservedEndOfIdentifier(s, out s2))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x203B, pos, false, s2);
		}
		else
		{
			if (container.Length == 0)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2026, pos, false, String.Join(".", [.. container.ToList().Convert(X => X.Name), s]));
			}
			else
			{
				pos--;
				UnvType = new UniversalType(container, NoGeneralExtraTypes);
				return TypeSingularTuple(UnvType);
			}
		}
	end:
		return _SuccessStack[_Stackpos] = false;
	}

	private bool IdentifierType2()
	{
		typeChainTemplate.RemoveAt(^1);
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not (BlockStack innerContainer, String s, GeneralExtraTypes types))
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		UniversalType UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))), types);
		return TypeComma(UnvType);
	}

	private bool? PrimitiveType(TypeConstraints constraints, String s)
	{
		UniversalType UnvType;
		if (s.ToString() is "short" or "long")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && (lexems[pos].String == "char" || lexems[pos].String == "int"
				/*|| s == "long" && (lexems[pos].input == "long" || lexems[pos].input == "real")*/))
			{
				UnvType = (new([new(BlockType.Primitive, s + " " + lexems[pos].String, 1)]), NoGeneralExtraTypes);
				_PosStack[_Stackpos] = ++pos;
				return TypeSingularTuple(UnvType);
			}
			else
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2027, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
		}
		else if (s == "unsigned")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			String mediumWord = [];
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && (lexems[pos].String == "short" || lexems[pos].String == "long"))
			{
				mediumWord = lexems[pos].String + " ";
				_PosStack[_Stackpos] = ++pos;
			}
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "int"/* || lexems[pos].input == "long"*/)
			{
				UnvType = (new([new(BlockType.Primitive, s + " " + mediumWord + lexems[pos].String, 1)]), NoGeneralExtraTypes);
				_PosStack[_Stackpos] = ++pos;
				return TypeSingularTuple(UnvType);
			}
			else
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2028, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
		}
		else if (s == "list")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = NullType;
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			if (pos > 0 && lexems[pos - 1].String == ".")
				return null;
			if (collectionTypes[^1] == "list")
				GenerateMessage(0x8003, pos, false);
			_PosStack[_Stackpos] = ++pos;
			GeneralExtraTypes typeParts = [];
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (IsCurrentLexemOther("("))
				_PosStack[_Stackpos] = ++pos;
			else
			{
				UnvType = NullType;
				GenerateMessage(0x200A, pos, false);
				goto list0;
			}
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Int)
			{
				_ExtraStack[_Stackpos - 1] = typeParts;
				typeParts.Add(new(lexems[pos].String, 0, []));
				_PosStack[_Stackpos] = ++pos;
			}
			else if (lexems[pos].Type is LexemType.UnsignedInt or LexemType.LongInt
				or LexemType.UnsignedLongInt or LexemType.Real or LexemType.String)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
				GenerateMessage(0x2017, pos, false);
				_PosStack[_Stackpos] = ++pos;
			}
			if (pos >= end)
			{
				UnvType = NullType;
				_ExtraStack[_Stackpos - 1] = UnvType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (IsCurrentLexemOther(")"))
			{
				_PosStack[_Stackpos] = ++pos;
				goto list1;
			}
			else
				return IncreaseStack("Expr", currentTask: nameof(TypeList), pos_: pos,
					applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		list0:
			typeDepth++;
			CloseBracket(ref pos, ")", ref errorsList!, false, end);
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeListFail), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		list1:
			typeDepth++;
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeList2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		UnvType = NullType;
		return null;
	}

	private bool TypeList()
	{
		if (!success || treeBranch == null)
		{
			GenerateMessage(0x200E, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
		var targetBranch = treeBranch.Info == "Expr" && treeBranch.Length == 1 ? treeBranch[0] : treeBranch;
		if (!(treeBranch.Extra == null || targetBranch.Extra is UniversalType UnvType
			&& TypesAreCompatible(UnvType, IntType, out var warning, [], out _, out _) && !warning))
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x2017, pos, false);
			goto list0;
		}
		else if (IsCurrentLexemOther(")"))
		{
			if (_ExtraStack[_Stackpos - 1] is not GeneralExtraTypes typeParts)
				_ExtraStack[_Stackpos - 1] = typeParts = [];
			typeParts.Add(Universal.TryParse(targetBranch.Info.ToString(), out var value)
				? new(value.ToString(true), targetBranch.Pos, []) : treeBranch);
			_PosStack[_Stackpos] = ++pos;
			typeDepth++;
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeList2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x200B, pos, false);
			goto list0;
		}
	list0:
		typeDepth++;
		CloseBracket(ref pos, ")", ref errorsList!, false, end);
		collectionTypes.Add("list");
		return IncreaseStack(nameof(Type), currentTask: nameof(TypeListFail), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool TypeList2()
	{
		typeDepth--;
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not UniversalType InnerUnvType)
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is not GeneralExtraTypes typeParts)
			typeParts = [];
		UniversalType UnvType = new(ListBlockStack,
			[.. typeParts, new("type", pos, container) { Extra = InnerUnvType }]);
		return TypeSingularTuple(UnvType);
	}

	private bool TypeListFail()
	{
		typeDepth--;
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not UniversalType InnerUnvType)
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is not GeneralExtraTypes typeParts)
			typeParts = [];
		_ExtraStack[_Stackpos - 1] = new UniversalType(ListBlockStack,
			[.. typeParts, new("type", pos, container) { Extra = InnerUnvType }]);
		return Default();
	}

	private bool TupleType()
	{
		_PosStack[_Stackpos] = ++pos;
		typeChainTemplate.Add([new(true, NoGeneralExtraTypes, new([new(BlockType.Primitive, "typename", 1)]), "")]);
		collectionTypes.Add("tuple");
		return IncreaseStack(nameof(TypeChain), currentTask: nameof(TupleType2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool TupleType2()
	{
		UniversalType UnvType;
		if (!success || extra is not GeneralExtraTypes innerArrayParameters)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther(")"))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x200B, pos, false);
			CloseBracket(ref pos, ")", ref errorsList, false, end);
			return _SuccessStack[_Stackpos] = false;
		}
		UnvType = (new([new(BlockType.Primitive, "tuple", 1)]), innerArrayParameters);
		return TypeSingularTuple(UnvType);
	}

	private bool TypeChain()
	{
		GeneralExtraTypes types = [];
		if (typeChainTemplate[^1].Length == 0)
		{
			types = NoGeneralExtraTypes;
			return _SuccessStack[_Stackpos] = false;
		}
		return IncreaseStack(nameof(TypeChainIteration), currentTask: nameof(TypeChain2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentExtra: types);
	}

	private bool TypeChain2()
	{
		if (!success || _TaskStack[_Stackpos] == nameof(IdentifierType2)
			&& _ExtraStack[_Stackpos - 1] is not (BlockStack, String, GeneralExtraTypes)
			|| extra is not GeneralExtraTypes tempTypes)
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is (BlockStack, String, GeneralExtraTypes types))
			types.AddRange(tempTypes);
		else
			_ExtraStack[_Stackpos - 1] = tempTypes;
		return Default();
	}

	private bool TypeChainIteration()
	{
		if (_ExtraStack[_Stackpos - 1] is not GeneralExtraTypes types)
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		if (TypeIsPrimitive(typeChainTemplate[^1][0].ArrayParameterType) && typeChainTemplate[^1][0].ArrayParameterType.Peek().Name == "typename")
		{
			tpos.Add(0);
			typeDepth++;
			collectionTypes.Add(collectionTypes[^1]);
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeChainIteration2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else if (TypeIsPrimitive(typeChainTemplate[^1][tpos[^1]].ArrayParameterType) && typeChainTemplate[^1][tpos[^1]].ArrayParameterType.Peek().Name == "int")
		{
			var minus = false;
			if (pos >= end)
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (IsCurrentLexemOperator("-"))
			{
				minus = true;
				_PosStack[_Stackpos] = ++pos;
			}
			if (pos >= end)
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Int)
				_PosStack[_Stackpos] = ++pos;
			else
			{
				types = NoGeneralExtraTypes;
				GenerateMessage(0x2016, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			types.Add(new((minus ? "-" : "") + lexems[pos - 1].String, pos - 1, container));
		}
		_TaskStack[_Stackpos] = nameof(TypeChainIteration2);
		return true;
	}

	private bool TypeChainIteration2()
	{
		if (extra is not UniversalType InnerUnvType || _ExtraStack[_Stackpos - 1] is not GeneralExtraTypes types)
		{
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		if (!success)
		{
			ReduceStack();
			types.Add(new("type", pos, container) { Extra = InnerUnvType });
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		String itemName = [];
		if (pos >= end)
		{
			ReduceStack();
			types = NoGeneralExtraTypes;
			_ExtraStack[_Stackpos - 1] = types;
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (lexems[pos].Type == LexemType.Identifier)
		{
			itemName = lexems[pos].String;
			_PosStack[_Stackpos] = ++pos;
		}
		TreeBranch branch = new("type", pos - 1, container) { Extra = InnerUnvType };
		if (collectionTypes[^1] == "tuple" && TypeEqualsToPrimitive(InnerUnvType, "tuple", false)
			&& InnerUnvType.ExtraTypes.Length == 2 && InnerUnvType.ExtraTypes[1].Info != "type"
			&& int.TryParse(InnerUnvType.ExtraTypes[1].Info.ToString(), out _))
			types.AddRange(InnerUnvType.ExtraTypes.Values);
		else if (itemName != "")
		{
			if (!types.TryAdd(itemName, branch))
				types.Add(branch);
		}
		else
			types.Add(branch);
		if (pos >= end)
		{
			ReduceStack();
			types = NoGeneralExtraTypes;
			_ExtraStack[_Stackpos - 1] = types;
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOperator(","))
		{
			if (typeChainTemplate[^1][tpos[^1]].ArrayParameterPackage == false)
			{
				tpos[^1]++;
				if (tpos[^1] >= typeChainTemplate[^1].Length)
				{
					ReduceStack();
					return Default();
				}
			}
			return IncreaseStack(nameof(Type), pos_: pos + 1, applyPos: true, applyCurrentErl: true);
		}
		else
		{
			if (tpos[^1] >= typeChainTemplate[^1].Length - 1 || tpos[^1] >= typeChainTemplate[^1].Length - 2 && typeChainTemplate[^1][tpos[^1] + 1].ArrayParameterPackage)
			{
				ReduceStack();
				_ExtraStack[_Stackpos - 1] = types;
				return Default();
			}
			else
			{
				ReduceStack();
				types = NoGeneralExtraTypes;
				_ExtraStack[_Stackpos - 1] = types;
				GenerateMessage(0x2018, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
		}
		void ReduceStack()
		{
			typeDepth--;
			tpos.RemoveAt(^1);
			collectionTypes.RemoveAt(^1);
		}
	}

	private bool TypeSingularTuple(UniversalType UnvType)
	{
		if (pos < end && IsCurrentLexemOther("["))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
			return Default();
		}
		return TypeInt(UnvType);
	}

	private bool TypeComma(UniversalType UnvType)
	{
		if (pos < end && IsCurrentLexemOther(","))
			_PosStack[_Stackpos] = ++pos;
		else
			return TypeClosing(UnvType);
		return TypeInt(UnvType);
	}

	private bool TypeInt(UniversalType UnvType)
	{
		if (pos >= end)
		{
			UnvType = NullType;
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos].String.ToString(), out var number))
		{
			if (number == 0)
				GenerateMessage(0x8004, pos, false);
			else if (number >= 2)
			{
				var UnvType2 = UnvType;
				var pos2 = pos - 1;
				UnvType = new(TupleBlockStack, new(RedStarLinq.Fill(number, _ =>
					new TreeBranch("type", pos2, container) { Extra = UnvType2 })));
			}
			_PosStack[_Stackpos] = ++pos;
		}
		else
		{
			UnvType = NullType;
			GenerateMessage(0x2016, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
		return TypeClosing(UnvType);
	}

	private bool TypeClosing(UniversalType UnvType)
	{
		if (pos >= end)
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther("]"))
		{
			_PosStack[_Stackpos] = ++pos;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = UnvType };
			return Default();
		}
		else
		{
			UnvType = NullType;
			_ExtraStack[_Stackpos - 1] = UnvType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = UnvType };
			GenerateMessage(0x200D, pos, false);
			CloseBracket(ref pos, "]", ref errorsList, false, end);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool BasicExpr()
	{
		var s = lexems[pos].String;
		if (lexems[pos].Type == LexemType.Keyword && new List<String> { "true", "false", "this", "null" }.Contains(s)
			|| lexems[pos].Type == LexemType.Operator && new List<String> { "Infty", "-Infty", "Uncty", "Pi", "E" }.Contains(s))
		{
			pos++;
			_TBStack[_Stackpos] = new(s, pos - 1, pos, container)
			{
				Extra = s.ToString() switch
				{
					"null" => NullType,
					"true" or "false" => BoolType,
					"this" => new(new(container), NoGeneralExtraTypes),
					_ => RealType,
				}
			};
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Int)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'i' ? s : s.Add('i'), pos - 1, pos, container)
			{
				Extra = byte.TryParse(CreateVar(s[..^1].ToString(), out var systemString), out _) ? ByteType
				: short.TryParse(systemString, out _) ? ShortIntType
				: ushort.TryParse(systemString, out _) ? UnsignedShortIntType : IntType
			};
			return Default();
		}
		else if (lexems[pos].Type == LexemType.UnsignedInt)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'u' ? s : s.Add('u'), pos - 1, pos, container) { Extra = UnsignedIntType };
			return Default();
		}
		else if (lexems[pos].Type == LexemType.LongInt)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'L' ? s : s.Add('L'), pos - 1, pos, container) { Extra = LongIntType };
			return Default();
		}
		else if (lexems[pos].Type == LexemType.UnsignedLongInt)
		{
			pos++;
			_TBStack[_Stackpos] = new(s.EndsWith("uL") ? s : s.AddRange("uL"), pos - 1, pos, container)
			{
				Extra = UnsignedLongIntType
			};
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Real)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'r' ? s : s.Add('r'), pos - 1, pos, container) { Extra = RealType };
			return Default();
		}
		else if (lexems[pos].Type == LexemType.String)
		{
			pos++;
			_TBStack[_Stackpos] = new(s, pos - 1, pos, container) { Extra = StringType };
			return Default();
		}
		else if (IsCurrentLexemOther("("))
		{
			pos++;
			if (IsCurrentLexemOther(")"))
			{
				_TBStack[_Stackpos] = new("List", pos - 1, container);
				pos++;
				return Default();
			}
			return IncreaseStack("Expr", currentTask: nameof(BasicExpr2), pos_: pos, applyPos: true,
				applyCurrentTask: true, currentBranch: new("Expr", pos, container), assignCurrentBranch: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool BasicExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther(")"))
			pos++;
		else
			return EndWithError(0x200B, pos, true);
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else
			goto l0;
		l0:
		return pos >= end ? (_SuccessStack[_Stackpos] = false) : EndWithAssigning(true);
	}

	private bool Default()
	{
		_SuccessStack[_Stackpos] = true;
		return false;
	}

	private void SkipSemicolonsAndNewLines()
	{
		while (pos < end && lexems[pos].Type == LexemType.Other && (lexems[pos].String == ";" || lexems[pos].String == "\r\n"))
		{
			pos++;
			if (lexems[pos - 1].String == ";" && _TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length >= 1 && new List<String> { "if", "if!", "else", "else if", "else if!", "while", "while!", "repeat", "for", "loop" }.Contains(_TBStack[_Stackpos]?[^1].Info ?? "") && treeBranch == null)
				AppendBranch(nameof(Main), new(nameof(Main), pos - 1, pos, container));
		}
	}

	private void TransformErrorMessage()
	{
		_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_ErLStack[_Stackpos + 1] = [];
	}

	private void TransformErrorMessage2()
	{
		TransformErrorMessage();
		_TBStack[_Stackpos + 1] = null;
	}

	private void TransformErrorMessageAndAppendBranch(String string_)
	{
		TransformErrorMessage();
		AppendBranch(string_);
	}

	private bool EndWithError(ushort code, Index pos, bool result, params dynamic[] parameters)
	{
		GenerateMessage(code, pos, true, parameters);
		_SuccessStack[_Stackpos] = result;
		return false;
	}

	private bool EndWithAdding(bool addError)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		if (treeBranch != null)
			_TBStack[_Stackpos]?.Add(treeBranch);
		return Default();
	}

	private bool EndWithAssigning(bool addError)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_TBStack[_Stackpos] = treeBranch;
		return Default();
	}

	private bool EndWithAddingOrAssigning(bool addError, int posToInsert)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0
			&& !(task == nameof(Expr2) && _TaskStack[_Stackpos - 1] != nameof(BasicExpr2) && treeBranch != null
			&& treeBranch.Info.ToString() is not ("Expr" or "Assignment" or "DeclarationAssignment")))
			_TBStack[_Stackpos] = treeBranch;
		else
			_TBStack[_Stackpos]?.Insert(posToInsert, treeBranch ?? TreeBranch.DoNotAdd());
		return Default();
	}

	private bool EndWithEmpty(bool addError = false)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		_TBStack[_Stackpos] = null;
		return Default();
	}

	private void CreateObjectList(out List<object>? l) => l = (List<object>?)_ExtraStack[_Stackpos - 1];

	public void AppendBranch(String newInfo) => AppendBranch(newInfo, treeBranch ?? TreeBranch.DoNotAdd());

	public void AppendBranch(String newInfo, TreeBranch newBranch)
	{
		if (_TBStack[_Stackpos] == null)
			_TBStack[_Stackpos] = new(newInfo, newBranch, container);
		else
		{
			_TBStack[_Stackpos]!.Info = newInfo;
			_TBStack[_Stackpos]?.Add(newBranch);
		}
	}

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errorsList, bool produceWreck, int end = -1)
	{
		while (pos < (end == -1 ? lexems.Length : end))
		{
			if (CloseBracketIteration(ref pos, bracket, ref errorsList, produceWreck) is bool b)
				return b;
		}
		return false;
	}

	private bool? CloseBracketIteration(ref int pos, String bracket, ref List<String>? errorsList, bool produceWreck)
	{
		if (lexems[pos].Type == LexemType.Other)
		{
			var s = lexems[pos].String;
			if (s == bracket)
			{
				pos++;
				return true;
			}
			else if (new List<String> { "(", "[", "{" }.Contains(s))
			{
				pos++;
				CloseBracket(ref pos, s == "(" ? ")" : s == "[" ? "]" : "}", ref errorsList, produceWreck);
			}
			else if (new List<String> { ")", "]", "}" }.Contains(s) || bracket != "}" && (s == ";" || s == "\r\n"))
			{
				if (produceWreck)
					GenerateMessage(0x9011, pos, false, bracket);
				return false;
			}
			else
				pos++;
		}
		else
			pos++;
		return null;
	}

	private void GenerateMessage(ushort code, Index pos, bool savePrevious, params dynamic[] parameters)
	{
		if (savePrevious)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		DeclaredConstructions.GenerateMessage(_ErLStack[_Stackpos], code, lexems[pos].LineN, lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			wreckOccurred = true;
	}

	private void GenerateUnexpectedEndError(bool savePrevious = false)
	{
		if (savePrevious)
			_ErLStack[_Stackpos].AddRange(errorsList ?? []);
		DeclaredConstructions.GenerateMessage(_ErLStack[_Stackpos], 0x2000, lexems[pos - 1].LineN, lexems[pos - 1].Pos + lexems[pos - 1].String.Length);
	}

	private void GenerateUnexpectedEndOfTypeError(ref List<String>? errorsList) => errorsList?.Add("Error in line " + lexems[pos - 1].LineN.ToString() + " at position " + (lexems[pos - 1].Pos + lexems[pos - 1].String.Length).ToString() + ": unexpected end of type reached");

	[GeneratedRegex("^[A-Za-zА-Яа-я_][0-9A-Za-zА-Яа-я_]*$")]
	private static partial Regex WordRegex();
}
