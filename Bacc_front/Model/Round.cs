using System.Collections;
using System.Collections.Generic;


namespace Bacc_front
{
	public class Round
	{
		public List<Card>[] hand_card;
		public BetSide winner;
		public Round()
		{
			hand_card = new List<Card>[2];
		}
	}
}