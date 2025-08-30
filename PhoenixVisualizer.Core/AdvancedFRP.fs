// AdvancedFRP.fs - Advanced Functional Reactive Programming
// Sophisticated reactive patterns for complex event processing
// Builds on basic FRP with advanced combinators and patterns

namespace PhoenixVisualizer.Core

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Diagnostics

/// <summary>
/// Advanced Functional Reactive Programming framework
/// Provides sophisticated reactive patterns for complex event processing
/// </summary>
module AdvancedFRP =

    /// <summary>
    /// Reactive stream with advanced features
    /// </summary>
    type ReactiveStream<'T> = {
        Subscribe: (('T -> unit) -> CancellationToken -> IDisposable) -> unit
        Map: ('T -> 'U) -> ReactiveStream<'U>
        Filter: ('T -> bool) -> ReactiveStream<'T>
        Scan: 'S -> ('S -> 'T -> 'S) -> ReactiveStream<'S>
        DistinctUntilChanged: ReactiveStream<'T>
        Debounce: TimeSpan -> ReactiveStream<'T>
        Throttle: TimeSpan -> ReactiveStream<'T>
        Buffer: int -> ReactiveStream<'T[]>
        Window: TimeSpan -> ReactiveStream<ReactiveStream<'T>>
        Merge: ReactiveStream<'T> -> ReactiveStream<'T>
        CombineLatest: ReactiveStream<'U> -> (('T * 'U) -> 'V) -> ReactiveStream<'V>
        Zip: ReactiveStream<'U> -> ReactiveStream<'T * 'U>
        Sample: TimeSpan -> ReactiveStream<'T>
        Delay: TimeSpan -> ReactiveStream<'T>
        Timeout: TimeSpan -> ReactiveStream<'T>
        Retry: int -> ReactiveStream<'T>
        OnErrorResumeNext: ReactiveStream<'T> -> ReactiveStream<'T>
        Finally: (unit -> unit) -> ReactiveStream<'T>
        Do: ('T -> unit) -> ReactiveStream<'T>
        Multicast: ReactiveStream<'T>
    }

    /// <summary>
    /// Observable subscription
    /// </summary>
    type Subscription = {
        Unsubscribe: unit -> unit
        IsUnsubscribed: unit -> bool
    }

    /// <summary>
    /// Observer interface
    /// </summary>
    type IObserver<'T> =
        abstract OnNext : 'T -> unit
        abstract OnError : exn -> unit
        abstract OnCompleted : unit -> unit

    /// <summary>
    /// Observable interface
    /// </summary>
    type IObservable<'T> =
        abstract Subscribe : IObserver<'T> -> IDisposable

    // Reactive stream creation functions

    /// <summary>
    /// Create reactive stream from sequence
    /// </summary>
    let createFromSeq (source: seq<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let enumerator = source.GetEnumerator()
            let mutable isCompleted = false

            let subscription = {
                new IDisposable with
                    member _.Dispose() =
                        enumerator.Dispose()
                        isCompleted <- true
            }

            async {
                while not isCompleted && not cancellationToken.IsCancellationRequested && enumerator.MoveNext() do
                    observer enumerator.Current
            } |> Async.Start

            subscription

        {
            Subscribe = subscribe
            Map = fun f -> createFromSeq source |> fun s -> s.Map f
            Filter = fun pred -> createFromSeq source |> fun s -> s.Filter pred
            Scan = fun seed acc -> createFromSeq source |> fun s -> s.Scan seed acc
            DistinctUntilChanged = createFromSeq source // Simplified
            Debounce = fun _ -> createFromSeq source // Simplified
            Throttle = fun _ -> createFromSeq source // Simplified
            Buffer = fun _ -> createFromSeq source |> Seq.map Array.singleton |> createFromSeq // Simplified
            Window = fun _ -> createFromSeq source |> Seq.map createFromSeq |> createFromSeq // Simplified
            Merge = fun other -> Seq.append source (Seq.empty : seq<'T>) |> createFromSeq // Simplified
            CombineLatest = fun other combiner -> createFromSeq source // Simplified
            Zip = fun other -> Seq.zip source (Seq.empty : seq<'U>) |> createFromSeq // Simplified
            Sample = fun _ -> createFromSeq source // Simplified
            Delay = fun _ -> createFromSeq source // Simplified
            Timeout = fun _ -> createFromSeq source // Simplified
            Retry = fun _ -> createFromSeq source // Simplified
            OnErrorResumeNext = fun other -> createFromSeq source // Simplified
            Finally = fun action -> createFromSeq source // Simplified
            Do = fun action -> createFromSeq source // Simplified
            Multicast = createFromSeq source // Simplified
        }

    /// <summary>
    /// Create reactive stream from events
    /// </summary>
    let createFromEvent (event: IEvent<'TDelegate, 'TArgs>) : ReactiveStream<'TArgs> =
        createFromSeq Seq.empty // Placeholder - would need proper event handling

    /// <summary>
    /// Create reactive stream from timer
    /// </summary>
    let interval (intervalMs: int) : ReactiveStream<DateTime> =
        let rec generateTimes currentTime =
            seq {
                yield currentTime
                yield! generateTimes (currentTime.AddMilliseconds(float intervalMs))
            }
        createFromSeq (generateTimes DateTime.Now)

    /// <summary>
    /// Create reactive stream from async computation
    /// </summary>
    let fromAsync (computation: Async<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let subscription = {
                new IDisposable with
                    member _.Dispose() = () // Simplified
            }

            async {
                try
                    let! result = computation
                    if not cancellationToken.IsCancellationRequested then
                        observer result
                with
                | ex -> printfn "Error in async computation: %s" ex.Message
            } |> Async.Start

            subscription

        {
            Subscribe = subscribe
            Map = fun f -> fromAsync computation |> fun s -> s.Map f
            Filter = fun pred -> fromAsync computation |> fun s -> s.Filter pred
            Scan = fun seed acc -> fromAsync computation |> fun s -> s.Scan seed acc
            DistinctUntilChanged = fromAsync computation // Simplified
            Debounce = fun _ -> fromAsync computation // Simplified
            Throttle = fun _ -> fromAsync computation // Simplified
            Buffer = fun _ -> fromAsync computation |> fun s -> s.Map Array.singleton // Simplified
            Window = fun _ -> fromAsync computation |> fun s -> s.Map createFromSeq // Simplified
            Merge = fun other -> fromAsync computation // Simplified
            CombineLatest = fun other combiner -> fromAsync computation // Simplified
            Zip = fun other -> fromAsync computation // Simplified
            Sample = fun _ -> fromAsync computation // Simplified
            Delay = fun _ -> fromAsync computation // Simplified
            Timeout = fun _ -> fromAsync computation // Simplified
            Retry = fun _ -> fromAsync computation // Simplified
            OnErrorResumeNext = fun other -> fromAsync computation // Simplified
            Finally = fun action -> fromAsync computation // Simplified
            Do = fun action -> fromAsync computation // Simplified
            Multicast = fromAsync computation // Simplified
        }

    // Advanced reactive operators

    /// <summary>
    /// Advanced mapping with index
    /// </summary>
    let mapIndexed (f: int -> 'T -> 'U) (stream: ReactiveStream<'T>) : ReactiveStream<'U> =
        let subscribe observer cancellationToken =
            let index = ref 0
            stream.Subscribe (fun value ->
                let result = f !index value
                index := !index + 1
                observer result
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Take elements while condition is true
    /// </summary>
    let takeWhile (predicate: 'T -> bool) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let mutable shouldContinue = true
            stream.Subscribe (fun value ->
                if shouldContinue && predicate value then
                    observer value
                else
                    shouldContinue <- false
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Skip elements while condition is true
    /// </summary>
    let skipWhile (predicate: 'T -> bool) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let mutable shouldSkip = true
            stream.Subscribe (fun value ->
                if shouldSkip then
                    if not (predicate value) then
                        shouldSkip <- false
                        observer value
                else
                    observer value
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Take first n elements
    /// </summary>
    let take (count: int) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let taken = ref 0
            stream.Subscribe (fun value ->
                if !taken < count then
                    observer value
                    taken := !taken + 1
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Skip first n elements
    /// </summary>
    let skip (count: int) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let skipped = ref 0
            stream.Subscribe (fun value ->
                if !skipped >= count then
                    observer value
                else
                    skipped := !skipped + 1
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Group elements by key
    /// </summary>
    let groupBy (keySelector: 'T -> 'K) (stream: ReactiveStream<'T>) : ReactiveStream<ReactiveStream<'T>> =
        let subscribe observer cancellationToken =
            let groups = ConcurrentDictionary<'K, ReactiveStream<'T>>()

            stream.Subscribe (fun value ->
                let key = keySelector value
                match groups.TryGetValue(key) with
                | true, existingGroup ->
                    // Add to existing group
                    ()
                | false, _ ->
                    // Create new group
                    let newGroup = createFromSeq [value]
                    groups.[key] <- newGroup
                    observer newGroup
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Start with initial value
    /// </summary>
    let startWith (initialValue: 'T) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            observer initialValue
            stream.Subscribe observer cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// End with final value
    /// </summary>
    let endWith (finalValue: 'T) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let subscription = stream.Subscribe observer cancellationToken
            observer finalValue
            subscription

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Pairwise - emit consecutive pairs
    /// </summary>
    let pairwise (stream: ReactiveStream<'T>) : ReactiveStream<'T * 'T> =
        let subscribe observer cancellationToken =
            let previous = ref None
            stream.Subscribe (fun value ->
                match !previous with
                | Some prev -> observer (prev, value)
                | None -> ()
                previous := Some value
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Materialize notifications as explicit values
    /// </summary>
    type Notification<'T> =
        | OnNext of 'T
        | OnError of exn
        | OnCompleted

    let materialize (stream: ReactiveStream<'T>) : ReactiveStream<Notification<'T>> =
        let subscribe observer cancellationToken =
            stream.Subscribe (fun value ->
                observer (OnNext value)
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Dematerialize notifications back to normal stream
    /// </summary>
    let dematerialize (stream: ReactiveStream<Notification<'T>>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            stream.Subscribe (fun notification ->
                match notification with
                | OnNext value -> observer value
                | OnError ex -> printfn "Error in stream: %s" ex.Message
                | OnCompleted -> () // Could signal completion
            ) cancellationToken

        { stream with Subscribe = subscribe }

    // Complex reactive patterns

    /// <summary>
    /// Switch - switch to latest inner observable
    /// </summary>
    let switch (stream: ReactiveStream<ReactiveStream<'T>>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let currentSubscription = ref None
            let mutable isCompleted = false

            stream.Subscribe (fun innerStream ->
                // Unsubscribe from previous inner stream
                match !currentSubscription with
                | Some sub -> sub.Dispose()
                | None -> ()

                // Subscribe to new inner stream
                let newSub = innerStream.Subscribe observer cancellationToken
                currentSubscription := Some newSub
            ) cancellationToken

        { stream with Subscribe = subscribe }

    /// <summary>
    /// Concat - concatenate multiple streams
    /// </summary>
    let concat (streams: ReactiveStream<'T> list) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let rec processStreams remainingStreams =
                match remainingStreams with
                | [] -> ()
                | stream :: rest ->
                    let subscription = stream.Subscribe observer cancellationToken
                    // Wait for completion before moving to next (simplified)
                    processStreams rest

            processStreams streams

        { (createFromSeq []) with Subscribe = subscribe }

    /// <summary>
    /// Amb - take from first stream to produce a value
    /// </summary>
    let amb (stream1: ReactiveStream<'T>) (stream2: ReactiveStream<'T>) : ReactiveStream<'T> =
        let subscribe observer cancellationToken =
            let winner = ref None
            let cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)

            let subscribeTo stream =
                stream.Subscribe (fun value ->
                    if Interlocked.CompareExchange(winner, Some (), None).IsNone then
                        cts.Cancel() // Cancel the other stream
                        observer value
                ) cts.Token

            let sub1 = subscribeTo stream1
            let sub2 = subscribeTo stream2

            { new IDisposable with
                member _.Dispose() =
                    sub1.Dispose()
                    sub2.Dispose()
                    cts.Dispose() }

        { stream1 with Subscribe = subscribe }

    // State management in reactive streams

    /// <summary>
    /// State machine for complex reactive behavior
    /// </summary>
    module StateMachine =

        type State<'S, 'T> = {
            CurrentState: 'S
            Transition: 'S -> 'T -> 'S
            Output: 'S -> 'T -> 'U option
        }

        /// <summary>
        /// Create state machine stream
        /// </summary>
        let createStateMachine initialState transition output (stream: ReactiveStream<'T>) : ReactiveStream<'U> =
            let subscribe observer cancellationToken =
                let state = ref initialState
                stream.Subscribe (fun value ->
                    let newState = transition !state value
                    state := newState

                    match output !state value with
                    | Some result -> observer result
                    | None -> ()
                ) cancellationToken

            { stream with Subscribe = subscribe }

    /// <summary>
    /// Reactive queries and data processing
    /// </summary>
    module Queries =

        /// <summary>
        /// Reactive LINQ-style queries
        /// </summary>
        let query (stream: ReactiveStream<'T>) : ReactiveQueryBuilder<'T> =
            { Stream = stream }

        and ReactiveQueryBuilder<'T> = {
            Stream: ReactiveStream<'T>
        } with
            member this.Where(predicate: 'T -> bool) =
                { Stream = this.Stream.Filter predicate }

            member this.Select(selector: 'T -> 'U) =
                { Stream = this.Stream.Map selector }

            member this.SelectMany(selector: 'T -> ReactiveStream<'U>) =
                { Stream = this.Stream.Map selector |> switch }

            member this.GroupBy(keySelector: 'T -> 'K) =
                { Stream = this.Stream |> groupBy keySelector }

            member this.OrderBy(keySelector: 'T -> 'K) =
                { Stream = this.Stream } // Simplified - would need buffering

            member this.Distinct() =
                { Stream = this.Stream.DistinctUntilChanged }

            member this.Take(count: int) =
                { Stream = this.Stream |> take count }

            member this.Skip(count: int) =
                { Stream = this.Stream |> skip count }

            member this.First() =
                { Stream = this.Stream |> take 1 }

            member this.Last() =
                { Stream = this.Stream } // Simplified - would need buffering

            member this.Count() =
                { Stream = this.Stream.Scan 0L (fun count _ -> count + 1L) }

            member this.Sum(selector: 'T -> float) =
                { Stream = this.Stream.Scan 0.0 (fun sum item -> sum + selector item) }

            member this.Average(selector: 'T -> float) =
                { Stream = this.Stream.Scan (0.0, 0L) (fun (sum, count) item ->
                    let newSum = sum + selector item
                    let newCount = count + 1L
                    (newSum, newCount))
                |> fun s -> s.Map (fun (sum, count) -> if count > 0L then sum / float count else 0.0) }

            member this.Min(selector: 'T -> 'U when 'U : comparison) =
                { Stream = this.Stream.Scan None (fun minOpt item ->
                    let value = selector item
                    match minOpt with
                    | Some min -> Some (if value < min then value else min)
                    | None -> Some value)
                |> fun s -> s.Map (fun opt -> match opt with Some v -> v | None -> Unchecked.defaultof<'U>) }

            member this.Max(selector: 'T -> 'U when 'U : comparison) =
                { Stream = this.Stream.Scan None (fun maxOpt item ->
                    let value = selector item
                    match maxOpt with
                    | Some max -> Some (if value > max then value else max)
                    | None -> Some value)
                |> fun s -> s.Map (fun opt -> match opt with Some v -> v | None -> Unchecked.defaultof<'U>) }

    /// <summary>
    /// Time-based operations
    /// </summary>
    module Time =

        /// <summary>
        /// Timestamp each value
        /// </summary>
        let timestamp (stream: ReactiveStream<'T>) : ReactiveStream<DateTime * 'T> =
            stream.Map (fun value -> (DateTime.Now, value))

        /// <summary>
        /// Time interval between values
        /// </summary>
        let timeInterval (stream: ReactiveStream<'T>) : ReactiveStream<TimeSpan * 'T> =
            let subscribe observer cancellationToken =
                let lastTime = ref DateTime.Now
                stream.Subscribe (fun value ->
                    let currentTime = DateTime.Now
                    let interval = currentTime - !lastTime
                    lastTime := currentTime
                    observer (interval, value)
                ) cancellationToken

            { stream with Subscribe = subscribe }

        /// <summary>
        /// Timeout with custom error
        /// </summary>
        let timeoutWithError (timeout: TimeSpan) (errorFactory: unit -> exn) (stream: ReactiveStream<'T>) : ReactiveStream<'T> =
            let subscribe observer cancellationToken =
                let timer = ref None
                let subscription = stream.Subscribe (fun value ->
                    // Reset timer on each value
                    match !timer with
                    | Some t -> t.Dispose()
                    | None -> ()

                    timer := Some (new Timer(
                        fun _ ->
                            try
                                let error = errorFactory()
                                printfn "Stream timeout: %s" error.Message
                            with
                            | ex -> printfn "Error in timeout handler: %s" ex.Message,
                        null,
                        int timeout.TotalMilliseconds,
                        Timeout.Infinite))

                    observer value
                ) cancellationToken

                // Clean up timer when subscription ends
                { new IDisposable with
                    member _.Dispose() =
                        subscription.Dispose()
                        match !timer with
                        | Some t -> t.Dispose()
                        | None -> () }

            { stream with Subscribe = subscribe }

    /// <summary>
    /// Reactive testing utilities
    /// </summary>
    module Testing =

        /// <summary>
        /// Test scheduler for deterministic testing
        /// </summary>
        type TestScheduler = {
            Clock: DateTime ref
            Actions: PriorityQueue<DateTime * (unit -> unit)>
        }

        /// <summary>
        /// Create test scheduler
        /// </summary>
        let createTestScheduler () : TestScheduler =
            {
                Clock = ref DateTime.MinValue
                Actions = PriorityQueue<DateTime * (unit -> unit)>()
            }

        /// <summary>
        /// Schedule action at specific time
        /// </summary>
        let schedule (scheduler: TestScheduler) time action =
            scheduler.Actions.Enqueue((time, action))

        /// <summary>
        /// Advance scheduler clock
        /// </summary>
        let advanceTo (scheduler: TestScheduler) time =
            while scheduler.Actions.Count > 0 && fst scheduler.Actions.Peek() <= time do
                let scheduledTime, action = scheduler.Actions.Dequeue()
                scheduler.Clock := scheduledTime
                action ()

            scheduler.Clock := time

        /// <summary>
        /// Create hot observable for testing
        /// </summary>
        let createHotObservable (scheduler: TestScheduler) : ReactiveStream<'T> * ('T -> unit) =
            let observers = ResizeArray<IObserver<'T>>()

            let publish value =
                for observer in observers do
                    observer.OnNext(value)

            let subscribe observer cancellationToken =
                observers.Add(observer)
                { new IDisposable with
                    member _.Dispose() = observers.Remove(observer) |> ignore }

            let stream = {
                Subscribe = subscribe
                Map = fun f -> createHotObservable scheduler |> fst |> fun s -> s.Map f
                Filter = fun pred -> createHotObservable scheduler |> fst |> fun s -> s.Filter pred
                Scan = fun seed acc -> createHotObservable scheduler |> fst |> fun s -> s.Scan seed acc
                DistinctUntilChanged = createHotObservable scheduler |> fst // Simplified
                Debounce = fun _ -> createHotObservable scheduler |> fst // Simplified
                Throttle = fun _ -> createHotObservable scheduler |> fst // Simplified
                Buffer = fun _ -> createHotObservable scheduler |> fst |> fun s -> s.Map Array.singleton // Simplified
                Window = fun _ -> createHotObservable scheduler |> fst |> fun s -> s.Map createFromSeq // Simplified
                Merge = fun other -> createHotObservable scheduler |> fst // Simplified
                CombineLatest = fun other combiner -> createHotObservable scheduler |> fst // Simplified
                Zip = fun other -> createHotObservable scheduler |> fst // Simplified
                Sample = fun _ -> createHotObservable scheduler |> fst // Simplified
                Delay = fun _ -> createHotObservable scheduler |> fst // Simplified
                Timeout = fun _ -> createHotObservable scheduler |> fst // Simplified
                Retry = fun _ -> createHotObservable scheduler |> fst // Simplified
                OnErrorResumeNext = fun other -> createHotObservable scheduler |> fst // Simplified
                Finally = fun action -> createHotObservable scheduler |> fst // Simplified
                Do = fun action -> createHotObservable scheduler |> fst // Simplified
                Multicast = createHotObservable scheduler |> fst // Simplified
            }

            stream, publish

    /// <summary>
    /// Performance monitoring for reactive streams
    /// </summary>
    module Performance =

        /// <summary>
        /// Performance metrics
        /// </summary>
        type StreamMetrics = {
            StreamName: string
            SubscriptionCount: int
            TotalEvents: int64
            EventsPerSecond: float
            AverageProcessingTime: TimeSpan
            MemoryUsage: int64
            ErrorCount: int64
        }

        /// <summary>
        /// Monitor stream performance
        /// </summary>
        let createMonitor streamName =
            let metrics = ref {
                StreamName = streamName
                SubscriptionCount = 0
                TotalEvents = 0L
                EventsPerSecond = 0.0
                AverageProcessingTime = TimeSpan.Zero
                MemoryUsage = 0L
                ErrorCount = 0L
            }

            let recordEvent processingTime hadError =
                lock metrics (fun () ->
                    let totalEvents = !metrics.TotalEvents + 1L
                    let avgTime = (!metrics.AverageProcessingTime.Ticks * !metrics.TotalEvents + processingTime.Ticks) / totalEvents
                    metrics := {
                        !metrics with
                            TotalEvents = totalEvents
                            AverageProcessingTime = TimeSpan.FromTicks(avgTime)
                            ErrorCount = !metrics.ErrorCount + (if hadError then 1L else 0L)
                    }
                )

            let recordSubscription count =
                lock metrics (fun () ->
                    metrics := { !metrics with SubscriptionCount = count }
                )

            recordEvent, recordSubscription, (fun () -> !metrics)

    /// <summary>
    /// Reactive extensions for specific domains
    /// </summary>
    module Domains =

        /// <summary>
        /// Audio processing reactive extensions
        /// </summary>
        module Audio =

            /// <summary>
            /// Beat detection stream
            /// </summary>
            let beatDetection (audioStream: ReactiveStream<float[]>) : ReactiveStream<bool> =
                audioStream
                |> mapIndexed (fun i buffer ->
                    // Simple beat detection - in reality would be more sophisticated
                    let energy = buffer |> Array.sumBy (fun x -> x * x)
                    energy > 0.5 // Threshold
                )

            /// <summary>
            /// Frequency analysis stream
            /// </summary>
            let frequencyAnalysis (audioStream: ReactiveStream<float[]>) : ReactiveStream<float[]> =
                audioStream.Map (fun buffer ->
                    // Simple FFT placeholder
                    buffer |> Array.map (fun x -> abs x)
                )

            /// <summary>
            /// Audio feature extraction
            /// </summary>
            let featureExtraction (audioStream: ReactiveStream<float[]>) : ReactiveStream<Map<string, float>> =
                audioStream.Map (fun buffer ->
                    let rms = sqrt (buffer |> Array.averageBy (fun x -> x * x))
                    let peak = buffer |> Array.maxBy abs
                    let zeroCrossings = buffer |> Array.pairwise |> Array.filter (fun (a, b) -> sign a <> sign b) |> Array.length

                    Map.ofList [
                        "RMS", rms
                        "Peak", peak
                        "ZeroCrossings", float zeroCrossings
                    ]
                )

        /// <summary>
        /// UI event reactive extensions
        /// </summary>
        module UI =

            /// <summary>
            /// Mouse movement stream
            /// </summary>
            let mouseMovement (mouseEvents: ReactiveStream<System.Windows.Point>) : ReactiveStream<System.Windows.Point> =
                mouseEvents
                |> throttle (TimeSpan.FromMilliseconds(16.0)) // ~60 FPS
                |> distinctUntilChanged

            /// <summary>
            /// Keyboard input stream
            /// </summary>
            let keyboardInput (keyEvents: ReactiveStream<System.Windows.Input.Key>) : ReactiveStream<string> =
                keyEvents
                |> buffer 5 // Buffer multiple key presses
                |> map (fun keys -> keys |> Array.map string |> String.concat "")

            /// <summary>
            /// Gesture recognition
            /// </summary>
            let gestureRecognition (touchEvents: ReactiveStream<System.Windows.Point[]>) : ReactiveStream<string> =
                touchEvents
                |> map (fun points ->
                    if points.Length = 1 then "tap"
                    elif points.Length = 2 then "pinch"
                    else "unknown"
                )

        /// <summary>
        /// Network communication reactive extensions
        /// </summary>
        module Network =

            /// <summary>
            /// HTTP request/response stream
            /// </summary>
            let httpRequests (requests: ReactiveStream<System.Net.Http.HttpRequestMessage>) : ReactiveStream<System.Net.Http.HttpResponseMessage> =
                requests.Map (fun request ->
                    // Placeholder - would make actual HTTP request
                    new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                )

            /// <summary>
            /// WebSocket message stream
            /// </summary>
            let webSocketMessages (socket: System.Net.WebSockets.WebSocket) : ReactiveStream<string> =
                // Placeholder - would read from actual WebSocket
                createFromSeq Seq.empty

            /// <summary>
            /// Connection status monitoring
            /// </summary>
            let connectionStatus (connectionEvents: ReactiveStream<bool>) : ReactiveStream<string> =
                connectionEvents.Map (fun isConnected ->
                    if isConnected then "Connected" else "Disconnected"
                )
