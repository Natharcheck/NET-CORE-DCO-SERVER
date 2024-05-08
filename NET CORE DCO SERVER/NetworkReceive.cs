using System;
using KaymakNetwork;

namespace Server_DCO
{
    internal static class NetworkReceive
    {
        enum ClientPackets
        {
            String = 1,
            Authorization,
            Registration,
            VerificationMail,
            SaveData,
            RoomList,

            CreateRoom,
            ConnectionRoom,
            LeaveRoom,

            CreatePlayer,
            IndexTrumpCard,
            AcceptGame,
            MoveCard,
            MissingCards,
            AcceptBat,
            AcceptPickUp,
            OutputMoney,
        }
        
        enum AdminClientPackets
        {
            GetAnalytics = 51,
            GetClientList,
            GetClientByUsername,
            GetCommissionInRoom,
            GetCommissionOutput,
        }

        internal static void PacketRouter()
        {
            NetworkConfig.socket.PacketId[(int) ClientPackets.String] = PacketString;
            NetworkConfig.socket.PacketId[(int) ClientPackets.VerificationMail] = PacketVerificationMail;
            NetworkConfig.socket.PacketId[(int) ClientPackets.Authorization] = PacketAuthorization;
            NetworkConfig.socket.PacketId[(int) ClientPackets.Registration] = PacketRegistration;
            NetworkConfig.socket.PacketId[(int) ClientPackets.SaveData] = PacketSaveData;
            NetworkConfig.socket.PacketId[(int) ClientPackets.RoomList] = PacketRoomList;

            NetworkConfig.socket.PacketId[(int) ClientPackets.CreateRoom] = PacketCreateRoom;
            NetworkConfig.socket.PacketId[(int) ClientPackets.ConnectionRoom] = PacketConnectionRoom;
            NetworkConfig.socket.PacketId[(int) ClientPackets.LeaveRoom] = PacketLeaveRoom;

            NetworkConfig.socket.PacketId[(int) ClientPackets.CreatePlayer] = PacketCreatePlayer;
            NetworkConfig.socket.PacketId[(int) ClientPackets.IndexTrumpCard] = PacketGlobalIndexTrumpCard;
            NetworkConfig.socket.PacketId[(int) ClientPackets.MissingCards] = PacketMissingQuantity;
            NetworkConfig.socket.PacketId[(int) ClientPackets.AcceptGame] = PacketAcceptGame;
            NetworkConfig.socket.PacketId[(int) ClientPackets.MoveCard] = PacketMoveCard;
            NetworkConfig.socket.PacketId[(int) ClientPackets.AcceptBat] = PacketAcceptBat;
            NetworkConfig.socket.PacketId[(int) ClientPackets.AcceptPickUp] = PacketAcceptPickUp;
            NetworkConfig.socket.PacketId[(int) ClientPackets.OutputMoney] = PacketOutputMoney;
            
            NetworkConfig.socket.PacketId[(int) AdminClientPackets.GetAnalytics] = PacketGetAnalytics;
            NetworkConfig.socket.PacketId[(int) AdminClientPackets.GetClientList] = PacketGetClientList;
            NetworkConfig.socket.PacketId[(int) AdminClientPackets.GetClientByUsername] = PacketGetClientByUsername;
            NetworkConfig.socket.PacketId[(int) AdminClientPackets.GetCommissionInRoom] = PacketGetCommissionInRoom;
            NetworkConfig.socket.PacketId[(int) AdminClientPackets.GetCommissionOutput] = PacketGetCommissionOutput;
        }

        private static void PacketOutputMoney(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            
            var client = LobbyManager.Clients[connectionId];
            var money = buffer.ReadInt32();

            client.ChangeMoney(-money);
            client.OutMoney += money;
            
            Console.WriteLine("Output Money:" + client.OutMoney);
            
            Database.SaveClientData(connectionId);
            NetworkSend.LoadData(connectionId, client.Username);
            
            buffer.Dispose();
        }

        private static void PacketGetCommissionInRoom(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var value = buffer.ReadInt32();

            Database.Analytics.Commission = value;
            Database.SaveAnalyticsData();
            
            buffer.Dispose();
        }
        
        private static void PacketGetCommissionOutput(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var value = buffer.ReadInt32();

            Database.Analytics.CommissionOutput = value;
            Database.SaveAnalyticsData();
            
            buffer.Dispose();
        }

        
        private static void PacketGetAnalytics(int connectionId, ref byte[] data)
        {
            NetworkSend.SendAnalytics(connectionId);
        }

