namespace TripPlanner.Api

type TripCostVm =
  { Id        : int
    TripId    : int
    Category  : string
    Amount    : decimal
    Currency  : string
    Note      : string
    SpentAt   : System.DateTime
    AmountPln : decimal option }
