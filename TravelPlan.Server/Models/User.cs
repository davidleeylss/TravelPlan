namespace TravelPlan.Server.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string? GoogleId { get; set; }

        public List<Trip> Trips { get; set; } = new List<Trip>();
    }
}
