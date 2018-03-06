
using System.Collections;

namespace Bacc_front
{
    public enum BetSide
    {
        banker = 0,
        tie = 1,
        player =2
    }

    /// <summary>
    /// 每次押注面额
    /// </summary>
    public enum BetDenomination
    {
        big = 0,
        mini = 1
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
    public enum WinnerEnum
    {
        none = -1,
        banker = 0,
        tie = 1,
        player =2
    }
    public enum Suits
	{
		Club,
		Diamond,
		Heart,
		Spade,
	}
	public enum Weight
	{
		One = 1,
		Two,
		Three,
		Four,
		Five,
		Six,
		Seven,
		Eight,
		Nine,
		Ten ,
		Jack ,
		Queen,
		King ,
	}
    public enum RemoteCommand
    {
        ImportFront = 2
    }
}