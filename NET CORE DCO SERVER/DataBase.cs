using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server_DCO
{
    internal static class Database
    {
        public static Analytics Analytics;
        
        private static readonly string PathData = "/DCO/data";
        private static readonly string PathAnalytics = "/analytics";
        private static readonly string PathAccount = "/accounts";
        private static readonly string PathMails = "/mails";

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
            
            Stream stream = File.Open(PathData + PathAccount + 
                                      "/" + client.Username + FileExtension, FileMode.Create);
            
            BinaryFormatter bf = new BinaryFormatter();

            client.RoomId = 0;
            
            bf.Serialize(stream, client);
            stream.Close();
        }


        private static void SaveClientMailData(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];
            
            Stream stream = File.Open(PathData + PathMails + "/" + 
                                      client.Mail + FileExtension, FileMode.Create);
            
            BinaryFormatter bf = new BinaryFormatter();
            
            bf.Serialize(stream, client);
            stream.Close();
        }

        public static void SaveAnalyticsData()
        {
            Stream stream = File.Open(PathData + PathAnalytics +
                                      "/" + "Analytics" + FileExtension, FileMode.Create);
            
            BinaryFormatter bf = new BinaryFormatter();
            
            bf.Serialize(stream, Analytics);
            stream.Close();
        }
        
        public static void LoadAnalyticsData()
        {
            Stream stream = File.Open(PathData + PathAnalytics +
                                      "/" + "Analytics"+ FileExtension, FileMode.Open);
            
            BinaryFormatter bf = new BinaryFormatter();
            
            Analytics = (Analytics) bf.Deserialize(stream);
            stream.Close();
        }

        public static void LoadClientData(int connectionId, string username)
        {
            Stream stream = File.Open(PathData + PathAccount +
                                      "/" + username + FileExtension, FileMode.Open);
            
            BinaryFormatter bf = new BinaryFormatter();

            LobbyManager.Clients[connectionId] = null;
            LobbyManager.Clients[connectionId] = (Client) bf.Deserialize(stream);
            stream.Close();
        }
        
        public static void LoadClientDataForSearch(ref Client client, string username)
        {
            Stream stream = File.Open(PathData + PathAccount +
                                      "/" + username + FileExtension, FileMode.Open);
            
            BinaryFormatter bf = new BinaryFormatter();
            
            client = (Client) bf.Deserialize(stream);
            stream.Close();
        }
        
        public static void LoadClientDataForList(ref Client client, string pathToFile)
        {
            Stream stream = File.Open(pathToFile, FileMode.Open);
            
            BinaryFormatter bf = new BinaryFormatter();
            
            client = (Client) bf.Deserialize(stream);
            stream.Close();
        }

        public static bool IsCorrectPassword(string username, string password)
        {
            Stream stream = File.Open(PathData + PathAccount +
                                      "/" + username + FileExtension, FileMode.Open);
            
            BinaryFormatter bf = new BinaryFormatter();

            var player = (Client) bf.Deserialize(stream);
            if (player.Password == password)
            {
                stream.Close();
                return true;
            }
            else
            {
                stream.Close();
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