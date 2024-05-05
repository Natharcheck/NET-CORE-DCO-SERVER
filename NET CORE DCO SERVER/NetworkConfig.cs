using System;
using KaymakNetwork.Network.Server;

namespace Server_DCO
{
    internal static class NetworkConfig
    {
        private static Server _socket;

        internal static Server socket
        {
            get { return _socket; }
            set
            {
                if (_socket != null)
                {
                    _socket.ConnectionReceived -= SocketConnectionReceived;
                    _socket.ConnectionLost -= SocketConnectionLost;
                }

                _socket = value;
                if (_socket != null)
                {
                    _socket.ConnectionReceived += SocketConnectionReceived;
                    _socket.ConnectionLost += SocketConnectionLost;
                }
            }
        }

        internal static void InitNetwork()
        {
            if (_socket != null)
                return;

            socket = new Server(6)
            {
                BufferLimit = 2048000,
                PacketAcceptLimit = 150,
                PacketDisconnectCount = 150
            };

            NetworkReceive.PacketRouter();
        }

        internal static void SocketConnectionReceived(int connectionId)
        {
            Console.WriteLine("Client received connectionID [" + connectionId + "]" +
                              " IP: " + socket.ClientIp(connectionId));

            LobbyManager.AddAccount(connectionId);
        }

        internal static void SocketConnectionLost(int connectionId)
        {
            Console.WriteLine("Client lost connectionID [" + connectionId + "]");

            Database.SaveClientData(connectionId);
            LobbyManager.RemoveAccount(connectionId);
        }
    }
}