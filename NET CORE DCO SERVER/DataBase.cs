using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Server_DCO
{
    internal static class Database
    {
        public static Analytics Analytics;
        
        private static string PathData = "Root/Nathar/DCO/data";
        private static string PathAnalytics = "/analytics/";
        private static string PathAccount = "/accounts/";
        private static string PathMails = "/mails/";

        public static readonly string FileExtension = ".bin";

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
                
                if (!File.Exists(PathData + PathAnalytics + "Analytics"))
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
                client.RoomId = 0;

            var name = client.Username;
            var path = PathData + PathAccount + name + FileExtension;

            using(FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(client.Username);
                    writer.Write(client.Password);
                    writer.Write(client.OutMoney);
                    writer.Write(client.Money);
                    writer.Write(client.Coins);
                    writer.Write(client.Rank);
                }
            }

            Console.WriteLine("Save Client File");
        }


        private static void SaveClientMailData(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];
            
            var name = client.Mail;
            var path = PathData + PathMails + name + FileExtension;
            
            using(FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(client.Username);
                    writer.Write(client.Mail);
                }
            }
 
            Console.WriteLine("Save Client Mail");
        }

        public static void SaveAnalyticsData()
        {
            var name = "Analytics";
            var path = PathData + PathAnalytics + name + FileExtension;
            
            using(FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Analytics.Commission);
                    writer.Write(Analytics.CommissionOutput);
                    writer.Write(Analytics.CurrentMoney);
                    writer.Write(Analytics.CurrentRooms);
                    writer.Write(Analytics.CurrentUsers);
                    writer.Write(Analytics.MaxMoney);
                    writer.Write(Analytics.MaxUsers);
                }
            }

            Console.WriteLine("Save Analytics");
        }

        public static void LoadAnalyticsData()
        {
            var name = "Analytics";
            var path = PathData + PathAnalytics + name + FileExtension;

            if (File.Exists(path))
            {
                using(FileStream stream = new FileStream(path, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        Analytics.Commission = reader.ReadInt32();
                        Analytics.CommissionOutput = reader.ReadInt32();
                        Analytics.CurrentMoney = reader.ReadSingle();
                        Analytics.CurrentRooms = reader.ReadInt32();
                        Analytics.CurrentUsers = reader.ReadInt32();
                        Analytics.MaxMoney = reader.ReadSingle();
                        Analytics.MaxUsers = reader.ReadInt32();
                    }
                }

                Console.WriteLine("Load Analytics");
            }
        } 

        public static void LoadClientData(int connectionId, string username)
        { 
            var name = username;
            var path = PathData + PathAccount + name + FileExtension;
            var client = LobbyManager.Clients[connectionId];

            if (File.Exists(path))
            {
                using(FileStream stream = new FileStream(path, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        client.Username = reader.ReadString();
                        client.Password = reader.ReadString();
                        client.OutMoney = reader.ReadInt32();
                        client.Money = reader.ReadInt32();
                        client.Coins = reader.ReadInt32();
                        client.Rank = reader.ReadInt32();
                    }
                }
            }
        }
        
        public static void LoadClientDataForSearch(ref Client client, string username)
        {
            var name = username;
            var path = PathData + PathAccount + name + FileExtension;

            if (File.Exists(path))
            {
                var jsonString = File.ReadAllText(path);
                client = JsonSerializer.Deserialize<Client>(jsonString);
            }
        }
        
        public static void LoadClientDataForList(ref Client client, string path)
        {
            if (File.Exists(path))
            {
                var jsonString = File.ReadAllText(path);
                client = JsonSerializer.Deserialize<Client>(jsonString);
            }
        }

        public static bool IsCorrectPassword(string username, string password)
        {
            var name = username;
            var path = PathData + PathAccount + name + FileExtension;
            
            if (File.Exists(path))
            {
                var client = new Client();

                var jsonString = File.ReadAllText(path);
                client = JsonSerializer.Deserialize<Client>(jsonString);

                if (client.Password == password)
                { 
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
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