﻿using System;
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
        [Theory]
        [InlineData(null)]
        [InlineData("foo")]
        public static void String(string s)
        {
            var e = Value<string>.To(s);
            Assert.Equal(s, Value<string>.From(e));
        }

        [Theory]
        [InlineData(99)]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public static void Int32(int i)
        {
            var e = Value<int>.To(i);
            Assert.Equal(i, Value<int>.From(e));
        }

        [Theory]
        [InlineData((uint)99)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue)]
        public static void UInt32(uint i)
        {
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

        [Fact]
        public static void ValueSame()
        {
            var now = new Value();
            var e = Value<Value>.To(now);
            Assert.Equal(now, e);
            Assert.Equal(now, Value<Value>.From(now));
            Assert.Equal(now, Value<Value>.To(e));
        }

        [Fact]
        public static void Key()
        {
            var now = Mapper.CreateIncompleteKey<Simple>();
            var e = Value<Key>.To(now);
            Assert.Equal(now, e.KeyValue);
        }

        [Fact]
        public static void FKEquality()
        {
            var fk1 = new FK<Simple>(Mapper.CreateIncompleteKey<Simple>());
            var fk2 = new FK<Simple>(Mapper.CreateIncompleteKey<Simple>());
            var fk3 = new FK<Simple>(fk1.Key);
            var fk4 = new FK<Simple>(new Simple { Baz = "Baz" });
            var fk5 = new FK<Simple>(new Simple { Baz = "Hello" });
            var fk6 = new FK<Simple>(fk4.Value);
            Assert.Equal(fk1, fk3);
            Assert.NotEqual(fk1, fk2);
            Assert.NotEqual(fk3, fk2);
            Assert.NotEqual(fk1, fk4);
            Assert.NotEqual(fk2, fk4);
            Assert.NotEqual(fk3, fk4);
            Assert.NotEqual(fk5, fk4);
            Assert.NotEqual(fk5, fk6);
            Assert.Equal(fk4, fk6);
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
        [InlineData(0)]
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
        public static void IntArraySegment()
        {
            var a = new[] { 0, int.MinValue, int.MaxValue, 99 };
            var x = new ArraySegment<int>(a, 1, 2);
            var v = Value<ArraySegment<int>>.To(x);
            Assert.Equal(x, Value<ArraySegment<int>>.From(v));
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
            var kv = new Dictionary<int, string>
            {
                { 99, "hello world!" },
                { int.MinValue, "it's the end!" },
            };
            var e = Value<Dictionary<int, string>>.To(kv);
            var rt = Value<Dictionary<int, string>>.From(e);
            Assert.NotNull(e.ArrayValue);
            //Assert.NotNull(e.EntityValue.Values[0].ArrayValue);
            //Assert.Equal(kv.First().Key, e.ArrayValue.Values[0].ArrayValue.Values[0]);
            Assert.Equal(kv, rt);
        }

        [Fact]
        public static void DictionaryNullTests()
        {
            Dictionary<int, string> kv = null;
            var e = Value<Dictionary<int, string>>.To(kv);
            var rt = Value<Dictionary<int, string>>.From(e);
            Assert.Null(e);
            Assert.Null(rt);
        }

        [Fact]
        public static void ListTests()
        {
            var l = new List<string> { "hello world!", "it's the end!" };
            var e = Value<List<string>>.To(l);
            var rt = Value<List<string>>.From(e);
            Assert.NotNull(e.ArrayValue);
            Assert.Equal(l[0], e.ArrayValue.Values[0].StringValue);
            Assert.Equal(l, rt);
        }

        [Fact]
        public static void IListTests()
        {
            IList<string> l = new List<string> { "hello world!", "it's the end!" };
            var e = Value<IList<string>>.To(l);
            var rt = Value<IList<string>>.From(e);
            Assert.NotNull(e.ArrayValue);
            Assert.Equal(l[0], e.ArrayValue.Values[0].StringValue);
            Assert.Equal(l, rt);
        }

        [Fact]
        public static void EntityTest()
        {
            var x = new Simple { Bar = 11, Baz = "hello world!" };
            var e = Value<Simple>.To(x);
            var rt = Value<Simple>.From(e);
            Assert.NotNull(e.EntityValue);
            Assert.Equal(x.Bar, rt.Bar);
            Assert.Equal(x.Baz, rt.Baz);
        }

        [Fact]
        public static void EntityNullTest()
        {
            Simple x = null;
            var e = Value<Simple>.To(x);
            var rt = Value<Simple>.From(e);
            Assert.Null(e);
            Assert.Null(rt);
        }

        [Fact]
        public static void TimeZoneTest()
        {
            var tz = TimeZoneInfo.Local;
            var e = Value<TimeZoneInfo>.To(tz);
            var rt = Value<TimeZoneInfo>.From(e);
            Assert.Equal(tz, rt);
            Assert.Equal(tz.ToSerializedString(), e.StringValue);
        }

        [Fact]
        public static void TimeZoneUtcTest()
        {
            var tz = TimeZoneInfo.Utc;
            var e = Value<TimeZoneInfo>.To(tz);
            var rt = Value<TimeZoneInfo>.From(e);
            Assert.Equal(tz, rt);
            Assert.Equal(tz.ToSerializedString(), e.StringValue);
        }

        [Fact]
        public static void TimeZoneRandTest()
        {
            var rand = new Random((int)DateTime.Now.Ticks);
            var tzs = TimeZoneInfo.GetSystemTimeZones();
            var tz = tzs[rand.Next() % tzs.Count];
            var e = Value<TimeZoneInfo>.To(tz);
            var rt = Value<TimeZoneInfo>.From(e);
            Assert.Equal(tz, rt);
            Assert.Equal(tz.ToSerializedString(), e.StringValue);
        }

        [Fact]
        public static void TimeZoneAdjustmentRuleTest()
        {
            foreach (var tz in TimeZoneInfo.Local.GetAdjustmentRules())
            {
                var e = Value<TimeZoneInfo.AdjustmentRule>.To(tz);
                var rt = Value<TimeZoneInfo.AdjustmentRule>.From(e);
                Assert.Equal(tz, rt);
                Assert.NotNull(e.EntityValue);
            }
        }

        [Fact]
        public static void TimeZoneTransitionTimeTest()
        {
            foreach (var x in TimeZoneInfo.Local.GetAdjustmentRules())
            {
                foreach (var tz in new[] { x.DaylightTransitionStart, x.DaylightTransitionEnd })
                {
                    var e = Value<TimeZoneInfo.TransitionTime>.To(tz);
                    var rt = Value<TimeZoneInfo.TransitionTime>.From(e);
                    Assert.Equal(tz, rt);
                }
            }
        }
    }
}
