﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bacc_front
{
	public enum BetSide
	{
		banker = 0,
		tie,
		player
	}
	public enum BetDenomination
	{
		big = 100,
		small = 10
	}
	public delegate int NativeCountRule(BetSide winner, Dictionary<BetSide, int> bet);

	public class Player
	{

		private int id;
		private int balance;
		private int last_add;
		private int add_score;
		private int last_sub;
		private int sub_score;
		private bool bet_hide;
		private Dictionary<BetSide, int> bet_score;

		public Dictionary<BetSide, int> BetScore
		{
			get { return bet_score; }
		}

		public int Id { get => id; set => id = value; }
		public int Balance { get => balance; set => balance = value; }
		public int Last_add { get => last_add; set => last_add = value; }
		public int Add_score { get => add_score; set => add_score = value; }
		public int Last_sub { get => last_sub; set => last_sub = value; }
		public int Sub_score { get => sub_score; set => sub_score = value; }
		public bool Bet_hide { get => bet_hide; set => bet_hide = value; }

		public BetDenomination denomination;

		public event NativeCountRule count_rule;

		private void ClearBet()
		{
			bet_score = new Dictionary<BetSide, int>()  //本次押注
		{
			{BetSide.banker,0 },
			{BetSide.player,0 },
			{BetSide.tie,0 },
		};
		}
		public Player(int id, NativeCountRule count_rule)
		{
			this.Id = id;
			Balance = 0;        //总积分;
			Last_add = 0;       //最后上分
			Add_score = 0;      //总上分
			Last_sub = 0;       //最后下分
			Sub_score = 0;      //总下分
			bet_score = new Dictionary<BetSide, int>()  //本次押注
		{
			{BetSide.banker,0 },
			{BetSide.player,0 },
			{BetSide.tie,0 },
		};
			denomination = BetDenomination.big;  //押注筹码大小
			Bet_hide = false;       //是否隐藏压哪边

			this.count_rule = count_rule;
		}


		#region 玩家押注的函数
		public void Bet(BetSide side)
		{
			var add_score = Balance >= (int)denomination ? (int)denomination : Balance;

			Balance -= add_score;
			bet_score[side] += add_score;
		}
		public void CancleBet()
		{
			var cancle_score = bet_score.Values.Sum();
			Balance += cancle_score;

			ClearBet();
		}


		public void SetAmount()
		{
			denomination = denomination == BetDenomination.big ? BetDenomination.small : BetDenomination.big;
		}
		public void SetHide()
		{
			Bet_hide = Bet_hide == true ? false : true;
		}

		public void Count(BetSide winner)
		{
			Balance += count_rule(winner, bet_score);
		}
		#endregion

		#region 庄家上分退分的函数
		public void AddScore(int add_score)
		{
			this.Add_score += add_score;
		}
		public void SubScore(int sub_score)
		{
			var ss = this.Sub_score + sub_score;
			if (ss > Balance)
			{
				this.Sub_score = Balance;
			}
		}
		public void CancleAddOrSub()
		{
			Add_score = 0;
			Sub_score = 0;
		}
		public void ConfirmAdd()
		{
			Balance += Add_score;
			Last_add = Add_score;
			Add_score = 0;
		}
		public void ConfirmSub()
		{
			Balance -= Sub_score;
			Sub_score = 0;
			Last_sub = Sub_score;
		}
		#endregion
	}
}