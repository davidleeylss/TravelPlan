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

        public DbSet<ItineraryItem> ItineraryItems { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        public DbSet<Flight> Flights { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Trip> Trips { get; set; }
    }
}
