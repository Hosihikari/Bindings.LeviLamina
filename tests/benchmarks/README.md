# Shim benchmarks

Loads the carrier-linked BindingShim inside a real BDS process and reports the managed-to-native cost of representative calls. The harness is a lightweight in-process loop rather than BenchmarkDotNet because the carrier shim only loads inside the BDS runtime.

## Run

```shell
powershell -File scripts/run-benchmarks.ps1 -ServerRoot C:\path\to\bedrock-server
```

The script builds the solution and native shim, installs `shim-benchmarks` as a managed plugin, starts the server, waits for `data/hosihikari/shim-benchmarks/benchmarks.txt`, and stops the server.

## Measurements

Each line reports nanoseconds per call and managed bytes allocated per call after warmup:

- `field.version.major`: generated field getter through the table.
- `field.eventid.name`: borrowed string field getter and managed copy.
- `field.hashedid.hash`: scalar field getter on a borrowed base view.
- `method.version.is_identical`: scalar argument and boolean result.
- `method.hashcombiner.hash`: out-of-line method call.
- `string.version.valid`: UTF-8 string parameter pinning and boolean result.
- `string.version.to_string`: string return, thread-local shim buffer, and managed copy.
- `shared.try_get`: shared-handle return and managed release.
- `vector.enum_register`: `KeyValuePair<string, ulong>[]` vector parameter encoding.

Expected result:

```text
load=ok
field.version.major=...
field.eventid.name=...
field.hashedid.hash=...
method.version.is_identical=...
method.hashcombiner.hash=...
string.version.valid=...
string.version.to_string=...
shared.try_get=...
vector.enum_register=...
result=OK
```

The numbers are environment-dependent and are not a CI gate; they provide a baseline for comparing generator changes on the same machine and server build.
