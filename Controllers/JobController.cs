using System.ComponentModel.DataAnnotations;
using Indeed.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indeed.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[EnableCors(policyName: "MyAllowSpecificOrigins")]
public class JobController : ControllerBase
{
    private readonly JobSiteContext _siteContext;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserController> _logger;

    public JobController(JobSiteContext siteContext, UserManager<User> userManager, ILogger<UserController> logger)
    {
        _siteContext = siteContext;
        _userManager = userManager;
        _logger = logger;
    }
    

    [HttpGet]
    [ActionName("Get")]
    public async Task<List<Job>> Get()
    {
        var dict = _siteContext.Jobs.Include("User").ToList();
        return dict;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var j = await _siteContext.Jobs.Where(j => j.JobId == id).FirstOrDefaultAsync();
        if (j == null)
        {
            var k = new
            {
                status=401,
                description = "Non Existent Job Or(Wrong Id)"
            };
            return BadRequest(k);
        }
        return Ok(j);
    }
    [HttpPost]
    [ActionName("Create")]
    public async Task<IActionResult> Create([FromBody]Job job)
    {
        try
        {
            string id = job.UserId;
            var user = await _userManager.Users.Where(user => user.Id == id).FirstOrDefaultAsync();
            if (user == null) throw new Exception($"`{id}` user doesn't exists!!!");
            job.User = user;
            var j = await _siteContext.Jobs.AddAsync(job);
            _logger.LogInformation(j.ToString());
            _siteContext.SaveChanges();
        }
        catch(Exception e)
        {
            return BadRequest(new {status=400,message=e.Message});
        }
       return Ok(new {status=200,message="new Job Posted"});
    }
    
    
    
}