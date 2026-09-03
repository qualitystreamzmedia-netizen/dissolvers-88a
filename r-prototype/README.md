# DISSOLVERS 88A — R console spike

A throwaway prototype to judge the speed and feel of embedding **R** in
DISSOLVERS 88A via **WebR** (R 4.3 compiled to WebAssembly). Same page would
later be hosted in a `WebView` on WPF and MAUI.

## Run

```
cd r-prototype
npm install          # pulls @r-wasm/webr@0.2.0 (~67 MB, includes the full R VFS)
```

Then, from the repo root, start the preview server (`.claude/launch.json` →
`r-prototype`) or run it directly:

```
node r-prototype/server.js      # http://localhost:8091
```

The server sets `Cross-Origin-Opener-Policy` / `Cross-Origin-Embedder-Policy`
so WebR can use `SharedArrayBuffer` (the fast channel).

## What it shows

- A real R REPL — `summary()`, `lm()`, `t.test()` etc. print genuine R output.
- Plots render onto the navy "plot screen", styled like the calculator's Graph
  tab; multiple plots stack with a pager (tap to cycle).
- Cold start ≈ 4–8 s first load (downloads ~30 MB, then cached); < 1 s after.
- Base R only — no package downloads (`webr::install()` needs the network).
