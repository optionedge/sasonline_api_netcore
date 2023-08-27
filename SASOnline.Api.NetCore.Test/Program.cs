using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OtpNet;
using RestSharp;
using RestSharp.Authenticators;
using System.Text;

namespace SASOnline.Api.NetCore.Test
{
    internal class Program
    {
        private static string AppID = ""; // Get from your broker
        private static string AppSecret = ""; // Get from your broker
        private static string RedirectUrl = "http://127.0.0.1/";  // You can create your own and ask your broker to update
        private static string BaseUrl = "https://alphaapi.sasonline.in"; // Get from your broker;it will be in the form of https://api.example.com
        private static string Scope = "orders holdings";
        private static readonly object _lock = new object();
        private static string url = null;
        private static string UserName = "";
        private static string Password = "";
        private static string accessToken = "";

        private static ChromeDriver _driver = null;

        static void Main(string[] args)
        {

            AppID = Environment.GetEnvironmentVariable("AppID");
            AppSecret = Environment.GetEnvironmentVariable("AppSecret");
            RedirectUrl = Environment.GetEnvironmentVariable("RedirectUrl");
            BaseUrl = Environment.GetEnvironmentVariable("BaseUrl");
            Scope = Environment.GetEnvironmentVariable("Scope");
            UserName = Environment.GetEnvironmentVariable("UserName");
            Password = Environment.GetEnvironmentVariable("Password");


            BaseUrl = BaseUrl.Trim('/', '\\');
            url = BaseUrl + "/oauth2/auth?scope=" + Scope + "&state=%7B%22param%22:%22value%22%7D&redirect_uri=" + RedirectUrl + "&response_type=code&client_id=" + AppID;


            ChromeOptions options = new ChromeOptions();

            // options.AddArgument("headless");
            options.AddArgument("window-size=1200x600");
            options.AddArguments("start-maximized");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(10);

            _driver.Navigate().GoToUrl(url);

            //_logger.LogInformation("Going to sleep for 300ms till we navigate to login url");
            Console.WriteLine("Going to sleep for 300ms till we navigate to login url");

            Thread.Sleep(300);

            Console.WriteLine("Finding elements and sending values for username & password");
            _driver.FindElement(OpenQA.Selenium.By.Name( "login_id")).SendKeys(UserName);
            _driver.FindElement(OpenQA.Selenium.By.Name("password")).SendKeys(Password);

            _driver.FindElement(OpenQA.Selenium.By.XPath("//*[@id=\"login_form\"]/form/div[2]/div/button")).Click();

            Console.WriteLine("Login button was clicked");

            Thread.Sleep(100);

            Console.WriteLine("Entering TOTP");

            // Geerate TOTP
            var secret = "2KF7GTHYD45HA5GE";
            var totpGenerayor = new Totp(Base32Encoding.ToBytes(secret));
            var totp = totpGenerayor.ComputeTotp();
            _driver.FindElement(OpenQA.Selenium.By.Name("answers[]")).SendKeys(totp);
            _driver.FindElement(OpenQA.Selenium.By.XPath("//*[@id=\"login_form\"]/form/div[2]/div/button")).Click();
            Thread.Sleep(300);

            /*
            _driver.FindElementByName("answer2").Click();
            _driver.FindElementByName("answer2").SendKeys(ans2);
            Thread.Sleep(300);
            */

            //_driver.FindElementByXPath("//*[@id='login_form']/form/div[2]/div/button").Click();

            //waitForElement = new WebDriverWait(_driver, TimeSpan.FromMinutes(1));
            //waitForElement.Until(ExpectedConditions.ElementIsVisible(By.ClassName("loader")));

            // Open the orders window
            //_driver.FindElementByXPath("//*[@id='step2']/a[2]").Click();

            //_logger.LogInformation("Url is: " + _driver.Url);

            var uri = new Uri(_driver.Url);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            _driver.Close();
            _driver.Dispose();

            // get the code
            var code = queryParams["code"];

            // _logger.LogInformation("Successfully logged into AliceBlue API. AuthCode is: " + code);

            var client = new RestClient(BaseUrl + "/oauth2/token");
           
            var request = new RestRequest(Method.POST);

            var authbasic = Base64Encode(AppID + ":" + AppSecret);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Authorization", "Basic " + authbasic);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", RedirectUrl);
            request.AddParameter("client_id", AppID);
            IRestResponse response = client.Execute(request);
            var response_access = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.Content);
            if (response_access.Keys.Count > 3)
                accessToken = response_access["access_token"];

