open BenchmarkDotNet.Running

[<EntryPoint>]
let main _ =
    BenchmarkRunner.Run<TripPlanner.Tests.ApiBenchmarks>() |> ignore
    0
