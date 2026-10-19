# LeviLamina BindingShim

The native side of the LeviLamina binding: it includes the LL/Bedrock headers and projects the selected declarations into the C ABI described in ADR-0004.

## Target

- Windows x64 only, matching upstream LeviLamina's release/CI support.
- Built with the LeviLamina ecosystem: xmake, clang-cl, MSVC STL, `/MD`, C++20/23 as required by the headers.
- The default link-safe mode emits header-only calls without a carrier import library. `--link-carrier` links `LeviLamina.lib` plus the carrier package libraries and admits exported out-of-line members. MCAPI symbols follow LeviLamina's own symbol toolchain (`symbolprovider` / `prelink` / `bedrockdata`).
- The C ABI is a versioned function table returned by `hosihikari_get_binding_table_v1()`. No CLR hosting, no HostingShim responsibilities, no C++ exceptions crossing the boundary; failures are reported through `hosihikari_get_last_error()`.

## Layout

```
generated/
  tables/        function-table declarations and stable ordering
  shim/          extern "C" implementations
  directors/     C++ directors for managed overrides
include/         generated public headers for the shim
src/             hand-written glue (error boundary, table registry)
```

Generated sources are produced by `Hosihikari/Bindings.Generator` with `--native-root` and consumed by this build target; the binding repository's CI diff is the integration gate.

## Status

The generator emits the v1 table, its `extern "C"` implementations, and the managed table layout for LLAPI declarations. The current LLAPI slice selects 167 link-safe functions, or 396 functions with `--link-carrier` against `LeviLamina.lib`. The generator's `frontend/build-shim.ps1` compiles both forms, and `tests/BindingShimSmoke` exercises string construction, string results, field access, opaque parameters, and methods end to end. Director emission is a subsequent layer.
