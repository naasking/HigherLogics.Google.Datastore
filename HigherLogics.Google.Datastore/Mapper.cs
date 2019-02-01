using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax.Grpc;
using Google.Cloud.Datastore.V1;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// The configuration options
    /// </summary>
    public static class Mapper
    {
        #region Configurable parameters
        /// <summary>
        /// The mapper used to marshal values and entities.
        /// </summary>
        public static IEntityMapper Default { get; set; } = new PropertyMapper();

        /// <summary>
        /// Let user declare a constructor.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="create">The constructor that will be used to initialize new entities of type <typeparamref name="T"/>.</param>
        public static void Constructor<T>(Func<T> create)
            where T : class
        {
            Entity<T>.Create = create;
        }

        /// <summary>
        /// Override the value conversions to/from Protobuf's <see cref="Value"/> type.
        /// </summary>
        /// <param name="from">The function mapping <see cref="Value"/> to <typeparamref name="T"/>.</param>
        /// <param name="to">The function mapping <typeparamref name="T"/> to <see cref="Value"/>.</param>
        public static void Convert<T>(Func<Value, T> from, Func<T, Value> to)
        {
            Value<T>.From = from ?? throw new ArgumentNullException(nameof(from));
            Value<T>.To = to ?? throw new ArgumentNullException(nameof(to));
        }
        #endregion

        #region Key extensions
        /// <summary>
        /// Generate a kind for the given type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A kind identifier for the entity type <typeparamref name="T"/>.</returns>
        public static string Kind<T>() where T : class => Entity<T>.Kind;

        /// <summary>
        /// Generate a kind for the given type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="kind">The kind to use for entities of type <typeparamref name="T"/>.</param>
        /// <returns>A kind identifier for the entity type <typeparamref name="T"/>.</returns>
        public static void Kind<T>(string kind) where T : class => Entity<T>.Kind = kind;

        /// <summary>
        /// Create a KeyFactory using the type name.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <returns>A key factory for entities of type <typeparamref name="T"/>.</returns>
        public static KeyFactory CreateKeyFactory<T>(this DatastoreDb db) where T: class =>
            db.CreateKeyFactory(Kind<T>());

        /// <summary>
        /// Extract the key identifier.
        /// </summary>
        /// <param name="key">The entity key.</param>
        /// <returns>The Int64 identifier for the given key.</returns>
        public static long Id(this Key key) => key.Path.First().Id;

        /// <summary>
        /// Convert an Int64 to a Key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="id">The entity identifier.</param>
        /// <returns>A key for the given identifier.</returns>
        public static Key ToKey<T>(this long id) where T : class =>
            new Key().WithElement(Kind<T>(), id);

        /// <summary>
        /// Create an incomplete key for a given type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns></returns>
        public static Key CreateIncompleteKey<T>() where T : class =>
            new Key().WithElement(new Key.Types.PathElement { Kind = Mapper.Kind<T>() });
        #endregion

        #region Internal key initializers
        static Key Init<T>(T obj, Key key)
            where T : class
        {
            if (key != null)
                Entity<T>.SetKey(obj, key);
            return key;
        }

        static IReadOnlyList<Key> Init<T>(IEnumerable<T> objs, IReadOnlyList<Key> keys)
            where T : class
        {
            var i = 0;
            foreach (var obj in objs)
                Entity<T>.SetKey(obj, keys[i++]);
            return keys;
        }
        #endregion

        #region Lookup extensions on entities
        /// <summary>
        /// Looks up a single entity by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="obj">The entity to fill with the retrieved data.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>The entity with the specified key, or null if no such entity exists.</returns>
        public static T Lookup<T>(this DatastoreDb db, Key key, T obj = null, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            return Entity<T>.From(obj ?? Entity<T>.Create(), db.Lookup(key, readConsistency, callSettings));
        }

        /// <summary>
        /// Looks up a single entity by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="key">The key to lookup.</param>
        /// <param name="obj">The entity to fill with the retrieved data.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>The entity with the specified key, or null if no such entity exists.</returns>
        public static async Task<T> LookupAsync<T>(this DatastoreDb db, Key key, T obj = null, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            return Entity<T>.From(obj ?? Entity<T>.Create(), await db.LookupAsync(key, readConsistency, callSettings));
        }

        /// <summary>
        /// Looks up a collection of entities by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The keys to lookup.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static IReadOnlyList<T> Lookup<T>(this DatastoreDb db, IEnumerable<Key> keys, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            return db.Lookup(keys, readConsistency, callSettings)
                     .Select(e => e == null ? default(T) : Entity<T>.From(Entity<T>.Create(), e))
                     .ToList();
        }

        /// <summary>
        /// Looks up a collection of entities by key.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The keys to lookup.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static IReadOnlyList<T> Lookup<T>(this DatastoreDb db, params Key[] keys)
            where T : class
        {
            return db.Lookup<T>(keys, null, null);
        }

        /// <summary>
        /// Looks up a collection of entities by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The key to lookup.</param>
        /// <param name="readConsistency">The desired read consistency of the lookup, or null to use the default.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static async Task<IReadOnlyList<T>> LookupAsync<T>(this DatastoreDb db, IEnumerable<Key> keys, ReadOptions.Types.ReadConsistency? readConsistency = null, CallSettings callSettings = null)
            where T : class
        {
            //if (create == null) throw new ArgumentNullException(nameof(create));
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            var entities = await db.LookupAsync(keys, readConsistency, callSettings);
            return entities.Select(e => e == null ? default(T) : Entity<T>.From(Entity<T>.Create(), e))
                           .ToList();
        }

        /// <summary>
        /// Looks up a collection of entities by key asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="keys">The key to lookup.</param>
        /// <returns>
        /// A collection of entities with the same size as keys, containing corresponding entity references,
        /// or null where the key was not found.
        /// </returns>
        public static Task<IReadOnlyList<T>> LookupAsync<T>(this DatastoreDb db, params Key[] keys)
            where T : class
        {
            return db.LookupAsync<T>(keys, null, null);
        }
        #endregion

        #region Insert extensions on entities
        /// <summary>
        /// Inserts a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to insert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// The key of the inserted entity if it was allocated by the server, or null
        /// if the inserted entity had a predefined key.
        /// </returns>
        public static Key Insert<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return Init(obj, db.Insert(Entity<T>.To(new Entity(), obj), callSettings));
        }

        /// <summary>
        /// Inserts a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to insert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// The key of the inserted entity if it was allocated by the server, or null
        /// if the inserted entity had a predefined key.
        /// </returns>
        public static async Task<Key> InsertAsync<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return Init(obj, await db.InsertAsync(Entity<T>.To(new Entity(), obj), callSettings));
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="objs">The entities to insert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A collection of keys of inserted entities, in the same order as entities. Only
        /// keys allocated by the server will be returned; any entity with a predefined key
        /// will have a null value in the collection.
        ///</returns>
        public static IReadOnlyList<Key> Insert<T>(this DatastoreDb db, IEnumerable<T> objs, CallSettings callSettings = null)
            where T : class
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            return Init(objs, db.Insert(objs.Select(x => Entity<T>.To(new Entity(), x)), callSettings));
        }

        /// <summary>
        /// Inserts a collection of entities, non-transactionally asn asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="objs">The entities to insert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The result of the task is a collection
        /// of keys of inserted entities, in the same order as entities. Only keys allocated
        /// by the server will be returned; any entity with a predefined key will have a
        /// null value in the collection.
        ///</returns>
        public static async Task<IReadOnlyList<Key>> InsertAsync<T>(this DatastoreDb db, IEnumerable<T> objs, CallSettings callSettings = null)
            where T : class
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            return Init(objs, await db.InsertAsync(objs.Select(x => Entity<T>.To(new Entity(), x)), callSettings));
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
        public static IReadOnlyList<Key> Insert<T>(this DatastoreDb db, params T[] entities) where T : class =>
            db.Insert<T>(entities, null);

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
        public static Task<IReadOnlyList<Key>> InsertAsync<T>(this DatastoreDb db, params T[] entities) where T : class =>
            db.InsertAsync<T>(entities, null);
        #endregion

        #region Delete extensions on entities
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
            db.Delete(entities.Select(Entity<T>.GetKey), callSettings);
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
            return db.DeleteAsync(entities.Select(Entity<T>.GetKey), callSettings);
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
            db.Delete(entities, null);
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
            return db.DeleteAsync(entities, null);
        }
        #endregion

        #region Update extensions on entities
        /// <summary>
        /// Updates a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to update. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static void Update<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            db.Update(Entity<T>.To(new Entity(), obj), callSettings);
        }

        /// <summary>
        /// Updates a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to update. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static Task UpdateAsync<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return db.UpdateAsync(Entity<T>.To(new Entity(), obj), callSettings);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        public static void Update<T>(this DatastoreDb db, IEnumerable<T> entities, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            db.Update(entities.Select(x =>
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                return Entity<T>.To(new Entity(), x);
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
        public static Task UpdateAsync<T>(this DatastoreDb db, IEnumerable<T> entities, CallSettings callSettings = null)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return db.UpdateAsync(entities.Select(x =>
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                return Entity<T>.To(new Entity(), x);
            }), callSettings);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static void Update<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            db.Update<T>(entities, null);
        }

        /// <summary>
        /// Updates a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to update. Must not be null or contain null entries.</param>
        /// <param name="db">The datastore instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task UpdateAsync<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            return db.UpdateAsync<T>(entities, null);
        }
        #endregion

        #region Upsert extensions on entities
        /// <summary>
        /// Upserts a single entity, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to upsert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>null if the entity was updated or was inserted with a predefined key, or the 
        /// new key if the entity was inserted and the mutation allocated the key.</returns>
        public static Key Upsert<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return Init(obj, db.Upsert(Entity<T>.To(new Entity(), obj), callSettings));
        }

        /// <summary>
        /// Upserts a single entity, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="obj">The entity to upsert. Must not be null.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>null if the entity was updated or was inserted with a predefined key, or the 
        /// new key if the entity was inserted and the mutation allocated the key.</returns>
        public static async Task<Key> UpsertAsync<T>(this DatastoreDb db, T obj, CallSettings callSettings = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return Init(obj, await db.UpsertAsync(Entity<T>.To(new Entity(), obj), callSettings));
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="objs">The entities to upsert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static IReadOnlyList<Key> Upsert<T>(this DatastoreDb db, IEnumerable<T> objs, CallSettings callSettings = null)
            where T : class
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            return Init(objs, db.Upsert(objs.Select(x => Entity<T>.To(new Entity(), x)), callSettings));
        }

        /// <summary>
        /// Upserts a collection of entities, non-transactionally and asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="db">The datastore instance.</param>
        /// <param name="objs">The entities to upsert. Must not be null or contain null entries.</param>
        /// <param name="callSettings">If not null, applies overrides to this RPC call.</param>
        /// <returns>A collection of allocated keys, in the same order as entities. Each inserted
        /// entity which had an incomplete key - requiring the server to allocate a new key
        /// - will have a non-null value in the collection, equal to the new key for the
        /// entity. Each updated entity or inserted entity with a predefined key will have
        /// a null value in the collection.</returns>
        public static async Task<IReadOnlyList<Key>> UpsertAsync<T>(this DatastoreDb db, IEnumerable<T> objs, CallSettings callSettings = null)
            where T : class
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            return Init(objs, await db.UpsertAsync(objs.Select(x => Entity<T>.To(new Entity(), x)), callSettings));
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
        public static IReadOnlyList<Key> Upsert<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            return db.Upsert<T>(entities, null);
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
        public static Task<IReadOnlyList<Key>> UpsertAsync<T>(this DatastoreDb db, params T[] entities)
            where T : class
        {
            return db.UpsertAsync<T>(entities, null);
        }
        #endregion

        #region Query extensions on entities
        /// <summary>
        /// Create a query for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to query.</typeparam>
        /// <returns>A query for <typeparamref name="T"/>.</returns>
        public static Query CreateQuery<T>(this DatastoreDb db) where T : class =>
            new Query(Kind<T>());

        /// <summary>
        /// Access query results as typed entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="results">The query results.</param>
        /// <returns>A sequence of entities matching the given query.</returns>
        public static IEnumerable<T> Entities<T>(this DatastoreQueryResults results)
            where T : class
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            return results.Entities.ToType<T>();
        }

        /// <summary>
        /// Map a sequence of <see cref="Entity"/> to a sequence of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The sequence of <see cref="Entity"/>.</param>
        /// <returns>A sequence of typed entities.</returns>
        public static IEnumerable<T> ToType<T>(this IEnumerable<Entity> entities)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return entities.Select(x => Entity<T>.From(Entity<T>.Create(), x));
        }

        /// <summary>
        /// Asynchronously map a sequence of <see cref="Entity"/> to a sequence of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The asynchronous sequence of <see cref="Entity"/>.</param>
        /// <returns>An asynchronous sequence of typed entities.</returns>
        public static IAsyncEnumerable<T> Select<T>(this IAsyncEnumerable<Entity> entities)
            where T : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            return new AsyncEnumerable<T> { create = Entity<T>.Create, entities = entities };
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
