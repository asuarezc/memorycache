using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Singleton factory to manage memory caches
    /// </summary>
    public sealed class CacheManager : ICacheManager
    {
        private readonly IDictionary<string, ICache> CurrentLocalCaches;

        private static readonly Lazy<ICacheManager> lazyInstance = new Lazy<ICacheManager>(
            () => new CacheManager(), LazyThreadSafetyMode.ExecutionAndPublication
        );

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ICacheManager Instance => lazyInstance.Value;

        private CacheManager()
        {
            CurrentLocalCaches = new Dictionary<string, ICache>();
        }

        /// <summary>
        /// Gets a existing cache. If does not exits, it will return null
        /// </summary>
        ICache<TKey, KValue> ICacheManager.Get<TKey, KValue>(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!Exits(name))
                return null;

            ICache result = CurrentLocalCaches[name];

            if (!result.KeyType.Equals(typeof(TKey)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "There is a cache named {0} but it's type for keys is \"{1}\", not \"{2}\"",
                        name, result.KeyType.FullName, typeof(TKey).FullName
                    )
                );
            }

            if (!result.ValueType.Equals(typeof(ValueType)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "There is a cache named {0} but it's type for values is \"{1}\", not \"{2}\"",
                        name, result.ValueType.FullName, typeof(KValue).FullName
                    )
                );
            }

            return result as ICache<TKey, KValue>;
        }

        /// <summary>
        /// Creates a new cache
        /// </summary>
        ICache<TKey, KValue> ICacheManager.Create<TKey, KValue>(
            string name, CacheConfiguration configuration)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.SizeLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(configuration.SizeLimit));

            if (configuration.DefaultItemSize > configuration.SizeLimit)
                throw new InvalidOperationException();

            if (configuration.OldestItemsRemovingPercentage < 0f
                || configuration.OldestItemsRemovingPercentage > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuration.OldestItemsRemovingPercentage)
                );
            }

            if (Exits(name))
            {
                throw new InvalidOperationException(
                    string.Format("There is already a cache named \"{0}\"", name)
                );
            }

            CurrentLocalCaches.Add(
                name, new Cache<TKey, KValue>(name, configuration)
            );

            return CurrentLocalCaches[name] as ICache<TKey, KValue>;
        }

        /// <summary>
        /// Returns true if a certain local memory cache exits, otherwise returns false
        /// </summary>
        public bool Exits(string name)
        {
            return CurrentLocalCaches.ContainsKey(name)
                && CurrentLocalCaches[name] != null;
        }

        /// <summary>
        /// Removes a local memory cache
        /// </summary>
        public void Remove(string name)
        {
            if (!Exits(name))
                return;

            CurrentLocalCaches[name].Dispose();
            CurrentLocalCaches.Remove(name);
        }

        /// <summary>
        /// Removes all local memory caches
        /// </summary>
        public void RemoveAll()
        {
            foreach (string cacheName in CurrentLocalCaches.Keys)
                Remove(cacheName);
        }
    }
}
