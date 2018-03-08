using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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
            // try to load implicit and explicit conversions that are provided in
            // Google's protobuf library, and if that fails, search for
            // conversions provided as static methods on this class.
            var type = typeof(T);
            var toTypes = new[] { type };
            var to = typeof(Value).GetMethod("op_Implicit", toTypes)
                  ?? typeof(Value<T>).GetMethod(typeof(T).Name, toTypes);
            var from = typeof(Value).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                    .SingleOrDefault(x => x.ReturnType == type && "op_Explicit".Equals(x.Name, StringComparison.Ordinal))
                    ?? typeof(Value<T>).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                       .SingleOrDefault(x => x.ReturnType == type && "op_Explicit".Equals(x.Name, StringComparison.Ordinal));
            if (to != null && from != null)
                Overload((Func<Value, T>)from.CreateDelegate(typeof(Func<Value, T>)),
                         (Func<T, Value>)to.CreateDelegate(typeof(Func<T, Value>)));
            else if (to != null || from != null)
                throw new Exception("Type " + type.Name + " has only one conversion but needs both.");
        }

        /// <summary>
        /// Overload the value conversions to/from Protobuf's <see cref="Value"/> type.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void Overload(Func<Value, T> from, Func<T, Value> to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        #region Convenient overloads
        [StructLayout(LayoutKind.Explicit)]
        struct Union
        {
            //FIXME: is this endian sensitive?
            [FieldOffset(0)] public long[] L;
            [FieldOffset(0)] public int[] I;
        }

        static decimal Decimal(Value v)
        {
            var x = v.ArrayValue.Values;
            return new decimal(new Union { L = (long[])v }.I);
        }

        static Value Decimal(decimal x)
        {
            return new Union { I = decimal.GetBits(x) }.L;
        }

        static Stream Stream(Value v) => new MemoryStream((byte[])v);

        static Value Stream(Stream x) => Google.Protobuf.ByteString.FromStream(x);

        //FIXME: figure out array support

        #endregion
    }
}
