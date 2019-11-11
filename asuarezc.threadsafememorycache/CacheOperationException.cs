using System;

namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Exception raised during a cache operation
    /// </summary>
    public class CacheOperationException : Exception
    {
        public CacheOperation CacheOperation { get; private set; }

        public CacheOperationException(Exception innerException, CacheOperation operation)
            : base("Local memory cache exception detected. See InnerException.", innerException)
        {
            CacheOperation = operation;
        }
    }
}
