using System.ComponentModel.DataAnnotations;

namespace TravelPlan.Server.Models
{
    public class ItineraryItem
    {
        public int Id { get; set; }
        public string Participants { get; set; } = string.Empty;

        [Required] // 必填
        public DateTime Date { get; set; } // 日期 (2025-04-10)

        [Required]
        public string Time { get; set; } = string.Empty; // 時間 (14:30)

        [Required]
        public string Location { get; set; } = string.Empty; // 地點

        public string? Note { get; set; } // 備註 (可空)

        // 這些欄位不存入資料庫，只是 API 回傳時用來裝天氣資訊
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? Temperature { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? WeatherIcon { get; set; }

        public int TripId { get; set; }
    }
}
