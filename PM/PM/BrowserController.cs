using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Web;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PM.Models;
using PM.Data;
using PM.Services;
using System.Drawing;
using PM.Controllers;

namespace PM
{
    [Route("[controller]")]
    [ApiController]
    public class BrowserController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserAccountsController> _logger;

        public BrowserController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ApplicationDbContext context, ILogger<UserAccountsController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [RequireHttps]
        [HttpGet("getUserNames")]
        public async Task<List<string>> getUserNames(string accountName)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                _logger.LogInformation("Unauthenticated user requested list of usernames");
                Response.StatusCode = 401;
                return null;
            }
            else
            {
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userID == null)
                {
                    _logger.LogCritical("Claim not found.");
                    Response.StatusCode = 500;
                    return null;
                }

                IdentityUser user = await _userManager.FindByIdAsync(userID);
                if(user == null)
                {
                    _logger.LogCritical("Identity user object could not be found for userID: " + userID);
                    Response.StatusCode = 500;
                    return null;
                }

                if(_context == null)
                {
                    _logger.LogCritical("Application Database Context was null");
                    Response.StatusCode = 500;
                    return null;
                }
                FileDB obj = _context.FileDB.Find(userID);

                if (!UserAccount.IsValidAccountName(accountName))
                {
                    Response.StatusCode = 400;
                    return null;
                }
                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    Response.StatusCode = 500;
                    return null;
                }

                byte[] encryptedData = System.IO.File.ReadAllBytes("Passwords/" + obj.FileName);
                List<string> userNames = new List<string>();
                if (encryptedData.Length == 0)
                {
                    _logger.LogInformation(user.UserName + " requested for Usernames from an empty data base");
                    return null;
                }
                else
                {
                    try
                    {
                        Crypto crypto_obj = new Crypto();
                        crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                        string data = crypto_obj.DecryptStringFromBytes_Aes(encryptedData);
                        UserAccounts a = System.Text.Json.JsonSerializer.Deserialize<UserAccounts>(data)!;
                        
                        foreach (var Account in a.userAccounts)
                        {
                            if (string.Equals(Account.AccountName, accountName))
                            {
                                userNames.Add(Account.UserName);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }

                    return userNames;
                }
            }
        }


        [RequireHttps]
        [HttpGet("getUserNamesFromURL")]
        public async Task<List<string>> getUserNamesFromURL(string URL)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                _logger.LogInformation("Unauthenticated user requested list of usernames");
                Response.StatusCode = 401;
                return null;
            }
            else
            {
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userID == null)
                {
                    _logger.LogCritical("Claim not found.");
                    Response.StatusCode = 500;
                    return null;
                }

                IdentityUser user = await _userManager.FindByIdAsync(userID);
                if (user == null)
                {
                    _logger.LogCritical("Identity user object could not be found for userID: " + userID);
                    Response.StatusCode = 500;
                    return null;
                }

                if (_context == null)
                {
                    _logger.LogCritical("Application Database Context was null");
                    Response.StatusCode = 500;
                    return null;
                }
                FileDB obj = _context.FileDB.Find(userID);

                if (!UserAccount.IsValidURL(URL))
                {
                    Response.StatusCode = 400;
                    return null;
                }
                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    Response.StatusCode = 500;
                    return null;
                }

                byte[] encryptedData = System.IO.File.ReadAllBytes("Passwords/" + obj.FileName);
                List<string> userNames = new List<string>();
                if (encryptedData.Length == 0)
                {
                    _logger.LogInformation(user.UserName + " requested for Usernames from an empty data base");
                    return null;
                }
                else
                {
                    try
                    {
                        Crypto crypto_obj = new Crypto();
                        crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                        string data = crypto_obj.DecryptStringFromBytes_Aes(encryptedData);
                        UserAccounts a = System.Text.Json.JsonSerializer.Deserialize<UserAccounts>(data)!;

                        foreach (var Account in a.userAccounts)
                        {
                            if (string.Equals(Account.URL, URL))
                            {
                                userNames.Add(Account.UserName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }

                    return userNames;
                }
            }
        }

        [RequireHttps]
        [HttpGet("getPassword")]
        public async Task<string> getPassword(string accountName, string userName)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                _logger.LogInformation("Unauthenticated user requested password");
                Response.StatusCode = 401;
                return null;
            }
            else
            {
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userID == null)
                {
                    _logger.LogCritical("Claim not found.");
                    Response.StatusCode = 500;
                    return null;
                }

                IdentityUser user = await _userManager.FindByIdAsync(userID);
                if (user == null)
                {
                    _logger.LogCritical("Identity user object could not be found for userID: " + userID);
                    Response.StatusCode = 500;
                    return null;
                }

                if (_context == null)
                {
                    _logger.LogCritical("Application Database Context was null");
                    Response.StatusCode = 500;
                    return null;
                }

                FileDB obj = _context.FileDB.Find(userID);

                if (!UserAccount.IsValidAccountName(accountName))
                {
                    Response.StatusCode = 400;
                    return null;
                }
                if (!UserAccount.IsValidUserName(userName))
                {
                    Response.StatusCode = 400;
                    return null;
                }

                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    Response.StatusCode = 500; //Internal Server Error
                    return null;
                }
                byte[] encryptedData = System.IO.File.ReadAllBytes("Passwords/" + obj.FileName);
                if (encryptedData.Length == 0)
                {
                    _logger.LogInformation(user.UserName + " requested for Password from an empty data base");
                    return null;    //No accounts created by current user
                }
                else
                {
                    try
                    {
                        Crypto crypto_obj = new Crypto();
                        crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                        string data = crypto_obj.DecryptStringFromBytes_Aes(encryptedData);
                        UserAccounts a = System.Text.Json.JsonSerializer.Deserialize<UserAccounts>(data)!;
                        foreach (var Account in a.userAccounts)
                        {
                            if (string.Equals(Account.AccountName, accountName) && string.Equals(Account.UserName, userName))
                            {
                                return Account.Password;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    _logger.LogTrace(user.UserName + " requested a password that could not be found");
                    return null;    //No password corresponding to given accountname and username
                }
            }
        }

        [RequireHttps]
        [HttpGet("getPasswordUsingURL")]
        public async Task<string> getPasswordUsingURL(string URL, string userName)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                _logger.LogInformation("Unauthenticated user requested password");
                Response.StatusCode = 401;
                return null;
            }
            else
            {
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userID == null)
                {
                    _logger.LogCritical("Claim not found.");
                    Response.StatusCode = 500;
                    return null;
                }

                IdentityUser user = await _userManager.FindByIdAsync(userID);
                if (user == null)
                {
                    _logger.LogCritical("Identity user object could not be found for userID: " + userID);
                    Response.StatusCode = 500;
                    return null;
                }

                if (_context == null)
                {
                    _logger.LogCritical("Application Database Context was null");
                    Response.StatusCode = 500;
                    return null;
                }

                FileDB obj = _context.FileDB.Find(userID);

                if (!UserAccount.IsValidURL(URL))
                {
                    Response.StatusCode = 400;
                    return null;
                }
                if (!UserAccount.IsValidUserName(userName))
                {
                    Response.StatusCode = 400;
                    return null;
                }

                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    Response.StatusCode = 500; //Internal Server Error
                    return null;
                }
                byte[] encryptedData = System.IO.File.ReadAllBytes("Passwords/" + obj.FileName);
                if (encryptedData.Length == 0)
                {
                    _logger.LogInformation(user.UserName + " requested for Password from an empty data base");
                    return null;    //No accounts created by current user
                }
                else
                {
                    try
                    {
                        Crypto crypto_obj = new Crypto();
                        crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                        string data = crypto_obj.DecryptStringFromBytes_Aes(encryptedData);
                        UserAccounts a = System.Text.Json.JsonSerializer.Deserialize<UserAccounts>(data)!;
                        foreach (var Account in a.userAccounts)
                        {
                            if (string.Equals(Account.URL, URL) && string.Equals(Account.UserName, userName))
                            {
                                return Account.Password;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                    _logger.LogTrace(user.UserName + " requested a password that could not be found");
                    return null;    //No password corresponding to given accountname and URL
                }
            }
        }


        [RequireHttps]
        [HttpPost("Login")]
        public async Task<string> Login()
        {
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if(authHeader != null && authHeader.StartsWith("Basic")) {
                try
                {
                    string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                    Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                    string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
                    int seperatorIndex = usernamePassword.IndexOf(':');

                    var username = usernamePassword.Substring(0, seperatorIndex);
                    var password = usernamePassword.Substring(seperatorIndex + 1);
                    var result = await _signInManager.PasswordSignInAsync(username, password, false, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(username + " logged in through browser extension");
                        return "Login Successful";
                    }
                    else if (result.IsLockedOut)
                    {
                        _logger.LogInformation(username + " tried to login when locked out");
                        Response.StatusCode = 429;
                        return "Account Locked Out";
                    }
                    else
                    {
                        _logger.LogInformation(username + " unsuccessful log in");
                        Response.StatusCode = 401;
                        return "Login Failed";
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return "Unknown error occurred";
                }
            }
            else
            {
                _logger.LogInformation("Authorization Header Could Not Be Processed");
                Response.StatusCode = 400; //Bad request
                return null;
            }
        }
    }
}
