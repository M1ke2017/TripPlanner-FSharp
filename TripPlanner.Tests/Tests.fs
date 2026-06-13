namespace TripPlanner.Tests

open System
open System.Net.Http
open System.Net.Http.Json
open System.Threading.Tasks
open Xunit

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open TripPlanner.Api
open TripPlanner.Persistence
open TripPlanner.Domain

module TestHost =

    let createHost () : IHost =
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(fun services ->
                        AppComposition2.configureServices services |> ignore)
                    .Configure(fun app ->
                        let env = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>()
                        AppComposition2.configureApp app env)
                |> ignore)
            .Build()

    let createClient () : Task<IHost * HttpClient> = task {
        let host = createHost()
        do! host.StartAsync()

        use scope = host.Services.CreateScope()
        let db = scope.ServiceProvider.GetRequiredService<TripContext>()
        db.Database.Migrate() |> ignore

        let client = host.GetTestClient()
        client.BaseAddress <- Uri("http://localhost")
        return host, client
    }

module Helpers =
    let ensureOk (resp: HttpResponseMessage) = task {
        if not resp.IsSuccessStatusCode then
            let! body = resp.Content.ReadAsStringAsync()
            Assert.True(false, $"HTTP {(int resp.StatusCode)} {resp.ReasonPhrase}\n{body}")
    }

module TripTests =

    open TestHost
    open Helpers

    [<Fact>]
    let ``GET /api/trip zwraca 200`` () = task {
        let! (host, client) = createClient()
        use _host = host

        let! resp = client.GetAsync("/api/trip")
        do! ensureOk resp
    }

    [<Fact>]
    let ``POST /api/trip dodaje i DELETE usuwa wycieczkę`` () = task {
        let! (host, client) = createClient()
        use _host = host

       
        let newTrip =
            {| name        = "Test integracyjny"
               destination = "Gdańsk"
               description = "Wycieczka testowa"
               startDate   = DateTime(2025, 5, 1)
               endDate     = DateTime(2025, 5, 3)
               statusId    = 1 |}

        let! postResp = client.PostAsJsonAsync("/api/trip", newTrip)
        do! ensureOk postResp

        
        let! created = postResp.Content.ReadFromJsonAsync<Trip>()
        Assert.NotNull(created)
        let tripId = created.Id
        Assert.True(tripId > 0)

        let! getOne = client.GetAsync($"/api/trip/{tripId}")
        do! ensureOk getOne

        let! deleteResp = client.DeleteAsync($"/api/trip/{tripId}")
        do! ensureOk deleteResp
    }
