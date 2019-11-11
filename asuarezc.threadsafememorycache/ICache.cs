using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Public interface for managing memory cache
    /// </summary>
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Gets the cache name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the current cache size
        /// </summary>
        long CurrentSize { get; }

        /// <summary>
        /// Gets the cache size limit
        /// </summary>
        long SizeLimit { get; }

        /// <summary>
        /// Gets the default item expiration time if is not provided when an item it is added or updated
        /// </summary>
        TimeSpan DefaultItemExpiration { get; }

        /// <summary>
        /// Gets the default item size if is not provided when an item it is added or updated
        /// </summary>
        long DefaultItemSize { get; }

        /// <summary>
        /// Determines the percentage of the oldest items to be deleted when the cache limit is reached
        /// Should be a value between 0 and 1. For example, a 50% is 0.5f
        /// </summary>
        float OldestItemsRemovingPercentage { get; }

        /// <summary>
        /// Time to wait between a removing outdated and oldest items operation an another
        /// </summary>
        TimeSpan PollingInterval { get; }

        /// <summary>
        /// Removes all cached items
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the type used for keys
        /// </summary>
        Type KeyType { get; }

        /// <summary>
        /// Gets the type used for values
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Raises when cache size limit is reached
        /// </summary>
        event EventHandler SizeLimitReached;
    }
}
