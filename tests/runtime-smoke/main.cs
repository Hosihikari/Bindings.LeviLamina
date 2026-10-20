using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Hosihikari;
using Hosihikari.Bindings.LeviLamina.Interop;
using Hosihikari.Bindings.LeviLamina.Interop.ll;
using Hosihikari.Bindings.LeviLamina.Interop.ll.hash_utils;
using Hosihikari.Bindings.LeviLamina.Interop.ll.io;
using Hosihikari.Bindings.LeviLamina.Interop.ll.thread;
using LlEventId = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventId;
using LlVersion = Hosihikari.Bindings.LeviLamina.Interop.ll.data.Version;

public sealed class ShimSmokePlugin : IPlugin
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CallbackInvoke(nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CallbackRelease(nint userData);

    private static readonly CallbackInvoke Callback = OnCallback;
    private static readonly CallbackRelease Release = OnRelease;
    private static int _callbackCount;
    private static bool _callbackReleased;

    private IPluginContext _context = null!;

    private static void OnCallback(nint userData) => Interlocked.Increment(ref _callbackCount);

    private static void OnRelease(nint userData)
    {
        _callbackReleased = true;
        GCHandle.FromIntPtr(userData).Free();
    }

    public void OnLoad(IPluginContext context) => _context = context;

    public PluginHookResult OnEnable()
    {
        Directory.CreateDirectory(_context.DataDirectory);
        var resultPath = Path.Combine(_context.DataDirectory, "shim-smoke.txt");
        var log = new StringBuilder();

        try
        {
            var shimPath = Path.Combine(_context.PackageDirectory, "Hosihikari.Bindings.LeviLamina.Shim.dll");
            NativeMethods.Load(shimPath);
            log.AppendLine("load=ok");

            using var id = LlEventId.Create("hosihikari");
            var hash = new HashedIdBase(id.Handle).hash;
            log.AppendLine($"eventid.name={id.name}");
            log.AppendLine($"eventid.hash=0x{hash:x16}");

            using var version = LlVersion.Create("1.2.3");
            log.AppendLine($"version.to_string={version.to_string()}");
            log.AppendLine($"version.fields={version.major}.{version.minor}.{version.patch}");
            log.AppendLine($"version.valid={LlVersion.valid("1.2.3")}");

            using var error = Error.Create();
            log.AppendLine($"error.bool={error.operator_bool()}");
            log.AppendLine($"error.message={error.message()}");
            log.AppendLine("error.dispose=ok");

            using (var pauser = GlobalThreadPauser.Create())
            {
                log.AppendLine("pauser.create=ok");
            }

            log.AppendLine("pauser.dispose=ok");

            using (var sink = ConsoleSink.Create())
            {
                log.AppendLine("sink.create=ok");
            }

            log.AppendLine("sink.dispose=ok");

            var executor = InplaceExecutor.Create("hosihikari-smoke");
            var state = GCHandle.Alloc(new object());
            executor.execute(
                (nint)Marshal.GetFunctionPointerForDelegate(Callback),
                GCHandle.ToIntPtr(state),
                (nint)Marshal.GetFunctionPointerForDelegate(Release));
            log.AppendLine($"callback.count={Volatile.Read(ref _callbackCount)}");
            log.AppendLine($"callback.release={_callbackReleased}");
            executor.Dispose();
            log.AppendLine("executor.dispose=ok");

            var registry = new LoggerRegistry(LoggerRegistry.getInstance());
            using (var logger = registry.getOrCreate("hosihikari-smoke"))
            {
                log.AppendLine($"shared.target={(logger is not null && logger.Target != 0 ? "ok" : "failed")}");
                using (var sameLogger = registry.tryGet("hosihikari-smoke"))
                {
                    log.AppendLine($"shared.same={(sameLogger is not null && logger is not null && sameLogger.Target == logger.Target ? "ok" : "failed")}");
                }

                var title = logger is null ? "<null>" : new Logger(logger.Target).getTitle();
                log.AppendLine($"shared.title={title}");
            }

            log.AppendLine("shared.dispose=ok");

            try
            {
                using var invalid = LlVersion.Create("not-a-version");
                log.AppendLine("invalid=unexpected-success");
            }
            catch (InvalidOperationException exception)
            {
                log.AppendLine($"invalid=caught:{exception.Message}");
            }

            log.AppendLine("result=OK");
        }
        catch (Exception exception)
        {
            log.AppendLine("result=FAIL");
            log.AppendLine(exception.ToString());
            _context.Logger.LogInformation("shim smoke failed: {Error}", exception);
        }

        File.WriteAllText(resultPath, log.ToString());
        return PluginHookResult.Allow();
    }

    public PluginHookResult OnDisable() => PluginHookResult.Allow();

    public void OnUnload()
    {
    }
}
