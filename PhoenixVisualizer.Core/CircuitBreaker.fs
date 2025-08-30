// CircuitBreaker.fs - Circuit Breaker Pattern for Error Handling and Recovery
// Functional implementation of circuit breaker with configurable policies
// Prevents cascading failures and enables graceful degradation

namespace PhoenixVisualizer.Core

open System
open System.Threading
open System.Collections.Concurrent

/// <summary>
/// Circuit breaker pattern implementation using functional programming
/// Provides resilience against cascading failures with automatic recovery
/// </summary>
module CircuitBreaker =

    /// <summary>
    /// Circuit breaker states
    /// </summary>
    type CircuitState =
        | Closed // Normal operation
        | Open // Circuit is open, failing fast
        | HalfOpen // Testing if service has recovered

    /// <summary>
    /// Failure reason
    /// </summary>
    type FailureReason =
        | Exception of exn
        | Timeout
        | Custom of string

    /// <summary>
    /// Circuit breaker statistics
    /// </summary>
    type CircuitStats = {
        TotalRequests: int64
        SuccessfulRequests: int64
        FailedRequests: int64
        CircuitOpenCount: int64
        LastFailureTime: DateTime option
        LastSuccessTime: DateTime option
        AverageResponseTime: TimeSpan
    }

    /// <summary>
    /// Circuit breaker configuration
    /// </summary>
    type CircuitConfig = {
        FailureThreshold: int // Number of failures before opening
        RecoveryTimeout: TimeSpan // Time to wait before trying HalfOpen
        SuccessThreshold: int // Number of successes needed to close circuit
        Timeout: TimeSpan // Request timeout
        MonitoringEnabled: bool
    }

    /// <summary>
    /// Circuit breaker with configurable policies
    /// </summary>
    type CircuitBreaker = {
        Config: CircuitConfig
        State: CircuitState ref
        FailureCount: int ref
        SuccessCount: int ref
        LastFailureTime: DateTime option ref
        Stats: CircuitStats ref
        Fallback: (FailureReason -> 'TResult) option
        OnStateChange: (CircuitState -> CircuitState -> unit) option
        OnFailure: (FailureReason -> unit) option
        OnSuccess: (unit -> unit) option
    }

    /// <summary>
    /// Result of a circuit breaker operation
    /// </summary>
    type CircuitResult<'T> =
        | Success of 'T
        | Failure of FailureReason
        | CircuitOpen
        | Timeout

    // Circuit breaker state management

    /// <summary>
    /// Check if circuit should transition to open
    /// </summary>
    let shouldOpenCircuit config failureCount =
        failureCount >= config.FailureThreshold

    /// <summary>
    /// Check if circuit should attempt recovery
    /// </summary>
    let shouldAttemptRecovery config lastFailureTime =
        match lastFailureTime with
        | Some time -> DateTime.Now - time >= config.RecoveryTimeout
        | None -> false

    /// <summary>
    /// Check if circuit should close based on success threshold
    /// </summary>
    let shouldCloseCircuit config successCount =
        successCount >= config.SuccessThreshold

    /// <summary>
    /// Update circuit state
    /// </summary>
    let updateState circuit newState =
        let oldState = !circuit.State
        circuit.State := newState

        // Notify state change
        match circuit.OnStateChange with
        | Some callback -> callback oldState newState
        | None -> ()

        // Update stats
        if circuit.Config.MonitoringEnabled then
            match newState with
            | Open ->
                circuit.Stats := { !circuit.Stats with CircuitOpenCount = !circuit.Stats.CircuitOpenCount + 1L }
            | _ -> ()

    /// <summary>
    /// Record failure
    /// </summary>
    let recordFailure circuit reason =
        circuit.FailureCount := !circuit.FailureCount + 1
        circuit.LastFailureTime := Some DateTime.Now

        if circuit.Config.MonitoringEnabled then
            circuit.Stats := {
                !circuit.Stats with
                    FailedRequests = !circuit.Stats.FailedRequests + 1L
                    LastFailureTime = Some DateTime.Now
            }

        // Notify failure
        match circuit.OnFailure with
        | Some callback -> callback reason
        | None -> ()

        // Check if should open circuit
        if shouldOpenCircuit circuit.Config !circuit.FailureCount then
            updateState circuit Open

    /// <summary>
    /// Record success
    /// </summary>
    let recordSuccess circuit =
        circuit.SuccessCount := !circuit.SuccessCount + 1
        circuit.FailureCount := 0 // Reset failure count on success

        if circuit.Config.MonitoringEnabled then
            circuit.Stats := {
                !circuit.Stats with
                    SuccessfulRequests = !circuit.Stats.SuccessfulRequests + 1L
                    LastSuccessTime = Some DateTime.Now
            }

        // Notify success
        match circuit.OnSuccess with
        | Some callback -> callback ()
        | None -> ()

        // Check if should close circuit
        if !circuit.State = HalfOpen && shouldCloseCircuit circuit.Config !circuit.SuccessCount then
            updateState circuit Closed
            circuit.SuccessCount := 0 // Reset success count

    /// <summary>
    /// Execute operation with circuit breaker protection
    /// </summary>
    let execute (circuit: CircuitBreaker) (operation: unit -> 'TResult) : CircuitResult<'TResult> =
        match !circuit.State with
        | Open ->
            // Check if we should attempt recovery
            if shouldAttemptRecovery circuit.Config !circuit.LastFailureTime then
                updateState circuit HalfOpen
                circuit.SuccessCount := 0
                // Fall through to execute
            else
                CircuitOpen

        | _ -> ()

        match !circuit.State with
        | Open -> CircuitOpen
        | _ ->
            try
                let startTime = DateTime.Now

                // Execute with timeout
                let result =
                    async {
                        try
                            let! result = Async.FromBeginEnd((fun (callback, state) ->
                                operation.BeginInvoke(callback, state)),
                                operation.EndInvoke)
                            return Success result
                        with
                        | :? TimeoutException -> return Timeout
                        | ex -> return Failure (Exception ex)
                    }
                    |> Async.RunSynchronously

                let endTime = DateTime.Now
                let responseTime = endTime - startTime

                // Update response time stats
                if circuit.Config.MonitoringEnabled then
                    let currentAvg = !circuit.Stats.AverageResponseTime
                    let totalRequests = !circuit.Stats.TotalRequests + 1L
                    let newAvg = (currentAvg.Ticks * (!circuit.Stats.TotalRequests)) + responseTime.Ticks
                    circuit.Stats := {
                        !circuit.Stats with
                            TotalRequests = totalRequests
                            AverageResponseTime = TimeSpan.FromTicks(newAvg / totalRequests)
                    }

                match result with
                | Success value ->
                    recordSuccess circuit
                    Success value
                | failure ->
                    recordFailure circuit (match failure with
                                           | Failure reason -> reason
                                           | Timeout -> Timeout
                                           | _ -> Custom "Unknown failure")
                    failure

            with
            | ex ->
                let reason = Exception ex
                recordFailure circuit reason
                Failure reason

    /// <summary>
    /// Execute async operation with circuit breaker protection
    /// </summary>
    let executeAsync (circuit: CircuitBreaker) (operation: Async<'TResult>) : Async<CircuitResult<'TResult>> =
        async {
            match !circuit.State with
            | Open ->
                if shouldAttemptRecovery circuit.Config !circuit.LastFailureTime then
                    updateState circuit HalfOpen
                    circuit.SuccessCount := 0
                else
                    return CircuitOpen

            | _ -> ()

            match !circuit.State with
            | Open -> return CircuitOpen
            | _ ->
                try
                    let startTime = DateTime.Now
                    let! result = operation
                    let endTime = DateTime.Now
                    let responseTime = endTime - startTime

                    // Update stats
                    if circuit.Config.MonitoringEnabled then
                        let currentAvg = !circuit.Stats.AverageResponseTime
                        let totalRequests = !circuit.Stats.TotalRequests + 1L
                        let newAvg = (currentAvg.Ticks * (!circuit.Stats.TotalRequests)) + responseTime.Ticks
                        circuit.Stats := {
                            !circuit.Stats with
                                TotalRequests = totalRequests
                                AverageResponseTime = TimeSpan.FromTicks(newAvg / totalRequests)
                        }

                    recordSuccess circuit
                    return Success result

                with
                | :? TimeoutException ->
                    recordFailure circuit Timeout
                    return Timeout
                | ex ->
                    let reason = Exception ex
                    recordFailure circuit reason
                    return Failure reason
        }

    /// <summary>
    /// Create a new circuit breaker
    /// </summary>
    let create config =
        {
            Config = config
            State = ref Closed
            FailureCount = ref 0
            SuccessCount = ref 0
            LastFailureTime = ref None
            Stats = ref {
                TotalRequests = 0L
                SuccessfulRequests = 0L
                FailedRequests = 0L
                CircuitOpenCount = 0L
                LastFailureTime = None
                LastSuccessTime = None
                AverageResponseTime = TimeSpan.Zero
            }
            Fallback = None
            OnStateChange = None
            OnFailure = None
            OnSuccess = None
        }

    /// <summary>
    /// Create circuit breaker with fallback
    /// </summary>
    let withFallback fallback circuit =
        { circuit with Fallback = Some fallback }

    /// <summary>
    /// Add state change callback
    /// </summary>
    let onStateChange callback circuit =
        { circuit with OnStateChange = Some callback }

    /// <summary>
    /// Add failure callback
    /// </summary>
    let onFailure callback circuit =
        { circuit with OnFailure = Some callback }

    /// <summary>
    /// Add success callback
    /// </summary>
    let onSuccess callback circuit =
        { circuit with OnSuccess = Some callback }

    /// <summary>
    /// Get current circuit state
    /// </summary>
    let getState circuit = !circuit.State

    /// <summary>
    /// Get circuit statistics
    /// </summary>
    let getStats circuit = !circuit.Stats

    /// <summary>
    /// Manually open circuit
    /// </summary>
    let openCircuit circuit =
        updateState circuit Open

    /// <summary>
    /// Manually close circuit
    /// </summary>
    let closeCircuit circuit =
        circuit.FailureCount := 0
        circuit.SuccessCount := 0
        updateState circuit Closed

    /// <summary>
    /// Reset circuit breaker
    /// </summary>
    let reset circuit =
        circuit.FailureCount := 0
        circuit.SuccessCount := 0
        circuit.LastFailureTime := None
        updateState circuit Closed

    // Advanced circuit breaker patterns

    /// <summary>
    /// Bulkhead pattern - isolate failures between different operations
    /// </summary>
    module Bulkhead =

        type BulkheadConfig = {
            MaxConcurrentOperations: int
            QueueSize: int
            Timeout: TimeSpan
        }

        type BulkheadBreaker = {
            Circuit: CircuitBreaker
            Semaphore: SemaphoreSlim
            Queue: ConcurrentQueue<unit -> unit>
            Config: BulkheadConfig
        }

        /// <summary>
        /// Create bulkhead circuit breaker
        /// </summary>
        let create config circuitConfig =
            {
                Circuit = create circuitConfig
                Semaphore = new SemaphoreSlim(config.MaxConcurrentOperations)
                Queue = ConcurrentQueue<unit -> unit>()
                Config = config
            }

        /// <summary>
        /// Execute operation through bulkhead
        /// </summary>
        let execute (bulkhead: BulkheadBreaker) operation =
            async {
                let! acquired = bulkhead.Semaphore.WaitAsync(int bulkhead.Config.Timeout.TotalMilliseconds) |> Async.AwaitTask
                if not acquired then
                    return CircuitOpen
                else
                    try
                        let! result = executeAsync bulkhead.Circuit (async { return operation () })
                        return result
                    finally
                        bulkhead.Semaphore.Release() |> ignore
            }

    /// <summary>
    /// Retry policy for circuit breaker
    /// </summary>
    module Retry =

        type RetryConfig = {
            MaxRetries: int
            InitialDelay: TimeSpan
            BackoffFactor: float
            MaxDelay: TimeSpan
        }

        /// <summary>
        /// Execute with retry policy
        /// </summary>
        let withRetry (retryConfig: RetryConfig) (circuit: CircuitBreaker) operation =
            let rec retry attempt =
                async {
                    match! executeAsync circuit (async { return operation () }) with
                    | Success result -> return Success result
                    | Failure reason when attempt < retryConfig.MaxRetries ->
                        let delay = min retryConfig.MaxDelay (retryConfig.InitialDelay * TimeSpan.FromSeconds(Math.Pow(retryConfig.BackoffFactor, float attempt)))
                        do! Async.Sleep(int delay.TotalMilliseconds)
                        return! retry (attempt + 1)
                    | result -> return result
                }
            retry 0

    /// <summary>
    /// Circuit breaker registry for managing multiple breakers
    /// </summary>
    module Registry =

        type CircuitRegistry = {
            Breakers: ConcurrentDictionary<string, CircuitBreaker>
            DefaultConfig: CircuitConfig
        }

        /// <summary>
        /// Create circuit registry
        /// </summary>
        let create defaultConfig =
            {
                Breakers = ConcurrentDictionary<string, CircuitBreaker>()
                DefaultConfig = defaultConfig
            }

        /// <summary>
        /// Get or create circuit breaker
        /// </summary>
        let getOrCreate name registry =
            registry.Breakers.GetOrAdd(name, fun _ -> create registry.DefaultConfig)

        /// <summary>
        /// Execute operation on named circuit
        /// </summary>
        let executeOn name operation registry =
            let circuit = getOrCreate name registry
            execute circuit operation

        /// <summary>
        /// Get all circuit breakers
        /// </summary>
        let getAllBreakers registry =
            registry.Breakers |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Seq.toList

        /// <summary>
        /// Get registry statistics
        /// </summary>
        let getRegistryStats registry =
            registry.Breakers
            |> Seq.map (fun kvp -> kvp.Key, getStats kvp.Value)
            |> Seq.toList

    /// <summary>
    /// Health monitoring for circuit breakers
    /// </summary>
    module Health =

        type HealthStatus =
            | Healthy
            | Degraded
            | Unhealthy

        type HealthCheck = {
            Name: string
            Status: HealthStatus
            LastChecked: DateTime
            Details: Map<string, obj>
        }

        /// <summary>
        /// Check circuit breaker health
        /// </summary>
        let checkHealth circuit =
            let stats = getStats circuit
            let state = getState circuit

            let status =
                match state, stats.FailedRequests, stats.SuccessfulRequests with
                | Open, _, _ -> Unhealthy
                | _, failed, success when failed > success * 2L -> Degraded
                | _ -> Healthy

            {
                Name = "CircuitBreaker"
                Status = status
                LastChecked = DateTime.Now
                Details = Map.ofList [
                    "State", box state
                    "TotalRequests", box stats.TotalRequests
                    "SuccessfulRequests", box stats.SuccessfulRequests
                    "FailedRequests", box stats.FailedRequests
                    "CircuitOpenCount", box stats.CircuitOpenCount
                    "AverageResponseTime", box stats.AverageResponseTime
                ]
            }

    /// <summary>
    /// Circuit breaker metrics and monitoring
    /// </summary>
    module Metrics =

        /// <summary>
        /// Prometheus-style metrics
        /// </summary>
        module Prometheus =

            /// <summary>
            /// Generate Prometheus metrics string
            /// </summary>
            let generateMetrics circuit name =
                let stats = getStats circuit
                let state = getState circuit

                sprintf """
# Circuit Breaker Metrics
circuit_breaker_state{name="%s"} %d
circuit_breaker_total_requests{name="%s"} %d
circuit_breaker_successful_requests{name="%s"} %d
circuit_breaker_failed_requests{name="%s"} %d
circuit_breaker_open_count{name="%s"} %d
circuit_breaker_average_response_time_ms{name="%s"} %d
                """
                    name
                    (match state with Closed -> 0 | Open -> 1 | HalfOpen -> 2)
                    name stats.TotalRequests
                    name stats.SuccessfulRequests
                    name stats.FailedRequests
                    name stats.CircuitOpenCount
                    name (int stats.AverageResponseTime.TotalMilliseconds)

    /// <summary>
    /// Adaptive circuit breaker that adjusts thresholds based on conditions
    /// </summary>
    module Adaptive =

        type AdaptiveConfig = {
            BaseConfig: CircuitConfig
            AdaptationEnabled: bool
            HighLoadThreshold: float
            LowLoadThreshold: float
            AdaptationFactor: float
        }

        type AdaptiveBreaker = {
            Breaker: CircuitBreaker
            Config: AdaptiveConfig
            LoadHistory: ConcurrentQueue<float>
            MaxHistorySize: int
        }

        /// <summary>
        /// Create adaptive circuit breaker
        /// </summary>
        let create adaptiveConfig =
            {
                Breaker = create adaptiveConfig.BaseConfig
                Config = adaptiveConfig
                LoadHistory = ConcurrentQueue<float>()
                MaxHistorySize = 100
            }

        /// <summary>
        /// Adapt thresholds based on load
        /// </summary>
        let adaptThresholds adaptiveBreaker =
            if not adaptiveBreaker.Config.AdaptationEnabled then ()
            else
                let avgLoad = adaptiveBreaker.LoadHistory |> Seq.average
                let currentConfig = adaptiveBreaker.Breaker.Config

                let newThreshold =
                    if avgLoad > adaptiveBreaker.Config.HighLoadThreshold then
                        max 1 (int (float currentConfig.FailureThreshold * adaptiveBreaker.Config.AdaptationFactor))
                    elif avgLoad < adaptiveBreaker.Config.LowLoadThreshold then
                        max 1 (int (float currentConfig.FailureThreshold / adaptiveBreaker.Config.AdaptationFactor))
                    else
                        currentConfig.FailureThreshold

                // Update config (in a real implementation, this would be more sophisticated)
                printfn "Adapted failure threshold from %d to %d based on load %.2f"
                    currentConfig.FailureThreshold newThreshold avgLoad

        /// <summary>
        /// Record load for adaptation
        /// </summary>
        let recordLoad load adaptiveBreaker =
            adaptiveBreaker.LoadHistory.Enqueue(load)

            // Keep history size manageable
            while adaptiveBreaker.LoadHistory.Count > adaptiveBreaker.MaxHistorySize do
                adaptiveBreaker.LoadHistory.TryDequeue() |> ignore

            adaptThresholds adaptiveBreaker
