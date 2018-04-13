using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WsUtils;

namespace Bacc_front
{
    [PropertyChanged.ImplementPropertyChanged]
    public class Desk
    {
        private const int player_num = 14;
        public ObservableCollection<Player> Players
        {
            get => players;
            set
            {
                players = value;
                
            }
        }
        public Dictionary<BetSide, int> desk_amount;
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
            Players = new ObservableCollection<Player>();
            if (Setting.Instance.GetJsonPlayersScore() == null) //如果是第一次启动游戏
            {
                Players = new ObservableCollection<Player>();
                for (int i = 0; i < player_num + 1; i++)
                {
                    if (i == 12)
                    {
                        continue;
                    }
                    var p = new Player(i + 1, CalcPlayerEarning)
                    {
                        Balance = 0,
                        CurEarn = 0
                    };
                    Players.Add(p);
                }
                SavePlayerScores();
            }
            else
            {
                Players = Setting.Instance.GetJsonPlayersScore();
                foreach (var p in players)
                {
                    var cancle_score = p.BetScore.Values.Sum();
                    p.Balance += cancle_score;
                    p.ClearBet();
                }
                Players = players;
            }
            //ControlBoard.Instance.dgScore.ItemsSource = Players;
            desk_amount = new Dictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
        }

        public ObservableCollection<AddSubScoreRecord> CreateNewScoreRecord()
        {
            var accounts = new ObservableCollection<AddSubScoreRecord>();
            for (int i = 0; i < player_num + 1; i++)
            {
                if (i == 12)
                {
                    continue;
                }
                accounts.Add(new AddSubScoreRecord()
                {
                    PlayerId = (i + 1).ToString(),
                    TotalAccount = 0,
                    TotalAddScore = 0,
                    TotalSubScore = 0
                });
            }
            return accounts;
        }
        public void SavePlayerScores()
        {
            Setting.Instance.SaveJsonPlayersScoreToDefault(Players);
        }
        public void CancelHide()
        {
            foreach (var p in Players)
            {
                p.Bet_hide = false;
            }
        }
        #region 赌桌规则
        public bool CanHeHide(Player p)
        {
            return p.Balance != 0;
        }
        public bool CanHeBetIn3Sec(Player p, BetSide side)
        {
            var bet_amount = 1;
            //待加入其他限制规则
            bool can_he = p.Balance > 0;
            var winner = Game.Instance.CurrentRound.Winner.Item1;
            if (!_setting._limit_red_on_3sec)    //超限红不可试分
            {
                if (winner != side)
                {
                    can_he = TotalLimitRed(bet_amount, side) && can_he;
                }
                else
                {
                    return false;
                }
            }
            if (_setting._limit_red_on_3sec)    //超限红可试分
            {
                if (winner != side)
                {
                    can_he = true;
                }
                else
                {
                    can_he = false;
                }
            }

            return can_he;
        }
        public bool CanHeCancleBet(Player p)
        {
            if (Game.Instance._isIn3)
            {
                return false;
            }
            var can_cancle_bet = _setting._can_cancle_bet;

            var b_amount = desk_amount[BetSide.banker] - p.BetScore[(int)BetSide.banker];
            var p_amount = desk_amount[BetSide.player] - p.BetScore[BetSide.player];
            var total_limit_red = _setting._total_limit_red;
            //如果撤注后庄闲差仍小于限红则允许撤注
            var limit_red_can = Math.Abs(b_amount - p_amount) <= total_limit_red;
            if (can_cancle_bet == 0)    //根据限红自动调整
            {
                return limit_red_can;
            }
            else
            {
                return (can_cancle_bet - p.BetScore.Values.Sum() >= 0) && limit_red_can;
            }
        }
        public bool CanHeBet(Player p, BetSide side)
        {
            var bet_amount = p.Balance >= p.denomination ? p.denomination : p.Balance;

            //待加入其他限制规则
            var can_he = p.Balance > 0 && TotalLimitRed(bet_amount, side) && MinLimitBet(p) && DeskLimitRed(bet_amount, side);

            return can_he;
        }
        public bool MinLimitBet(Player p)   //例如现在最小限注50，小筹码10，押的第一下就是50，第二次起每次10递增
        {
            //if (p.BetScore.Values.Sum() == 0)
            //{
            //    return p.Balance >= _setting._min_limit_bet;
            //}
            return true;
        }
        public int SetTotalLimitRedDenomination(int add_score, BetSide side)
        {
            var b_amount = desk_amount[BetSide.banker];
            var t_amount = desk_amount[BetSide.tie];
            var p_amount = desk_amount[BetSide.player];

            var total_limit = _setting._total_limit_red;
            int desk_red;

            switch (side)
            {
                case BetSide.banker:
                    desk_red = Math.Abs(add_score + b_amount - p_amount);
                    if (desk_red >= total_limit)
                    {
                        return total_limit - (b_amount - p_amount);
                    }
                    break;
                case BetSide.tie:
                    desk_red = add_score + t_amount;
                    if (desk_red >= _setting._tie_limit_red)
                    {
                        return _setting._tie_limit_red - t_amount;
                    }
                    break;
                case BetSide.player:
                    desk_red = Math.Abs(add_score + p_amount - b_amount);
                    if (desk_red >= total_limit)
                    {
                        return total_limit - (p_amount - b_amount);
                    }
                    break;
                default:
                    break;
            }
            return add_score;
        }
        private bool TotalLimitRed(int bet_amount, BetSide side)
        {
            var b_amount = desk_amount[BetSide.banker];
            var t_amount = desk_amount[BetSide.tie];
            var p_amount = desk_amount[BetSide.player];

            var total_limit = _setting._total_limit_red;
            var cur = Math.Abs(bet_amount - p_amount);

            if (side == BetSide.banker)
            {
                if (cur == total_limit)
                {
                    return false;
                }
                //var desk_red = Math.Abs(bet_amount + b_amount - p_amount);
                //if (desk_red > total_limit)
                //{
                //    return false;
                //}
            }
            if (side == BetSide.player)
            {
                if (cur == total_limit)
                {
                    return false;
                }
                //var desk_red = Math.Abs(bet_amount + p_amount - b_amount);
                //if (desk_red > total_limit)
                //{
                //    return false;
                //}
            }
            if (side == BetSide.tie)
            {
                var tie_limit = _setting._tie_limit_red;
                var desk_tie_red = Math.Abs(bet_amount + t_amount);
                if (desk_tie_red > tie_limit)
                {
                    return false;
                }
            }

            return true;
        }
        private bool DeskLimitRed(int bet_amount, BetSide side)
        {
            var desk_score = desk_amount.Values.Sum();
            var desk_limit_red = _setting._desk_limit_red;

            return bet_amount + desk_score <= desk_limit_red;
        }
        public int GetCardValue(Card card)
        {
            var weight = (int)card.GetCardWeight;
            var singleDouble = _setting._single_double;
            if (singleDouble == "单张牌")
            {
                if (weight == 1)
                {
                    weight = 14;
                }
                weight = (int)card.GetCardSuit * 14 + weight;
            }
            else
            {
                if (weight > 9)
                {
                    weight = 0;
                }
            }
            return weight;
        }
        public int[] CancleSpace()
        {
            var can_cancle_bet = _setting._can_cancle_bet;

            var res = new int[3];
            res[0] = _setting._total_limit_red;
            res[1] = _setting._total_limit_red;
            if (can_cancle_bet == 0)    //根据限红自动调整
            {
                var banker = desk_amount[BetSide.banker];
                var player = desk_amount[BetSide.player];
                var sub = banker - player;
                var desk_limit = _setting._total_limit_red;
                if (banker >= desk_limit || player >= desk_limit)
                {
                    res[0] = desk_limit - sub;
                    res[1] = desk_limit + sub;
                }
            }
            var tie = desk_amount[BetSide.tie];
            res[2] = _setting._tie_limit_red - tie;
            return res;
        }
        #endregion
        #region 赌桌及玩家收益操作
        public void ChangeDenomationType()
        {
            foreach (var p in Players)
            {
                if (p.Choose_denomination == BetDenomination.big)
                {
                    if (p.Balance < p.Denominations[(int)BetDenomination.big])
                    {
                        p.Choose_denomination = BetDenomination.mini;
                        p.IntChoose = (int)p.Choose_denomination;
                        p.denomination = p.Denominations[(int)p.Choose_denomination];
                    }
                }
            }
        }
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
                //player.Balance += CalcPlayerEarning(winner.Item1, player.BetScore);
                player.CurEarn = CalcPlayerEarning(winner.Item1, player.BetScore);
                player.ClearBet();

