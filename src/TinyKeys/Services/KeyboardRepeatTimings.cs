using TinyKeys.Interop;

namespace TinyKeys.Services;

internal readonly struct KeyboardRepeatTimings
{
    public KeyboardRepeatTimings(int delayMilliseconds, int intervalMilliseconds)
    {
        DelayMilliseconds = delayMilliseconds;
        IntervalMilliseconds = intervalMilliseconds;
    }

    public int DelayMilliseconds { get; }
    public int IntervalMilliseconds { get; }

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

        delaySetting = Math.Min(3, Math.Max(0, delaySetting));
        speedSetting = Math.Min(31, Math.Max(0, speedSetting));

        var delayMs = 250 * (delaySetting + 1);
        // Windows exposes 0..31, roughly 2.5..30 repetitions per second.
        var repetitionsPerSecond = 2.5 + 27.5 * speedSetting / 31.0;
        var intervalMs = (int)Math.Round(1000.0 / repetitionsPerSecond);
        intervalMs = Math.Min(400, Math.Max(25, intervalMs));

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
