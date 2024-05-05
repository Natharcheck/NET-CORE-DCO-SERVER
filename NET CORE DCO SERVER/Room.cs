using System;
using System.Linq;
using System.Collections.Generic;

namespace Server_DCO
{
    internal class Room : IDisposable
    {
        public int RoomId;
        
        public int MaxPlayers = 2;
        public int QuantityCards = 24;
        public float CapitalBet = 500;

        public int TrumpSuit = 0;
        public int TrumpCard = 0;
        public int IndexTrumpCard = 0;

        public readonly List<Player> Players = new List<Player>();
        public readonly List<Player> WinPlayers = new List<Player>();

        public List<Card> BatCards = new List<Card>();
        public readonly List<Card> Cards = new List<Card>();
        public readonly List<Card> FieldCards = new List<Card>();
        public readonly List<Card> TrashCards = new List<Card>();

        public readonly List<int> jShuffle = new List<int>();

        public Player Striker = new Player();
        public Player Defender = new Player();

        public int StrikerIndex = 0;
        public int DefenderIndex = 0;

        public void ConnectionPlayer(int connectionId, bool isCreate = false)
        {
            if (isCreate)
            {
                VerificationCreate(connectionId);
                
                InitCards();
                InitTrump();
                Shuffling();
            }
            
            AddPlayer(connectionId);
        }

        #region Players

        private void AddPlayer(int connectionId)
        {
            if (VerificationConnection(connectionId) == false)
                return;
            
            var player = new Player()
            {
                ConnectionId = connectionId,
                RoomId = RoomId
            };

            var client = LobbyManager.Clients[connectionId];
            client.RoomId = RoomId;

            Players.Add(player);
        }

        public void RemovePlayer(int connectionId)
        {
            var client = LobbyManager.Clients[connectionId];
            client.RoomId = 0;

            foreach (var player in Players)
            {
                if (player.ConnectionId == connectionId)
                {
                    Players.Remove(player);
                    break;
                }
            }
            
            VerificationLeave(connectionId);

            if (Players.Count <= 0)
                RoomManager.Delete(this);
        }

        public Player GetPlayer(int connectionId)
        {
            var players = Players;
            var tmpPlayer = new Player();

            foreach (var player in players)
            {
                if (player.ConnectionId == connectionId)
                    tmpPlayer = player;
            }

            return tmpPlayer;
        }

        #endregion

        #region CardsDeck

        private void InitCards()
        {
            for (var i = 1; i <= QuantityCards; i++)
            {
                var card = new Card();
                card.Index = i;
                
                Cards.Add(card);
            }
        }
        
        private void InitTrump()
        {
            var random = new Random();
            
            TrumpSuit = random.Next(1,5);
            TrumpCard = random.Next(1,10);
        }
        
        private void Shuffling()
        {
            var random = new Random();

            for (var i = QuantityCards - 1; i >= 1; i--)
            {
                var j = random.Next(i + 1);
                jShuffle.Add(j);

                var temp = Cards[j];
                Cards[j] = Cards[i];
                Cards[i] = temp;
            }
        }

        public void InitFirstCard()
        {
            var count = Cards.Count;

            for (var i = 0; i < count; i++)
            {
                if (Cards.ToList()[i].Index == IndexTrumpCard)
                {
                    var trumpCard = Cards[i];
                    var last = Cards[count - 1];

                    Cards[count - 1] = trumpCard;
                    Cards[i] = last;
                    
                    break;
                }
            }
        }
        
        public List<Card> GetCards(int missingQuantity)
        {
            if (Cards.Count <= 0)
                return null;
            
            var cardsHand = new List<Card>();

            if (Cards.Count < missingQuantity)
                missingQuantity = Cards.Count;

            for (var i = 0; i < missingQuantity; i++) 
            {
                cardsHand.Add(Cards[i]);
                TrashCards.Add(Cards[i]);
            }

            foreach (var card in TrashCards)
                Cards.Remove(card);

            TrashCards.Clear();

            return cardsHand;
        }

        #endregion
        
        #region Verification

        public void ReadyPlayer(int connectionId)
        {
            var isStartGame = false;

            foreach (var player in Players)
            {
                if (player.ConnectionId == connectionId)
                    player.IsReady = true;
            }

            foreach (var player in Players)
            {
                if (player.IsReady == false)
                {
                    isStartGame = false;
                    break;
                }
                else
                {
                    isStartGame = true;
                }
            }

            if (isStartGame)
            {
                NetworkSend.GettingDeck(this);
                
                foreach (var player in Players)
                    player.MissingCards = 6;
                
                NetworkSend.GiveCards(this);
                NetworkSend.InitMove(this);
            }
        }
        private bool VerificationConnection(int connectionId)
        {
            var isDone = Players.Count != MaxPlayers;
            
            NetworkSend.VerificationConnection(connectionId, isDone);

            return isDone;
        }
        
