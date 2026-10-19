# LeviLamina BindingShim

The native side of the LeviLamina binding: it includes the LL/Bedrock headers and projects the selected declarations into the C ABI described in ADR-0004.

## Target

- Windows x64 only, matching upstream LeviLamina's release/CI support.
- Built with the LeviLamina ecosystem: xmake, clang-cl, MSVC STL, `/MD`, C++20/23 as required by the headers.
- Linked against LLAPI when the declaration set requires carrier symbols; the v1 link-safe rule emits header-only calls without a carrier import library. MCAPI symbols follow LeviLamina's own symbol toolchain (`symbolprovider` / `prelink` / `bedrockdata`).
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

The generator emits the v1 table, its `extern "C"` implementations, and the managed table layout for link-safe LLAPI declarations. The current slice covers the event identity/hash types and other header-only classes. The generator's `frontend/build-shim.ps1` compiles the emitted tree, and `tests/BindingShimSmoke` exercises it end to end. Director emission and carrier import-library linking are subsequent layers.
