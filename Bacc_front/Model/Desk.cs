using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WsUtils;

namespace Bacc_front
{
    [PropertyChanged.ImplementPropertyChanged]
    public class Desk
    {
        public int RoundIndex { get; set; }
        public int CountDown { get; set; }
        public string StateText { get; internal set; }

        public static Desk Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Desk();
                }
                return instance;
            }
        }

        private Desk()
        {
            RoundIndex = 1;
            CountDown = 0;
            players = new ObservableCollection<Player>();
            desk_amount = new Dictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
            
            Waybill = new ObservableCollection<WhoWin>();
            InitPlayers();
        }
        private void InitPlayers()
        {
            for (int i = 0; i < player_num + 1; i++)
            {
                if (i == 12)
                {
                    continue;
                }
                var p = new Player(i + 1, CalcPlayerEarning);
                p.Balance = 8000;
                players.Add(p);
            }
        }
        public bool CanHeCancleBet(Player p)
        {
            var b_amount = desk_amount[BetSide.banker] - p.BetScore[(int)BetSide.banker];
            var p_amount = desk_amount[BetSide.player] - p.BetScore[BetSide.player];

            var desk_limit_red = Setting.Instance.GetIntSetting("desk_limit_red");

            //如果撤注后庄闲差仍小于限红则允许撤注
            return Math.Abs(b_amount - p_amount) <= desk_limit_red;
        }
        public bool CanHeBet(Player p, BetSide side)
        {
            var bet_amount = p.Balance >= (int)p.denomination ? (int)p.denomination : p.Balance;

            //待加入其他限制规则
            var can_he = LimitRed(bet_amount, side) && true;

            return can_he;
        }
        /// <summary>
        /// 检测限红
        /// </summary>
        /// <param name="bet_amount">押注大小</param>
        /// <param name="side">押注哪边</param>
        /// <returns></returns>
        private bool LimitRed(int bet_amount, BetSide side)
        {
            var b_amount = desk_amount[BetSide.banker];
            var t_amount = desk_amount[BetSide.tie];
            var p_amount = desk_amount[BetSide.player];

            var desk_limit = Setting.Instance.GetIntSetting("desk_limit_red");

            if (side == BetSide.banker)
            {
                var desk_red = Math.Abs(bet_amount + b_amount - p_amount);
                if (desk_red > desk_limit)
                {
                    return false;
                }
            }

            if (side == BetSide.player)
            {
                var desk_red = Math.Abs(bet_amount + p_amount - b_amount);
                if (desk_red > desk_limit)
                {
                    return false;
                }
            }

            if (side == BetSide.tie)
            {
                var tie_limit = Setting.Instance.GetIntSetting("tie_limit_red");
                var desk_tie_red = Math.Abs(bet_amount + t_amount);
                if (desk_tie_red > tie_limit)
                {
                    return false;
                }
            }

            return true;
        }
        private int CalcHandValue(List<Card> hand_cards)
        {
            var sum = 0;
            foreach (var card in hand_cards)
            {
                var weight = GetWeightValue(card);
                sum += weight;
            }
            return sum;
        }
        private int GetWeightValue(Card card)
        {
            var weight = (int)card.GetCardWeight;
            if (weight > 9)
            {
                weight = 0;
            }
            return weight;
        }

        public void UpdateDeskAmount(BetSide side,int score)
        {
            desk_amount[side] += score;
        }
        public void ClearDeskAmount()
        {
            desk_amount = new Dictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
        }
        public ObservableCollection<Player> Players
        {
            get { return players; }
            set { players = value; }
        }

        private ObservableCollection<WhoWin> waybill;
        public ObservableCollection<WhoWin> Waybill { get => waybill; set => waybill = value; }

        public void AddScore(int p_index, int score)
        {
            players[p_index].AddScore(score);
        }
        public void SubScore(int p_idx, int score)
        {
            players[p_idx].SubScore(score);
        }

        public void CalcAllPlayersEarning()
        {
            var winner = GetWinner(_curHandCards);

            var round_idx = RoundIndex;
            Waybill[round_idx - 1].Winner = (int)winner;

            foreach (var player in players)
            {
                //lock (objlock)
                //{
                    player.Balance += CalcPlayerEarning(winner, player.BetScore);
                    player.ClearBet();
                //}
            }
        }
        public int CalcPlayerEarning(BetSide winner, ObservableDictionary<BetSide, int> p_bets)
        {
            var cost = p_bets[winner];

            float native_odds;
            if (winner == BetSide.banker)
            {
                native_odds = cost >= 10 ? odds_map[(int)winner] : 1;
            }
            else
            {
                native_odds = odds_map[(int)winner];
            }

            var earning = cost + cost * native_odds;

            return (int)Math.Floor(earning);
        }

        public List<Card>[] DealSingleCard()
        {
            List<Card>[] hand_card = new List<Card>[2];

            var banker_card = new List<Card>();
            banker_card.Add(Deck.Instance.Deal());
            var player_card = new List<Card>();
            player_card.Add(Deck.Instance.Deal());

            hand_card[0] = banker_card;
            hand_card[1] = player_card;

            return hand_card;
        }
        public List<Card>[] DealTwoCard()
        {
            List<Card>[] hand_card = new List<Card>[2];

            var player_card = new List<Card>();
            player_card.Add(Deck.Instance.Deal());
            var banker_card = new List<Card>();
            banker_card.Add(Deck.Instance.Deal());

            player_card.Add(Deck.Instance.Deal());
            banker_card.Add(Deck.Instance.Deal());

            hand_card[0] = banker_card;
            hand_card[1] = player_card;

            CheckThirdCard(hand_card);
            return hand_card;
        }
        public void CheckThirdCard(List<Card>[] hand_cards)
        {
            bool playerDrawStatus = false, bankerDrawStatus = false;
            int playerThirdCard = 0;
            int[] handValues = { CalcHandValue(hand_cards[0]), CalcHandValue(hand_cards[1]) };

            if (!(handValues[0] > 7) || !(handValues[1] > 7))
            {

                //Determine Player Hand first
                if (handValues[1] < 6)
                {
                    var d_card = Deck.Instance.Deal();
                    hand_cards[1].Add(d_card);
                    playerThirdCard = (int)d_card.GetCardWeight;
                    playerDrawStatus = true;
                }
                else
                    playerDrawStatus = false;

                //Determine Banker Hand
                if (playerDrawStatus == false)
                {
                    if (handValues[0] < 6)
                        bankerDrawStatus = true;
                }
                else
                {
                    if (handValues[0] < 3)
                        bankerDrawStatus = true;
                    else
                    {
                        switch (handValues[0])
                        {
                            case 3:
                                if (playerThirdCard != 8)
                                    bankerDrawStatus = true;
                                break;
                            case 4:
                                if (playerThirdCard > 1 && playerThirdCard < 8)
                                    bankerDrawStatus = true;
                                break;
                            case 5:
                                if (playerThirdCard > 3 && playerThirdCard < 8)
                                    bankerDrawStatus = true;
                                break;
                            case 6:
                                if (playerThirdCard > 5 && playerThirdCard < 8)
                                    bankerDrawStatus = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            //deal banker third card
            if (bankerDrawStatus == true)
            {
                hand_cards[0].Add(Deck.Instance.Deal());
            }
        }
        public BetSide GetWinner(List<Card>[] hand_cards)
        {
            var b_amount = CalcHandValue(hand_cards[0]);
            var p_amount = CalcHandValue(hand_cards[1]);

            if (b_amount == p_amount)
            {
                return BetSide.tie;
            }
            else
            {
                return b_amount > p_amount ? BetSide.banker : BetSide.player;
            }
        }

        private const int player_num = 14;
        private const float BANKER_ODDS = 0.95f;
        private const float PLAYER_ODDS = 1;
        private const float TIE_ODDS = 8;
        private float[] odds_map = new float[3] { BANKER_ODDS, TIE_ODDS, PLAYER_ODDS };

        private ObservableCollection<Player> players;
        private Dictionary<BetSide, int> desk_amount;
        public List<Card>[] _curHandCards;

        private static object objlock = new object();
        private static Desk instance;
    }

    [PropertyChanged.ImplementPropertyChanged]
    public class WhoWin
    {
        public int Winner { get; set; }
        public WhoWin()
        {
            Winner = -1;
        }
    }

    public enum Winner
    {
        none=-1,
        banker,
        player,
        tie
    }
    
}