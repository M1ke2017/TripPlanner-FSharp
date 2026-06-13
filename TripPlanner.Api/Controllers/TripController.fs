namespace TripPlanner.Api.Controllers

open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.EntityFrameworkCore
open TripPlanner.Domain
open TripPlanner.Persistence
open Microsoft.AspNetCore.Http
open TripPlanner.Service
open TripPlanner.Api


[<ApiController; Route("api/[controller]")>]
type TripController(ctx: TripContext) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() =
        task {
        try
            let! trips =
                ctx.Trips
                    .Include(fun t -> t.Status)
                    .ToListAsync()
            return Results.Ok(trips)
        with ex ->
       
            return Results.Problem(ex.Message)
    }

    [<HttpPost>]
    member _.Post([<FromBody>] trip: Trip) : Task<IResult> =
        task {
            use! transaction = ctx.Database.BeginTransactionAsync()
            try
                let startUtc =
                    if   trip.StartDate.Kind = DateTimeKind.Utc
                    then trip.StartDate
                    else DateTime.SpecifyKind(trip.StartDate, DateTimeKind.Utc)

                let endUtc =
                    if   trip.EndDate.Kind = DateTimeKind.Utc
                    then trip.EndDate
                    else DateTime.SpecifyKind(trip.EndDate, DateTimeKind.Utc)

                trip.StartDate <- startUtc
                trip.EndDate   <- endUtc

                ctx.Trips.Add(trip) |> ignore
                let! _ = ctx.SaveChangesAsync() 
                do! transaction.CommitAsync()
                return Results.Ok(trip)
            with ex ->
                do! transaction.RollbackAsync()
                return Results.Problem(ex.Message)
        }

    [<HttpGet("statuses")>]
    member _.GetStatuses() =
        task {
        let! allStatuses = ctx.Statuses.ToListAsync()
        let uniqueStatuses =
            allStatuses
            |> Seq.distinctBy (fun s -> s.Name)
            |> Seq.toList
        return Results.Ok(uniqueStatuses)
    }

    [<HttpPut("{id}")>]
    member _.UpdateTrip(id: int, [<FromBody>] updatedTrip: Trip) = 
        task {
            use! transaction = ctx.Database.BeginTransactionAsync()
            try
                let! trip = ctx.Trips.FindAsync(id)
                if box trip = null then 
                    return Results.NotFound()
                else 
                    trip.Destination <- updatedTrip.Destination
                    trip.Description <- updatedTrip.Description
                    trip.StartDate <- updatedTrip.StartDate.ToUniversalTime()
                    trip.EndDate <- updatedTrip.EndDate.ToUniversalTime()
                    trip.StatusId <- updatedTrip.StatusId
                    let! _ = ctx.SaveChangesAsync()
                    do! transaction.CommitAsync()
                    return Results.Ok(trip)
            with ex -> 
                do! transaction.RollbackAsync()
                return Results.Problem(ex.Message)
        }

    [<HttpGet("{id}")>]
    member _.GetById(id: int) =
        task {
        let! trip = 
            ctx.Trips
                .Include(fun t -> t.Status)
                .FirstOrDefaultAsync(fun t -> t.Id = id)
        if box trip = null then
            return Results.NotFound()
        else
            return Results.Ok(trip)
    }

    [<HttpDelete("{id}")>]
    member _.DeleteTrip(id: int) =
        task {
            use! tranansction = ctx.Database.BeginTransactionAsync()
            try
                let! tripOpt = ctx.Trips.FindAsync(id).AsTask()
                match box tripOpt with
                | null -> return Results.NotFound()
                | _ -> 
                    ctx.Trips.Remove(tripOpt) |> ignore
                    let! _ = ctx.SaveChangesAsync()
                    do! tranansction.CommitAsync()
                    return Results.Ok()
            with ex ->
                do! tranansction.RollbackAsync()
                return Results.Problem(ex.Message)           
    }


  
    [<HttpGet("{id}/costs")>]
    member _.GetCosts([<FromRoute(Name="id")>] tripId:int, [<FromQuery(Name="to")>] toCurrency:string) =
        task {
            let tgt =
                if System.String.IsNullOrWhiteSpace toCurrency then "PLN"
                else toCurrency.ToUpper()

            let! raw =
                ctx.TripCosts
                   .AsNoTracking()
                   .Where(fun c -> c.TripId = tripId)
                   .OrderBy(fun c -> c.SpentAt)
                
                   .Select(fun c ->
                       struct(
                           c.Id,          
                           c.TripId,      
                           c.Category,
                           c.Amount,
                           c.Currency,
                           c.Note,
                           c.SpentAt,
                           (DbFunctions.FxConvert(c.Amount, c.Currency, tgt, c.SpentAt.Date)
                               : System.Nullable<decimal>)
                       ))
                   .ToListAsync()

            let rows : TripCostVm[] =
                raw
                |> Seq.map (fun (struct(costId, rowTripId, category, amount, currency, note, spentAt, amountPln)) ->
                    let amountPlnRounded =
                        match Option.ofNullable amountPln with
                        | Some v -> Some (Math.Round(v, 2, MidpointRounding.AwayFromZero))
                        | None   -> None
                    { Id        = costId
                      TripId    = rowTripId
                      Category  = category
                      Amount    = amount
                      Currency  = currency
                      Note      = note
                      SpentAt   = spentAt        
                      AmountPln = amountPlnRounded })
                |> Seq.toArray

            return Results.Ok(rows)
        }

    [<HttpGet("{id}/costs/summary")>]
    member _.GetCostsSummary(id: int, [<FromQuery(Name="to")>] toCurrency:string) =
        task {
        let tgt =
            if String.IsNullOrWhiteSpace toCurrency then "PLN"
            else toCurrency.ToUpper()

        let! total =
            ctx.TripCosts
                .Where(fun c -> c.TripId = id)
                .Select(fun c -> DbFunctions.FxConvert(c.Amount, c.Currency, tgt, c.SpentAt.Date))
                .Where(fun v -> v.HasValue)
                .Select(fun v -> v.Value)
                .SumAsync()

        
        return Results.Ok(box {| tripId = id; currency = tgt; total = total |})
    }

    [<HttpPost("{id}/costs")>]
    member _.AddCost(id:int, [<FromBody>] vm: TripPlanner.Api.TripCostVm) =
        task {
            use! tx = ctx.Database.BeginTransactionAsync()
            try

                let spentAtUtc =
                    if   vm.SpentAt.Kind = DateTimeKind.Utc
                    then vm.SpentAt
                    else DateTime.SpecifyKind(vm.SpentAt, DateTimeKind.Utc)

                let entity : TripCost =
                    { Id        = 0
                      TripId    = id
                      Category  = vm.Category
                      Amount    = vm.Amount
                      Currency  = vm.Currency.ToUpper()
                      Note      = if String.IsNullOrWhiteSpace vm.Note then null else vm.Note
                      SpentAt   = spentAtUtc
                      AmountPln = None }        

                ctx.TripCosts.Add(entity) |> ignore
                let! _ = ctx.SaveChangesAsync()
                do! tx.CommitAsync()

                return Results.Created($"/api/trip/{id}/costs/{entity.Id}", entity.Id)
            with ex ->
                do! tx.RollbackAsync()
                return Results.Problem(ex.Message)
        }

    [<HttpDelete("{tripId}/costs/{costId}")>]
    member _.DeleteCost(tripId:int, costId:int) =
        task {
            use! tx = ctx.Database.BeginTransactionAsync()
            let! cost =
                ctx.TripCosts.FirstOrDefaultAsync(fun c -> c.Id = costId && c.TripId = tripId)
            if isNull (box cost) then
                do! tx.RollbackAsync()
                return Results.NotFound()
            else
                ctx.TripCosts.Remove(cost) |> ignore
                let! _ = ctx.SaveChangesAsync()
                do! tx.CommitAsync()
                return Results.NoContent()

        }
[<ApiController>]
[<Route("api/[controller]")>]
type AttractionsController(service: IAttractionsService) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Get([<FromQuery>] place: string, [<FromQuery>] radius: Nullable<int>) : Task<IActionResult> =
        task {
            if String.IsNullOrWhiteSpace place then
                return (this.BadRequest("Query ?place=... jest wymagane") :> IActionResult)
            else
                let r = if radius.HasValue then radius.Value else 1000
                try
                    let! items = service.Get(place, r)
                    return (this.Ok(items |> List.toArray) :> IActionResult)
                with ex ->
                    return (this.StatusCode(502, $"Upstream error: {ex.Message}") :> IActionResult)
        }
 