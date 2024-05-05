using System;
using System.Collections.Generic;

namespace Server_DCO
{
    internal static class RoomManager
    {
        private static Dictionary<int ,Room> Rooms = new Dictionary<int, Room>();

        public static Dictionary<int ,Room> GetRoomList() { return Rooms; }

        public static Room GetRoom(int roomId)
        {
            return Rooms.ContainsKey(roomId) ? Rooms[roomId] : null;
        }

        public static int GetRoomCount() { return Rooms.Count; }

        public static void Create(int connectionId, int maxPlayers, int quantityCards, int quantityMoney)
        {
            var roomId = Rooms.Count + 1;

            var room = new Room()
            {
                RoomId = roomId,
                MaxPlayers = maxPlayers,
                QuantityCards = quantityCards,
                CapitalBet = quantityMoney
            };
            
            room.ConnectionPlayer(connectionId, true);
            Rooms.Add(roomId, room);

            Database.Analytics.CurrentRooms = Rooms.Count;
        }

        public static void Delete(Room room)
        {
            Rooms.Remove(room.RoomId);
            room.Dispose();
        }
        
        public static void Connection(int connectionId, int roomId)
        {
            var room = GetRoom(roomId);
            room.ConnectionPlayer(connectionId);
        }
        
        public static void Leave(int connectionId, int roomId)
        {
            var room = GetRoom(roomId);
            room.RemovePlayer(connectionId);
        }
    }
}