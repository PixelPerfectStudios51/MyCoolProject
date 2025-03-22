using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Telegram.Bot;

namespace WifiTelegramBot
{
    class Program
    {
        private static readonly string BotToken = "YOUR_BOT_TOKEN"; // Replace with your Telegram bot token
        private static readonly long ChatId = CHAT_ID; // Replace with your chat ID
        private static TelegramBotClient botClient;
        private static List<DeviceInfo> previousDevices = new List<DeviceInfo>();

        // Device info class
        class DeviceInfo
        {
            public string IP { get; set; }
            public string MAC { get; set; }
            public string Name { get; set; }
            public override bool Equals(object obj) => obj is DeviceInfo other && IP == other.IP && MAC == other.MAC;
            public override int GetHashCode() => (IP + MAC).GetHashCode();
            public override string ToString() => $"{Name ?? "Unknown"} ({IP}, {MAC})";
        }

        static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient(BotToken);
            await botClient.SendTextMessageAsync(ChatId, "Router monitoring bot started!");

            while (true)
            {
                await CheckRouterDevices();
                await Task.Delay(10000); // Check every 10 seconds
            }
        }

        static async Task CheckRouterDevices()
        {
            try
            {
                List<DeviceInfo> currentDevices = await GetDevicesFromRouter();

                if (previousDevices.Count == 0)
                {
                    previousDevices = currentDevices;
                    await botClient.SendTextMessageAsync(ChatId, $"Initial scan: {currentDevices.Count} devices connected.");
                    return;
                }

                var newDevices = currentDevices.Except(previousDevices).ToList();
                foreach (var device in newDevices)
                {
                    await botClient.SendTextMessageAsync(ChatId, $"Device connected: {device}");
                }

                var disconnectedDevices = previousDevices.Except(currentDevices).ToList();
                foreach (var device in disconnectedDevices)
                {
                    await botClient.SendTextMessageAsync(ChatId, $"Device disconnected: {device}");
                }

                previousDevices = currentDevices;
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(ChatId, $"Error: {ex.Message}");
            }
        }

        static async Task<List<DeviceInfo>> GetDevicesFromRouter()
        {
            var devices = new List<DeviceInfo>();

            // Router configuration - REPLACE THESE WITH YOUR ROUTER'S DETAILS
            string routerUrl = "http://192.168.1.1"; // Your router's IP
            string loginPath = "/"; // Login page (e.g., "/" or "/login.htm")
            string deviceListPath = "/platform.cgi?page=lan_clients.html"; // Device list page (check your router's manual)
            string username = "JioFiber-dgYK3"; // Router admin username
            string password = "thei1jeemoorooM8"; // Router admin password

            using (var client = new HttpClient())
            {
                // Set up basic authentication (if required)
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                // Step 1: Log in (some routers require this first)
                var loginResponse = await client.GetAsync(routerUrl + loginPath);
                if (!loginResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to log in to router");
                }

                // Step 2: Fetch the device list page
                var deviceResponse = await client.GetStringAsync(routerUrl + deviceListPath);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(deviceResponse);

                // Step 3: Parse the device list (example for TP-Link; adjust for your router)
                // Inspect your router's HTML in browser dev tools (F12) to find the table structure
                var deviceRows = htmlDoc.DocumentNode.SelectNodes("//table//tr"); // Adjust XPath to match your router's table
                if (deviceRows != null)
                {
                    foreach (var row in deviceRows.Skip(1)) // Skip header row if present
                    {
                        var cells = row.SelectNodes("td");
                        if (cells != null && cells.Count >= 3) // Ensure enough columns
                        {
                            string name = cells[0].InnerText.Trim(); // Device name (adjust index)
                            string ip = cells[1].InnerText.Trim();   // IP address (adjust index)
                            string mac = cells[2].InnerText.Trim();  // MAC address (adjust index)

                            devices.Add(new DeviceInfo
                            {
                                Name = string.IsNullOrEmpty(name) || name == "-" ? "Unknown" : name,
                                IP = ip,
                                MAC = mac
                            });
                        }
                    }
                }
            }

            return devices;
        }
    }
}
