using System;
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

        [Fact]
        public static void Double()
        {
            var x = double.MaxValue / 3;
            Value e = x;
            Assert.Equal(x, Value<double>.From(e));
        }

        [Fact]
        public static void Float()
        {
            var x = float.MaxValue / 3;
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

    }
}
