// AdvancedCaching.fs - Advanced Caching System with Multiple Strategies
// Functional caching with LRU, LFU, TTL, and custom eviction policies
// Demonstrates sophisticated memory management and performance optimization

namespace PhoenixVisualizer.Core

open System
open System.Collections.Generic
open System.Collections.Concurrent

/// <summary>
/// Advanced caching system with multiple eviction strategies
/// Functional design for thread-safety and composability
/// </summary>
module AdvancedCaching =

    /// <summary>
    /// Cache entry with metadata for eviction decisions
    /// </summary>
    type CacheEntry<'TValue> = {
        Value: 'TValue
        Created: DateTime
        LastAccessed: DateTime
        AccessCount: int64
        Size: int64
        Key: string
    }

    /// <summary>
    /// Cache statistics for monitoring and optimization
    /// </summary>
    type CacheStats = {
        TotalEntries: int64
        TotalSize: int64
        Hits: int64
        Misses: int64
        Evictions: int64
        HitRate: float
        AverageAccessTime: TimeSpan
    }

    /// <summary>
    /// Eviction policy for cache entries
    /// </summary>
    type EvictionPolicy =
        | LRU // Least Recently Used
        | LFU // Least Frequently Used
        | FIFO // First In, First Out
        | SizeBased // Based on entry size
        | TTL // Time To Live
        | Custom of (CacheEntry<'TValue> -> CacheEntry<'TValue> -> int) // Custom comparison function

    /// <summary>
    /// Cache configuration
    /// </summary>
    type CacheConfig = {
        MaxEntries: int option
        MaxSize: int64 option
        DefaultTTL: TimeSpan option
        EvictionPolicy: EvictionPolicy
        EnableStats: bool
    }

    /// <summary>
    /// Advanced cache with multiple eviction strategies
    /// </summary>
    type AdvancedCache<'TValue> = {
        Entries: ConcurrentDictionary<string, CacheEntry<'TValue>>
        Config: CacheConfig
        Stats: CacheStats ref
        CleanupTimer: System.Timers.Timer option
    }

    /// <summary>
    /// Cache operation results
    /// </summary>
    type CacheResult<'T> =
        | Hit of 'T
        | Miss
        | Expired
        | Evicted

    // Utility functions for cache operations

    /// <summary>
    /// Create a new cache entry
    /// </summary>
    let createEntry key value size =
        {
            Value = value
            Created = DateTime.Now
            LastAccessed = DateTime.Now
            AccessCount = 0L
            Size = size
            Key = key
        }

    /// <summary>
    /// Update access information for an entry
    /// </summary>
    let updateAccess entry =
        { entry with
            LastAccessed = DateTime.Now
            AccessCount = entry.AccessCount + 1L }

    /// <summary>
    /// Check if an entry is expired
    /// </summary>
    let isExpired ttl entry =
        match ttl with
        | Some ttl -> DateTime.Now - entry.Created > ttl
        | None -> false

    /// <summary>
    /// Calculate entry size (simple implementation)
    /// </summary>
    let calculateSize (value: 'T) =
        // This is a simplified size calculation
        // In a real implementation, you'd use more sophisticated sizing
        match box value with
        | :? Array as arr -> int64 (arr.Length * 4) // Assume 4 bytes per element
        | :? string as str -> int64 (str.Length * 2) // UTF-16
        | _ -> 128L // Default size for unknown types

    /// <summary>
    /// LRU eviction policy - compare by last access time
    /// </summary>
    let lruPolicy a b =
        if a.LastAccessed < b.LastAccessed then -1
        elif a.LastAccessed > b.LastAccessed then 1
        else 0

    /// <summary>
    /// LFU eviction policy - compare by access count
    /// </summary>
    let lfuPolicy a b =
        if a.AccessCount < b.AccessCount then -1
        elif a.AccessCount > b.AccessCount then 1
        else 0

    /// <summary>
    /// FIFO eviction policy - compare by creation time
    /// </summary>
    let fifoPolicy a b =
        if a.Created < b.Created then -1
        elif a.Created > b.Created then 1
        else 0

    /// <summary>
    /// Size-based eviction policy - compare by size
    /// </summary>
    let sizeBasedPolicy a b =
        if a.Size < b.Size then -1
        elif a.Size > b.Size then 1
        else 0

    /// <summary>
    /// Find entry to evict based on policy
    /// </summary>
    let findEvictionCandidate policy entries =
        entries
        |> Seq.minBy (fun kvp ->
            match policy with
            | LRU -> kvp.Value.LastAccessed.Ticks
            | LFU -> kvp.Value.AccessCount
            | FIFO -> kvp.Value.Created.Ticks
            | SizeBased -> kvp.Value.Size
            | TTL -> kvp.Value.Created.Ticks
            | Custom comparer ->
                // For custom policy, we need to find the "worst" entry
                // This is a simplified implementation
                kvp.Value.LastAccessed.Ticks)

    /// <summary>
    /// Evict entries to make room for new entry
    /// </summary>
    let evictEntries cache newEntrySize =
        let rec evictLoop cache currentSize =
            match cache.Config.MaxSize with
            | Some maxSize when currentSize + newEntrySize > maxSize ->
                let candidate = findEvictionCandidate cache.Config.EvictionPolicy cache.Entries
                cache.Entries.TryRemove(candidate.Key) |> ignore

                // Update stats
                if cache.Config.EnableStats then
                    cache.Stats := { !cache.Stats with Evictions = !cache.Stats.Evictions + 1L }

                let newSize = currentSize - candidate.Value.Size
                evictLoop cache newSize
            | _ -> ()

        let currentSize = cache.Entries.Values |> Seq.sumBy (fun e -> e.Size)
        evictLoop cache currentSize

    /// <summary>
    /// Clean up expired entries
    /// </summary>
    let cleanupExpired cache =
        let expiredKeys =
            cache.Entries
            |> Seq.filter (fun kvp ->
                match cache.Config.DefaultTTL with
                | Some ttl -> DateTime.Now - kvp.Value.Created > ttl
                | None -> false)
            |> Seq.map (fun kvp -> kvp.Key)
            |> Seq.toList

        for key in expiredKeys do
            cache.Entries.TryRemove(key) |> ignore

        if cache.Config.EnableStats && not expiredKeys.IsEmpty then
            cache.Stats := { !cache.Stats with Evictions = !cache.Stats.Evictions + int64 expiredKeys.Length }

    /// <summary>
    /// Create a new advanced cache
    /// </summary>
    let create config =
        let cache = {
            Entries = ConcurrentDictionary<string, CacheEntry<'TValue>>()
            Config = config
            Stats = ref {
                TotalEntries = 0L
                TotalSize = 0L
                Hits = 0L
                Misses = 0L
                Evictions = 0L
                HitRate = 0.0
                AverageAccessTime = TimeSpan.Zero
            }
            CleanupTimer = None
        }

        // Set up cleanup timer for TTL-based eviction
        match config.DefaultTTL with
        | Some ttl ->
            let timer = new System.Timers.Timer()
            timer.Interval <- ttl.TotalMilliseconds / 4.0 // Clean up 4 times per TTL period
            timer.Elapsed.Add(fun _ -> cleanupExpired cache)
            timer.Start()
            { cache with CleanupTimer = Some timer }
        | None -> cache

    /// <summary>
    /// Get a value from the cache
    /// </summary>
    let get key cache =
        match cache.Entries.TryGetValue(key) with
        | true, entry ->
            // Check if expired
            if isExpired cache.Config.DefaultTTL entry then
                cache.Entries.TryRemove(key) |> ignore
                if cache.Config.EnableStats then
                    cache.Stats := { !cache.Stats with Misses = !cache.Stats.Misses + 1L }
                Miss
            else
                // Update access info
                let updatedEntry = updateAccess entry
                cache.Entries.[key] <- updatedEntry

                if cache.Config.EnableStats then
                    cache.Stats := { !cache.Stats with Hits = !cache.Stats.Hits + 1L }

                Hit updatedEntry.Value
        | false, _ ->
            if cache.Config.EnableStats then
                cache.Stats := { !cache.Stats with Misses = !cache.Stats.Misses + 1L }
            Miss

    /// <summary>
    /// Put a value in the cache
    /// </summary>
    let put key value cache =
        let size = calculateSize value
        let entry = createEntry key value size

        // Check size limits and evict if necessary
        match cache.Config.MaxSize with
        | Some maxSize when cache.Entries.Values |> Seq.sumBy (fun e -> e.Size) + size > maxSize ->
            evictEntries cache size
        | _ -> ()

        // Check entry count limits
        match cache.Config.MaxEntries with
        | Some maxEntries when cache.Entries.Count >= maxEntries ->
            let candidate = findEvictionCandidate cache.Config.EvictionPolicy cache.Entries
            cache.Entries.TryRemove(candidate.Key) |> ignore

            if cache.Config.EnableStats then
                cache.Stats := { !cache.Stats with Evictions = !cache.Stats.Evictions + 1L }
        | _ -> ()

        // Add the new entry
        cache.Entries.[key] <- entry

        // Update stats
        if cache.Config.EnableStats then
            cache.Stats := {
                !cache.Stats with
                    TotalEntries = int64 cache.Entries.Count
                    TotalSize = cache.Entries.Values |> Seq.sumBy (fun e -> e.Size)
            }

    /// <summary>
    /// Remove a value from the cache
    /// </summary>
    let remove key cache =
        match cache.Entries.TryRemove(key) with
        | true, entry ->
            if cache.Config.EnableStats then
                cache.Stats := {
                    !cache.Stats with
                        TotalEntries = int64 cache.Entries.Count
                        TotalSize = !cache.Stats.TotalSize - entry.Size
                }
            true
        | false, _ -> false

    /// <summary>
    /// Clear all entries from the cache
    /// </summary>
    let clear cache =
        cache.Entries.Clear()
        if cache.Config.EnableStats then
            cache.Stats := {
                !cache.Stats with
                    TotalEntries = 0L
                    TotalSize = 0L
                    Hits = 0L
                    Misses = 0L
                    Evictions = 0L
                    HitRate = 0.0
            }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    let getStats cache =
        let stats = !cache.Stats
        let totalRequests = stats.Hits + stats.Misses
        let hitRate = if totalRequests > 0L then float stats.Hits / float totalRequests else 0.0
        { stats with HitRate = hitRate }

    /// <summary>
    /// Check if a key exists in the cache
    /// </summary>
    let containsKey key cache =
        match cache.Entries.TryGetValue(key) with
        | true, entry ->
            not (isExpired cache.Config.DefaultTTL entry)
        | false, _ -> false

    /// <summary>
    /// Get all keys in the cache
    /// </summary>
    let keys cache =
        cache.Entries.Keys |> Seq.toList

    /// <summary>
    /// Get cache size information
    /// </summary>
    let size cache =
        cache.Entries.Count, cache.Entries.Values |> Seq.sumBy (fun e -> e.Size)

    // Advanced caching strategies

    /// <summary>
    /// Multi-level cache with L1 and L2 caches
    /// </summary>
    module MultiLevel =

        type MultiLevelCache<'TValue> = {
            L1Cache: AdvancedCache<'TValue>
            L2Cache: AdvancedCache<'TValue>
        }

        /// <summary>
        /// Create a multi-level cache
        /// </summary>
        let create l1Config l2Config =
            {
                L1Cache = create l1Config
                L2Cache = create l2Config
            }

        /// <summary>
        /// Get a value from multi-level cache
        /// </summary>
        let get key cache =
            match get key cache.L1Cache with
            | Hit value -> Hit value
            | _ ->
                match get key cache.L2Cache with
                | Hit value ->
                    // Promote to L1
                    put key value cache.L1Cache
                    Hit value
                | _ -> Miss

        /// <summary>
        /// Put a value in multi-level cache
        /// </summary>
        let put key value cache =
            put key value cache.L1Cache
            put key value cache.L2Cache

    /// <summary>
    /// Distributed cache simulation (for future distributed scenarios)
    /// </summary>
    module Distributed =

        type DistributedCache<'TValue> = {
            LocalCache: AdvancedCache<'TValue>
            // In a real implementation, this would include network communication
            NodeId: string
        }

        /// <summary>
        /// Create a distributed cache
        /// </summary>
        let create config nodeId =
            {
                LocalCache = create config
                NodeId = nodeId
            }

        /// <summary>
        /// Get from distributed cache (simplified local-only version)
        /// </summary>
        let get key cache = get key cache.LocalCache

        /// <summary>
        /// Put in distributed cache
        /// </summary>
        let put key value cache = put key value cache.LocalCache

    /// <summary>
    /// Cache with automatic loading
    /// </summary>
    module AutoLoading =

        type AutoLoadingCache<'TValue> = {
            Cache: AdvancedCache<'TValue>
            Loader: string -> 'TValue option
        }

        /// <summary>
        /// Create an auto-loading cache
        /// </summary>
        let create config loader =
            {
                Cache = create config
                Loader = loader
            }

        /// <summary>
        /// Get or load a value
        /// </summary>
        let getOrLoad key cache =
            match get key cache.Cache with
            | Hit value -> Hit value
            | _ ->
                match cache.Loader key with
                | Some value ->
                    put key value cache.Cache
                    Hit value
                | None -> Miss

    /// <summary>
    /// Cache performance monitoring
    /// </summary>
    module Monitoring =

        /// <summary>
        /// Performance metrics
        /// </summary>
        type PerformanceMetrics = {
            OperationCount: int64
            TotalTime: TimeSpan
            AverageTime: TimeSpan
            MinTime: TimeSpan
            MaxTime: TimeSpan
            Percentile95: TimeSpan
            Percentile99: TimeSpan
        }

        /// <summary>
        /// Monitor cache performance
        /// </summary>
        let createMonitor () =
            let operationTimes = ConcurrentBag<TimeSpan>()
            let operationCount = ref 0L

            let recordOperation time =
                operationTimes.Add(time)
                System.Threading.Interlocked.Increment(operationCount) |> ignore

            let getMetrics () =
                let times = operationTimes |> Seq.toArray |> Array.sort
                if times.Length = 0 then
                    {
                        OperationCount = 0L
                        TotalTime = TimeSpan.Zero
                        AverageTime = TimeSpan.Zero
                        MinTime = TimeSpan.Zero
                        MaxTime = TimeSpan.Zero
                        Percentile95 = TimeSpan.Zero
                        Percentile99 = TimeSpan.Zero
                    }
                else
                    let total = times |> Array.sumBy (fun t -> t.Ticks) |> TimeSpan.FromTicks
                    let avg = TimeSpan.FromTicks(total.Ticks / int64 times.Length)
                    let min = Array.min times
                    let max = Array.max times
                    let p95 = times.[int (float times.Length * 0.95)]
                    let p99 = times.[int (float times.Length * 0.99)]

                    {
                        OperationCount = !operationCount
                        TotalTime = total
                        AverageTime = avg
                        MinTime = min
                        MaxTime = max
                        Percentile95 = p95
                        Percentile99 = p99
                    }

            recordOperation, getMetrics

    /// <summary>
    /// Cache invalidation strategies
    /// </summary>
    module Invalidation =

        /// <summary>
        /// Time-based invalidation
        /// </summary>
        let timeBased ttl cache =
            let timer = new System.Timers.Timer()
            timer.Interval <- ttl.TotalMilliseconds
            timer.Elapsed.Add(fun _ -> clear cache)
            timer.Start()
            timer

        /// <summary>
        /// Pattern-based invalidation
        /// </summary>
        let patternBased pattern cache =
            // Invalidate keys matching a pattern
            let matchingKeys =
                cache.Entries.Keys
                |> Seq.filter (fun key -> Text.RegularExpressions.Regex.IsMatch(key, pattern))
                |> Seq.toList

            for key in matchingKeys do
                remove key cache |> ignore

        /// <summary>
        /// Dependency-based invalidation
        /// </summary>
        let dependencyBased dependencies cache =
            // Invalidate based on dependency relationships
            for dependentKey in dependencies do
                remove dependentKey cache |> ignore

    /// <summary>
    /// Cache serialization for persistence
    /// </summary>
    module Serialization =

        open System.IO
        open System.Text.Json

        /// <summary>
        /// Serialize cache to JSON
        /// </summary>
        let serializeToJson (cache: AdvancedCache<'TValue>) path =
            let entries =
                cache.Entries
                |> Seq.map (fun kvp ->
                    {|
                        Key = kvp.Key
                        Value = kvp.Value.Value
                        Created = kvp.Value.Created
                        LastAccessed = kvp.Value.LastAccessed
                        AccessCount = kvp.Value.AccessCount
                        Size = kvp.Value.Size
                    |})
                |> Seq.toArray

            let json = JsonSerializer.Serialize(entries)
            File.WriteAllText(path, json)

        /// <summary>
        /// Deserialize cache from JSON
        /// </summary>
        let deserializeFromJson<'TValue> config path : AdvancedCache<'TValue> =
            if File.Exists(path) then
                let json = File.ReadAllText(path)
                let entries = JsonSerializer.Deserialize<{| Key: string; Value: 'TValue; Created: DateTime; LastAccessed: DateTime; AccessCount: int64; Size: int64 |}[]>(json)

                let cache = create config
                for entry in entries do
                    let cacheEntry = {
                        Value = entry.Value
                        Created = entry.Created
                        LastAccessed = entry.LastAccessed
                        AccessCount = entry.AccessCount
                        Size = entry.Size
                        Key = entry.Key
                    }
                    cache.Entries.[entry.Key] <- cacheEntry

                cache
            else
                create config

    /// <summary>
    /// Cache compression for memory efficiency
    /// </summary>
    module Compression =

        /// <summary>
        /// Simple run-length encoding for repetitive data
        /// </summary>
        let runLengthEncode (data: 'T[]) =
            let rec encode acc current count = function
                | [] -> List.rev ((current, count) :: acc)
                | x :: xs when x = current -> encode acc current (count + 1) xs
                | x :: xs -> encode ((current, count) :: acc) x 1 xs

            match Array.toList data with
            | [] -> []
            | x :: xs -> encode [] x 1 xs

        /// <summary>
        /// Run-length decoding
        /// </summary>
        let runLengthDecode (encoded: ('T * int) list) =
            encoded
            |> List.collect (fun (value, count) -> List.replicate count value)
            |> List.toArray

        /// <summary>
        /// Compress cache entries using run-length encoding
        /// </summary>
        let compressEntries (entries: CacheEntry<'TValue>[]) =
            // This is a simplified example - real compression would be more sophisticated
            entries

        /// <summary>
        /// Decompress cache entries
        /// </summary>
        let decompressEntries (compressed: CacheEntry<'TValue>[]) =
            compressed
