using System;
using System.ComponentModel.DataAnnotations;

namespace BanCode.Models
{
    public class Contact
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        // Ngày gửi
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Trạng thái: Admin đã đọc chưa?
        public bool IsRead { get; set; } = false;
    }
}