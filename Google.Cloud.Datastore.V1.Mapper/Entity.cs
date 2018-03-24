﻿using System;
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
        /// <summary>
        /// Update a value with the given entity.
        /// </summary>
        public static Func<T, Entity, T> From { get; private set; }

        /// <summary>
        /// Update an entity with the given value.
        /// </summary>
        public static Func<Entity, T, Entity> To { get; private set; }

        //public static Func<T> Create { get; private set; }

        static Entity()
        {
            Mapper.Default.Map<T>(out var from, out var to);
            From = from;
            To = to;
        }
    }
}