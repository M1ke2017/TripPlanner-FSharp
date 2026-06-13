namespace TripPlanner.Tests

open System.Net.Http
open System.Net.Http.Json
open BenchmarkDotNet.Attributes

open TripPlanner.Tests.TestHost

[<MemoryDiagnoser>]
type ApiBenchmarks() =

    
    let (host, client: HttpClient) =
        createClient() |> Async.AwaitTask |> Async.RunSynchronously

    [<Benchmark>]
    member _.GetCostsSummary_EUR() =
        
        let resp =
            client.GetAsync("/api/trip/3/costs/summary?to=EUR")
            |> Async.AwaitTask
            |> Async.RunSynchronously

        resp.EnsureSuccessStatusCode() |> ignore

        
        let txt =
            resp.Content.ReadAsStringAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        txt.Length
