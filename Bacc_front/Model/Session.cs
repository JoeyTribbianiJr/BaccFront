using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bacc_front
{
	public class Session
	{
		public int SessionId;
		public Session(int id, ObservableCollection<Round> master_round = null)
		{
			this.SessionId = id;
            ResetSession(master_round);
        }

        public ObservableCollection<Round> AllRounds { get => allRounds; set => allRounds = value; }
        public DateTime StartTime { get => startTime; set => startTime = value; }

        private DateTime startTime;

        public void ResetSession(ObservableCollection<Round> master_round = null)
		{
			round_num = Setting.Instance.GetIntSetting("round_num_per_session");
			if (master_round == null)
			{
				AllRounds= CreateRounds(round_num);
			}
			else
			{
				//AllRounds = master_round.GetRange(round_num * (session_id - 1), round_num);
			}
			StartTime = DateTime.Now;
		}
        private ObservableCollection<Round> CreateRounds(int rounds_num)
        {
            var rounds = new ObservableCollection<Round>();
            for (int i = 0; i < rounds_num; i++)
            {
                var singleDouble = Setting.Instance.GetStrSetting("single_double");
                singleDouble = "两张牌";

                if (singleDouble == "单张牌")
                {
                    Desk.Instance.DealSingleCard();
                }
                if (singleDouble == "两张牌")
                {
                    Desk.Instance.DealTwoCard();
                }

                var hand_cards = Desk.Instance._curHandCards;
                var round = new Round()
                {
                    hand_card = hand_cards,
                    winner = Desk.Instance.GetWinner(hand_cards)
                };
                rounds.Add(round);
            }
            return rounds;
        }

        private ObservableCollection<Round> allRounds;
        private int round_num;
	}
}
