namespace ThreadSafeMemoryCache
{
    /// <summary>
    /// Cache operations
    /// </summary>
    public enum CacheOperation
    {
        AddOrUpdate,
        ContainsKey,
        Get,
        Remove,
        RemoveAll,
        BackgroundThreadFlushItems
    }
}
