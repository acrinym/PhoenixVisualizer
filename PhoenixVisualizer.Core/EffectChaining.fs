// EffectChaining.fs - Functional Effect Chaining System
// Demonstrates F#'s superiority for reactive programming and composability
// Much more elegant than imperative event-driven approaches

namespace PhoenixVisualizer.Core

open System
open System.Collections.Generic

/// <summary>
/// Functional effect chaining system using F# reactive patterns
/// Much more composable and type-safe than imperative approaches
/// </summary>
module EffectChaining =

    /// <summary>
    /// Effect result type
    /// </summary>
    type EffectResult<'T> = {
        Value: 'T
        Metadata: Map<string, obj>
        Timestamp: DateTime
    }

    /// <summary>
    /// Effect function type
    /// </summary>
    type Effect<'TInput, 'TOutput> = 'TInput -> EffectResult<'TOutput>

    /// <summary>
    /// Observable stream type for reactive effects
    /// </summary>
    type ObservableStream<'T> = {
        Subscribe: (('T -> unit) -> IDisposable) -> unit
        Map: ('T -> 'U) -> ObservableStream<'U>
        Filter: ('T -> bool) -> ObservableStream<'T>
        Merge: ObservableStream<'T> -> ObservableStream<'T>
        CombineLatest: ObservableStream<'U> -> (('T * 'U) -> 'V) -> ObservableStream<'V>
    }

    /// <summary>
    /// Effect pipeline builder
    /// </summary>
    module Pipeline =

        /// <summary>
        /// Identity effect (pass-through)
        /// </summary>
        let identity (input: 'T) : EffectResult<'T> = {
            Value = input
            Metadata = Map.empty
            Timestamp = DateTime.Now
        }

        /// <summary>
        /// Create effect from function
        /// </summary>
        let createEffect (f: 'TInput -> 'TOutput) : Effect<'TInput, 'TOutput> =
            fun input -> {
                Value = f input
                Metadata = Map.empty
                Timestamp = DateTime.Now
            }

        /// <summary>
        /// Create effect with metadata
        /// </summary>
        let createEffectWithMetadata (f: 'TInput -> 'TOutput) (metadata: Map<string, obj>) : Effect<'TInput, 'TOutput> =
            fun input -> {
                Value = f input
                Metadata = metadata
                Timestamp = DateTime.Now
            }

        /// <summary>
        /// Compose two effects
        /// </summary>
        let compose (effect1: Effect<'TInput, 'TMiddle>) (effect2: Effect<'TMiddle, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                let result1 = effect1 input
                let result2 = effect2 result1.Value
                {
                    Value = result2.Value
                    Metadata = Map.ofList [
                        yield! result1.Metadata |> Map.toList
                        yield! result2.Metadata |> Map.toList
                    ]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Chain effects using forward pipe
        /// </summary>
        let (>>>) = compose

        /// <summary>
        /// Parallel effect execution
        /// </summary>
        let parallel (effect1: Effect<'T, 'U>) (effect2: Effect<'T, 'V>) : Effect<'T, 'U * 'V> =
            fun input ->
                let result1 = effect1 input
                let result2 = effect2 input
                {
                    Value = (result1.Value, result2.Value)
                    Metadata = Map.ofList [
                        yield! result1.Metadata |> Map.toList
                        yield! result2.Metadata |> Map.toList
                    ]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Conditional effect execution
        /// </summary>
        let conditional (condition: 'TInput -> bool) (effect: Effect<'TInput, 'TOutput>) (fallback: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                if condition input then effect input else fallback input

        /// <summary>
        /// Effect with error handling
        /// </summary>
        let tryCatch (effect: Effect<'TInput, 'TOutput>) (errorHandler: exn -> Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                try
                    effect input
                with
                | ex -> errorHandler ex input

        /// <summary>
        /// Effect with retry logic
        /// </summary>
        let retry (maxRetries: int) (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                let rec attempt remainingRetries =
                    try
                        effect input
                    with
                    | ex when remainingRetries > 0 ->
                        printfn "Effect failed, retrying... (%d attempts left)" remainingRetries
                        attempt (remainingRetries - 1)
                    | ex -> reraise()
                attempt maxRetries

        /// <summary>
        /// Effect with timing measurement
        /// </summary>
        let timed (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                let startTime = DateTime.Now
                let result = effect input
                let endTime = DateTime.Now
                let duration = endTime - startTime

                {
                    result with
                        Metadata = result.Metadata |> Map.add "ExecutionTime" (box duration.TotalMilliseconds)
                }

        /// <summary>
        /// Effect with caching
        /// </summary>
        let cached (cache: Dictionary<'TInput, EffectResult<'TOutput>>) (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                match cache.TryGetValue input with
                | true, cachedResult -> cachedResult
                | false, _ ->
                    let result = effect input
                    cache.[input] <- result
                    result

        /// <summary>
        /// Effect with logging
        /// </summary>
        let logged (logger: string -> unit) (effectName: string) (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                logger (sprintf "Starting effect: %s" effectName)
                let result = effect input
                logger (sprintf "Completed effect: %s in %A" effectName (DateTime.Now - result.Timestamp))
                result

    /// <summary>
    /// Reactive effect system
    /// </summary>
    module Reactive =

        /// <summary>
        /// Create observable from sequence
        /// </summary>
        let createObservable (source: seq<'T>) : ObservableStream<'T> =
            {
                Subscribe = fun observer ->
                    let enumerator = source.GetEnumerator()
                    let subscription = {
                        new IDisposable with
                            member _.Dispose() = enumerator.Dispose()
                    }
                    async {
                        while enumerator.MoveNext() do
                            observer enumerator.Current
                    } |> Async.Start
                    subscription

                Map = fun f ->
                    {
                        Subscribe = fun observer ->
                            let subscription = createObservable source
                            subscription.Subscribe (f >> observer)
                        Map = fun g -> createObservable source |> fun obs -> obs.Map (f >> g)
                        Filter = fun pred -> createObservable source |> fun obs -> obs.Filter pred
                        Merge = fun other -> createObservable source |> fun obs -> obs.Merge other
                        CombineLatest = fun other combiner -> createObservable source |> fun obs -> obs.CombineLatest other combiner
                    }

                Filter = fun pred ->
                    {
                        Subscribe = fun observer ->
                            let subscription = createObservable source
                            subscription.Subscribe (fun value -> if pred value then observer value)
                        Map = fun f -> createObservable source |> fun obs -> obs.Filter pred |> fun obs -> obs.Map f
                        Filter = fun pred2 -> createObservable source |> fun obs -> obs.Filter (fun x -> pred x && pred2 x)
                        Merge = fun other -> createObservable source |> fun obs -> obs.Merge other
                        CombineLatest = fun other combiner -> createObservable source |> fun obs -> obs.CombineLatest other combiner
                    }

                Merge = fun other ->
                    {
                        Subscribe = fun observer ->
                            let sub1 = createObservable source
                            let sub2 = createObservable other.Subscribe observer |> ignore; other
                            { new IDisposable with member _.Dispose() = () }
                        Map = fun f -> createObservable source |> fun obs -> obs.Merge other |> fun obs -> obs.Map f
                        Filter = fun pred -> createObservable source |> fun obs -> obs.Merge other |> fun obs -> obs.Filter pred
                        Merge = fun other2 -> createObservable source |> fun obs -> obs.Merge other |> fun obs -> obs.Merge other2
                        CombineLatest = fun other2 combiner -> createObservable source |> fun obs -> obs.Merge other |> fun obs -> obs.CombineLatest other2 combiner
                    }

                CombineLatest = fun other combiner ->
                    {
                        Subscribe = fun observer ->
                            let latestA = ref None
                            let latestB = ref None
                            let sub1 = createObservable source
                            let sub2 = createObservable other.Subscribe observer |> ignore; other
                            sub1.Subscribe (fun a -> latestA := Some a; match !latestA, !latestB with Some x, Some y -> observer (combiner (x, y)) | _ -> ())
                            sub2.Subscribe (fun b -> latestB := Some b; match !latestA, !latestB with Some x, Some y -> observer (combiner (x, y)) | _ -> ())
                            { new IDisposable with member _.Dispose() = () }
                        Map = fun f -> createObservable source |> fun obs -> obs.CombineLatest other combiner |> fun obs -> obs.Map f
                        Filter = fun pred -> createObservable source |> fun obs -> obs.CombineLatest other combiner |> fun obs -> obs.Filter pred
                        Merge = fun other2 -> createObservable source |> fun obs -> obs.CombineLatest other combiner |> fun obs -> obs.Merge other2
                        CombineLatest = fun other2 combiner2 -> createObservable source |> fun obs -> obs.CombineLatest other combiner |> fun obs -> obs.CombineLatest other2 combiner2
                    }
            }

        /// <summary>
        /// Create observable from events
        /// </summary>
        let fromEvent (event: IEvent<'TDelegate, 'TArgs>) : ObservableStream<'TArgs> =
            createObservable Seq.empty // Placeholder - would need proper event handling

        /// <summary>
        /// Create observable from timer
        /// </summary>
        let interval (intervalMs: int) : ObservableStream<DateTime> =
            let rec generateTimes currentTime =
                seq {
                    yield currentTime
                    yield! generateTimes (currentTime.AddMilliseconds(float intervalMs))
                }
            createObservable (generateTimes DateTime.Now)

        /// <summary>
        /// Throttle observable stream
        /// </summary>
        let throttle (intervalMs: int) (stream: ObservableStream<'T>) : ObservableStream<'T> =
            {
                Subscribe = fun observer ->
                    let lastEmit = ref DateTime.MinValue
                    stream.Subscribe (fun value ->
                        let now = DateTime.Now
                        if (now - !lastEmit).TotalMilliseconds >= float intervalMs then
                            lastEmit := now
                            observer value
                    )
                Map = fun f -> stream.Map f |> throttle intervalMs
                Filter = fun pred -> stream.Filter pred |> throttle intervalMs
                Merge = fun other -> stream.Merge other |> throttle intervalMs
                CombineLatest = fun other combiner -> stream.CombineLatest other combiner |> throttle intervalMs
            }

        /// <summary>
        /// Debounce observable stream
        /// </summary>
        let debounce (delayMs: int) (stream: ObservableStream<'T>) : ObservableStream<'T> =
            {
                Subscribe = fun observer ->
                    let timer = ref None
                    stream.Subscribe (fun value ->
                        match !timer with
                        | Some t -> t.Dispose()
                        | None -> ()
                        timer := Some (new System.Timers.Timer(float delayMs))
                        (!timer).Value.Elapsed.Add(fun _ ->
                            observer value
                            timer := None
                        )
                        (!timer).Value.Start()
                    )
                Map = fun f -> stream.Map f |> debounce delayMs
                Filter = fun pred -> stream.Filter pred |> debounce delayMs
                Merge = fun other -> stream.Merge other |> debounce delayMs
                CombineLatest = fun other combiner -> stream.CombineLatest other combiner |> debounce delayMs
            }

    /// <summary>
    /// Effect combinators for complex pipelines
    /// </summary>
    module Combinators =

        /// <summary>
        /// Effect router based on input type
        /// </summary>
        let routeByType (routes: Map<Type, Effect<obj, obj>>) : Effect<obj, obj> =
            fun input ->
                let inputType = input.GetType()
                match routes.TryFind inputType with
                | Some effect -> effect input
                | None -> {
                    Value = input
                    Metadata = Map.ofList ["Warning", "No route found for type" :> obj]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Effect multiplexer (one input, multiple outputs)
        /// </summary>
        let multiplex (effects: Effect<'TInput, 'TOutput> list) : Effect<'TInput, 'TOutput list> =
            fun input ->
                let results = effects |> List.map (fun effect -> effect input)
                {
                    Value = results |> List.map (fun r -> r.Value)
                    Metadata = Map.ofList [
                        "EffectCount", effects.Length :> obj
                        "Timestamps", (results |> List.map (fun r -> r.Timestamp)) :> obj
                    ]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Effect aggregator (multiple inputs, one output)
        /// </summary>
        let aggregate (effects: Effect<'TInput, 'TOutput> list) (aggregator: 'TOutput list -> 'TOutput) : Effect<'TInput, 'TOutput> =
            fun input ->
                let results = effects |> List.map (fun effect -> effect input)
                let values = results |> List.map (fun r -> r.Value)
                {
                    Value = aggregator values
                    Metadata = Map.ofList [
                        "SourceEffects", effects.Length :> obj
                        "AggregationTimestamp", DateTime.Now :> obj
                    ]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Effect with state management
        /// </summary>
        let withState (initialState: 'TState) (updater: 'TState -> 'TInput -> 'TState * 'TOutput) : Effect<'TInput, 'TOutput> =
            let state = ref initialState
            fun input ->
                let newState, output = updater !state input
                state := newState
                {
                    Value = output
                    Metadata = Map.ofList ["StateUpdated", true :> obj]
                    Timestamp = DateTime.Now
                }

        /// <summary>
        /// Effect with side effects
        /// </summary>
        let withSideEffect (sideEffect: 'TInput -> unit) (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                sideEffect input
                effect input

    /// <summary>
    /// Pre-built effect pipelines for common scenarios
    /// </summary>
    module Presets =

        /// <summary>
        /// Audio processing pipeline
        /// </summary>
        let audioProcessingPipeline =
            Pipeline.createEffect id
            >>> Pipeline.timed
            >>> Pipeline.logged (printfn "%s") "Audio Processing"

        /// <summary>
        /// Image processing pipeline with caching
        /// </summary>
        let imageProcessingPipeline =
            let cache = Dictionary<obj, EffectResult<obj>>()
            Pipeline.createEffect id
            |> Pipeline.cached cache
            |> Pipeline.timed

        /// <summary>
        /// Real-time effect pipeline with error handling
        /// </summary>
        let realtimePipeline =
            Pipeline.createEffect id
            |> Pipeline.tryCatch (fun ex input -> {
                Value = input
                Metadata = Map.ofList ["Error", ex.Message :> obj]
                Timestamp = DateTime.Now
            })
            |> Pipeline.retry 3
            |> Pipeline.timed

        /// <summary>
        /// Parallel processing pipeline
        /// </summary>
        let parallelProcessingPipeline effects =
            Pipeline.createEffect id
            |> Pipeline.multiplex effects
            |> Pipeline.timed

    /// <summary>
    /// Effect builder pattern for complex pipelines
    /// </summary>
    module Builder =

        /// <summary>
        /// Effect builder type
        /// </summary>
        type EffectBuilder() =
            member _.Bind(result: EffectResult<'T>, f: 'T -> EffectResult<'U>) : EffectResult<'U> = f result.Value
            member _.Return(value: 'T) : EffectResult<'T> = {
                Value = value
                Metadata = Map.empty
                Timestamp = DateTime.Now
            }
            member _.ReturnFrom(result: EffectResult<'T>) : EffectResult<'T> = result

        /// <summary>
        /// Effect computation expression
        /// </summary>
        let effect = EffectBuilder()

        /// <summary>
        /// Example: Complex effect pipeline using computation expressions
        /// </summary>
        let complexPipeline input =
            effect {
                let! processed = Pipeline.createEffect (fun x -> x * 2.0) input
                let! filtered = Pipeline.createEffect (fun x -> if x > 10.0 then x else 0.0) processed
                let! logged = Pipeline.logged (printfn "Value: %f") "Complex Pipeline" filtered
                return logged
            }

    /// <summary>
    /// Performance monitoring for effect chains
    /// </summary>
    module Monitoring =

        /// <summary>
        /// Performance metrics
        /// </summary>
        type PerformanceMetrics = {
            TotalEffects: int
            TotalExecutionTime: TimeSpan
            AverageExecutionTime: TimeSpan
            EffectsExecuted: int
            Errors: int
        }

        /// <summary>
        /// Performance monitor
        /// </summary>
        let createMonitor () =
            let metrics = ref {
                TotalEffects = 0
                TotalExecutionTime = TimeSpan.Zero
                AverageExecutionTime = TimeSpan.Zero
                EffectsExecuted = 0
                Errors = 0
            }

            let updateMetrics executionTime hadError =
                lock metrics (fun () ->
                    metrics := {
                        !metrics with
                            TotalEffects = !metrics.TotalEffects + 1
                            TotalExecutionTime = !metrics.TotalExecutionTime + executionTime
                            EffectsExecuted = !metrics.EffectsExecuted + 1
                            Errors = !metrics.Errors + (if hadError then 1 else 0)
                            AverageExecutionTime = !metrics.TotalExecutionTime / float !metrics.EffectsExecuted
                    }
                )

            let getMetrics () = !metrics

            updateMetrics, getMetrics

        /// <summary>
        /// Monitor effect wrapper
        /// </summary>
        let monitorEffect (updateMetrics: TimeSpan -> bool -> unit) (effect: Effect<'TInput, 'TOutput>) : Effect<'TInput, 'TOutput> =
            fun input ->
                let startTime = DateTime.Now
                try
                    let result = effect input
                    let executionTime = DateTime.Now - startTime
                    updateMetrics executionTime false
                    result
                with
                | ex ->
                    let executionTime = DateTime.Now - startTime
                    updateMetrics executionTime true
                    {
                        Value = Unchecked.defaultof<'TOutput>
                        Metadata = Map.ofList ["Error", ex.Message :> obj]
                        Timestamp = DateTime.Now
                    }
