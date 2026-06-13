namespace TripPlanner.Persistence
open System

type DbFunctions =
    static member FxConvert(amount: decimal, fromCode: string, toCode: string, onDate: DateTime) : Nullable<decimal> =
        Unchecked.defaultof<_>
