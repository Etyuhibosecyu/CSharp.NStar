global using NStar.Core;
global using System;
global using System.Diagnostics;
global using System.Drawing;
global using System.Numerics;
global using static System.Math;

namespace CSharp.NStar;

public static class NStarUtilityFunctions
{
	private static int random_calls;
	private static readonly double random_initializer = DateTime.Now.ToBinary() / 1E+9;
	private static BitList? primes;

	public static Chain Chain(int start, int end) => new(start, end - start + 1);

	public static Chain Chain(Range range)
	{
		int index = range.Start.Value, index2 = range.End.Value;
		if (!range.Start.IsFromEnd && !range.End.IsFromEnd)
			return Chain(index + 1, index2);
		else if (!range.Start.IsFromEnd && range.End.IsFromEnd)
			return Chain(index + 1, int.MaxValue - index2);
		else if (range.Start.IsFromEnd && !range.End.IsFromEnd)
			return Chain(int.MaxValue - index + 1, index2);
		else if (range.Start.IsFromEnd && range.End.IsFromEnd)
			return Chain(int.MaxValue - index + 1, int.MaxValue - index2);
		else
			return new();
	}

	public static T Choose<T>(params List<T> variants) => variants.Random();

	public static double Factorial(uint x)
	{
		if (x <= 1)
			return 1;
		else if (x > 170)
			return 1d / 0;
		else
		{
			double n = 1;
			for (var i = 2; i <= x; i++)
				n *= i;
			return n;
		}
	}

	public static double Fibonacci(uint x)
	{
		if (x <= 1)
			return x;
		else if (x > 1476)
			return 0;
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

	public static bool IsPrime(int n)
	{
		if (n <= 1)
			return false;
		if (n is 2 or 3)
			return true;
		if ((n & 1) == 0 || n % 3 == 0)
			return false;
		if (primes != null)
		{
			Debug.Assert(primes.Length > n / 3 - 1);
			return primes[n / 3 - 1];
		}
		primes = new(int.MaxValue / 3, true);
		var sqrt = (int)Sqrt(int.MaxValue);
		var increment = 4;
		for (var i = 5; i < sqrt; i += increment = 6 - increment)
		{
			if (!primes[i / 3 - 1])
				continue;
			var innerIncrement = i % 3;
			for (var j = i * i; j >= 0; j += i << (innerIncrement = 3 - innerIncrement))
				primes[j / 3 - 1] = false;
		}
		return primes[n / 3 - 1];
	}

	public static double Log(double a, double x) => Math.Log(x, a);

	public static Complex Log(double a, Complex x) => Complex.Log(x, a);

	public static double RandomNumber(double max)
	{
		var a = RandomNumberBase(random_calls, random_initializer, max);
		random_calls++;
		return a;
	}

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

	public static double RealRemainder(double x, double y) => x - Floor(x / y) * y;

	public static int RGB(int r, int g, int b) => Color.FromArgb(r, g, b).ToArgb();
}
