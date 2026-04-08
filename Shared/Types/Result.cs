namespace Shared.Types;

public record Result(string? Error = null)
{
    public static Result Bad(string error) => new(error);
    public static Result Ok() => new();

    public bool IsOk() => Error is null;
};

public record Result<T>(T? Value, string Error = "")
{
    public static Result<T> Bad(string error) => new(default(T?), error);
    public static Result<T> Ok(T value) => new(value);

    public bool IsOk() => Value is not null;
};

public record Result<TValue, TError>(TValue? Value, TError Error = default(TError)!)
{
    public static Result<TValue, TError> Bad(TError error) => new(default(TValue?), error);
    public static Result<TValue, TError> Ok(TValue value) => new(value);

    public bool IsOk() => Value is not null;
}