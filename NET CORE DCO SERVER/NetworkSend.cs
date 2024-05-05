using System;
using System.Collections.Generic;
using System.Linq;
using KaymakNetwork;

namespace Server_DCO
{
    internal static class NetworkSend
    {
        enum ServerPackets
        {
            String = 1,
            Verification,
            LoadData,
            RoomList,
            
            VerificationConnection,
            VerificationCreate,
            VerificationLeave,
        
            CreatePlayer,
            AcceptGame,
            GetDeck,
            GetCards,
            MoveCard,
            InitMove,
            InitPickUp,
            InitBat,
            PickUp,
            Bat,
            Win
        }

        enum AdminServerPackets
        {
            SendAnalytics = 51,
            SendClientList,
            SendClientByUsername,
        }

        private static void Send(int connectionId, ByteBuffer buffer)
        {
            NetworkConfig.socket.SendDataTo(connectionId, buffer.Data, buffer.Head);
            buffer.Dispose();
        }
        
        public static void SendAnalytics(int connectionId)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) AdminServerPackets.SendAnalytics);

            var analytics = Database.Analytics;
            
            buffer.WriteInt32(analytics.MaxUsers);
            buffer.WriteSingle(analytics.MaxMoney);
            buffer.WriteInt32(analytics.CurrentUsers);
            buffer.WriteSingle(analytics.CurrentMoney);
            buffer.WriteInt32(analytics.Commission);
            buffer.WriteInt32(analytics.CommissionOutput);
            
            Database.SaveAnalyticsData();
            
            Send(connectionId, buffer);
        }
        
        public static void SendClientList(int connectionId, bool isOutput = false)
        {
            var clients = Database.GetClientsDataList();
            var clientsCount = clients.Count;

            ByteBuffer buffer = new ByteBuffer(clientsCount * 4);
            buffer.WriteInt32((int) AdminServerPackets.SendClientList);
            
            buffer.WriteInt32(clientsCount);
            buffer.WriteBoolean(isOutput);

            foreach (var client in clients)
            {
                var i = 1; i++;
                if (i <= 8)
                {
                    if (isOutput)
                    {
                        if (client.OutMoney > 0)
                        {
                            buffer.WriteString(client.Username);
                            buffer.WriteInt32(client.OutMoney);
                            buffer.WriteInt32(client.Money);
                            buffer.WriteInt32(client.Coins);
                        }
                    }
                    else
                    {
                        buffer.WriteString(client.Username);
                        buffer.WriteInt32(client.OutMoney);
                        buffer.WriteInt32(client.Money);
                        buffer.WriteInt32(client.Coins);
                    }
                }
            }
            
            Send(connectionId, buffer);
        }
        
        public static void SendClientByUsername(int connectionId, string username)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) AdminServerPackets.SendClientByUsername);

            if (Database.AccountExist(username))
            {
                var client = new Client();
                Database.LoadClientDataForSearch(ref client, username);
                
                buffer.WriteString(client.Username);
                buffer.WriteInt32(client.OutMoney);
                buffer.WriteInt32(client.Money);
                buffer.WriteInt32(client.Coins);
            }

            Send(connectionId, buffer);
        }
        
        public static void SendString(int connectionId, string msg)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.String);
            buffer.WriteString(msg);

            Send(connectionId, buffer);
        }
        
        #region Authorization
        public static void Verification(int connectionId, bool isDone)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.Verification);
            buffer.WriteBoolean(isDone);

            Send(connectionId, buffer);
        }
        
        public static void LoadData(int connectionId, string username)
        {
            ByteBuffer buffer = new ByteBuffer(6);
            buffer.WriteInt32((int) ServerPackets.LoadData);

            Database.LoadClientData(connectionId, username);

            var client = LobbyManager.Clients[connectionId];
            client.ConnectionId = connectionId;
            
            buffer.WriteString(client.Username);
            buffer.WriteInt32(client.ConnectionId);
            buffer.WriteInt32(client.Money); 
            buffer.WriteInt32(client.Coins); 
            buffer.WriteInt32(client.Rank);

            Send(connectionId, buffer);
        }
        
        #endregion

        #region RoomConnection

        public static void VerificationCreate(int connectionId, bool isDone)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.VerificationCreate);
            
            buffer.WriteBoolean(isDone);

            Send(connectionId, buffer);
        }

        public static void VerificationConnection(int connectionId, bool isDone)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.VerificationConnection);
            buffer.WriteBoolean(isDone);

            Send(connectionId, buffer);
        }
        
        public static void VerificationLeave(int connectionId, bool isDone)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.VerificationLeave);
            buffer.WriteBoolean(isDone);

            Send(connectionId, buffer);
            
            if (isDone)
            {
                var username = LobbyManager.Clients[connectionId].Username;
                LoadData(connectionId, username);
            }
        }

        #endregion

        #region RoomManagment

        public static void RoomListUpdate(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];
            var rooms = RoomManager.GetRoomList();
            var roomsCount = rooms.Count;

            var bufferSize = 4 * roomsCount;
            
            ByteBuffer buffer = new ByteBuffer(bufferSize);
            buffer.WriteInt32((int) ServerPackets.RoomList);

            buffer.WriteInt32(roomsCount);

            for (var i = 1; i <= roomsCount; i++)
            {
                if (i > 8) break;
                
                buffer.WriteInt32(rooms[i].RoomId);
                buffer.WriteInt32(rooms[i].MaxPlayers);
                buffer.WriteInt32(rooms[i].Players.Count);
                buffer.WriteInt32(rooms[i].QuantityCards);
            }
            
            if(client.RoomId == 0)
                NetworkConfig.socket.SendDataTo(connectionId, buffer.Data, buffer.Head);
            
            buffer.Dispose();
        }
        
        private static ByteBuffer PlayerData(int connectionId, int playersCount, int maxPlayersCount, int capitalBet, int roomId)
        {
            ByteBuffer buffer = new ByteBuffer(6);
            buffer.WriteInt32((int) ServerPackets.CreatePlayer);
            
            buffer.WriteInt32(connectionId);
            buffer.WriteInt32(playersCount);
            buffer.WriteInt32(maxPlayersCount);
            buffer.WriteInt32(capitalBet);
            buffer.WriteInt32(roomId);

            return buffer;
        }

        public static void CreatePlayer(int connectionId, Room room)
        {
            var players = room.Players;
            var playersCount = room.Players.Count;
            var maxPlayersCount = room.MaxPlayers;
            var capital = room.CapitalBet;
            var roomId = room.RoomId;
            
            foreach (var player in players)
            {
                if (player != null)
                {
                    if (player.ConnectionId != connectionId)
                    {
                        NetworkConfig.socket.SendDataTo(connectionId,
                            PlayerData(player.ConnectionId, playersCount, maxPlayersCount, (int)capital, roomId).Data,
                            PlayerData(player.ConnectionId, playersCount, maxPlayersCount, (int)capital, roomId).Head);
                    }
                    
                    NetworkConfig.socket.SendDataTo(player.ConnectionId,
                        PlayerData(connectionId, playersCount, maxPlayersCount, (int)capital, roomId).Data,
                        PlayerData(connectionId, playersCount, maxPlayersCount, (int)capital, roomId).Head);
                }
            }
        }

        public static void AcceptGame(int connectionId, Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.AcceptGame);

            var players = room.Players;

            buffer.WriteInt32(connectionId);
            
            room.ReadyPlayer(connectionId);
            
            foreach (var player in players)
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);

            buffer.Dispose();
        }
        
        public static void GettingDeck(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(256);
            buffer.WriteInt32((int) ServerPackets.GetDeck);

            var players = room.Players;
            var jCards = room.jShuffle;

            buffer.WriteInt32(room.QuantityCards);
            buffer.WriteInt32(room.TrumpSuit);
            buffer.WriteInt32(room.TrumpCard);
            
            foreach (var card in jCards)
                buffer.WriteInt32(card);

            foreach (var player in players)
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);

            buffer.Dispose();
        }

        public static void GiveCards(Room room)
        {
            if (room.Cards.Count <= 0) return;
            
            var players = room.Players;
            var missingCards = 0;

            foreach (var player in players)
            {
                if (player.MissingCards > missingCards)
                    missingCards = player.MissingCards;
            }

            var bufferSize = (6 + 6) + (6 + 6) * players.Count + ((missingCards * 4) * players.Count);

            ByteBuffer buffer = new ByteBuffer(bufferSize);
            buffer.WriteInt32((int) ServerPackets.GetCards);

            var cards = new List<Card>();

            foreach (var player in players)
            {
                if (player.MissingCards > 0)
                {
                    if (player != room.Defender)
                    {
                        var tempСards = room.GetCards(player.MissingCards);

                        if (tempСards != null)
                        {
                            cards.AddRange(tempСards);
                        }
                    }
                }
            }
            
            foreach (var player in players)
            {
                if (player.MissingCards > 0)
                {
                    if (player == room.Defender && room.Defender.Cards.Count < 6)
                    {
                        var tempСards = room.GetCards(player.MissingCards);

                        if (tempСards != null)
                        {
                            cards.AddRange(tempСards);
                        }
                    }
                }
            }

            buffer.WriteInt32(cards.Count);

            foreach (var card in cards)
                buffer.WriteInt32(card.Index);

            foreach (var player in players)
            {
                buffer.WriteInt32(player.ConnectionId);
                buffer.WriteInt32(player.MissingCards);
            }

            buffer.WriteInt32(room.Cards.Count);

            foreach (var player in players)
            {
                NetworkConfig.socket.SendDataTo(player.ConnectionId,
                    buffer.Data,
                    buffer.Head);
            }

            buffer.Dispose();
        }
        
        public static void InitMove(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.InitMove);

            var players = room.Players;

            room.InitPlayerMove();
            room.InitTime();
            
            buffer.WriteInt32(room.StrikerIndex);
            buffer.WriteInt32(room.DefenderIndex);
            
            buffer.WriteSingle(room.Striker.Time);
            buffer.WriteSingle(room.Defender.Time);

            foreach (var player in players)
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);

            buffer.Dispose();
        }
        
        public static void MoveCard(int connectionId, int card, int cardDown, bool isUpCard, Room room)
        {
            ByteBuffer buffer = new ByteBuffer(8);
            buffer.WriteInt32((int) ServerPackets.MoveCard);

            var players = room.Players;
            
            buffer.WriteInt32(connectionId);
            buffer.WriteInt32(card);
            buffer.WriteInt32(cardDown);
            buffer.WriteBoolean(isUpCard);

            InitBat(room);
            InitPickUp(room);
            
            room.InitTime();
            
            buffer.WriteInt32(room.StrikerIndex);
            buffer.WriteInt32(room.DefenderIndex);
            
            buffer.WriteSingle(room.Striker.Time);
            buffer.WriteSingle(room.Defender.Time);

            foreach (var player in players)
            {
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);   
            }
            
            buffer.Dispose();
        }
        
        private static void InitBat(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.InitBat);

            var players = room.Players;

            buffer.WriteBoolean(room.FieldCardsComparisonBat());

            foreach (var player in players)
            {
                if(player.ConnectionId != room.Defender.ConnectionId)
                    NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);   
            }
            
            buffer.Dispose();
        }
        
        public static void Bat(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.Bat);

            var players = room.Players;

            foreach (var player in players)
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);

            buffer.Dispose();
        }
        
        private static void InitPickUp(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.InitPickUp);

            buffer.WriteBoolean(room.FieldCardsComparisonPickUp());
            
            Send(room.Defender.ConnectionId, buffer);
        }
        
        public static void PickUp(Room room)
        {
            ByteBuffer buffer = new ByteBuffer(4);
            buffer.WriteInt32((int) ServerPackets.PickUp);

            var players = room.Players;

            buffer.WriteInt32(room.Defender.ConnectionId);
            
            foreach (var player in players)
                NetworkConfig.socket.SendDataTo(player.ConnectionId, buffer.Data, buffer.Head);

            buffer.Dispose();
        }
        
        public static void Win(int connectionId, float reward, Room room, bool isLose = false)
        {
            var players = room.WinPlayers;
            var bufferSize = players.Count * 4;
            
            ByteBuffer buffer = new ByteBuffer(bufferSize);
            buffer.WriteInt32((int) ServerPackets.Win);

            buffer.WriteInt32((int) reward);
            buffer.WriteBoolean(isLose);

            NetworkConfig.socket.SendDataTo(connectionId, buffer.Data, buffer.Head);

            Console.WriteLine("PacketWin send");

            buffer.Dispose();
        }
        
        #endregion
    }
}