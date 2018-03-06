using System;
using System.Collections;
using System.Collections.Generic;


namespace Bacc_front
{
	public class Round
	{
        public List<Card>[] HandCard { get => handCard; set => handCard = value; }
        /// <summary>
        /// <赢家,庄点数,闲点数>
        /// </summary>
        public Tuple<BetSide, int, int> Winner { get => winner; set => winner = value; }

        public Round(List<Card>[] handCard, Tuple<BetSide, int, int> winner)
        {
            this.handCard = handCard;
            this.winner = winner;
        }
        private List<Card>[] handCard;
        private Tuple<BetSide, int, int> winner;
    }
}