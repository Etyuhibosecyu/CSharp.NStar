using System.IO;
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
	private readonly List<int> _PosStack = new(16, 0);
	private readonly List<int> _StartStack = new(16, 0);
	private readonly List<int> _EndStack = new(16, -1);
	private readonly List<String> _TaskStack = new(16, nameof(Main));
	private readonly List<List<String>> _ErLStack = new(16, [], []);
	private readonly List<TreeBranch?> _TBStack = new(16, null, null);
	private readonly List<object?> _ExtraStack = new(16) { null };
	private readonly List<BlockStack> _ContainerStack = new(16) { new() };
	private readonly BitList _SuccessStack = new(16) { false, false };
	private readonly List<int> _BTJPStack = new(16, 0), _RTPStack = new(16, 0), _PLPStack = new(16, 0);
	private int typeDepth;
	private readonly List<String> collectionTypes = new(16) { "" };
	private readonly List<ExtendedRestrictions> typeChainTemplate = new(16);
	private readonly List<int> tpos = new(16);
	private int globalUnnamedIndex = 1;
	private int _Stackpos;

	private static readonly Dictionary<String, (String Next, String TreeLabel, List<String> Operators)> operatorsMapping = new()
	{
		{ "Members", ("Member", "Members", []) }, { "Expr", ("List", "Expr", []) }, { "List", ("LambdaExpr", "List", []) },
		{ "LambdaExpr", ("AssignedExpr", "Expr", []) }, { "Switch", ("IsXorExpr", "Expr", []) },
		{ "AssignedExpr", ("QuestionExpr", "Expr", []) }, { "QuestionExpr", ("XorExpr", "Expr", []) },
		{ "XorExpr", ("OrExpr", "Expr", ["^^"]) }, { "OrExpr", ("AndExpr", "Expr", ["||"]) },
		{ "AndExpr", ("EquatedExpr", "Expr", ["&&"]) }, { "EquatedExpr", ("ComparedExpr", "Expr", []) },
		{ "ComparedExpr", ("BitwiseXorExpr", "Expr", [">", "<", ">=", "<="]) },
		{ "IsXorExpr", ("IsOrExpr", "Pattern", ["xor"]) }, { "IsOrExpr", ("IsAndExpr", "Pattern", ["or"]) },
		{ "IsAndExpr", (nameof(IsUnaryExpr), "Pattern", ["and"]) },
		{ "BitwiseXorExpr", ("BitwiseOrExpr", "Expr", ["^"]) }, { "BitwiseOrExpr", ("BitwiseAndExpr", "Expr", ["|"]) },
		{ "BitwiseAndExpr", ("BitwiseShiftExpr", "Expr", ["&"]) },
		{ "BitwiseShiftExpr", ("PMExpr", "Expr", [">>>", ">>", "<<"]) }, { "PMExpr", ("MulDivExpr", "Expr", ["+", "-"]) },
		{ "MulDivExpr", ("PowExpr", "Expr", ["*", "/", "%"]) }, { "PowExpr", ("TetraExpr", "Expr", ["pow"]) },
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
	private static readonly ImmutableArray<string> BasicExprKeywords = ["true", "false", "this", "null"];
	private static readonly ImmutableArray<string> BasicExprKeywordsAndOperators = ["_",
		"true", "false", "this", "base", "null", "Infty", "Uncty", "Pi", "E", "List"];
	private static readonly ImmutableArray<string> BasicExprOperators = ["Infty", "-Infty", "Uncty", "Pi", "E"];
	private static readonly ImmutableArray<string> MultiCharUnaryOperators = ["++", "--", "^",
		"sin", "cos", "tan", "asin", "acos", "atan"];

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
			(_ErLStack[0] ??= []).AddRange(errors ?? []);
			if (_SuccessStack[0])
				return (lexems, input, _TBStack[0] ?? new(nameof(Main), 0, []), _ErLStack[0], wreckOccurred);
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
			var errorMessage = GetWreckPosPrefix(0xF000, targetPos)
				+ ": compilation failed because of internal compiler error\r\n";
			(errors ??= []).Add(errorMessage + @" (see %TEMP%\CSharp.NStar.log for details)");
			File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
				+ @"\CSharp.NStar.log", [errorMessage, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? "null",
					"The underlying internal exception message was:", ex.InnerException?.Message ?? "null"]);
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
			"Members" or "Expr" or "List" or "LambdaExpr" or "Switch" or "AssignedExpr" or "QuestionExpr"
				or "XorExpr" or "OrExpr" or "AndExpr" or "IsXorExpr" or "IsOrExpr" or "IsAndExpr"
				or "EquatedExpr" or "ComparedExpr" or "BitwiseXorExpr" or "BitwiseOrExpr" or "BitwiseAndExpr"
				or "BitwiseShiftExpr" or "PMExpr" or "MulDivExpr" or "PowExpr" or "TetraExpr" or "RangeExpr" or "PrefixExpr" =>
				IncreaseStack(operatorsMapping[this.task].Next, currentTask: this.task + "2", applyCurrentTask: true,
				currentBranch: new(operatorsMapping[this.task].TreeLabel, pos, pos + 1, container), assignCurrentBranch: true),
			"Member" => IncreaseStack(nameof(Property), currentTask: "Member2", applyCurrentTask: true,
				currentExtra: new List<object>()),
			"XorExpr2" or "OrExpr2" or "AndExpr2" or "IsXorExpr2" or "IsOrExpr2" or "IsAndExpr2" or "ComparedExpr2"
				or "BitwiseXorExpr2" or "BitwiseOrExpr2" or "BitwiseAndExpr2" or "BitwiseShiftExpr2"
				or "PMExpr2" or "MulDivExpr2" =>
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
		nameof(MainClosing) => MainClosing,
		nameof(Namespace) => Namespace,
		nameof(NamespaceClosing) => NamespaceClosing,
		nameof(Class) => Class,
		nameof(Class2) => Class2,
		nameof(ClassClosing) => ClassClosing,
		nameof(Record) => Record,
		nameof(Record2) => Record2,
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
		nameof(ActionChain2) => ActionChain2,
		nameof(ActionChain3) => ActionChain3,
		nameof(Condition) => Condition,
		"Condition2" or "WhileRepeat2" or "For3" => Condition2_WhileRepeat2_For3,
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
		nameof(AssignedExpr3) => AssignedExpr3,
		nameof(DictionaryExpr) => DictionaryExpr,
		nameof(DictionaryExpr2) => DictionaryExpr2,
		nameof(DictionaryExpr3) => DictionaryExpr3,
		nameof(QuestionExpr2) => QuestionExpr2,
		nameof(QuestionExpr3) => QuestionExpr3,
		nameof(QuestionExpr4) => QuestionExpr4,
		nameof(EquatedExpr2) => EquatedExpr2,
		nameof(EquatedExpr3) => EquatedExpr3,
		"IsExprClosing" or "TetraExpr2" or "UnaryExpr4" or "PostfixExpr4" => PassExpr,
		nameof(IsUnaryExpr) => IsUnaryExpr,
		nameof(IsExpr) => IsExpr,
		nameof(IsExpr2) => IsExpr2,
		nameof(RangeExpr2) => RangeExpr2,
		nameof(RangeExpr3) => RangeExpr3,
		nameof(UnaryExpr) => UnaryExpr,
		nameof(UnaryExpr2) => UnaryExpr2,
		nameof(UnaryExpr3) => UnaryExpr3,
		nameof(PrefixExpr2) => PrefixExpr2,
		nameof(Hypername) or "HypernameNotCall" => Hypername,
		nameof(HypernameNew) => HypernameNew,
		nameof(HypernameConstType) or "HypernameNotCallConstType" => HypernameConstType,
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
		nameof(TypeClass) => TypeClass,
		nameof(BasicExpr) => BasicExpr,
		nameof(BasicExpr2) => BasicExpr2,
		nameof(BasicExpr3) => BasicExpr3,
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
		_ContainerStack.Add(container_.Value);
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
		{
			RemoveUnclosedTempTypes();
			return Default();
		}
		else if (IsCurrentLexemOther("}"))
			return Default();
		else if (CheckBlockToJump(nameof(Namespace)))
			return IncreaseStack(nameof(Namespace), currentTask: nameof(Main2), applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Class)))
			return IncreaseStack(nameof(Class), currentTask: nameof(Main2), applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Record)))
			return IncreaseStack(nameof(Record), currentTask: nameof(Main2), applyPos: true, applyCurrentTask: true);
		else if (CheckBlockToJump(nameof(Function)))
			return IncreaseStack(nameof(Function), currentTask: nameof(Main2), applyPos: true, applyCurrentTask: true);
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: nameof(MainClosing), pos_: pos + 1, applyPos: true,
				applyCurrentTask: true, container_: new(container.Append(new(BlockType.Unnamed,
				"#" + (container.Length == 0 ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++).ToString(), 1))));
		else
			return IncreaseStack(nameof(ActionChain), currentTask: nameof(Main2), applyPos: true, applyCurrentTask: true);
	}

	private bool Main2()
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
				_TBStack[_Stackpos] = new(nameof(Main), treeBranch.Name == nameof(Main)
					&& !(pos < end && IsCurrentLexemOther("}")) ? treeBranch.Elements : [treeBranch]);
			else if (_TBStack[_Stackpos]!.Name == nameof(Main)
				&& treeBranch.Name.AsSpan() is nameof(Class) or nameof(BlockType.Struct)
				or nameof(Function) or nameof(Constructor) or "return")
				_TBStack[_Stackpos]?.Add(treeBranch);
			else if (treeBranch.Name == nameof(Main) && treeBranch.Length == 1
				&& treeBranch[0].Name.AsSpan() is "while" or "while!" && treeBranch[0].Length == 0)
				_TBStack[_Stackpos]?.Name = nameof(Main);
			else
			{
				_TBStack[_Stackpos]?.Name = nameof(Main);
				_TBStack[_Stackpos]?.AddRange(treeBranch.Elements);
			}
		}
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private bool MainClosing()
	{
		SkipSemicolonsAndNewLines();
		if (!IsCurrentLexemOther("}"))
			return EndWithError(0x2004, pos, false);
		RemoveUnclosedTempTypes();
		_PosStack[_Stackpos] = ++pos;
		_TaskStack[_Stackpos] = nameof(Main);
		TransformErrorMessage();
		if (_TBStack[_Stackpos] == null || _TBStack[_Stackpos]?.Length == 0 && (treeBranch == null
			|| treeBranch.Length <= 1 && treeBranch[0].Name == nameof(Main)))
			_TBStack[_Stackpos] = treeBranch;
		else
			AppendBranch(nameof(Main));
		if (_Stackpos != 0 && _TaskStack[_Stackpos - 1] == nameof(ActionChain2))
			return Default();
		_TBStack[_Stackpos + 1] = null;
		return true;
	}

	private void RemoveUnclosedTempTypes()
	{
		if (TempTypes.TryGetValue(container, out var containerTempTypes)
			&& Variables.TryGetValue(container, out var containerVariables))
		{
			containerTempTypes.BreakFilterInPlace(x => x.EndPos >= 0, out var wrongTempTypes);
			containerVariables.ExceptWith(wrongTempTypes.Convert(x => x.Name));
			Variables.ExceptWith(Variables.Filter(x => x.Value.Length < 0));
			containerTempTypes.FilterInPlace(x => x.EndPos >= 0);
			TempTypes.ExceptWith(TempTypes.Filter(x => x.Value.Length < 0));
		}
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
		if (!CheckBlockToJump(nameof(Class))
			|| !Enum.TryParse<BlockType>(blocksToJump[blocksToJumpPos].Type.ToString(), out var blockType))
			return _SuccessStack[_Stackpos] = false;
		_TBStack[_Stackpos] = new(blocksToJump[blocksToJumpPos].Type,
			new(blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container));
		pos = blocksToJump[blocksToJumpPos].End;
		if (CheckClassSubordination())
			return CheckColonAndAddTask(nameof(TypeConstraints.BaseClassOrInterface), nameof(Class2), blockType);
		else
			return CheckOpeningBracketAndAddTask(nameof(ClassMain), nameof(ClassClosing), blockType);
	}

	private bool Class2()
	{
		CheckSuccess();
		TransformErrorMessage2();
		pos = registeredTypes[registeredTypesPos].End;
		return CheckOpeningBracketAndAddTask(nameof(ClassMain), nameof(ClassClosing),
			Enum.Parse<BlockType>(blocksToJump[blocksToJumpPos].Type.ToString()), registeredTypes[registeredTypesPos++].Name);
		void CheckSuccess()
		{
			if (!(success && extra is NStarType NStarType && treeBranch != null))
			{
				_TBStack[_Stackpos]?.Add(new TreeBranch("type", registeredTypes[registeredTypesPos].Start,
					registeredTypes[registeredTypesPos].End, container)
				{
					Extra = NullType
				});
				return;
			}
			if (blocksToJump[blocksToJumpPos].Type == nameof(BlockType.Struct) && NStarType.MainType.TryPeek(out var block)
				&& block.BlockType != BlockType.Interface)
			{
				_TBStack[_Stackpos]?.Add(new TreeBranch("type", registeredTypes[registeredTypesPos].Start,
					registeredTypes[registeredTypesPos].End, container)
				{
					Extra = NullType
				});
				GenerateMessage(0x2036, treeBranch.Pos, true);
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

	private bool Record()
	{
		if (!CheckBlockToJump(nameof(Record)))
			return _SuccessStack[_Stackpos] = false;
		_TBStack[_Stackpos] = new(nameof(Record), new(blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container));
		pos = blocksToJump[blocksToJumpPos].End;
		if (CheckParameterList())
			return IncreaseStack("Parameters", currentTask: nameof(Record2), pos_: parameterLists[parameterListsPos].Start,
				applyCurrentTask: true, start_: parameterLists[parameterListsPos].Start,
				end_: parameterLists[parameterListsPos].End, currentExtra: new ExtendedMethodParameters(),
				plp: parameterListsPos + 1);
		else
		{
			_TaskStack[_Stackpos] = nameof(Record2);
			_ExtraStack[_Stackpos] = new ExtendedMethodParameters();
			return true;
		}
	}

	private bool Record2()
	{
		if (!CheckBlockToJump2(nameof(Record)))
			return _SuccessStack[_Stackpos] = false;
		CheckSuccess();
		pos = blocksToJump[blocksToJumpPos++].End;
		if (!IsCurrentLexemTerminator())
			return _SuccessStack[_Stackpos] = false;
		return EndWithAdding(true);
		void CheckSuccess()
		{
			if (!success || extra is not ExtendedMethodParameters parameters)
				parameters = [];
			var blockToJumpContainer = blocksToJump[blocksToJumpPos].Container;
			var name = blocksToJump[blocksToJumpPos].Name;
			var start = blocksToJump[blocksToJumpPos].Start;
			var t = UserDefinedTypes[(blockToJumpContainer, name)];
			if (parameters.Length == 0)
				_TBStack[_Stackpos]?.Add(new("no parameters", blocksToJump[blocksToJumpPos].End - 1,
					blocksToJump[blocksToJumpPos].End, container));
			t.Decomposition = [.. parameters.Convert(x => new G.KeyValuePair<String, TreeBranch>(x.Name,
				new("type", blocksToJump[blocksToJumpPos].Start, blockToJumpContainer) { Extra = x.Type }))];
			UserDefinedTypes[(blockToJumpContainer, name)] = t;
			BlockStack propertyContainer = new(blockToJumpContainer.Append(new(BlockType.Struct, name, 1)));
			if (!UserDefinedFunctions.TryGetValue(propertyContainer, out var containerFunctions))
				UserDefinedFunctions.Add(propertyContainer, containerFunctions = []);
			containerFunctions.Add(nameof(Equals), [new(nameof(Equals), [], BoolType, FunctionAttributes.None,
				[new(ObjectType, "obj", ParameterAttributes.None, "")])]);
			containerFunctions.Add(nameof(GetHashCode), [new(nameof(GetHashCode), [], IntType, FunctionAttributes.None, [])]);
			if (!UserDefinedProperties.TryGetValue(propertyContainer, out var containerProperties))
				UserDefinedProperties.Add(propertyContainer, containerProperties = []);
			containerProperties.UnionWith(parameters.Convert(x => new G.KeyValuePair<String, UserDefinedProperty>(x.Name,
				new(x.Type, PropertyAttributes.NoSet | PropertyAttributes.Required, ""))));
			if (!UserDefinedConstructors.TryGetValue(propertyContainer, out var containerConstructors))
				UserDefinedConstructors.Add(propertyContainer, containerConstructors = []);
			containerConstructors.Add((ConstructorAttributes.AutoGenerated, parameters, []));
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			return;
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
		if (CheckParameterList())
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

	private bool FunctionClosing()
	{
		if (!IsClosingFigureBracket())
			return EndWithError(0x2004, pos, false);
		var lineCount = lexems[pos - 1].LineN - lexems[_TBStack[_Stackpos]?.EndPos ?? pos - 1].LineN - 1;
		if (lineCount > CodeStyleRules.MaxLinesInFunction)
			GenerateMessage(0x8010, _TBStack[_Stackpos]?.Pos ?? 0, true, CodeStyleRules.MaxLinesInFunction, lineCount);
		return EndWithAdding(true);
	}

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
		&& (lexems.GetSlice(pos..blocksToJump[blocksToJumpPos].Start)
		.All(x => x.Type == LexemType.Keyword || x.String.AsSpan() is "partial" or "record")
		|| string_.AsSpan() is not (nameof(Class) or nameof(BlockType.Struct)))
		&& !lexems.GetRange(pos, blocksToJump[blocksToJumpPos].Start - pos).ToHashSet().Convert(X => (X.Type, X.String))
		.Intersect([(LexemType.Other, ";"), (LexemType.Other, "\r\n"), (LexemType.Other, "{"), (LexemType.Other, "}")]).Any()
		&& (blocksToJump[blocksToJumpPos].Type == string_
		|| string_ == nameof(Class) && blocksToJump[blocksToJumpPos].Type == nameof(BlockType.Struct));

	private bool CheckBlockToJump2(String string_) => CheckBlockToJump3() && blocksToJump[blocksToJumpPos].Type == string_;

	private bool CheckBlockToJump3() => blocksToJumpPos < blocksToJump.Length
		&& lexems[blocksToJump[blocksToJumpPos].Start].LineN == lexems[pos].LineN;

	private bool CheckClassSubordination() => registeredTypesPos < registeredTypes.Length
		&& lexems[registeredTypes[registeredTypesPos].Start].LineN == lexems[pos].LineN;

	private bool CheckParameterList() => parameterListsPos < parameterLists.Length
		&& parameterLists[parameterListsPos].Start >= blocksToJump[blocksToJumpPos].Start
		&& parameterLists[parameterListsPos].End <= blocksToJump[blocksToJumpPos].End;

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

	private bool CheckColonAndAddTask(String newTask, String closingTask, BlockType blockType)
	{
		if (IsCurrentLexemOperator(":"))
			return IncreaseStack(newTask, currentTask: closingTask, pos_: pos + 1, applyPos: true, applyCurrentTask: true,
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
		if (extra is NStarType NStarType)
			ValidateLocalName(NStarType);
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
				Variables.Add(container, containerVariables = []);
			containerVariables.Add((String)l[2], NStarType);
			parameterValues.Add((parameterLists[parameterListsPos - 1].Container, parameterLists[parameterListsPos - 1].Name,
				((ExtendedMethodParameters?)_ExtraStack[_Stackpos - 2] ?? []).Length + 1, (int)l[3], pos));
		}
		catch
		{
		}
		return Default();
	}

	private bool IsParameterModifier() => IsLexemKeyword(pos, ["ref", "out", "params"]);

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
			UserDefinedConstructors.Add(container, containerConstructors = []);
		var increment = 0;
		if (!containerConstructors.Exists(x => x.Parameters.Length == 0))
		{
			containerConstructors.Insert(0, (ConstructorAttributes.Multiconst | ConstructorAttributes.AutoGenerated, [], [-1]));
			increment++;
		}
		ExtendedMethodParameters parameters = [];
		if (UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType)
			&& CreateVar(GetAllProperties(userDefinedType.BaseType.MainType), out var baseProperties).Length != 0)
			foreach (var property in baseProperties)
			{
				if (property.Value.Attributes is not (PropertyAttributes.None or PropertyAttributes.Internal))
					continue;
				parameters.Add(new(property.Value.NStarType,
					property.Key, ParameterAttributes.Optional, "null"));
			}
		if (UserDefinedProperties.TryGetValue(container, out var properties)
			&& properties.Length != 0
			&& UserDefinedPropertiesOrder.TryGetValue(container, out var propertiesOrder)
			&& propertiesOrder.Length != 0)
		{
			foreach (var propertyName in propertiesOrder)
			{
				if (!properties.TryGetValue(propertyName, out var property))
					continue;
				parameters.Add(new(property.NStarType,
					propertyName, ParameterAttributes.Optional, "null"));
			}
		}
		if (!containerConstructors.Exists(x => x.Parameters.Length == parameters.Length
			&& x.Parameters.Combine(parameters).All(x => x.Item1.Type.Equals(x.Item2.Type))))
		{
			containerConstructors.Insert(increment++,
				(ConstructorAttributes.Multiconst | ConstructorAttributes.AutoGenerated, parameters, [-1]));
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
		if (IsLexemKeyword(pos, ["private", "protected", "internal", "public"]))
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
			_TBStack[_Stackpos]?.Name = "Constant";
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
		if (extra is NStarType NStarType)
		{
			CreateObjectList(out var l);
			if (NStarType.MainType.Equals(RecursiveBlockStack))
				ValidateTypeName();
			else if (((PropertyAttributes)l![0] & (PropertyAttributes.Private | PropertyAttributes.Protected)) == 0
				|| ((PropertyAttributes)l![0] & (PropertyAttributes.Const)) == PropertyAttributes.Const)
				ValidateOpenName();
			else
				ValidateLocalName(NStarType);
		}
		_ErLStack[_Stackpos].AddRange(errors ?? []);
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
				while (!IsCurrentLexemTerminator())
					pos++;
				return EndWithEmpty();
			}
			if (!IsCurrentLexemTerminator())
				return EndWithError(0x2002, pos, false);
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
				while (!IsCurrentLexemTerminator())
					pos++;
				return EndWithEmpty();
			}
			if (!CheckIdentifier("get"))
				return false;
			if (IsCurrentLexemOperator(","))
				pos++;
			else
			{
				AddPropertyAttribute2(PropertyAttributes.NoSet);
				if (ValidateLexemOrEndWithError("}"))
					return true;
				CloseBracket(ref pos, "}", ref errors, false);
				while (!IsCurrentLexemTerminator())
					pos++;
				return EndWithEmpty();
			}
			if (IsLexemKeyword(pos, ["private", "protected"]))
			{
				AddPropertyAttribute2(lexems[pos].String == "private" ? PropertyAttributes.PrivateSet
					: PropertyAttributes.ProtectedSet);
				pos++;
			}
			if (pos >= end || lexems[pos].Type != LexemType.Identifier || lexems[pos].String != "init")
				return CheckIdentifier("set") && ValidateLexemOrEndWithError("}");
			AddPropertyAttribute2(PropertyAttributes.SetOnce);
			pos++;
			if (ValidateLexemOrEndWithError("}"))
				return true;
			CloseBracket(ref pos, "}", ref errors, false);
			while (!IsCurrentLexemTerminator())
				pos++;
			return EndWithEmpty();
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
		if (!IsCurrentLexemTerminator())
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
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			if (_ExtraStack[_Stackpos - 1] is List<object> paramCollection2)
				paramCollection2.Add(lexems[pos].String);
			pos++;
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
				UserDefinedProperties.Add(container, containerProperties = []);
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
				if (!UserDefinedPropertiesMapping.TryGetValue(container, out var containerPropertiesMapping))
					UserDefinedPropertiesMapping.Add(container, containerPropertiesMapping = []);
				containerPropertiesMapping.Add(name, containerPropertiesMapping.Length);
			}
			if (!UserDefinedPropertiesOrder.TryGetValue(container, out var containerPropertiesOrder))
				UserDefinedPropertiesOrder.Add(container, containerPropertiesOrder = []);
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
				UserDefinedConstants.Add(container, containerConstants = []);
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
		if (IsLexemOther(pos, string_))
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
		else if (IsCurrentLexemOther("{"))
			return IncreaseStack(nameof(Main), currentTask: nameof(ActionChain2), applyCurrentTask: true);
		else if (IsCurrentLexemOther("}"))
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
			return IncreaseStack("Expr", currentTask: nameof(ActionChain3), applyCurrentTask: true);
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return EndWithEmpty();
		}
		else if (IsCurrentLexemKeyword("loop"))
		{
			_PosStack[_Stackpos] = ++pos;
			if (_TBStack[_Stackpos] == null)
				_TBStack[_Stackpos] = new(nameof(Main), pos - 1, pos, container);
			_TBStack[_Stackpos]?.Add(new("loop", pos - 1, pos, container));
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			_TaskStack[_Stackpos] = nameof(ActionChain);
			return true;
		}
		var newTask = lexems[pos].String.ToString() switch
		{
			"if" or "else" => nameof(Condition),
			"repeat" or "while" => nameof(WhileRepeat),
			"for" => nameof(For),
			"continue" or "break" => nameof(SpecialAction),
			"return" => nameof(Return),
			"null" when pos + 1 < end && IsLexemKeyword(pos + 1, [nameof(Function), "Operator", "Extent"]) =>
				nameof(Main),
			_ => "Expr",
		};
		return IncreaseStack(newTask, currentTask: newTask == "Expr"
			? nameof(ActionChain3) : nameof(ActionChain2), applyPos: true, applyCurrentTask: true);
	}

	private bool ActionChain2()
	{
		if (!success)
			return IncreaseStack("Expr", currentTask: nameof(ActionChain3), pos_: pos, applyPos: true, applyCurrentTask: true,
				currentExtra: new List<object>());
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (treeBranch == null || treeBranch.Length == 0 && treeBranch.Name.AsSpan() is not ("while" or "while!"))
			return EndActionChain();
		if (_TaskStack[_Stackpos - 1] == nameof(Main2))
		{
			if (_TBStack[_Stackpos] == null)
				_TBStack[_Stackpos] = treeBranch;
			else if (_TBStack[_Stackpos] == null || treeBranch.Name.AsSpan() is nameof(Main)
				&& (treeBranch.Length == 1 || treeBranch[0].Name.AsSpan() is "else if" or "else if!" or "else"))
				(_TBStack[_Stackpos] ??= new(nameof(Main), treeBranch.Pos, container))?.AddRange(treeBranch.Elements);
			else
				_TBStack[_Stackpos]?.Add(treeBranch);
		}
		else
		{
			if (treeBranch.Name == nameof(Main)
				&& (treeBranch.Length == 1 || treeBranch[0].Name.AsSpan() is "else if" or "else if!" or "else"))
				_TBStack[_Stackpos]?.AddRange(treeBranch.Elements);
			else
				_TBStack[_Stackpos]?.Add(treeBranch);
			if (pos < end && IsCurrentLexemKeyword("else") && _TBStack[_Stackpos]?[^2]?.Name != "else")
			{
				_TaskStack[_Stackpos] = nameof(Condition);
				return true;
			}
		}
		if (_Stackpos >= 2 && _TaskStack[_Stackpos] == nameof(ActionChain2)
			&& _TaskStack[_Stackpos - 1] == nameof(ActionChain2) && _TaskStack[_Stackpos - 2] == nameof(ActionChain2)
			&& _TaskStack[_Stackpos - 3] == nameof(Main2) && _TBStack[_Stackpos - 3] != null
			&& _TBStack[_Stackpos - 1]!.Length >= 1 && _TBStack[_Stackpos - 1]![^1].Name.AsSpan() is "repeat" or "for"
			&& _TBStack[_Stackpos]!.Length >= 1 && _TBStack[_Stackpos]![0].Name.AsSpan() is "if" or "if!"
			&& IsLexemKeyword(_TBStack[_Stackpos]![0].Pos, ["while", "while!"]))
		{
			_TBStack[_Stackpos]?.Add(new("else", pos - 1, container));
			_TBStack[_Stackpos]?.Add(new("break", pos - 1, container));
		}
		return Default();
	}

	private bool ActionChain3()
	{
		if (success && treeBranch != null && treeBranch.Length != 0 && !ValidateEndOrLexem(";", true))
		{
			_PosStack[_Stackpos] = pos;
			if (_TaskStack[_Stackpos - 1] == nameof(Main2))
				return ChangeTaskAndAppendBranch(nameof(Main));
			_TBStack[_Stackpos]?.Add(treeBranch);
			if (IsCurrentLexemKeyword("else"))
			{
				_TaskStack[_Stackpos] = nameof(Condition);
				return true;
			}
			return Default();
		}
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return Default();
		}
		else if (IsLexemKeyword(pos, ["switch", "case", "delete"]))
			GenerateMessage(0x201E, pos, false, lexems[pos].String);
		else if (!IsStopLexem(pos))
			GenerateMessage(0x2007, pos, false);
		return EndActionChain();
	}

	private bool EndActionChain()
	{
		pos--;
		if (pos >= 0 && IsLexemKeyword(pos, ["else", "loop"])
			|| pos >= 1 && IsLexemKeyword(pos - 1, ["continue", "break"]) && IsCurrentLexemTerminator())
		{
			_PosStack[_Stackpos] = pos;
			_TaskStack[_Stackpos] = nameof(ActionChain);
			AppendBranch(nameof(Main), treeBranch!);
			return true;
		}
		pos++;
		while (!IsStopLexem(pos))
		{
			pos++;
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
		}
		if (!IsLexemOther(pos, ["{", "}"]))
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
				_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
				_TBStack[_Stackpos]?.Add(new("if!", pos - 1, pos, container));
				pos++;
			}
			else
			{
				_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
				_TBStack[_Stackpos]?.Add(new("if", pos - 1, pos, container));
			}
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
					_TBStack[_Stackpos] ??= new(nameof(Main), pos - 2, container);
					_TBStack[_Stackpos]?.Add(new("else if!", pos - 2, pos, container));
					pos++;
				}
				else
				{
					_TBStack[_Stackpos] ??= new(nameof(Main), pos - 2, container);
					_TBStack[_Stackpos]?.Add(new("else if", pos - 2, pos, container));
				}
			}
			else
			{
				_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
				_TBStack[_Stackpos]?.Add(new("else", pos - 1, pos, container));
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				_PosStack[_Stackpos] = pos;
				_TaskStack[_Stackpos] = nameof(ActionChain);
				return true;
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
		if (!success || treeBranch == null)
		{
			GenerateMessage(0x200E, pos, true);
			return EndWithEmpty();
		}
		if (_Stackpos >= 2 && _TaskStack[_Stackpos - 1] == nameof(ActionChain2)
			&& _TaskStack[_Stackpos - 2] == nameof(Main2) && _TBStack[_Stackpos - 2] != null
			&& _TBStack[_Stackpos - 2]!.Length >= 2 && _TBStack[_Stackpos - 2]![^2].Name == "loop")
		{
			if (pos >= end || !IsCurrentLexemOther(")"))
			{
				GenerateMessage(0x200B, pos, true);
				return EndWithEmpty();
			}
			pos++;
			if (pos >= end || !IsCurrentLexemTerminator())
			{
				GenerateMessage(0x2002, pos, false);
				return EndWithEmpty();
			}
			_TBStack[_Stackpos - 2]![^2].Name = "loop-while";
			if (_TBStack[_Stackpos]!.Length != 0 && _TBStack[_Stackpos]![0].Name == "while!")
				_TBStack[_Stackpos - 2]![^2].Name.Add('!');
			_TBStack[_Stackpos - 2]![^2].Add(treeBranch);
			return Default();
		}
		if (pos >= end)
		{
			GenerateUnexpectedEndError(true);
			return EndWithEmpty();
		}
		else if (IsCurrentLexemOther(")"))
		{
			_PosStack[_Stackpos] = ++pos;
			_TBStack[_Stackpos]?[^1].Add(treeBranch);
		}
		else
		{
			GenerateMessage(0x200B, pos, true);
			return EndWithEmpty();
		}
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		if (pos < end && IsCurrentLexemOther(";") && lexems[pos].LineN == lexems[pos - 1].LineN)
			GenerateMessage(0x8001, pos, false);
		_TaskStack[_Stackpos] = nameof(ActionChain);
		return true;
	}

	private bool ValidateEndOrLexem(String string_, bool addQuotes = false)
	{
		if (pos >= end)
		{
			GenerateUnexpectedEndError();
			return true;
		}
		else if (IsLexemOther(pos, string_))
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

	private bool WhileRepeat()
	{
		if (IsLexemKeyword(pos, "while"))
		{
			pos++;
			var reduceToCondition = _Stackpos >= 3 && _TaskStack[_Stackpos - 1] == nameof(ActionChain2)
				&& _TaskStack[_Stackpos - 2] == nameof(ActionChain2)
				&& _TaskStack[_Stackpos - 3] == nameof(Main2) && _TBStack[_Stackpos - 3] != null
				&& _TBStack[_Stackpos - 3]!.Length != 0 && _TBStack[_Stackpos - 1]![^1].Name.AsSpan() is "for" or "repeat";
			if (pos >= end)
			{
				GenerateUnexpectedEndError();
				return EndWithEmpty();
			}
			else if (IsCurrentLexemOperator("!"))
			{
				pos++;
				_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
				_TBStack[_Stackpos]?.Add(new(reduceToCondition ? "if!" : "while!", pos - 2, pos, container));
			}
			else
			{
				_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
				_TBStack[_Stackpos]?.Add(new(reduceToCondition ? "if" : "while", pos - 1, pos, container));
			}
		}
		else if (IsLexemKeyword(pos, "repeat"))
		{
			pos++;
			_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
			_TBStack[_Stackpos]?.Add(new("repeat", pos - 1, pos, container));
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
		if (IsLexemKeyword(pos, ["for", "foreach"]))
		{
			pos++;
			_TBStack[_Stackpos] ??= new(nameof(Main), pos - 1, container);
			_TBStack[_Stackpos]?.Add(new("for", pos - 1, pos, container));
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
		if (!success || treeBranch == null || treeBranch.Name != "type" || treeBranch.Extra is not NStarType NStarType)
		{
			NStarType = GetPrimitiveType("var");
			treeBranch = new("type", pos, pos + 1, container) { Extra = NStarType };
		}
		if (pos >= end)
		{
			GenerateUnexpectedEndError(true);
			return EndWithEmpty();
		}
		else if (lexems[pos].Type == LexemType.Identifier)
		{
			ValidateLocalName(NStarType);
			_TBStack[_Stackpos]?[^1]?.Add(new("Declaration", [treeBranch, new(lexems[pos].String, pos, pos + 1, container)]));
			pos++;
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
		return IncreaseStack("LambdaExpr", currentTask: "For3", pos_: pos, applyPos: true, applyCurrentTask: true);
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
			else if (IsCurrentLexemTerminator())
				_TBStack[_Stackpos] = new(lexems[pos - 2].String, pos - 2, pos, container);
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
		else if (IsCurrentLexemTerminator())
		{
			GenerateMessage(0x8002, pos - 1, false);
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
		else if (IsCurrentLexemTerminator())
			return EndWithAdding(true);
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
		return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
	}

	private bool List2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator(","))
		{
			if (_TaskStack[_Stackpos - 1] == nameof(HypernameCall)
				|| _TaskStack[_Stackpos - 1] == nameof(Expr2)
				&& _TaskStack[_Stackpos - 2].AsSpan() is nameof(BasicExpr2) or nameof(TypeInt2)
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
				&& _TaskStack[_Stackpos - 1] == nameof(Expr2)
				&& _TaskStack[_Stackpos - 2].AsSpan() is nameof(BasicExpr2) or nameof(TypeInt2)
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
		return IncreaseStack("LambdaExpr", currentTask: "LambdaExpr3",
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
		treeBranch?.Name = "Lambda";
		if (treeBranch != null && treeBranch.Length == 1
			&& treeBranch[0].Name.AsSpan() is "Assignment" or "DeclarationAssignment")
		{
			var assignmentBranch = treeBranch[0];
			treeBranch.RemoveAt(0);
			assignmentBranch[0] = new("Lambda", [assignmentBranch[0], this.treeBranch!]);
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
		if (treeBranch != null && treeBranch.Length == 2
			&& treeBranch[0].Name.AsSpan() is "Assignment" or "DeclarationAssignment")
		{
			var assignmentBranch = treeBranch[0];
			var switchBranch = treeBranch[1];
			treeBranch.RemoveAt(0);
			assignmentBranch[0] = new("SwitchExpr", [assignmentBranch[0], switchBranch]);
			_TBStack[_Stackpos] = assignmentBranch;
			return Default();
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
			_TBStack[_Stackpos]?.Add(new("case", treeBranch));
			return IncreaseStack("AssignedExpr", currentTask: nameof(Switch5),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		if (pos >= end || !IsCurrentLexemOperator("=>"))
		{
			GenerateMessage(0x2008, pos, false, "\"if\" or =>");
			return SwitchFail();
		}
		pos++;
		_TBStack[_Stackpos]?.Add(new("case", treeBranch));
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
			return IncreaseStack("IsXorExpr", currentTask: nameof(Switch2),
				pos_: pos, applyPos: true, applyCurrentTask: true);
		}
		_TBStack[_Stackpos]![^1].Add(treeBranch);
		_TBStack[_Stackpos]?.Add(new("case", new("_", pos, container) { Extra = NullType }));
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
		if (pos >= end || !IsLexemOperator(pos, AssignmentOperators))
		{
			if (!IsCurrentLexemOther(")"))
				CloseTempTypes();
			return EndWithAddingOrAssigning(true, 0);
		}
		if (treeBranch == null || treeBranch.Name != "Declaration" && treeBranch.Name != nameof(Hypername))
		{
			GenerateMessage(0x201D, pos, false);
			_TBStack[_Stackpos]?.Insert(0, new TreeBranch("null", pos, pos + 1, container));
			while (!IsStopLexem())
				pos++;
			return Default();
		}
		_TBStack[_Stackpos]?.Insert(0, [treeBranch, new(lexems[pos].String, pos, pos + 1, container)]);
		_TBStack[_Stackpos]?.Name = treeBranch.Name == "Declaration" ? "DeclarationAssignment" : "Assignment";
		pos++;
		if (treeBranch.Name == "Declaration" && treeBranch.Length == 2
			&& treeBranch[0].Name == "type" && treeBranch[0].Extra is NStarType VarNStarType
			&& VarNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive && block.Name == "var"
			&& UserDefinedConstants.TryGetValue(treeBranch[1].Container, out var containerConstants)
			&& containerConstants.TryGetValue(treeBranch[1].Name, out var constant)
			&& constant.NStarType.Equals(VarNStarType) && VarNStarType.ExtraTypes.Length == 0)
		{
			if (!IsCurrentLexemKeyword("new"))
				return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
			if (pos + 1 >= end)
				return EndWithAddingOrAssigning(true, 0);
			else if (IsLexemOther(pos + 1, "[") || lexems[pos + 1].Type == LexemType.Identifier
				&& !(UserDefinedNamespaces.Contains(lexems[pos + 1].String)
				|| CheckContainer(container, stack => ExtraTypeExists(stack, lexems[pos + 1].String, out _), out _))
				&& (lexems[pos + 1].String == nameof(Dictionary<,>)
				|| pos + 5 < end && lexems[pos + 1].String == "System" && IsLexemOperator(pos + 2, ".")
				&& lexems[pos + 1].Type == LexemType.Identifier && lexems[pos + 3].String == "Collections"
				&& IsLexemOperator(pos + 4, ".")
				&& lexems[pos + 5].Type == LexemType.Identifier && lexems[pos + 5].String == nameof(Dictionary<,>)))
				return IncreaseStack(nameof(Hypername), pos_: pos, applyPos: true, applyCurrentErl: true);
			else
				return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		if (!(treeBranch.Name == "Declaration" && treeBranch.Length == 2
			&& treeBranch[0].Name == "type" && treeBranch[0].Extra is NStarType DictionaryNStarType
			&& DictionaryNStarType.MainType.Equals(DictionaryBlockStack)
			&& UserDefinedConstants.TryGetValue(treeBranch[1].Container, out containerConstants)
			&& containerConstants.TryGetValue(treeBranch[1].Name, out constant)
			&& constant.NStarType.Equals(DictionaryNStarType) && DictionaryNStarType.ExtraTypes.Length == 2
			&& DictionaryNStarType.ExtraTypes[1].Name == "type"
			&& DictionaryNStarType.ExtraTypes[1].Extra is NStarType ValueNStarType && pos < end))
			return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		if (IsCurrentLexemOther("("))
		{
			pos++;
			return IncreaseStack(nameof(DictionaryExpr), currentTask: nameof(AssignedExpr3),
				pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		if (!IsCurrentLexemKeyword("new"))
			return IncreaseStack("QuestionExpr", pos_: pos, applyPos: true, applyCurrentErl: true);
		if (pos + 1 < end && IsLexemOther(pos + 1, "("))
		{
			pos += 2;
			return IncreaseStack(nameof(DictionaryExpr), currentTask: nameof(AssignedExpr3),
				pos_: pos, applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		return IncreaseStack(nameof(Hypername), pos_: pos, applyPos: true, applyCurrentErl: true);
	}

	private bool AssignedExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end || !IsCurrentLexemOther(")"))
		{
			GenerateMessage(0x200B, pos, false);
			_TBStack[_Stackpos]?.Insert(0, new TreeBranch("null", pos, pos + 1, container));
			while (!IsStopLexem())
				pos++;
			return Default();
		}
		pos++;
		if (pos >= end || !IsCurrentLexemOther(")"))
			CloseTempTypes();
		return EndWithAddingOrAssigning(true, 0);
	}

	private void CloseTempTypes()
	{
		if (treeBranch == null)
			return;
		if (TempTypes.TryGetValue(container, out var containerTempTypes)
			&& Variables.TryGetValue(container, out var containerVariables))
		{
			var wrongTempTypes = containerTempTypes.ToList((elem, index) => (elem, index))
				.FilterInPlace(x => treeBranch.Pos >= x.elem.StartPos && x.elem.EndPos < 0);
			for (var i = 0; i < wrongTempTypes.Length; i++)
			{
				var x = wrongTempTypes[i].elem;
				x.EndPos = pos;
				containerTempTypes[wrongTempTypes[i].index] = x;
			}
		}
	}

	private bool DictionaryExpr()
	{
		_TBStack[_Stackpos] = new("List", pos, pos + 1, container);
		if (ValidateDictionaryVar())
			return true;
		return IncreaseStack("LambdaExpr", currentTask: nameof(DictionaryExpr2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool DictionaryExpr2()
	{
		if (!success || treeBranch == null)
			return _SuccessStack[_Stackpos] = false;
		if (treeBranch.Name == "Expr" && treeBranch.Length == 3 && treeBranch[2].Name == ":")
		{
			treeBranch.Name = "List";
			treeBranch.RemoveAt(2);
		}
		if (pos < end && IsCurrentLexemOther(")"))
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			if (_TBStack[_Stackpos] != null && (_TBStack[_Stackpos]!.Length == 0
				|| _TBStack[_Stackpos]![^1].Name == "List" && _TBStack[_Stackpos]![^1].Length == 2))
				_TBStack[_Stackpos]?.Add(treeBranch);
			else
				_TBStack[_Stackpos]?[^1] = new("List", [_TBStack[_Stackpos]![^1], treeBranch]);
			return Default();
		}
		if (pos < end && IsCurrentLexemOperator(":"))
		{
			_TBStack[_Stackpos]?.Add(treeBranch);
			pos++;
			return IncreaseStack("LambdaExpr", currentTask: nameof(DictionaryExpr3), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		if (pos >= end || !IsCurrentLexemOperator(","))
		{
			GenerateMessage(0x2008, pos, false, "\":\"");
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1),
				new TreeBranch("null", pos, pos + 1, container));
			while (!IsStopLexem())
				pos++;
			return Default();
		}
		_TBStack[_Stackpos]?.Add(treeBranch);
		pos++;
		if (ValidateDictionaryVar())
			return true;
		return IncreaseStack("LambdaExpr", currentTask: nameof(DictionaryExpr2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool DictionaryExpr3()
	{
		if (!success || treeBranch == null)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOther(")"))
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			if (_TBStack[_Stackpos] != null && (_TBStack[_Stackpos]!.Length == 0
				|| _TBStack[_Stackpos]![^1].Name == "List" && _TBStack[_Stackpos]![^1].Length == 2))
				_TBStack[_Stackpos]?.Add(treeBranch);
			else
				_TBStack[_Stackpos]?[^1] = new("List", [_TBStack[_Stackpos]![^1], treeBranch]);
			return Default();
		}
		if (pos >= end || !IsCurrentLexemOperator(","))
		{
			GenerateMessage(0x2008, pos, false, "\":\"");
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1),
				new TreeBranch("null", pos, pos + 1, container));
			while (!IsStopLexem())
				pos++;
			return Default();
		}
		if (_TBStack[_Stackpos] != null && (_TBStack[_Stackpos]!.Length == 0
			|| _TBStack[_Stackpos]![^1].Name == "List" && _TBStack[_Stackpos]![^1].Length == 2))
			_TBStack[_Stackpos]?.Add(treeBranch);
		else
			_TBStack[_Stackpos]?[^1] = new("List", [_TBStack[_Stackpos]![^1], treeBranch]);
		pos++;
		if (ValidateDictionaryVar())
			return true;
		return IncreaseStack("LambdaExpr", currentTask: nameof(DictionaryExpr2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
	}

	private bool ValidateDictionaryVar()
	{
		if (pos + 1 < end && lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "var"
			&& lexems[pos + 1].Type == LexemType.Identifier
			&& _TBStack[_Stackpos - 1] != null && (_TBStack[_Stackpos - 1]!.Name == "DeclarationAssignment"
			&& _TBStack[_Stackpos - 1]!.Length == 2 && _TBStack[_Stackpos - 1]![0].Name == "Declaration"
			&& _TBStack[_Stackpos - 1]![0].Length == 2 && _TBStack[_Stackpos - 1]![0][0].Name == "type"
			? _TBStack[_Stackpos - 1]![0][0] : _TBStack[_Stackpos - 1]!.Name == nameof(Hypername)
			&& _TBStack[_Stackpos - 1]!.Length == 2 && _TBStack[_Stackpos - 1]![0].Name == "new type"
			? _TBStack[_Stackpos - 1]![0] : new("", 0, [])).Extra is NStarType DictionaryNStarType
			&& DictionaryNStarType.ExtraTypes.Length == 2 && DictionaryNStarType.ExtraTypes[0].Name == "type"
			&& DictionaryNStarType.ExtraTypes[0].Extra is NStarType NStarType)
		{
			_TBStack[_Stackpos + 1] = new("Declaration",
				[new("type", pos, container) { Extra = NStarType }, new(lexems[pos + 1].String, pos + 1, container)]);
			_SuccessStack[_Stackpos + 1] = true;
			_PosStack[_Stackpos] = pos += 2;
			_TaskStack[_Stackpos] = nameof(DictionaryExpr2);
			if (!Variables.TryGetValue(container, out var containerVariables))
				Variables.Add(container, containerVariables = []);
			containerVariables[lexems[pos - 1].String] = NStarType;
			return true;
		}
		return false;
	}

	private bool PassExpr() =>
		success ? EndWithAssigning(true) : (_SuccessStack[_Stackpos] = false);

	private bool QuestionExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator(":") && _TBStack[_Stackpos]!.Length == 0)
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			return IncreaseStack("XorExpr", currentTask: nameof(QuestionExpr4), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		if (pos >= end || !IsLexemOperator(pos, TernaryOperators))
			return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
		_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
		pos++;
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

	private bool QuestionExpr4() => success ? EndWithAddingOrAssigning(true, 1) : (_SuccessStack[_Stackpos] = false);

	private bool EquatedExpr2()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && IsCurrentLexemOperator("is"))
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new("is", pos, pos + 1, container));
			pos++;
			if (pos < end && IsCurrentLexemKeyword("null"))
			{
				_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1),
					(TreeBranch)new("null", pos, container));
				pos++;
				return Default();
			}
			else
				return IncreaseStack("IsXorExpr", currentTask: "EquatedExpr3", applyCurrentTask: true,
					pos_: pos, applyPos: true, applyCurrentErl: true);
		}
		else if (pos < end && IsLexemOperator(pos, ["==", "!="]))
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
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

	private bool IsUnaryExpr()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Pattern", pos, container);
		if (lexems[pos].Type == LexemType.Identifier && lexems[pos].String == "not")
		{
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			isPrefix = true;
		}
		return IncreaseStack(nameof(IsExpr), currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool IsExpr()
	{
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		if (IsCurrentLexemKeyword("_"))
		{
			_TBStack[_Stackpos] = new("_", pos, container);
			pos++;
			return Default();
		}
		if (IsCurrentLexemKeyword("null"))
		{
			_TBStack[_Stackpos] = new("null", pos, container);
			pos++;
			return Default();
		}
		return IncreaseStack(nameof(Type), currentTask: nameof(IsExpr2), applyCurrentTask: true,
				currentBranch: new("Pattern", pos, pos + 1, container), assignCurrentBranch: true);
	}

	private bool IsExpr2()
	{
		if (success)
		{
			if (extra is not NStarType NStarType)
				return EndWithError(0x2001, pos, true);
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			if (lexems[pos].Type != LexemType.Identifier)
				return Default();
			ValidateLocalName(NStarType);
			AppendBranch("Declaration", new(lexems[pos].String, pos, pos + 1, container) { Extra = NStarType });
			pos++;
			return HypernameDeclaration();
		}
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		var isPrefix = false;
		if (IsLexemOperator(pos, [">", "<", ">=", "<="]))
		{
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			isPrefix = true;
		}
		return IncreaseStack("BitwiseXorExpr", currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4",
			applyCurrentTask: true, pos_: pos, applyPos: true);
	}

	private bool LeftAssociativeOperatorExpr2(String newTask, List<String> operators)
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos < end && lexems[pos].Type == LexemType.Operator && operators.Contains(lexems[pos].String))
		{
			_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch ?? TreeBranch.DoNotAdd());
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
		}
		else
		{
			if (treeBranch != null && treeBranch.Name.AsSpan() is "0" or "0i" or "0u" or "0L" or "0uL" or "\"0\""
				&& _TBStack[_Stackpos] != null && _TBStack[_Stackpos]!.Length != 0
				&& _TBStack[_Stackpos]![^1].Name.AsSpan() is "/" or "%")
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
			_TBStack[_Stackpos]?.Insert(0, [treeBranch ?? TreeBranch.DoNotAdd(),
				new(lexems[pos].String, pos, pos + 1, container)]);
			pos++;
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
		_TBStack[_Stackpos]?.Insert(Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1), treeBranch);
		_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
		pos++;
		return IncreaseStack(nameof(UnaryExpr), currentTask: nameof(RangeExpr3),
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool RangeExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (treeBranch == null)
		{
			treeBranch = new(nameof(Index), [new("1", pos, container), new("^", pos, container)]);
			errors = null;
		}
		_TBStack[_Stackpos]?.Name = nameof(Range);
		return EndWithAddingOrAssigning(true, Max(0, (_TBStack[_Stackpos]?.Length ?? 0) - 1));
	}

	private bool UnaryExpr()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator
			&& lexems[pos].String.Length == 1 && lexems[pos].String[0] is '+' or '-' or '!' or '~')
		{
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			isPrefix = true;
		}
		return IncreaseStack(nameof(UnaryExpr2), currentTask: isPrefix ? nameof(UnaryExpr3) : "UnaryExpr4",
			pos_: pos, applyPos: true, applyCurrentTask: true);
	}

	private bool UnaryExpr2()
	{
		var isPrefix = false;
		_TBStack[_Stackpos] = new("Expr", pos, container);
		if (lexems[pos].Type == LexemType.Operator && lexems[pos].String.AsSpan() is "ln" or "#" or "$")
		{
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			return IncreaseStack(nameof(UnaryExpr2), currentTask: nameof(UnaryExpr3), pos_: pos, applyPos: true,
				applyCurrentTask: true);
		}
		else if (lexems[pos].Type == LexemType.Operator && MultiCharUnaryOperators.Contains(lexems[pos].String.ToString()))
		{
			_TBStack[_Stackpos]?.Add(new(lexems[pos].String, pos, pos + 1, container));
			pos++;
			if (lexems[pos - 1].String == "^")
				_TBStack[_Stackpos]?.Name = "Index";
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
		if (pos < end && IsLexemOperator(pos, ["++", "--", "!", "!!"]))
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
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			return IncreaseStack(newTask, currentTask: currentTask, applyCurrentTask: true);
		}
		if (treeBranch != null && treeBranch.Name == nameof(Hypername) && treeBranch.Length == 1
			&& (!WordRegex().IsMatch(treeBranch[0].Name.ToString())
			|| BasicExprKeywordsAndOperators.Contains(treeBranch[0].Name.ToString())
			&& (treeBranch[0].Name != "List" || treeBranch[0].Length != 0 || IsCurrentLexemOperator("=>"))
			|| treeBranch[0].Length != 0))
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
		if (IsLexemKeyword(pos, ["ref", "out"]))
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
			return IncreaseStack(nameof(TypeConstraints.NotAbstract), currentTask: nameof(HypernameNew),
				applyCurrentTask: true, currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		else if (IsCurrentLexemKeyword("const"))
		{
			pos++;
			var newTask = task == "HypernameNotCall" ? "HypernameNotCallConstType" : nameof(HypernameConstType);
			if (pos + 1 < end && lexems[pos].Type == LexemType.Identifier && IsLexemOperator(pos + 1, "="))
			{
				_PosStack[_Stackpos] = pos;
				_TaskStack[_Stackpos] = newTask;
				_TBStack[_Stackpos] = new(nameof(Hypername), pos, container);
				_TBStack[_Stackpos + 1] = new("type", pos, container)
				{
					Extra = _ExtraStack[_Stackpos] = GetPrimitiveType("var")
				};
				_SuccessStack[_Stackpos + 1] = true;
				return true;
			}
			return IncreaseStack(nameof(Type), currentTask: newTask, applyCurrentTask: true,
				currentBranch: new(nameof(Hypername), pos, container), assignCurrentBranch: true);
		}
		if (pos >= 1 && IsLexemOperator(pos - 1, "."))
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
		if (!success || extra is not NStarType NStarType)
		{
			if (errors != null)
				_ErLStack[_Stackpos].AddRange(errors);
			return Default();
		}
		_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
		_TBStack[_Stackpos]![^1].Name = "new type";
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (!IsCurrentLexemOther("("))
			return EndWithError(0x200A, pos, true);
		pos++;
		_TBStack[_Stackpos]?.Add(new("ConstructorCall", pos - 1, pos, container));
		if (_TBStack[_Stackpos - 1] != null && _TBStack[_Stackpos - 1]!.Name == "DeclarationAssignment"
			&& _TBStack[_Stackpos - 1]!.Length == 2 && _TBStack[_Stackpos - 1]![1].Name == "="
			&& _TBStack[_Stackpos - 1]![0] is var targetBranch && targetBranch.Name == "Declaration" && targetBranch.Length == 2
			&& targetBranch[0].Name == "type" && targetBranch[0].Extra is NStarType VarNStarType
			&& (VarNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive && block.Name == "var"
			? extra : targetBranch[0].Extra) is NStarType DictionaryNStarType
			&& DictionaryNStarType.MainType.Equals(DictionaryBlockStack)
			&& UserDefinedConstants.TryGetValue(targetBranch[1].Container, out var containerConstants)
			&& containerConstants.TryGetValue(targetBranch[1].Name, out var constant)
			&& constant.NStarType.Equals(VarNStarType) && DictionaryNStarType.ExtraTypes.Length == 2
			&& DictionaryNStarType.ExtraTypes[1].Name == "type"
			&& DictionaryNStarType.ExtraTypes[1].Extra is NStarType ValueNStarType && pos < end)
		{
			constant.NStarType = DictionaryNStarType;
			UserDefinedConstants[targetBranch[1].Container][targetBranch[1].Name] = constant;
			return IncreaseStack(nameof(DictionaryExpr), currentTask: nameof(HypernameCall), pos_: pos, applyPos: true,
				applyCurrentTask: true, applyCurrentErl: success);
		}
		return IncreaseStack("List", currentTask: nameof(HypernameCall), pos_: pos, applyPos: true,
			applyCurrentTask: true, applyCurrentErl: success);
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
		ValidateOpenName();
		_ErLStack[_Stackpos].AddRange(errors ?? []);
		AppendBranch("Declaration", new(lexems[pos].String, pos, pos + 1, container) { Extra = NStarType });
		pos++;
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
			if (NStarType.Equals(WrongVarType))
			{
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				return _SuccessStack[_Stackpos] = false;
			}
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			if (pos >= end)
				return _SuccessStack[_Stackpos] = false;
			if (NStarType.MainType.Equals(RecursiveBlockStack) && NStarType.ExtraTypes.Length == 1
				&& TempTypes.TryGetValue(container, out var containerTempTypes)
				&& Variables.TryGetValue(container, out var containerVariables)
				&& containerTempTypes.Find(x => pos >= x.StartPos && x.EndPos < 0) is var tempType && tempType.Name != null)
			{
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				AppendBranch("Declaration", new(tempType.Name, pos, pos + 1, container) { Extra = NStarType });
				return HypernameDeclaration();
			}
			if (lexems[pos].Type == LexemType.Identifier)
			{
				_ErLStack[_Stackpos].AddRange(errors ?? []);
				ValidateLocalName(NStarType);
				AppendBranch("Declaration", new(lexems[pos].String, pos, pos + 1, container) { Extra = NStarType });
				pos++;
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
				_TBStack[_Stackpos]?[0] = UserDefinedConstantExists(container, NStarType.MainType.Peek().Name,
					out var constant, out _, out _) && constant.HasValue && constant.Value.DefaultValue != null
					? constant.Value.DefaultValue.Name == "Expr" && constant.Value.DefaultValue.Length == 1
					? new TreeBranch("Expr", constant.Value.DefaultValue, container) : constant.Value.DefaultValue
					: new(NStarType.MainType.Peek().Name, treeBranch?.Pos ?? -1, treeBranch?.Container ?? []);
			return HypernameBracketsAndDot();
		}
		errors?.Clear();
		var @ref = false;
		if (IsLexemKeyword(pos, ["ref", "out"]))
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
			var newBranch = UserDefinedConstantExists(container, lexems[pos].String, out var constant, out _, out _)
				&& constant.HasValue && constant.Value.DefaultValue != null
				? constant.Value.DefaultValue.Name == "Expr" && constant.Value.DefaultValue.Length == 1
				? new TreeBranch("Expr", constant.Value.DefaultValue, container) : constant.Value.DefaultValue
				: new(lexems[pos].String, pos, pos + 1, container);
			AppendBranch(nameof(Hypername), !@ref ? newBranch : new(lexems[pos - 1].String, newBranch));
			pos++;
			return HypernameBracketsAndDot();
		}
		if (_TaskStack[_Stackpos - 1].AsSpan() is not "HypernameClosing" and not "HypernameNotCallClosing")
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
		CloseTempTypes();
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
				UserDefinedConstants.Add(container, containerConstants = []);
			containerConstants[lexems[pos - 1].String] = new(NStarType, ConstantAttributes.None, null!);
			return Default();
		}
		else
		{
			if (!Variables.TryGetValue(container, out var containerVariables))
				Variables.Add(container, containerVariables = []);
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
		else if (CheckBlockToJump(nameof(Class)))
		{
			_TBStack[_Stackpos] = new(nameof(Class), new(blocksToJump[blocksToJumpPos].Name, pos, pos + 1, container));
			var oldPos = pos;
			pos = blocksToJump[blocksToJumpPos].End;
			var unnamedIndex = (container.Length == 0) ? globalUnnamedIndex : container.Peek().UnnamedIndex;
			if (!(UnnamedTypeStartIndexes.TryGetValue(container, out var containerStartIndexes)
				&& containerStartIndexes.Find(x => int.TryParse(x[1..].ToString(), out var otherUnnamedIndex)
				&& otherUnnamedIndex == unnamedIndex) is var startIndex && startIndex != null
				&& UserDefinedTypes.TryGetValue((container, startIndex), out var userDefinedType)))
				return _SuccessStack[_Stackpos] = false;
			else if (CheckClassSubordination())
			{
				blocksToJump[blocksToJumpPos].Name.Replace(startIndex);
				var savedContainer = container;
				SubscribeToChanges(startIndex, savedContainer);
				var result = CheckColonAndAddTask(nameof(TypeConstraints.BaseClassOrInterface),
					nameof(TypeClass), BlockType.Class);
				_BTJPStack[_Stackpos] = ++blocksToJumpPos;
				return result;
			}
			else
			{
				_BTJPStack[_Stackpos] = ++blocksToJumpPos;
				return TypeSingularTuple(new(new(new Block(BlockType.Other, "Class", 1)),
					new([new("Class", oldPos, pos, container) { Extra = userDefinedType }])));
			}
		}
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
		Stack<Block> container = [];
		bool result;
		while (IdentifierTypeIteration(constraints, container, ref namespace_, ref outerClass, out result)) ;
		return result;
	}

	private bool IdentifierTypeIteration(TypeConstraints constraints, Stack<Block> container,
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
		if (ExtraTypeExists(new(container), s, out var @class) || container.Length == 0
			&& CheckContainer(mainContainer, stack => ExtraTypeExists(stack, s, out @class), out innerContainer))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface && !@class)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			_PosStack[_Stackpos] = ++pos;
			if (pos < end && IsCurrentLexemOperator("."))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2025, pos, false);
			}
			else
			{
				if (@class)
					NStarType = (new(innerContainer.ToList().Append(new(BlockType.Class, s, 1))), NoBranches);
				else
					NStarType = (new(container.ToList().Append(new(BlockType.Extra, s, 1))), NoBranches);
				if (@class && !IsCurrentLexemOther("["))
				{
					GenerateMessage(0x2037, pos, false, NStarType.MainType);
					NStarType = NullType;
					_ExtraStack[_Stackpos - 1] = NStarType;
					_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				}
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if (Namespaces.Contains(namespace_ == "" ? s : namespace_ + "." + s)
			|| ImportedNamespaces.Contains(namespace_ == "" ? s : namespace_ + "." + s)
			|| UserDefinedNamespaces.Contains(namespace_ == "" ? s : namespace_ + "." + s))
		{
			_PosStack[_Stackpos] = ++pos;
			if (pos >= end)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos - 1, new(container)) { Extra = NStarType };
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
				_TBStack[_Stackpos] = new("type", pos - 1, new(container)) { Extra = NStarType };
				GenerateMessage(0x2020, pos, false);
			}
		}
		else if (container.Length == 0 && PrimitiveTypes.ContainsKey(s))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			if (typeDepth != 0 && s == "var")
			{
				NStarType = WrongVarType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2022, pos, false);
				_SuccessStack[_Stackpos] = true;
				outerResult = false;
				return false;
			}
			_PosStack[_Stackpos] = ++pos;
			NStarType = (new(container.ToList().Append(new(BlockType.Primitive, s, 1))), NoBranches);
			if (!(typeDepth != 0 && s == "typename" && pos < end && lexems[pos].Type == LexemType.Identifier))
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
			pos++;
			if (!TempTypes.TryGetValue(mainContainer, out var containerTempTypes))
				TempTypes.Add(mainContainer, containerTempTypes = []);
			if (!Variables.TryGetValue(mainContainer, out var containerVariables))
				Variables.Add(mainContainer, containerVariables = []);
			var name = lexems[pos - 1].String;
			containerVariables[name] = RecursiveType;
			if (!containerTempTypes.Any(x => x.Name == name && (x.StartPos == pos || x.EndPos == -1)))
				containerTempTypes.Add(new(name, TypeAttributes.None, NullType, _PosStack[_Stackpos] = pos, -1));
			if (pos >= end || !IsCurrentLexemOperator(":"))
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
			pos++;
			typeChainTemplate.Add([new(false, RecursiveType, "")]);
			collectionTypes.Add("associativeArray");
			outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
				currentExtra: (new Stack<Block>(innerContainer = NStarType.MainType), typeParts));
			return false;
		}
		else if ((ExtraTypes.TryGetValue((namespace_, s), out netType)
			|| ImportedTypes.TryGetValue((namespace_, s), out netType) || namespace_ == ""
			&& (ExplicitlyConnectedNamespaces.FindIndex(x => ExtraTypes.TryGetValue((x, s), out netType)) >= 0
			|| ExplicitlyConnectedNamespaces.FindIndex(x => ImportedTypes.TryGetValue((x, s), out netType)) >= 0))
			&& netType != null)
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& (!netType.IsClass && !netType.IsInterface || netType.IsSealed))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(container.ToList().Append(new(typeof(Delegate).IsAssignableFrom(netType) ? BlockType.Delegate
				: netType.IsInterface ? BlockType.Interface
				: netType.IsClass ? BlockType.Class : netType.IsValueType
				? BlockType.Struct : throw new InvalidOperationException(), s, 1))), NoBranches);
			_PosStack[_Stackpos] = ++pos;
			if (IsCurrentLexemOperator("."))
			{
				container.Push(new(NStarType.MainType.Peek().BlockType, s, 1));
				namespace_ = namespace_ == "" ? s : namespace_ + "." + s;
				_PosStack[_Stackpos] = ++pos;
				outerResult = false;
				return true;
			}
			if (constraints == TypeConstraints.NotAbstract && netType.IsAbstract)
			{
				GenerateMessage(0x2023, pos, false, NStarType.ToString());
			}
			if (netType.GetGenericArguments().Length == 0)
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if ((Interfaces.TryGetValue((namespace_, s), out var @interface) || namespace_ == ""
			&& ExplicitlyConnectedNamespaces.FindIndex(x => Interfaces.TryGetValue((x, s), out @interface)) >= 0)
			&& @interface.DotNetType != null)
		{
			netType = @interface.DotNetType;
			if (constraints is TypeConstraints.NotAbstract)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(container.ToList().Append(new(typeof(Delegate).IsAssignableFrom(netType)
				? BlockType.Delegate : netType.IsInterface
				? BlockType.Interface : netType.IsClass ? BlockType.Class : netType.IsValueType
				? BlockType.Struct : throw new InvalidOperationException(), s, 1))), NoBranches);
			_PosStack[_Stackpos] = ++pos;
			if (IsCurrentLexemOperator("."))
			{
				container.Push(new(NStarType.MainType.Peek().BlockType, s, 1));
				namespace_ = namespace_ == "" ? s : namespace_ + "." + s;
				_PosStack[_Stackpos] = ++pos;
				outerResult = false;
				return true;
			}
			if (netType.GetGenericArguments().Length == 0)
			{
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else if (ExtendedTypes.TryGetValue((innerContainer = new(container), s), out var value) || namespace_ == ""
			&& ExplicitlyConnectedNamespaces.FindIndex(x => ExtendedTypes.TryGetValue((innerContainer
			= new(x.Split('.').Convert(x => new Block(BlockType.Namespace, x, 1))), s), out value)) >= 0)
		{
			var (Restrictions, Attributes) = value;
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(Attributes))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(innerContainer.ToList().Append(new(s.AsSpan() is nameof(Action) or nameof(Func<>)
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
				_TBStack[_Stackpos] = new("type", pos - 1, new(container)) { Extra = NStarType };
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
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x200C, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			typeChainTemplate.Add(Restrictions);
			collectionTypes.Add("associativeArray");
			outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
				currentExtra: (new Stack<Block>(innerContainer = NStarType.MainType), typeParts));
			return false;
		}
		else if (UserDefinedTypes.TryGetValue((innerUserDefinedContainer = new(container), s), out var value2)
			|| container.Length == 0 && CheckContainer(mainContainer, stack =>
			UserDefinedTypes.TryGetValue((stack, s), out value2), out innerUserDefinedContainer))
		{
			if (constraints is TypeConstraints.BaseClassOrInterface or TypeConstraints.BaseInterface
				&& !IsValidBaseClass(value2.Attributes) && !(pos + 1 < end && IsLexemOperator(pos + 1, ".")))
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2015, pos, false);
				outerResult = _SuccessStack[_Stackpos] = false;
				return false;
			}
			NStarType = (new(innerUserDefinedContainer.ToList().Append(new(BlockType.Class, s, 1))), NoBranches);
			if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) == TypeAttributes.Static
				&& !(pos + 1 < end && IsLexemOperator(pos + 1, ".")))
			{
				GenerateMessage(0x2024, pos, false, NStarType);
			}
			else if (constraints == TypeConstraints.NotAbstract
				&& (value2.Attributes & (TypeAttributes.Struct | TypeAttributes.Static)) is not (0 or TypeAttributes.Sealed
				or TypeAttributes.Struct or TypeAttributes.Enum) && !(pos + 1 < end && IsLexemOperator(pos + 1, ".")))
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
					currentExtra: (new Stack<Block>(innerContainer = NStarType.MainType), typeParts));
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
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x202A, pos, false, s);
		}
		else if (IsNotImplementedEndOfIdentifier(s, out var s2))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x202B, pos, false, s2);
		}
		else if (IsOutdatedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]), out var useInstead)
			&& useInstead != null)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x202C, pos, false, s, useInstead);
		}
		else if (IsOutdatedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s, out useInstead)
			&& useInstead != null)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x202D, pos, false, s, useInstead);
		}
		else if (IsOutdatedEndOfIdentifier(s, out s2, out useInstead))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x202E, pos, false, s2, useInstead);
		}
		else if (IsReservedNamespace(String.Join(".", [.. container.ToList().Convert(X => X.Name), s]))
			|| IsReservedType(String.Join(".", [.. container.ToList().Convert(X => X.Name)]), s))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x203A, pos, false, s);
		}
		else if (IsReservedEndOfIdentifier(s, out s2))
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
			GenerateMessage(0x203B, pos, false, s2);
		}
		else if (!IsCurrentLexemOther("["))
		{
			if (container.Length == 0)
			{
				NStarType = NullType;
				_ExtraStack[_Stackpos - 1] = NStarType;
				_TBStack[_Stackpos] = new("type", pos, new(container)) { Extra = NStarType };
				GenerateMessage(0x2026, pos, false, String.Join(".", [.. container.ToList().Convert(X => X.Name), s]));
			}
			else
			{
				pos--;
				NStarType = new(new(container), NoBranches);
				outerResult = TypeSingularTuple(NStarType);
				return false;
			}
		}
		else
		{
			netType = typeof(Dictionary<,>);
			NStarType = (DictionaryBlockStack, NoBranches);
		}
		if (pos >= end)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, new(container)) { Extra = NStarType };
			GenerateUnexpectedEndOfTypeError(ref errors);
			outerResult = _SuccessStack[_Stackpos] = false;
			return false;
		}
		else if (IsCurrentLexemOther("["))
			_PosStack[_Stackpos] = ++pos;
		else
		{
			if (s != nameof(Dictionary<,>))
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
		var genericArguments = netType!.GetGenericArguments();
		for (var i = 0; i < genericArguments.Length; i++)
			template.Add(new(false, RecursiveType, genericArguments[i].Name));
		typeChainTemplate.Add(template);
		collectionTypes.Add("associativeArray");
		outerResult = IncreaseStack(nameof(TypeChain), currentTask: nameof(IdentifierType2), pos_: pos,
			applyPos: true, applyCurrentTask: true, applyCurrentErl: true,
			currentExtra: (container = new(NStarType.MainType), typeParts));
		return false;
	}

	private bool IdentifierType2()
	{
		typeChainTemplate.RemoveAt(^1);
		collectionTypes.RemoveAt(^1);
		if (!success || extra is not (Stack<Block> innerContainer, BranchCollection types))
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		TransformErrorMessage();
		NStarType NStarType = new(new(innerContainer), types);
		if (NStarType.ExtraTypes.Length == 1 && NStarType.MainType.Equals(RecursiveBlockStack) 
			&& _Stackpos >= 3 && _TaskStack[_Stackpos] == nameof(IdentifierType2)
			&& _TaskStack[_Stackpos - 3].AsSpan() is nameof(IdentifierType2) or nameof(TupleType2)
			&& TempTypes.TryGetValue(container, out var containerTempTypes)
			&& containerTempTypes.FindLast(x => pos >= x.StartPos && x.EndPos == -1) is var found && found.Name != null
			&& Variables.TryGetValue(container, out var containerVariables)
			&& containerVariables.TryGetValue(found.Name, out var VariableNStarType)
			&& VariableNStarType.Equals(RecursiveType))
		{
			if (NStarType.ExtraTypes.Length == 1 && NStarType.ExtraTypes[0].Name == "type"
				&& NStarType.ExtraTypes[0].Extra is NStarType WrongNStarType
				&& (UserDefinedTypes.TryGetValue(SplitType(WrongNStarType.MainType), out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Delegate) is TypeAttributes.Sealed or TypeAttributes.Static
				or TypeAttributes.Struct or TypeAttributes.Enum or TypeAttributes.Delegate
				|| WrongNStarType.Equals(ObjectType)
				|| TypeExists(SplitType(WrongNStarType.MainType), out var netType)
				&& !(netType.IsClass && !netType.IsSealed || netType.IsInterface)))
			{
				GenerateMessage(0x2038, NStarType.ExtraTypes[0].Pos, true, WrongNStarType);
				NStarType = RecursiveType;
			}
			else
				containerVariables[found.Name] = new(RecursiveBlockStack, NStarType.ExtraTypes);
		}
		return TypeClosing(NStarType);
	}

	private bool? PrimitiveType(TypeConstraints constraints, String s)
	{
		NStarType NStarType;
		if (s.AsSpan() is "short" or "long")
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
				|| s == "long" && (lexems[pos].String == "long" || lexems[pos].String == "long"/*"real"*/)))
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
			if (collectionTypes[^1] == "list")
				GenerateMessage(0x8003, pos, false);
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
		else if (lexems[pos].Type is LexemType.UnsignedInt or LexemType.LongInt or LexemType.UnsignedLongInt
			or LexemType.LongLong or LexemType.Real or LexemType.Complex or LexemType.String)
		{
			NStarType = NullType;
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos, container) { Extra = NStarType };
			GenerateMessage(0x2017, pos, false);
			if (s != "list")
				return _SuccessStack[_Stackpos] = false;
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
		var targetBranch = treeBranch.Name.AsSpan() is "Expr" or nameof(Hypername) && treeBranch.Length == 1
			|| treeBranch.Name == "Declaration" && treeBranch.Length == 2 && treeBranch[0].Name == "type"
			&& treeBranch[0].Extra is NStarType RecursiveNStarType && RecursiveNStarType.MainType.Equals(RecursiveBlockStack)
			&& RecursiveNStarType.ExtraTypes.Length == 1 ? treeBranch[0] : treeBranch;
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
			if (treeBranch.Length != 0 && targetBranch.Extra is NStarType ReturnNStarType)
				return TypeSingularTuple(ReturnNStarType);
			typeDepth++;
			collectionTypes.Add("list");
			return IncreaseStack(nameof(Type), currentTask: nameof(TypeList2), pos_: pos,
				applyPos: true, applyCurrentTask: true, applyCurrentErl: true);
		}
		else
		{
			if (IsLexemOther(start, "("))
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
		if (InnerNStarType.Equals(WrongVarType))
		{
			_ErLStack[_Stackpos].AddRange(errors ?? []);
			_ExtraStack[_Stackpos - 1] = WrongVarType;
			_SuccessStack[_Stackpos] = true;
			return false;
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
			&& _ExtraStack[_Stackpos - 1] is not (Stack<Block>, BranchCollection)
			|| extra is not BranchCollection typesBuffer)
		{
			GenerateUnexpectedEndOfTypeError(ref errors);
			return _SuccessStack[_Stackpos] = false;
		}
		TransformErrorMessage();
		if (_ExtraStack[_Stackpos - 1] is (Stack<Block>, BranchCollection types))
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
			TransformErrorMessage();
			ReduceStack();
			if (treeBranch == null)
				return _SuccessStack[_Stackpos] = false;
			if (treeBranch.Name != "List")
				treeBranch.Extra = RecursiveType;
			else if (!treeBranch.Elements.ToList(x =>
			{
				if (x.Name != nameof(Hypername) || x.Length != 1)
					return false;
				if (PrimitiveTypes.ContainsKey(x[0].Name))
				{
					var NStarType = new NStarType(new(new Block(BlockType.Primitive, x[0].Name, 1)), NoBranches);
					x[0].Replace(new("type", x[0].Pos, x[0].Container) { Extra = NStarType });
					return true;
				}
				else if (CheckContainer(container, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
					&& containerTempTypes.Any(y => y.Name == x[0].Name), out _))
				{
					var NStarType = new NStarType(new(new Block(BlockType.Extra, x[0].Name, 1)), NoBranches);
					x[0].Replace(new("type", x[0].Pos, x[0].Container) { Extra = NStarType });
					return true;
				}
				return false;
			}).Any(x => x))
				treeBranch.Extra = GetListType(RecursiveType);
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
				NStarType = new(TupleBlockStack,
					new(RedStarLinq.Fill(number, _ => new TreeBranch("type", pos - 1, container) { Extra = NStarType })));
			}
			_PosStack[_Stackpos] = ++pos;
			return TypeClosing(NStarType);
		}
		else if (lexems[pos].Type is LexemType.UnsignedInt or LexemType.LongInt or LexemType.UnsignedLongInt
			or LexemType.LongLong or LexemType.Real or LexemType.Complex or LexemType.String)
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
			return IncreaseStack("Expr", currentTask: nameof(TypeInt2), pos_: pos,
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
		foreach (var x in targetBranch.Name == "List" ? targetBranch.Elements : [targetBranch])
		{
			if (!(x.Name == nameof(Hypername) && (x.Length == 1 || x.Length == 2 && x[1].Name == "Indexes")))
				continue;
			if (CheckContainer(container, stack => UserDefinedConstants.TryGetValue(stack, out var containerConstants)
				&& containerConstants.ContainsKey(x[0].Name), out _))
				continue;
			NStarType TargetNStarType;
			UserDefinedType userDefinedType = default!;
			if (PrimitiveTypes.ContainsKey(x[0].Name))
				TargetNStarType = new NStarType(new(new Block(BlockType.Primitive, x[0].Name, 1)), NoBranches);
			else if (CheckContainer(container, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
				&& containerTempTypes.Any(y => y.Name == x[0].Name), out _))
				TargetNStarType = new NStarType(new(new Block(BlockType.Extra, x[0].Name, 1)), NoBranches);
			else if (CheckContainer(container, stack => TypeExists((stack, x[0].Name), out _), out _)
				|| CheckContainer(container, stack => UserDefinedTypes.TryGetValue((stack, x[0].Name), out userDefinedType),
				out _))
				TargetNStarType = new NStarType(new(new Block((userDefinedType.Attributes & TypeAttributes.Delegate) switch
				{
					TypeAttributes.Delegate => BlockType.Delegate,
					TypeAttributes.Interface => BlockType.Interface,
					TypeAttributes.Enum => BlockType.Enum,
					TypeAttributes.Struct => BlockType.Struct,
					_ => BlockType.Class,
				}, x[0].Name, 1)), NoBranches);
			else
				continue;
			x[0].Replace(new("type", x[0].Pos, x[0].Container)
			{
				Extra = TargetNStarType
			});
			if (targetBranch.Name != "List")
				targetBranch.Replace(new("List", new(targetBranch.Name, targetBranch.Elements)));
		}
		if (OuterNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Class
			&& CheckContainer(container, stack => UserDefinedConstants.TryGetValue(stack, out var containerConstants)
			&& containerConstants.ContainsKey(block.Name), out var matchingContainer)
			&& UserDefinedConstants[matchingContainer][block.Name] is var constant
			&& constant.NStarType.MainType.Equals(DictionaryBlockStack)
			&& constant.NStarType.ExtraTypes.Length == 2 && constant.NStarType.ExtraTypes[0].Name == "type"
			&& constant.NStarType.ExtraTypes[0].Extra is NStarType KeyNStarType
			&& (KeyNStarType.MainType.Equals(RecursiveBlockStack) || KeyNStarType.MainType.Equals(TupleBlockStack)
			&& KeyNStarType.ExtraTypes.Values.All(x => x.Name == "type" && x.Extra is NStarType NStarType
			&& NStarType.MainType.Equals(RecursiveBlockStack))) && constant.NStarType.ExtraTypes[1].Name == "type"
			&& constant.NStarType.ExtraTypes[1].Extra is NStarType ValueNStarType
			&& ValueNStarType.MainType.TryPeek(out block) && block.BlockType == BlockType.Other && block.Name == nameof(Class))
			return TypeClosing(new(OuterNStarType.MainType, new([.. OuterNStarType.ExtraTypes.Values, targetBranch])));
		return TypeClosing(new(TupleBlockStack, new([new("type", OuterNStarType.ExtraTypes.Length != 0
			? OuterNStarType.ExtraTypes[0].Pos : treeBranch.Pos - 2, container) { Extra = OuterNStarType }, targetBranch])));
	}

	private bool TypeClass()
	{
		if (!success || extra is not NStarType NStarType)
			return _SuccessStack[_Stackpos] = false;
		else
		{
			var t = UserDefinedTypes[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)];
			t.BaseType = NStarType;
			UserDefinedTypes[(registeredTypes[registeredTypesPos].Container, registeredTypes[registeredTypesPos].Name)] = t;
			_TBStack[_Stackpos]?.Add(treeBranch ?? TreeBranch.DoNotAdd());
			pos = registeredTypes[registeredTypesPos++].End;
			return TypeSingularTuple(new(new(new Block(BlockType.Other, "Class", 1)),
				new([new("Class", start, pos, container) { Extra = t }])));
		}
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
		else if (IsCurrentLexemOperator(","))
		{
			_ExtraStack[_Stackpos - 1] = NStarType;
			_TBStack[_Stackpos] = new("type", pos - 1, container) { Extra = NStarType };
			return Default();
		}
		else if (IsLexemOther(pos, [")", "]"]))
		{
			if (!(_Stackpos >= 3 && _TaskStack[_Stackpos] == nameof(IdentifierType2)
				&& _ExtraStack[_Stackpos - 1] is NStarType RecursiveNStarType && RecursiveNStarType.Equals(RecursiveType)))
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
		if (lexems[pos].Type == LexemType.Keyword && BasicExprKeywords.Contains(s.ToString())
			|| lexems[pos].Type == LexemType.Operator && BasicExprOperators.Contains(s.ToString()))
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
		else if (lexems[pos].Type == LexemType.LongLong)
		{
			pos++;
			_TBStack[_Stackpos] = new(s.EndsWith("LL") ? s : s.AddRange("LL"), pos - 1, pos, container)
			{
				Extra = LongLongType
			};
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Real)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] == 'r' ? s : s.Add('r'), pos - 1, pos, container) { Extra = RealType };
			return Default();
		}
		else if (lexems[pos].Type == LexemType.Complex)
		{
			pos++;
			_TBStack[_Stackpos] = new(s[^1] is 'c' or 'I' ? s : s.Add('c'), pos - 1, pos, container) { Extra = ComplexType };
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
		else if (IsCurrentLexemOther("{") && UnnamedTypeStartIndexes.TryGetValue(container, out var containerStartIndexes)
			&& containerStartIndexes.Find(x => int.TryParse(x[1..].ToString(), out var otherUnnamedIndex)
			&& otherUnnamedIndex == ((container.Length == 0) ? globalUnnamedIndex : container.Peek().UnnamedIndex))
			is var startIndex && startIndex != null
			&& UserDefinedTypes.ContainsKey((container, startIndex)))
		{
			pos++;
			if (IsCurrentLexemOther("}"))
			{
				_TBStack[_Stackpos] = new(nameof(ClassMain), pos - 1, container);
				pos++;
				return Default();
			}
			_ = (container.Length == 0) ? globalUnnamedIndex++ : container.Peek().UnnamedIndex++;
			return IncreaseStack(nameof(ClassMain), currentTask: nameof(BasicExpr3),
				applyCurrentTask: true, currentBranch: new("Expr", pos, container), assignCurrentBranch: true,
				container_: new(container.Append(new(BlockType.Class, startIndex, 1))));
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

	private bool BasicExpr3()
	{
		if (!success)
			return _SuccessStack[_Stackpos] = false;
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
		else if (IsCurrentLexemOther("}"))
			pos++;
		else
			return EndWithError(0x200B, pos, true);
		if (pos >= end)
			return _SuccessStack[_Stackpos] = false;
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
			if (lexems[pos - 1].String != ";" || _TBStack[_Stackpos] == null || !(_TBStack[_Stackpos]?.Length >= 1))
				continue;
			if (new List<String>
			{
				"if", "if!", "else", "else if", "else if!", "while", "while!", "repeat", "for", "loop", "loop-while",
				"loop-while!"
			}.Contains(_TBStack[_Stackpos]?[^1].Name ?? "") && treeBranch == null)
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
			&& !(task == nameof(Expr2) && _TaskStack[_Stackpos - 1].AsSpan() is not (nameof(BasicExpr2) or nameof(TypeInt2))
			&& treeBranch != null && treeBranch.Name.AsSpan() is not ("Expr" or "Assignment" or "DeclarationAssignment")))
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
			_TBStack[_Stackpos]?.Name = newInfo;
			_TBStack[_Stackpos]?.Add(newBranch);
		}
	}

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errors, bool produceWreck, int end = -1)
	{
		while (pos < (end == -1 ? lexems.Length : end))
		{
			if (lexems[pos].Type != LexemType.Other)
			{
				pos++;
				continue;
			}
			var s = lexems[pos].String;
			if (s == bracket)
			{
				pos++;
				return true;
			}
			else if (s.Length == 1 && s[0] is '(' or '[' or '{')
			{
				pos++;
				CloseBracket(ref pos, s == "(" ? ")" : s == "[" ? "]" : "}", ref errors, produceWreck);
			}
			else if (s.Length == 1 && s[0] is ')' or ']' or '}' || bracket != "}" && s.AsSpan() is ";" or "\r\n")
			{
				if (produceWreck)
					GenerateMessage(0x9011, pos, false, bracket);
				return false;
			}
			else
				pos++;
		}
		return false;
	}

	private void ValidateLocalName(NStarType NStarType)
	{
		var bTypeContext = NStarType.MainType.Equals(RecursiveBlockStack);
		if (bTypeContext)
			ValidateTypeName();
		else
			ValidateLocalNonTypeName(NStarType);
	}

	private void ValidateTypeName()
	{
		if (CodeStyleRules.TestEnvironment)
			return;
		if (lexems[pos].String.Length == 1 && lexems[pos].String[0] != 'T')
			GenerateMessage(0x8016, pos, false);
		if (lexems[pos].String[0] != 'T' || lexems[pos].String.Length != 1
			&& (lexems[pos].String.ToHashSet().ExceptWith("0123456789_").Length == 1
			? lexems[pos].String.GetSlice(1).ToHashSet().ExceptWith("0123456789").Length != 0
			: !char.IsUpper(lexems[pos].String[1])))
			GenerateMessage(0x8019, pos, false);
	}

	private void ValidateLocalNonTypeName(NStarType NStarType)
	{
		if (CodeStyleRules.TestEnvironment)
			return;
		if (NStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive && block.Name == "var")
			return;
		var bNumberContext = NStarType.MainType.Length == 1
			&& NStarType.MainType.TryPeek(out block) && block.Name.AsSpan() is "byte" or "short int" or "unsigned short int"
			or "int" or "unsigned int" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			&& NStarType.ExtraTypes.Length == 0;
		var bStringContext = TypeEqualsToPrimitive(NStarType, "string");
		if (lexems[pos].String.Length == 1
			&& !(bNumberContext ? "ijknxyz" : bStringContext ? "sxyz" : "xyz").Contains(lexems[pos].String[0]))
			GenerateMessage((ushort)(bNumberContext ? 0x8014 : bStringContext ? 0x8015 : 0x8017), pos, false);
		else if (lexems[pos].String.Length != 1
			&& lexems[pos].String.ToHashSet().ExceptWith("0123456789_").Length == 1)
			GenerateMessage(0x801A, pos, false);
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
