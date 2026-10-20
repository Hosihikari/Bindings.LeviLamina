using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using Hosihikari;
using Hosihikari.Bindings.LeviLamina.Interop;
using Hosihikari.Bindings.LeviLamina.Interop.ll;
using Hosihikari.Bindings.LeviLamina.Interop.ll.hash_utils;
using LlEventId = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventId;
using LlVersion = Hosihikari.Bindings.LeviLamina.Interop.ll.data.Version;

public sealed class ShimSmokePlugin : IPlugin
{
    private IPluginContext _context = null!;

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
