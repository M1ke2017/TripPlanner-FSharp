namespace TripPlanner.Service 

open System
open System.Globalization
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks

[<CLIMutable>]
type PoiDto =
    { Name: string
      Category: string
      Lat: float
      Lon: float }

type IAttractionsService =
    abstract member Get : place:string * radiusMeters:int -> Task<PoiDto list>


type AttractionsService(httpFactory: IHttpClientFactory) =

    let jsonDocOpts =
        let mutable o = JsonDocumentOptions()
        o.CommentHandling <- JsonCommentHandling.Skip
        o.AllowTrailingCommas <- true
        o

    interface IAttractionsService with
        member _.Get(place, radiusMeters) = task {
            if String.IsNullOrWhiteSpace place then
                return []
            else
                let http = httpFactory.CreateClient()

               
                let nomiUrl =
                    $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(place)}&format=json&limit=1"

                use geoReq = new HttpRequestMessage(HttpMethod.Get, nomiUrl)
                geoReq.Headers.UserAgent.ParseAdd("TripPlanner/1.0 (+contact@example.com)")

                use! geoResp = http.SendAsync geoReq
                geoResp.EnsureSuccessStatusCode() |> ignore

                use! geoStream = geoResp.Content.ReadAsStreamAsync()
                use geoDoc = JsonDocument.Parse(geoStream, jsonDocOpts)

                let root = geoDoc.RootElement
                if root.ValueKind <> JsonValueKind.Array || root.GetArrayLength() = 0 then
                    return []
                else
                    let first = root.[0]
                    let latS = first.GetProperty("lat").GetString()
                    let lonS = first.GetProperty("lon").GetString()

                    let lat =
                        match Double.TryParse(latS, NumberStyles.Float, CultureInfo.InvariantCulture) with
                        | true, v -> v | _ -> nan
                    let lon =
                        match Double.TryParse(lonS, NumberStyles.Float, CultureInfo.InvariantCulture) with
                        | true, v -> v | _ -> nan

                    if Double.IsNaN lat || Double.IsNaN lon then
                        return []
                    else
                      
                        let inv = CultureInfo.InvariantCulture
                        let latStr = lat.ToString(inv)
                        let lonStr = lon.ToString(inv)

                        let query = $"""
[out:json][timeout:25];
(
  node["amenity"](around:{radiusMeters},{latStr},{lonStr});
  node["tourism"](around:{radiusMeters},{latStr},{lonStr});
  node["leisure"](around:{radiusMeters},{latStr},{lonStr});
);
out body;
"""

                        use overReq = new HttpRequestMessage(HttpMethod.Post, "https://overpass-api.de/api/interpreter")
                        overReq.Headers.UserAgent.ParseAdd("TripPlanner/1.0 (+contact@example.com)")
                        overReq.Headers.Accept.ParseAdd("application/json")
                        overReq.Content <- new StringContent(query, Encoding.UTF8, "text/plain")

                        use! overResp = http.SendAsync overReq
                        overResp.EnsureSuccessStatusCode() |> ignore

                        use! overStream = overResp.Content.ReadAsStreamAsync()
                        use overDoc = JsonDocument.Parse(overStream, jsonDocOpts)

                       
                        let elements =
                            let mutable a = Unchecked.defaultof<JsonElement>
                            if overDoc.RootElement.TryGetProperty("elements", &a) && a.ValueKind = JsonValueKind.Array
                            then a else JsonDocument.Parse("[]").RootElement

                        
                        let results =
                            [ for i in 0 .. elements.GetArrayLength() - 1 do
                                let el = elements.[i]

                                let mutable tags = Unchecked.defaultof<JsonElement>
                                let hasTags = el.TryGetProperty("tags", &tags)

                                let mutable latJ = Unchecked.defaultof<JsonElement>
                                let mutable lonJ = Unchecked.defaultof<JsonElement>
                                let hasLat = el.TryGetProperty("lat", &latJ)
                                let hasLon = el.TryGetProperty("lon", &lonJ)

                                if hasTags && hasLat && hasLon then
                                    let latVal = latJ.GetDouble()
                                    let lonVal = lonJ.GetDouble()

                                    let mutable nameJ = Unchecked.defaultof<JsonElement>
                                    let mutable amenityJ = Unchecked.defaultof<JsonElement>
                                    let mutable tourismJ = Unchecked.defaultof<JsonElement>
                                    let mutable leisureJ = Unchecked.defaultof<JsonElement>

                                    let name =
                                        if tags.TryGetProperty("name", &nameJ) then
                                            let n = nameJ.GetString()
                                            if String.IsNullOrWhiteSpace n then "(unnamed)" else n
                                        else "(unnamed)"

                                    let category =
                                        if tags.TryGetProperty("amenity", &amenityJ) then amenityJ.GetString()
                                        elif tags.TryGetProperty("tourism", &tourismJ) then tourismJ.GetString()
                                        elif tags.TryGetProperty("leisure", &leisureJ) then leisureJ.GetString()
                                        else "other"

                                    yield { Name = name; Category = category; Lat = latVal; Lon = lonVal } ]

                        return results
        }
