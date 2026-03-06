using CollageMaker.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CollageMaker.Services.Decorators;

/// <summary>
/// Decorator that measures wall clock time for ImmichClient methods.
/// </summary>
internal sealed partial class TimingImmichClientDecorator(IImmichClient inner, ILogger<TimingImmichClientDecorator> logger) : IImmichClient
{
    public async Task<IReadOnlyList<Image<Rgba32>>> GetRandomImagesAsync(int count, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await inner.GetRandomImagesAsync(count, cancellationToken);
        LogGetRandomImagesCompleted(count, sw.ElapsedMilliseconds);
        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "GetRandomImagesAsync({Count}) completed in {ElapsedMs}ms")]
    partial void LogGetRandomImagesCompleted(int count, long elapsedMs);
}
