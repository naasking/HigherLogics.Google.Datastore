using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Google.Cloud.Datastore.V1;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// Map entities to/from values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    static class Entity<T>
        where T : class
    {
        /// <summary>
        /// The kind used to find entities in the datastore.
        /// </summary>
        public static string Kind { get; internal set; } = typeof(T).FullName;

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
        public static Func<T> Create { get; set; }

        static Entity()
        {
            // the string prefix is to recursively handle value types as inlined properties
            Create = Mapper.Default.Map<T>(out var gk, out var sk, out var from, out var to);
            GetKey = gk;
            SetKey = sk;
            From = from;
            To = to;
        }
    }
}