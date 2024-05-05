using System.Collections.Generic;

namespace Server_DCO
{
    public class Player
    {
        public int ConnectionId;
        public int RoomId;

        public int QuantityCards;
        public int MissingCards;

        public List<Card> Cards = new List<Card>();

        public bool IsBat;
        public bool IsReady;
        public bool IsPickUp;
        public float Time;
    }
}