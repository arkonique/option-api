namespace OptionPricing.Core.Engines;
public static class EngineFactoryBootstrap
{
    private static bool _configured;
    private static readonly object _gate = new();

    public static void ConfigureDefault(Action<EngineFactory> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        lock (_gate)
        {
            if (_configured) return;            // idempotent
            configure(EngineFactory.Default);
            _configured = true;
        }
    }

    // Optional: test-only reset
    internal static void ResetForTests()
    {
        lock (_gate) { _configured = false; }
    }
}
