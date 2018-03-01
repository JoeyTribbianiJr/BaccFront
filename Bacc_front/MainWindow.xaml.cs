using System;
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
using System.Windows.Threading;
using PropertyChanged;

namespace Bacc_front
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Game game { get; set; }
        public int hehe { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Casino.DataContext = Desk.Instance;
            InitGame();

            BindWaybills();
        }

        private void BindWaybills()
        {
            var round_num = Setting.Instance.GetIntSetting("round_num_per_session");
            var grid = this.grdBigWaybill;
            grid.DataContext = Desk.Instance.Waybill;

            for (int i = 0; i < round_num; i++)
            {
                Desk.Instance.Waybill.Add(new WhoWin() { Winner = 0 });
                var block = new Button();
                block.Style = (Style)Resources["WaybillBlock"];
                block.Tag = i;
                //block.Click += Button_Click;
                grid.Children.Add(block);

                Binding myBinding = new Binding();
                myBinding.Source = Desk.Instance;
                myBinding.Path = new PropertyPath("Waybill[" + i + "].Winner");
                myBinding.Mode = BindingMode.TwoWay;
                myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(block, DataContextProperty, myBinding);

                var col = i / 6;
                var row = i % 6;
                Grid.SetRow(block, row);
                Grid.SetColumn(block, col);
            }
        }

        private void InitGame()
        {
            game = new Game();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (game._isBetting)
            {
                var code = (int)e.Key;
                game.HandleKeyDown(code);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            game.HandleKeyUp((int)e.Key);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var tag = ((Button)sender).Tag;
            var idx = Convert.ToInt32(tag);
            Desk.Instance.Waybill[idx] = new WhoWin() { Winner = 2 };
            Desk.Instance.Waybill[2].Winner = 2;
        }
    }

}
