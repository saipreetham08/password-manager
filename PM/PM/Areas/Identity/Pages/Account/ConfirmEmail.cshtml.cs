// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PM.Data;
using PM.Models;
using System.Security.Cryptography;
using PM.Controllers;

namespace PM.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.LogError("userID or code was null");
                ModelState.AddModelError(string.Empty, "Bad request");
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogCritical("User could not be found");
                ModelState.AddModelError(string.Empty, "Check UserID");
                return NotFound();
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
            if (result.Succeeded)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
                Random random_obj = new Random();  
                string fileName = new string(Enumerable.Repeat(chars, 16).Select(s => s[random_obj.Next(s.Length)]).ToArray()); //online
                fileName = fileName + ".json";
                var userID = user.Id;
                var fileDBEntry = new FileDB() { Id = userID, FileName = fileName };
                try
                {
                    FileStream fs = System.IO.File.Create("Passwords/" + fileName);
                    fs.Close();
                }
                catch(Exception ex)
                {
                    _logger.LogCritical("File could not be created due to " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                }
                try
                {
                    _dbContext.FileDB.Add(fileDBEntry);
                    _dbContext.SaveChanges();
                }
                catch(Exception ex)
                {
                    _logger.LogCritical("DataBase could not be updated becaause " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                }
            }
            return Page();
        }
    }
}
