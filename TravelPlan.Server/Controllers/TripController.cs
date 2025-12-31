using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlan.Server.Data;
using TravelPlan.Server.Models;

namespace TravelPlan.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly TravelPlanContext _context;

        public TripController(TravelPlanContext context)
        {
            _context = context;
        }

        // 取得目前使用者的所有旅遊
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trip>>> GetMyTrips(int userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.Trips)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        // 新增旅遊
        [HttpPost]
        public async Task<ActionResult<Trip>> CreateTrip(TripDto request)
        {
            var owner = await _context.Users.FindAsync(request.OwnerId);
            if (owner == null) return BadRequest("User not found");

            var newTrip = new Trip
            {
                Title = request.Title,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                OwnerId = request.OwnerId,
                Participants = new List<User> { owner } // 創立者預設加入
            };

            // 如果有選其他參加者 (傳入 Username 陣列)
            if (request.ParticipantNames != null)
            {
                var others = await _context.Users
                    .Where(u => request.ParticipantNames.Contains(u.Username))
                    .ToListAsync();
                newTrip.Participants.AddRange(others);
            }

            _context.Trips.Add(newTrip);
            await _context.SaveChangesAsync();

            return Ok(newTrip);
        }

        // 刪除旅遊
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class TripDto
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int OwnerId { get; set; }
        public List<string> ParticipantNames { get; set; } // 前端傳來參加者的名字
    }
}
