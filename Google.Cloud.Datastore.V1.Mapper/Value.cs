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
    /// <typeparam name="T">The type of value.</typeparam>
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
            //FIXME: need to specially handle: enums, arrays, maybe Version, maybe tuples and value tuples?
            //FIXME: use an entity conversion if no overload available.
            var type = typeof(T);
            var toTypes = new[] { type };
            MethodInfo to, from;
            if (type.IsArray)
            {
                // Extract the array element type and then search for a matching conversion in Value.
                // If none present, then use the generic Convert.Array methods.
                var elem = type.GetElementType();
                to = typeof(Value).GetMethod("op_Implicit", toTypes)
                  ?? new Func<int[], Value>(Convert.Array<int>).GetMethodInfo()
                        .GetGenericMethodDefinition()
                        .MakeGenericMethod(elem);
                from = typeof(Value).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    .SingleOrDefault(x => x.ReturnType == type && "op_Explicit".Equals(x.Name, StringComparison.Ordinal))
                    ?? new Func<Value, int[]>(Convert.Array<int>).GetMethodInfo()
                        .GetGenericMethodDefinition()
                        .MakeGenericMethod(elem);
            }
            else if (type.IsConstructedGenericType)
            {
                // handle some generic types like IEnumerable<T>, IList<T>, etc.
                var baseType = type.GetGenericTypeDefinition();
                var baseName = baseType.Name.Remove(baseType.Name.LastIndexOf('`'));
                var tval = typeof(Value);
                to = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                    .SingleOrDefault(x => x.Name == baseName && x.ReturnType == tval)
                                    ?.MakeGenericMethod(type.GetGenericArguments());
                from = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                      .SingleOrDefault(x => x.Name == baseName && x.ReturnType != tval)
                                      ?.MakeGenericMethod(type.GetGenericArguments());
            }
            else
            {
                // First search for conversions provided as static methods on this class which may override
                // the default conversions provided by Google's library, then fall back to Google's defaults.
                // This is because searching for a conversion for Byte will return the conversion for Int64.
                to = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                    .SingleOrDefault(x => x.Name == type.Name && x.ReturnType == typeof(Value))
                  ?? typeof(Value).GetMethod("op_Implicit", toTypes);
                from = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                      .SingleOrDefault(x => x.ReturnType == type)
                    ?? typeof(Value).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    .SingleOrDefault(x => x.ReturnType == type && "op_Explicit".Equals(x.Name, StringComparison.Ordinal));
            }
            // If no match succeeds and T is a reference type then treat it like an entity
            if (to == null && from == null && !type.GetTypeInfo().IsValueType)
            {
                to = new Func<object, Value>(Convert.Entity<object>).GetMethodInfo()
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(type);
                from = new Func<Value, object>(Convert.Entity<object>).GetMethodInfo()
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(type);
            }
            if (to != null && from != null)
                Override((Func<Value, T>)from.CreateDelegate(typeof(Func<Value, T>)),
                         (Func<T, Value>)to.CreateDelegate(typeof(Func<T, Value>)));
            else if (to != null || from != null)
                throw new Exception("Type " + type.Name + " has only one conversion but needs both.");
            else
                Override(v => throw new InvalidOperationException("No value conversion for type " + type.Name),
                         x => throw new InvalidOperationException("No value conversion for type " + type.Name));
        }

        /// <summary>
        /// Override the value conversions to/from Protobuf's <see cref="Value"/> type.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void Override(Func<Value, T> from, Func<T, Value> to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }
    }
}
