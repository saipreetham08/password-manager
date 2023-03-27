using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;


namespace PM.Models
{

    public enum ErrorCodes
    {
        OBJ_NULL,
        OBJ_TOO_LONG,
        INCORRECT_FORMAT,
        SUCCESS
    }

    public class UserAccount
    {
        

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int AccId { get; set; }
        [Required]
        [Display(Name ="Account Name: ")]
        public string AccountName { get; set; }
        [Required]
        [Display(Name = "User Name: ")]
        public string UserName { get; set; }
        [Required]
        [Display(Name = "Password: ")]
        public string Password { get; set; }
        [Display(Name = "Website URL: ")]
        public string URL { get; set; }
        [Display(Name = "Last Updated Date: ")]
        public DateTime LastUpdated { get; set; }
        [Display(Name = "Reminder Scheduled on: ")]
        public DateTime ReminderScheduled { get; set; }

        public static bool IsValidAccountName(string AccountName)
        {
            //null check already present before reaching here
            Regex rx = new Regex("^([a-zA-Z0-9]){1,51}$");  
            if(!rx.IsMatch(AccountName))
            {
                return false;
            }
            return true;
        }

        public static bool IsValidUserName(string AccountName)
        {
            //null check already present before reaching here
            Regex rx = new Regex("^([a-zA-Z0-9._]){1,51}$");
            if (!rx.IsMatch(AccountName))
            {
                return false;
            }
            return true;
        }

        public static bool IsValidPassword(string Password)
        {
            if(Password.Length > 50)
            {
                return false;
            }
            return true;
        }

        public static bool IsValidURL(string URL)
        {
            if(URL.Length > 50)
            {
                return false;
            }
            Regex rx = new Regex("^https?:\\/\\/(?:www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,30}\\.[a-zA-Z0-9()]{1,6}\\b(?:[-a-zA-Z0-9()@:%_\\+.~#?&\\/=]*)$");
            if (!rx.IsMatch(URL))
            {
                return false;
            }
            return true;
        }

        public static bool IsValidReminderDate(DateTime reminder, DateTime lastUpdated)
        {
            if (reminder.CompareTo(lastUpdated) <= 0)
            {
                return false;
            }
            return true;
        }
    }

    public class UserAccounts
    {
        public List<UserAccount> userAccounts { get; set; } 
    }
}
