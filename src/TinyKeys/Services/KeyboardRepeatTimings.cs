using TinyKeys.Interop;

namespace TinyKeys.Services;

internal readonly record struct KeyboardRepeatTimings(int DelayMilliseconds, int IntervalMilliseconds)
{
    // Windows defaults: 500 ms before the first repeat, roughly 30 repeats/s.
    private const int DefaultDelayMilliseconds = 500;
    private const int DefaultIntervalMilliseconds = 40;

    public static KeyboardRepeatTimings FromSystem()
    {
        var delaySetting = 1;
        var speedSetting = 15;

        var haveDelay = NativeMethods.SystemParametersInfo(
            NativeMethods.SPI_GETKEYBOARDDELAY,
            0,
            out delaySetting,
            0);
        var haveSpeed = NativeMethods.SystemParametersInfo(
            NativeMethods.SPI_GETKEYBOARDSPEED,
            0,
            out speedSetting,
            0);

        if (!haveDelay)
        {
            delaySetting = 1;
        }

        if (!haveSpeed)
        {
            speedSetting = 15;
        }

        delaySetting = Math.Clamp(delaySetting, 0, 3);
        speedSetting = Math.Clamp(speedSetting, 0, 31);

        var delayMs = 250 * (delaySetting + 1);
        // Windows exposes 0..31, roughly 2.5..30 repetitions per second.
        var repetitionsPerSecond = 2.5 + 27.5 * speedSetting / 31.0;
        var intervalMs = Math.Clamp(
            (int)Math.Round(1000.0 / repetitionsPerSecond),
            25,
            400);

        if (!haveDelay)
        {
            delayMs = DefaultDelayMilliseconds;
        }

        if (!haveSpeed)
        {
            intervalMs = DefaultIntervalMilliseconds;
        }

        return new KeyboardRepeatTimings(delayMs, intervalMs);
    }
}
