using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Datastore.V1;
using System.Runtime.Serialization;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// A delegate representing a struct setter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TField"></typeparam>
    /// <param name="obj"></param>
    /// <param name="value"></param>
    delegate void Setter<T, TField>(ref T obj, TField value) where T : struct;

    /// <summary>
    /// A delegate representing a struct getter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TField"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    delegate TField Getter<T, TField>(ref T obj) where T : struct;

    /// <summary>
    /// Generates mapping delegates without any runtime code generation.
    /// </summary>
    public class PropertyMapper : IEntityMapper
    {
        static readonly MethodInfo map = typeof(PropertyMapper).GetMethod(nameof(PropertyMapper.Map));

        /// <inheritdoc />
        public Func<T> Map<T>(string prefix, out Func<T, Key> getKey, out Action<T, Key> setKey, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
        {
            // read/write entities and values using delegates generated from method-backed properties
            var objType = typeof(T);
            var from = new List<Action<Entity, T>>();
            var to = new List<Action<T, Entity>>();
            var sset = new Func<string, Action<T, int>, Action<Entity, T>>(Set).GetMethodInfo().GetGenericMethodDefinition();
            var sget = new Func<string, Func<T, int>, Action<T, Entity>>(Get).GetMethodInfo().GetGenericMethodDefinition();
            var oparams = new object[3];
            var gk = getKey = null;
            var sk = setKey = null;
            foreach (var member in typeof(T).GetProperties())
            {
                //FIXME: can generalize this to work with value types? The value type must be inlined 
                //into the entity using the 'prefix' parameter. Here we must detect whether the value
                //type has a Value<T>.From/To, and if not, we use entity deserialization.

                // extract delegates from Value<TProperty>.From/To for each member of T
                var vals = typeof(Value<>).MakeGenericType(member.PropertyType);
                var f = vals.GetProperty(nameof(Value<object>.From), BindingFlags.Static | BindingFlags.Public);
                if (member.GetCustomAttribute<KeyAttribute>() != null)
                {
                    //FIXME: I think API supports string keys
                    if (member.PropertyType != typeof(long))
                        throw new NotSupportedException($"{member.PropertyType} is not a supported key type. Supported types are: System.Int64.");
                    if (getKey != null)
                        throw new NotSupportedException($"Duplicate [Key] property {member.DeclaringType}.{member.Name}");
                    var kparams = new object[1];
                    kparams[0] = member.GetSetMethod().CreateDelegate(typeof(Action<,>).MakeGenericType(objType, typeof(long)));
                    sk = setKey = (Action<T, Key>)typeof(PropertyMapper)
                        .GetMethod(nameof(SetKey), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(objType)
                        .Invoke(null, kparams);
                    kparams[0] = member.GetGetMethod().CreateDelegate(typeof(Func<,>).MakeGenericType(objType, typeof(long)));
                    gk = getKey = (Func<T, Key>)typeof(PropertyMapper)
                        .GetMethod(nameof(GetKey), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(objType)
                        .Invoke(null, kparams);
                }
                else if (f.GetValue(null) == null && f.PropertyType.GetTypeInfo().IsValueType)
                {
                    oparams[0] = f.Name + "_";
                    map.MakeGenericMethod(f.PropertyType).Invoke(null, oparams);
                }
                else
                {
                    var tset = typeof(Action<,>).MakeGenericType(objType, member.PropertyType);
                    from.Add((Action<Entity, T>)sset.MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { prefix + member.Name, member.GetSetMethod().CreateDelegate(tset) }));

                    var t = vals.GetProperty("To", BindingFlags.Static | BindingFlags.Public);
                    var tget = typeof(Func<,>).MakeGenericType(objType, member.PropertyType);
                    to.Add((Action<T, Entity>)sget.MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { prefix + member.Name, member.GetGetMethod().CreateDelegate(tget) }));
                }
            }
            if (gk == null || sk == null)
                throw new MissingMemberException($"{objType.FullName} is missing a property with a [Key] attribute.");
            From = (obj, e) =>
            {
                if (obj == null) throw new ArgumentNullException("entity");
                if (e == null) return obj;
                //FIXME: should require non-null keys? Probably not since this will be extended for structs.
                sk?.Invoke(obj, e.Key);
                for (int i = 0; i < from.Count; ++i)
                    from[i](e, obj);
                return obj;
            };
            To = (e, obj) =>
            {
                if (e == null) throw new ArgumentNullException("entity");
                if (obj == null) return e;
                //FIXME: should require non-null keys? Probably not since this will be extended for structs.
                e.Key = gk?.Invoke(obj);// ?? Mapper.CreateIncompleteKey<T>();
                for (int i = 0; i < to.Count; ++i)
                    to[i](obj, e);
                return e;
            };
            return () => Activator.CreateInstance<T>();
        }

        #region Key getters/setters
        static Func<T, Key> GetKey<T>(Func<T, long> getKey) where T : class =>
            (obj) =>
            {
                var id = getKey(obj);
                return id == 0 ? Mapper.CreateIncompleteKey<T>():
                                 id.ToKey<T>();
            };

        static Action<T, Key> SetKey<T>(Action<T, long> setKey) where T : class =>
            (obj, key) => setKey(obj, key.Id());
        #endregion

        #region Property getter/setters
        static Action<Entity, T> Set<T, TField>(string name, Action<T, TField> setter) =>
            (e, obj) => setter(obj, Value<TField>.From(e[name]));

        static Action<T, Entity> Get<T, TField>(string name, Func<T, TField> getter) =>
            (obj, e) => e[name] = Value<TField>.To(getter(obj));

        //FUTURE: this may be less efficient than it could be.
        static Action<Entity, T> SetStruct<T, TField>(string name, Getter<T, TField> getter, Setter<T, TField> setter, Func<TField, Entity, TField> map)
            where T : struct =>
            (e, obj) => setter(ref obj, map(getter(ref obj), e));

        static Action<T, Entity> GetStruct<T, TField>(string name, Getter<T, TField> getter, Func<Entity, TField, Entity> map) where T : struct =>
            (obj, e) => map(e, getter(ref obj));
        #endregion
    }
}
