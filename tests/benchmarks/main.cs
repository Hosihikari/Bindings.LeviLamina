using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Hosihikari;
using Hosihikari.Bindings.LeviLamina.Interop;
using Hosihikari.Bindings.LeviLamina.Interop.ll.io;
using Hosihikari.Bindings.LeviLamina.Interop.ll.command;
using Hosihikari.Bindings.LeviLamina.Interop.ll.hash_utils;
using LlEventId = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventId;
using LlVersion = Hosihikari.Bindings.LeviLamina.Interop.ll.data.Version;

public sealed class ShimBenchmarkPlugin : IPlugin
{
    private static int _intSink;
    private static ulong _sink;
    private static bool _boolSink;
    private static string? _stringSink;

    private IPluginContext _context = null!;

    private static void RunBenchmark(
        StringBuilder output,
        string name,
        int iterations,
        int warmup,
        Action action)
    {
        for (var index = 0; index < warmup; index++)
        {
            action();
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
        {
            action();
        }

        var elapsed = Stopwatch.GetTimestamp() - start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var nanoseconds = (double)elapsed / Stopwatch.Frequency * 1_000_000_000 / iterations;
        var bytes = (double)allocated / iterations;
        output.Append(name)
            .Append('=')
            .Append(nanoseconds.ToString("F2", CultureInfo.InvariantCulture))
            .Append(" ns/op, ")
            .Append(bytes.ToString("F1", CultureInfo.InvariantCulture))
            .AppendLine(" B/op");
    }

    public void OnLoad(IPluginContext context) => _context = context;

    public PluginHookResult OnEnable()
    {
        Directory.CreateDirectory(_context.DataDirectory);
        var resultPath = Path.Combine(_context.DataDirectory, "benchmarks.txt");
        var log = new StringBuilder();

        try
        {
            var shimPath = Path.Combine(_context.PackageDirectory, "Hosihikari.Bindings.LeviLamina.Shim.dll");
            NativeMethods.Load(shimPath);
            log.AppendLine("load=ok");

            using var version = LlVersion.Create("1.2.3");
            using var sameVersion = LlVersion.Create("1.2.3");
            using var id = LlEventId.Create("hosihikari-benchmark");
            var baseView = id.AsHashedIdBase();
            using var combiner = HashCombiner.Create(123);

            RunBenchmark(log, "field.version.major", 500_000, 10_000, () => _intSink = version.major);
            RunBenchmark(log, "field.eventid.name", 500_000, 10_000, () => _stringSink = id.name);
            RunBenchmark(log, "field.hashedid.hash", 500_000, 10_000, () => _sink = baseView.hash);
            RunBenchmark(log, "method.version.is_identical", 500_000, 10_000, () => _boolSink = version.isIdenticalTo(sameVersion));
            RunBenchmark(log, "method.hashcombiner.hash", 500_000, 10_000, () => _sink = combiner.hash());
            RunBenchmark(log, "string.version.valid", 200_000, 10_000, () => _boolSink = LlVersion.valid("1.2.3"));
            RunBenchmark(log, "string.version.to_string", 200_000, 10_000, () => _stringSink = version.to_string());

            var registry = LoggerRegistry.getInstance();
            using (var logger = registry.getOrCreate("hosihikari-benchmark"))
            {
                if (logger is not null)
                {
                    RunBenchmark(log, "shared.try_get", 200_000, 5_000, () =>
                    {
                        using var handle = registry.tryGet("hosihikari-benchmark");
                        _sink ^= (ulong)(handle?.Target ?? 0);
                    });
                }
            }

            var commandRegistrar = CommandRegistrar.getInstance(false);
            var enumPairs = new[]
            {
                new KeyValuePair<string, ulong>("one", 1),
                new KeyValuePair<string, ulong>("two", 2),
            };
            RunBenchmark(log, "vector.enum_register", 20_000, 1_000, () =>
            {
                _boolSink = commandRegistrar.tryRegisterRuntimeEnum("hosihikari-benchmark-enum", enumPairs);
            });

            log.AppendLine("result=OK");
        }
        catch (Exception exception)
        {
            log.AppendLine("result=FAIL");
            log.AppendLine(exception.ToString());
            _context.Logger.LogInformation("shim benchmark failed: {Error}", exception);
        }

        File.WriteAllText(resultPath, log.ToString());
        return PluginHookResult.Allow();
    }

    public PluginHookResult OnDisable() => PluginHookResult.Allow();

    public void OnUnload()
    {
    }
}
