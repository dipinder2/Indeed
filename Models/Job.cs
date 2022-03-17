﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Indeed.Models;

public class Job
{
    [Key]
    public int JobId { get; set; }
    
    public string? Title { get; set; }
    [Required]
    [StringLength(1024), MinLength(4, ErrorMessage = "Description Too Short")]
    public string? Description { get; set; }

    public DateTime PostedOn { get; set; } =DateTime.Now;
    [ForeignKey("UserId")]
    public string? UserId { get; set; }


    //1
    public User? User { get; set; }
    
    public List<User>? Candidates { get; set; }
}