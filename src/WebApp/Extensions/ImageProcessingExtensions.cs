using NetVips;

namespace WebApp.Extensions;

public static class ImageProcessingExtensions
{
    private const int VipsThreads = 2;
    private const Enums.LogLevelFlags VipsLogLevels =
        Enums.LogLevelFlags.Critical | Enums.LogLevelFlags.Error | Enums.LogLevelFlags.Warning |
        Enums.LogLevelFlags.Message | Enums.LogLevelFlags.Info | Enums.LogLevelFlags.Debug;

    private static readonly Log.LogDelegate VipsLogHandler = (domain, level, message) =>
    {
        var serilogLevel = (level & (Enums.LogLevelFlags.Critical | Enums.LogLevelFlags.Error)) != 0
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Debug;
        Serilog.Log.Write(serilogLevel, "{VipsDomain}: {VipsMessage}", domain, message?.Trim());
    };

    public static IServiceCollection AddImageProcessing(this IServiceCollection services)
    {
        if (!ModuleInitializer.VipsInitialized)
        {
            throw new InvalidOperationException(
                "libvips failed to initialise for this platform.", ModuleInitializer.Exception);
        }

        Log.SetLogHandler("VIPS", VipsLogLevels, VipsLogHandler);

        NetVips.NetVips.BlockUntrusted = true;
        Cache.Max = 0;
        Cache.MaxMem = 0;
        Cache.MaxFiles = 0;
        NetVips.NetVips.Concurrency = VipsThreads;

        Serilog.Log.Information("libvips {Major}.{Minor}.{Micro} initialised",
            NetVips.NetVips.Version(0), NetVips.NetVips.Version(1), NetVips.NetVips.Version(2));
        return services;
    }
}
