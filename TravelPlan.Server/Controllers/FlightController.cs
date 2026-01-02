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
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights([FromQuery] int tripId)
        {
            // 只回傳該旅遊 ID (TripId) 的機票
            return await _context.Flights
                .Where(f => f.TripId == tripId)
                .ToListAsync();
        }

        // 更新或新增機票：POST api/flight
        [HttpPost]
        public async Task<ActionResult<Flight>> CreateOrUpdateFlight(Flight request)
        {
            if (request.TripId == 0) return BadRequest("TripId cannot be 0");

            // 情況 1: 更新現有機票 (依靠 Id)
            if (request.Id != 0)
            {
                var existingFlight = await _context.Flights.FindAsync(request.Id);
                if (existingFlight == null) return NotFound("找不到該機票");

                // 更新欄位
                existingFlight.Date = request.Date;
                existingFlight.Airline = request.Airline;
                existingFlight.Departure = request.Departure;
                existingFlight.Arrival = request.Arrival;
                existingFlight.DepartureTime = request.DepartureTime;
                existingFlight.ArrivalTime = request.ArrivalTime;
                existingFlight.Number = request.Number;

                await _context.SaveChangesAsync();
                return Ok(existingFlight);
            }
            // 情況 2: 新增一段航程 (Id 為 0)
            else
            {
                _context.Flights.Add(request);
                await _context.SaveChangesAsync();
                return Ok(request);
            }
        }

        // 刪除單張機票 (用於移除轉機行程)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null) return NotFound();

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
