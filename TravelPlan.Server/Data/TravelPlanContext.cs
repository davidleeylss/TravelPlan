using Microsoft.EntityFrameworkCore;
using TravelPlan.Server.Models;

namespace TravelPlan.Server.Data
{
    public class TravelPlanContext : DbContext
    {
        public TravelPlanContext(DbContextOptions<TravelPlanContext> options)
            : base(options)
        {
        }


        public DbSet<User> Users { get; set; } // 登入user
        public DbSet<Trip> Trips { get; set; } // 旅遊行程
        public DbSet<ItineraryItem> ItineraryItems { get; set; } //行程細項
        public DbSet<Flight> Flights { get; set; } // 航班資訊
        public DbSet<Expense> Expenses { get; set; } // 花費記錄
        public DbSet<TravelGroup> TravelGroups { get; set; } // 旅遊群組
    } 
}
