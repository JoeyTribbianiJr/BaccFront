﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bacc_front
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		//public List<Player> Players { get; set; }

		public MainWindow()
		{
			//InitGame();
			Desk.Instance.Players[6].Balance = 100;
			Desk.Instance.Players[7].Balance = 5000;
			Desk.Instance.Players[8].Balance = 3000;
			Desk.Instance.Players[9].Balance = 6312;
			Desk.Instance.Players[9].BetScore = new Dictionary<BetSide, int>
			{
				{BetSide.banker,1000 },{BetSide.tie ,220}
			};
			InitializeComponent();
			Casino.DataContext = Desk.Instance;
		}
		private void InitGame()
		{
			//Players = Desk.Instance.Players;
		}
	}
}
