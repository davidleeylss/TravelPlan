using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlan.Server.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public string ItemName { get; set; } = string.Empty; // 項目名稱 (例如: 拉麵)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // 金額

        [Required]
        public string PayerName { get; set; } = string.Empty; // 付款人 (Me 或 Friend)

        public int TripId { get; set; }
    }
}
