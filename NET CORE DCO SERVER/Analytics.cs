using System;

namespace Server_DCO
{
    [Serializable]
    public class Analytics
    {
        public int Commission = 5;
        public int CommissionOutput = 5;
        
        public int   MaxUsers       = 0;
        public int   CurrentUsers   = 0;
        public float MaxMoney     = 0;
        public float CurrentMoney = 0;

        public int CurrentRooms;
    }
}