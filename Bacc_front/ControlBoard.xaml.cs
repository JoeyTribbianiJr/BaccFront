using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bacc_front
{
    /// <summary>
    /// ControlBoard.xaml 的交互逻辑
    /// </summary>
    public partial class ControlBoard : Window
    {
        public ObservableCollection<Player> Players { get; set; }
        private const int player_num = 14;
        public ControlBoard()
        {

            InitializeComponent();
            Players = new ObservableCollection<Player>()
            {
                new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 }
            };
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
            InitPlayersForBoard();

            dgScore.ItemsSource = Players;
            DataContext = this;
        }
        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            var game = Game.Instance;
            game.Start();
        }
        #region 初始化
        private void InitPlayersForBoard()
        {
            var desk = Desk.Instance;
            for (int i = 0; i < player_num + 1; i++)
            {
                if (i == 12)
                {
                    continue;
                }
                var p = new Player(i + 1, desk.CalcPlayerEarning);
                p.Balance = 8000;
                desk.Players.Add(p);
            }
        }
        #endregion
        private void OnAddScore(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].AddScore(score);
        }

        private void OnConfirmAddScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].ConfirmAdd();
        }

        private void OnSubScore(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].SubScore(score);
        }

        private void OnSubAllScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].SubAllScore();
        }

        private void OnConfirmSubScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].ConfirmSub();
        }

        private void OnConfig(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
