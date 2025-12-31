using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlan.Server.Data;
using TravelPlan.Server.Models;

namespace TravelPlan.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private readonly TravelPlanContext _context;

        public FlightController(TravelPlanContext context)
        {
            _context = context;
        }

        // 取得某人的機票資訊：GET api/flight?user=UserA
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] int tripId, [FromQuery] string user)
        {
            return await _context.Flights
                        .Where(f => f.TripId == tripId)            // 鎖定旅遊 ID
                        .Where(f => f.Participants.Contains(user))
                        .ToListAsync();
        }

        // 更新或新增機票：POST api/flight
        [HttpPost]
        public async Task<IActionResult> SaveFlight([FromBody] Flight flight)
        {
            if (flight.Id > 0)
            {
                // --- 更新現有資料 (Update) ---

                // 先去資料庫把舊資料抓出來
                var existing = await _context.Flights.FindAsync(flight.Id);

                if (existing != null)
                {
                    // 更新所有欄位
                    // 記得更新 Participants (參加者)
                    existing.Participants = flight.Participants;

                    existing.Date = flight.Date;
                    existing.DepartureTime = flight.DepartureTime;
                    existing.ArrivalTime = flight.ArrivalTime;
                    existing.Departure = flight.Departure;
                    existing.Arrival = flight.Arrival;
                    existing.Airline = flight.Airline;
                    existing.Number = flight.Number;
                }
                else
                {
                    return NotFound("找不到該筆機票資料");
                }
            }
            else
            {
                // --- 新增全新資料 (Insert) ---
                _context.Flights.Add(flight);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
