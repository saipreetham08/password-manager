using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using PM.Models;
using System.Security.Policy;
using PM.Services;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;


namespace PM_Test
{
    public class UnitTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient httpClient;

        public UnitTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            httpClient = _factory.CreateClient();
        }

        [Theory]
        [InlineData("Face Book")] //Spaces should not be accepted
        [InlineData("Yahoo!")] //Special Characters should not be accepted
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYXZABCDEFGHIJKLMNOPQRSTUVWXYXZ")] //52 characters is too long
        public void Test_InvalidAccountNames(string AccountName)
        {
            Assert.False(UserAccount.IsValidAccountName(AccountName)); 
        }

        [Theory]
        [InlineData("A")] //1 Character Accepted
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYXZABCDEFGHIJKLMNOPQRSTUVWX")] //50 Characters Accepted
        [InlineData("W3Schools")] //Numbers Accepted
        public void Test_ValidAccountNames(string AccountName)
        {
            Assert.True(UserAccount.IsValidAccountName(AccountName));
        }

        [Theory]
        [InlineData("Sai Preetham")] //Spaces should not be accepted
        [InlineData("saipreetham!")] //The only symbols accepted are . and _
        [InlineData("saipreethamsaipreethamsaipreethamsaipreethamsaipreetham")] //55 characters is too long
        public void Test_InvalidUserNames(string UserName)
        {
            Assert.False(UserAccount.IsValidUserName(UserName));
        }

        [Theory]
        [InlineData("saipreetham")]
        [InlineData("sai.d.preetham")]
        [InlineData("sai_preetham")]
        [InlineData("sai.preetham_123")]
        public void Test_ValidUserNames(string UserName)
        {
            Assert.True(UserAccount.IsValidUserName(UserName));
        }

        [Theory]
        [InlineData("www.gmail.com")] //No http or https
        [InlineData("gmail.com")]
        [InlineData("http:/www.gmail.comcomcomcom/login")]
        [InlineData("HelloWorld123")]
        public void Test_InvalidURLs(string URL)
        {
            Assert.False(UserAccount.IsValidURL(URL));
        }

        [Theory]
        [InlineData("https://www.gmail.com")] 
        [InlineData("http://www.gmail.com/login")]
        [InlineData("https://www.umd.edu/login")]
        public void Test_ValidURLs(string URL)
        {
            Assert.True(UserAccount.IsValidURL(URL));
        }

        [Theory]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890")]
        public void Test_InvalidPasswords(string Password)
        {
            Assert.False(UserAccount.IsValidPassword(Password));
        }

        [Theory]
        [InlineData("vnsfjdk9e34HBCD890csdhjcds&*^&^&CDS")]
        [InlineData("hjcsdhjbdsj")]
        [InlineData("HBCJHDSBC")]
        [InlineData("8998878786768687")]
        [InlineData("&^*&%&%^$%%&^^*&&")]
        public void Test_ValidPasswords(string Password)
        {
            Assert.True(UserAccount.IsValidPassword(Password));
        }

        [Fact]
        public void Test_InvalidReminderDate()
        {
            DateTime reminder = DateTime.MinValue;
            DateTime lastUpdated = DateTime.Now;
            Assert.False(UserAccount.IsValidReminderDate(reminder, lastUpdated));
        }

        [Fact]
        public void Test_ValidReminderDate()
        {
            Assert.True(UserAccount.IsValidReminderDate(DateTime.Now.AddDays(5), DateTime.Now));
            Assert.True(UserAccount.IsValidReminderDate(DateTime.Now.AddMinutes(20), DateTime.Now));
            Assert.True(UserAccount.IsValidReminderDate(DateTime.Now.AddMonths(5), DateTime.Now));
            Assert.True(UserAccount.IsValidReminderDate(DateTime.Now.AddYears(5), DateTime.Now));
        }

        [Fact]
        public void TestCrypto()
        {
            Crypto cryptoObj = new Crypto();
            string securityStamp = "QFFKUTWY36DKHKCQF6GJHMLE76JUVRZM";
            byte[] hash = Encoding.ASCII.GetBytes("AQAAAAEAACcQAAAAEAk5uYwJ9HMOesmkvhtJSJkap2+F+vk8T7smTrAyIhcI1FjN7aXPOSY5PKjE3eaVuA==");
            cryptoObj.setKeyandIV(securityStamp, hash);
            string randomData = "Hello World. This is John Doe.";
            byte[] encryptedData = cryptoObj.EncryptStringtoBytes_Aes(randomData);
            string decryptedData = cryptoObj.DecryptStringFromBytes_Aes(encryptedData);
            Assert.Equal(randomData, decryptedData);
        }

        [Theory]
        [InlineData("Identity/Account/Login")]
        [InlineData("Identity/Account/Register")]
        [InlineData("Identity/Account/ForgotPassword")]
        public async Task Test_PublicPages(string URL)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(URL);
            Assert.Equal(200, (int)response.StatusCode);
        }

        [Theory]
        [InlineData("UserAccounts/Index")]
        [InlineData("UserAccounts/Details?accName=Gmail&UserName=saipreetham")]
        [InlineData("UserAccounts/Edit?accName=Gmail&UserName=saipreetham")]
        [InlineData("UserAccounts/Delete?accName=Gmail&UserName=saipreetham")]
        [InlineData("Identity/Account/Manage")]
        [InlineData("Identity/Account/Manage/ChangePassword")]
        public async Task Test_PrivatePages_Redirect(string URL)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(URL);
            var responseContent = await response.Content.ReadAsStringAsync();
            var content = responseContent.ToString();
            Assert.Contains("Please enter your credentials", content);  //All requests should be redirected to login page
        }

    }
}