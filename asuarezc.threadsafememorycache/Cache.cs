using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Internal class to manage a generic memory cache
    /// </summary>
    internal class Cache<TKey, KValue> : ICache<TKey, KValue>
        where TKey : class, IEquatable<TKey>
        where KValue : class
    {
        private readonly IList<CacheItem<TKey, KValue>> InternalCache;
        private readonly ReaderWriterLockSlim Lock;
        private readonly CancellationTokenSource TokenSource;
        private readonly Task RemoveOutdatedAndOldestItemsTask;
        private CancellationToken Token;

        /// <summary>
        /// Raises when cache size limit is reached
        /// </summary>
        public event EventHandler SizeLimitReached;

        /// <summary>
        /// Current cache size
        /// </summary>
        public long CurrentSize { get; private set; }

        /// <summary>
        /// Cache size limit
        /// </summary>
        public long SizeLimit { get; private set; }

        /// <summary>
        /// Gets the default item expiration time if is not provided when an item it is added or updated
        /// </summary>
        public TimeSpan DefaultItemExpiration { get; private set; }

        /// <summary>
        /// Gets the default item size if is not provided when an item it is added or updated
        /// </summary>
        public long DefaultItemSize { get; private set; }

        /// <summary>
        /// Determines the percentage of the oldest items to be deleted when the cache limit is reached
        /// Should be a value between 0 and 1. For example, a 50% is 0.5f
        /// </summary>
        public float OldestItemsRemovingPercentage { get; private set; }

        /// <summary>
        /// Time to wait between a removing outdated and oldest items operation an another
        /// </summary>
        public TimeSpan PollingInterval { get; private set; }

        /// <summary>
        /// Gets the cache name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the type used for keys
        /// </summary>
        public Type KeyType { get { return typeof(TKey); } }

        /// <summary>
        /// Gets the type used for values
        /// </summary>
        public Type ValueType { get { return typeof(KValue); } }

        /// <summary>
        /// Create a new instance of Itx.Mobility.MoneyMapping.Data.LocalMemoryCache.Cache
        /// </summary>
        internal Cache(string name, CacheConfiguration configuration)
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

            Name = name;
            InternalCache = new List<CacheItem<TKey, KValue>>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            RemoveOutdatedAndOldestItemsTask =
                Task.Factory.StartNew(RemoveOutdatedAndOldestItems, Token);
        }

        /// <summary>
        /// If a certain key does not exits in cache, it will create a new cache entry,
        /// otherwise will overwrite value
        /// </summary>
        public void AddOrUpdate(TKey key, KValue value, long size = 0)
        {
            if (Token.IsCancellationRequested)
                return;

            if (key == default(TKey) || value == default(KValue))
                throw new ArgumentNullException();

            if (size < 1 || size > SizeLimit)
                throw new ArgumentOutOfRangeException(nameof(size));

            Lock.EnterWriteLock();

            try
            {
                CacheItem<TKey, KValue> item =
                    InternalCache.SingleOrDefault(it => it.Key.Equals(key));

                if (item == null)
                {
                    Add(
                        key, value, DefaultItemExpiration,
                        size == 0 ? DefaultItemSize : size
                    );
                }
                else
                {
                    if (!item.IsOutdated)
                        Update(key, value, DefaultItemExpiration, size);
                    else
                    {
                        InternalCache.Remove(item);
                        CurrentSize -= item.Size;

                        Add(
                            key, value, DefaultItemExpiration,
                            size == 0 ? DefaultItemSize : size
                        );
                    } 
                }
            }
            catch(Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.AddOrUpdate);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// If a certain key does not exits in cache, it will create a new cache entry,
        /// otherwise will overwrite value
        /// </summary>
        public void AddOrUpdate(TKey key, KValue value, TimeSpan timeToExpiration,
            long size = 0)
        {
            if (Token.IsCancellationRequested)
                return;

            if (key == default(TKey) || value == default(KValue))
                throw new ArgumentNullException();

            if (size < 1 || size > SizeLimit)
                throw new ArgumentOutOfRangeException(nameof(size));

            Lock.EnterWriteLock();

            try
            {
                CacheItem<TKey, KValue> item =
                    InternalCache.SingleOrDefault(it => it.Key.Equals(key));

                if (item == null)
                {
                    Add(
                        key, value, timeToExpiration,
                        size == 0 ? DefaultItemSize : size
                    );
                }
                else
                {
                    if (!item.IsOutdated)
                        Update(key, value, timeToExpiration, size);
                    else
                    {
                        InternalCache.Remove(item);
                        CurrentSize -= item.Size;

                        Add(
                            key, value, timeToExpiration,
                            size == 0 ? DefaultItemSize : size
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.AddOrUpdate);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns a bool indicating if a certain key exits in memory cache
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            if (Token.IsCancellationRequested)
                return false;

            if (key == default(TKey))
                throw new ArgumentNullException(nameof(key));

            Lock.EnterReadLock();

            try
            {
                CacheItem<TKey, KValue> item =
                    InternalCache.SingleOrDefault(it => it.Key.Equals(key));

                if (item != null && !item.IsOutdated)
                    return true;

                if (item != null && item.IsOutdated)
                {
                    InternalCache.Remove(item);
                    CurrentSize -= item.Size;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.ContainsKey);
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a value by its key. If key is not present,
        /// it will returns default value for KValue type
        /// </summary>
        public KValue Get(TKey key, KValue defaultValue = default(KValue))
        {
            if (Token.IsCancellationRequested)
                return defaultValue;

            if (key == default(TKey))
                throw new ArgumentNullException(nameof(key));

            Lock.EnterReadLock();

            try
            {
                CacheItem<TKey, KValue> item =
                    InternalCache.SingleOrDefault(it => it.Key.Equals(key));

                if (item == null)
                    return defaultValue;

                if (item.IsOutdated)
                {
                    InternalCache.Remove(item);
                    CurrentSize -= item.Size;
                    return defaultValue;
                }

                return item.Value;
            }
            catch (Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.Get);
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes a certain item from local memory cache
        /// </summary>
        public void Remove(TKey key)
        {
            if (Token.IsCancellationRequested)
                return;

            if (key == default(TKey))
                throw new ArgumentNullException(nameof(key));

            Lock.EnterWriteLock();

            try
            {
                CacheItem<TKey, KValue> item =
                    InternalCache.SingleOrDefault(it => it.Key.Equals(key));

                if (item == null)
                    return;

                InternalCache.Remove(item);
                CurrentSize -= item.Size;
            }
            catch (Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.Remove);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Remove all items from cache
        /// </summary>
        public void Clear()
        {
            RemoveAll();
        }

        private void RemoveAll(bool ignoreCancelationToken = false)
        {
            if (Token.IsCancellationRequested && !ignoreCancelationToken)
                return;

            Lock.EnterWriteLock();

            try
            {
                InternalCache.Clear();
            }
            catch (Exception ex)
            {
                throw new CacheOperationException(ex, CacheOperation.RemoveAll);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        private async Task RemoveOutdatedAndOldestItems()
        {
            while (true)
            {
                Lock.EnterReadLock();

                try
                {
                    FlushOutdatedItems();

                    if (CurrentSize >= SizeLimit)
                    {
                        SizeLimitReached?.Invoke(this, EventArgs.Empty);
                        FlushOldestItems();
                    } 
                }
                catch (Exception ex)
                {
                    throw new CacheOperationException(ex, CacheOperation.BackgroundThreadFlushItems);
                }
                finally
                {
                    Lock.ExitReadLock();
                }

                if (Token.IsCancellationRequested)
                    return;

                await Task.Delay(PollingInterval, Token);
            }
        }

        private void Add(TKey key, KValue value, TimeSpan timeToExpiration,
            long size = 1) //Must be invoked inside a write lock
        {
            if ((CurrentSize + size) >= SizeLimit)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);
                FlushOutdatedItems();
            }

            if ((CurrentSize + size) >= SizeLimit)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);
                FlushOldestItems();
            }

            CacheItem<TKey, KValue> itemToBeAdded =
                new CacheItem<TKey, KValue>(key, value, timeToExpiration, size);

            InternalCache.Add(itemToBeAdded);
            CurrentSize += itemToBeAdded.Size;
        }

        private void Update(TKey key, KValue value, TimeSpan timeToExpiration,
            long size = 1) //Must be invoked inside a write lock
        {
            CacheItem<TKey, KValue> itemToBeUpdated =
                InternalCache.Single(it => it.Key.Equals(key));

            if ((CurrentSize - itemToBeUpdated.Size + size) >= SizeLimit)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);
                FlushOutdatedItems();
            }

            if ((CurrentSize + size) >= SizeLimit)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);
                FlushOldestItems();
            } 

            CurrentSize -= itemToBeUpdated.Size;

            itemToBeUpdated.Value = value;
            itemToBeUpdated.Expiration = timeToExpiration;
            itemToBeUpdated.Size = size;

            CurrentSize += itemToBeUpdated.Size;
        }

        private void FlushOldestItems() //Must be invoked inside a write lock
        {
            int numberOfItemsToBeRemoved = (int)Math.Truncate(
                InternalCache.Count * OldestItemsRemovingPercentage
            );

            IEnumerable<CacheItem<TKey, KValue>> itemsToBeRemoded = InternalCache
                .OrderBy(it => it.UTCDateAdded)
                .Take(numberOfItemsToBeRemoved);

            if (Token.IsCancellationRequested)
                return;

            foreach (CacheItem<TKey, KValue> item in itemsToBeRemoded)
            {
                InternalCache.Remove(item);
                CurrentSize -= item.Size;

                if (Token.IsCancellationRequested)
                    return;
            }
        }

        private void FlushOutdatedItems(bool ignoreCancelationToken = false) //Must be invoked inside a write lock
        {
            IEnumerable<CacheItem<TKey, KValue>> outdatedItems =
                InternalCache.Where(it => it.IsOutdated);

            if (Token.IsCancellationRequested && !ignoreCancelationToken)
                return;

            foreach (CacheItem<TKey, KValue> item in outdatedItems)
            {
                InternalCache.Remove(item);
                CurrentSize -= item.Size;

                if (Token.IsCancellationRequested && !ignoreCancelationToken)
                    return;
            }
        }

        /// <summary>
        /// Finalice this instance
        /// </summary>
        public void Dispose()
        {
            SizeLimitReached = null;
            TokenSource.Cancel();

            if (RemoveOutdatedAndOldestItemsTask != null && !RemoveOutdatedAndOldestItemsTask.IsCompleted)
                RemoveOutdatedAndOldestItemsTask.Wait();

            RemoveAll(true);
            TokenSource?.Dispose();
            Lock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
