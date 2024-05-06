using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server_DCO
{
    internal static class Database
    {
        public static Analytics Analytics;
        
        private static string PathData = "/DCO/data";
        private static string PathAnalytics = "/analytics";
        private static string PathAccount = "/accounts";
        private static string PathMails = "/mails";

        public static readonly string FileExtension = ".json";

        public static void CreateAccount(int connectionId, string mail, string username, string password)
        {
            var client = LobbyManager.Clients[connectionId];
            
            client.Mail = mail;
            client.Username = username;
            client.Password = password;
            client.Money = 2500;
            client.Coins = 10;
            client.Rank = 0;

            SaveClientData(connectionId);
            SaveClientMailData(connectionId);
            
            Analytics.MaxUsers += 1;
            
            SaveAnalyticsData();
        }

        public static void CorrectPath()
        {
            Console.WriteLine("Path:..." + PathData + PathAccount);
            Console.WriteLine("Path:..." + PathData + PathMails);
            Console.WriteLine("Path:..." + PathData + PathAnalytics);

            if (!Directory.Exists(PathData + PathAccount))
                Directory.CreateDirectory(PathData + PathAccount);
            
            if (!Directory.Exists(PathData + PathMails))
                Directory.CreateDirectory(PathData + PathMails);

            if (!Directory.Exists(PathData + PathAnalytics))
            {
                Directory.CreateDirectory(PathData + PathAnalytics);
                
                if (!File.Exists(PathData + PathAnalytics + "/" + "Analytics"))
                    SaveAnalyticsData();
                else
                {
                    LoadAnalyticsData();
                }
            }
            
        }

        public static void SaveClientData(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];

            string path = Path.Combine(PathData, PathAccount, $"{client.Username}");
            string jsonString = JsonSerializer.Serialize(client);

            client.RoomId = 0;

            File.WriteAllText(path, jsonString);
        }


        private static void SaveClientMailData(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];
            
            string path = Path.Combine(PathData, PathAccount, $"{client.Mail}");
            string jsonString = JsonSerializer.Serialize(client.Mail);
            
            File.WriteAllText(path, jsonString);
        }

        public static void SaveAnalyticsData()
        {
            string path = Path.Combine(PathData, PathAccount, $"{Analytics}");
            string jsonString = JsonSerializer.Serialize(Analytics);
            
            File.WriteAllText(path, jsonString);
        }

        public static void LoadAnalyticsData()
        {
            string path = Path.Combine(PathData, PathAnalytics, $"Analytics{FileExtension}");

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
                Analytics = JsonSerializer.Deserialize<Analytics>(jsonString);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            }
        } 

        public static void LoadClientData(int connectionId, string username)
        { 
            string path = Path.Combine(PathData, PathAccount, $"{username}{FileExtension}");

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
                LobbyManager.Clients[connectionId] = JsonSerializer.Deserialize<Client>(jsonString);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            }
        }
        
        public static void LoadClientDataForSearch(ref Client client, string username)
        {
            string path = Path.Combine(PathData, PathAccount, $"{username}{FileExtension}");

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
                client = JsonSerializer.Deserialize<Client>(jsonString);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            }
        }
        
        public static void LoadClientDataForList(ref Client client, string pathToFile)
        {
            string path = Path.Combine(pathToFile, $"{client.Username}{FileExtension}");

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
                client = JsonSerializer.Deserialize<Client>(jsonString);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            }
        }

        public static bool IsCorrectPassword(string username, string password)
        {
            string path = Path.Combine(PathData, PathAccount, $"{username}{FileExtension}");
            var client = new Client();

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
                client = JsonSerializer.Deserialize<Client>(jsonString);
            }

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            if (client.Password == password)
            { 
                return true;
            }
            else
            {
                return false;
            }
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
        }
        
        public static List<Client> GetClientsDataList()
        {
            var fileEntries = Directory.GetFiles(PathData + PathAccount);
            var clients = new List<Client>();
            
            foreach (var pathFile in fileEntries)
            {
                var client = new Client();
                
                LoadClientDataForList(ref client, pathFile);
                    
                clients.Add(client);
            }

            return clients;
        }

        public static bool AccountExist(string username)
        {
            return File.Exists(PathData + PathAccount + "/" + username + FileExtension);
        }
        
        public static bool MailExist(string mail)
        {
            return File.Exists(PathData + PathMails + "/" + mail + FileExtension);
        }
    }
}