using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bacc_front
{
	public class Session
	{
		public int SessionId;
		public Session(int id)
		{
			SessionId = id;
			RoundNumber = Setting.Instance.GetIntSetting("round_num_per_session");
			RoundsOfSession= CreateRounds(RoundNumber);
			StartTime = DateTime.Now;
        }

        public int RoundNumber;
        public ObservableCollection<Round> RoundsOfSession { get => allRounds; set => allRounds = value; }
        public DateTime StartTime { get => startTime; set => startTime = value; }
        
        private ObservableCollection<Round> CreateRounds(int rounds_num)
        {
            var rounds = new ObservableCollection<Round>();
            for (int i = 0; i < rounds_num; i++)
            {
                var singleDouble = Setting.Instance.GetStrSetting("single_double");

                List<Card>[] hand_card = new List<Card>[2]; 
                if (singleDouble == "单张牌")
                {
                    hand_card =  Desk.Instance.DealSingleCard();
                }
                if (singleDouble == "两张牌")
                {
                    hand_card =  Desk.Instance.DealTwoCard();
                }

                var round = new Round(hand_card, Desk.Instance.GetWinner(hand_card));
                rounds.Add(round);
            }
            return rounds;
        }

        private DateTime startTime;
        private ObservableCollection<Round> allRounds;
	}
}
