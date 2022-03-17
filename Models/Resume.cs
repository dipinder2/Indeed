using System.ComponentModel.DataAnnotations;

namespace Indeed.Models;

public class Resume
{
    [Key]
    public int ResumeId { get; set; }
    [Required]
    [MinLength(4, ErrorMessage = "Title Too Short!!!")]
    public string? Title { get; set; }
    [Required]
    public string? Url { get; set; }
    public DateOnly PostedOn { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public string UserId { get; set; }
    public User User { get; set; }
}