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
    /// <typeparam name="T">The type of entity designated by this reference.</typeparam>
    public sealed class FK<T>
        where T : class
    {
        T value;
        Key key;
        
        /// <summary>
        /// Construct a new foreign key reference.
        /// </summary>
        /// <param name="key">The foreign entity's key.</param>
        public FK(Key key)
        {
            this.Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Construct a new foreign key reference.
        /// </summary>
        /// <param name="key">The foreign entity's key.</param>
        public FK(T value)
        {
            if (Entity<T>.GetKey == null)
                throw new ArgumentException($"Type {typeof(T).Name} does not have a property decorated with [Key].");
            this.value = value;
        }

        /// <summary>
        /// The key that designates the entity.
        /// </summary>
        public Key Key
        {
            // return assigned key or dynamically generate a key if the value is not null
            get => key ?? (value == null ? null : Entity<T>.GetKey(value));
            private set => key = value;
        }

        /// <summary>
        /// The entity referenced by this key.
        /// </summary>
        public T Value
        {
            // return the cached value if it's loaded or if no key is present
            get => key != null && this.value == null
                ? throw new InvalidOperationException("Entity is not loaded.")
                : this.value;
            set
            {
                // reset the stored key reference so it's re-saved with the new entity's key
                if (key != null)
                    key = null;
                this.value = value;
            }
        }

        /// <summary>
        /// Lookup the designated reference.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="readConsistency"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public T Get(DatastoreDb db, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null) =>
            value ?? (value = db.Lookup<T>(Key, null, readConsistency, callSettings));

        /// <summary>
        /// Lookup the designated reference.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="readConsistency"></param>
        /// <param name="callSettings"></param>
        /// <returns></returns>
        public async Task<T> GetAsync(DatastoreDb db, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null) =>
            value ?? (value = await db.LookupAsync<T>(Key, null, readConsistency, callSettings));
    }
}
