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
            return await _context.Trips
                        .Include(t => t.TravelGroup) // 載入群組
                        .ThenInclude(g => g.Members) // 載入成員
                        .Where(t => t.OwnerId == userId || t.TravelGroup.Members.Any(u => u.Id == userId))
                        .OrderByDescending(t => t.StartDate)
                        .ToListAsync();
        }

        // 新增旅遊
        [HttpPost]
        public async Task<ActionResult<Trip>> CreateTrip(TripDto request)
        {
            var owner = await _context.Users.FindAsync(request.OwnerId);
            if (owner == null) return BadRequest("User not found");

            // 先建立一個新群組 (或是去資料庫找現有的)
            var newGroup = new TravelGroup
            {
                GroupName = $"{request.Title} 的成員", // 自動命名
                Members = new List<User> { owner } // 預設擁有者在裡面
            };

            // 把其他人加進群組
            if (request.Participants != null && request.Participants.Count > 0)
            {
                var others = await _context.Users
                    .Where(u => request.Participants.Contains(u.Username))
                    .ToListAsync();
                newGroup.Members.AddRange(others);
            }

            // 建立旅遊，並綁定這個群組
            var newTrip = new Trip
            {
                Title = request.Title,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                OwnerId = request.OwnerId,
                TravelGroup = newGroup // 綁定群組
            };

            _context.Trips.Add(newTrip);
            await _context.SaveChangesAsync();

            return Ok(newTrip);
        }

        // PUT: 編輯旅遊 (修改標題、時間、成員)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrip(int id, TripDto request)
        {
            // 找出旅遊 (包含群組與成員)
            var trip = await _context.Trips
                .Include(t => t.TravelGroup)
                .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound("找不到該旅遊");

            // 權限檢查：只有擁有者可以編輯旅遊資訊
            if (trip.OwnerId != request.OwnerId)
            {
                return StatusCode(403, "只有擁有者可以編輯旅遊資訊");
            }

            // 更新基本資料
            trip.Title = request.Title;
            trip.StartDate = request.StartDate;
            trip.EndDate = request.EndDate;

            // 更新成員邏輯
            if (request.Participants != null && trip.TravelGroup != null)
            {
                // 找出目前資料庫裡的成員
                var currentMembers = trip.TravelGroup.Members.ToList();

                // 前端傳來的成員名單 (使用者名稱)
                var newMemberNames = request.Participants;

                // 要新增的人 (在傳來的名單中，但不在資料庫裡)
                var usersToAdd = await _context.Users
                    .Where(u => newMemberNames.Contains(u.Username) && !currentMembers.Select(m => m.Username).Contains(u.Username))
                    .ToListAsync();

                // 要移除的人 (在資料庫裡，但不在傳來的名單中)
                // 注意：絕對不能移除擁有者 (Owner)
                var usersToRemove = currentMembers
                    .Where(m => !newMemberNames.Contains(m.Username) && m.Id != trip.OwnerId)
                    .ToList();

                // 執行新增與移除
                if (usersToAdd.Any()) trip.TravelGroup.Members.AddRange(usersToAdd);
                foreach (var user in usersToRemove)
                {
                    trip.TravelGroup.Members.Remove(user);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(trip);
        }

        // DELETE: 刪除或是退出旅遊
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id, [FromQuery] int userId)
        {
            var trip = await _context.Trips
                .Include(t => t.TravelGroup)
                .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            // 情況 A: 如果是「擁有者」刪除 -> 整個旅遊消失 (Hard Delete)
            if (trip.OwnerId == userId)
            {
                // 連同群組一起刪除 (視需求，這邊先只刪除 Trip，DB 設定 Cascade 會自動清關聯)
                _context.Trips.Remove(trip);

                // 把那個群組刪掉
            }
            // 情況 B: 如果是「參加者」刪除 -> 只是退出 (Leave Group)
            else
            {
                var user = trip.TravelGroup?.Members.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    trip.TravelGroup.Members.Remove(user);
                }
                else
                {
                    return BadRequest("你不是這個旅遊的成員");
                }
            }

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
        public List<string> Participants { get; set; } // 前端傳來參加者的名字
    }
}
