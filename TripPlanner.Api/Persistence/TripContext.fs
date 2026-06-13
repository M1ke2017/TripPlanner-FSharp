namespace TripPlanner.Persistence

open Microsoft.EntityFrameworkCore
open TripPlanner.Domain
open System

type TripContext(options: DbContextOptions<TripContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable trips : DbSet<Trip>
    member this.Trips
        with get() = this.trips
        and set v = this.trips <- v

    [<DefaultValue>]
    val mutable statuses : DbSet<TripStatus>
    member this.Statuses
        with get() = this.statuses
        and set v = this.statuses <- v

    [<DefaultValue>]
    val mutable tripCosts : DbSet<TripCost>
    member this.TripCosts
        with get() = this.tripCosts
        and set v = this.tripCosts <- v


    override this.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.Entity<Trip>().ToTable("trips") |> ignore
        modelBuilder.Entity<TripStatus>().ToTable("tripstatus") |> ignore
        modelBuilder.Entity<TripCost>().ToTable("tripcosts") |> ignore
        modelBuilder.Entity<TripCost>().Ignore("AmountPln") |> ignore

        let e = modelBuilder.Entity<TripCost>()
        e.ToTable("tripcosts") |> ignore
        e.HasKey("Id") |> ignore


        e.Ignore("AmountPln") |> ignore


        e.Property(fun x -> x.Id).HasColumnName("id") |> ignore
        e.Property(fun x -> x.TripId).HasColumnName("tripid") |> ignore
        e.Property(fun x -> x.Category).HasColumnName("category") |> ignore
        e.Property(fun x -> x.Amount).HasColumnName("amount") |> ignore
        e.Property(fun x -> x.Currency).HasColumnName("currency") |> ignore
        e.Property(fun x -> x.Note).HasColumnName("note") |> ignore
        e.Property(fun x -> x.SpentAt)
         .HasColumnName("spentat")
         .HasColumnType("timestamptz")
         |> ignore


        modelBuilder
            .HasDbFunction(
            (fun () ->
                DbFunctions.FxConvert(0m, "", "", DateTime.Today)
                : Nullable<decimal>)
              )
              .HasName("fx_convert")
              .HasSchema("public")
              |> ignore