using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using Bacc_front.Properties;
using Newtonsoft.Json;
using PropertyChanged;
using WsUtils;
using WsUtils.SqliteEFUtils;

namespace Bacc_front
{
    public delegate int NativeCountRule(BetSide winner, ObservableDictionary<BetSide, int> bet);

    [ImplementPropertyChanged]
    public class Player
    {
        private int id;
        private int balance;
        private int last_add;
        private int add_score;
        private int last_sub;
        private int sub_score;
        private bool bet_hide;
        private int curEarn;
        private ObservableDictionary<BetSide, int> bet_score;
        public int IntChoose { get; set; }
        public ObservableDictionary<BetSide, int> BetScore
        {
            get { return bet_score; }
            set { bet_score = value; }
        }
        public int BetScoreOnBank { get; set; }
        public int BetScoreOnPlayer { get; set; }
        public int BetScoreOnTie { get; set; }

        public int Id { get => id; set => id = value; }
        public int Balance { get => balance; set => balance = value;}
        public int Last_add { get => last_add; set => last_add = value; }
        public int Add_score { get => add_score; set => add_score = value; }
        public int Last_sub { get => last_sub; set => last_sub = value; }
        public int Sub_score { get => sub_score; set => sub_score = value; }
        public bool Bet_hide { get => bet_hide; set => bet_hide = value; }

        public int denomination;
        public BetDenomination Choose_denomination { get; set; }
        public int[] Denominations { get; set; }
        public int CurEarn { get => curEarn; set => curEarn = value; }

        public event NativeCountRule count_rule;


        public Player(int id, NativeCountRule count_rule)
        {
            this.Id = id;
            Balance = 0;        //总积分;
            Last_add = 0;       //最后上分
            Add_score = 0;      //总上分
            Last_sub = 0;       //最后下分
            Sub_score = 0;      //总下分
            BetScoreOnBank = 0;
            BetScoreOnPlayer = 0;
            BetScoreOnTie = 0;
            bet_score = new ObservableDictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
            SetDenomination(BetDenomination.mini);

            Bet_hide = false;       //是否隐藏压哪边

            this.count_rule = count_rule;
        }

        public void SetDenomination(BetDenomination bd)
        {
            Denominations = new int[2];
            Denominations[0] = Setting.Instance._big_chip_facevalue;
            Denominations[1] = Setting.Instance._mini_chip_facevalue;
            Choose_denomination = bd;  //押注筹码大小
            IntChoose = (int)Choose_denomination;
            denomination = Denominations[(int)Choose_denomination];
        }
        #region 玩家押注的函数
        public void Bet(BetSide side)
        {
            if (Game.Instance._isIn3)
            {
                if (Desk.Instance.CanHeBetIn3Sec(this, side))
                {
                    var add_score = 1;
                    Balance -= add_score;
                    bet_score[side] += add_score;

                    Desk.Instance.UpdateDeskAmount(side, add_score);
                    switch (side)
                    {
                        case BetSide.banker:
                            BetScoreOnBank += add_score;
                            break;
                        case BetSide.player:
                            BetScoreOnPlayer += add_score;
                            break;
                        case BetSide.tie:
                            BetScoreOnTie += add_score;
                            break;
                    }
                }
            }
            else if (Desk.Instance.CanHeBet(this, side))
            {
                var add_score = Balance >= denomination ? denomination : Balance;

                //第一手计算最小限注
                if (BetScore.Values.Sum() == 0 )
                {
                    if(denomination < Setting.Instance._min_limit_bet && Balance  >= Setting.Instance._min_limit_bet)
                    {
                        add_score = Setting.Instance._min_limit_bet;
                    }
                }

                add_score= Desk.Instance.SetTotalLimitRedDenomination(add_score,side);

                Balance -= add_score;
                bet_score[side] += add_score;

                Desk.Instance.UpdateDeskAmount(side, add_score);

                switch (side)
                {
                    case BetSide.banker:
                        BetScoreOnBank += add_score;
                        break;
                    case BetSide.player:
                        BetScoreOnPlayer += add_score;
                        break;
                    case BetSide.tie:
                        BetScoreOnTie += add_score;
                        break;
                }
            }
        }

        public void CancleBet()
        {
            if (Desk.Instance.CanHeCancleBet(this))
            {
                var cancle_score = bet_score.Values.Sum();
                Balance += cancle_score;

                foreach (var bet in bet_score)
                {
                    Desk.Instance.UpdateDeskAmount(bet.Key, -bet.Value);
                }
                ClearBet();
            }
        }
        public void ClearBet()
        {
            BetScoreOnBank = 0;
            BetScoreOnPlayer = 0;
            BetScoreOnTie = 0;

            bet_score = new ObservableDictionary<BetSide, int>()
            {
                {BetSide.banker,0 },
                {BetSide.player,0 },
                {BetSide.tie,0 },
            };
        }
        public void Set3SecDenomination()
        {
            denomination = 1;
        }
        public void ChangeDenomination()
        {
            if (Choose_denomination == BetDenomination.mini && Balance >= Denominations[(int)BetDenomination.big])
            {
                SetDenomination(BetDenomination.big);
            }
            else
            {
                SetDenomination(BetDenomination.mini);
            }
        }
        public void SetHide()
        {
            if (Desk.Instance.CanHeHide(this))
            {
                Bet_hide = Bet_hide == true ? false : true;
            }
        }

        public void Count(BetSide winner)
        {
            Balance += count_rule(winner, bet_score);
        }
        #endregion

        #region 庄家上分退分的函数
        public void AddScore(int add_score)
        {
            Sub_score = 0;

            if (Add_score + add_score < 0)
            {
                Add_score = 0;
            }
            else
            {
                Add_score += add_score;
            }
        }
        public void SubScore(int sub_score)
        {
            Add_score = 0;

            var ss = Sub_score + sub_score;
            if (ss >= Balance)
            {
                Sub_score = Balance;
            }
            else
            {
                Sub_score = ss < 0 ? 0 : ss;
            }
        }
        public void CancleAddOrSub()
        {
            Add_score = 0;
            Sub_score = 0;
        }
        public void ConfirmAdd()
        {
            Game.Instance.Manager.SaveAddScoreAccount(Add_score, Id);
            if(Balance == 0)
            {
                SetDenomination(BetDenomination.mini);
            }
            Balance += Add_score;
            if (Add_score != 0)
            {
                Last_add = Add_score;
            }
            Add_score = 0;
            Desk.Instance.SavePlayerScores();
        }
        public void SubAllScore()
        {
            if (Sub_score == 0)
            {
                SubScore(Balance);
            }
            else
            {
                Sub_score = 0;
            }
        }
        public void ConfirmSub()
        {
            Game.Instance.Manager.SaveSubScoreAccount(Sub_score, Id);
            Balance -= Sub_score;
            if(Balance == 0)
            {
                SetDenomination(BetDenomination.mini);
            }
            if (Sub_score != 0)
            {
                Last_sub = Sub_score;
            }
            Sub_score = 0;
            Desk.Instance.SavePlayerScores();
        }
        #endregion
    }
}