        private static void PacketGetClientList(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            var isOutput = buffer.ReadBoolean();

            NetworkSend.SendClientList(connectionId, isOutput);

            buffer.Dispose();
        }

        private static void PacketGetClientByUsername(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            var username = buffer.ReadString();
            
            NetworkSend.SendClientByUsername(connectionId, username);
            buffer.Dispose();
        }

        private static void PacketString(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            var msg = buffer.ReadString();

            Console.WriteLine("Str. by client: " + msg);
            buffer.Dispose();
        }

        #region Authorization

        private static void PacketAuthorization(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var username = buffer.ReadString();
            var password = buffer.ReadString();

            if (Database.AccountExist(username))
            {
                Console.WriteLine("Account Exist");
                if (Database.IsCorrectPassword(connectionId, username, password))
                {
                    NetworkSend.LoadData(connectionId, username);
                    Console.WriteLine("Load Data");
                }
                else
                {
                    NetworkSend.SendString(connectionId, "This password not correct");
                }
            }
            else
            {
                NetworkSend.SendString(connectionId, "This account not exist");
            }

            buffer.Dispose();
        }

        private static void PacketRegistration(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var mail = buffer.ReadString();
            var username = buffer.ReadString();
            var password = buffer.ReadString();

            Console.WriteLine("Packet Registration");

            if (Database.MailExist(mail))
            {
                NetworkSend.SendString(connectionId, "This mail exist");
                return;
            }
            
            if (Database.AccountExist(username))
            {
                NetworkSend.SendString(connectionId, "This username exist");
                return;
            }

            Database.CreateAccount(connectionId, mail, username, password);

            buffer.Dispose();
        }
        
        private static void PacketVerificationMail(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var mail = buffer.ReadString();
            var mailExist = Database.MailExist(mail);

            NetworkSend.Verification(connectionId, mailExist);
            
            buffer.Dispose();
        }

        private static void PacketSaveData(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            
            Database.SaveClientData(connectionId);

            buffer.Dispose();
        }

        #endregion

        #region RoomConnection

        private static void PacketCreateRoom(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var maxPlayers = buffer.ReadInt32();
            var quantityCards = buffer.ReadInt32();
            var quantityMoney = buffer.ReadInt32();

            RoomManager.Create(connectionId, maxPlayers, quantityCards, quantityMoney);

            buffer.Dispose();
        }
        
        private static void PacketConnectionRoom(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = buffer.ReadInt32();
    
            RoomManager.Connection(connectionId, roomId);

            buffer.Dispose();
        }

        private static void PacketLeaveRoom(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = buffer.ReadInt32();
            
            Console.WriteLine("RoomId PacketLeave: " + roomId);
            RoomManager.Leave(connectionId, roomId);

            buffer.Dispose();
        }

        #endregion

        #region RoomManagment

        private static void PacketRoomList(int connectionId, ref byte[] data)
        {
            NetworkSend.RoomListUpdate(connectionId);  
        }
        
        private static void PacketCreatePlayer(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);
            
            NetworkSend.CreatePlayer(connectionId, room);

            buffer.Dispose();
        }
        
        private static void PacketGlobalIndexTrumpCard(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);
            
            room.IndexTrumpCard = buffer.ReadInt32();
            room.InitFirstCard();
            
            buffer.Dispose();
        }
        private static void PacketAcceptGame(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);
            
            NetworkSend.AcceptGame(connectionId, room);
            buffer.Dispose();
        }
        
        private static void PacketMoveCard(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);
            var fieldCards = room.FieldCards;
            
            var card = buffer.ReadInt32();
            var cardDown = buffer.ReadInt32();
            var isUpCard = buffer.ReadBoolean();

            var addCard = new Card {Index = card, IsUpCard = isUpCard};

            fieldCards.Add(addCard);
            
            NetworkSend.MoveCard(connectionId, card, cardDown, isUpCard, room);
            buffer.Dispose();
        }
        
        private static void PacketAcceptBat(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);

            room.InitBat(connectionId);
            
            buffer.Dispose();
        }
        
        private static void PacketAcceptPickUp(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);

            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var room = RoomManager.GetRoom(roomId);

            room.PickUp();
            
            buffer.Dispose();
        }
        
        private static void PacketMissingQuantity(int connectionId, ref byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            
            var roomId = LobbyManager.Clients[connectionId].RoomId;
            var player = RoomManager.GetRoom(roomId).GetPlayer(connectionId);
            
            var missingQuantity = buffer.ReadInt32();

            player.MissingCards = missingQuantity;
            
            buffer.Dispose();
        }
        
        #endregion
    }
}