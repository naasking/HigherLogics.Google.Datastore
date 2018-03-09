﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Google.Cloud.Datastore.V1;
using Google.Cloud.Datastore.V1.Mapper;

namespace MapperTests
{
    public class Tests
    {
        [Fact]
        public static void String()
        {
            Value e = "foo";
            Assert.Equal("foo", Value<string>.From(e));
        }

        [Fact]
        public static void Int32()
        {
            Value e = 99;
            Assert.Equal(99, Value<int>.From(e));
        }

        [Fact]
        public static void UInt32()
        {
            Value e = (uint)99;
            Assert.Equal((uint)99, Value<uint>.From(e));
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
        [InlineData(typeof(Tests))]
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
            Assert.Throws<InvalidOperationException>(() => Value<Assert>.From(new Value()));
            Assert.Throws<InvalidOperationException>(() => Value<IntPtr>.To(new IntPtr()));
        }
    }
}