// MemoryPool.fs - Custom Memory Pooling System
// High-performance object pooling for frequently allocated objects
// Reduces GC pressure and improves real-time audio processing performance

namespace PhoenixVisualizer.Core

open System
open System.Collections.Concurrent
open System.Threading
open System.Diagnostics

/// <summary>
/// Custom memory pooling system for high-performance object reuse
/// Significantly reduces GC pressure in real-time audio applications
/// </summary>
module MemoryPool =

    /// <summary>
    /// Pool statistics for monitoring and optimization
    /// </summary>
    type PoolStats = {
        TotalAllocated: int64
        TotalReturned: int64
        TotalCreated: int64
        TotalDestroyed: int64
        CurrentSize: int64
        PeakSize: int64
        HitRate: float
        AverageWaitTime: TimeSpan
    }

    /// <summary>
    /// Pool configuration
    /// </summary>
    type PoolConfig = {
        InitialSize: int
        MaxSize: int
        EnableStats: bool
        EnableThreading: bool
        CleanupInterval: TimeSpan
    }

    /// <summary>
    /// Memory pool for reusable objects
    /// </summary>
    type MemoryPool<'T> = {
        Items: ConcurrentStack<'T>
        Factory: unit -> 'T
        Reset: 'T -> unit
        Config: PoolConfig
        Stats: PoolStats ref
        CleanupTimer: Timer option
        Lock: obj
    }

    /// <summary>
    /// Pool item wrapper for tracking
    /// </summary>
    type PooledItem<'T> = {
        Item: 'T
        Pool: MemoryPool<'T>
        Created: DateTime
        mutable Returned: bool
    }

    // Pool management functions

    /// <summary>
    /// Create a new memory pool
    /// </summary>
    let create (config: PoolConfig) (factory: unit -> 'T) (reset: 'T -> unit) : MemoryPool<'T> =
        let pool = {
            Items = ConcurrentStack<'T>()
            Factory = factory
            Reset = reset
            Config = config
            Stats = ref {
                TotalAllocated = 0L
                TotalReturned = 0L
                TotalCreated = 0L
                TotalDestroyed = 0L
                CurrentSize = 0L
                PeakSize = 0L
                HitRate = 0.0
                AverageWaitTime = TimeSpan.Zero
            }
            CleanupTimer = None
            Lock = obj()
        }

        // Pre-populate pool
        for _ in 1 .. config.InitialSize do
            let item = factory ()
            pool.Items.Push(item)

            if config.EnableStats then
                pool.Stats := {
                    !pool.Stats with
                        TotalCreated = !pool.Stats.TotalCreated + 1L
                        CurrentSize = !pool.Stats.CurrentSize + 1L
                }

        // Set up cleanup timer for periodic maintenance
        let cleanupTimer = new Timer(
            fun _ -> cleanupPool pool,
            null,
            int config.CleanupInterval.TotalMilliseconds,
            int config.CleanupInterval.TotalMilliseconds
        )

        { pool with CleanupTimer = Some cleanupTimer }

    /// <summary>
    /// Rent an item from the pool
    /// </summary>
    let rent (pool: MemoryPool<'T>) : PooledItem<'T> =
        let startTime = if pool.Config.EnableStats then DateTime.Now else DateTime.MinValue

        let item, fromPool =
            if pool.Items.TryPop() then
                let item = pool.Items |> Seq.head
                item, true
            else
                let item = pool.Factory()
                item, false

        let pooledItem = {
            Item = item
            Pool = pool
            Created = DateTime.Now
            Returned = false
        }

        if pool.Config.EnableStats then
            let waitTime = DateTime.Now - startTime
            let stats = !pool.Stats
            let newTotalRequests = stats.TotalAllocated + 1L
            let newAverageWait =
                if newTotalRequests = 1L then waitTime
                else (stats.AverageWaitTime * TimeSpan.FromTicks(stats.TotalAllocated)) + waitTime
                     |> fun ts -> TimeSpan.FromTicks(ts.Ticks / newTotalRequests)

            pool.Stats := {
                stats with
                    TotalAllocated = newTotalRequests
                    TotalCreated = if fromPool then stats.TotalCreated else stats.TotalCreated + 1L
                    CurrentSize = if fromPool then stats.CurrentSize - 1L else stats.CurrentSize
                    PeakSize = max stats.PeakSize (stats.CurrentSize - (if fromPool then 1L else 0L))
                    AverageWaitTime = newAverageWait
            }

        pooledItem

    /// <summary>
    /// Return an item to the pool
    /// </summary>
    let returnItem (pooledItem: PooledItem<'T>) : unit =
        if pooledItem.Returned then
            failwith "Item has already been returned to the pool"
        else
            pooledItem.Returned <- true

            // Reset the item before returning
            pooledItem.Pool.Reset(pooledItem.Item)

            // Check if pool is at capacity
            if pooledItem.Pool.Items.Count < pooledItem.Pool.Config.MaxSize then
                pooledItem.Pool.Items.Push(pooledItem.Item)

                if pooledItem.Pool.Config.EnableStats then
                    pooledItem.Pool.Stats := {
                        !pooledItem.Pool.Stats with
                            TotalReturned = !pooledItem.Pool.Stats.TotalReturned + 1L
                            CurrentSize = !pooledItem.Pool.Stats.CurrentSize + 1L
                    }
            else
                // Pool is full, destroy the item
                // In a real implementation, you might want to queue items for later use
                if pooledItem.Pool.Config.EnableStats then
                    pooledItem.Pool.Stats := {
                        !pooledItem.Pool.Stats with
                            TotalDestroyed = !pooledItem.Pool.Stats.TotalDestroyed + 1L
                    }

    /// <summary>
    /// Get pool statistics
    /// </summary>
    let getStats (pool: MemoryPool<'T>) : PoolStats =
        let stats = !pool.Stats
        let totalRequests = stats.TotalAllocated
        let hitRate = if totalRequests > 0L then
                         float (stats.TotalAllocated - stats.TotalCreated) / float totalRequests
                      else 0.0
        { stats with HitRate = hitRate }

    /// <summary>
    /// Cleanup pool by removing stale items
    /// </summary>
    let cleanupPool (pool: MemoryPool<'T>) : unit =
        // In a real implementation, you might want to implement TTL for pool items
        // For now, just ensure we're within bounds
        while pool.Items.Count > pool.Config.MaxSize do
            pool.Items.TryPop() |> ignore

            if pool.Config.EnableStats then
                pool.Stats := {
                    !pool.Stats with
                        TotalDestroyed = !pool.Stats.TotalDestroyed + 1L
                        CurrentSize = !pool.Stats.CurrentSize - 1L
                }

    /// <summary>
    /// Dispose of the pool
    /// </summary>
    let dispose (pool: MemoryPool<'T>) : unit =
        match pool.CleanupTimer with
        | Some timer -> timer.Dispose()
        | None -> ()

        // Clear all items
        pool.Items.Clear()

    // Specialized pools for common types

    /// <summary>
    /// Pool for arrays
    /// </summary>
    module ArrayPool =

        /// <summary>
        /// Create array pool
        /// </summary>
        let createArrayPool<'T> config size : MemoryPool<'T[]> =
            let factory = fun () -> Array.zeroCreate<'T> size
            let reset = fun (arr: 'T[]) ->
                match box arr with
                | :? (float32[]) as floatArr -> Array.fill floatArr 0 arr.Length 0.0f
                | :? (int[]) as intArr -> Array.fill intArr 0 arr.Length 0
                | :? (byte[]) as byteArr -> Array.fill byteArr 0 arr.Length 0uy
                | _ -> () // For other types, assume they're reference types and set to null/default
            create config factory reset

        /// <summary>
        /// Rent array from pool
        /// </summary>
        let rentArray<'T> (pool: MemoryPool<'T[]>) size : PooledItem<'T[]> * 'T[] =
            let pooled = rent pool
            if pooled.Item.Length <> size then
                failwithf "Array size mismatch: expected %d, got %d" size pooled.Item.Length
            pooled, pooled.Item

    /// <summary>
    /// Pool for audio buffers
    /// </summary>
    module AudioBufferPool =

        type AudioBuffer = {
            Left: float32[]
            Right: float32[]
            SampleRate: int
            Channels: int
        }

        /// <summary>
        /// Create audio buffer pool
        /// </summary>
        let createAudioBufferPool config bufferSize sampleRate : MemoryPool<AudioBuffer> =
            let factory = fun () -> {
                Left = Array.zeroCreate<float32> bufferSize
                Right = Array.zeroCreate<float32> bufferSize
                SampleRate = sampleRate
                Channels = 2
            }
            let reset = fun (buffer: AudioBuffer) ->
                Array.fill buffer.Left 0 bufferSize 0.0f
                Array.fill buffer.Right 0 bufferSize 0.0f
            create config factory reset

    /// <summary>
    /// Pool for image buffers
    /// </summary>
    module ImageBufferPool =

        type ImageBuffer = {
            Pixels: int[]
            Width: int
            Height: int
            Stride: int
        }

        /// <summary>
        /// Create image buffer pool
        /// </summary>
        let createImageBufferPool config width height : MemoryPool<ImageBuffer> =
            let stride = width * 4 // Assume 32-bit RGBA
            let bufferSize = height * stride
            let factory = fun () -> {
                Pixels = Array.zeroCreate<int> bufferSize
                Width = width
                Height = height
                Stride = stride
            }
            let reset = fun (buffer: ImageBuffer) ->
                Array.fill buffer.Pixels 0 bufferSize 0
            create config factory reset

    /// <summary>
    /// Pool registry for managing multiple pools
    /// </summary>
    module Registry =

        type PoolRegistry = {
            Pools: ConcurrentDictionary<string, obj>
            DefaultConfig: PoolConfig
        }

        /// <summary>
        /// Create pool registry
        /// </summary>
        let create defaultConfig =
            {
                Pools = ConcurrentDictionary<string, obj>()
                DefaultConfig = defaultConfig
            }

        /// <summary>
        /// Register a pool
        /// </summary>
        let register name pool registry =
            registry.Pools.[name] <- box pool

        /// <summary>
        /// Get a pool by name
        /// </summary>
        let getPool<'T> name registry : MemoryPool<'T> option =
            match registry.Pools.TryGetValue(name) with
            | true, pool -> Some (unbox pool)
            | false, _ -> None

        /// <summary>
        /// Get or create a pool
        /// </summary>
        let getOrCreatePool name factory registry =
            match getPool name registry with
            | Some pool -> pool
            | None ->
                let pool = factory registry.DefaultConfig
                register name pool registry
                pool

        /// <summary>
        /// Get registry statistics
        /// </summary>
        let getRegistryStats registry =
            registry.Pools
            |> Seq.map (fun kvp ->
                let poolName = kvp.Key
                match kvp.Value with
                | :? MemoryPool<obj> as pool -> poolName, getStats pool
                | _ -> poolName, {
                    TotalAllocated = 0L
                    TotalReturned = 0L
                    TotalCreated = 0L
                    TotalDestroyed = 0L
                    CurrentSize = 0L
                    PeakSize = 0L
                    HitRate = 0.0
                    AverageWaitTime = TimeSpan.Zero
                })
            |> Seq.toList

    /// <summary>
    /// Performance monitoring for pools
    /// </summary>
    module Monitoring =

        /// <summary>
        /// Performance metrics
        /// </summary>
        type PerformanceMetrics = {
            PoolCount: int
            TotalMemoryUsed: int64
            AverageHitRate: float
            TotalAllocations: int64
            TotalDeallocations: int64
            MemoryEfficiency: float
        }

        /// <summary>
        /// Calculate performance metrics for registry
        /// </summary>
        let calculateMetrics registry =
            let stats = Registry.getRegistryStats registry

            let totalMemory = stats |> List.sumBy (fun (_, s) -> s.CurrentSize * 128L) // Rough estimate
            let avgHitRate = stats |> List.averageBy (fun (_, s) -> s.HitRate)
            let totalAllocations = stats |> List.sumBy (fun (_, s) -> s.TotalAllocated)
            let totalDeallocations = stats |> List.sumBy (fun (_, s) -> s.TotalReturned + s.TotalDestroyed)

            let memoryEfficiency =
                if totalAllocations > 0L then
                    float (totalAllocations - (stats |> List.sumBy (fun (_, s) -> s.TotalCreated))) / float totalAllocations
                else 0.0

            {
                PoolCount = stats.Length
                TotalMemoryUsed = totalMemory
                AverageHitRate = avgHitRate
                TotalAllocations = totalAllocations
                TotalDeallocations = totalDeallocations
                MemoryEfficiency = memoryEfficiency
            }

    /// <summary>
    /// Pool utilities and helpers
    /// </summary>
    module Utils =

        /// <summary>
        /// Use pooled item with automatic return
        /// </summary>
        let usingPooledItem (pool: MemoryPool<'T>) (action: 'T -> 'U) : 'U =
            let pooled = rent pool
            try
                action pooled.Item
            finally
                returnItem pooled

        /// <summary>
        /// Use pooled array with automatic return
        /// </summary>
        let usingPooledArray<'T> (pool: MemoryPool<'T[]>) size (action: 'T[] -> 'U) : 'U =
            let pooled, array = ArrayPool.rentArray pool size
            try
                action array
            finally
                returnItem pooled

        /// <summary>
        /// Transform pooled item
        /// </summary>
        let mapPooledItem (f: 'T -> 'U) (pooled: PooledItem<'T>) : PooledItem<'U> * 'U =
            let transformed = f pooled.Item
            // Note: This creates a new pooled item, but doesn't return the original
            // In practice, you'd want a more sophisticated approach
            pooled, transformed

        /// <summary>
        /// Create pool with automatic sizing based on usage patterns
        /// </summary>
        let createAdaptivePool factory reset initialConfig : MemoryPool<'T> =
            let mutable currentConfig = initialConfig
            let mutable pool = create currentConfig factory reset

            // In a real implementation, you'd monitor usage and adjust config
            // For now, just return the initial pool
            pool

    /// <summary>
    /// Pool serialization for persistence
    /// </summary>
    module Serialization =

        open System.IO
        open System.Text.Json

        /// <summary>
        /// Serialize pool configuration
        /// </summary>
        let serializeConfig config path =
            let json = JsonSerializer.Serialize(config)
            File.WriteAllText(path, json)

        /// <summary>
        /// Deserialize pool configuration
        /// </summary>
        let deserializeConfig path : PoolConfig =
            let json = File.ReadAllText(path)
            JsonSerializer.Deserialize<PoolConfig>(json)

        /// <summary>
        /// Save pool state for debugging
        /// </summary>
        let savePoolState pool name path =
            let stats = getStats pool
            let state = Map.ofList [
                "Name", box name
                "TotalAllocated", box stats.TotalAllocated
                "TotalReturned", box stats.TotalReturned
                "CurrentSize", box stats.CurrentSize
                "PeakSize", box stats.PeakSize
                "HitRate", box stats.HitRate
            ]
            let json = JsonSerializer.Serialize(state)
            File.WriteAllText(path, json)

    /// <summary>
    /// Pool stress testing and benchmarking
    /// </summary>
    module Benchmarking =

        /// <summary>
        /// Benchmark pool performance
        /// </summary>
        let benchmarkPool pool operationCount threadCount =
            let sw = Stopwatch.StartNew()

            let results =
                [1..threadCount]
                |> List.map (fun _ ->
                    async {
                        let mutable operations = 0
                        for _ in 1..(operationCount / threadCount) do
                            let pooled = rent pool
                            // Simulate work
                            Thread.Sleep(1)
                            returnItem pooled
                            operations <- operations + 1
                        return operations
                    })
                |> Async.Parallel
                |> Async.RunSynchronously

            sw.Stop()

            let totalOperations = Array.sum results
            let opsPerSecond = float totalOperations / sw.Elapsed.TotalSeconds

            printfn "Pool Benchmark Results:"
            printfn "  Total Operations: %d" totalOperations
            printfn "  Total Time: %.2f seconds" sw.Elapsed.TotalSeconds
            printfn "  Operations/Second: %.2f" opsPerSecond
            printfn "  Average Time/Operation: %.4f ms" (sw.Elapsed.TotalMilliseconds / float totalOperations)

            opsPerSecond

        /// <summary>
        /// Memory pressure test
        /// </summary>
        let memoryPressureTest pool maxItems =
            printfn "Starting memory pressure test..."

            let items = ResizeArray<PooledItem<obj>>()

            for i in 1..maxItems do
                let pooled = rent pool
                items.Add(pooled)

                if i % 1000 = 0 then
                    printfn "Allocated %d items, current pool size: %d" i pool.Items.Count

            // Return items gradually
            for i in 0..items.Count-1 do
                returnItem items.[i]

                if (i + 1) % 1000 = 0 then
                    printfn "Returned %d items, current pool size: %d" (i + 1) pool.Items.Count

            printfn "Memory pressure test completed"
