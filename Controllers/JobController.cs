using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Indeed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;

namespace Indeed.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize]
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
    
    
    /*
     * Applying for the job and needs JWT Token to retrieve User Information
     */
    [HttpGet("{Jid}")]
    [ActionName("Apply")]
    public async Task<IActionResult> Apply(int Jid=1)
    {
        _logger.LogInformation("Comes herer");
        var jwt = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty);
        var handler = new JwtSecurityTokenHandler();
        var Uid = handler.ReadJwtToken(jwt).Id;

        try
        {
            var job = await _siteContext.Jobs.Include("Candidates").Where(j => j.JobId == Jid).FirstOrDefaultAsync();
            var user = await _userManager.Users.Where(j => j.Id == Uid).FirstOrDefaultAsync(); 
            if (job == null)
            {
                throw new Exception("Job is no longer available!!!");
            }
            if (user == null)
            {
                throw new Exception("User is longer available!!!");
            }

            if (user.Id == job.UserId)
            {
                throw new Exception("You cannot apply for your own Job!!!");
            }
            else if (job.Candidates.Contains(user))
            {
                throw new Exception("Already Applied for this job!!!");
            }
            
            else if (job.Candidates.Count < 1)
            {
                job.Candidates = new List<User>() { user};
            }
            else
            {
                job.Candidates?.Add(user);
            }

            await _siteContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return BadRequest(new {status = 300, message =e.Message});
        }

        return Ok(new {status = 200, message = "You Applied for the job! Congratulations!!!"});
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
        var j = await _siteContext.Jobs.Include("Candidates").Where(j => j.JobId == id).FirstOrDefaultAsync();
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