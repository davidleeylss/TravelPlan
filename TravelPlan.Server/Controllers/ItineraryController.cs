using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlan.Server.Data;
using TravelPlan.Server.Models;

namespace TravelPlan.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItineraryController : ControllerBase
    {
        private readonly TravelPlanContext _context;

        public ItineraryController(TravelPlanContext context)
        {
            _context = context;
        }

        // 取得某天的行程
        // GET: api/itinerary?date=2025-04-10&user=UserA
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItineraryItem>>> GetItineraries(DateTime date, string user)
        {
            // 檢查 Participants 是否包含 user
            var items = await _context.ItineraryItems
                                      .Where(i => i.Date.Date == date.Date)
                                      .Where(i => i.Participants.Contains(user)) // 只傳回有參加的行程
                                      .OrderBy(i => i.Time)
                                      .ToListAsync();

            // 模擬天氣資訊
            var rng = new Random();
            foreach (var item in items)
            {
                item.Temperature = rng.Next(15, 25).ToString();
                item.WeatherIcon = "fa-solid fa-cloud-sun";
            }

            return items;
        }

        // 新增行程
        // POST: api/itinerary
        [HttpPost]
        public async Task<ActionResult<ItineraryItem>> PostItinerary(ItineraryItem item)
        {
            _context.ItineraryItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // 3. 刪除行程
        // DELETE: api/itinerary/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItinerary(int id)
        {
            var item = await _context.ItineraryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.ItineraryItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 4. 修改行程
        // PUT: api/itinerary/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItinerary(int id, ItineraryItem item)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ItineraryItems.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
    }
}