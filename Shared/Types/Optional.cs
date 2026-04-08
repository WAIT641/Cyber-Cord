namespace Shared.Types;

public record Optional<T>(T? Value, bool HasValue = true)
{
    public static readonly Optional<T> Empty = new(default(T?), false);
};
