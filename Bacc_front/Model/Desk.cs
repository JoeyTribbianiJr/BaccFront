using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using WsUtils;

namespace Bacc_front
{
    
    [PropertyChanged.ImplementPropertyChanged]
    public class Desk
    {
        public ObservableCollection<Player> Players { get => players; set => players = value; }
        public int AllMostLimit { get; set; }
        public int LeastBet { get; set; }
        public int TieMostBet { get; set; }
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
            players = new ObservableCollection<Player>();
            AllMostLimit = Setting.Instance.GetIntSetting("total_limit_red");
            LeastBet = Setting.Instance.GetIntSetting("mini_chip_facevalue");
            TieMostBet = Setting.Instance.GetIntSetting("tie_limit_red");

            desk_amount = new Dictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
        }

        #region 赌桌规则
        public bool CanHeCancleBet(Player p)
        {
            var b_amount = desk_amount[BetSide.banker] - p.BetScore[(int)BetSide.banker];
            var p_amount = desk_amount[BetSide.player] - p.BetScore[BetSide.player];

            var desk_limit_red = _setting.GetIntSetting("desk_limit_red");

            //如果撤注后庄闲差仍小于限红则允许撤注
            return Math.Abs(b_amount - p_amount) <= desk_limit_red;
        }
        public bool CanHeBet(Player p, BetSide side)
        {
            var bet_amount = p.Balance >= p.denomination ? p.denomination : p.Balance;

            //待加入其他限制规则
            var can_he = LimitRed(bet_amount, side) && true;

            return can_he;
        }
        private bool LimitRed(int bet_amount, BetSide side)
        {
            var b_amount = desk_amount[BetSide.banker];
            var t_amount = desk_amount[BetSide.tie];
            var p_amount = desk_amount[BetSide.player];

            var desk_limit = _setting.GetIntSetting("desk_limit_red");

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
                var tie_limit = _setting.GetIntSetting("tie_limit_red");
                var desk_tie_red = Math.Abs(bet_amount + t_amount);
                if (desk_tie_red > tie_limit)
                {
                    return false;
                }
            }

            return true;
        }
        private int GetCardValue(Card card)
        {
            var weight = (int)card.GetCardWeight;
            if (weight > 9)
            {
                weight = 0;
            }
            return weight;
        }
        public int[] CancleSpace()
        {
            var res = new int[2];
            var banker = desk_amount[BetSide.banker];
            var player =  desk_amount[BetSide.player];
            var sub = banker - player;
            var desk_limit = _setting.GetIntSetting("desk_limit_red");
            res[0] = desk_limit - sub;
            res[1] = desk_limit + sub;
            return res;
        }
        #endregion
        #region 赌桌及玩家收益操作
        public void UpdateDeskAmount(BetSide side, int score)
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
        public void CalcAllPlayersEarning(Round cur_round)
        {
            var winner = cur_round.Winner;

            foreach (var player in players)
            {
                player.Balance += CalcPlayerEarning(winner.Item1, player.BetScore);
                player.ClearBet();
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
        #endregion

        #region 发牌及结算操作
        public List<Card>[] DealSingleCard()
        {
            var _curHandCards = new List<Card>[2];
            
            var player_card = new List<Card>();
            player_card.Add(Deck.Instance.Deal());
            var banker_card = new List<Card>();
            banker_card.Add(Deck.Instance.Deal());

            _curHandCards[0] = player_card;
            _curHandCards[1] = banker_card;
            return _curHandCards;
        }
        public List<Card>[] DealTwoCard()
        {
            var _curHandCards = new List<Card>[2];

            var player_card = new List<Card>();
            player_card.Add(Deck.Instance.Deal());
            var banker_card = new List<Card>();
            banker_card.Add(Deck.Instance.Deal());

            player_card.Add(Deck.Instance.Deal());
            banker_card.Add(Deck.Instance.Deal());

            _curHandCards[0] = player_card;
            _curHandCards[1] = banker_card;

            CheckThirdCard(_curHandCards);
            return _curHandCards;
        }
        public void CheckThirdCard(List<Card>[] hand_cards)
        {
            bool playerDrawStatus = false, bankerDrawStatus = false;
            int playerThirdCard = 0;
            int[] handValues = { CalcHandValue(hand_cards[0]), CalcHandValue(hand_cards[1]) };

            if (!(handValues[0] > 7) || !(handValues[1] > 7))
            {

                //Determine Player Hand first
                if (handValues[0] < 6)
                {
                    var d_card = Deck.Instance.Deal();
                    hand_cards[0].Add(d_card);
                    playerThirdCard = GetCardValue(d_card);
                    playerDrawStatus = true;
                }
                else
                    playerDrawStatus = false;

                //Determine Banker Hand
                if (playerDrawStatus == false)
                {
                    if (handValues[1] < 6)
                        bankerDrawStatus = true;
                }
                else
                {
                    if (handValues[1] < 3)
                        bankerDrawStatus = true;
                    else
                    {
                        switch (handValues[1])
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
                hand_cards[1].Add(Deck.Instance.Deal());
            }
        }
        private int CalcHandValue(List<Card> hand_cards)
        {
            var sum = 0;
            foreach (var card in hand_cards)
            {
                var weight = GetCardValue(card);
                sum += weight;
            }
            return sum % 10;
        }
        public Tuple<BetSide, int,int> GetWinner(List<Card>[] hand_cards)   //<赢家,庄点数,闲点数>
        {
            var p_amount = CalcHandValue(hand_cards[0]);
            var b_amount = CalcHandValue(hand_cards[1]);
            var value = Math.Abs(b_amount - p_amount);

            Tuple<BetSide, int, int> winner;
            if (b_amount == p_amount)
            {
                winner= new Tuple<BetSide, int,int>(BetSide.tie, b_amount, p_amount);
                return winner;
            }
            else
            {
                winner = b_amount > p_amount 
                    ? new Tuple<BetSide, int,int>(BetSide.banker, b_amount,p_amount) 
                    : new Tuple<BetSide, int,int>(BetSide.player, b_amount,p_amount);
                return winner;
            }
        }
        #endregion

        #region 私有变量
        private const float BANKER_ODDS = 0.95f;    
        private const float PLAYER_ODDS = 1;       
        private const float TIE_ODDS = 8;
        private float[] odds_map = new float[3] { BANKER_ODDS, TIE_ODDS, PLAYER_ODDS };

        private ObservableCollection<Player> players;
        private Dictionary<BetSide, int> desk_amount;

        private Setting _setting = Setting.Instance;
        private static Desk instance;
        #endregion
    }
    
}