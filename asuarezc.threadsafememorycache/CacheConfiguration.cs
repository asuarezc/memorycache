using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Defines a memory cache configuration
    /// </summary>
    public sealed class CacheConfiguration
    {
        /// <summary>
        /// Cache size limit
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// Gets or sets the default item expiration time if is not provided when it is added or updated
        /// </summary>
        public TimeSpan DefaultItemExpiration { get; set; }

        /// <summary>
        /// Gets or sets the default item size if is not provided when an item it is added or updated
        /// </summary>
        public long DefaultItemSize { get; set; }

        /// <summary>
        /// Determines the percentage of the oldest items to be deleted when the cache limit is reached
        /// Should be a value between 0 and 1. For example, a 50% is 0.5f
        /// </summary>
        public float OldestItemsRemovingPercentage { get; set; }

        /// <summary>
        /// Time to wait between a removing outdated and oldest items operation an another
        /// </summary>
        public TimeSpan PollingInterval { get; set; }
    }
}
