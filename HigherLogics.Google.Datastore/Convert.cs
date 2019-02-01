using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Google.Cloud.Datastore.V1;
using G = Google.Type;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// Conversions between standard CLR types and Google datastore types.
    /// </summary>
    static class Convert
    {
        public static Value Value(Value x) => x;

        public static FK<T> FK<T>(Value x) where T : class => new FK<T>(x.KeyValue);
        public static Value FK<T>(FK<T> x) where T : class => x.Key;

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
                case DateTimeKind.Local:
                    return x.ToUniversalTime();
                case DateTimeKind.Utc:
                    return x;
                default:
                    return new DateTime(x.Ticks, DateTimeKind.Utc);
                    //throw new ArgumentException("DateTime.Kind must be either local or UTC.");
            }
        }

        public static TimeSpan TimeSpan(Value x) => new TimeSpan(x.IntegerValue);
        public static Value TimeSpan(TimeSpan x) => x.Ticks;

        public static Guid Guid(Value x) => new System.Guid(x.BlobValue.ToByteArray());
        public static Value Guid(Guid x) => x.ToByteArray();

        public static char Char(Value x) => x.StringValue[0];
        public static Value Char(char x) => x.ToString();

        public static Uri Uri(Value x) => x == null ? null : new System.Uri(x.StringValue);
        public static Value Uri(Uri x) => x?.ToString();

        public static ConsoleKeyInfo ConsoleKeyInfo(Value v)
        {
            if (v == null) return default(ConsoleKeyInfo);
            var mod = (ConsoleModifiers)Int32(v.EntityValue[nameof(System.ConsoleKeyInfo.Modifiers)]);
            return new ConsoleKeyInfo(Char(v.EntityValue[nameof(System.ConsoleKeyInfo.KeyChar)]),
                                      (ConsoleKey)Int32(v.EntityValue[nameof(System.ConsoleKeyInfo.Key)]),
                                      mod.HasFlag(ConsoleModifiers.Shift),
                                      mod.HasFlag(ConsoleModifiers.Alt),
                                      mod.HasFlag(ConsoleModifiers.Control));
        }
        public static Value ConsoleKeyInfo(ConsoleKeyInfo v) => new Entity
        {
            [nameof(v.KeyChar)] = Char(v.KeyChar),
            [nameof(v.Key)] = Int32((int)v.Key),
            [nameof(v.Modifiers)] = Int32((int)v.Modifiers),
        };

        public static TimeZoneInfo TimeZoneInfo(Value x) =>
            x == null ? null : System.TimeZoneInfo.FromSerializedString(x.StringValue);
        public static Value TimeZoneInfo(TimeZoneInfo x) => x?.ToSerializedString();

        public static TimeZoneInfo.AdjustmentRule AdjustmentRule(Value x)
        {
            if (x == null) return null;
            var date = (DateTime)x.EntityValue[nameof(System.TimeZoneInfo.AdjustmentRule.DateStart)];
            var dateStart = new DateTime(date.Ticks, DateTimeKind.Unspecified);
            date = (DateTime)x.EntityValue[nameof(System.TimeZoneInfo.AdjustmentRule.DateEnd)];
            var dateEnd = new DateTime(date.Ticks, DateTimeKind.Unspecified);
            return System.TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                dateStart, dateEnd,
                TimeSpan(x.EntityValue[nameof(System.TimeZoneInfo.AdjustmentRule.DaylightDelta)]),
                TransitionTime(x.EntityValue[nameof(System.TimeZoneInfo.AdjustmentRule.DaylightTransitionStart)]),
                TransitionTime(x.EntityValue[nameof(System.TimeZoneInfo.AdjustmentRule.DaylightTransitionEnd)]));
        }
        public static Value AdjustmentRule(TimeZoneInfo.AdjustmentRule x) => new Entity
        {
            [nameof(x.DateEnd)] = DateTime(x.DateEnd),
            [nameof(x.DateStart)] = DateTime(x.DateStart),
            [nameof(x.DaylightDelta)] = TimeSpan(x.DaylightDelta),
            [nameof(x.DaylightTransitionEnd)] = TransitionTime(x.DaylightTransitionEnd),
            [nameof(x.DaylightTransitionStart)] = TransitionTime(x.DaylightTransitionStart),
        };

        public static TimeZoneInfo.TransitionTime TransitionTime(Value v)
        {
            if (v == null) return default(TimeZoneInfo.TransitionTime);
            var e = v.EntityValue;
            var isFixed = (bool)e[nameof(System.TimeZoneInfo.TransitionTime.IsFixedDateRule)];
            var timeOfDayUtc = (DateTime)e[nameof(System.TimeZoneInfo.TransitionTime.TimeOfDay)];
            var timeOfDay = new DateTime(timeOfDayUtc.Ticks, DateTimeKind.Unspecified);
            var month = Int32(e[nameof(System.TimeZoneInfo.TransitionTime.Month)]);
            return isFixed
                ? System.TimeZoneInfo.TransitionTime.CreateFixedDateRule(
                    timeOfDay, month, Int32(e[nameof(System.TimeZoneInfo.TransitionTime.Day)]))
                : System.TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                    timeOfDay,
                    month,
                    Int32(e[nameof(System.TimeZoneInfo.TransitionTime.Week)]),
                    (DayOfWeek)Int32(e[nameof(System.TimeZoneInfo.TransitionTime.DayOfWeek)]));
        }
        public static Value TransitionTime(TimeZoneInfo.TransitionTime v) => v.IsFixedDateRule
            ? new Entity
            {
                [nameof(v.Day)] = Int32(v.Day),
                [nameof(v.IsFixedDateRule)] = v.IsFixedDateRule,
                [nameof(v.Month)] = Int32(v.Month),
                [nameof(v.TimeOfDay)] = v.TimeOfDay,
            }
            : new Entity
            {
                [nameof(v.DayOfWeek)] = Int32((int)v.DayOfWeek),
                [nameof(v.IsFixedDateRule)] = v.IsFixedDateRule,
                [nameof(v.Month)] = Int32(v.Month),
                [nameof(v.TimeOfDay)] = DateTime(v.TimeOfDay),
                [nameof(v.Week)] = Int32(v.Week),
            };

        public static System.Type Type(Value x) => x == null ? null : System.Type.GetType(x.StringValue);
        public static Value Type(System.Type x) => x?.AssemblyQualifiedName;

        public static Stream Stream(Value v) => v == null ? null : new MemoryStream((byte[])v);
        public static Value Stream(Stream x) => x == null ? null : global::Google.Protobuf.ByteString.FromStream(x);

        //FIXME: can Values be null or is this a result of a marshalling error in my code?
        public static string String(Value x) => x?.StringValue;
        //public static Value String(string x) => x;

        public static T? Nullable<T>(Value v) where T : struct =>
            v?.IsNull == false ? Value<T>.From(v) : new T?();
        public static Value Nullable<T>(T? v) where T : struct =>
            v == null ? global::Google.Cloud.Datastore.V1.Value.ForNull() : Value<T>.To(v.Value);

        public static T[] Array<T>(Value v) =>
            v?.ArrayValue.Values.Select(Value<T>.From).ToArray();
        public static Value Array<T>(T[] v) => v?.Select(Value<T>.To).ToArray();

        public static ArraySegment<T> ArraySegment<T>(Value v) =>
            v == null ? default(ArraySegment<T>) :
                        new ArraySegment<T>(Array<T>(v.EntityValue[nameof(System.ArraySegment<T>.Array)]),
                                            Int32(v.EntityValue[nameof(System.ArraySegment<T>.Offset)]),
                                            Int32(v.EntityValue[nameof(System.ArraySegment<T>.Count)]));
        public static Value ArraySegment<T>(ArraySegment<T> v) =>
            v.Array == null ? null : new Entity
            {
                [nameof(v.Array)] = Array<T>(v.Array),
                [nameof(v.Offset)] = Int32(v.Offset),
                [nameof(v.Count)] = Int32(v.Count),
            };

        public static T EntityValue<T>(Value v) =>
            v == null ? default(T) : Entity<T>.From(Entity<T>.Create(), v.EntityValue);
        public static Value EntityValue<T>(T v) =>
            v == null ? null : Entity<T>.To(new Entity(), v);

        #region Collection conversions

        public static IEnumerable<T> IEnumerable<T>(Value v) =>
            v?.ArrayValue.Values.Select(Value<T>.From);
        public static Value IEnumerable<T>(IEnumerable<T> v) =>
            v?.Select(Value<T>.To).ToArray();

        public static List<T> List<T>(Value v) =>
            v?.ArrayValue.Values.Select(Value<T>.From).ToList();
        public static Value List<T>(List<T> v) =>
            v?.Select(Value<T>.To).ToArray();

        public static IList<T> IList<T>(Value v) =>
            v?.ArrayValue.Values.Select(Value<T>.From).ToList();
        public static Value IList<T>(IList<T> v) =>
            v?.Select(Value<T>.To).ToArray();

        public static KeyValuePair<TKey, TValue> KeyValuePair<TKey, TValue>(Value v)
        {
            if (v == null)
                throw new ArgumentNullException("value");
            var kv = v.ArrayValue;
            if (kv.Values.Count != 2) throw new InvalidDataException($"Deserializing to {typeof(KeyValuePair<TKey, TValue>)} requires a 2-element array but found a {kv.Values.Count}-element array.");
            return new KeyValuePair<TKey, TValue>(Value<TKey>.From(kv.Values[0]), Value<TValue>.From(kv.Values[1]));
        }
        public static Value KeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> v) =>
            new[] { Value<TKey>.To(v.Key), Value<TValue>.To(v.Value) };

        public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>(Value v) =>
            v?.ArrayValue.Values.Select(Value<KeyValuePair<TKey, TValue>>.From).ToDictionary(x => x.Key, x => x.Value);
        public static Value Dictionary<TKey, TValue>(Dictionary<TKey, TValue> v) =>
            v?.Select(Value<KeyValuePair<TKey, TValue>>.To).ToArray();

        //FIXME: add stack, set, queue, all collections under System.Collections.Generic? Or figure out
        //a way to dispatch to the underlying interfaces

        #endregion

        #region Tuple conversions
        //FIXME: add overloads for Tuple<*> and ValueTuple<*>
        public static Tuple<T0, T1> Tuple<T0, T1>(Value x) =>
            System.Tuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.Tuple<T0, T1>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.Tuple<T0, T1>.Item2)]));
        public static Value Tuple<T0, T1>(Tuple<T0, T1> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
            };

        public static Tuple<T0, T1, T2> Tuple<T0, T1, T2>(Value x) =>
            System.Tuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2>.Item2)]),
                Value<T2>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2>.Item3)]));
        public static Value Tuple<T0, T1, T2>(Tuple<T0, T1, T2> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
                [nameof(x.Item3)] = Value<T2>.To(x.Item3),
            };

        public static Tuple<T0, T1, T2, T3> Tuple<T0, T1, T2, T3>(Value x) =>
            System.Tuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2, T3>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2, T3>.Item2)]),
                Value<T2>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2, T3>.Item3)]),
                Value<T3>.From(x.EntityValue[nameof(System.Tuple<T0, T1, T2, T3>.Item4)]));
        public static Value Tuple<T0, T1, T2, T3>(Tuple<T0, T1, T2, T3> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
                [nameof(x.Item3)] = Value<T2>.To(x.Item3),
                [nameof(x.Item4)] = Value<T3>.To(x.Item4),
            };

        public static ValueTuple<T0, T1> ValueTuple<T0, T1>(Value x) =>
            System.ValueTuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1>.Item2)]));
        public static Value ValueTuple<T0, T1>(ValueTuple<T0, T1> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
            };

        public static ValueTuple<T0, T1, T2> ValueTuple<T0, T1, T2>(Value x) =>
            System.ValueTuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2>.Item2)]),
                Value<T2>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2>.Item3)]));
        public static Value ValueTuple<T0, T1, T2>(ValueTuple<T0, T1, T2> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
                [nameof(x.Item3)] = Value<T2>.To(x.Item3),
            };

        public static ValueTuple<T0, T1, T2, T3> ValueTuple<T0, T1, T2, T3>(Value x) =>
            System.ValueTuple.Create(
                Value<T0>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2, T3>.Item1)]),
                Value<T1>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2, T3>.Item2)]),
                Value<T2>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2, T3>.Item3)]),
                Value<T3>.From(x.EntityValue[nameof(System.ValueTuple<T0, T1, T2, T3>.Item4)]));
        public static Value ValueTuple<T0, T1, T2, T3>(ValueTuple<T0, T1, T2, T3> x) =>
            new Entity
            {
                [nameof(x.Item1)] = Value<T0>.To(x.Item1),
                [nameof(x.Item2)] = Value<T1>.To(x.Item2),
                [nameof(x.Item3)] = Value<T2>.To(x.Item3),
                [nameof(x.Item4)] = Value<T3>.To(x.Item4),
            };

        #endregion

        #region Google type conversions

        public static G.Color Color(Value x) =>
            x == null ? null : new G.Color
            {
                Red = Single(x.EntityValue[nameof(G.Color.Red)]),
                Green = Single(x.EntityValue[nameof(G.Color.Green)]),
                Blue = Single(x.EntityValue[nameof(G.Color.Blue)]),
                Alpha = Nullable<float>(x.EntityValue[nameof(G.Color.Alpha)]),
            };
        public static Value Color(G.Color x) => new Entity
        {
            [nameof(x.Red)] = Single(x.Red),
            [nameof(x.Green)] = Single(x.Green),
            [nameof(x.Blue)] = Single(x.Blue),
            [nameof(x.Alpha)] = Nullable(x.Alpha),
        };

        public static G.Date Date(Value x) => new G.Date
        {
            Year = Int32(x.EntityValue[nameof(G.Date.Year)]),
            Month = Int32(x.EntityValue[nameof(G.Date.Month)]),
            Day = Int32(x.EntityValue[nameof(G.Date.Day)]),
        };
        public static Value Date(G.Date x) => new Entity
        {
            [nameof(x.Year)] = Int32(x.Year),
            [nameof(x.Month)] = Int32(x.Month),
            [nameof(x.Day)] = Int32(x.Day),
        };

        public static G.Money Money(Value x) => new G.Money
        {
            CurrencyCode = String(x.EntityValue[nameof(G.Money.CurrencyCode)]),
            Units = x.EntityValue[nameof(G.Money.Units)].IntegerValue,
            Nanos = Int32(x.EntityValue[nameof(G.Money.Nanos)]),
        };
        public static Value Money(G.Money x) => new Entity
        {
            [nameof(x.CurrencyCode)] = x.CurrencyCode,
            [nameof(x.Units)] = x.Units,
            [nameof(x.Nanos)] = Int32(x.Nanos),
        };

        public static G.PostalAddress PostalAddress(Value x)
        {
            var addr = new G.PostalAddress
            {
                Sublocality = String(x.EntityValue[nameof(G.PostalAddress.Sublocality)]),
                Locality = String(x.EntityValue[nameof(G.PostalAddress.Locality)]),
                AdministrativeArea = String(x.EntityValue[nameof(G.PostalAddress.AdministrativeArea)]),
                SortingCode = String(x.EntityValue[nameof(G.PostalAddress.SortingCode)]),
                PostalCode = String(x.EntityValue[nameof(G.PostalAddress.PostalCode)]),
                LanguageCode = String(x.EntityValue[nameof(G.PostalAddress.LanguageCode)]),
                RegionCode = String(x.EntityValue[nameof(G.PostalAddress.RegionCode)]),
                Organization = String(x.EntityValue[nameof(G.PostalAddress.Organization)]),
                Revision = Int32(x.EntityValue[nameof(G.PostalAddress.Revision)]),
            };
            addr.AddressLines.AddRange(IEnumerable<string>(x.EntityValue[nameof(G.PostalAddress.AddressLines)]));
            addr.Recipients.AddRange(IEnumerable<string>(x.EntityValue[nameof(G.PostalAddress.Recipients)]));
            return addr;
        }
        public static Value PostalAddress(G.PostalAddress x) => new Entity
        {
            [nameof(x.AddressLines)] = IEnumerable(x.AddressLines),
            [nameof(x.Recipients)] = IEnumerable(x.Recipients),
            [nameof(x.Sublocality)] = String(x.Sublocality),
            [nameof(x.Locality)] = String(x.Locality),
            [nameof(x.AdministrativeArea)] = String(x.AdministrativeArea),
            [nameof(x.SortingCode)] = String(x.SortingCode),
            [nameof(x.PostalCode)] = String(x.PostalCode),
            [nameof(x.LanguageCode)] = String(x.LanguageCode),
            [nameof(x.RegionCode)] = String(x.RegionCode),
            [nameof(x.Organization)] = String(x.Organization),
            [nameof(x.Revision)] = Int32(x.Revision),
        };

        public static G.TimeOfDay TimeOfDay(Value x) => new G.TimeOfDay
        {
            Hours = Int32(x.EntityValue[nameof(G.TimeOfDay.Hours)]),
            Minutes = Int32(x.EntityValue[nameof(G.TimeOfDay.Minutes)]),
            Seconds = Int32(x.EntityValue[nameof(G.TimeOfDay.Seconds)]),
            Nanos = Int32(x.EntityValue[nameof(G.TimeOfDay.Nanos)]),
        };
        public static Value TimeOfDay(G.TimeOfDay x) => new Entity
        {
            [nameof(x.Hours)] = Int32(x.Hours),
            [nameof(x.Minutes)] = Int32(x.Minutes),
            [nameof(x.Seconds)] = Int32(x.Seconds),
            [nameof(x.Nanos)] = Int32(x.Nanos),
        };

        #endregion
    }
}
