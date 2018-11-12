using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Google.Cloud.Datastore.V1;
using HigherLogics.Google.Datastore;

namespace MapperTests
{
    public class ConversionTests
    {
        [Fact]
        public static void String()
        {
            var s = "foo";
            var e = Value<string>.To(s);
            Assert.Equal(s, Value<string>.From(e));
        }

        [Fact]
        public static void Int32()
        {
            var i = 99;
            var e = Value<int>.To(i);
            Assert.Equal(i, Value<int>.From(e));
        }

        [Fact]
        public static void UInt32()
        {
            uint i = 99;
            var e = Value<uint>.To(i);
            Assert.Equal(i, Value<uint>.From(e));
        }

        [Fact]
        public static void Int64()
        {
            Value e = long.MaxValue / 2;
            Assert.Equal(long.MaxValue / 2, Value<long>.From(e));
        }

        [Fact]
        public static void UInt64()
        {
            Value e = Value<ulong>.To(ulong.MaxValue / 2);
            Assert.Equal(ulong.MaxValue / 2, Value<ulong>.From(e));
        }

        [Fact]
        public static void Int16()
        {
            Value e = (short)99;
            Assert.Equal((short)99, Value<short>.From(e));
        }

        [Fact]
        public static void UInt16()
        {
            Value e = (ushort)99;
            Assert.Equal((ushort)99, Value<ushort>.From(e));
        }

        [Fact]
        public static void SByte()
        {
            Value e = (sbyte)99;
            Assert.Equal((sbyte)99, Value<sbyte>.From(e));
        }

        [Fact]
        public static void Byte()
        {
            Value e = (byte)99;
            Assert.Equal((byte)99, Value<byte>.From(e));
        }


        [Fact]
        public static void DateTimes()
        {
            Value e = DateTime.Today.ToUniversalTime();
            Assert.Equal(DateTime.Today.ToUniversalTime(), Value<DateTime>.From(e));
        }

        [Fact]
        public static void DateTimeOffsets()
        {
            var now = new DateTimeOffset(DateTime.Now);
            Value e = now;
            Assert.Equal(now, Value<DateTimeOffset>.From(e));
        }

