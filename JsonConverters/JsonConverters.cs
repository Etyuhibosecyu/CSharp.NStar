using NStar.Core;
using NStar.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using static CSharp.NStar.TypeMappings;
using static System.Math;
using String = NStar.Core.String;

namespace CSharp.NStar;

public static class JsonConverters
{
	public static JsonSerializerSettings SerializerSettings { get; } = new()
	{
		Converters = [new StringConverter(), new IEnumerableConverter(), new TypeConverter(), new TupleConverter(),
			new UniversalConverter(), new ValueTypeConverter(), new IClassConverter(), new DoubleConverter()]
	};

	public class DoubleConverter : JsonConverter<double>
	{
		public override double ReadJson(JsonReader reader, Type objectType, double existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
		public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
		{
			if (value is (double)1 / 0)
			{
				writer.WriteRaw("Infty");
				return;
			}
			if (value is (double)-1 / 0)
			{
				writer.WriteRaw("-Infty");
				return;
			}
			if (value is (double)0 / 0)
			{
				writer.WriteRaw("Uncty");
				return;
			}
			var truncated = unchecked((long)Truncate(value));
			if (truncated == value)
				writer.WriteValue(truncated);
			else
				writer.WriteValue(value);
		}
	}

	public class IClassConverter : JsonConverter<IClass>
	{
		public override IClass? ReadJson(JsonReader reader, Type objectType, IClass? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();
		public override void WriteJson(JsonWriter writer, IClass? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}

			var netType = value.GetType();
			writer.WriteRaw("new " + netType.Name + "(");
			List<Type> types = [];
			for (var baseType = netType; baseType != null; baseType = baseType.BaseType)
				types.Add(baseType);
			ListHashSet<PropertyInfo> hs = new(new EComparer<PropertyInfo>((x, y) => x.Name == y.Name,
				x => x.Name.GetHashCode()));
			foreach (var x in types.Reverse())
				hs.AddRange(x.GetProperties());
			var en = hs.GetEnumerator();
			if (!en.MoveNext())
			{
				writer.WriteRaw(")");
				return;
			}
			writer.WriteRaw(JsonConvert.SerializeObject(en.Current.GetValue(value), SerializerSettings));
			while (en.MoveNext())
				writer.WriteRaw(", " + JsonConvert.SerializeObject(en.Current.GetValue(value), SerializerSettings));
			writer.WriteRaw(")");
		}
	}

	public class IEnumerableConverter : JsonConverter<IEnumerable>
	{
		public override IEnumerable? ReadJson(JsonReader reader, Type objectType, IEnumerable? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

		public override void WriteJson(JsonWriter writer, IEnumerable? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			var en = value.GetEnumerator();
			if (!en.MoveNext())
			{
				writer.WriteRaw("()");
				return;
			}
			writer.WriteRaw("(" + JsonConvert.SerializeObject(en.Current, SerializerSettings));
			while (en.MoveNext())
				writer.WriteRaw(", " + JsonConvert.SerializeObject(en.Current, SerializerSettings));
			writer.WriteRaw(")");
		}
	}

	public class StringConverter : JsonConverter<String>
	{
		public override String? ReadJson(JsonReader reader, Type objectType, String? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

		public override void WriteJson(JsonWriter writer, String? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			if (value.GetAfter('\"').Contains('\"') && value.TryTakeIntoRawQuotes(out var rawString))
				writer.WriteRaw(rawString.ToString());
			else if (!value.GetAfter('\\').Contains('\\'))
				writer.WriteRaw(value.TakeIntoQuotes().ToString());
			else
				writer.WriteRaw(value.TakeIntoVerbatimQuotes().ToString());
		}
	}

	public class TupleConverter : JsonConverter<ITuple>
	{
		public override ITuple? ReadJson(JsonReader reader, Type objectType, ITuple? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

		public override void WriteJson(JsonWriter writer, ITuple? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			var outputArray = new string[value.Length];
			for (var i = 0; i < outputArray.Length; i++)
				outputArray[i] = JsonConvert.SerializeObject(value[i], SerializerSettings);
			writer.WriteRaw("(" + string.Join(", ", outputArray) + ")");
		}
	}

	public class TypeConverter : JsonConverter<Type>
	{
		public override Type ReadJson(JsonReader reader, Type objectType, Type? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();
		public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteRaw(TypeMappingBack(value, [], []).ToString());
		}
	}

	public class UniversalConverter : JsonConverter<NStarObject>
	{
		public override NStarObject ReadJson(JsonReader reader, Type objectType, NStarObject existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();
		public override void WriteJson(JsonWriter writer, NStarObject value, JsonSerializer serializer) =>
			writer.WriteRaw(value.ToString(true).ToString());
	}

	public class ValueTypeConverter : JsonConverter<ValueType>
	{
		public override ValueType? ReadJson(JsonReader reader, Type objectType, ValueType? existingValue,
			bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

		public override void WriteJson(JsonWriter writer, ValueType? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			var type = value.GetType();
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var outputArray = new string[fields.Length];
			for (var i = 0; i < fields.Length; i++)
			{
				var x = fields[i];
				if (x.DeclaringType == typeof(bool))
					outputArray[i] = value.ToString()!.ToLowerInvariant();
				else if (x.FieldType == type)
					outputArray[i] = value.ToString()!;
				else
					outputArray[i] = JsonConvert.SerializeObject(x.GetValue(value), SerializerSettings);
			}
			var joined = string.Join(", ", outputArray);
			writer.WriteRaw(fields.Length == 1 ? joined : "(" + joined + ")");
		}
	}
}
