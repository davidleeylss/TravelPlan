namespace TravelPlan.Server.Models
{
    public class Flight
    {
        public int Id { get; set; }

        // 儲存參加者名單，例如 "UserA,UserB"
        public string Participants { get; set; } = string.Empty;

        // "Outbound"(去程) 或 "Inbound"(回程)
        public string Type { get; set; } = string.Empty;

        public DateTime Date { get; set; }
        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string Departure { get; set; } = string.Empty;
        public string Arrival { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public int TripId { get; set; }
    }
}
