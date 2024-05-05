using System;

namespace Server_DCO
{
    [Serializable]
    public class Client
    {
        public int ConnectionId;
        public int RoomId;

        public string Mail;
        public string Username;
        public string Password;

        public int Money;
        public int OutMoney = 0;

        public int Coins = 100;
        public int Rank = 0;

        public void ChangeMoney(float value)
        {
            Money += (int)value;

            if (Money <= 0) Money = 0;
        }
    }
}