            // SaveAccessToken(_accessToken);

            // IsLoggedIn = true;

            //_bot.Send("Successfully logged into AliceBlue. Auth code is received.");

            var api = new PrimusApi(new Uri(BaseUrl));
            api.SetAuthenticationToken(accessToken);

            var _octopusInstance = new Octopus(accessToken, UserName, new Uri(BaseUrl).Host);
            _octopusInstance.MarketDataSource.PriceUpdateEvent += MarketDataSource_PriceUpdateEvent; ;
            _octopusInstance.MarketDataSource.SubscribeOrderTradeUpdates(UserName, "web");
            _octopusInstance.MarketDataSource.OrderUpdateEvent += MarketDataSource_OrderUpdateEvent;
            _octopusInstance.MarketDataSource.TradeUpdateEvent += MarketDataSource_TradeUpdateEvent;
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void MarketDataSource_TradeUpdateEvent(TradeUpdate tradeUpdate)
        {
            Console.WriteLine("Client Id " + tradeUpdate.ClientId + " Product Type " + tradeUpdate.Product + " Order Type " + tradeUpdate.OrderType + " Trade Price " + tradeUpdate.TradePrice + " Traded Qty " + tradeUpdate.TradeQuantity + " Filled Qty " + tradeUpdate.FilledQty);
        }

        private static void MarketDataSource_OrderUpdateEvent(OrderUpdate orderDetail)
        {
            if (orderDetail.OrderStatus.ToLower() == "accepted" || orderDetail.OrderStatus.ToLower() == "rejected")
            {
                Console.WriteLine(orderDetail.OrderStatus + " for Client Id " + orderDetail.ClientId + " Product Type " + orderDetail.Product + " Order Type " + orderDetail.OrderType + " Price " + orderDetail.Price + " Avg Price " + orderDetail.AverageTradePrice + " Trigger Price " + orderDetail.TriggerPrice + " Qty " + orderDetail.Quantity + " Disc Qty " + orderDetail.DisclosedQuantity);
            }
        }

        private static void MarketDataSource_PriceUpdateEvent(FullMarketTick feed)
        {
            if (string.IsNullOrEmpty(feed.ToString())) return;

            var data = new MarketData
            {
                Exchange = feed.Exchange,
                InstrumentCode = feed.InstrumentToken,
                BidPrice = feed.BestBidPrice,
                BidQty = feed.BestBidQty,
                AskPrice = feed.BestAskPrice,
                AskQty = feed.BestAskQty,
                LastTradePrice = feed.LastTradedPrice,
                LastTradeQuantity = feed.LastTradeQty,
                ExchangeTimestamp = feed.ExchangeTimeStamp,
                LastTradeTime = feed.LastTradeTime,
                OpenPrice = feed.OpenPrice,
                HighPrice = feed.HighPrice,
                LowPrice = feed.LowPrice,
                ClosePrice = feed.ClosePrice,
                TotalBuyQty = feed.TotalBuyQty,
                TotalSellQty = feed.TotalSellQty,
                YearlyHigh = feed.YearlyHigh,
                YearlyLow = feed.YearlyLow,
                AvgTradePrice = feed.AverageTradePrice,
                TradeVolume = feed.Volume
            };

            string exg = getExchange(data.Exchange.ToString());
            Console.WriteLine("Exchange " + exg + " Token " + data.InstrumentCode + " LTP " + data.LastTradePrice + " LTQ " + data.LastTradeQuantity + " Volume " + data.TradeVolume);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string getExchange(string exchange)
        {
            switch (exchange)
            {
                case "1":
                    return "NSE";
                case "2":
                    return "NFO";
                case "3":
                    return "CDS";
                case "4":
                    return "MCX";
                case "6":
                    return "BSE";
                case "7":
                    return "BFO";
            }
            return "";
        }
    }
}