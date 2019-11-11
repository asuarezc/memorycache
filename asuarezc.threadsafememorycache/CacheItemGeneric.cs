using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Internal class to manage cache items
    /// </summary>
    internal class CacheItem<TKey, KValue>
        where TKey : class, IEquatable<TKey>
        where KValue : class
    {
        private KValue _item;

        /// <summary>
        /// Gets the cache item key
        /// </summary>
        public TKey Key { get; private set; }

        /// <summary>
        /// Gets or sets the cache item value
        /// </summary>
        public KValue Value
        {
            get { return _item; }
            set
            {
                _item = value;
                UTCDateAdded = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets or sets the cache item size
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets the cache item addition UTC date
        /// </summary>
        public DateTime UTCDateAdded { get; private set; }

        /// <summary>
        /// Gets or sets the item cache expiration time
        /// </summary>
        public TimeSpan Expiration { get; set; }

        internal CacheItem(TKey key, KValue item, TimeSpan expiration, long size = 1)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Key = key;
            Value = item;
            Expiration = expiration;
            Size = size;
            UTCDateAdded = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns true if this cache item is outdated, otherwise returns false
        /// </summary>
        public bool IsOutdated
        {
            get { return DateTime.UtcNow - UTCDateAdded > Expiration; }
        }
    }
}
