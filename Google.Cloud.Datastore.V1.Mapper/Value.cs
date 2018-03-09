using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using W = Google.Protobuf.WellKnownTypes;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// Convert serialized values into CLR values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Value<T>
    {
        /// <summary>
        /// Convert a <see cref="Value"/> to type <typeparamref name="T"/>.
        /// </summary>
        public static Func<Value, T> From { get; private set; }

        /// <summary>
        /// Convert a <typeparamref name="T"/> to a <see cref="Value"/>.
        /// </summary>
        public static Func<T, Value> To { get; private set; }

        static Value()
        {
            // first search for conversions provided as static methods on this class, which may override
            // the default conversions provided by Google's library, then fall back to Google's defaults
            //FIXME: need to specially handle: enums, arrays
            var type = typeof(T);
            var toTypes = new[] { type };
            var to = typeof(Value<T>).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                     .SingleOrDefault(x => x.Name == type.Name && x.ReturnType == typeof(Value))
                  ?? typeof(Value).GetMethod("op_Implicit", toTypes);
            var from = typeof(Value<T>).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                       .SingleOrDefault(x => x.ReturnType == type)
                    ?? typeof(Value).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    .SingleOrDefault(x => x.ReturnType == type && "op_Explicit".Equals(x.Name, StringComparison.Ordinal));
            if (to != null && from != null)
                Override((Func<Value, T>)from.CreateDelegate(typeof(Func<Value, T>)),
                         (Func<T, Value>)to.CreateDelegate(typeof(Func<T, Value>)));
            else if (to != null || from != null)
                throw new Exception("Type " + type.Name + " has only one conversion but needs both.");
        }

        /// <summary>
        /// Overload the value conversions to/from Protobuf's <see cref="Value"/> type.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void Override(Func<Value, T> from, Func<T, Value> to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        #region Convenient overloads
        static int Int32(Value x) => (int)x.IntegerValue;
        static Value Int32(int x) => x;

        static short Int16(Value x) => (short)x.IntegerValue;
        static Value Int16(short x) => x;

        static sbyte SByte(Value x) => (sbyte)x.IntegerValue;
        static Value SByte(sbyte x) => x;

        static ulong UInt64(Value x) => unchecked((ulong)x.IntegerValue);
        static Value UInt64(ulong x) => unchecked((long)x);

        static uint UInt32(Value x) => (uint)x.IntegerValue;
        static Value UInt32(uint x) => x;

        static ushort UInt16(Value x) => (ushort)x.IntegerValue;
        static Value UInt16(ushort x) => x;

        static byte Byte(Value x) => (byte)x.IntegerValue;
        static Value Byte(byte x) => x;

        static float Single(Value x) => (float)x.DoubleValue;
        static Value Single(float x) => x;

        static decimal Decimal(Value v)
        {
            var x = v.ArrayValue.Values;
            return new decimal(new Union { L = (long[])v }.I);
        }

        static Value Decimal(decimal x)
        {
            return new Union { I = decimal.GetBits(x) }.L;
        }

        static Value DateTime(DateTime x)
        {
            switch (x.Kind)
            {
                case DateTimeKind.Utc: return x;
                case DateTimeKind.Local: return x.ToUniversalTime();
                default:
                    throw new ArgumentException("DateTime.Kind must be either local or UTC.");
            }
        }

        static TimeSpan TimeSpan(Value x) => new TimeSpan(x.IntegerValue);
        static Value TimeSpan(TimeSpan x) => x.Ticks;

        static Guid Guid(Value x) => new System.Guid(x.BlobValue.ToByteArray());
        static Value Guid(Guid x) => x.ToByteArray();

        static char Char(Value x) => x.StringValue[0];
        static Value Char(char x) => x.ToString();

        static Uri Uri(Value x) => new System.Uri(x.StringValue);
        static Value Uri(Uri x) => x.ToString();

        static System.Type Type(Value x) => System.Type.GetType(x.StringValue);
        static Value Type(System.Type x) => x.AssemblyQualifiedName;

        static Stream Stream(Value v) => new MemoryStream((byte[])v);

        static Value Stream(Stream x) => Google.Protobuf.ByteString.FromStream(x);

        //FIXME: figure out array support

        #endregion
    }
}
