namespace CSharp.NStar;

public enum TypeAttributes
{
	None = 0,
	Sealed = 1,
	Abstract = 2,
	Static = 3,
	Struct = 4,
	Enum = 5,
	Delegate = 6,
	Closed = 16,
	Protected = 32,
	Internal = 64,
	Partial = 256,
}

public enum PropertyAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	Const = 16,
	NoSet = 32,
	ClosedSet = 64,
	ProtectedSet = 128,
}

public enum ConstantAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
}

public enum FunctionAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	Const = 16,
	Multiconst = 32,
	Abstract = 64,
	Sealed = 128,
	New = 192,
	Wrong = 256,
}

public enum ParameterAttributes
{
	None = 0,
	Optional = 1,
	Ref = 2,
	Out = 4,
	Params = 6,
}

public enum ConstructorAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	Multiconst = 16,
	Abstract = 32,
}

public enum BlockType
{
	Unnamed,
	Primitive,
	Extra,
	Namespace,
	Class,
	Struct,
	Interface,
	Delegate,
	Enum,
	Function,
	Constructor,
	Destructor,
	Operator,
	Extent,
	Other,
}

public enum TypeConstraints
{
	None,
	BaseClassOrInterface,
	BaseInterface,
	NotAbstract,
}

public enum RawStringState
{
	Normal,
	ForwardSlash,
	Quote,
	ForwardSlashAndQuote,
	EmailSign,
}
