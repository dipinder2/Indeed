using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;


namespace Indeed.Models;

public class User:IdentityUser
{
    [NotMapped]
    [Required]
    public string Password { get; set; }
    [NotMapped]
    [Required]
    [Compare("Password", ErrorMessage = "Password and Confirm Password Must Match!")]
    public string ConfirmPassword { get; set; }
    // /*
    //  * This is for the Resume for the User 1:1
    //  */
    // [ForeignKey("ResumeId")]
    // public int? ResumeId { get; set; }
    // public Resume? Resume { get; set; }
    /*
     * Jobs Posted By User 1:M
     * Where 1 is User &
     * M is Jobs
     */
    
    //Many
    public virtual ICollection<Job>? JobsCreated { get; set; } 
    /*
     * Jobs Applied By User M:M
     * Where M is Users &
     * M is Jobs
     */
    public List<Job>? AppliedJobs { get; set; }
}
