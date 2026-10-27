using System.Text.Json;

namespace BMCL.Core.IO;

public static class JsonFile
{
    public static async Task<T?> ReadAsync<T>(
        string path,
        JsonSerializerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The JSON file is invalid: {path}", exception);
        }
    }

    public static Task WriteAsync<T>(
        string path,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return AtomicFile.WriteAsync(
            path,
            (stream, token) => JsonSerializer.SerializeAsync(stream, value, options, token),
            cancellationToken);
    }
}
