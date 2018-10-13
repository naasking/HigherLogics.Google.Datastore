using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// Conversions for standard CLR types.
    /// </summary>
    static class Convert
    {
        public static int Int32(Value x) => (int)x.IntegerValue;
        public static Value Int32(int x) => x;

        public static short Int16(Value x) => (short)x.IntegerValue;
        public static Value Int16(short x) => x;

        public static sbyte SByte(Value x) => (sbyte)x.IntegerValue;
        public static Value SByte(sbyte x) => x;

        public static ulong UInt64(Value x) => unchecked((ulong)x.IntegerValue);
        public static Value UInt64(ulong x) => unchecked((long)x);

        public static uint UInt32(Value x) => (uint)x.IntegerValue;
        public static Value UInt32(uint x) => x;

        public static ushort UInt16(Value x) => (ushort)x.IntegerValue;
        public static Value UInt16(ushort x) => x;

        public static byte Byte(Value x) => (byte)x.IntegerValue;
        public static Value Byte(byte x) => x;

        public static float Single(Value x) => (float)x.DoubleValue;
        public static Value Single(float x) => x;

        public static decimal Decimal(Value v)
        {
            var x = v.ArrayValue.Values;
            return new decimal(new Union { L = (long[])v }.I);
        }

        public static Value Decimal(decimal x)
        {
            return new Union { I = decimal.GetBits(x) }.L;
        }

        public static Value DateTime(DateTime x)
        {
            switch (x.Kind)
            {
                case DateTimeKind.Utc: return x;
                case DateTimeKind.Local: return x.ToUniversalTime();
                default:
                    throw new ArgumentException("DateTime.Kind must be either local or UTC.");
            }
        }

        public static TimeSpan TimeSpan(Value x) => new TimeSpan(x.IntegerValue);
        public static Value TimeSpan(TimeSpan x) => x.Ticks;
        
        public static Guid Guid(Value x) => new System.Guid(x.BlobValue.ToByteArray());
        public static Value Guid(Guid x) => x.ToByteArray();

        public static char Char(Value x) => x.StringValue[0];
        public static Value Char(char x) => x.ToString();

        public static Uri Uri(Value x) => new System.Uri(x.StringValue);
        public static Value Uri(Uri x) => x.ToString();

        public static System.Type Type(Value x) => System.Type.GetType(x.StringValue);
        public static Value Type(System.Type x) => x.AssemblyQualifiedName;

        public static Stream Stream(Value v) => new MemoryStream((byte[])v);
        public static Value Stream(Stream x) => Google.Protobuf.ByteString.FromStream(x);

        public static string String(Value x) => x.StringValue;
        public static Value String(string x) => x;

        public static T? Nullable<T>(Value v) where T : struct =>
            v.IsNull ? new T?() : Value<T>.From(v);
        public static Value Array<T>(T? v) where T : struct =>
            v == null ? Value.ForNull() : Value<T>.To(v.Value);

        public static T[] Array<T>(Value v) =>
            v.ArrayValue.Values.Select(Value<T>.From).ToArray();
        public static Value Array<T>(T[] v) => v.Select(Value<T>.To).ToArray();

        public static IEnumerable<T> IEnumerable<T>(Value v) =>
            v.ArrayValue.Values.Select(Value<T>.From);
        public static Value IEnumerable<T>(IEnumerable<T> v) =>
            v.Select(Value<T>.To).ToArray();

        public static T Entity<T>(Value v) where T : class =>
            Google.Cloud.Datastore.V1.Mapper.Entity<T>.From(Activator.CreateInstance<T>(), v.EntityValue);

        public static Value Entity<T>(T v) where T : class =>
            Google.Cloud.Datastore.V1.Mapper.Entity<T>.To(new Entity(), v);
    }
}
