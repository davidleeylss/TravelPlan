namespace TravelPlan.Server.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string? GoogleId { get; set; }

        // 參加了哪些群組
        public List<TravelGroup> Groups { get; set; } = new List<TravelGroup>();
    }
}
