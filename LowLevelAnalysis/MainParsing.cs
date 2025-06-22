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
	private readonly List<List<String>?> _ErLStack = new(16, [], []);
	private readonly List<TreeBranch?> _TBStack = new(16, null, null);
	private readonly List<object?> _ExtraStack = new(16) { null };
	private readonly List<BlockStack> _ContainerStack = new(16) { new() };
	private readonly BitList _SuccessStack = new(16) { false, false };
	private readonly NList<int> _BTJPStack = new(16, 0), _RTPStack = new(16, 0), _PLPStack = new(16, 0);
	private int globalUnnamedIndex = 1;
	private int _Stackpos;

	private static readonly G.Dictionary<String, (String Next, String TreeLabel, List<String> Operators)> operatorsMapping = new() { { "Properties", ("Property", "Properties", []) }, { "Methods", ("Method", "Methods", []) }, { "Expr", ("List", "Expr", []) }, { "List", ("CommaExpr", "List", []) }, { "CommaExpr", ("SublambdaExpr", "Expr", []) }, { "SublambdaExpr", ("QuestionExpr", "Expr", []) }, { "QuestionExpr", ("XorExpr", "xorList", []) }, { "XorExpr", ("OrExpr", "Expr", new("or")) }, { "OrExpr", ("AndExpr", "Expr", new("and")) }, { "AndExpr", ("Xor2Expr", "Expr", new("^^")) }, { "Xor2Expr", ("Or2Expr", "Expr", new("||")) }, { "Or2Expr", ("And2Expr", "Expr", new("&&")) }, { "And2Expr", ("EquatedExpr", "Expr", []) }, { "EquatedExpr", ("ComparedExpr", "Expr", new(">", "<", ">=", "<=")) }, { "ComparedExpr", ("AssignedExpr", "Expr", []) }, { "AssignedExpr", ("SubhawaytennExpr", "Expr", []) }, { "SubhawaytennExpr", ("BitwiseXorExpr", "Expr", new("^")) }, { "BitwiseXorExpr", ("BitwiseOrExpr", "Expr", new("|")) }, { "BitwiseOrExpr", ("BitwiseAndExpr", "Expr", new("&")) }, { "BitwiseAndExpr", ("BitwiseShiftExpr", "Expr", new(">>", "<<")) }, { "BitwiseShiftExpr", ("PMExpr", "Expr", new("+", "-")) }, { "PMExpr", ("MulDivExpr", "Expr", new("*", "/", "%")) }, { "MulDivExpr", ("PowExpr", "Expr", new("pow")) }, { "PowExpr", ("UnaryExpr", "Expr", []) }, { "PrefixExpr", ("PostfixExpr", "Expr", []) } };
	private static readonly G.Dictionary<String, dynamic> attributesMapping = new() { { "ref", ParameterAttributes.Ref }, { "out", ParameterAttributes.Out }, { "params", ParameterAttributes.Params }, { "closed", PropertyAttributes.Closed }, { "protected", PropertyAttributes.Protected }, { "internal", PropertyAttributes.Internal }, { "public", PropertyAttributes.None } };

	public MainParsing(LexemStream lexemStream, bool wreckOccurred) : base(lexemStream)
	{
		this.wreckOccurred = wreckOccurred;
		_ErLStack[0] = errorsList;
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
				return (lexems, input, _TBStack[0] ?? TreeBranch.DoNotAdd(), errorsList, wreckOccurred);
			else
			{
				(errorsList ??= []).Add(GetWreckPosPrefix(^1) + ": main parsing failed");
				return RaiseWreck();
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			var pos2 = _Stackpos >= 0 ? _PosStack[_Stackpos] : lexems.Length - 1;
			var pos3 = pos2 >= lexems.Length ? pos2 - 1 : pos2;
			(errorsList ??= []).Add(GetWreckPosPrefix(pos3) + ": compilation failed because of internal compiler error\r\n");
			return RaiseWreck();
		}
	}

	private String GetWreckPosPrefix(Index pos) => "Wreck in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString();

	private (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) RaiseWreck() => EmptySyntaxTree();

	private delegate bool ParseAction();

	private bool MainParseAction()
	{
		(String Next, String TreeLabel, List<String> Operators) taskWithoutIndex;
		var task = this.task.ToString();
		return task switch
		{
			"Parameters" => IncreaseStack(nameof(Parameter), currentTask: "Parameters2", applyCurrentTask: true, currentExtra: new List<object>()),
			"Properties" or "Methods" or "Expr" or "List" or "CommaExpr" or "SublambdaExpr" or "QuestionExpr" or "XorExpr" or "OrExpr" or "AndExpr" or "Xor2Expr" or "Or2Expr" or "And2Expr" or "EquatedExpr" or "ComparedExpr" or "AssignedExpr" or "SubhawaytennExpr" or "BitwiseXorExpr" or "BitwiseOrExpr" or "BitwiseAndExpr" or "BitwiseShiftExpr" or "PMExpr" or "MulDivExpr" or "PowExpr" or "PrefixExpr" => IncreaseStack(operatorsMapping[this.task].Next, currentTask: this.task + "2", applyCurrentTask: true, currentBranch: new(operatorsMapping[this.task].TreeLabel, pos, pos + 1, container), assignCurrentBranch: true),
			"Properties2" => Properties2_Methods2("Property", PropertiesAction),
			"Property" => IncreaseStack(nameof(ClearProperty), currentTask: "Property2", applyCurrentTask: true, currentExtra: new List<object>()),
			"Methods2" => Properties2_Methods2("Method", () =>
			{
				if (!IsCurrentLexemOther("}"))
					GenerateError(pos, "expected: methods or }", true);
			}),
			"Method" => IncreaseStack(nameof(Function), currentTask: "Method2", applyCurrentTask: true, currentExtra: new List<object>()),
			"ActionChain2" => ActionChain2_3_4(nameof(Cycle), "ActionChain3"),
			"ActionChain3" => ActionChain2_3_4(nameof(SpecialAction), "ActionChain4"),
			"ActionChain4" => ActionChain2_3_4(nameof(Return), "ActionChain5"),
			"XorExpr2" or "OrExpr2" or "AndExpr2" or "Xor2Expr2" or "Or2Expr2" or "EquatedExpr2" or "SubhawaytennExpr2" or "BitwiseXorExpr2" or "BitwiseOrExpr2" or "BitwiseAndExpr2" or "BitwiseShiftExpr2" or "PMExpr2" => LeftAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next, taskWithoutIndex.Operators),
			"MulDivExpr2" => RightAssociativeOperatorExpr2((taskWithoutIndex = operatorsMapping[this.task[..^1]]).Next, taskWithoutIndex.Operators),
			"PostfixExpr" => IncreaseStack(nameof(Hypername), currentTask: "PostfixExpr3", applyCurrentTask: true, currentBranch: new("Expr", pos, pos + 1, container), assignCurrentBranch: true),
			"PostfixExpr2" => PostfixExpr2_3(nameof(Type), "PostfixExpr3"),
			"PostfixExpr3" => PostfixExpr2_3(nameof(BasicExpr), "PostfixExpr4"),
			"Indexes" => IncreaseStack("CommaExpr", currentTask: "Indexes2", applyCurrentTask: true, currentBranch: new("Indexes", pos, pos + 1, container), assignCurrentBranch: true),
			_ => MainParseDelegate(task)(),
		};
	}

	private ParseAction MainParseDelegate(string task) => task switch
	{
			nameof(Main) => Main,
			"Main2" => Main2,
			"Main3" => Main3,
			"Main4" => Main4,
			"Main5" => Main5,
			"Main}" => MainClosing,
			nameof(Namespace) => Namespace,
			"Namespace}" => NamespaceClosing,
			nameof(Class) => Class,
			"Class2" => Class2,
			"Class}" => ClassClosing,
			nameof(Function) => Function,
			"Function2" => Function2,
			"Function3" => Function3,
			"Function}" => FunctionClosing,
			nameof(Constructor) => Constructor,
			"Constructor2" => Constructor2,
			"Constructor}" => ConstructorClosing,
			"Parameters2" or "Parameters3" => Parameters2_3,
			"Parameters4" => Parameters4,
			nameof(Parameter) => Parameter,
			"Parameter2" => Parameter2,
			"Parameter3" => Parameter3,
			nameof(ClassMain) => ClassMain,
			"ClassMain2" => ClassMain2,
			"ClassMain3" => ClassMain3,
			"ClassMain4" => ClassMain4,
			"Property2" => Property2,
			"Property3" => Property3,
			nameof(ClearProperty) => ClearProperty,
			"ClearProperty2" => ClearProperty2,
			"ClearProperty3" => ClearProperty3,
			"Method2" => Method2,
			"Method3" => Method3,
			nameof(ActionChain) => ActionChain,
			"ActionChain5" => ActionChain5,
			"ActionChain6" => ActionChain6,
			nameof(Condition) => Condition,
			"Condition2" or "WhileRepeat2" or "For3" => Condition2_WhileRepeat2_For3,
			nameof(Cycle) => Cycle,
			"Cycle2" => Cycle2,
			"Cycle3" => Cycle3,
			nameof(WhileRepeat) => WhileRepeat,
			nameof(For) => For,
			"For2" => For2,
			nameof(SpecialAction) => SpecialAction,
			nameof(Return) => Return,
			"Return2" => Return2,
			"Expr2" => Expr2,
			"List2" => List2,
			"CommaExpr2" or "AssignedExpr2" or "PowExpr2" or "UnaryExpr4" or "PostfixExpr4" => CommaExpr3_AssignedExpr2_PowExpr2_UnaryExpr4_PostfixExpr4,
			"SublambdaExpr2" => SublambdaExpr2,
			"SublambdaExpr3" => SublambdaExpr3,
			"QuestionExpr2" => QuestionExpr2,
			"And2Expr2" => And2Expr2,
			"And2Expr3" => And2Expr3,
			"ComparedExpr2" => ComparedExpr2,
			nameof(UnaryExpr) => UnaryExpr,
			"UnaryExpr2" => UnaryExpr2,
			"UnaryExpr3" => UnaryExpr3,
			"PrefixExpr2" => PrefixExpr2,
			nameof(Hypername) or "HypernameNotCall" => Hypername,
			"HypernameNew" => HypernameNew,
			"HypernameType" or "HypernameNotCallType" => HypernameType,
			nameof(HypernameBasicExpr) or "HypernameNotCallBasicExpr" => HypernameBasicExpr,
			nameof(HypernameCall) => HypernameCall,
			nameof(HypernameIndexes) or "HypernameNotCallIndexes" => HypernameIndexes,
			"HypernameClosing" or "HypernameNotCallClosing" or "BasicExpr4" => HypernameClosing_BasicExpr4,
			"Indexes2" => Indexes2,
			nameof(Type) or nameof(TypeConstraints.BaseClassOrInterface) or nameof(TypeConstraints.NotAbstract) => Type,
			nameof(BasicExpr) => BasicExpr,
			"BasicExpr2" => BasicExpr2,
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
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
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
			return IncreaseStack(nameof(Namespace), currentTask: "Main5", applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Class)))
			return IncreaseStack(nameof(Class), currentTask: "Main5", applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Function)))
			return IncreaseStack(nameof(Function), currentTask: "Main5", applyPos: true, applyCurrentTask: true);
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: "Main}", pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed, "#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: "Main5", applyPos: true, applyCurrentTask: true);
	}

	private bool Main2() => success ? NewMainTask() : IncreaseStack(nameof(Class), currentTask: "Main3", applyCurrentTask: true);

	private bool Main3() => success ? NewMainTask() : IncreaseStack(nameof(Function), currentTask: "Main4", applyCurrentTask: true);

	private bool Main4()
	{
		if (success)
			return NewMainTask();
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: "Main}", pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(BlockType.Unnamed, "#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: "Main5", applyCurrentTask: true);
	}

	private bool Main5()
	{
		if (success)
		{
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
		else
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return Default();
		}
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
			return EndWithError(pos, "expected: }");
	}

	private bool GoDownWithPos()
	{
		_PosStack[_Stackpos] = pos;
		_TaskStack[_Stackpos] = "Main4";
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
			CloseBracket(ref pos, "}", ref errorsList, end);
			GenerateError(pos2, "expected: }", true);
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
			return CheckColonAndAddTask(nameof(TypeConstraints.BaseClassOrInterface), "Class2", BlockType.Class);
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
			CloseBracket(ref pos, "}", ref errorsList, end);
			GenerateError(pos2, "expected: }", true);
			return EndWithAdding(false);
		}
	}

	private bool Function()
	{
		if (CheckBlockToJump(nameof(Function)))
		{
			if (registeredTypesPos < registeredTypes.Length && blocksToJump[blocksToJumpPos].Start >= pos && blocksToJump[blocksToJumpPos].End <= end)
				return IncreaseStack(nameof(Type), currentTask: "Function2", pos_: blocksToJump[blocksToJumpPos].Start, applyCurrentTask: true, start_: blocksToJump[blocksToJumpPos].Start, end_: blocksToJump[blocksToJumpPos].End, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes), rtp: registeredTypesPos + 1);
			else
			{
				_TaskStack[_Stackpos] = "Function2";
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
			if (success && extra is UniversalType UnvType)
			{
				var t = UserDefinedFunctionsList[blocksToJump[blocksToJumpPos].Container][blocksToJump[blocksToJumpPos].Name][0];
				t.ReturnUnvType = UnvType;
				UserDefinedFunctionsList[blocksToJump[blocksToJumpPos].Container][blocksToJump[blocksToJumpPos].Name][0] = t;
				_TBStack[_Stackpos] = new(nameof(Function), [new(blocksToJump[blocksToJumpPos].Name, blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container), treeBranch ?? TreeBranch.DoNotAdd()], container);
				return;
			}
			_TBStack[_Stackpos] = new(nameof(Function), [new(blocksToJump[blocksToJumpPos].Name, blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container), new("type", blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container) { Extra = NullType }], container);
		}
		if (CheckBlockToJump2(nameof(Function)))
		{
			CheckSuccess();
			TransformErrorMessage2();
			if (parameterListsPos < parameterLists.Length && parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start && parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
				return IncreaseStack("Parameters", currentTask: "Function3", pos_: parameterLists[parameterListsPos].Start, applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start, end_: parameterLists[parameterListsPos].End, currentExtra: new GeneralMethodParameters(), plp: parameterListsPos + 1);
			else
			{
				_TaskStack[_Stackpos] = "Function3";
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
			if (success)
			{
				if (extra is GeneralMethodParameters parameters && parameters.Length != 0)
				{
					var t = UserDefinedFunctionsList[blocksToJump[blocksToJumpPos].Container][blocksToJump[blocksToJumpPos].Name][0];
					t.Parameters = parameters;
					UserDefinedFunctionsList[blocksToJump[blocksToJumpPos].Container][blocksToJump[blocksToJumpPos].Name][0] = t;
					_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
					return;
				}
			}
			_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1, blocksToJump[blocksToJumpPos].End, container));
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

	private bool FunctionClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(pos, "expected: }");

	private bool Constructor()
	{
		if (CheckBlockToJump2(nameof(Constructor)))
		{
			_TBStack[_Stackpos] = new(nameof(Constructor), blocksToJump[blocksToJumpPos].Start, blocksToJump[blocksToJumpPos].End, container);
			TransformErrorMessage2();
			if (parameterListsPos < parameterLists.Length && parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start && parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End)
				return IncreaseStack("Parameters", currentTask: "Constructor2", pos_: parameterLists[parameterListsPos].Start, applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start, end_: parameterLists[parameterListsPos].End, currentExtra: new GeneralMethodParameters(), plp: parameterListsPos + 1);
			else
			{
				_TaskStack[_Stackpos] = "Constructor2";
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
			if (success && extra is GeneralMethodParameters parameters && parameters.Length != 0)
			{
				var container2 = blocksToJump[blocksToJumpPos].Container;
				var index = UserDefinedConstructorIndexesList[container2][blocksToJump[blocksToJumpPos].Start];
				var t = UserDefinedConstructorsList[container2][index];
				t.Parameters = parameters;
				UserDefinedConstructorsList[container2][index] = t;
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
				return;
			}
			_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1, blocksToJump[blocksToJumpPos].End, container));
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

	private bool ConstructorClosing() => IsClosingFigureBracket() ? EndWithAdding(true) : EndWithError(pos, "expected: }");

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
			return EndWithError(pos, "expected: \":\"");
	}

	private bool CheckOpeningBracketAndAddTask(String newTask, String closingString, BlockType blockType, String? name = null)
	{
		if (IsCurrentLexemOther("{"))
			return IncreaseStack(newTask, currentTask: closingString, pos_: pos + 1, applyPos: true, applyCurrentTask: true, container_: new(container.ToList().Append(new(blockType, name ?? blocksToJump[blocksToJumpPos].Name, 1))), btjp: blocksToJumpPos + 1);
		else
			return EndWithError(pos, "expected: {");
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
			return EndWithError(pos, "incorrect construction in parameters list");
	}

	private bool Parameters4()
	{
		if (pos >= end)
			return ParametersEnd();
		else if (IsCurrentLexemOperator(","))
			return EndWithError(pos, "parameter with the \"params\" keyword must be last in the list");
		else
			return EndWithError(pos, "incorrect construction in parameters list");
	}

	private bool ParametersEnd()
	{
		_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
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
				_TaskStack[_Stackpos - 1] = "Parameters4";
			AddPropertyAttribute(attributesMapping[lexems[pos].String], nameof(Parameter));
		}
		else
			AddPropertyAttribute(ParameterAttributes.None, nameof(Parameter), false);
		while (IsParameterModifier())
		{
			pos++;
			GenerateError(pos - 1, "incorrect word or order of words in construction declaration");
		}
		return IncreaseStack(nameof(Type), currentTask: "Parameter2", pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes));
	}

	private bool Parameter2()
	{
		if (_TBStack[_Stackpos] != null && _ExtraStack[_Stackpos - 1] is List<object> list && list[0] is ParameterAttributes attributes)
			_TBStack[_Stackpos]!.Extra = attributes;
		if (!AddExtraAndIdentifier())
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(new("", pos, pos + 1, container));
			CheckParameters3(true, true);
			return Default();
		}
		if (IsCurrentLexemOperator("="))
		{
			if (_TaskStack[_Stackpos - 1] != "Parameters4")
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
			return IncreaseStack("Expr", currentTask: "Parameter3", pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentBranch: new("optional", pos, pos + 1, container), addCurrentBranch: true);
		}
		else
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
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
		_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		try
		{
			CreateObjectList(out var l);
			var (s, arrayParameters) = (UniversalType)l![1];
			_ExtraStack[_Stackpos - 1] = new GeneralMethodParameter(s, (String)l[2], arrayParameters, (ParameterAttributes)l[0], "null");
			var index = VariablesList.IndexOfKey(container);
			if (index == -1)
			{
				VariablesList.Add(container, []);
				index = VariablesList.IndexOfKey(container);
			}
			var list = VariablesList.Values[index];
			list.Add((String)l[2], (s, arrayParameters));
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
			GenerateError(pos, "parameters without default value and without \"params\" modifier must appear before parameters with default value" + (expectIdentifier ? "; expected: identifier" : ""));
			_TBStack[_Stackpos]?.Add(new("null", pos, pos + 1, container));
		}
		else
			_TBStack[_Stackpos]?.Add(new("no optional", pos, pos + 1, container));
		try
		{
			CreateObjectList(out var l);
			var (s, arrayParameters) = (UniversalType)l![1];
			_ExtraStack[_Stackpos - 1] = new GeneralMethodParameter(s, skipParameterName ? "" : (String)l[2], arrayParameters, (ParameterAttributes)l[0], "null");
		}
		catch
		{
		}
	}

	private bool ClassMain()
	{
		SkipSemicolonsAndNewLines();
		return IncreaseStack(nameof(Class), currentTask: "ClassMain2", pos_: pos, applyPos: true, applyCurrentTask: true, currentBranch: new(nameof(ClassMain), pos, pos + 1, container), assignCurrentBranch: true);
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
			return IncreaseStack("Properties", currentTask: "ClassMain3", pos_: pos, applyPos: true, applyCurrentTask: true);
		}
	}

	private bool ClassMain3()
	{
		if (success && treeBranch != null && treeBranch.Length != 0)
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		}
		SkipSemicolonsAndNewLines();
		return IncreaseStack("Methods", currentTask: "ClassMain4", applyCurrentTask: true);
	}

	private bool ClassMain4()
	{
		if (success && treeBranch != null && treeBranch.Length != 0)
		{
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		}
		return Default();
	}

	private bool Properties2_Methods2(String newTask, Action action)
	{
		if (success)
		{
			SkipSemicolonsAndNewLines();
			return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true, currentBranch: treeBranch, addCurrentBranch: true);
		}
		else
		{
			action();
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
					list[1].Parameters.Add(new(property.Value.UnvType.MainType, property.Key, property.Value.UnvType.ExtraTypes, ParameterAttributes.Optional, "null"));
		if (UserDefinedPropertiesList.TryGetValue(container, out propertiesList) && propertiesList.Length != 0 && UserDefinedPropertiesOrder.TryGetValue(container, out propertiesOrder) && propertiesOrder.Length != 0)
		{
			foreach (var propertyName in propertiesOrder)
			{
				if (propertiesList.TryGetValue(propertyName, out var property)/* && property.Attributes is PropertyAttributes.None or PropertyAttributes.Internal*/)
					list[1].Parameters.Add(new(property.UnvType.MainType, propertyName, property.UnvType.ExtraTypes, ParameterAttributes.Optional, "null"));
			}
			increment++;
		}
		if (UserDefinedConstructorIndexesList.TryGetValue(container, out var list2))
		{
			for (var i = 0; i < list2.Length; i++)
				list2[list2.Keys[i]] += increment;
		}
	}

	private bool Property2()
	{
		if (success)
			return EndWithAssigning(true);
		else
		{
			_TaskStack[_Stackpos] = "Property3";
			return true;
		}
	}

	private bool Property3()
	{
		if (success)
			return EndWithAssigning(true);
		else
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool ClearProperty()
	{
		if (IsLexemKeyword(lexems[pos], ["closed", "protected", "internal", "public"]))
		{
			AddPropertyAttribute(attributesMapping[lexems[pos].String], "Property");
			if (lexems[pos].String == "public")
				GenerateError(pos - 1, "properties can only have proper access, no public");
		}
		else
			AddPropertyAttribute(PropertyAttributes.None, "Property", false);
		var value = (IsCurrentLexemKeyword("static") ? 2 : 0) + ((UserDefinedTypesList[SplitType(container)].Attributes
			& TypeAttributes.Static) == TypeAttributes.Static ? 1 : 0);
		if (value == 3)
			GenerateMessage("Warning", pos,
				"properties and methods are static in the static class implicitly; word \"static\" is not necessary");
		if (value >= 1)
			AddPropertyAttribute2(PropertyAttributes.Static);
		if (value >= 2)
			pos++;
		return IncreaseStack(nameof(Type), currentTask: "ClearProperty2", pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new UniversalType(new BlockStack(), NoGeneralExtraTypes));
	}

	private bool ClearProperty2()
	{
		bool CheckIdentifier(String string_)
		{
			if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == string_)
			{
				pos++;
				return true;
			}
			else
				return EndWithError(pos, "expected: \"" + string_ + "\"");
		}
		if (!AddExtraAndIdentifier())
			return EndWithError(pos, "expected: identifier");
		if (IsCurrentLexemOther("{"))
			pos++;
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
			return IncreaseStack("Expr", currentTask: "ClearProperty3", pos_: pos + 1, applyPos: true, applyCurrentTask: true);
		else
		{
			if (!CheckLexemAndEndWithError(";", true))
				return false;
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Add(new("null", pos - 1, container));
			return AddUserDefinedProperty();
		}
	}

	private bool ClearProperty3()
	{
		if (success)
		{
			if (IsCurrentLexemOther(";"))
				pos++;
			else
				return EndWithError(pos, "expected: \";\"");
		}
		else
			return _SuccessStack[_Stackpos] = false;
		_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
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
				t.Decomposition.Add(name, UnvType);
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

	private bool CheckLexemAndEndWithError(String string_, bool addQuotes = false)
	{
		if (IsLexemOther(lexems[pos], string_))
		{
			pos++;
			return true;
		}
		else
			return EndWithError(pos, "expected: " + (addQuotes ? "\"" + string_ + "\"" : string_));
	}

	private bool Method2() => success ? EndWithAssigning(true) : IncreaseStack(nameof(Constructor), currentTask: "Method3", applyCurrentTask: true);

	private bool Method3()
	{
		if (success)
			return EndWithAssigning(true);
		else
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return _SuccessStack[_Stackpos] = false;
		}
	}

	private bool ActionChain()
	{
		SkipSemicolonsAndNewLines();
		if (pos >= end)
			return Default();
		else if (IsCurrentLexemOther("{") || IsCurrentLexemOther("}"))
			return Default();
		if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "goto")
		{
			var pos2 = pos + 1;
			if (lexems[pos2].Type == LexemType.Identifier)
			{
				GenerateError(pos, "goto is a bad operator, it worsens the organization of the code; C#.NStar refused from its using intentionally", true);
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
		} : "Expr", out var newTask), currentTask: newTask == "Expr" ? "ActionChain6" : "ActionChain5", applyCurrentTask: true);
	}

	private bool ActionChain2_3_4(String newTask, String currentTask)
	{
		if (success)
		{
			_TaskStack[_Stackpos] = nameof(ActionChain);
			TransformErrorMessage();
			if (treeBranch != null && (treeBranch.Info != "" || treeBranch.Length != 0))
				AppendBranch(nameof(ActionChain));
			_TBStack[_Stackpos + 1] = null;
			return true;
		}
		else
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
	}

	private bool ActionChain5()
	{
		if (success)
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			if (treeBranch != null && treeBranch.Length != 0)
			{
				AppendBranch(nameof(ActionChain));
				return Default();
			}
			else
				return EndActionChain();
		}
		else
			return IncreaseStack("Expr", currentTask: "ActionChain6", pos_: pos, applyPos: true, applyCurrentTask: true, currentExtra: new List<object>());
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
			GenerateError(pos, "keyword \"" + lexems[pos].String + "\" is under development");
		else if (!IsLexemOther(lexems[pos], StopLexemsList))
			GenerateError(pos, "unrecognized construction");
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
		if (IsLexemOther(lexems[pos], ["{", "}"]))
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return Default();
		}
		else
			pos++;
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
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				return Default();
			}
		}
		else
			return EndWithError(pos, "expected: \"if\" or \"else\"");
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
				GenerateError(pos, "expected: )", true);
				return EndWithEmpty();
			}
		}
		else
		{
			GenerateError(pos, "expected: expression", true);
			return EndWithEmpty();
		}
		_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		if (pos < end && IsCurrentLexemOther(";") && lexems[pos].LineN == lexems[pos - 1].LineN)
			GenerateMessage("Warning", pos, "semicolon in the end of the line with condition or cycle may easily be unnoticed and lead to hard-catchable errors");
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
			GenerateError(pos, "expected: " + (addQuotes ? "\"" + string_ + "\"" : string_), true);
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
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return Default();
		}
		return lexems[pos].Type == LexemType.Keyword ? lexems[pos].String.ToString() switch
		{
			"while" or "repeat" => IncreaseStack(nameof(WhileRepeat), currentTask: "Cycle3", pos_: pos, applyPos: true, applyCurrentTask: true),
			"for" => IncreaseStack(nameof(For), currentTask: "Cycle3", pos_: pos, applyPos: true, applyCurrentTask: true),
			_ => EndWithError(pos, "expected: \"loop\" or \"while\" or \"repeat\" or \"for\""),
		} : EndWithError(pos, "expected: \"loop\" or \"while\" or \"repeat\" or \"for\"");
	}

	private bool Cycle2() => success ? EndWithAssigning(true) : IncreaseStack(nameof(For), currentTask: "Cycle3", applyCurrentTask: true);

	private bool Cycle3() => success ? EndWithAssigning(true) : EndWithError(pos, "expected: \"loop\" or \"while\" or \"repeat\" or \"for\"");

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
			return IncreaseStack(nameof(Type), currentTask: "For2", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool For2()
	{
		if (success)
		{
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
				GenerateError(pos, "expected: identifier", true);
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
				GenerateError(pos, "expected: \"in\"", true);
				return EndWithEmpty();
			}
		}
		else
			return EndWithEmpty(true);
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
				GenerateError(pos, "expected: \";\"", true);
				return EndWithEmpty();
			}
			return Default();
		}
		else
			return EndWithError(pos, "expected: \"continue\" or \"break\"");
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
			GenerateMessage("Warning", pos, "syntax \"return;\" is deprecated; consider using \"return null;\" instead");
			pos++;
			_TBStack[_Stackpos]?.Add(new("null", pos - 1, container));
			return Default();
		}
		return IncreaseStack("Expr", currentTask: "Return2", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool Return2()
	{
		if (success)
		{
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOther(";"))
			{
				pos++;
				return EndWithAdding(true);
			}
			else
			{
				GenerateError(pos, "expected: \";\"", true);
				return EndWithEmpty();
			}
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Expr2()
	{
		if (success)
		{
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
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool List2()
	{
		if (success)
		{
			if (pos < end && IsCurrentLexemOperator(","))
			{
				if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall) || _TaskStack[_Stackpos - 1] == "Expr2" && _TaskStack[_Stackpos - 2] == "BasicExpr2")
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
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall) || _TBStack[_Stackpos] != null && _TBStack[_Stackpos]?.Length != 0 && _TaskStack[_Stackpos - 1] == "Expr2" && _TaskStack[_Stackpos - 2] == "BasicExpr2")
					_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
				else
					_TBStack[_Stackpos] = treeBranch;
				return Default();
			}
			return IncreaseStack("CommaExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool CommaExpr2()
	{
		if (success)
			return EndWithAssigning(true);
		else
			return IncreaseStack("SublambdaExpr", currentTask: "CommaExpr3", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool CommaExpr3_AssignedExpr2_PowExpr2_UnaryExpr4_PostfixExpr4() => success ? EndWithAssigning(true) : (_SuccessStack[_Stackpos] = false);

	private bool SublambdaExpr2()
	{
		if (success)
		{
			if (pos < end && IsLexemOperator(lexems[pos], ["?", "?=", "?>", "?<", "?>=", "?<=", "?!="]))
			{
				pos++;
				_TaskStack[_Stackpos] = "SublambdaExpr3";
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
				_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			}
			else
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool SublambdaExpr3()
	{
		if (success)
		{
			if (pos < end && IsCurrentLexemOperator(":"))
			{
				pos++;
				_TaskStack[_Stackpos] = "SublambdaExpr2";
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
				_TBStack[_Stackpos]?.Add(new(":", pos - 1, pos, container));
			}
			else
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool QuestionExpr2()
	{
		if (success)
		{
			if (pos < end && IsCurrentLexemOperator("xor"))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			}
			else
			{
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0)
					_TBStack[_Stackpos] = treeBranch;
				else
					_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
				return Default();
			}
			return IncreaseStack("XorExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool And2Expr2()
	{
		if (success)
		{
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
					return IncreaseStack(nameof(Type), currentTask: "And2Expr3", applyCurrentTask: true, pos_: pos, applyPos: true, applyCurrentErl: true);
			}
			else if (pos < end && IsLexemOperator(lexems[pos], ["==", "!="]))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
				_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			}
			else
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			return IncreaseStack("EquatedExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool And2Expr3()
	{
		if (success)
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else
			{
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				return Default();
			}
		}
		else
		{
			GenerateError(pos, "at present time the \"is\" operator can be used only with \"null\" and types");
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), (TreeBranch)new("null", pos, pos + 1, container));
			return Default();
		}
	}

	private bool ComparedExpr2()
	{
		if (success)
		{
			if (pos < end && IsLexemOperator(lexems[pos], ["=", "+=", "-=", "*=", "/=", "%=", "pow=", "&=", "|=", "^=", ">>=", "<<="]))
			{
				if (treeBranch != null && (treeBranch.Info == "Declaration" || treeBranch.Info == nameof(Hypername)))
				{
					pos++;
					_TBStack[_Stackpos]?.Insert(0, [treeBranch, new(lexems[pos - 1].String, pos - 1, pos, container)]);
				}
				else
					goto l0;
			}
			else
				return EndWithAddingOrAssigning(true, 0);
			return IncreaseStack("AssignedExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		l0:
			GenerateError(pos, "only variables can be assigned");
			_TBStack[_Stackpos]?.Insert(0, new TreeBranch("null", pos, pos + 1, container));
			while (!IsLexemOther(lexems[pos], StopLexemsList))
				pos++;
			return Default();
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool LeftAssociativeOperatorExpr2(String newTask, List<String> operatorsList)
	{
		if (success)
		{
			if (pos < end && lexems[pos].Type == LexemType.Operator && operatorsList.Contains(lexems[pos].String))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
				_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			}
			else
			{
				if (treeBranch != null && treeBranch.Info.ToString() is "0" or "0i" or "0u" or "0L" or "0uL" or "\"0\"" && _TBStack[_Stackpos] != null && _TBStack[_Stackpos]!.Length != 0 && _TBStack[_Stackpos]![^1].Info.ToString() is "/" or "%")
					GenerateMessage("Error", pos, "division by integer zero is forbidden", true);
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			}
			return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool RightAssociativeOperatorExpr2(String newTask, List<String> operatorsList)
	{
		if (success)
		{
			if (pos < end && lexems[pos].Type == LexemType.Operator && operatorsList.Contains(lexems[pos].String))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(0, [treeBranch ?? TreeBranch.DoNotAdd(), new(lexems[pos - 1].String, pos - 1, pos, container)]);
			}
			else
				return EndWithAddingOrAssigning(true, 0);
			return IncreaseStack(newTask, pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
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
		return IncreaseStack("UnaryExpr2", currentTask: isPrefix ? "UnaryExpr3" : "UnaryExpr4", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr2()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator && new List<String> { "ln", "#", "$" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			return IncreaseStack("UnaryExpr2", currentTask: "UnaryExpr3", pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		else if (lexems[pos].Type == LexemType.Operator && new List<String> { "++", "--", "sin", "cos", "tan", "asin", "acos", "atan" }.Contains(lexems[pos].String))
		{
			pos++;
			_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String, pos - 1, pos, container));
			isPrefix = true;
		}
		return IncreaseStack("PrefixExpr", currentTask: isPrefix ? "UnaryExpr3" : "UnaryExpr4", pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr3()
	{
		if (success)
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			return Default();
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool PrefixExpr2()
	{
		if (success)
		{
			if (pos < end && IsLexemOperator(lexems[pos], ["++", "--", "!", "!!"]))
			{
				pos++;
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
				_TBStack[_Stackpos]?.Add(new(lexems[pos - 1].String == "!!" ? "!!" : "postfix " + lexems[pos - 1].String, pos - 1, pos, container));
			}
			else
				return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
			_PosStack[_Stackpos] = pos;
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return Default();
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool PostfixExpr2_3(String newTask, String currentTask)
	{
		if (success)
		{
			if (treeBranch != null && treeBranch.Info == nameof(Hypername) && treeBranch.Length == 1 && (!WordRegex().IsMatch(treeBranch[0].Info.ToString()) || new List<String> { "_", "true", "false", "this", "base", "null", "Infty", "Uncty", "Pi", "E" }.Contains(treeBranch[0].Info) || treeBranch[0].Length != 0))
			{
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				_TBStack[_Stackpos] = treeBranch[0];
				return Default();
			}
			else
				return EndWithAssigning(true);
		}
		else
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
	}

	private bool Hypername()
	{
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemKeyword("new"))
		{
			if (task == "HypernameNotCall" || _TaskStack[_Stackpos - 1] == "HypernameClosing" || _TaskStack[_Stackpos - 1] == "HypernameNotCallClosing")
				return EndWithError(pos, "the \"new\" keyword is forbidden here", true);
			pos++;
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOther("("))
				return EndWithError(pos, "the \"new\" keyword with implicit type is under development", true);
			else
				return IncreaseStack(nameof(TypeConstraints.NotAbstract), currentTask: "HypernameNew", applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		else
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
				return EndWithError(pos, "expected: (", true);
		}
		else
		{
			if (errorsList != null)
				_ErLStack[_Stackpos]?.AddRange(errorsList);
			return Default();
		}
	}

	private bool HypernameType()
	{
		if (success)
		{
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (lexems[pos].Type == LexemType.Identifier)
			{
				pos++;
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
				AppendBranch("Declaration", new(lexems[pos - 1].String, pos - 1, pos, container));
				return HypernameDeclaration();
			}
			else if (IsCurrentLexemOperator("."))
				return IncreaseStack(task == "HypernameNotCallType" ? "HypernameNotCall" : nameof(Hypername), currentTask: task == "HypernameNotCallType" ? "HypernameNotCallClosing" : "HypernameClosing", pos_: pos + 1, applyPos: true, applyCurrentTask: true, applyCurrentErl: true, currentBranch: new(".", pos, container), addCurrentBranch: true);
			else if (extra is UniversalType UnvType)
			{
				if (UnvType.MainType.Length == 1 && !UnvType.MainType.Peek().Name.Contains(' ') && UnvType.ExtraTypes.Length == 0)
				{
					_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
					_TBStack[_Stackpos]![0] = new(UnvType.MainType.Peek().Name, treeBranch?.Pos ?? -1, treeBranch?.Container ?? new());
				}
				else
					return EndWithError(pos, "expected: identifier", true);
			}
			else
				return EndWithError(pos, "expected: identifier", true);
		}
		else
		{
			errorsList?.Clear();
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (lexems[pos].Type == LexemType.Identifier)
			{
				pos++;
				AppendBranch(nameof(Hypername), new(lexems[pos - 1].String, pos - 1, pos, container));
			}
			else if (_TaskStack[_Stackpos - 1].ToString() is not "HypernameClosing" and not "HypernameNotCallClosing")
				return IncreaseStack(nameof(BasicExpr), currentTask: nameof(HypernameBasicExpr), applyCurrentTask: true);
			else
				return EndWithError(pos, "expected: identifier", true);
		}
		return HypernameBracketsAndDot();
	}

	private bool HypernameBasicExpr()
	{
		if (success)
			AppendBranch(nameof(Hypername));
		else
		{
			_TBStack[_Stackpos] = null;
			return IsCurrentLexemOther(")") ? Default() : EndWithError(pos, "expected: identifier or basic expr or expr in round brackets", true);
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
			return EndWithError(pos, "expected: )", true);
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
			return EndWithError(pos, "expected: ]", true);
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
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? new());
			return Default();
		}
	}

	private bool HypernameClosing_BasicExpr4() => success ? pos >= end ? (_SuccessStack[_Stackpos] = false) : EndWithAdding(true) : (_SuccessStack[_Stackpos] = false);

	private bool HypernameDeclaration()
	{
		if (extra is UniversalType UnvType)
		{
			var index = VariablesList.IndexOfKey(container);
			if (index == -1)
			{
				VariablesList.Add(container, []);
				index = VariablesList.IndexOfKey(container);
			}
			var list = VariablesList.Values[index];
			list[lexems[pos - 1].String] = UnvType;
		}
		return Default();
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
				_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
			return Default();
		}
	}

	private bool Indexes2()
	{
		if (success)
		{
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOperator(","))
			{
				pos++;
				_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			}
			else
				return EndWithAdding(true);
			return IncreaseStack("CommaExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool Type()
	{
		var pos2 = pos;
		if (ParseType(ref pos, end, container, out var UnvType, ref errorsList!, constraints: task.ToString() switch
		{
			nameof(TypeConstraints.BaseClassOrInterface) => TypeConstraints.BaseClassOrInterface,
			nameof(TypeConstraints.NotAbstract) => TypeConstraints.NotAbstract,
			_ => TypeConstraints.None,
		}))
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList);
			_TBStack[_Stackpos] = new("type", pos2, container) { Extra = UnvType };
			_ExtraStack[_Stackpos - 1] = UnvType;
			return Default();
		}
		else
		{
			_ErLStack[_Stackpos]?.AddRange(errorsList);
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
			_TBStack[_Stackpos] = new(s, pos - 1, pos, container);
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Int)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'i' ? s : s.Add('i'), pos - 1, pos, container);
			return Default();
		}
		else if (lexems[pos].Type == LexemType.LongInt)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'L' ? s : s.Add('L'), pos - 1, pos, container);
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Real)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'r' ? s : s.Add('r'), pos - 1, pos, container);
			return Default();
		}
		else if (lexems[pos].Type == LexemType.String)
		{
			pos++;
			_TBStack[_Stackpos] = new(s, pos - 1, pos, container);
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Other && s == "(")
			return IncreaseStack("Expr", currentTask: "BasicExpr2", pos_: pos + 1, applyPos: true, applyCurrentTask: true, currentBranch: new("Expr", pos, container), assignCurrentBranch: true);
		else
			return _SuccessStack[_Stackpos] = false;
	}

	private bool BasicExpr2()
	{
		if (success)
		{
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			else if (IsCurrentLexemOther(")"))
				pos++;
			else
				return EndWithError(pos, "expected: )", true);
		}
		else
			return _SuccessStack[_Stackpos] = false;
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
		_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
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

	private bool EndWithError(Index pos, String text, bool result = false)
	{
		GenerateError(pos, text, true);
		_SuccessStack[_Stackpos] = result;
		return false;
	}

	private bool EndWithAdding(bool addError)
	{
		if (addError)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		if (treeBranch != null)
			_TBStack[_Stackpos]?.Add(treeBranch);
		return Default();
	}

	private bool EndWithAssigning(bool addError)
	{
		if (addError)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		_TBStack[_Stackpos] = treeBranch;
		return Default();
	}

	private bool EndWithAddingOrAssigning(bool addError, int posToInsert)
	{
		if (addError)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0 && !(task == "Expr2" && _TaskStack[_Stackpos - 1] != "BasicExpr2" && treeBranch != null && treeBranch.Info != "Expr"))
			_TBStack[_Stackpos] = treeBranch;
		else
			_TBStack[_Stackpos]?.Insert(posToInsert, treeBranch ?? TreeBranch.DoNotAdd());
		return Default();
	}

	private bool EndWithEmpty(bool addError = false)
	{
		if (addError)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
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

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errorsList, int end = -1)
	{
		while (pos < (end == -1 ? lexems.Length : end))
		{
			if (CloseBracketIteration(ref pos, bracket, ref errorsList) is bool b)
				return b;
		}
		return false;
	}

	private bool? CloseBracketIteration(ref int pos, String bracket, ref List<String>? errorsList)
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
				CloseBracket(ref pos, s == "(" ? ")" : s == "[" ? "]" : "}", ref errorsList);
			}
			else if (new List<String> { ")", "]", "}" }.Contains(s) || bracket != "}" && (s == ";" || s == "\r\n"))
			{
				GenerateError(pos, "unpaired brackets; expected: " + bracket + "");
				return false;
			}
			else
				pos++;
		}
		else
			pos++;
		return null;
	}

	private bool ParseType(ref int pos, int end, BlockStack mainContainer, out UniversalType UnvType,
		ref List<String>? errorsList, bool inner = false, string collectionType = "",
		TypeConstraints constraints = TypeConstraints.None)
	{
		try
		{
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemKeyword("null"))
			{
				UnvType = NullType;
				pos++;
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			else if (lexems[pos].Type == LexemType.Identifier)
				return ParseIdentifierType(ref pos, end, mainContainer, out UnvType,
					ref errorsList, inner, collectionType, constraints);
			else if (IsCurrentLexemOther("("))
			{
				if (constraints == TypeConstraints.None)
					return ParseTupleType(ref pos, end, mainContainer, out UnvType, ref errorsList);
				else
				{
					UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
					GenerateError(pos, "expected: non-sealed class or interface");
					return false;
				}
			}
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: \"null\" or identifier or tuple");
			}
			return false;
		}
		catch (StackOverflowException)
		{
			if (inner)
				throw;
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "too many nested collections of types");
				return false;
			}
		}
	}

	private bool ParseIdentifierType(ref int pos, int end, BlockStack mainContainer, out UniversalType UnvType,
		ref List<String>? errorsList, bool inner = false, string collectionType = "",
		TypeConstraints constraints = TypeConstraints.None)
	{
		String s, namespace_ = [], outerClass = [];
		BlockStack container = new(), innerContainer, innerUserDefinedContainer;
	l0:
		s = lexems[pos].String;
		if (ParsePrimitiveType(ref pos, end, mainContainer, out UnvType,
			ref errorsList, collectionType, constraints, s) is bool b)
			return b;
		if (NamespacesList.Contains(namespace_ == "" ? s : namespace_ + "." + s) || UserDefinedNamespacesList.Contains(namespace_ == "" ? s : namespace_ + "." + s))
		{
			pos++;
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Namespace, s, 1));
				namespace_ = namespace_ == "" ? s : namespace_ + "." + s;
				pos++;
				goto l0;
			}
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "incorrect word or order of words in construction declaration");
			}
		}
		else if (container.Length == 0 && PrimitiveTypesList.ContainsKey(s))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			if (inner && s == "var")
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "keyword \"var\" is not a type and cannot be used inside the type");
			}
			else
			{
				pos++;
				UnvType = (new(container.ToList().Append(new(BlockType.Primitive, s, 1))), NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
		}
		else if (ExtraTypesList.TryGetValue((namespace_, s), out var netType) || namespace_ == ""
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, s), out netType)) >= 0)
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& (!netType.IsClass || netType.IsSealed))
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			if (constraints == TypeConstraints.NotAbstract && netType.IsAbstract)
			{
				UnvType = (new(container.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				GenerateError(pos, "cannot create instance of abstract type \"" + UnvType.ToString() + "\"");
			}
			var pos2 = pos;
			pos++;
			if (netType.GetGenericArguments().Length == 0)
			{
				UnvType = (new(container.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOther("["))
				pos++;
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: [");
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			GeneralArrayParameters template = [];
			for (var i = 0; i < netType.GetGenericArguments().Length; i++)
				template.Add((false, NoGeneralExtraTypes, new([new(BlockType.Primitive, "typename", 1)]), ""));
			ParseTypeChain(ref pos, end, mainContainer, template, out var innerArrayParameters, ref errorsList, "associativeArray");
			UnvType = (new(container.ToList().Append(new(BlockType.Class, s, 1))), innerArrayParameters);
			return EndParseType2(ref pos, end, ref UnvType, ref errorsList);
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
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static)
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateError(pos, "cannot create instance of static type \"" + UnvType.ToString() + "\"");
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not 0 or TypeAttributes.Sealed
				or TypeAttributes.Struct)
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateError(pos, "cannot create instance of abstract type \"" + UnvType.ToString() + "\"");
			}
			pos++;
			if (ArrayParameters.Length == 0)
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOther("["))
				pos++;
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: [");
				return false;
			}
			ParseTypeChain(ref pos, end, mainContainer, ArrayParameters, out var innerArrayParameters, ref errorsList, "associativeArray");
			UnvType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))), innerArrayParameters);
			return EndParseType2(ref pos, end, ref UnvType, ref errorsList);
		}
		else if (UserDefinedTypesList.TryGetValue((innerUserDefinedContainer = container, s), out var value2)
			|| container.Length == 0 && CheckContainer(mainContainer, stack =>
			UserDefinedTypesList.TryGetValue((stack, s), out value2), out innerUserDefinedContainer))
		{
			var pos2 = pos;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(value2.Attributes) && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static
				&& !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateError(pos, "cannot create instance of static type \"" + UnvType.ToString() + "\"");
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not 0 or TypeAttributes.Sealed
				or TypeAttributes.Struct && !(pos + 1 < end && IsLexemOperator(lexems[pos + 1], ".")))
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))),
					NoGeneralExtraTypes);
				GenerateError(pos, "cannot create instance of abstract type \"" + UnvType.ToString() + "\"");
			}
			pos++;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				container.Push(new(BlockType.Class, s, 1));
				outerClass = outerClass == "" ? s : outerClass + "." + s;
				pos++;
				goto l0;
			}
			else
			{
				UnvType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))), NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
		}
		else if (ExtraTypeExists(container, s) || container.Length == 0
			&& CheckContainer(mainContainer, stack => ExtraTypeExists(stack, s), out innerContainer))
		{
			var pos2 = pos;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			pos++;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "incorrect construction after type name");
			}
			else
			{
				UnvType = (new(innerContainer.ToList().Append(new(BlockType.Extra, s, 1))), NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
		}
		else if (IsNotImplementedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s])) || IsNotImplementedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "identifier \"" + s + "\" is still not implemented, wait for next versions");
		}
		else if (IsNotImplementedEndOfIdentifier(s, out var s2))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "end of identifier \"" + s2 + "\" is still not implemented, wait for next versions");
		}
		else if (IsOutdatedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]), out var useInstead))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "namespace \"" + s + "\" is outdated, consider using " + useInstead + " instead");
		}
		else if (IsOutdatedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s, out useInstead))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "type \"" + s + "\" is outdated, consider using " + useInstead + " instead");
		}
		else if (IsOutdatedEndOfIdentifier(s, out useInstead, out s2))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "end of identifier \"" + s2 + "\" is outdated, consider using " + useInstead + " instead");
		}
		else if (IsReservedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s])) || IsReservedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "identifier \"" + s + "\" is reserved for next versions of C#.NStar and cannot be used");
		}
		else if (IsReservedEndOfIdentifier(s, out s2))
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "end of identifier \"" + s2 + "\" is reserved for next versions of C#.NStar and cannot be used");
		}
		else
		{
			if (container.Length == 0)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "type \"" + String.Join(".", [.. container.ToList().Convert(X => X.Name), s]) + "\" is not defined in this location");
			}
			else
			{
				pos--;
				UnvType = (container, NoGeneralExtraTypes);
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
		}
		return false;
	}

	private bool? ParsePrimitiveType(ref int pos, int end, BlockStack mainContainer, out UniversalType UnvType,
		ref List<String>? errorsList, string collectionType, TypeConstraints constraints, String s)
	{
		if (s.ToString() is "short" or "long")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			pos++;
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && (lexems[pos].String == "char" || lexems[pos].String == "int"
				/*|| s == "long" && (lexems[pos].input == "long" || lexems[pos].input == "real")*/))
			{
				UnvType = (new([new(BlockType.Primitive, s + " " + lexems[pos].String, 1)]), NoGeneralExtraTypes);
				pos++;
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "incorrect type with \"short\" or \"long\" word");
				return false;
			}
		}
		else if (s == "unsigned")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			String mediumWord = [];
			pos++;
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && (lexems[pos].String == "short" || lexems[pos].String == "long"))
			{
				mediumWord = lexems[pos].String + " ";
				pos++;
			}
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "int"/* || lexems[pos].input == "long"*/)
			{
				UnvType = (new([new(BlockType.Primitive, s + " " + mediumWord + lexems[pos].String, 1)]), NoGeneralExtraTypes);
				pos++;
				return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
			}
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "incorrect type with \"unsigned\" word");
				return false;
			}
		}
		else if (s == "list")
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: non-sealed class or interface");
				return false;
			}
			if (collectionType == "list")
				GenerateMessage("Warning", pos, "two \"list\" modifiers in a row; consider using multi-dimensional list instead");
			pos++;
			GeneralExtraTypes typeParts = [];
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOther("("))
				pos++;
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: (");
				goto list0;
			}
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Int)
			{
				typeParts.Add(((TypeOrValue)lexems[pos].String, NoGeneralExtraTypes));
				pos++;
			}
			else if (!IsCurrentLexemOther(")"))
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: int number or ); variables and complex expressions at this place are under development");
				goto list0;
			}
			if (pos >= end)
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOther(")"))
			{
				pos++;
				goto list1;
			}
			else
			{
				UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
				GenerateError(pos, "expected: )");
				goto list0;
			}
		list0:
			CloseBracket(ref pos, ")", ref errorsList!, end);
			ParseType(ref pos, end, mainContainer, out var InnerUnvType, ref errorsList, true, "list");
			typeParts.Add(InnerUnvType);
			UnvType = (new([new(BlockType.Primitive, "list", 1)]), typeParts);
			return false;
		list1:
			ParseType(ref pos, end, mainContainer, out var InnerUnvType2, ref errorsList, true, "list");
			typeParts.Add(InnerUnvType2);
			UnvType = (new([new(BlockType.Primitive, "list", 1)]), typeParts);
			return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
		}
		UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
		return null;
	}

	private bool ParseTupleType(ref int pos, int end, BlockStack mainContainer, out UniversalType UnvType, ref List<String>? errorsList)
	{
		pos++;
		if (ParseTypeChain(ref pos, end, mainContainer, [(true, NoGeneralExtraTypes, new([new(BlockType.Primitive, "typename", 1)]), "")], out var innerArrayParameters, ref errorsList, "tuple") == false)
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			return false;
		}
		if (pos >= end)
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return false;
		}
		else if (IsCurrentLexemOther(")"))
			pos++;
		else
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "expected: )");
			CloseBracket(ref pos, ")", ref errorsList, end);
			return false;
		}
		UnvType = (new([new(BlockType.Primitive, "tuple", 1)]), innerArrayParameters);
		return EndParseType1(ref pos, end, ref UnvType, ref errorsList);
	}

	private bool EndParseType1(ref int pos, int end, ref UniversalType UnvType, ref List<String>? errorsList)
	{
		if (pos < end && IsCurrentLexemOther("["))
			pos++;
		else
			return true;
		return EndParseType3(ref pos, end, ref UnvType, ref errorsList);
	}

	private bool EndParseType2(ref int pos, int end, ref UniversalType UnvType, ref List<String>? errorsList)
	{
		if (pos < end && IsCurrentLexemOther(","))
			pos++;
		else
			return EndParseType4(ref pos, end, ref UnvType, ref errorsList);
		return EndParseType3(ref pos, end, ref UnvType, ref errorsList);
	}

	private bool EndParseType3(ref int pos, int end, ref UniversalType UnvType, ref List<String>? errorsList)
	{
		if (pos >= end)
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return false;
		}
		else if (lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos].String.ToString(), out var number))
		{
			if (number == 0)
				GenerateMessage("Warning", pos, "count of identical types in tuple cannot be zero; it has been set to 1");
			else if (number >= 2)
			{
				var UnvType2 = UnvType;
				UnvType = new(TupleBlockStack, new(RedStarLinq.Fill(number, _ => (UniversalTypeOrValue)UnvType2)));
			}
			pos++;
		}
		else
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "expected: int number; variables and complex expressions at this place are under development");
			return false;
		}
		return EndParseType4(ref pos, end, ref UnvType, ref errorsList);
	}

	private bool EndParseType4(ref int pos, int end, ref UniversalType UnvType, ref List<String>? errorsList)
	{
		if (pos >= end)
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return false;
		}
		else if (IsCurrentLexemOther("]"))
		{
			pos++;
			return true;
		}
		else
		{
			UnvType = (EmptyBlockStack, NoGeneralExtraTypes);
			GenerateError(pos, "expected: ]");
			CloseBracket(ref pos, "]", ref errorsList, end);
			return false;
		}
	}

	private bool ParseTypeChain(ref int pos, int end, BlockStack mainContainer, GeneralArrayParameters template, out GeneralExtraTypes types, ref List<String>? errorsList, string collectionType = "")
	{
		List<TreeBranch> tempTrees = [];
		GeneralExtraTypes tempTypes = [];
		var tpos = 0;
		types = [];
		if (template.Length == 0)
		{
			types = NoGeneralExtraTypes;
			return false;
		}
		while (true)
		{
			if (ParseTypeChainIteration(ref pos, end, mainContainer, template, ref types, ref errorsList, collectionType, tempTrees, tempTypes, ref tpos) is bool b)
				return b;
		}
	}

	private bool? ParseTypeChainIteration(ref int pos, int end, BlockStack mainContainer, GeneralArrayParameters template, ref GeneralExtraTypes types, ref List<String>? errorsList, String collectionType, List<TreeBranch> tempTrees, GeneralExtraTypes tempTypes, ref int tpos)
	{
		if (TypeIsPrimitive(template[tpos].ArrayParameterType) && template[tpos].ArrayParameterType.Peek().Name == "typename")
		{
			if (!ParseType(ref pos, end, mainContainer, out var InnerUnvType, ref errorsList, true, collectionType.ToString()))
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			String itemName = [];
			if (pos >= end)
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Identifier)
			{
				itemName = lexems[pos].String;
				pos++;
			}
			if (collectionType == "tuple" && TypeEqualsToPrimitive(InnerUnvType, "tuple", false) && InnerUnvType.ExtraTypes.Length == 2 && InnerUnvType.ExtraTypes[1].MainType.IsValue && int.TryParse(InnerUnvType.ExtraTypes[1].MainType.Value.ToString(), out _) && InnerUnvType.ExtraTypes[1].ExtraTypes.Length == 0)
				tempTypes.AddRange(InnerUnvType.ExtraTypes.Values);
			else if (itemName != "")
			{
				if (!tempTypes.TryAdd(itemName, InnerUnvType))
					tempTypes.Add(InnerUnvType);
			}
			else
				tempTypes.Add(InnerUnvType);
		}
		else if (TypeIsPrimitive(template[tpos].ArrayParameterType) && template[tpos].ArrayParameterType.Peek().Name == "int")
		{
			var minus = false;
			if (pos >= end)
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (IsCurrentLexemOperator("-"))
			{
				minus = true;
				pos++;
			}
			if (pos >= end)
			{
				types = NoGeneralExtraTypes;
				GenerateUnexpectedEndOfTypeError(ref errorsList);
				return false;
			}
			else if (lexems[pos].Type == LexemType.Int)
				pos++;
			else
			{
				types = NoGeneralExtraTypes;
				GenerateError(pos, "expected: int number; variables and complex expressions at this place are under development");
				return false;
			}
			tempTrees.Add(new((minus ? "-" : "") + lexems[pos].String, pos, mainContainer));
			tempTypes.Add(((TypeOrValue)((minus ? "-" : "") + lexems[pos].String), NoGeneralExtraTypes));
		}
		if (pos >= end)
		{
			types = NoGeneralExtraTypes;
			GenerateUnexpectedEndOfTypeError(ref errorsList);
			return false;
		}
		else if (IsCurrentLexemOperator(","))
		{
			if (ParseCommaTypeChain(ref pos, template, ref types, tempTypes, ref tpos) is bool b)
				return b;
		}
		else
		{
			if (tpos >= template.Length - 1 || tpos >= template.Length - 2 && template[tpos + 1].ArrayParameterPackage)
			{
				types = tempTypes;
				return true;
			}
			else
			{
				types = NoGeneralExtraTypes;
				GenerateError(pos, "expected: comma; chain of indexes is not ended");
				return false;
			}
		}
		return null;
	}

	private static bool? ParseCommaTypeChain(ref int pos, GeneralArrayParameters template, ref GeneralExtraTypes types, GeneralExtraTypes tempTypes, ref int tpos)
	{
		if (template[tpos].ArrayParameterPackage == false)
		{
			tpos++;
			if (tpos >= template.Length)
			{
				types = tempTypes;
				return true;
			}
		}
		pos++;
		return null;
	}

	public bool ParseTreeBranch(BlockStack container, out TreeBranch? value)
	{
		value = null;
		end = lexems.Length;
		var startingBracket = ValidateOtherLexem("(");
		var info = lexems[pos].String;
		pos++;
		if (ValidateOperatorLexem("@"))
		{
			if (!ParseContainer(container, out var newContainer))
				return false;
			container = newContainer;
		}
		if (!startingBracket)
		{
			if (!ValidateOperatorLexem("#"))
				return false;
			if (!(pos < end && lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos].String.ToString(), out var Pos)))
				return false;
			pos++;
			value = new(info, Pos, new());
			return true;
		}
		if (!ValidateOperatorLexem(":"))
			return false;
		if (!ValidateOperatorLexem(":"))
		{
			List<TreeBranch> elements = [];
			do
			{
				if (!(ParseTreeBranch(container, out var branch) && branch != null))
					return false;
				elements.Add(branch);
			} while (ValidateOperatorLexem(","));
		}
		if (!ValidateOperatorLexem(":"))
			return false;
		object? extra;
		if (ParseType(ref pos, end, container, out var UnvType, ref errorsList))
			extra = UnvType;
		else
			return false;
		{
			if (!ValidateOtherLexem(")"))
				return false;
			if (!ValidateOperatorLexem("#"))
				return false;
			if (!(pos < end && lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos].String.ToString(), out var Pos)))
				return false;
			pos++;
			value = new(info, Pos, new()) { Extra = extra };
		}
		return ValidateOtherLexem(")");
	}

	public bool ParseContainer(BlockStack parentContainer, out BlockStack container, bool recursion = false)
	{
		if (!(ParseBlock(parentContainer, out var block, recursion) && block != null))
		{
			container = parentContainer;
			return true;
		}
		container = new(parentContainer);
		container.Push(block);
		while (ValidateOperatorLexem("."))
		{
			if (!(ParseBlock(parentContainer, out block, recursion) && block != null))
				return false;
			container.Push(block);
		}
		return true;
	}

	public bool ParseBlock(BlockStack parentContainer, out Block? block, bool recursion = false)
	{
		if (lexems[pos].Type != LexemType.Identifier)
		{
			block = null;
			return false;
		}
		else if (CreateVar(lexems[pos].String, out var @string) == "Unnamed")
		{
			pos++;
			if (ValidateOtherLexem("(") && ValidateOperatorLexem("#") && lexems[pos].Type == LexemType.Int && int.TryParse(lexems[pos++].String.ToString(), out var n) && ValidateOtherLexem(")"))
			{
				block = new(BlockType.Unnamed, "#" + n.ToString(), 1);
				return true;
			}
			else
			{
				block = null;
				return false;
			}
		}
		else if ((parentContainer.Length == 0 || recursion) && PrimitiveTypesList.ContainsKey(@string))
		{
			block = new(BlockType.Primitive, @string, 1);
			return true;
		}
		else if (ExtraTypeExists(parentContainer, @string))
		{
			block = new(BlockType.Extra, @string, 1);
			return true;
		}
		else if (NamespacesList.Contains(parentContainer.ToString() + "." + @string))
		{
			block = new(BlockType.Namespace, @string, 1);
			return true;
		}
		else if (UserDefinedTypesList.ContainsKey((parentContainer, @string)) || (parentContainer.Length == 0 || recursion) && CheckContainer(parentContainer, stack => UserDefinedTypesList.ContainsKey((stack, @string)), out _))
		{
			block = new(BlockType.Class, @string, 1);
			return true;
		}
		else if (PublicFunctionExists(@string, out _) || MethodExists((parentContainer, NoGeneralExtraTypes), @string, out _) || (parentContainer.Length == 0 || recursion) && UserDefinedFunctionExists(parentContainer, @string, out _))
		{
			block = new(BlockType.Function, @string, 1);
			return true;
		}
		else
		{
			block = null;
			return false;
		}
	}

	private void GenerateError(Index pos, String text, bool savePrevious = false) => GenerateMessage("Error", pos, text, savePrevious);

	private void GenerateMessage(String typeName, Index pos, String text, bool savePrevious = false)
	{
		if (savePrevious)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		_ErLStack[_Stackpos]?.Add(typeName + " in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString() + ": " + text);
		if (typeName == "Wreck")
			wreckOccurred = true;
	}

	private void GenerateUnexpectedEndError(bool savePrevious = false)
	{
		if (savePrevious)
			_ErLStack[_Stackpos]?.AddRange(errorsList ?? []);
		_ErLStack[_Stackpos]?.Add("Error in line " + lexems[pos - 1].LineN.ToString() + " at position " + (lexems[pos - 1].Pos + lexems[pos - 1].String.Length).ToString() + ": unexpected end of code reached");
	}

	private void GenerateUnexpectedEndOfTypeError(ref List<String>? errorsList) => errorsList?.Add("Error in line " + lexems[pos - 1].LineN.ToString() + " at position " + (lexems[pos - 1].Pos + lexems[pos - 1].String.Length).ToString() + ": unexpected end of type reached");

	[GeneratedRegex("^[A-Za-zА-Яа-я_][0-9A-Za-zА-Яа-я_]*$")]
	private static partial Regex WordRegex();
}
