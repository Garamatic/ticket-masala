namespace IT_Project2526.Common;

/// <summary>
/// Result pattern for explicit error handling without exceptions.
/// Provides a type-safe way to represent success or failure of operations.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Failure result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Generic Result type for operations that return a value.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Map the value to a new type if the result is successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result.Success(mapper(Value!))
            : Result.Failure<TNew>(Error!);
    }

    /// <summary>
    /// Get the value or a default if the result is a failure.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? Value! : defaultValue;
    }

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Result.Success(value);
}

/// <summary>
/// Extension methods for Result pattern.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Convert a nullable value to a Result.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, string errorIfNull = "Value is null")
        where T : class
    {
        return value != null
            ? Result.Success(value)
            : Result.Failure<T>(errorIfNull);
    }

    /// <summary>
    /// Execute action if result is successful.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value!);
        return result;
    }

    /// <summary>
    /// Execute action if result is failure.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error!);
        return result;
    }
}
