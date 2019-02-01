using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Datastore.V1;
using Google.Api.Gax.Grpc;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// A foreign key reference.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FK<T>
        where T : class
    {
        T value;

        /// <summary>
        /// The key that designates the entity.
        /// </summary>
        public Key Key { get; private set; }

        /// <summary>
        /// Lookup the designated reference.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="readConsistency"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public T Lookup(DatastoreDb db, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null) =>
            value ?? (value = db.Lookup<T>(Key, null, readConsistency, callSettings));

        /// <summary>
        /// Lookup the designated reference.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="readConsistency"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public async Task<T> LookupAsync(DatastoreDb db, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null) =>
            value ?? (value = await db.LookupAsync<T>(Key, null, readConsistency, callSettings));
    }
}
