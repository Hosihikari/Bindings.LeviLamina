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
- Native exception capture: `Version.Create("not-a-version")` must surface as a managed `InvalidOperationException` with the native message.

Expected result:

```text
load=ok
eventid.name=hosihikari
eventid.hash=0x72f9cc1f8b00ae5e
version.to_string=1.2.3
version.fields=1.2.3
version.valid=True
error.bool=True
error.message=success
error.dispose=ok
invalid=caught:invalid argument
result=OK
```

The smoke is not part of CI; it requires a local BDS/LeviLamina runtime.
