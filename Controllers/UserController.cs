using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Indeed.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Indeed.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
    [ApiController]
    [EnableCors(policyName: "MyAllowSpecificOrigins")]
    public class UserController : ControllerBase
    {
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager; 
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        
        public UserController(UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager, 
            ILogger<UserController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _emailStore = GetEmailStore();
            _logger = logger;
            _configuration = configuration;
        }

        // GET: api/<UserController>
        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.ActionName("Get")]
        public async Task<List<User>> Get()
        {
            var dict = _userManager.Users.Include("JobsCreated").ToList();
            return dict;
        }

        // GET api/<UserController>/5
        [Microsoft.AspNetCore.Mvc.HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(string id)
        {
            try
            {
                var user = await _userManager.Users
                    .Where(u => u.Id == id).FirstOrDefaultAsync();
                if (user == null) throw new Exception("User Id Not Found!!!");
                return Ok(user);

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        // POST api/<UserController>
        [Microsoft.AspNetCore.Mvc.HttpPost]
        public async Task<ActionResult> Post([Microsoft.AspNetCore.Mvc.FromBody] User value)
        {
            try
            {
                ModelState.Remove("JobsCreated");
                ModelState.Remove("AppliedJobs");
                if (ModelState.IsValid)
                {
                    var user = CreateUser();

                    await _userStore.SetUserNameAsync(user, value.Email, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, value.Email, CancellationToken.None);

                    var result = await _userManager.CreateAsync(user, value.Password);

                    if (result.Succeeded)
                    {
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        await _signInManager.SignInAsync(user, isPersistent: false);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        throw new Exception();
                    }

                    var data = new {status = 200, result = "Created User"};
                    return Ok(data);
                }

                throw new Exception();
            }
            catch
            {
                return BadRequest(ModelState);
            }
            

        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        [Microsoft.AspNetCore.Mvc.ActionName("Login")]
        public async Task<ActionResult> LogIn([Microsoft.AspNetCore.Mvc.FromBody] User Input)
        {
            try
            {
                ModelState.Remove("ConfirmPassword");
                if (ModelState.IsValid)
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, true,
                        lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        var idforuser = await _userManager.FindByEmailAsync(Input.Email);
                       // create claims details based on the user information
                            var claims = new[] {
                                new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
                                new Claim(JwtRegisteredClaimNames.Jti, idforuser.Id),
                                new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                                new Claim("Email", Input.Email)
                            };

                            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                            SigningCredentials signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            JwtSecurityToken token = new JwtSecurityToken(
                                _configuration["Jwt:Issuer"],
                                _configuration["Jwt:Audience"],
                                claims,
                                expires: DateTime.UtcNow.AddMinutes(10),
                                signingCredentials: signIn);

                        _logger.LogInformation($"{Input.Email} signed in");
                        string t = new JwtSecurityTokenHandler().WriteToken(token);
                        CookieOptions co = new CookieOptions();
                        co.Expires = DateTime.UtcNow.Date.AddDays(1);
                        Response.Cookies.Append("ASPAuthenticationToken",t,co);
                        return Ok(t);
                    }

                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        throw new Exception();
                    }
                }

                _logger.LogInformation($"{Input.Email} signed in");
                return Ok($"{Input.Email} signed in");
            }
            catch
            {
                return BadRequest(ModelState);
            }

        }
        
        // GET api/Logout
        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.ActionName("Logout")]
        public async void Logout()
        {
            await _signInManager.SignOutAsync();
            if (Request.Cookies["ASPAuthenticationToken"] != null)
            {
                Response.Cookies.Delete("ASPAuthenticationToken");
            }
            _logger.LogInformation("User logged out.");
        }

        // DELETE api/<UserController>/5
        [Microsoft.AspNetCore.Mvc.HttpDelete("{id}")]
        public void Delete(int id)
        {
        }


        [Microsoft.AspNetCore.Mvc.NonAction]
        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
        [Microsoft.AspNetCore.Mvc.NonAction]
        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                                                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                                                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }
    }