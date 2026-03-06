namespace CollageMaker.Services.Decorators;

/// <summary>
/// Decorator that measures wall clock time for CollageService methods.
/// </summary>
internal sealed partial class TimingCollageServiceDecorator(ICollageService inner, ILogger<TimingCollageServiceDecorator> logger) : ICollageService
{
    public async Task CreateCollageAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        await inner.CreateCollageAsync(cancellationToken);
        LogCreateCollageCompleted(sw.ElapsedMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "CreateCollageAsync completed in {ElapsedMs}ms")]
    partial void LogCreateCollageCompleted(long elapsedMs);
}
