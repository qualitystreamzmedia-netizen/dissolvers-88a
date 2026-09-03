# DISSOLVERS 88A

A TI-84–style graphing & statistics calculator, in the DISSOLVERS design language.

- **`Dissolvers88A.Engine`** — the expression engine (lexer → recursive-descent parser →
  evaluator), ~80 functions incl. the full `2nd`-DISTR distributions, plus 1-Var / 2-Var
  statistics and linear regression. Pure C#, no UI dependency. 80 self-tests.
- **`Dissolvers88A`** — WPF desktop app (Calculate / Graph / Stats).
- **`Dissolvers88A.Maui`** — .NET MAUI Android app, same engine.

## Features

- **Calculate** — full expression entry with live preview, history, `Ans`, 27 variables,
  DEG/RAD, a `2nd` modifier, TI-style number formatting. Missing close-parens are auto-closed
  (`nCr(10,2` → 45).
- **Graph** — Y1–Y6, editable WINDOW, ZOOM menu (Standard / Fit / Square / In / Out / Trig /
  Decimal), drag-to-pan, wheel/pinch-zoom, TRACE, and stat plots (Scatter / xy Line /
  Histogram / Box).
- **Stats** — list editor L1–L6, 1-Var Stats, 2-Var Stats (auto-scatter), linear regression
  (ŷ = a·x + b) with "send fit to Y1". Distributions callable from the calculator:
  `normalcdf`, `invNorm`, `binompdf/cdf`, `poisson`, `geometric`, `t`, `chi2`, `F`.

## Build

Requires the .NET 8 SDK. For the Android app also set `JAVA_HOME` to a JDK 17–21 and have the
Android SDK installed.

```
dotnet run    --project Dissolvers88A/Dissolvers88A.csproj                     # WPF
dotnet run    --project Dissolvers88A.EngineTests                              # engine tests
dotnet build  Dissolvers88A.Maui/Dissolvers88A.Maui.csproj -c Release -f net8.0-android
```

## Downloads

See the [latest release](../../releases/latest) for the Windows `.exe` and the Android `.apk`.
