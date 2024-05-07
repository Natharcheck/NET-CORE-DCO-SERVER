using System.Collections.Generic;

namespace Server_DCO
{
    internal static class LobbyManager
    {
        public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();
        public static void AddAccount(int connectionId)
        {
            var client = new Client {ConnectionId = connectionId};

            Clients.Add(connectionId, client);
            Database.Analytics.CurrentUsers = Clients.Count;

            Database.CreateAccount(connectionId, "ilopuke@gmail.com", "admin", "admin");
        }
        
        public static void RemoveAccount(int connectionId)
        {
            var roomId = Clients[connectionId].RoomId;

            if (roomId != 0) RoomManager.Leave(connectionId, roomId);
            
            Clients.Remove(connectionId);
            Database.Analytics.CurrentUsers = Clients.Count;
        }
        
    }
}