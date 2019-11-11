using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Public interface for managing memory cache with generics types for keys and values
    /// </summary>
    public interface ICache<TKey, KValue> : ICache
        where TKey : class, IEquatable<TKey>
        where KValue : class
    {
        /// <summary>
        /// Returns a bool indicating if a certain key exits in memory cache
        /// </summary>
        bool ContainsKey(TKey key);

        /// <summary>
        /// If a certain key does not exits in cache, it will create a new cache entry,
        /// otherwise will overwrite value
        /// </summary>
        void AddOrUpdate(TKey key, KValue value, long size = 1);

        /// <summary>
        /// If a certain key does not exits in cache, it will create a new cache entry,
        /// otherwise will overwrite value
        /// </summary>
        void AddOrUpdate(TKey key, KValue value, TimeSpan timeToExpiration, long size = 1);

        /// <summary>
        /// Returns a value by its key. If key is not present, it will returns default value
        /// for KValue type
        /// </summary>
        KValue Get(TKey key, KValue defaultValue = default(KValue));

        /// <summary>
        /// Removes a certain item from local memory cache
        /// </summary>
        void Remove(TKey key);
    }
}
