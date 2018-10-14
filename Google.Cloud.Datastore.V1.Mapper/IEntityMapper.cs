using System;
using System.Collections.Generic;
using System.Text;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// The contract used to map entities to values.
    /// </summary>
    public interface IEntityMapper
    {
        /// <summary>
        /// Generate delegates to marshal values to/from entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <param name="to"></param>
        void Map<T>(out Func<T, Entity, T> from, out Func<Entity, T, Entity> to)
            where T : class;

        //FIXME: may need to add a dictionary/hashtable as a parameter to each delegate
        //in order to handle circular references
    }
}
