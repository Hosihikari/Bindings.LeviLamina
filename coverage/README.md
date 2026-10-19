# Coverage and generated-source provenance

- `manifest.jsonl` / `summary.json`: emitted by `Hosihikari.Bindings.Generator` from the versioned IR.
- `baseline.json`: the committed bound counts and BindingShim function count. Lowering a value requires an explicit baseline update.
- `provenance.json`: generator version, IR version and manifest hash, carrier commit, table version, function count, and coverage manifest hash.
- `generated.sha256`: SHA-256 for every generated C# file, shim source, and the coverage manifest/summary.

`scripts/verify_generated.py` checks the provenance, baseline, table shape, managed table, and the hash manifest. `scripts/regenerate.ps1` refreshes the generated files and provenance from an IR directory.