        private void VerificationCreate(int connectionId)
        {
            NetworkSend.VerificationCreate(connectionId,true);
        }
        
        private void VerificationLeave(int connectionId)
        {
            NetworkSend.VerificationLeave(connectionId, true);
        }

        #endregion

        #region GameLogic


        // ReSharper disable once RedundantAssignment
        public void InitPlayerMove()
        {
            if (!Defender.IsPickUp)
            {
                StrikerIndex += 1;
                DefenderIndex = StrikerIndex - 1;
            }

            if (StrikerIndex >= Players.Count)
            {
                StrikerIndex = 0;
                DefenderIndex = 1;
            }

            Striker = Players[StrikerIndex];
            Defender = Players[DefenderIndex];
        }

        public void InitTime()
        {
            if (Striker.Time == 0)
            {
                Striker.Time = 60;
                Defender.Time = 0;
            }
            else
            {
                Striker.Time = 0;
                Defender.Time = 60;
            }
        }
        
        public void InitBat(int connectionId)
        {
            var isBat = false;
            
            foreach (var player in Players)
            {
                if (player.ConnectionId == connectionId)
                    player.IsBat = true;
            }

            foreach (var player in Players)
            {
                if (player != Defender)
                {
                    if (player.IsBat == false)
                    {
                        isBat = false;
                        break;
                    }
                    else
                    {
                        isBat = true;
                    }
                }
            }

            if (isBat)
                Bat();
        }

        public void PickUp()
        {
            foreach (var card in FieldCards)
                Defender.Cards.Add(card);

            Defender.IsPickUp = true;
            FieldCards.Clear();
            
            NetworkSend.PickUp(this);
            NetworkSend.GiveCards(this);
            
            InitWin();
            NetworkSend.InitMove(this);
        }
        
        private void Bat()
        {
            foreach (var card in FieldCards)
                BatCards.Add(card);
            
            Defender.IsPickUp = false;
            FieldCards.Clear();
            
            NetworkSend.Bat(this);
            NetworkSend.GiveCards(this);
            
            InitWin();
            NetworkSend.InitMove(this);
        }

        private void InitWin()
        {
            if(Cards.Count > 0) 
                return;
            
            var wPlayers = WinPlayers;
            
            foreach (var player in Players)
            {
                if (player.MissingCards >= 6)
                {
                    for (var i = 0; i < wPlayers.Count; i++)
                    {
                        if (wPlayers[i] != null)
                        {
                            if (wPlayers[i] != player)
                            {
                                wPlayers.Add(player);
                            }
                        }
                    }

                    if (wPlayers.Count == 0)
                    {
                        wPlayers.Add(player);
                    }
                }
            }

            foreach (var player in Players)
            {
                if (wPlayers.Count == MaxPlayers - 1)
                {
                    foreach (var wPlayer in wPlayers)
                    {
                        if (wPlayer != player)
                        {
                            wPlayers.Add(player);
                            break;
                        }
                    }
                }
            }

            if (wPlayers.ToList().Count == MaxPlayers)
                Win();
        }

        private void Win()
        {
            var commission = CapitalBet * Database.Analytics.Commission / 100;
            var reward = CapitalBet - commission;

            var wPlayers = WinPlayers.ToList();
            var count = wPlayers.Count;

            for (var i = 0; i < count; i++)
            {
                if (i < count - 1)
                {
                    var rewardPlayer = reward / (count - 2);

                    LobbyManager.Clients[wPlayers[i].ConnectionId].ChangeMoney(rewardPlayer);
                    Database.SaveClientData(wPlayers[i].ConnectionId);

                    NetworkSend.Win(wPlayers[i].ConnectionId, rewardPlayer, this);
                }
                
                if (i == count - 1)
                {
                    LobbyManager.Clients[wPlayers[i].ConnectionId].ChangeMoney(-CapitalBet);
                    Database.SaveClientData(wPlayers[i].ConnectionId);
                    
                    NetworkSend.Win(wPlayers[i].ConnectionId, reward,this,true);
                }    
            }
        }

        public bool FieldCardsComparisonBat()
        {
            var fieldCards = FieldCards;
            
            var upCardsCount = new int();
            var downCardsCount = new int();

            foreach (var card in fieldCards)
            {
                if (card.IsUpCard)
                    upCardsCount++;
                else
                    downCardsCount++;
            }

            return upCardsCount == downCardsCount;
        }
        
        public bool FieldCardsComparisonPickUp()
        {
            var fieldCards = FieldCards;
            
            var upCardsCount = new int();
            var downCardsCount = new int();

            foreach (var card in fieldCards)
            {
                if (card.IsUpCard)
                    upCardsCount++;
                else
                    downCardsCount++;
            }

            return upCardsCount < downCardsCount;
        }
        
        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}