        [Theory]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue / 3)]
        [InlineData(double.MaxValue / 99)]
        [InlineData(double.MaxValue / 99999)]
        [InlineData(double.MinValue / 3)]
        [InlineData(double.MinValue / 99)]
        [InlineData(double.MinValue / 99999)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        public static void Double(double x)
        {
            Value e = x;
            Assert.Equal(x, Value<double>.From(e));
        }

        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue / 3)]
        [InlineData(float.MaxValue / 99)]
        [InlineData(float.MaxValue / 99999)]
        [InlineData(float.MinValue / 3)]
        [InlineData(float.MinValue / 99)]
        [InlineData(float.MinValue / 99999)]
        public static void Float(float x)
        {
            Value e = x;
            Assert.Equal(x, Value<float>.From(e));
        }

        [Fact]
        public static void Decimal()
        {
            var x = decimal.MaxValue / 3;
            Value e = Value<decimal>.To(x);
            Assert.Equal(x, Value<decimal>.From(e));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Boolean(bool x)
        {
            Value e = x;
            Assert.Equal(x, Value<bool>.From(e));
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue / 999999)]
        [InlineData(long.MaxValue / 99)]
        public static void TimeSpans(long ticks)
        {
            var x = new TimeSpan(ticks);
            Value e = Value<TimeSpan>.To(x);
            Assert.Equal(x, Value<TimeSpan>.From(e));
        }

        [Theory]
        [InlineData('c')]
        [InlineData(char.MaxValue)]
        [InlineData(char.MinValue)]
        public static void Chars(char x)
        {
            Value e = Value<char>.To(x);
            Assert.Equal(x, Value<char>.From(e));
        }

        [Theory]
        [InlineData("http://microsoft.com")]
        [InlineData("file://foo/bar/")]
        public static void Uris(string uri)
        {
            var x = new Uri(uri);
            Value e = Value<Uri>.To(x);
            Assert.Equal(x, Value<Uri>.From(e));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(ConversionTests))]
        [InlineData(typeof(Value))]
        public static void Types(Type x)
        {
            Value e = Value<Type>.To(x);
            Assert.Equal(x, Value<Type>.From(e));
        }

        [Fact]
        public static void Guids()
        {
            var x = Guid.NewGuid();
            Value e = Value<Guid>.To(x);
            Assert.Equal(x, Value<Guid>.From(e));
        }

        [Fact]
        public static void Errors()
        {
            Assert.Throws<InvalidOperationException>(() => Value<IntPtr>.From(new Value()));
            Assert.Throws<InvalidOperationException>(() => Value<IntPtr>.To(new IntPtr()));
        }

        [Fact]
        public static void IntArrays()
        {
            var x = new[] { 0, int.MinValue, int.MaxValue, 99 };
            var v = Value<int[]>.To(x);
            Assert.Equal(x, Value<int[]>.From(v));
        }

        [Fact]
        public static void StringArrays()
        {
            var x = new[] { "hello", "world", "!", };
            var v = Value<string[]>.To(x);
            Assert.Equal(x, Value<string[]>.From(v));
        }

        [Fact]
        public static void DecimalArrays()
        {
            var x = new[] { 0M, decimal.MinValue, decimal.MaxValue, 99M };
            var v = Value<decimal[]>.To(x);
            Assert.Equal(x, Value<decimal[]>.From(v));
        }

        [Fact]
        public static void DoubleArrays()
        {
            var x = new[] { 0.0, double.MinValue, double.MaxValue, double.MaxValue / 123, double.NegativeInfinity, double.PositiveInfinity };
            var v = Value<double[]>.To(x);
            Assert.Equal(x, Value<double[]>.From(v));
        }

        [Fact]
        public static void IntEnumerable()
        {
            var x = new[] { 0, int.MinValue, int.MaxValue, 99 };
            var v = Value<IEnumerable<int>>.To(x);
            Assert.Equal(x, Value<IEnumerable<int>>.From(v));
        }

        [Fact]
        public static void DecimalEnumerable()
        {
            var x = new[] { 0M, decimal.MinValue, decimal.MaxValue, 99M };
            var v = Value<IEnumerable<decimal>>.To(x);
            Assert.Equal(x, Value<IEnumerable<decimal>>.From(v));
        }

        [Theory]
        [InlineData(99)]
        [InlineData(null)]
        public static void NullableInt32(int? i)
        {
            var e = Value<int?>.To(i);
            Assert.Equal(i, Value<int?>.From(e));
        }

        [Fact]
        public static void NullableDecimal()
        {
            NullableDecimalTheory(99M);
        }

        [Theory]
        [InlineData(null)]
        public static void NullableDecimalTheory(decimal? i)
        {
            var e = Value<decimal?>.To(i);
            Assert.Equal(i, Value<decimal?>.From(e));
        }

        [Fact]
        public static void NullableDecimalArray()
        {
            var x = new[] { 0M, decimal.MinValue, new decimal?(), decimal.MaxValue, 99M, };
            var e = Value<decimal?[]>.To(x);
            Assert.Equal(x, Value<decimal?[]>.From(e));
        }

        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        public static void EnumTests(DateTimeKind i)
        {
            var e = Value<DateTimeKind>.To(i);
            Assert.Equal(i, Value<DateTimeKind>.From(e));
        }

        public enum Temp : sbyte { Foo, Bar }

        [Theory]
        [InlineData(Temp.Bar)]
        [InlineData(Temp.Foo)]
        public static void EnumTempTests(Temp i)
        {
            var e = Value<Temp>.To(i);
            Assert.Equal(i, Value<Temp>.From(e));
        }

        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(null)]
        public static void NullableEnumTests(DateTimeKind? i)
        {
            var e = Value<DateTimeKind?>.To(i);
            Assert.Equal(i, Value<DateTimeKind?>.From(e));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("214 159 196 149 203 218 203 163 137 126")]
        [InlineData("122 233 93 243 249 155 70 181 173 128 184 194 81 160 13 219 8 140 24 197 79 22 77 89 250 157 203 7 98 226 75 237")]
        public static void StreamTests(string data)
        {
            var bytes = data?.Split(' ').Select(byte.Parse).ToArray() ?? new byte[0];
            using (var ms = new MemoryStream(bytes))
            {
                var e = Value<Stream>.To(ms);
                var rt = Value<Stream>.From(e);
                var buf = new byte[rt.Length];
                rt.Read(buf, 0, buf.Length);
                Assert.Equal(bytes, buf);
            }
        }

        [Fact]
        public static void KeyValuePairTests()
        {
            var kv = new KeyValuePair<int, string>(99, "hello world!");
            var e = Value<KeyValuePair<int, string>>.To(kv);
            var rt = Value<KeyValuePair<int, string>>.From(e);
            Assert.Equal(kv, rt);
        }

        [Fact]
        public static void DictionaryTests()
        {
            var kv = new Dictionary<int, string>{
                { 99, "hello world!" },
                { int.MinValue, "it's the end!" },
            };
            var e = Value<Dictionary<int, string>>.To(kv);
            var rt = Value<Dictionary<int, string>>.From(e);
            Assert.NotNull(e.ArrayValue);
            Assert.NotNull(e.ArrayValue.Values[0].ArrayValue);
            Assert.Equal(kv.First().Key, e.ArrayValue.Values[0].ArrayValue.Values[0]);
            Assert.Equal(kv, rt);
        }

        [Fact]
        public static void ListTests()
        {
            var l = new List<string>{ "hello world!", "it's the end!" };
            var e = Value<List<string>>.To(l);
            var rt = Value<List<string>>.From(e);
            Assert.NotNull(e.ArrayValue);
            Assert.Equal(l[0], e.ArrayValue.Values[0].StringValue);
            Assert.Equal(l, rt);
        }
    }
}
