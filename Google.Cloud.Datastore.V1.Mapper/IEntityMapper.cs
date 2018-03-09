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
        void Map<T>(out Func<Entity, T, T> from, out Func<T, Entity, Entity> to)
            where T : class;

        //FIXME: may need to add a HashMap<T> as a parameter in order to handle circular references?
    }
}
