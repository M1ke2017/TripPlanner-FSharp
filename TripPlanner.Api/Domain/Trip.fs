namespace TripPlanner.Domain

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<CLIMutable>]
type TripStatus = {
    [<Column("id")>]
    Id: int

    [<Column("name")>]
    Name: string
}

[<CLIMutable>]
type Trip = {
    [<Column("id")>]
    Id: int

    [<Column("destination")>]
     mutable Destination: string

    [<Column("startdate")>]
    mutable StartDate: DateTime

    [<Column("enddate")>]
    mutable EndDate: DateTime

    [<Column("description")>]
    mutable Description: string

    [<Column("statusid")>]
    mutable StatusId: int

    mutable Status: TripStatus
}

[<CLIMutable>]
type TripCost = { 
    Id: int
    TripId: int
    Category: string 
    Amount:decimal
    Currency:string
    Note:string
    SpentAt:System.DateTime
    AmountPln:decimal option
}

[<CLIMutable>]
type CreateTripCost = {
    Category:string
    Amount:decimal
    Currency:string
    Note:string
    SpentAt:System.DateTime
}

