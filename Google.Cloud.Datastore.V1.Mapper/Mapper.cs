using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax.Grpc;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// The configuration options
    /// </summary>
    public static class Mapper
    {
        /// <summary>
        /// The mapper used to marshal values and entities.
        /// </summary>
        public static IEntityMapper Default { get; set; } = new PropertyMapper();

        /// <summary>
        /// Create a KeyFactory using the type name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <returns></returns>
        public static KeyFactory CreateKeyFactory<T>(this DatastoreDb db)
        {
            return db.CreateKeyFactory(typeof(T).FullName);
        }

        #region Lookup extensions on entities
        /// <summary>
        /// Looks up a single entity by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="entity">The entity to fill with the retrieved data.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>The entity with the specified key, or null if no such entity exists.</returns>
        public static T Lookup<T>(this DatastoreDb db, T entity, Key key, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            return Entity<T>.From(entity, db.Lookup(key, readConsistency, callSettings));
        }

        /// <summary>
        /// Looks up a single entity by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="entity">The entity to fill with the retrieved data.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>The entity with the specified key, or null if no such entity exists.</returns>
        public static async Task<T> LookupAsync<T>(this DatastoreDb db, T entity, Key key, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            return Entity<T>.From(entity, await db.LookupAsync(key, readConsistency, callSettings));
        }

        /// <summary>
        /// Looks up a collection of entities by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The keys to lookup.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <param name="create">A constructor for creating fresh instances of <typeparamref name="T"/>.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static IReadOnlyList<T> Lookup<T>(this DatastoreDb db, Func<T> create, IEnumerable<Key> keys, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            if (create == null) throw new ArgumentNullException(nameof(create));
            return db.Lookup(keys, readConsistency, callSettings)
                     .Select(e => e == null ? default(T) : Entity<T>.From(create(), e))
                     .ToList();
        }

        /// <summary>
        /// Looks up a collection of entities by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The keys to lookup.</param>
        /// <param name="create">A constructor for creating fresh instances of <typeparamref name="T"/>.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static IReadOnlyList<T> Lookup<T>(this DatastoreDb db, Func<T> create, params Key[] keys)
            where T : class
        {
            return db.Lookup(create, keys, null, null);
        }

        /// <summary>
        /// Looks up a collection of entities by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The key to lookup.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <param name="create">A constructor for creating fresh instances of <typeparamref name="T"/>.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static async Task<IReadOnlyList<T>> LookupAsync<T>(this DatastoreDb db, Func<T> create, IEnumerable<Key> keys, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            if (create == null) throw new ArgumentNullException(nameof(create));
            var entities = await db.LookupAsync(keys, readConsistency, callSettings);
            return entities.Select(e => e == null ? default(T) : Entity<T>.From(create(), e))
                           .ToList();
        }

        /// <summary>
        /// Looks up a collection of entities by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The key to lookup.</param>
        /// <param name="create">A constructor for creating fresh instances of <typeparamref name="T"/>.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static Task<IReadOnlyList<T>> LookupAsync<T>(this DatastoreDb db, Func<T> create, params Key[] keys)
            where T : class
        {
            return db.LookupAsync(create, keys, null, null);
        }
        #endregion

        #region Insert extensions on entities
        /// <summary>
        /// Inserts a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to insert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// The key of the inserted entity if it was allocated by the server, or null
        /// if the inserted entity had a predefined key.
        /// </returns>
        public static Key Insert<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            var e = new Entity { Key = key };
            return db.Insert(Entity<T>.To(e, entity), callSettings);
        }

        /// <summary>
        /// Inserts a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to insert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// The key of the inserted entity if it was allocated by the server, or null
        /// if the inserted entity had a predefined key.
        /// </returns>
        public static Task<Key> InsertAsync<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            var e = new Entity { Key = key };
            return db.InsertAsync(Entity<T>.To(e, entity), callSettings);
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to insert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A collection of keys of inserted entities, in the same order as entities. Only
        /// keys allocated by the server will be returned; any entity with a predefined key
        /// will have a null value in the collection.
        ///</returns>
        public static IReadOnlyList<Key> Insert<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.Insert(entities.Select(x => Entity<T>.To(new Entity
            {
                Key = getKey(x),
            }, x)), callSettings);
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally asn asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to insert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The result of the task is a collection
        /// of keys of inserted entities, in the same order as entities. Only keys allocated
        /// by the server will be returned; any entity with a predefined key will have a
        /// null value in the collection.
        ///</returns>
        public static Task<IReadOnlyList<Key>> InsertAsync<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.InsertAsync(entities.Select(x => Entity<T>.To(new Entity
            {
                Key = getKey(x)
            }, x)), callSettings);
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to insert. Must not be null or contain null entries.</param>
        /// <returns>
        /// A collection of keys of inserted entities, in the same order as entities. Only
        /// keys allocated by the server will be returned; any entity with a predefined key
        /// will have a null value in the collection.
        ///</returns>
        public static IReadOnlyList<Key> Insert<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            return db.Insert<T>(entities, getKey, null);
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally asn asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to insert. Must not be null or contain null entries.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The result of the task is a collection
        /// of keys of inserted entities, in the same order as entities. Only keys allocated
        /// by the server will be returned; any entity with a predefined key will have a
        /// null value in the collection.
        ///</returns>
        public static Task<IReadOnlyList<Key>> InsertAsync<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            return db.InsertAsync<T>(entities, getKey, null);
        }
        #endregion

        #region Delete extensions on entities
        /// <summary>
        /// Deletes a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to delete. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to RPC calls.</param>
        public static void Delete<T>(this DatastoreDb db, T entity, CallSettings callSettings = null)
            where T : class
        {
            db.Delete(Entity<T>.To(new Entity(), entity), callSettings);
        }

        /// <summary>
        /// Deletes a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to delete. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to RPC calls.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task DeleteAsync<T>(this DatastoreDb db, T entity, CallSettings callSettings = null)
            where T : class
        {
            return db.DeleteAsync(Entity<T>.To(new Entity(), entity), callSettings);
        }

        /// <summary>
        /// Deletes a collection of keys, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to delete. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to RPC calls.</param>
        public static void Delete<T>(this DatastoreDb db, IEnumerable<T> entities, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            db.Delete(entities.Select(x => Entity<T>.To(new Entity(), x)), callSettings);
        }

        /// <summary>
        /// Deletes a collection of keys, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to delete. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to RPC calls.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task DeleteAsync<T>(this DatastoreDb db, IEnumerable<T> entities, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.DeleteAsync(entities.Select(x => Entity<T>.To(new Entity(), x)), callSettings);
        }

        /// <summary>
        /// Deletes a collection of keys, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to delete. Must not be null or contain null entries.</param>
        public static void Delete<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            db.Delete<T>(entities, null);
        }

        /// <summary>
        /// Deletes a collection of keys, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to delete. Must not be null or contain null entries.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task DeleteAsync<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            return db.DeleteAsync<T>(entities, null);
        }
        #endregion

        #region Update extensions on entities
        /// <summary>
        /// Updates a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to update. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static void Update<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            db.Update(Entity<T>.To(new Entity { Key = key }, entity), callSettings);
        }

        /// <summary>
        /// Updates a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to update. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static Task UpdateAsync<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return db.UpdateAsync(Entity<T>.To(new Entity { Key = key }, entity), callSettings);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static void Update<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            db.Update(entities.Select(x =>
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                return Entity<T>.To(new Entity { Key = getKey(x) }, x);
            }), callSettings);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task UpdateAsync<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.UpdateAsync(entities.Select(x =>
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                return Entity<T>.To(new Entity { Key = getKey(x) }, x);
            }), callSettings);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static void Update<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            db.Update<T>(entities, getKey, null);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task UpdateAsync<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            return db.UpdateAsync<T>(entities, getKey, null);
        }
        #endregion

        #region Upsert extensions on entities
        /// <summary>
        /// Upserts a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to upsert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>null if the entity was updated or was inserted with a predefined key, or the 
        /// new key if the entity was inserted and the mutation allocated the key.</returns>
        public static Key Upsert<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            return db.Upsert(Entity<T>.To(new Entity { Key = key }, entity), callSettings);
        }

        /// <summary>
        /// Upserts a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entity">The entity to upsert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>null if the entity was updated or was inserted with a predefined key, or the 
        /// new key if the entity was inserted and the mutation allocated the key.</returns>
        public static Task<Key> UpsertAsync<T>(this DatastoreDb db, T entity, Key key, CallSettings callSettings = null)
            where T : class
        {
            return db.UpsertAsync(Entity<T>.To(new Entity { Key = key }, entity), callSettings);
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to upsert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static IReadOnlyList<Key> Upsert<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.Upsert(entities.Select(x => Entity<T>.To(new Entity
            {
                Key = getKey(x),
            }, x)), callSettings);
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to upsert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static Task<IReadOnlyList<Key>> UpsertAsync<T>(this DatastoreDb db, IEnumerable<T> entities, Func<T, Key> getKey, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.UpsertAsync(entities.Select(x => Entity<T>.To(new Entity
            {
                Key = getKey(x)
            }, x)), callSettings);
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to upsert. Must not be null or contain null entries.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static IReadOnlyList<Key> Upsert<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            return db.Upsert<T>(entities, getKey, null);
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="entities">The entities to upsert. Must not be null or contain null entries.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static Task<IReadOnlyList<Key>> UpsertAsync<T>(this DatastoreDb db, Func<T, Key> getKey, params T[] entities)
            where T : class
        {
            return db.UpsertAsync<T>(entities, getKey, null);
        }
        #endregion

        #region Query extensions on entities
        /// <summary>
        /// Access query results as typed entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="results">The query results.</param>
        /// <param name="create">The entity constructor.</param>
        /// <returns>A sequence of entities matching the given query.</returns>
        public static IEnumerable<T> Entities<T>(this DatastoreQueryResults results, Func<T> create)
            where T : class
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            return results.Entities.Select(create);
        }

        /// <summary>
        /// Map a sequence of <see cref="Entity"/> to a sequence of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The sequence of <see cref="Entity"/>.</param>
        /// <param name="create">The typed entity constructor.</param>
        /// <returns>A sequence of typed entities.</returns>
        public static IEnumerable<T> Select<T>(this IEnumerable<Entity> entities, Func<T> create)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (create == null) throw new ArgumentNullException(nameof(create));
            return entities.Select(x => Entity<T>.From(create(), x));
        }

        /// <summary>
        /// Asynchronously map a sequence of <see cref="Entity"/> to a sequence of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The asynchronous sequence of <see cref="Entity"/>.</param>
        /// <param name="create">The typed entity constructor.</param>
        /// <returns>An asynchronous sequence of typed entities.</returns>
        public static IAsyncEnumerable<T> Select<T>(this IAsyncEnumerable<Entity> entities, Func<T> create)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (create == null) throw new ArgumentNullException(nameof(create));
            return new AsyncEnumerable<T> { create = create, entities = entities };
        }

        sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
            where T : class
        {
            internal Func<T> create;
            internal IAsyncEnumerable<Entity> entities;
            public IAsyncEnumerator<T> GetEnumerator()
            {
                return new AsyncEnumerator<T> { create = create, entities = entities.GetEnumerator() };
            }
        }
        sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
            where T : class
        {
            internal Func<T> create;
            internal IAsyncEnumerator<Entity> entities;

            public T Current { get; private set; }
            public async Task<bool> MoveNext(CancellationToken token)
            {
                var result = await entities.MoveNext(token);
                Current = Entity<T>.From(create(), entities.Current);
                return result;
            }
            public void Dispose()
            {
                Interlocked.Exchange(ref entities, null)?.Dispose();
            }
        }
        #endregion
    }
}
