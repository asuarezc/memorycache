using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Public interface for memory cache manager
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// Gets a existing cache. If does not exits, it will return null
        /// </summary>
        ICache<TKey, KValue> Get<TKey, KValue>(string name)
            where TKey : class, IEquatable<TKey>
            where KValue : class;

        /// <summary>
        /// Creates a new cache
        /// </summary>
        ICache<TKey, KValue> Create<TKey, KValue>(
            string name, CacheConfiguration configuration)
            where TKey : class, IEquatable<TKey>
            where KValue : class;

        /// <summary>
        /// Returns true if a certain local memory cache exits, otherwise returns false
        /// </summary>
        bool Exits(string name);

        /// <summary>
        /// Removes a local memory cache
        /// </summary>
        void Remove(string name);

        /// <summary>
        /// Removes all local memory caches
        /// </summary>
        void RemoveAll();
    }
}
