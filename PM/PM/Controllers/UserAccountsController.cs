using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PM.Data;
using PM.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using PM.Services;
using System.Xml.Linq;
using System.Text;
using Serilog;
using System.Diagnostics;

namespace PM.Controllers
{
    public class UserAccountsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserAccountsController> _logger;

        public UserAccountsController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context, ILogger<UserAccountsController> logger)
        { 
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        private UserAccounts getUserAccounts(string userID, IdentityUser user)
        {
            try
            {
                FileDB obj = _context.FileDB.Find(userID);
                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
                    return null;
                }

                byte[] encryptedData = System.IO.File.ReadAllBytes("Passwords/" + obj.FileName);
                if (encryptedData.Length == 0)  //No passwords exist in the database
                    return null;
                else
                {
                    Crypto crypto_obj = new Crypto();
                    crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                    string data = crypto_obj.DecryptStringFromBytes_Aes(encryptedData);
                    return System.Text.Json.JsonSerializer.Deserialize<UserAccounts>(data)!;
                }
            }
            catch(Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message); //Most likely to catch an exception while deserializing JSON Data
            }
            return null;
        }

        private void writeToFile(UserAccounts accList, IdentityUser user) 
        {
            try
            {
                string jsonString = System.Text.Json.JsonSerializer.Serialize(accList);

                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(userID == null)
                {
                    _logger.LogCritical("Claim not found");
                    ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                    return;
                }

                FileDB obj = _context.FileDB.Find(userID);

                if (obj == null)
                {
                    _logger.LogCritical("Signed In User " + user.UserName + " not found in Password File DB");
                    ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                    return;
                }

                Crypto crypto_obj = new Crypto();
                crypto_obj.setKeyandIV(user.SecurityStamp, Encoding.ASCII.GetBytes(user.PasswordHash));
                byte[] encryptedData = crypto_obj.EncryptStringtoBytes_Aes(jsonString);
                System.IO.File.WriteAllBytes(@"Passwords/" + obj.FileName, encryptedData);
                _logger.LogInformation("Password DB of " + user.UserName + "updated");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

        }

        // GET: UserAccounts
        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login");
            }
            
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
                return View();
            }

            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if(user == null)
            {
                _logger.LogCritical("Identity User object could not be found for userID: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
                return View();
            }

            if(_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
                return View();
            }
            UserAccounts a = getUserAccounts(userID, user);
            if(ModelState.ErrorCount > 0)
            {
                return View();
            }
            _logger.LogInformation(user.UserName + " requested Index page");
            return View(a);
        }

        // GET: UserAccounts/Details/
        public async Task<IActionResult> Details([FromQuery(Name = "accName")] string AccountName = null, [FromQuery(Name = "UserName")] string UserName = null)
        {

            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login"); 
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Bad Request");
                return View();
            }

            //Custom Validations
            if (!UserAccount.IsValidAccountName(AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid Account Name");
                return View();
            }
            if (!UserAccount.IsValidUserName(UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid User Name");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }

            if(a == null)
            {
                ModelState.AddModelError(string.Empty, "You have not stored any passwords");
                return View();
            }

            _logger.LogInformation(user.UserName + " requested details of an account");
            foreach (var Account in a.userAccounts)
            {
                if(string.Equals(Account.AccountName, AccountName) && string.Equals(Account.UserName, UserName))
                {
                    return View(Account);
                }
            }

            _logger.LogInformation("Requested details could not be found");
            ModelState.AddModelError(string.Empty, "No records with given Account Name and User Name");
            return View();
        }

        // GET: UserAccounts/Create
        public IActionResult Create()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login");
            }
            return View();
        }

        // POST: UserAccounts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccId,AccountName,UserName,Password,URL,ReminderScheduled")] UserAccount userAccount)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login");
            }

            if(!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Bad request");
                return View();
            }

            //Custom Validations
            if(!UserAccount.IsValidAccountName(userAccount.AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter an account name that contains less than 50 alphanumeric characters");
                return View();
            }
            if (!UserAccount.IsValidUserName(userAccount.UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a user name that contains less than 50 alphanumeric or . or _ characters");
                return View();
            }
            if (!UserAccount.IsValidPassword(userAccount.Password))
            {
                ModelState.AddModelError(string.Empty, "Please enter a password of length between 8 and 50");
                return View();
            }
            if (!UserAccount.IsValidURL(userAccount.URL))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid URL");
                return View();
            }
            userAccount.LastUpdated = DateTime.Now;
            if (!UserAccount.IsValidReminderDate(userAccount.ReminderScheduled, userAccount.LastUpdated))
            {
                ModelState.AddModelError(string.Empty, "Reminder time cannot be in the past");
                _logger.LogWarning(userAccount.UserName + " created an account with reminder set in the past");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }
            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }

            if (a == null)
            {
                a = new UserAccounts();
                a.userAccounts = new List<UserAccount>();
            }
            foreach(var account in a.userAccounts)
            {
                if(string.Equals(account.AccountName, userAccount.AccountName) && string.Equals(account.UserName, userAccount.UserName))
                {
                    _logger.LogWarning(userAccount.UserName + " tried to create an account who's details already exist");
                    ModelState.AddModelError(String.Empty, "An entry with this Account Name and User Name already exists. Please edit that entry if required.");
                    return View();
                }
            }
            a.userAccounts.Add(userAccount);
            writeToFile(a, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }

            _logger.LogInformation(user.UserName + " created a new entry");
            return Redirect("Index");
        }

        // GET: UserAccounts/Edit/5
        public async Task<IActionResult> Edit([FromQuery(Name = "accName")] string AccountName = "", [FromQuery(Name = "UserName")] string UserName = "")
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login"); 
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Bad Request");
                return View();
            }

            if (!UserAccount.IsValidAccountName(AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid account name");
                return View();
            }
            if (!UserAccount.IsValidUserName(UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid user name");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }

            if (a == null)
            {
                _logger.LogWarning("No user accounts were found even though user was trying to edit an account");
                ModelState.AddModelError(string.Empty, "No accounts exist to edit");
                return View();
            }
            foreach (var account in a.userAccounts)
            {
                if(string.Equals(account.AccountName,AccountName) && string.Equals(account.UserName,UserName))
                {
                    _logger.LogInformation(user.UserName + " is tring to edit an entry");
                    return View(account);
                }
            }

            _logger.LogError("The entry the user wanted to edit could not be found");
            ModelState.AddModelError(string.Empty, "The entry you are trying to edit does not exist. Please use the create entry option");
            return View();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("AccountName,UserName,Password,URL,ReminderScheduled")] UserAccount userAccount)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login");
            }

            if(!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Bad request");
                return View();
            }

            //Custom Validations
            if (!UserAccount.IsValidAccountName(userAccount.AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter an account name that contains less than 50 alphanumeric characters");
                return View();
            }
            if (!UserAccount.IsValidUserName(userAccount.UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a user name that contains less than 50 alphanumeric or . or _ characters");
                return View();
            }
            if (!UserAccount.IsValidPassword(userAccount.Password))
            {
                ModelState.AddModelError(string.Empty, "Please enter a password of length between 8 and 50");
                return View();
            }
            if (!UserAccount.IsValidURL(userAccount.URL))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid URL");
                return View();
            }
            userAccount.LastUpdated = DateTime.Now;
            if (!UserAccount.IsValidReminderDate(userAccount.ReminderScheduled, userAccount.LastUpdated))
            {
                ModelState.AddModelError(string.Empty, "Reminder time cannot be in the past");
                _logger.LogWarning(userAccount.UserName + " created an account with reminder set in the past");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }
            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }

            if (a == null)
            {
                _logger.LogWarning("No user accounts were found even though user was trying to edit an account");
                ModelState.AddModelError(string.Empty, "There are no accounts to edit. Please create an account before trying to edit");
                return View();
            }
            
            foreach(var account in a.userAccounts)
            {
                if(string.Equals(userAccount.AccountName,account.AccountName) && string.Equals(userAccount.UserName,account.UserName))
                {
                    account.Password = userAccount.Password;
                    account.URL = userAccount.URL;
                    account.ReminderScheduled = userAccount.ReminderScheduled;
                    account.LastUpdated = userAccount.LastUpdated;
                    writeToFile(a, user);
                    if (ModelState.ErrorCount > 0)
                    {
                        return View();
                    }
                    _logger.LogInformation(user.UserName + " finished editing an entry");
                    return Redirect("Details?accName=" + account.AccountName + "&UserName=" + account.UserName);
                }
            }

            _logger.LogError("The entry the user wanted to edit could not be found");
            ModelState.AddModelError(string.Empty, "You tried to edit an entry that wasn't present");
            return View();
        }

        public async Task<IActionResult> Delete([FromQuery(Name = "accName")] string AccountName = "", [FromQuery(Name = "UserName")] string UserName = "")
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login"); //change redirection method
            }

            if (!ModelState.IsValid) 
            {
                ModelState.AddModelError(string.Empty, "Bad request");
                return View();
            }

            if (!UserAccount.IsValidAccountName(AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid account name");
                return View();
            }
            if (!UserAccount.IsValidUserName(UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid user name");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }
            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }
            if (a == null)
            {
                _logger.LogWarning("No user accounts were found even though user was trying to delete an account");
                ModelState.AddModelError(string.Empty, "Please create an entry before trying to delete one");
                return View();
            }

            foreach (var account in a.userAccounts)
            {
                if (string.Equals(account.AccountName, AccountName) && string.Equals(account.UserName, UserName))
                {
                    return View(account);
                }
            }

            _logger.LogError("The entry the user wanted to delete could not be found");
            ModelState.AddModelError(string.Empty, "The entry you are trying to delete does not exist");
            return View();
            
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string AccountName, string UserName)
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("../Identity/Account/Login"); //change redirection method
            }

            if(!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Bad request");
                return View();
            }

            if (!UserAccount.IsValidAccountName(AccountName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid account name");
                return View();
            }
            if (!UserAccount.IsValidUserName(UserName))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid user name");
                return View();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userID == null)
            {
                _logger.LogCritical("Claim not found.");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            IdentityUser user = await _userManager.FindByIdAsync(userID);
            if (user == null)
            {
                _logger.LogCritical("Identity User object not found for user: " + userID);
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                return View();
            }

            if (_context == null)
            {
                _logger.LogCritical("DataBase Context is null");
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later");
            }
            UserAccounts a = getUserAccounts(userID, user);
            if (ModelState.ErrorCount > 0)
            {
                return View();
            }
            if (a == null)
            {
                _logger.LogWarning("No user accounts were found even though user was trying to delete an account");
                ModelState.AddModelError(string.Empty, "Please create an entry before trying to delete one");
                return View();
            }
            foreach (var account in a.userAccounts)
            {
                if (string.Equals(account.AccountName, AccountName) && string.Equals(account.UserName, UserName))
                {
                    if(!a.userAccounts.Remove(account))
                    {
                        ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                        return View();
                    }

                    writeToFile(a, user);
                    if (ModelState.ErrorCount > 0)
                    {
                        return View();
                    }
                    _logger.LogInformation(user.UserName + " deleted an entry");
                    return Redirect("Index");
                }
            }
            _logger.LogError("The entry the user wanted to delete could not be found");
            ModelState.AddModelError(string.Empty, "The entry you are trying to delete does not exist");
            return View();  
        }

        private bool UserAccountExists(int id)
        {
          return _context.UserAccount.Any(e => e.AccId == id);
        }
    }
}
