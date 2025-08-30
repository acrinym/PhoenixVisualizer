// ParameterValidation.fs - Functional Parameter Validation
// Demonstrates F#'s superiority for validation logic and data processing
// Much more composable and type-safe than imperative validation approaches

namespace PhoenixVisualizer.Core

open System
open System.Text.RegularExpressions

/// <summary>
/// Functional parameter validation utilities using F# patterns
/// Much more elegant and composable than imperative validation
/// </summary>
module ParameterValidation =

    /// <summary>
    /// Validation result type
    /// </summary>
    type ValidationResult<'T> =
        | Success of 'T
        | Failure of string list

    /// <summary>
    /// Parameter validation rules
    /// </summary>
    module Rules =

        /// <summary>
        /// Not null or empty validation
        /// </summary>
        let notNullOrEmpty fieldName (value: string) =
            if String.IsNullOrWhiteSpace value then
                Failure [sprintf "%s cannot be null or empty" fieldName]
            else Success value

        /// <summary>
        /// String length validation
        /// </summary>
        let stringLength minLength maxLength fieldName (value: string) =
            if value.Length < minLength then
                Failure [sprintf "%s must be at least %d characters" fieldName minLength]
            elif value.Length > maxLength then
                Failure [sprintf "%s cannot exceed %d characters" fieldName maxLength]
            else Success value

        /// <summary>
        /// Numeric range validation
        /// </summary>
        let inRange minValue maxValue fieldName (value: 'T when 'T : comparison) =
            if value < minValue then
                Failure [sprintf "%s must be at least %A" fieldName minValue]
            elif value > maxValue then
                Failure [sprintf "%s cannot exceed %A" fieldName maxValue]
            else Success value

        /// <summary>
        /// Regex pattern validation
        /// </summary>
        let matchesPattern pattern fieldName (value: string) =
            if Regex.IsMatch(value, pattern) then
                Success value
            else
                Failure [sprintf "%s does not match required pattern" fieldName]

        /// <summary>
        /// Email validation
        /// </summary>
        let email fieldName (value: string) =
            let emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
            matchesPattern emailPattern fieldName value

        /// <summary>
        /// One of validation (enum-like)
        /// </summary>
        let oneOf validValues fieldName value =
            if List.contains value validValues then
                Success value
            else
                Failure [sprintf "%s must be one of: %s" fieldName (String.Join(", ", validValues))]

        /// <summary>
        /// Custom validation with predicate
        /// </summary>
        let custom predicate errorMessage value =
            if predicate value then Success value
            else Failure [errorMessage]

        /// <summary>
        /// Positive number validation
        /// </summary>
        let positive fieldName (value: 'T when 'T : comparison and 'T : (static member Zero : 'T)) =
            if value > LanguagePrimitives.GenericZero then
                Success value
            else
                Failure [sprintf "%s must be positive" fieldName]

        /// <summary>
        /// Non-negative validation
        /// </summary>
        let nonNegative fieldName (value: 'T when 'T : comparison and 'T : (static member Zero : 'T)) =
            if value >= LanguagePrimitives.GenericZero then
                Success value
            else
                Failure [sprintf "%s cannot be negative" fieldName]

    /// <summary>
    /// Validation combinators
    /// </summary>
    module Combinators =

        /// <summary>
        /// Combine two validations
        /// </summary>
        let andThen validator1 validator2 value =
            match validator1 value with
            | Success _ -> validator2 value
            | Failure errors -> Failure errors

        /// <summary>
        /// Combine multiple validations
        /// </summary>
        let all validators value =
            let results = validators |> List.map (fun validator -> validator value)
            let errors = results |> List.choose (function Failure errs -> Some errs | _ -> None) |> List.concat
            if errors.IsEmpty then Success value else Failure errors

        /// <summary>
        /// Optional validation (only validate if value is present)
        /// </summary>
        let optional validator value =
            match value with
            | Some v -> validator v |> Result.map Some
            | None -> Success None

        /// <summary>
        /// Validate a list of items
        /// </summary>
        let validateList validator items =
            let results = items |> List.mapi (fun i item ->
                match validator item with
                | Success v -> Success v
                | Failure errors -> Failure (errors |> List.map (fun e -> sprintf "Item %d: %s" i e)))
            let errors = results |> List.choose (function Failure errs -> Some errs | _ -> None) |> List.concat
            if errors.IsEmpty then Success items else Failure errors

        /// <summary>
        /// Validate a dictionary of key-value pairs
        /// </summary>
        let validateDict keyValidator valueValidator dict =
            let results = dict |> Map.toList |> List.mapi (fun i (key, value) ->
                match keyValidator key, valueValidator value with
                | Success k, Success v -> Success (k, v)
                | Failure keyErrors, Success _ -> Failure (keyErrors |> List.map (fun e -> sprintf "Key %d: %s" i e))
                | Success _, Failure valueErrors -> Failure (valueErrors |> List.map (fun e -> sprintf "Value %d: %s" i e))
                | Failure keyErrors, Failure valueErrors ->
                    Failure ((keyErrors |> List.map (fun e -> sprintf "Key %d: %s" i e)) @
                            (valueErrors |> List.map (fun e -> sprintf "Value %d: %s" i e))))
            let errors = results |> List.choose (function Failure errs -> Some errs | _ -> None) |> List.concat
            if errors.IsEmpty then Success dict else Failure errors

    /// <summary>
    /// Parameter validation pipeline
    /// </summary>
    module Pipeline =

        /// <summary>
        /// Create a validation pipeline
        /// </summary>
        let create validators = Combinators.all validators

        /// <summary>
        /// Run validation pipeline
        /// </summary>
        let run pipeline value = pipeline value

        /// <summary>
        /// Convert validation result to option
        /// </summary>
        let toOption = function
            | Success value -> Some value
            | Failure _ -> None

        /// <summary>
        /// Get errors from validation result
        /// </summary>
        let getErrors = function
            | Success _ -> []
            | Failure errors -> errors

        /// <summary>
        /// Check if validation succeeded
        /// </summary>
        let isValid = function
            | Success _ -> true
            | Failure _ -> false

    /// <summary>
    /// Pre-built validation pipelines for common scenarios
    /// </summary>
    module Presets =

        /// <summary>
        /// User name validation
        /// </summary>
        let userName = Rules.stringLength 2 50 "User name" |> Combinators.andThen (Rules.matchesPattern "^[a-zA-Z0-9_]+$" "User name")

        /// <summary>
        /// Password validation
        /// </summary>
        let password = Rules.stringLength 8 128 "Password" |> Combinators.andThen (Rules.matchesPattern "(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)" "Password")

        /// <summary>
        /// Email validation
        /// </summary>
        let email = Rules.email "Email"

        /// <summary>
        /// File path validation
        /// </summary>
        let filePath = Rules.notNullOrEmpty "File path" |> Combinators.andThen (Rules.custom (fun s -> not (s.Contains("..") || s.Contains("\\"))) "Invalid file path")

        /// <summary>
        /// URL validation
        /// </summary>
        let url = Rules.matchesPattern "^https?://[^\\s/$.?#].[^\\s]*$" "URL"

        /// <summary>
        /// Positive integer validation
        /// </summary>
        let positiveInt = Rules.positive "Value" |> Combinators.andThen (Rules.inRange 1 Int32.MaxValue "Value")

        /// <summary>
        /// Percentage validation (0-100)
        /// </summary>
        let percentage = Rules.inRange 0.0 100.0 "Percentage"

        /// <summary>
        /// Hex color validation
        /// </summary>
        let hexColor = Rules.matchesPattern "^#[0-9A-Fa-f]{6}$" "Hex color"

        /// <summary>
        /// Phone number validation (basic)
        /// </summary>
        let phoneNumber = Rules.matchesPattern "^[+]?[0-9\\s\\-\\(\\)]{10,15}$" "Phone number"

    /// <summary>
    /// Effect parameter validation
    /// </summary>
    module EffectParameters =

        /// <summary>
        /// Blur strength validation
        /// </summary>
        let blurStrength = Rules.inRange 0.0 10.0 "Blur strength"

        /// <summary>
        /// Opacity validation
        /// </summary>
        let opacity = Rules.inRange 0.0 1.0 "Opacity"

        /// <summary>
        /// Color component validation
        /// </summary>
        let colorComponent = Rules.inRange 0 255 "Color component"

        /// <summary>
        /// Frequency validation (in Hz)
        /// </summary>
        let frequency = Rules.inRange 20.0 20000.0 "Frequency"

        /// <summary>
        /// BPM validation
        /// </summary>
        let bpm = Rules.inRange 60.0 200.0 "BPM"

        /// <summary>
        /// Sensitivity validation
        /// </summary>
        let sensitivity = Rules.inRange 0.0 1.0 "Sensitivity"

        /// <summary>
        /// Effect name validation
        /// </summary>
        let effectName = Rules.stringLength 1 100 "Effect name" |> Combinators.andThen (Rules.matchesPattern "^[a-zA-Z0-9_\\s]+$" "Effect name")

        /// <summary>
        /// Parameter group validation
        /// </summary>
        let parameterGroup = Rules.oneOf ["General"; "Audio"; "Visual"; "Advanced"] "Parameter group"

    /// <summary>
    /// Validation utilities
    /// </summary>
    module Utils =

        /// <summary>
        /// Validate all parameters in a dictionary
        /// </summary>
        let validateParameterDict (validators: Map<string, 'T -> ValidationResult<'T>>) (parameters: Map<string, 'T>) : ValidationResult<Map<string, 'T>> =
            let results = parameters |> Map.map (fun key value ->
                match validators.TryFind key with
                | Some validator -> validator value
                | None -> Success value)
            let errors = results |> Map.toList |> List.choose (function
                | key, Failure errs -> Some (errs |> List.map (fun e -> sprintf "%s: %s" key e))
                | _ -> None) |> List.concat
            if errors.IsEmpty then Success parameters else Failure errors

        /// <summary>
        /// Sanitize string input
        /// </summary>
        let sanitizeString (input: string) : string =
            input.Trim().Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;")

        /// <summary>
        /// Validate and sanitize parameters
        /// </summary>
        let validateAndSanitize validator sanitizer value =
            match validator value with
            | Success v -> Success (sanitizer v)
            | Failure errors -> Failure errors

        /// <summary>
        /// Batch validation for multiple items
        /// </summary>
        let batchValidate validator items =
            let results = items |> Array.map validator
            let successes = results |> Array.choose (function Success v -> Some v | _ -> None)
            let failures = results |> Array.choose (function Failure errs -> Some errs | _ -> None) |> Array.concat
            if failures.Length = 0 then Success successes else Failure failures

    /// <summary>
    /// Validation builder pattern
    /// </summary>
    module Builder =

        /// <summary>
        /// Validation builder type
        /// </summary>
        type ValidationBuilder() =
            member _.Bind(x, f) = Result.bind f x
            member _.Return(x) = Success x
            member _.ReturnFrom(x) = x

        /// <summary>
        /// Validation computation expression
        /// </summary>
        let validate = ValidationBuilder()

        /// <summary>
        /// Example: Complex parameter validation using computation expressions
        /// </summary>
        let validateComplexParameter (name: string) (value: float) (min: float) (max: float) (required: bool) =
            validate {
                let! nonNullName = Rules.notNullOrEmpty "Name" name
                let! validValue = Rules.inRange min max "Value" value
                if required && value = 0.0 then
                    return! Failure ["Value is required"]
                return (nonNullName, validValue)
            }

    /// <summary>
    /// Advanced validation scenarios
    /// </summary>
    module Advanced =

        /// <summary>
        /// Cross-field validation
        /// </summary>
        let crossFieldValidation field1 field2 predicate errorMessage value1 value2 =
            if predicate value1 value2 then Success (value1, value2)
            else Failure [errorMessage]

        /// <summary>
        /// Conditional validation
        /// </summary>
        let conditionalValidation condition validator value =
            if condition then validator value
            else Success value

        /// <summary>
        /// Validation with context
        /// </summary>
        let withContext context validator value =
            match validator value with
            | Success v -> Success v
            | Failure errors -> Failure (errors |> List.map (fun e -> sprintf "%s: %s" context e))

        /// <summary>
        /// Async validation (for expensive operations)
        /// </summary>
        let asyncValidation validator value = async {
            return validator value
        }

        /// <summary>
        /// Validation with caching
        /// </summary>
        let cachedValidation (cache: System.Collections.Concurrent.ConcurrentDictionary<'T, ValidationResult<'T>>) validator value =
            match cache.TryGetValue value with
            | true, result -> result
            | false, _ ->
                let result = validator value
                cache.TryAdd(value, result) |> ignore
                result
