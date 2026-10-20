# Runtime smoke

Loads the committed carrier-linked BindingShim inside a real BDS process and validates header-only and out-of-line LLAPI calls.

## Requirements

- Windows x64.
- BDS 1.26.51.1 with LeviLamina 26.51.7.
- The Hosihikari host and `hosihikari-csharp` provider in `plugins/`.
- A local LeviLamina build with `build/windows/x64/release/LeviLamina.lib`.
- The generated BindingShim and Interop from this repository.

## Run

```shell
powershell -File scripts/run-runtime-smoke.ps1 -ServerRoot C:\path\to\bedrock-server
```

The script builds the solution and native shim, copies the plugin sources and both DLLs into `plugins/shim-smoke`, starts the server, waits for `data/hosihikari/shim-smoke/shim-smoke.txt`, stops the server, and requires `result=OK`.

## Coverage

- `NativeMethods.Load` inside BDS.
- `EventId` string construction, name and hash.
- `Version` string construction, string result, field getters, string method parameter.
- Out-of-line `ll::Error` construction, `operator bool`, `message()` (string result), and destruction.
- Out-of-line virtual-destructor and allocator-backed lifecycle: `GlobalThreadPauser`, `ConsoleSink`, and `InplaceExecutor` creation and destruction.
- Multiple string parameters in one call: `Version(1, 2, 3, "beta", "build.5")` pins both the pre-release and build strings and round-trips through `to_string()`.
- `std::function` callback projection: `InplaceExecutor.execute` invokes a managed C callback and its release callback exactly once.
- `std::shared_ptr<T>` projection: `LoggerRegistry::getOrCreate`/`tryGet` return a `SharedHandle`; both lookups alias the same target, the target serves `Logger::getTitle`, and the shared references release cleanly.
- `vector<pair<string, uint64>>` parameters surface as `KeyValuePair<string, ulong>[]`; `CommandRegistrar.tryRegisterRuntimeEnum` and `addRuntimeEnumValues` register and extend a runtime enum.
- `optional_ref<T>` projection: `PlayerInfo::fromName`/`fromXuid` return `nint?`; lookups for absent players report an empty reference.
- `nonstd::expected_lite::expected<T, ll::Error>` projection: `Property::setJson` reports the native error through `InvalidOperationException` with the `ll::Error` message.
- Native exception capture: `Version.Create("not-a-version")` must surface as a managed `InvalidOperationException` with the native message.

Expected result:

```text
load=ok
eventid.name=hosihikari
eventid.hash=0x72f9cc1f8b00ae5e
version.to_string=1.2.3
version.fields=1.2.3
version.valid=True
version.multi=1.2.3-beta+build.5
error.bool=True
error.message=success
error.dispose=ok
pauser.create=ok
pauser.dispose=ok
sink.create=ok
sink.dispose=ok
callback.count=1
callback.release=True
executor.dispose=ok
shared.target=ok
shared.same=ok
shared.title=hosihikari-smoke
shared.dispose=ok
command_enum.pairs=ok
optional_ref.missing=ok
expected.error=caught
invalid=caught:invalid argument
result=OK
```

The smoke is not part of CI; it requires a local BDS/LeviLamina runtime.
