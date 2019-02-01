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
    /// Open access delegates for struct getters.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    delegate T1 VFunc<T0, T1>(ref T0 obj);

    /// <summary>
    /// Open access delegates for struct setters.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="arg0"></param>
    /// <param name="arg1"></param>
    delegate void VAction<T0, T1>(ref T0 arg0, T1 arg1);
    
    /// <summary>
    /// Generates mapping delegates without any runtime code generation.
    /// </summary>
    public class PropertyMapper : IEntityMapper
    {
        static readonly MethodInfo mapClass = typeof(PropertyMapper).GetMethod(nameof(PropertyMapper.Map));
        static readonly MethodInfo mapStruct = typeof(PropertyMapper).GetMethod(nameof(PropertyMapper.MapStruct));

        /// <inheritdoc />
        public Func<T> Map<T>(out Func<T, Key> getKey, out Action<T, Key> setKey, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
        {
            // read/write entities and values using delegates generated from method-backed properties
            if (typeof(T).IsValueType)
                MapStruct<T>(out getKey, out setKey, out From, out To);
            else
                MapClass<T>(out getKey, out setKey, out From, out To);
            return () => Activator.CreateInstance<T>();
        }

        void MapClass<T>(out Func<T, Key> getKey, out Action<T, Key> setKey, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
        {
            var objType = typeof(T);
            var sset = new Func<string, Action<T, int>, Action<Entity, T>>(ClassSet).Method.GetGenericMethodDefinition();
            var sget = new Func<string, Func<T, int>, Action<T, Entity>>(ClassGet).Method.GetGenericMethodDefinition();
            var oparams = new object[3];
            var gk = getKey = null;
            var sk = setKey = null;
            var members = objType.GetProperties();
            var from = new Action<Entity, T>[members.Length];
            var to = new Action<T, Entity>[members.Length];
            int propCount = 0;  // this indexes the valid properties
            for (int i = 0; i < members.Length; ++i)
            {
                var member = members[i];
                // extract delegates from Value<TProperty>.From/To for each member of T
                if (member.GetCustomAttribute<KeyAttribute>() != null)
                {
                    //FIXME: I think API supports string keys
                    if (member.PropertyType != typeof(long))
                        throw new NotSupportedException($"{member.PropertyType} is not a supported key type. Supported types are: System.Int64.");
                    if (gk != null)
                        throw new NotSupportedException($"Duplicate [Key] property {member.DeclaringType}.{member.Name}");
                    var kparams = new object[1];
                    kparams[0] = member.GetSetMethod()?.CreateDelegate(typeof(Action<,>).MakeGenericType(objType, typeof(long)));
                    sk = setKey = (Action<T, Key>)typeof(PropertyMapper)
                        .GetMethod(nameof(SetKey), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(objType)
                        .Invoke(null, kparams);
                    kparams[0] = member.GetGetMethod()?.CreateDelegate(typeof(Func<,>).MakeGenericType(objType, typeof(long)));
                    gk = getKey = (Func<T, Key>)typeof(PropertyMapper)
                        .GetMethod(nameof(GetKey), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(objType)
                        .Invoke(null, kparams);
                }
                else if (member.GetCustomAttribute<IgnoreDatastoreAttributeAttribute>() == null)
                {
                    // this is a reference type, so accumulate a list of getters/setters for the type's members
                    //FIXME: add support for overridable field members via attributes
                    var tset = typeof(Action<,>).MakeGenericType(objType, member.PropertyType);
                    from[propCount] = (Action<Entity, T>)sset
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetSetMethod().CreateDelegate(tset) });

                    var tget = typeof(Func<,>).MakeGenericType(objType, member.PropertyType);
                    to[propCount++] = (Action<T, Entity>)sget
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetGetMethod().CreateDelegate(tget) });
                }
            }
            //FIXME: should probably remove this check, probably move it to Mapper.cs as precondition to
            //each insert/lookup/delete op
            if (gk == null || sk == null)
                throw new MissingMemberException($"{objType.FullName} is missing a property with a [Key] attribute.");
            From = (obj, e) =>
            {
                if (obj == null) throw new ArgumentNullException("entity");
                if (e == null) return default(T);
                //FIXME: should require non-null keys? Probably not since this will be extended for structs.
                sk(obj, e.Key);
                for (int i = 0; i < propCount; ++i)
                    from[i](e, obj);
                return obj;
            };
            To = (e, obj) =>
            {
                if (e == null) throw new ArgumentNullException("entity");
                if (obj == null) return e;
                //FIXME: should require non-null keys? Probably not since this will be extended for structs.
                e.Key = gk(obj);
                for (int i = 0; i < propCount; ++i)
                    to[i](obj, e);
                return e;
            };
        }

        void MapStruct<T>(out Func<T, Key> getKey, out Action<T, Key> setKey, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
        {
            var objType = typeof(T);
            var sset = new Func<string, VAction<T, int>, VAction<T, Entity>>(StructSet).Method.GetGenericMethodDefinition();
            var sget = new Func<string, VFunc<T, int>, VAction<T, Entity>>(StructGet).Method.GetGenericMethodDefinition();
            var members = objType.GetProperties();
            var from = new VAction<T, Entity>[members.Length];
            var to = new VAction<T, Entity>[members.Length];
            // value types can't have keys
            getKey = null;
            setKey = null;
            int propCount = 0;
            for (int i = 0; i < members.Length; ++i)
            {
                // this is a reference type, so accumulate a list of getters/setters for the type's members
                //FIXME: add support for overridable field members via attributes
                var member = members[i];
                if (member.GetCustomAttribute<IgnoreDatastoreAttributeAttribute>() == null)
                {
                    var tset = typeof(VAction<,>).MakeGenericType(objType, member.PropertyType);
                    from[propCount] = (VAction<T, Entity>)sset
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetSetMethod().CreateDelegate(tset) });

                    var tget = typeof(VFunc<,>).MakeGenericType(objType, member.PropertyType);
                    to[propCount++] = (VAction<T, Entity>)sget
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetGetMethod().CreateDelegate(tget) });
                }
            }
            From = (obj, e) =>
            {
                if (e == null) return default(T);
                for (var i = 0; i < propCount; ++i)
                    from[i](ref obj, e);
                return obj;
            };
            To = (e, obj) =>
            {
                if (e == null) throw new ArgumentNullException("entity");
                for (var i = 0; i < propCount; ++i)
                    to[i](ref obj, e);
                return e;
            };
        }

        #region Key getters/setters
        static Func<T, Key> GetKey<T>(Func<T, long> getKey) where T : class
        {
            if (getKey == null) throw new MissingMemberException($"{typeof(T).FullName} is missing a getter with a [Key] attribute.");
            return (obj) =>
            {
                var id = getKey(obj);
                return id == 0 ? Mapper.CreateIncompleteKey<T>():
                                 id.ToKey<T>();
            };
        }

        static Action<T, Key> SetKey<T>(Action<T, long> setKey) where T : class
        {
            if (setKey == null) throw new MissingMemberException($"{typeof(T).FullName} is missing a setter with a [Key] attribute.");
            return (obj, key) => setKey(obj, key.Id());
        }
        #endregion

        #region Property getter/setters
        static Action<Entity, T> ClassSet<T, TField>(string name, Action<T, TField> setter) =>
            (e, obj) => setter(obj, Value<TField>.From(e[name]));

        static Action<T, Entity> ClassGet<T, TField>(string name, Func<T, TField> getter) =>
            (obj, e) => e[name] = Value<TField>.To(getter(obj));

        static VAction<T, Entity> StructSet<T, TField>(string name, VAction<T, TField> setter) =>
            (ref T obj, Entity e) => setter(ref obj, Value<TField>.From(e[name]));

        static VAction<T, Entity> StructGet<T, TField>(string name, VFunc<T, TField> getter) =>
            (ref T obj, Entity e) => e[name] = Value<TField>.To(getter(ref obj));
        #endregion
    }
}
