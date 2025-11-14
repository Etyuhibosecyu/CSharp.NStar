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
	private readonly List<ExtendedRestrictions> typeChainTemplate = new(16);
	private readonly NList<int> tpos = new(16);
	private int globalUnnamedIndex = 1;
	private int _Stackpos;

	private static readonly Dictionary<String, (String Next, String TreeLabel, List<String> Operators)> operatorsMapping = new()
	{
		{ "Members", ("Member", "Members", []) }, { "Expr", ("List", "Expr", []) }, { "List", ("LambdaExpr", "List", []) },
		{ "LambdaExpr", ("AssignedExpr", "Expr", []) }, { "Switch", ("AssignedExpr", "Expr", []) },
		{ "AssignedExpr", ("QuestionExpr", "Expr", []) },
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
		{ "TetraExpr", ("RangeExpr", "Expr", []) }, { "RangeExpr", (nameof(UnaryExpr), "Expr", []) },
		{ "PrefixExpr", ("PostfixExpr", "Expr", []) }
	};
	private static readonly G.Dictionary<String, dynamic> attributesMapping = new()
	{
		{ "ref", ParameterAttributes.Ref }, { "out", ParameterAttributes.Out },
		{ "params", ParameterAttributes.Params }, { "private", PropertyAttributes.Private },
		{ "protected", PropertyAttributes.Protected }, { "internal", PropertyAttributes.Internal },
		{ "public", PropertyAttributes.None }
	};

	public MainParsing(LexemStream lexemStream, bool wreckOccurred) : base(lexemStream)
	{
		this.wreckOccurred = wreckOccurred;
		_ErLStack[0] = errors ?? [];
		_EndStack[0] = lexems.Length;
	}

	internal (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) MainParse()
	{
		try
		{
			while (_Stackpos >= 0)
			{
				pos = _PosStack[_Stackpos];
				start = _StartStack[_Stackpos];
				end = _EndStack[_Stackpos];
				task = _TaskStack[_Stackpos];
				errors = _ErLStack[_Stackpos + 1];
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
				_PosStack.RemoveAt(^1);
				_StartStack.RemoveAt(^1);
				_EndStack.RemoveAt(^1);
				_TaskStack.RemoveAt(^1);
				_ErLStack.RemoveAt(^1);
				_TBStack.RemoveAt(^1);
				_ExtraStack.RemoveAt(^1);
				_ContainerStack.RemoveAt(^1);
				_SuccessStack.RemoveAt(^1);
				_BTJPStack.RemoveAt(^1);
				_RTPStack.RemoveAt(^1);
				_PLPStack.RemoveAt(^1);
				_Stackpos--;
			}
			(errors ??= []).AddRange(_ErLStack[0] ?? []);
			if (_SuccessStack[0])
				return (lexems, input, _TBStack[0] ?? new(nameof(Main), 0, []), errors, wreckOccurred);
			else
			{
				(errors ??= []).Add(GetWreckPosPrefix(0xF001, ^1) + ": main parsing failed because of internal error");
				return RaiseWreck();
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			var targetPos = _Stackpos >= 0 ? _PosStack[_Stackpos] : lexems.Length - 1;
			if (targetPos >= lexems.Length)
				targetPos--;
			(errors ??= []).Add(GetWreckPosPrefix(0xF000, targetPos)
				+ ": compilation failed because of internal compiler error\r\n");
			return RaiseWreck();
		}
	}

	private String GetWreckPosPrefix(ushort code, Index pos) =>
		"Technical wreck " + Convert.ToString(code, 16).ToUpper().PadLeft(4, '0')
		+ "in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString();

	private (List<Lexem> Lexems, String String, TreeBranch TopBranch,
		List<String>? ErrorsList, bool WreckOccurred) RaiseWreck() => EmptySyntaxTree();

	private delegate bool ParseAction();

	private bool MainParseAction()
	{
		(String Next, String TreeLabel, List<String> Operators) taskWithoutIndex;
		var task = this.task.ToString();
		return task switch
		{
			"Parameters" => IncreaseStack(nameof(Parameter), currentTask: "Parameters2",
				applyCurrentTask: true, currentExtra: new List<object>()),
			"Members" or "Expr" or "List" or "LambdaExpr" or "Switch" or "AssignedExpr" or "QuestionExpr" or "XorExpr"
				or "OrExpr" or "AndExpr" or "Xor2Expr" or "Or2Expr" or "And2Expr" or "EquatedExpr" or "ComparedExpr"
				or "BitwiseXorExpr" or "BitwiseOrExpr" or "BitwiseAndExpr" or "BitwiseShiftExpr" or "PMExpr" or "MulDivExpr"
				or "PowExpr" or "TetraExpr" or "RangeExpr" or "PrefixExpr" =>
				IncreaseStack(operatorsMapping[this.task].Next, currentTask: this.task + "2", applyCurrentTask: true,
				currentBranch: new(operatorsMapping[this.task].TreeLabel, pos, pos + 1, container), assignCurrentBranch: true),
			"Member" => IncreaseStack(nameof(Property), currentTask: "Member2", applyCurrentTask: true,
				currentExtra: new List<object>()),
			"ActionChain2" => ActionChain2_3_4(nameof(Cycle), "ActionChain3"),
			"ActionChain3" => ActionChain2_3_4(nameof(SpecialAction), "ActionChain4"),
			"ActionChain4" => ActionChain2_3_4(nameof(Return), "ActionChain5"),
			"OrExpr2" or "AndExpr2" or "Xor2Expr2" or "Or2Expr2" or "And2Expr2" or "ComparedExpr2" or "BitwiseXorExpr2"
				or "BitwiseOrExpr2" or "BitwiseAndExpr2" or "BitwiseShiftExpr2" or "PMExpr2" or "MulDivExpr2" =>
				LeftAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next,
				taskWithoutIndex.Operators),
			"PowExpr2" => RightAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next,
				taskWithoutIndex.Operators),
			"PostfixExpr" => IncreaseStack(nameof(Hypername), currentTask: "PostfixExpr3", applyCurrentTask: true,
				currentBranch: new("Expr", pos, pos + 1, container), assignCurrentBranch: true),
			"PostfixExpr2" => PostfixExpr2_3(nameof(Type), "PostfixExpr3"),
			"PostfixExpr3" => PostfixExpr2_3(nameof(BasicExpr), "PostfixExpr4"),
			"Indexes" => IncreaseStack("LambdaExpr", currentTask: "Indexes2", applyCurrentTask: true,
				currentBranch: new("Indexes", pos, pos + 1, container), assignCurrentBranch: true),
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
		nameof(MainClosing) => MainClosing,
		nameof(Namespace) => Namespace,
		nameof(NamespaceClosing) => NamespaceClosing,
		nameof(Class) => Class,
		nameof(Class2) => Class2,
		nameof(ClassClosing) => ClassClosing,
		nameof(Function) => Function,
		nameof(Function2) => Function2,
		nameof(Function3) => Function3,
		nameof(FunctionClosing) => FunctionClosing,
		nameof(Constructor) => Constructor,
		nameof(Constructor2) => Constructor2,
		nameof(ConstructorClosing) => ConstructorClosing,
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
		nameof(LambdaExpr5) => LambdaExpr5,
		nameof(Switch2) => Switch2,
		"Switch3" or "Switch4" => Switch3_4,
		nameof(Switch5) => Switch5,
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
		nameof(HypernameType) or "HypernameNotCallType" => HypernameType,
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
		nameof(TypeInt2) => TypeInt2,
		nameof(BasicExpr) => BasicExpr,
		nameof(BasicExpr2) => BasicExpr2,
		_ => Default,
	};

	private bool IncreaseStack(String newTask, String? currentTask = null, int pos_ = -1, bool applyPos = false,
		bool applyCurrentTask = false, int start_ = -1, int end_ = -1, List<String>? erl = null, bool applyCurrentErl = false,
		TreeBranch? currentBranch = null, bool addCurrentBranch = false, bool assignCurrentBranch = false,
		TreeBranch? newBranch = null, object? currentExtra = null, object? newExtra = null,
		BlockStack? container_ = null, int btjp = -1, int rtp = -1, int plp = -1)
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		_StartStack.Add(pos_);
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
			return IncreaseStack(nameof(Main), currentTask: nameof(MainClosing), pos_: pos + 1, applyPos: true,
				applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed,
				"#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: nameof(Main5), applyPos: true, applyCurrentTask: true);
	}

	private bool Main2() => success ? NewMainTask() : IncreaseStack(nameof(Class),
		currentTask: nameof(Main3), applyCurrentTask: true);

	private bool Main3() => success ? NewMainTask() : IncreaseStack(nameof(Function),
		currentTask: nameof(Main4), applyCurrentTask: true);

	private bool Main4()
	{
		if (success)
			return NewMainTask();
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: nameof(MainClosing), pos_: pos + 1, applyPos: true,
				applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed,
				"#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: nameof(Main5), applyCurrentTask: true);
	}

	private bool Main5()
	{
		if (!success)
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			return Default();
		}
		_TaskStack[_Stackpos] = nameof(Main);
		TransformErrorMessage();
		if (treeBranch != null && !(treeBranch.Name == "" && treeBranch.Length == 0))
		{
			if (_TBStack[_Stackpos] == null)
				_TBStack[_Stackpos] = new(nameof(Main), treeBranch.Name.ToString() is nameof(Main) or nameof(ActionChain)
					? treeBranch.Elements : [treeBranch], container);
			else if (_TBStack[_Stackpos]!.Name == nameof(Main) && treeBranch.Name.ToString() is nameof(Class)
				or nameof(Function) or nameof(Constructor))
				_TBStack[_Stackpos]!.Add(treeBranch);
			else
			{
				_TBStack[_Stackpos]!.Name = nameof(Main);
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
		if ((_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0) && (treeBranch == null
			|| treeBranch.Length <= 1 && treeBranch[0].Name == nameof(Main)))
			_TBStack[_Stackpos] = treeBranch;
		else if (_TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length >= 1
			&& new List<String> { "if", "else", "else if", "while", "repeat", "for", "loop" }
			.Contains(_TBStack[_Stackpos]?[^1].Name ?? "") && treeBranch == null)
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
			return CheckOpeningBracketAndAddTask(nameof(Main), nameof(NamespaceClosing), BlockType.Namespace);
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
			var preservedPos = pos;
			CloseBracket(ref pos, "}", ref errors, true, end);
			GenerateMessage(0x2004, preservedPos, true);
			return EndWithAdding(false);
		}
	}

	private bool Class()
	{
		if (!CheckBlockToJump(nameof(Class)))
			return _SuccessStack[_Stackpos] = false;
		pos = blocksToJump[blocksToJumpPos].End;
		_TBStack[_Stackpos] = new(nameof(Class), new TreeBranch(blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container),
			container);
		if (CheckClassSubordination())
			return CheckColonAndAddTask(nameof(TypeConstraints.BaseClassOrInterface), nameof(Class2), BlockType.Class);
		else
			return CheckOpeningBracketAndAddTask(nameof(ClassMain), nameof(ClassClosing), BlockType.Class);
	}

	private bool Class2()
	{
		CheckSuccess();
		TransformErrorMessage2();
		pos = registeredTypes[registeredTypesPos].End;
		return CheckOpeningBracketAndAddTask(nameof(ClassMain), nameof(ClassClosing), BlockType.Class,
			registeredTypes[registeredTypesPos++].Name);
		void CheckSuccess()
		{
			if (!(success && extra is NStarType NStarType))
			{
				_TBStack[_Stackpos]?.Add(new TreeBranch("type", registeredTypes[registeredTypesPos].Start,
					registeredTypes[registeredTypesPos].End, container) { Extra = NullType });
				return;
			}
			var t = UserDefinedTypes[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)];
			t.BaseType = NStarType;
			UserDefinedTypes[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
	}

	private bool ClassClosing()
	{
		if (IsClosingFigureBracket())
			return EndWithAdding(true);
		else
		{
			var preservedPos = pos;
			CloseBracket(ref pos, "}", ref errors, true, end);
			GenerateMessage(0x2004, preservedPos, true);
			return EndWithAdding(false);
		}
	}

	private bool Function()
	{
		if (CheckBlockToJump(nameof(Function)))
		{
			if (registeredTypesPos < registeredTypes.Length && blocksToJump[blocksToJumpPos].Start >= pos
				&& blocksToJump[blocksToJumpPos].End <= end)
				return IncreaseStack(nameof(Type), currentTask: nameof(Function2), pos_: blocksToJump[blocksToJumpPos].Start,
					applyCurrentTask: true, start_: blocksToJump[blocksToJumpPos].Start,
					end_: blocksToJump[blocksToJumpPos].End, currentExtra: new NStarType(new BlockStack(), NoBranches),
					rtp: registeredTypesPos + 1);
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
		if (!CheckBlockToJump2(nameof(Function)))
			return _SuccessStack[_Stackpos] = false;
		CheckSuccess();
		TransformErrorMessage2();
		if (parameterListsPos < parameterLists.Length
			&& parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start
			&& parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
			return IncreaseStack("Parameters", currentTask: nameof(Function3), pos_: parameterLists[parameterListsPos].Start,
				applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start,
				end_: parameterLists[parameterListsPos].End, currentExtra: new ExtendedMethodParameters(),
				plp: parameterListsPos + 1);
		else
		{
			_TaskStack[_Stackpos] = nameof(Function3);
			_ExtraStack[_Stackpos] = new ExtendedMethodParameters();
			return true;
		}
		void CheckSuccess()
		{
			if (success && extra is NStarType NStarType)
			{
				var container2 = blocksToJump[blocksToJumpPos].Container;
				var name = blocksToJump[blocksToJumpPos].Name;
				var start = blocksToJump[blocksToJumpPos].Start;
				var index = UserDefinedFunctionIndexes[container2][start];
				var t = UserDefinedFunctions[container2][name][index];
				t.ReturnNStarType = NStarType;
				UserDefinedFunctions[container2][name][index] = t;
				_TBStack[_Stackpos] = new(nameof(Function), [new(name, start, blocksToJump[blocksToJumpPos].End, container),
					treeBranch ?? TreeBranch.DoNotAdd()], container);
				return;
			}
			_TBStack[_Stackpos] = new(nameof(Function), [new(blocksToJump[blocksToJumpPos].Name,
				blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container),
				new("type", blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container)
				{
					Extra = NullType
				}], container);
		}
	}

	private bool Function3()
	{
		if (!CheckBlockToJump2(nameof(Function)))
			return _SuccessStack[_Stackpos] = false;
		CheckSuccess();
		TransformErrorMessage2();
		pos = blocksToJump[blocksToJumpPos].End;
		if (success && (UserDefinedFunctions[blocksToJump[blocksToJumpPos].Container]
			[blocksToJump[blocksToJumpPos].Name][0].Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
		{
			SkipSemicolonsAndNewLines();
			blocksToJumpPos++;
			return EndWithAdding(true);
		}
		else
			return CheckOpeningBracketAndAddTask(nameof(Main), nameof(FunctionClosing), BlockType.Function);
		void CheckSuccess()
		{
			if (!success || extra is not ExtendedMethodParameters parameters)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1,
					blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			var blockToJumpContainer = blocksToJump[blocksToJumpPos].Container;
			var name = blocksToJump[blocksToJumpPos].Name;
			var start = blocksToJump[blocksToJumpPos].Start;
			var index = UserDefinedFunctionIndexes[blockToJumpContainer][start];
			var t = UserDefinedFunctions[blockToJumpContainer][name][index];
			if (UserDefinedFunctions[blockToJumpContainer][name].Take(index)
				.Exists(x => x.Parameters.Length == parameters.Length
				&& x.Parameters.Combine(parameters).All(y => y.Item1.Type.Equals(y.Item2.Type)
				&& (y.Item1.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)) == 0
				&& (y.Item2.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)) == 0)))
			{
				GenerateMessage(0x2032, start, false, name, lexems[start].LineN, lexems[start].Pos);
				t.Attributes |= FunctionAttributes.Wrong;
				UserDefinedFunctions[blockToJumpContainer][name][index] = t;
				return;
			}
			if (parameters.Length == 0)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1,
					blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			t.Parameters = parameters;
			UserDefinedFunctions[blockToJumpContainer][name][index] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			return;
		}
	}

	private bool FunctionClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(0x2004, pos, false);

	private bool Constructor()
	{
		if (CheckBlockToJump2(nameof(Constructor)))
		{
			_TBStack[_Stackpos] = new(nameof(Constructor), blocksToJump[blocksToJumpPos].Start,
				blocksToJump[blocksToJumpPos].End, container);
			TransformErrorMessage2();
			if (parameterListsPos < parameterLists.Length
				&& parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start
				&& parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
				return IncreaseStack("Parameters", currentTask: nameof(Constructor2),
					pos_: parameterLists[parameterListsPos].Start, applyCurrentTask: true,
					start_: parameterLists[parameterListsPos].Start, end_: parameterLists[parameterListsPos].End,
					currentExtra: new ExtendedMethodParameters(), plp: parameterListsPos + 1);
			else
			{
				_TaskStack[_Stackpos] = nameof(Constructor2);
				_ExtraStack[_Stackpos] = new ExtendedMethodParameters();
				return true;
			}
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Constructor2()
	{
		if (CheckBlockToJump2(nameof(Constructor)))
		{
			CheckSuccess();
			TransformErrorMessage2();
			pos = blocksToJump[blocksToJumpPos].End;
			return CheckOpeningBracketAndAddTask(nameof(Main), nameof(ConstructorClosing), BlockType.Constructor);
		}
		else
			return _SuccessStack[_Stackpos] = false;
		void CheckSuccess()
		{
			if (!success || extra is not ExtendedMethodParameters parameters || parameters.Length == 0)
			{
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1,
					blocksToJump[blocksToJumpPos].End, container));
				return;
			}
			var blockToJumpContainer = blocksToJump[blocksToJumpPos].Container;
			var index = UserDefinedConstructorIndexes[blockToJumpContainer][blocksToJump[blocksToJumpPos].Start];
			var t = UserDefinedConstructors[blockToJumpContainer][index];
			t.Parameters = parameters;
			UserDefinedConstructors[blockToJumpContainer][index] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
	}

	private bool ConstructorClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(0x2004, pos, false);

	private bool CheckBlockToJump(String string_) => CheckBlockToJump3() && blocksToJump[blocksToJumpPos].Start >= pos
		&& !lexems.GetRange(pos, blocksToJump[blocksToJumpPos].Start - pos).ToHashSet().Convert(X => (X.Type, X.String))
		.Intersect([(LexemType.Other, ";"), (LexemType.Other, "\r\n"), (LexemType.Other, "{"), (LexemType.Other, "}")]).Any()
		&& blocksToJump[blocksToJumpPos].Type == string_;

	private bool CheckBlockToJump2(String string_) => CheckBlockToJump3() && blocksToJump[blocksToJumpPos].Type == string_;

	private bool CheckBlockToJump3() => blocksToJumpPos < blocksToJump.Length
		&& lexems[blocksToJump[blocksToJumpPos].Start].LineN == lexems[pos].LineN;

	private bool CheckClassSubordination() => registeredTypesPos < registeredTypes.Length
		&& lexems[registeredTypes[registeredTypesPos].Start].LineN == lexems[pos].LineN;

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
			return IncreaseStack(newTask, currentTask: closingString, pos_: pos + 1, applyPos: true, applyCurrentTask: true,
				container_: new(container.ToList().Append(new(blockType, blocksToJump[blocksToJumpPos].Name, 1))));
		else
			return EndWithError(0x2006, pos, false);
	}

	private bool CheckOpeningBracketAndAddTask(String newTask, String closingString, BlockType blockType, String? name = null)
	{
		if (IsCurrentLexemOther("{"))
			return IncreaseStack(newTask, currentTask: closingString, pos_: pos + 1, applyPos: true, applyCurrentTask: true,
				container_: new(container.ToList().Append(new(blockType, name ?? blocksToJump[blocksToJumpPos].Name, 1))),
				btjp: blocksToJumpPos + 1);
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		AppendBranch("Parameters");
		AddParameter();
		return Default();
	}

	private void AddParameter()
	{
		try
		{
			var parameters = (ExtendedMethodParameters?)_ExtraStack[_Stackpos - 1];
			parameters?.Add((ExtendedMethodParameter?)extra ?? new());
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
		return IncreaseStack(nameof(Type), currentTask: nameof(Parameter2), pos_: pos, applyPos: true, applyCurrentTask: true,
			currentExtra: new NStarType(new BlockStack(), NoBranches));
	}

	private bool Parameter2()
	{
		if (_TBStack[_Stackpos] != null && _ExtraStack[_Stackpos - 1] is List<object> paramCollection
			&& paramCollection[0] is ParameterAttributes attributes)
			_TBStack[_Stackpos]!.Extra = attributes;
		if (!AddExtraAndIdentifier())
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
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
			return IncreaseStack("Expr", currentTask: nameof(Parameter3), pos_: pos + 1, applyPos: true, applyCurrentTask: true,
				applyCurrentErl: true, currentBranch: new("optional", pos, pos + 1, container), addCurrentBranch: true);
		}
		else
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		try
		{
			CreateObjectList(out var l);
			var NStarType = (NStarType)l![1];
			_ExtraStack[_Stackpos - 1] = new ExtendedMethodParameter(NStarType, (String)l[2],
				(ParameterAttributes)l[0], "null");
			if (!Variables.TryGetValue(container, out var containerVariables))
			{
				containerVariables = [];
				Variables.Add(container, containerVariables);
			}
			containerVariables.Add((String)l[2], NStarType);
			parameterValues.Add((parameterLists[parameterListsPos - 1].Container, parameterLists[parameterListsPos - 1].Name,
				((ExtendedMethodParameters?)_ExtraStack[_Stackpos - 2] ?? []).Length + 1, (int)l[3], pos));
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
			var NStarType = (NStarType)l![1];
			_ExtraStack[_Stackpos - 1] = new ExtendedMethodParameter(NStarType, skipParameterName ? "" : (String)l[2],
				(ParameterAttributes)l[0], _TBStack[_Stackpos]?[^1].Name ?? "no optional");
		}
		catch
		{
		}
	}

	private bool ClassMain()
	{
		SkipSemicolonsAndNewLines();
		return IncreaseStack(nameof(Class), currentTask: nameof(ClassMain2), pos_: pos, applyPos: true, applyCurrentTask: true,
			currentBranch: new(nameof(ClassMain), pos, pos + 1, container), assignCurrentBranch: true);
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (success && treeBranch != null && treeBranch.Length != 0)
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		return Default();
	}

	private bool Members2()
	{
		if (success)
		{
			SkipSemicolonsAndNewLines();
			if (treeBranch == null || treeBranch.Length == 0)
				_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		if (!UserDefinedConstructors.TryGetValue(container, out var containerConstructors))
		{
			containerConstructors = [];
			UserDefinedConstructors.Add(container, containerConstructors);
		}
		containerConstructors.Insert(0, (ConstructorAttributes.Multiconst, [], [-1]));
		var increment = 1;
		if (UserDefinedProperties.TryGetValue(container, out var properties) && properties.Length != 0
			&& UserDefinedPropertiesOrder.TryGetValue(container, out var propertiesOrder)
			&& propertiesOrder.Length != 0)
			containerConstructors.Insert(1, (ConstructorAttributes.Multiconst, [], [-1]));
		if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType)
			&& CreateVar(GetAllProperties(userDefinedType.BaseType.MainType), out var baseProperties).Length != 0)
			foreach (var property in baseProperties)
			{
				if (property.Value.Attributes is not (PropertyAttributes.None or PropertyAttributes.Internal))
					continue;
				containerConstructors[1].Parameters.Add(new(property.Value.NStarType,
					property.Key, ParameterAttributes.Optional, "null"));
			}
		if (UserDefinedProperties.TryGetValue(container, out properties)
			&& properties.Length != 0
			&& UserDefinedPropertiesOrder.TryGetValue(container, out propertiesOrder)
			&& propertiesOrder.Length != 0)
		{
			foreach (var propertyName in propertiesOrder)
			{
				if (!properties.TryGetValue(propertyName, out var property))
					continue;
				containerConstructors[1].Parameters.Add(new(property.NStarType,
					propertyName, ParameterAttributes.Optional, "null"));
			}
			increment++;
		}
		if (UserDefinedConstructorIndexes.TryGetValue(container, out var containerConstructorIndexes))
		{
			for (var i = 0; i < containerConstructorIndexes.Length; i++)
				containerConstructorIndexes[containerConstructorIndexes.Keys[i]] += increment;
		}
	}

	private bool Member2()
	{
		if (success)
			return EndWithAssigning(true);
		else
			return IncreaseStack(nameof(Function), currentTask: nameof(Member3), applyCurrentTask: true,
				currentExtra: new List<object>());
	}

	private bool Member3() => success ? EndWithAssigning(true) : IncreaseStack(nameof(Constructor),
		currentTask: nameof(Member4), applyCurrentTask: true);

	private bool Member4()
	{
		if (success)
			return EndWithAssigning(true);
		else
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool Property()
	{
		var oldPos = pos;
		if (IsLexemKeyword(lexems[pos], ["private", "protected", "internal", "public"]))
		{
			AddPropertyAttribute(attributesMapping[lexems[pos].String], nameof(Property));
			if (lexems[pos].String == "public")
				GenerateMessage(0x202F, pos - 1, false);
		}
		else
			AddPropertyAttribute(PropertyAttributes.None, nameof(Property), false);
		var mask = (IsCurrentLexemKeyword("static") ? 2 : 0) + ((UserDefinedTypes[SplitType(container)].Attributes
			& TypeAttributes.Static) == TypeAttributes.Static ? 1 : 0);
		if (mask == 3)
			GenerateMessage(0x8000, pos, false);
		if (mask >= 1)
			AddPropertyAttribute2(PropertyAttributes.Static);
		if (mask >= 2)
			pos++;
		if (IsCurrentLexemKeyword("const"))
		{
			if (mask >= 2)
				GenerateMessage(0x800B, pos - 1, false);
			_TBStack[_Stackpos]!.Name = "Constant";
			AddPropertyAttribute2(PropertyAttributes.Static | PropertyAttributes.Const);
			pos++;
		}
		else if (pos < end && lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "required")
		{
			if (mask >= 2)
				GenerateMessage(0x2035, pos - 1, false);
			else
				AddPropertyAttribute2(PropertyAttributes.Required);
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
		return IncreaseStack(nameof(Type), currentTask: nameof(Property2), pos_: pos, applyPos: true, applyCurrentTask: true,
			currentExtra: new NStarType(new BlockStack(), NoBranches));
	}

	private bool Property2()
	{
		if (IsCurrentLexemKeyword(nameof(Function)))
			return _SuccessStack[_Stackpos] = false;
		if (!AddExtraAndIdentifier())
			return EndWithError(0x2001, pos, false);
		if (IsCurrentLexemOther("{"))
		{
			var getSet = GetSet();
			if (!getSet)
				return getSet;
		}
		if (IsCurrentLexemOperator("="))
			return IncreaseStack("Expr", currentTask: nameof(Property3), pos_: pos + 1, applyPos: true, applyCurrentTask: true);
		else
		{
			CreateObjectList(out var l);
			if (((PropertyAttributes)l![0] & PropertyAttributes.Const) == PropertyAttributes.Const)
			{
				GenerateMessage(0x203D, pos, false);
				for (; !IsLexemOther(lexems[pos++], ";");) ;
				return EndWithEmpty();
			}
			if (!ValidateLexemOrEndWithError(";", true))
				return false;
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			_TBStack[_Stackpos]?.Add(new("null", pos - 1, container));
			return AddUserDefinedProperty();
		}
		bool GetSet()
		{
			CreateObjectList(out var l);
			pos++;
			if (((PropertyAttributes)l![0] & PropertyAttributes.Const) == PropertyAttributes.Const)
			{
				GenerateMessage(0x203C, pos - 1, false);
				CloseBracket(ref pos, "}", ref errors, false);
				for (; !IsLexemOther(lexems[pos++], ";");) ;
				return EndWithEmpty();
			}
			if (!CheckIdentifier("get"))
				return false;
			if (IsCurrentLexemOperator(","))
				pos++;
			else
			{
				AddPropertyAttribute2(PropertyAttributes.NoSet);
				if (!ValidateLexemOrEndWithError("}"))
				{
					CloseBracket(ref pos, "}", ref errors, false);
					for (; !IsLexemOther(lexems[pos++], ";");) ;
					return EndWithEmpty();
				}
				return true;
			}
			if (IsLexemKeyword(lexems[pos], ["private", "protected"]))
			{
				AddPropertyAttribute2(lexems[pos].String == "private" ? PropertyAttributes.PrivateSet
					: PropertyAttributes.ProtectedSet);
				pos++;
			}
			if (pos < end && lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "init")
			{
				AddPropertyAttribute2(PropertyAttributes.SetOnce);
				pos++;
				if (!ValidateLexemOrEndWithError("}"))
				{
					CloseBracket(ref pos, "}", ref errors, false);
					for (; !IsLexemOther(lexems[pos++], ";");) ;
					return EndWithEmpty();
				}
			}
			else if (!CheckIdentifier("set") || !ValidateLexemOrEndWithError("}"))
				return false;
			return true;
		}
		bool CheckIdentifier(String string_)
		{
			if (pos < end && lexems[pos].Type == LexemType.Identifier && lexems[pos].String == string_)
			{
				pos++;
				return true;
			}
			else
				return EndWithError(0x2008, pos, false, "\"" + string_ + "\"");
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		if (_ExtraStack[_Stackpos - 1] is List<object> paramCollection)
			paramCollection.Add((success ? (NStarType?)extra : null) ?? NullType);
		if (lexems[pos].Type == LexemType.Identifier)
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			if (_ExtraStack[_Stackpos - 1] is List<object> paramCollection2)
				paramCollection2.Add(lexems[pos - 1].String);
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
			var NStarType = (NStarType)l![1];
			if (!UserDefinedProperties.TryGetValue(container, out var containerProperties))
			{
				containerProperties = [];
				UserDefinedProperties.Add(container, containerProperties);
			}
			var attributes = (PropertyAttributes)l[0];
			var name = (String)l[2];
			if ((attributes & (PropertyAttributes.Private | PropertyAttributes.Protected | PropertyAttributes.Internal
				| PropertyAttributes.SetOnce | PropertyAttributes.Required))
				== (PropertyAttributes.SetOnce | PropertyAttributes.Required))
			{
				var userDefinedType = UserDefinedTypes[SplitType(container)];
				userDefinedType.Restrictions.Add(new(false, NStarType, name));
				return EndWithEmpty();
			}
			containerProperties.Add(name, new(NStarType, attributes, treeBranch == null ? "" : treeBranch.Length == 0
				&& NStarEntity.TryParse(treeBranch.Name.ToString(), out var value) ? value.ToString(true)
				: treeBranch.Name == "Expr" && treeBranch.Length == 1 && treeBranch[0].Length == 0
				&& NStarEntity.TryParse(treeBranch[0].Name.ToString(), out value) ? value.ToString(true) : ""));
			var t = UserDefinedTypes[SplitType(container)];
			if ((attributes & PropertyAttributes.Static) == 0)
			{
				t.Decomposition ??= [];
				t.Decomposition.Add(name, new("type", pos, container) { Extra = NStarType });
				UserDefinedTypes[SplitType(container)] = t;
				if (!UserDefinedPropertiesMapping.TryGetValue(container, out var dic))
				{
					UserDefinedPropertiesMapping.Add(container, []);
					dic = UserDefinedPropertiesMapping[container];
				}
				dic.Add(name, dic.Length);
			}
			if (!UserDefinedPropertiesOrder.TryGetValue(container, out var containerPropertiesOrder))
			{
				containerPropertiesOrder = [];
				UserDefinedPropertiesOrder.Add(container, containerPropertiesOrder);
			}
			containerPropertiesOrder.Add(name);
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
			var NStarType = (NStarType)l![1];
			if (!UserDefinedConstants.TryGetValue(container, out var containerConstants))
			{
				containerConstants = [];
				UserDefinedConstants.Add(container, containerConstants);
			}
			var attributes = (ConstantAttributes)l[0];
			var name = (String)l[2];
			if (treeBranch == null)
				containerConstants.Add(name, new(NStarType, attributes, new("null", 0, [])));
			else if (treeBranch.Length == 0 && NStarEntity.TryParse(treeBranch.Name.ToString(), out var value))
				containerConstants.Add(name, new(NStarType, attributes, new(value.ToString(true),
					treeBranch.Pos, treeBranch.Container)
				{
					Extra = value.InnerType
				}));
			else if (treeBranch.Name == "Expr" && treeBranch.Length == 1 && treeBranch[0].Length == 0
				&& NStarEntity.TryParse(treeBranch[0].Name.ToString(), out value))
				containerConstants.Add(name, new(NStarType, attributes, new(value.ToString(true),
					treeBranch.Pos, treeBranch.Container)
				{
					Extra = value.InnerType
				}));
			else
				containerConstants.Add(name, new(NStarType, attributes, treeBranch));
		}
		catch
		{
		}
		return Default();
	}

	private bool ValidateLexemOrEndWithError(String string_, bool addQuotes = false)
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
			if (lexems[pos + 1].Type == LexemType.Identifier)
			{
				GenerateMessage(0x2048, pos, true);
				return EndActionChain();
			}
		}
		if (lexems[pos].Type != LexemType.Keyword)
			return IncreaseStack("Expr", currentTask: nameof(ActionChain6), applyCurrentTask: true);
		var newTask = lexems[pos].String.ToString() switch
		{
			"if" or "else" => nameof(Condition),
			"loop" or "repeat" or "while" or "for" => nameof(Cycle),
			"continue" or "break" => nameof(SpecialAction),
			"return" => nameof(Return),
			"null" when pos + 1 < end && IsLexemKeyword(lexems[pos + 1], [nameof(Function), "Operator", "Extent"]) =>
				nameof(Main),
			_ => "Expr",
		};
		return IncreaseStack(newTask, currentTask: newTask == "Expr"
			? nameof(ActionChain6) : nameof(ActionChain5), applyCurrentTask: true);
	}

	private bool ActionChain2_3_4(String newTask, String currentTask)
	{
		if (!success)
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
		_TaskStack[_Stackpos] = nameof(ActionChain);
		TransformErrorMessage();
		if (treeBranch != null && (treeBranch.Name != "" || treeBranch.Length != 0))
			AppendBranch(nameof(ActionChain));
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool ActionChain5()
	{
		if (!success)
			return IncreaseStack("Expr", currentTask: nameof(ActionChain6), pos_: pos, applyPos: true, applyCurrentTask: true,
				currentExtra: new List<object>());
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		if (pos >= 1 && IsLexemKeyword(lexems[pos - 1], ["else", "loop"])
			|| pos >= 2 && IsLexemKeyword(lexems[pos - 2], ["continue", "break"]) && IsLexemOther(lexems[pos - 1], ";"))
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
				_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			return Default();
		}
		return lexems[pos].Type == LexemType.Keyword ? lexems[pos].String.ToString() switch
		{
			"while" or "repeat" => IncreaseStack(nameof(WhileRepeat), currentTask: nameof(Cycle3), pos_: pos, applyPos: true,
				applyCurrentTask: true),
			"for" => IncreaseStack(nameof(For), currentTask: nameof(Cycle3), pos_: pos, applyPos: true, applyCurrentTask: true),
			_ => EndWithError(0x2010, pos, false),
		} : EndWithError(0x2010, pos, false);
	}

	private bool Cycle2() => success ? EndWithAssigning(true) : IncreaseStack(nameof(For), currentTask: nameof(Cycle3),
		applyCurrentTask: true);

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
			_TBStack[_Stackpos]?.Add(new("Declaration", [treeBranch ?? TreeBranch.DoNotAdd(),
				new(lexems[pos - 1].String, pos - 1, pos, container)], container));
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
			if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall)
				|| _TaskStack[_Stackpos - 1] == nameof(Expr2) && _TaskStack[_Stackpos - 2] == nameof(BasicExpr2)
				|| _TaskStack[_Stackpos - 1] == nameof(TypeChainIteration2))
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall)
				|| _TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length != 0
				&& _TaskStack[_Stackpos - 1] == nameof(Expr2) && _TaskStack[_Stackpos - 2] == nameof(BasicExpr2)
				|| _TaskStack[_Stackpos - 1] == nameof(TypeChainIteration2))
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
		if (pos >= end)
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		if (IsCurrentLexemKeyword("switch"))
		{
			pos++;
			if (!IsCurrentLexemOther("{"))
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new("switch", pos - 2, container));
			return IncreaseStack("Switch", currentTask: nameof(LambdaExpr5),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		if (!IsCurrentLexemOperator("=>"))
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
			treeBranch.Name = "Lambda";
		if (treeBranch != null && treeBranch.Length == 1
			&& treeBranch[0].Name.ToString() is "Assignment" or "DeclarationAssignment")
		{
			var assignmentBranch = treeBranch[0];
			treeBranch.RemoveAt(0);
			assignmentBranch[0] = new("Lambda", [assignmentBranch[0], this.treeBranch!], assignmentBranch[0].Container);
			_TBStack[_Stackpos] = assignmentBranch;
			return Default();
		}
		return EndWithAddingOrAssigning(true, treeBranch?.Length ?? 0);
	}

	private bool LambdaExpr5()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (!IsCurrentLexemOther("}"))
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		pos++;
		var treeBranch = _TBStack[_Stackpos];
		if (treeBranch != null && this.treeBranch != null)
		{
			treeBranch.Name = "SwitchExpr";
			treeBranch[^1].AddRange(this.treeBranch.Elements);
		}
		return Default();
	}

	private bool Switch2()
	{
		if (!success || _TBStack[_Stackpos] == null || treeBranch == null)
		{
			GenerateMessage(0x2033, pos, false);
			return SwitchFail();
		}
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (pos < end && IsCurrentLexemKeyword("if"))
		{
			pos++;
			_TBStack[_Stackpos]!.Add(new("case", treeBranch, container));
			return IncreaseStack("AssignedExpr", currentTask: nameof(Switch5),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		if (pos >= end || !IsCurrentLexemOperator("=>"))
		{
			GenerateMessage(0x2008, pos, false, "\"if\" or =>");
			return SwitchFail();
		}
		pos++;
		_TBStack[_Stackpos]!.Add(new("case", treeBranch, container));
		return IncreaseStack("AssignedExpr", currentTask: "Switch3",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool Switch3_4()
	{
		if (!success || _TBStack[_Stackpos] == null || _TBStack[_Stackpos]!.Length == 0 || treeBranch == null)
		{
			GenerateMessage(0x200E, pos, false);
			return SwitchFail();
		}
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (IsCurrentLexemOther("}") && lexems[pos].LineN == lexems[_TBStack[_Stackpos - 1]!.Pos].LineN)
		{
			_TBStack[_Stackpos]![^1].Add(treeBranch);
			return Default();
		}
		if (pos >= end || !IsCurrentLexemOperator(","))
		{
			GenerateMessage(0x2008, pos, false, "comma" + (IsCurrentLexemOther("}")
				? "; no final comma is allowed only if the switch expression is single-line" : ""));
			return SwitchFail();
		}
		pos++;
		if (IsCurrentLexemOther("}"))
		{
			_TBStack[_Stackpos]![^1].Add(treeBranch);
			return Default();
		}
		if (task == "Switch4")
		{
			GenerateMessage(0x2034, pos, false);
			return SwitchFail();
		}
		if (!IsCurrentLexemKeyword("_"))
		{
			_TBStack[_Stackpos]![^1].Add(treeBranch);
			return IncreaseStack("AssignedExpr", currentTask: nameof(Switch2),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		_TBStack[_Stackpos]![^1].Add(treeBranch);
		_TBStack[_Stackpos]!.Add(new("case", new TreeBranch("_", pos, container) { Extra = NullType }, container));
		pos++;
		if (pos < end && IsCurrentLexemKeyword("if"))
		{
			pos++;
			return IncreaseStack("AssignedExpr", currentTask: nameof(Switch5),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		if (pos >= end || !IsCurrentLexemOperator("=>"))
		{
			GenerateMessage(0x2008, pos, false, "=>");
			return SwitchFail();
		}
		pos++;
		return IncreaseStack("AssignedExpr", currentTask: "Switch4",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool Switch5()
	{
		if (!success || _TBStack[_Stackpos] == null || _TBStack[_Stackpos]!.Length == 0 || treeBranch == null)
		{
			GenerateMessage(0x200E, pos, false);
			return SwitchFail();
		}
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (pos >= end || !IsCurrentLexemOperator("=>"))
		{
			GenerateMessage(0x2008, pos, false, "=>");
			CloseBracket(ref pos, "}", ref errors, false);
			pos--;
			return EndWithEmpty();
		}
		pos++;
		_TBStack[_Stackpos]![^1].Add(treeBranch);
		return IncreaseStack("AssignedExpr", currentTask: "Switch3",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}
	private bool SwitchFail()
	{
		CloseBracket(ref pos, "}", ref errors, false);
		pos--;
		return EndWithEmpty();
	}

	private bool AssignedExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsLexemOperator(lexems[pos], AssignmentOperators))
			return EndWithAddingOrAssigning(true, 0);
		if (treeBranch != null && (treeBranch.Name == "Declaration" || treeBranch.Name == nameof(Hypername)))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(0, [treeBranch, new(lexems[pos - 1].String, pos - 1, pos, container)]);
			_TBStack[_Stackpos]!.Name = treeBranch.Name == "Declaration" ? "DeclarationAssignment" : "Assignment";
			return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
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
		if (pos >= end || !IsLexemOperator(lexems[pos], TernaryOperators))
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
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
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1),
					(TreeBranch)new("null", pos - 1, container));
				return Default();
			}
			else
				return IncreaseStack(nameof(Type), currentTask: nameof(EquatedExpr3), applyCurrentTask: true,
					pos_: pos, applyPos: true, applyCurrentErl: true);
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
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				return Default();
			}
		}
		else
		{
			GenerateMessage(0x201C, pos, false);
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1),
				(TreeBranch)new("null", pos, pos + 1, container));
			return Default();
		}
	}

	private bool LeftAssociativeOperatorExpr2(String newTask, List<String> operators)
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && lexems[pos].Type == LexemType.Operator && operators.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
		}
		else
		{
			if (treeBranch != null && treeBranch.Name.ToString() is "0" or "0i" or "0u" or "0L" or "0uL" or "\"0\""
				&& _TBStack[_Stackpos] != null && _TBStack[_Stackpos]!.Length != 0
				&& _TBStack[_Stackpos]![^1].Name.ToString() is "/" or "%")
				GenerateMessage(0x201F, pos, true);
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		}
		return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool RightAssociativeOperatorExpr2(String newTask, List<String> operators)
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && lexems[pos].Type == LexemType.Operator && operators.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Insert(0, [treeBranch ?? TreeBranch.DoNotAdd(),
				new(lexems[pos - 1].String, pos - 1, pos, container)]);
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
			errors = null;
		}
		_TBStack[_Stackpos]!.Name = nameof(Range);
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
		return IncreaseStack(nameof(UnaryExpr2), currentTask: isPrefix ? nameof(UnaryExpr3)
			: "UnaryExpr4", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr2()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator && new List<String> { "ln", "#", "$" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			return IncreaseStack(nameof(UnaryExpr2), currentTask: nameof(UnaryExpr3), pos_: pos, applyPos: true,
				applyCurrentTask: true);
		}
		else if (lexems[pos].Type == LexemType.Operator
			&& new List<String> { "++", "--", "^", "sin", "cos", "tan", "asin", "acos", "atan" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			if (lexems[pos - 1].String == "^")
				_TBStack[_Stackpos]!.Name = "Index";
			isPrefix = true;
		}
		return IncreaseStack("PrefixExpr", currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String == "!!" ? "!!" : "postfix " + lexems[pos - 1].String,
				pos - 1, pos, container));
		}
		else
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		_PosStack[_Stackpos] = pos;
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		return Default();
	}

	private bool PostfixExpr2_3(String newTask, String currentTask)
	{
		if (!success)
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
		if (treeBranch != null && treeBranch.Name == nameof(Hypername) && treeBranch.Length == 1
			&& (!WordRegex().IsMatch(treeBranch[0].Name.ToString())
			|| new List<String> { "_", "true", "false", "this", "base", "null", "Infty", "Uncty", "Pi", "E", "List" }
			.Contains(treeBranch[0].Name) || treeBranch[0].Length != 0))
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
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
			return IncreaseStack(nameof(Type),
				currentTask: task == "HypernameNotCall" ? "HypernameNotCallType" : nameof(HypernameType),
				applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		if (IsCurrentLexemKeyword("new"))
		{
			if (task == "HypernameNotCall" || _TaskStack[_Stackpos - 1] == "HypernameClosing"
				|| _TaskStack[_Stackpos - 1] == "HypernameNotCallClosing")
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
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true,
					applyCurrentTask: true, applyCurrentErl: success);
			}
			return IncreaseStack(nameof(TypeConstraints.NotAbstract), currentTask: "HypernameNew", applyCurrentTask: true,
				currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		else if (IsCurrentLexemKeyword("const"))
		{
			pos++;
			return IncreaseStack(nameof(Type),
				currentTask: task == "HypernameNotCall" ? "HypernameNotCallConstType" : "HypernameConstType",
				applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		if (pos >= 1 && IsLexemOperator(lexems[pos - 1], "."))
		{
			_TaskStack[_Stackpos] = nameof(HypernameType);
			return true;
		}
		return IncreaseStack(nameof(Type),
			currentTask: task == "HypernameNotCall" ? "HypernameNotCallType" : nameof(HypernameType),
			applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
	}

	private bool HypernameNew()
	{
		if (success)
		{
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]![^1].Name = "new type";
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOther("("))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(new("ConstructorCall", pos - 1, pos, container));
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true,
					applyCurrentTask: true, applyCurrentErl: success);
			}
			else
				return EndWithError(0x200A, pos, true);
		}
		else
		{
			if (errors != null)
				_ErLStack[_Stackpos].AddRange(errors);
			return Default();
		}
	}

	private bool HypernameConstType()
	{
		if (!success || extra is not NStarType NStarType)
			return EndWithError(0x2001, pos, true);
		_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		if (lexems[pos].Type != LexemType.Identifier)
			return EndWithError(0x2001, pos, true);
		pos++;
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		AppendBranch("Declaration", new(lexems[pos - 1].String, pos - 1, pos, container) { Extra = NStarType });
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
			if (extra is not NStarType NStarType)
				return EndWithError(0x2001, pos, true);
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			if (lexems[pos].Type == LexemType.Identifier)
			{
				pos++;
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				AppendBranch("Declaration", new(lexems[pos - 1].String, pos - 1, pos, container) { Extra = NStarType });
				return HypernameDeclaration();
			}
			if (IsCurrentLexemOperator("."))
				return IncreaseStack(task == "HypernameNotCallType" ? "HypernameNotCall" : nameof(Hypername),
					currentTask: task == "HypernameNotCallType" ? "HypernameNotCallClosing" : "HypernameClosing",
					pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
					currentBranch: new(".", pos, container), addCurrentBranch: true);
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			if (NStarType.MainType.Length == 1 && !NStarType.MainType.Peek().Name.Contains(' ')
				&& NStarType.ExtraTypes.Length == 0)
			{
				var newBranch = UserDefinedConstantExists(container, NStarType.MainType.Peek().Name,
					out var constant, out _, out _) && constant.HasValue && constant.Value.DefaultValue != null
					? constant.Value.DefaultValue.Name == "Expr" && constant.Value.DefaultValue.Length == 1
				? new TreeBranch("Expr", constant.Value.DefaultValue, container) : constant.Value.DefaultValue
					: new(NStarType.MainType.Peek().Name, treeBranch?.Pos ?? -1, treeBranch?.Container ?? []);
				_TBStack[_Stackpos]![0] = newBranch;
			}
			return HypernameBracketsAndDot();
		}
		errors?.Clear();
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
				? constant.Value.DefaultValue.Name == "Expr" && constant.Value.DefaultValue.Length == 1
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
			if (treeBranch != null && treeBranch.Name == "Indexes" && treeBranch.Length != 0)
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
				return IncreaseStack("List", currentTask: task == "HypernameNotCallIndexes" ? "HypernameNotCallCall"
					: nameof(HypernameCall), pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
			}
			else
				return _SuccessStack[_Stackpos] = false;
		}
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOperator("."))
			return IncreaseStack(task == "HypernameNotCallIndexes" ? "HypernameNotCall" : nameof(Hypername),
				currentTask: task == "HypernameNotCallIndexes" ? "HypernameNotCallClosing" : "HypernameClosing",
				pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
				currentBranch: new(".", pos, container), addCurrentBranch: true);
		else
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			return Default();
		}
	}

	private bool HypernameClosing_BasicExpr4() =>
		success ? pos >= end ? (_SuccessStack[_Stackpos] = false) : EndWithAdding(true) : (_SuccessStack[_Stackpos] = false);

	private bool HypernameDeclaration(bool @const = false)
	{
		if (extra is not NStarType NStarType)
			return Default();
		if (@const)
		{
			if (!UserDefinedConstants.TryGetValue(container, out var containerConstants))
			{
				containerConstants = [];
				UserDefinedConstants.Add(container, containerConstants);
			}
			containerConstants[lexems[pos - 1].String] = new(NStarType, ConstantAttributes.None, null!);
			return Default();
		}
		else
		{
			if (!Variables.TryGetValue(container, out var containerVariables))
			{
				containerVariables = [];
				Variables.Add(container, containerVariables);
			}
			containerVariables[lexems[pos - 1].String] = NStarType;
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
				return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true,
					applyCurrentTask: true, applyCurrentErl: success);
			}
			else
				return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther("["))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new("Indexes", pos - 1, pos, container));
			return IncreaseStack("Indexes",
				currentTask: task.StartsWith("HypernameNotCall") ? "HypernameNotCallIndexes" : nameof(HypernameIndexes),
				pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: success);
		}
		else if (IsCurrentLexemOperator("."))
			return IncreaseStack(task.StartsWith("HypernameNotCall") ? "HypernameNotCall" : nameof(Hypername),
				currentTask: task.StartsWith("HypernameNotCall") ? "HypernameNotCallClosing" : "HypernameClosing",
				pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: success,
				currentBranch: new(".", pos, container), addCurrentBranch: true);
		else
		{
			if (success)
				_ErLStack[_Stackpos].AddRange(errors ?? []);
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
		NStarType NStarType;
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemKeyword("null"))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			pos++;
			return TypeSingularTuple(NStarType);
		}
		else if (lexems[pos].Type == LexemType.Identifier || IsCurrentLexemOther("["))
			return IdentifierType(constraints);
		else if (!IsCurrentLexemOther("("))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2014, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IdentifierType(constraints))
			return true;
		else if (constraints == TypeConstraints.None)
			return TupleType();
		else
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2015, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool IdentifierType(TypeConstraints constraints = TypeConstraints.None)
	{
		String namespace_ = [], outerClass = [];
		BlockStack container = [];
		bool result;
		while (IdentifierTypeIteration(constraints, container, ref namespace_, ref outerClass, out result)) ;
		return result;
	}

	private bool IdentifierTypeIteration(TypeConstraints constraints, BlockStack container,
		ref String namespace_, ref String outerClass, out bool outerResult)
	{
		var s = lexems[pos].String;
		var mainContainer = this.container;
		BlockStack innerContainer = [], innerUserDefinedContainer;
		Type? netType = null;
		NStarType NStarType;
		BranchCollection typeParts = [];
		if (PrimitiveType(constraints, s) is bool b)
		{
			outerResult = b;
			return false;
		}
		if (ExtraTypeExists(container, s) || container.Length == 0
			&& CheckContainer(mainContainer, stack => ExtraTypeExists(stack, s), out innerContainer))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2025, pos, false);
			}
			else
			{
				NStarType = (new(container.ToList().Append(new(BlockType.Extra, s, 1))), NoBranches);
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if (Namespaces.Contains(namespace_ == "" ? s : namespace_ + "." + s)
			|| UserDefinedNamespaces.Contains(namespace_ == "" ? s : namespace_ + "." + s))
		{
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
				GenerateUnexpectedEndOfTypeError(ref errors);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			else if (IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Namespace, s, 1));
				namespace_ = namespace_ == "" ? s : namespace_ + "." + s;
				_PosStack[_Stackpos] = ++pos;
				outerResult = false;
				return true;
			}
			else
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
				GenerateMessage(0x2020, pos, false);
			}
		}
		else if (container.Length == 0 && PrimitiveTypes.ContainsKey(s))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			if (typeDepth != 0 && s == "var")
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2022, pos, false);
			}
			else
			{
				_PosStack[_Stackpos] = ++pos;
				NStarType = (new(container.ToList().Append(new(BlockType.Primitive, s, 1))), NoBranches);
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if ((ExtraTypes.TryGetValue((namespace_, s), out netType) || namespace_ == ""
			&& ExplicitlyConnectedNamespaces.FindIndex(x => ExtraTypes.TryGetValue((x, s), out netType)) >= 0)
			&& netType != null)
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& (!netType.IsClass || netType.IsSealed))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(container.ToList().Append(new(typeof(Delegate).IsAssignableFrom(netType) ? BlockType.Delegate
				: netType.IsInterface ? BlockType.Interface
				: netType.IsClass ? BlockType.Class : netType.IsValueType
				? BlockType.Struct : throw new InvalidOperationException(), s, 1))), NoBranches);
			if (constraints == TypeConstraints.NotAbstract && netType.IsAbstract)
			{
				GenerateMessage(0x2023, pos, false, NStarType.ToString());
			}
			_PosStack[_Stackpos] = ++pos;
			if (netType.GetGenericArguments().Length == 0)
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if (ExtendedTypes.TryGetValue((innerContainer = container, s), out var value) || namespace_ == ""
			&& ExplicitlyConnectedNamespaces.FindIndex(x => ExtendedTypes.TryGetValue((innerContainer
			= new(x.Split('.').Convert(x => new Block(BlockType.Namespace, x, 1))), s), out value)) >= 0)
		{
			var (Restrictions, Attributes) = value;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(Attributes))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(innerContainer.ToList().Append(new(s.ToString() is nameof(Action) or nameof(Func<bool>)
				? BlockType.Delegate : BlockType.Class, s, 1))), NoBranches);
			if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static)
				GenerateMessage(0x2024, pos, false, NStarType.ToString());
			else if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not (0 or TypeAttributes.Sealed
				or TypeAttributes.Struct or TypeAttributes.Enum))
				GenerateMessage(0x2024, pos, false, NStarType.ToString());
			_PosStack[_Stackpos] = ++pos;
			if (Restrictions.Length == 0)
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
			if (pos >= end)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
				GenerateUnexpectedEndOfTypeError(ref errors);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			else if (IsCurrentLexemOther("["))
				_PosStack[_Stackpos] = ++pos;
			else
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x200C, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			typeChainTemplate.Add(Restrictions);
			collectionTypes.Add("associativeArray");
			outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
				currentExtra: (innerContainer = NStarType.MainType, typeParts));
			return false;
		}
		else if (UserDefinedTypes.TryGetValue((innerUserDefinedContainer = container, s), out var value2)
			|| container.Length == 0 && CheckContainer(mainContainer, stack =>
			UserDefinedTypes.TryGetValue((stack, s), out value2), out innerUserDefinedContainer))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(value2.Attributes) && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))), NoBranches);
			if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static
				&& !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				GenerateMessage(0x2024, pos, false, NStarType);
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not (0 or TypeAttributes.Sealed
				or TypeAttributes.Struct or TypeAttributes.Enum) && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				GenerateMessage(0x2023, pos, false, NStarType);
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Class, s, 1));
				outerClass = outerClass == "" ? s : outerClass + "." + s;
				_PosStack[_Stackpos] = ++pos;
				outerResult = false;
				return true;
			}
			if (pos < end && IsCurrentLexemOther("["))
			{
				_PosStack[_Stackpos] = ++pos;
				typeChainTemplate.Add([new(true, ObjectType, "")]);
				collectionTypes.Add("associativeArray");
				outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
					applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
					currentExtra: (innerContainer = NStarType.MainType, typeParts));
				return false;
			}
			NStarType = (new(innerUserDefinedContainer.ToList()
				.Append(new((value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) switch
				{
					TypeAttributes.None or TypeAttributes.Sealed or TypeAttributes.Abstract
						or TypeAttributes.Static => BlockType.Class,
					TypeAttributes.Struct => BlockType.Struct,
					TypeAttributes.Enum => BlockType.Enum,
					TypeAttributes.Interface => BlockType.Interface,
					TypeAttributes.Delegate => BlockType.Delegate,
					_ => throw new InvalidOperationException(),
				}, s, 1))), NoBranches);
			outerResult = TypeSingularTuple(NStarType);
			return false;
		}
		else if (IsNotImplementedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]))
			|| IsNotImplementedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x202A, pos, false, s);
		}
		else if (IsNotImplementedEndOfIdentifier(s, out var s2))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x202B, pos, false, s2);
		}
		else if (IsOutdatedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]), out var useInstead))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x202C, pos, false, s, useInstead);
		}
		else if (IsOutdatedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s, out useInstead))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x202D, pos, false, s, useInstead);
		}
		else if (IsOutdatedEndOfIdentifier(s, out s2, out useInstead))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x202E, pos, false, s2, useInstead);
		}
		else if (IsReservedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]))
			|| IsReservedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x203A, pos, false, s);
		}
		else if (IsReservedEndOfIdentifier(s, out s2))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x203B, pos, false, s2);
		}
		else if (!IsCurrentLexemOther("["))
		{
			if (container.Length == 0)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2026, pos, false, String.Join(".", [.. container.ToList().Convert(X => X.Name), s]));
			}
			else
			{
				pos--;
				NStarType = new NStarType(container, NoBranches);
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else
		{
			netType = typeof(Dictionary<,>);
			NStarType = (new(container.ToList().Append(new(BlockType.Class, nameof(Dictionary<bool, bool>), 1))), NoBranches);
		}
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			outerResult = _SuccessStack[_Stackpos] = false;
			return false;
		}
		else if (IsCurrentLexemOther("["))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			if (s != nameof(Dictionary<bool, bool>))
			{
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = NullType;
			GenerateMessage(0x200C, pos, false);
			outerResult = TypeSingularTuple(NStarType);
			return false;
		}
		ExtendedRestrictions template = [];
		typeParts = [];
		for (var i = 0; i < netType!.GetGenericArguments().Length; i++)
			template.Add(new(false, RecursiveType, ""));
		typeChainTemplate.Add(template);
		collectionTypes.Add("associativeArray");
		outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
			currentExtra: (container = NStarType.MainType, typeParts));
		return false;
	}

	private bool IdentifierType2()
	{
		typeChainTemplate.RemoveAt(^1);
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not (BlockStack innerContainer, BranchCollection types))
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		NStarType NStarType = (innerContainer, types);
		return TypeComma(NStarType);
	}

	private bool? PrimitiveType(TypeConstraints constraints, String s)
	{
		NStarType NStarType;
		if (s.ToString() is "short" or "long")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
				GenerateUnexpectedEndOfTypeError(ref errors);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && (lexems[pos].String == "char" || lexems[pos].String == "int"
				/*|| s == "long" && (lexems[pos].input == "long" || lexems[pos].input == "real")*/))
			{
				NStarType = (new([new(BlockType.Primitive, s + " " + lexems[pos].String, 1)]), NoBranches);
				_PosStack[_Stackpos] = ++pos;
				return TypeSingularTuple(NStarType);
			}
			else
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2027, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
		}
		else if (s == "unsigned")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			String mediumWord = [];
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				GenerateUnexpectedEndOfTypeError(ref errors);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier
				&& (lexems[pos].String == "short" || lexems[pos].String == "long"))
			{
				mediumWord = lexems[pos].String + " ";
				_PosStack[_Stackpos] = ++pos;
			}
			if (pos >= end)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
				GenerateUnexpectedEndOfTypeError(ref errors);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "int"/* || lexems[pos].input == "long"*/)
			{
				NStarType = (new([new(BlockType.Primitive, s + " " + mediumWord + lexems[pos].String, 1)]), NoBranches);
				_PosStack[_Stackpos] = ++pos;
				return TypeSingularTuple(NStarType);
			}
			else
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
				GenerateMessage(0x2028, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
		}
		else if (s == "list")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				GenerateMessage(0x2015, pos, false);
				return _SuccessStack[_Stackpos] = false;
			}
			if (pos > 0 && lexems[pos - 1].String == ".")
				return null;
			_PosStack[_Stackpos] = ++pos;
		}
		if (collectionTypes[^1] == "list")
			GenerateMessage(0x8003, pos - 1, false);
		BranchCollection typeParts = [];
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther("("))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			NStarType = NullType;
			if (s != "list")
				return null;
			GenerateMessage(0x200A, pos, false);
			typeDepth++;
			CloseBracket(ref pos, ")", ref errors!, false, end);
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeListFail), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (lexems[pos].Type == LexemType.Int)
		{
			_ExtraStack[_Stackpos - 1] = typeParts;
			typeParts.Add(new(lexems[pos].String, 0, []));
			_PosStack[_Stackpos] = ++pos;
		}
		else if (lexems[pos].Type is LexemType.UnsignedInt or LexemType.LongInt
			or LexemType.UnsignedLongInt or LexemType.Real or LexemType.Complex or LexemType.String)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2017, pos, false);
			_PosStack[_Stackpos] = ++pos;
		}
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther(")"))
		{
			_PosStack[_Stackpos] = ++pos;
			typeDepth++;
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeList2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else
			return IncreaseStack("LambdaExpr", currentTask: nameof(TypeList), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool TypeList()
	{
		if (!success || treeBranch == null)
		{
			GenerateMessage(0x200E, pos, false);
			return _SuccessStack[_Stackpos] = false;
		}
		var targetBranch = treeBranch.Name == "Expr" && treeBranch.Length == 1 ? treeBranch[0] : treeBranch;
		if (!(treeBranch.Extra == null || targetBranch.Extra is NStarType NStarType
			&& TypesAreCompatible(NStarType, IntType, out var warning, [], out _, out _) && !warning))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2017, pos, false);
		}
		else if (IsCurrentLexemOther(")"))
		{
			if (_ExtraStack[_Stackpos - 1] is not BranchCollection typeParts)
				_ExtraStack[_Stackpos - 1] = typeParts = [];
			typeParts.Add(NStarEntity.TryParse(targetBranch.Name.ToString(), out var value)
				? new(value.ToString(true), targetBranch.Pos, []) : treeBranch);
			_PosStack[_Stackpos] = ++pos;
			typeDepth++;
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeList2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else
		{
			if (IsLexemOther(lexems[start], "("))
			{
				_PosStack[_Stackpos] = pos = start;
				return TupleType();
			}
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x200B, pos, false);
		}
		typeDepth++;
		collectionTypes.Add("list");
		CloseBracket(ref pos, ")", ref errors!, false, end);
		return IncreaseStack(nameof(Type), currentTask: nameof(TypeListFail), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool TypeList2()
	{
		typeDepth--;
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not NStarType InnerNStarType)
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is not BranchCollection typeParts)
			typeParts = [];
		NStarType NStarType = new(ListBlockStack,
			[.. typeParts, new("type", pos, container) { Extra = InnerNStarType }]);
		return TypeSingularTuple(NStarType);
	}

	private bool TypeListFail()
	{
		typeDepth--;
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not NStarType InnerNStarType)
		{
			_PosStack[_Stackpos] = pos = start;
			if (IsCurrentLexemOther("("))
				return TupleType();
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is not BranchCollection typeParts)
			typeParts = [];
		_ExtraStack[_Stackpos - 1] = new NStarType(ListBlockStack,
			[.. typeParts, new("type", pos, container) { Extra = InnerNStarType }]);
		return Default();
	}

	private bool TupleType()
	{
		_PosStack[_Stackpos] = ++pos;
		typeChainTemplate.Add([new(true, RecursiveType, "")]);
		collectionTypes.Add("tuple");
		return IncreaseStack(nameof(TypeChain), currentTask: nameof(TupleType2), pos_: pos,
			applyPos: true, applyCurrentTask: true);
	}

	private bool TupleType2()
	{
		NStarType NStarType;
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not BranchCollection innerRestrictions)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther(")"))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x200B, pos, false);
			CloseBracket(ref pos, ")", ref errors, false, end);
			return _SuccessStack[_Stackpos] = false;
		}
		NStarType = (new([new(BlockType.Primitive, "tuple", 1)]), innerRestrictions);
		return TypeSingularTuple(NStarType);
	}

	private bool TypeChain()
	{
		BranchCollection types = [];
		if (typeChainTemplate[^1].Length == 0)
		{
			types = NoBranches;
			return _SuccessStack[_Stackpos] = false;
		}
		return IncreaseStack(nameof(TypeChainIteration), currentTask: nameof(TypeChain2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentExtra: types);
	}

	private bool TypeChain2()
	{
		if (!success || _TaskStack[_Stackpos] == nameof(IdentifierType2)
			&& _ExtraStack[_Stackpos - 1] is not (BlockStack, BranchCollection)
			|| extra is not BranchCollection typesBuffer)
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (_ExtraStack[_Stackpos - 1] is (BlockStack, BranchCollection types))
			types.AddRange(typesBuffer);
		else
			_ExtraStack[_Stackpos - 1] = typesBuffer;
		return Default();
	}

	private bool TypeChainIteration()
	{
		if (_ExtraStack[_Stackpos - 1] is not BranchCollection types)
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (typeChainTemplate[^1][0].RestrictionType.MainType.Equals(RecursiveBlockStack))
		{
			tpos.Add(0);
			typeDepth++;
			collectionTypes.Add(collectionTypes[^1]);
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeChainIteration2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else if (typeChainTemplate[^1][0].RestrictionType.Equals(ObjectType))
		{
			tpos.Add(0);
			typeDepth++;
			collectionTypes.Add(collectionTypes[^1]);
			return IncreaseStack("List", currentTask: nameof(TypeChainIteration2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
				currentBranch: new("List", pos, pos + 1, container));
		}
		else if (TypeEqualsToPrimitive(typeChainTemplate[^1][tpos[^1]].RestrictionType, "int"))
		{
			var minus = false;
			if (pos >= end)
			{
				types = NoBranches;
				GenerateUnexpectedEndOfTypeError(ref errors);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (IsCurrentLexemOperator("-"))
			{
				minus = true;
				_PosStack[_Stackpos] = ++pos;
			}
			if (pos >= end)
			{
				types = NoBranches;
				GenerateUnexpectedEndOfTypeError(ref errors);
				return _SuccessStack[_Stackpos] = false;
			}
			else if (lexems[pos].Type == LexemType.Int)
				_PosStack[_Stackpos] = ++pos;
			else
			{
				types = NoBranches;
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
		if (_ExtraStack[_Stackpos - 1] is not BranchCollection types)
		{
			ReduceStack();
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (typeChainTemplate[^1][0].RestrictionType.Equals(ObjectType))
		{
			ReduceStack();
			if (treeBranch == null)
				return _SuccessStack[_Stackpos] = false;
			if (treeBranch.Name == "List")
				treeBranch.Extra = GetListType(RecursiveType);
			else
				treeBranch.Extra = RecursiveType;
			types.Add(treeBranch);
			return Default();
		}
		if (extra is not NStarType InnerNStarType)
		{
			ReduceStack();
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		if (!success)
		{
			ReduceStack();
			types.Add(new("type", pos, container) { Extra = InnerNStarType });
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		String itemName = [];
		if (pos >= end)
		{
			ReduceStack();
			types = NoBranches;
			_ExtraStack[_Stackpos - 1] = types;
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (lexems[pos].Type == LexemType.Identifier)
		{
			itemName = lexems[pos].String;
			_PosStack[_Stackpos] = ++pos;
		}
		TreeBranch branch = new("type", pos - 1, container) { Extra = InnerNStarType };
		if (collectionTypes[^1] == "tuple" && TypeEqualsToPrimitive(InnerNStarType, "tuple", false)
			&& InnerNStarType.ExtraTypes.Length == 2 && InnerNStarType.ExtraTypes[1].Name != "type"
			&& int.TryParse(InnerNStarType.ExtraTypes[1].Name.ToString(), out _))
			types.AddRange(InnerNStarType.ExtraTypes.Values);
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
			_ExtraStack[_Stackpos - 1] = types = NoBranches;
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOperator(","))
		{
			if (!typeChainTemplate[^1][tpos[^1]].Package)
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
			if (tpos[^1] >= typeChainTemplate[^1].Length - 1 || tpos[^1] >= typeChainTemplate[^1].Length - 2
				&& typeChainTemplate[^1][tpos[^1] + 1].Package)
			{
				ReduceStack();
				_ExtraStack[_Stackpos - 1] = types;
				return Default();
			}
			else
			{
				ReduceStack();
				_ExtraStack[_Stackpos - 1] = types = NoBranches;
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

	private bool TypeSingularTuple(NStarType NStarType)
	{
		if (pos < end && IsCurrentLexemOther("["))
		{
			_PosStack[_Stackpos] = ++pos;
			return TypeInt(NStarType);
		}
		else
		{
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			return Default();
		}
	}

	private bool TypeComma(NStarType NStarType)
	{
		if (pos < end && IsCurrentLexemOther(","))
		{
			_PosStack[_Stackpos] = ++pos;
			return TypeInt(NStarType);
		}
		else
			return TypeClosing(NStarType);
	}

	private bool TypeInt(NStarType NStarType)
	{
		if (pos >= end)
		{
			NStarType = NullType;
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos].String.ToString(), out var number))
		{
			if (number == 0)
				GenerateMessage(0x8004, pos, false);
			else if (number >= 2)
			{
				NStarType = new(TupleBlockStack, new(RedStarLinq.Fill(number, _ =>
					new TreeBranch("type", pos - 1, container) { Extra = NStarType })));
			}
			_PosStack[_Stackpos] = ++pos;
			return TypeClosing(NStarType);
		}
		else if (lexems[pos].Type is LexemType.UnsignedInt or LexemType.LongInt
			or LexemType.UnsignedLongInt or LexemType.Real or LexemType.Complex or LexemType.String)
		{
			_ExtraStack[_Stackpos - 1] = NullType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NullType };
			GenerateMessage(0x2017, pos, false);
			CloseBracket(ref pos, "]", ref errors!, false, end);
			return Default();
		}
		else
		{
			_ExtraStack[_Stackpos - 1] = NStarType;
			return IncreaseStack("LambdaExpr", currentTask: nameof(TypeInt2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
	}

	private bool TypeInt2()
	{
		if (!success || treeBranch == null || _ExtraStack[_Stackpos - 1] is not NStarType OuterNStarType)
			return _SuccessStack[_Stackpos] = false;
		var targetBranch = treeBranch.Name == "Expr" && treeBranch.Length == 1 ? treeBranch[0] : treeBranch;
		if (!(treeBranch.Extra == null || targetBranch.Extra is NStarType NStarType
			&& TypesAreCompatible(NStarType, IntType, out var warning, [], out _, out _) && !warning))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2017, pos, false);
			return Default();
		}
		return TypeClosing(new(TupleBlockStack, new([new("type", OuterNStarType.ExtraTypes.Length != 0
			? OuterNStarType.ExtraTypes[0].Pos : treeBranch.Pos - 2, container) { Extra = OuterNStarType }, targetBranch])));
	}

	private bool TypeClosing(NStarType NStarType)
	{
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		else if (IsCurrentLexemOther("]"))
		{
			_PosStack[_Stackpos] = ++pos;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			return Default();
		}
		else
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x200D, pos, false);
			CloseBracket(ref pos, "]", ref errors, false, end);
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
					"this" => new(new(container), NoBranches),
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
		//else if (lexems[pos].Type == LexemType.Complex)
		//{
		//	pos++;
		//	_TBStack[_Stackpos] = new(s[^1] is 'c' or 'I' ? s : s.Add('c'), pos - 1, pos, container) { Extra = RealType };
		//	return Default();
		//}
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
		else if (IsCurrentLexemOperator("typeof"))
		{
			pos++;
			if (!IsCurrentLexemOther("("))
			{
				GenerateMessage(0x200A, pos, true);
				return EndWithEmpty();
			}
			pos++;
			if (IsCurrentLexemOther(")"))
			{
				GenerateMessage(0x200E, pos, true);
				pos++;
				return EndWithEmpty();
			}
			return IncreaseStack("Expr", currentTask: nameof(BasicExpr2), pos_: pos, applyPos: true,
				applyCurrentTask: true, currentBranch: new("typeof", pos, container), assignCurrentBranch: true);
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
		else if (_TBStack[_Stackpos] != null && _TBStack[_Stackpos]!.Name == "typeof")
			return EndWithAdding(true);
		else
			return EndWithAssigning(true);
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
			if (lexems[pos - 1].String == ";" && _TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length >= 1 &&
				new List<String> { "if", "if!", "else", "else if", "else if!", "while", "while!", "repeat", "for", "loop" }
				.Contains(_TBStack[_Stackpos]?[^1].Name ?? "") && treeBranch == null)
				AppendBranch(nameof(Main), new(nameof(Main), pos - 1, pos, container));
		}
	}

	private void TransformErrorMessage()
	{
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (treeBranch != null)
			_TBStack[_Stackpos]?.Add(treeBranch);
		return Default();
	}

	private bool EndWithAssigning(bool addError)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		_TBStack[_Stackpos] = treeBranch;
		return Default();
	}

	private bool EndWithAddingOrAssigning(bool addError, int posToInsert)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0
			&& !(task == nameof(Expr2) && _TaskStack[_Stackpos - 1] != nameof(BasicExpr2) && treeBranch != null
			&& treeBranch.Name.ToString() is not ("Expr" or "Assignment" or "DeclarationAssignment")))
			_TBStack[_Stackpos] = treeBranch;
		else
			_TBStack[_Stackpos]?.Insert(posToInsert, treeBranch ?? TreeBranch.DoNotAdd());
		return Default();
	}

	private bool EndWithEmpty(bool addError = false)
	{
		if (addError)
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		_TBStack[_Stackpos] = null;
		return Default();
	}

	private void CreateObjectList(out List<object>? l) => l = (List<object>?)_ExtraStack[_Stackpos - 1];

	private void AppendBranch(String newInfo) => AppendBranch(newInfo, treeBranch ?? TreeBranch.DoNotAdd());

	private void AppendBranch(String newInfo, TreeBranch newBranch)
	{
		if (_TBStack[_Stackpos] == null)
			_TBStack[_Stackpos] = new(newInfo, newBranch, container);
		else
		{
			_TBStack[_Stackpos]!.Name = newInfo;
			_TBStack[_Stackpos]?.Add(newBranch);
		}
	}

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errors, bool produceWreck, int end = -1)
	{
		while (pos < (end == -1 ? lexems.Length : end))
		{
			if (CloseBracketIteration(ref pos, bracket, ref errors, produceWreck) is bool b)
				return b;
		}
		return false;
	}

	private bool? CloseBracketIteration(ref int pos, String bracket, ref List<String>? errors, bool produceWreck)
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
				CloseBracket(ref pos, s == "(" ? ")" : s == "[" ? "]" : "}", ref errors, produceWreck);
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
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		Messages.GenerateMessage(_ErLStack[_Stackpos], code, lexems[pos].LineN, lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			wreckOccurred = true;
	}

	private void GenerateUnexpectedEndError(bool savePrevious = false)
	{
		if (savePrevious)
			_ErLStack[_Stackpos].AddRange(errors ?? []);
		Messages.GenerateMessage(_ErLStack[_Stackpos], 0x2000, lexems[pos - 1].LineN,
			lexems[pos - 1].Pos + lexems[pos - 1].String.Length);
	}

	private void GenerateUnexpectedEndOfTypeError(ref List<String>? errors) =>
		errors?.Add("Error in line " + lexems[pos - 1].LineN.ToString() + " at position "
			+ (lexems[pos - 1].Pos + lexems[pos - 1].String.Length).ToString() + ": unexpected end of type reached");

	[GeneratedRegex("^[A-Za-zА-Яа-я_][0-9A-Za-zА-Яа-я_]*$")]
	private static partial Regex WordRegex();
}
