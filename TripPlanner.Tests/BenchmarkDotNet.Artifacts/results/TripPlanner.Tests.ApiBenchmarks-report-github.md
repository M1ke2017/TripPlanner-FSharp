```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
11th Gen Intel Core i5-11300H 3.10GHz (Max: 3.11GHz), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v4 DEBUG
  DefaultJob : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v4


```
| Method              | Mean     | Error     | StdDev    | Gen0    | Allocated |
|-------------------- |---------:|----------:|----------:|--------:|----------:|
| GetCostsSummary_EUR | 3.720 ms | 0.1311 ms | 0.3846 ms | 46.8750 | 206.55 KB |
