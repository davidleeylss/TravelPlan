namespace TravelPlan.Server.Models
{
    public class TravelGroup
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = "未命名群組"; 

        // 這個群組裡有哪些人 (多對多)
        public List<User> Members { get; set; } = new List<User>();

        // 這個群組被哪些旅遊使用 (一對多)
        public List<Trip> Trips { get; set; }
    }
}
