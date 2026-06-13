namespace TripPlanner.Api
#nowarn "20"
open TripPlanner.Service

open TripPlanner.Persistence
open TripPlanner.Service
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.Cors.Infrastructure
open Npgsql.EntityFrameworkCore.PostgreSQL
open EFCore.NamingConventions

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)
        let builder = WebApplication.CreateBuilder(args)

        let connectionString = "Host=localhost;Database=travelplannerdb;Username=postgres;Password=postgres12"
        builder.Services.AddDbContext<TripContext>(fun options ->
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()   
            |> ignore
        )

        builder.Services.AddCors(fun options ->
            options.AddDefaultPolicy(fun policy ->
                policy
                    .WithOrigins("https://localhost:7048")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                |> ignore
            )
        ) |> ignore
        builder.Services.AddHttpClient() |> ignore
        builder.Services.AddSingleton<IAttractionsService, AttractionsService>() |> ignore
        builder.Services.AddControllers()

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.UseCors()
        app.MapControllers()

        app.Run()

        exitCode

