namespace CSharp.NStar;

public static class IntermediateFunctions
{
	private static int random_calls;
	private static readonly double random_initializer = DateTime.Now.ToBinary() / 1E+9;

	private static double RandomNumberBase(int calls, double initializer, double max)
	{
		var a = initializer * 5.29848949848415968;
		var b = Abs(a - Floor(a / 100000) * 100000 + Sin(calls / 1.597486513 + 2.5845984) * 45758.479849894 - 489.498489641984);
		var c = Tan((b - Floor(b / 179.999) * 179.999 - 90) * PI / 180);
		var d = Pow(Abs(Sin(Cos(Tan(calls) * 3.0362187913025793 + 0.10320655487900326) * PI - 2.032198747013) * 146283.032478491032657 - 2903.0267951604) + 0.000001, 2.3065479615036587) + Pow(Abs(Math.Log(Abs(Pow(Pow((double)calls * 123 + 64.0657980165456, 2) + Pow(max - 21.970264984615, 2), 0.5) * 648.0654731649 - 47359.03197931073648) + 0.000001)) + 0.000001, 0.60265497063473049);
		var e = Math.Log(Abs(Pow(Abs(Atan((a - Floor(a / 1000) * 1000 - max) / 169.340493) * 1.905676152049703) + 0.000001, 12.206479803657304) - 382.0654987304) + 0.000001);
		var f = Pow(Abs(c * 1573.06546157302 + d / 51065574.32761504 + e * 1031.3248941027032) + 0.000001, 2.30465546897032);
		return RealRemainder(f, max);
	}

	public static List<int> Chain(int start, int end) => new Chain(start, end - start + 1).ToList();

	public static T Choose<T>(params List<T> variants) => variants.Random();

	public static double Factorial(uint x)
	{
		if (x <= 1)
			return 1;
		else if (x > 170)
			return (double)1 / 0;
		else
		{
			double n = 1;
			for (var i = 2; i <= x; i++)
			{
				n *= i;
			}
			return n;
		}
	}

	public static double Fibonacci(uint x)
	{
		if (x <= 1)
		{
			return x;
		}
		else if (x > 1476)
		{
			return 0;
		}
		else
		{
			var a = new double[] { 0, 1, 1 };
			for (var i = 2; i <= (int)x - 1; i++)
			{
				a[0] = a[1];
				a[1] = a[2];
				a[2] = a[0] + a[1];
			}
			return a[2];
		}
	}

	public static double Frac(double x) => x - Truncate(x);

	public static int IntRandomNumber(int max)
	{
		var a = (int)Floor(RandomNumberBase(random_calls, random_initializer, max) + 1);
		random_calls++;
		return a;
	}

	public static dynamic ListWithSingle<T>(T item)
	{
		if (item is bool b)
			return new BitList([b]);
		else if (typeof(T).IsUnmanaged())
			return typeof(NList<>).MakeGenericType(typeof(T)).GetConstructor([typeof(G.IEnumerable<T>)])
				?.Invoke([(G.IEnumerable<T>)[item]]) ?? throw new InvalidOperationException();
		else
			return new List<T>(item);
	}

	public static double Log(double a, double x) => Math.Log(x, a);

	public static double RandomNumber(double max)
	{
		var a = RandomNumberBase(random_calls, random_initializer, max);
		random_calls++;
		return a;
	}

	public static double RealRemainder(double x, double y) => x - Floor(x / y) * y;

	public static int RGB(int r, int g, int b) => Color.FromArgb(r, g, b).ToArgb();
}
