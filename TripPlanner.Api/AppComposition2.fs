namespace TripPlanner.Api

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.EntityFrameworkCore
open TripPlanner.Api.Controllers
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.ApplicationParts
open TripPlanner.Persistence
open TripPlanner.Service

module AppComposition2 =

    let configureServices (services: IServiceCollection) =
        services.AddDbContext<TripContext>(fun (sp: IServiceProvider) (options: DbContextOptionsBuilder) ->
            let cfg: IConfiguration = sp.GetRequiredService<IConfiguration>()
            let cs =
                match cfg.GetConnectionString("Postgres") with
                | null | "" -> "Host=localhost;Database=travelplannerdb;Username=postgres;Password=postgres12"
                | v -> v
            options.UseNpgsql(cs).UseSnakeCaseNamingConvention() |> ignore
        ) |> ignore

        services.AddCors(fun options ->
            options.AddDefaultPolicy(fun policy ->
                policy.WithOrigins("https://localhost:7048").AllowAnyHeader().AllowAnyMethod() |> ignore))
        |> ignore

        services.AddHttpClient() |> ignore
        services.AddSingleton<IAttractionsService, AttractionsService>() |> ignore
        services
            .AddControllers()
            .AddApplicationPart(typeof<TripController>.Assembly)
            .AddApplicationPart(typeof<AttractionsController>.Assembly)
            |> ignore

        services

    let configureApp (app: IApplicationBuilder) (env: IHostEnvironment) : unit =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore
        app.UseHttpsRedirection() |> ignore
        app.UseAuthorization()    |> ignore
        app.UseCors()             |> ignore
        app.UseRouting()          |> ignore
        app.UseEndpoints(fun e -> e.MapControllers() |> ignore) |> ignore
        ()
