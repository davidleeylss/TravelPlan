using BCrypt.Net;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TravelPlan.Server.Data;
using TravelPlan.Server.Models;

namespace TravelPlan.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TravelPlanContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(TravelPlanContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 註冊
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest("帳號已存在");

            // 密碼加密
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "註冊成功" });
        }

        // 登入
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);

            // 驗證帳號與加密後的密碼
            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.Password))
                return Unauthorized("帳號或密碼錯誤");

            var token = GenerateJwtToken(dbUser);
            return Ok(new { token, username = dbUser.Username, id = dbUser.Id });
        }

        // 產生 JWT Token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("id", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(7), // Token 7天有效
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Google 登入 (驗證 Google Token + 發 JWT)
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            try
            {
                // 使用 HttpClient 去問 Google 這個 Access Token 的主人是誰
                // (不依賴本地時間，所以不會有 JWT 時間誤差問題)
                using var client = new HttpClient();
                var googleUser = await client.GetFromJsonAsync<GoogleUserResponse>(
                    $"https://www.googleapis.com/oauth2/v3/userinfo?access_token={request.AccessToken}");

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Sub))
                {
                    return BadRequest("無效的 Google Token");
                }

                // 檢查 DB 有沒有這個人 (用 Google 的唯一 ID "Sub" 來找)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleUser.Sub);

                if (user == null)
                {
                    // 沒人就自動註冊
                    user = new User
                    {
                        Username = googleUser.Name,
                        GoogleId = googleUser.Sub, // Google 的 User ID
                        Password = ""
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // 發 JWT Token 給前端
                var token = GenerateJwtToken(user);
                return Ok(new { token, username = user.Username, id = user.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest($"Google 驗證失敗: {ex.Message}");
            }
        }
        // 為了接收 Google 前端傳來的物件
        public class GoogleLoginDto
        {
            public string AccessToken { get; set; }
        }
        // 接 Google API 回傳的使用者資料
        public class GoogleUserResponse
        {
            public string Sub { get; set; }  // Google ID
            public string Name { get; set; } // 姓名
            public string Email { get; set; } // 信箱
        }

        // 取得所有使用者 (給下拉選單選參加者用)
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllUsers()
        {
            return await _context.Users.Select(u => u.Username).ToListAsync();
        }
    }
}
