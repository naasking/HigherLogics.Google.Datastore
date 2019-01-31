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
    delegate T1 VFunc<T0, T1>(ref T0 obj) where T0 : struct;

    /// <summary>
    /// Open access delegates for struct setters.
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="arg0"></param>
    /// <param name="arg1"></param>
    delegate void VAction<T0, T1>(ref T0 arg0, T1 arg1) where T0 : struct;
    
    /// <summary>
    /// Generates mapping delegates without any runtime code generation.
    /// </summary>
    public class PropertyMapper : IEntityMapper
    {
        static readonly MethodInfo mapClass = typeof(PropertyMapper).GetMethod(nameof(PropertyMapper.Map));
        static readonly MethodInfo mapStruct = typeof(PropertyMapper).GetMethod(nameof(PropertyMapper.MapStruct));

        /// <inheritdoc />
        public Func<T> Map<T>(out Func<T, Key> getKey, out Action<T, Key> setKey, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
            where T : class
        {
            // read/write entities and values using delegates generated from method-backed properties
            var objType = typeof(T);
            var sset = new Func<string, Action<T, int>, Action<Entity, T>>(Set).GetMethodInfo().GetGenericMethodDefinition();
            var sget = new Func<string, Func<T, int>, Action<T, Entity>>(Get).GetMethodInfo().GetGenericMethodDefinition();
            var oparams = new object[3];
            var gk = getKey = null;
            var sk = setKey = null;
            var members = typeof(T).GetProperties();
            var from = new Action<Entity, T>[members.Length];
            var to = new Action<T, Entity>[members.Length];
            int propCount = 0;  // this indexes the valid properties
            for (int i = 0; i < members.Length; ++i)
            {
                var member = members[i];
                // extract delegates from Value<TProperty>.From/To for each member of T
                var vals = typeof(Value<>).MakeGenericType(member.PropertyType);
                var f = vals.GetProperty(nameof(Value<object>.From), BindingFlags.Static | BindingFlags.Public);
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
                else if (f.GetValue(null) == null && f.PropertyType.GetTypeInfo().IsValueType)
                {
                    //FIXME: call MapStruct for a struct property that has no Value conversion. This returns a set
                    //of delegates which have to be wrapped by GetStruct/SetStruct. I think this will require
                    //restructuring this loop because the delegate signatures for structs require a ref as first
                    //parameter so the current delegate arrays are insufficient.
                    oparams[0] = f.Name + "_";
                    mapStruct.MakeGenericMethod(f.PropertyType).Invoke(null, oparams);
                }
                else
                {
                    // this is a reference type, so accumulate a list of getters/setters for the type's members
                    //FIXME: add support for overridable field members via attributes
                    var tset = typeof(Action<,>).MakeGenericType(objType, member.PropertyType);
                    from[propCount] = (Action<Entity, T>)sset
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetSetMethod().CreateDelegate(tset) });

                    var t = vals.GetProperty("To", BindingFlags.Static | BindingFlags.Public);
                    var tget = typeof(Func<,>).MakeGenericType(objType, member.PropertyType);
                    to[propCount++] = (Action<T, Entity>)sget
                        .MakeGenericMethod(objType, member.PropertyType)
                        .Invoke(null, new object[] { member.Name, member.GetGetMethod().CreateDelegate(tget) });
                }
            }
            if (gk == null || sk == null)
                throw new MissingMemberException($"{objType.FullName} is missing a property with a [Key] attribute.");
            From = (obj, e) =>
            {
                if (obj == null) throw new ArgumentNullException("entity");
                if (e == null) return obj;
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
            return () => Activator.CreateInstance<T>();
        }

        public void MapStruct<T>(string prefix, out Func<T, Entity, T> From, out Func<Entity, T, Entity> To)
            where T : struct
        {
            var members = typeof(T).GetProperties();
            var from = new VAction<T, Entity>[members.Length];
            var to = new VAction<T, Entity>[members.Length];
            From = null;
            To = null;
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
        static Action<Entity, T> Set<T, TField>(string name, Action<T, TField> setter) =>
            (e, obj) => setter(obj, Value<TField>.From(e[name]));

        static Action<T, Entity> Get<T, TField>(string name, Func<T, TField> getter) =>
            (obj, e) => e[name] = Value<TField>.To(getter(obj));

        //FUTURE: this may be less efficient than it could be.
        //FIXME: duplicate Entity<T>.To/From/Create but for value types. Then we can eliminate
        //this map parameter which would recursive reflection calls to MapStruct.
        static VAction<T, Entity> SetStructStruct<T, TField>(string name, VAction<T, TField> setter, Func<TField, Entity, TField> map)
            where T : struct
            where TField : struct =>
            (ref T obj, Entity e) => setter(ref obj, map(default(TField), e));

        static VAction<T, Entity> SetStructClass<T, TField>(string name, VAction<T, TField> setter)
            where T : struct
            where TField : class =>
            (ref T obj, Entity e) => setter(ref obj, Entity<TField>.From(Entity<TField>.Create(), e));

        static VAction<T, Entity> GetStruct<T, TField>(string name, VFunc<T, TField> getter, Func<Entity, TField, Entity> map) where T : struct =>
            (ref T obj, Entity e) => map(e, getter(ref obj));
        #endregion
    }
}