                //爆机累加
                Game.Instance.CurrentSession.BoomAcc += player.CurEarn;
            }
        }
        public void SettleEarnToBalance()
        {
            foreach (var player in players)
            {
                player.Balance += player.CurEarn;
                player.CurEarn = 0;
            }
            Setting.Instance.SaveJsonPlayersScoreToDefault(Desk.Instance.Players);
            Desk.Instance.ChangeDenomationType();
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

            if (!(handValues[0] > 7) && !(handValues[1] > 7))
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
                    {
                        bankerDrawStatus = true;
                    }
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
        public Tuple<BetSide, int, int> GetWinner(List<Card>[] hand_cards)   //<赢家,庄点数,闲点数>
        {
            var p_amount = CalcHandValue(hand_cards[0]);
            var b_amount = CalcHandValue(hand_cards[1]);
            var value = Math.Abs(b_amount - p_amount);

            Tuple<BetSide, int, int> winner;
            if (b_amount == p_amount)
            {
                winner = new Tuple<BetSide, int, int>(BetSide.tie, b_amount, p_amount);
                return winner;
            }
            else
            {
                winner = b_amount > p_amount
                    ? new Tuple<BetSide, int, int>(BetSide.banker, b_amount, p_amount)
                    : new Tuple<BetSide, int, int>(BetSide.player, b_amount, p_amount);
                return winner;
            }
        }

        public static int GetProfit(int winner, int banker, int player, int tie)
        {
            var side = (WinnerEnum)winner;
            switch (side)
            {
                case WinnerEnum.banker:
                    return (player + tie - (int)Math.Ceiling(BANKER_ODDS * banker));
                case WinnerEnum.tie:
                    return player + banker - (int)Math.Ceiling(TIE_ODDS* tie);
                case WinnerEnum.player:
                    return banker + tie - (int)Math.Ceiling(PLAYER_ODDS * player);
                default:
                    return 0;
            }
        }
        #endregion
        #region 私有变量
        private const float BANKER_ODDS = 0.95f;
        private const float PLAYER_ODDS = 1;
        private const float TIE_ODDS = 8;
        private float[] odds_map = new float[3] { BANKER_ODDS, TIE_ODDS, PLAYER_ODDS };

        private ObservableCollection<Player> players;

        private Setting _setting = Setting.Instance;
        private static Desk instance;
        #endregion
    }

}