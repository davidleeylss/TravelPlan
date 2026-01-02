namespace TravelPlan.Server.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // 例如：2026 福岡行
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // 誰創立的
        public int OwnerId { get; set; }

        // 新增: 關聯到 TravelGroup
        public int? TravelGroupId { get; set; }
        public TravelGroup? TravelGroup { get; set; }

        // 關聯的資料
        public List<ItineraryItem> Itineraries { get; set; }
        public List<Flight> Flights { get; set; }
        public List<Expense> Expenses { get; set; }
    }
}
