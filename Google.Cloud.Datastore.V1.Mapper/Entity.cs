using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Google.Cloud.Datastore.V1;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// Map entities to/from values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Entity<T>
        where T : class
    {
        //FIXME: support value types by inlining the properties into the containing
        //entity. This requires passing around a string prefix, indicating the entity
        //value' property name which is concatenated with the value type's property names.
        //Could probably add the string prefix to the IEntityMapper interface and then
        //any value type fields would simply invoke the mapper directly, passing in the 
        //current prefix. This generates all the prefixes only once at load time instead of
        //generating them on the fly at runtime.

        /// <summary>
        /// Update an object with the given entity data.
        /// </summary>
        public static Func<T, Entity, T> From { get; private set; }

        /// <summary>
        /// Update an entity with the given object.
        /// </summary>
        public static Func<Entity, T, Entity> To { get; private set; }

        /// <summary>
        /// Initialize the object's key.
        /// </summary>
        public static Action<T, Key> SetKey { get; private set; }

        /// <summary>
        /// Obtain the object's key.
        /// </summary>
        public static Func<T, Key> GetKey { get; private set; }

        /// <summary>
        /// Parameterless constructor for type <typeparamref name="T"/>.
        /// </summary>
        public static Func<T> Constructor { get; set; }

        /// <summary>
        /// Create an empty instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns></returns>
        public static T Create() => Constructor?.Invoke() ?? Activator.CreateInstance<T>();

        static Entity()
        {
            Mapper.Default.Map<T>("", out var gk, out var sk, out var from, out var to);
            GetKey = gk;
            SetKey = sk;
            From = from;
            To = to;
        }
    }
}