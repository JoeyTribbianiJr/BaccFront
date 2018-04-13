using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WsUtils.SqliteEFUtils;

namespace Bacc_front
{
    /// <summary>
    /// BetRecord.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.ImplementPropertyChanged]
    public partial class BetRecord : Window
    {
        public ObservableCollection<Player> Players { get; set; }
        public List<BetScoreRecord> RecordsInSession { get; set; }
        public ObservableCollection<BetScoreRecord> CurrentRecords { get; set; }
        public BetRecord()
        {
            InitializeComponent();
            using (var db = new SQLiteDB())
            {
                try
                {
                    RecordsInSession = db.BetScoreRecords.Where(
                            r => r.SessionIndex == db.BetScoreRecords.Max(mr => mr.SessionIndex))
                            .ToList();
                    if (RecordsInSession == null)
                    {
                        return;
                    }
                    SessionIds = db.BetScoreRecords.Select(b => b.SessionIndex + 1).Distinct().OrderBy(t=>t).ToList();
                    cmbSessionStrIndex.ItemsSource = SessionIds;
                    cmbSessionStrIndex.SelectedItem = RecordsInSession[0].SessionIndex + 1;
                }
                catch (Exception ex)
                {
                    return;
                }
            }
            CurrentRecords = new ObservableCollection<BetScoreRecord>(RecordsInSession.OrderBy(r=>r.RoundIndex).ToList());
            ShowWaybillAndDetail();
            BindCbEvent();
            cmbSessionStrIndex.SelectionChanged += cbRoundStrIndex_SelectionChanged;
        }
        public List<int> SessionIds = new List<int>();
        public void ShowWaybillAndDetail()
        {
            SetDetails();
            BindWaybills();
        }

        private void SetDetails()
        {
            var total_count = (double)RecordsInSession.Count;
            var b_count = (double)RecordsInSession.Count(r => r.Winner == (int)WinnerEnum.banker);
            txtBetRate.Text = "庄比例：" + (b_count / total_count).ToString("P")
                + "   和比例：" + ((double)RecordsInSession.Count(r => r.Winner == (int)WinnerEnum.tie) / total_count).ToString("P")
                + "   闲比例：" + ((double)RecordsInSession.Count(r => r.Winner == (int)WinnerEnum.player) / total_count) * 100 + "%";

            var b_all = CurrentRecords.Sum(b => b.DeskBanker);
            var t_all = CurrentRecords.Sum(b => b.DeskTie);
            var p_all = CurrentRecords.Sum(b => b.DeskPlayer);
            int profit = 0;
            foreach (var r in CurrentRecords)
            {
                profit += Desk.GetProfit(r.Winner, r.DeskBanker, r.DeskPlayer, r.DeskTie);
            }
            txtSessionDetails.Text = "当前局数：" + (CurrentRecords[0].SessionIndex + 1) + "\n" +
                "机台盈利：" + profit + "\n" +
                "庄总押：" + b_all + "\n" +
                "和总押：" + t_all + "\n" +
                "闲总押：" + p_all + "\n";
        }

        private void BindWaybills()
        {
            var round_num = 66;
            sm_waybill_btns = new List<Button>();
            grdSmallWaybill.Children.Clear();
            grdBigWaybill.Children.Clear();
            try
            {
                for (int i = 0; i < round_num; i++)
                {
                    ////小路单按钮绑定
                    var block = new Button();
                    block.Style = (Style)Resources["BetRecordSmallWaybillButton"];
                    block.Tag = i;
                    grdSmallWaybill.Children.Add(block);
                    block.DataContext = CurrentRecords.FirstOrDefault(r => r.RoundIndex == i);
                    sm_waybill_btns.Add(block);

                    //大路单按钮绑定
                    var blockBig = new Button();
                    blockBig.Style = (Style)Resources["BetRecordBigWaybillBlock"];
                    blockBig.Tag = i;
                    grdBigWaybill.Children.Add(blockBig);
                    blockBig.DataContext = CurrentRecords.FirstOrDefault(r => r.RoundIndex == i);
                    var col = i / 6;
                    var row = i % 6;
                    Grid.SetRow(blockBig, row);
                    Grid.SetColumn(blockBig, col);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            ResetSmWaybill();
            }
        }
        private void BindCbEvent()
        {
            foreach (var item in gdPickPlayer.Children)
            {
                if (item is CheckBox cb)
                {
                    pick_players_cbs.Add(cb);
                    cb.IsChecked = true;
                    cb.Checked += Cb_Checked;
                    cb.Unchecked += Cb_Checked;
                }
            }
        }

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            SetChecked();
        }

        private void SetChecked()
        {
            List<int> checked_ids = new List<int>();
            foreach (var cb in pick_players_cbs)
            {
                if (cb.IsChecked == true)
                {
                    var id = Convert.ToInt32(cb.Content);
                    id = id == 14 ? 13 : id == 15 ? 14 : id;
                    checked_ids.Add(id);
                }
            }
            for (int i = 0; i < 66; i++)
            {
                var record = RecordsInSession[i];
                int banker, tie, player;
                if (record == null || string.IsNullOrEmpty(record.JsonPlayerScores))
                {
                    banker = 0;
                    tie = 0;
                    player = 0;
                }
                else
                {
                    var scores = JsonConvert.DeserializeObject<ObservableCollection<Player>>(record.JsonPlayerScores);
                    var checked_players = scores.Where(s => checked_ids.Contains(s.Id));
                    banker = checked_players.Sum(p => p.BetScoreOnBank);
                    tie = checked_players.Sum(p => p.BetScoreOnTie);
                    player = checked_players.Sum(p => p.BetScoreOnPlayer);
                }
                CurrentRecords[i].CreateTime = record.CreateTime;
                CurrentRecords[i].RoundIndex = record.RoundIndex;
                CurrentRecords[i].SessionIndex = record.SessionIndex;
                CurrentRecords[i].Winner = record.Winner;
                CurrentRecords[i].DeskBanker = banker;
                CurrentRecords[i].DeskTie = tie;
                CurrentRecords[i].DeskPlayer = player;
            }
            ShowWaybillAndDetail();
        }

        private List<Button> sm_waybill_btns;
        private List<CheckBox> pick_players_cbs = new List<CheckBox>();
        private void ResetSmWaybill()
        {
            var Waybill = CurrentRecords;
            int pre_row = 0, pre_col = 0, pre = 0;
            int cur_side = Waybill[0].Winner;

            sm_waybill_btns[0].SetValue(Grid.ColumnProperty, pre_row);
            sm_waybill_btns[0].SetValue(Grid.RowProperty, pre_col);

            for (int i = 1; i < Waybill.Count; i++)
            {
                var w_i = Waybill[i].Winner;
                var w_pre = Waybill[pre].Winner;

                if (w_pre == (int)WinnerEnum.tie)
                {
                    if (cur_side == (int)WinnerEnum.tie || w_i == (int)WinnerEnum.tie)
                    {
                        if (++pre_row >= 9)
                        {
                            pre_row = 0;
                            pre_col++;
                        }
                        if (w_i != (int)WinnerEnum.tie)
                        {

                            cur_side = w_i;
                        }
                    }
                    else
                    {
                        if (w_i == cur_side)
                        {
                            if (++pre_row >= 9)
                            {
                                pre_row = 0;
                                pre_col++;
                            }
                        }
                        else
                        {
                            pre_row = 0;
                            pre_col++;
                            cur_side = w_i;
                        }
                    }
                }
                else
                {
                    if (w_i == (int)WinnerEnum.tie || w_i == w_pre)
                    {
                        if (++pre_row >= 9)
                        {
                            pre_row = 0;
                            pre_col++;
                        }
                    }
                    else
                    {
                        pre_row = 0;
                        pre_col++;
                        cur_side = w_i;
                    }
                }

                sm_waybill_btns[i].SetValue(Grid.ColumnProperty, pre_col);
                sm_waybill_btns[i].SetValue(Grid.RowProperty, pre_row);
                pre++;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in pick_players_cbs)
            {
                cb.Checked -= Cb_Checked;
                cb.Unchecked -= Cb_Checked;
                cb.IsChecked = true;
                cb.Checked += Cb_Checked;
                cb.Unchecked += Cb_Checked;
            }
            SetChecked();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            foreach (var cb in pick_players_cbs)
            {
                cb.Checked -= Cb_Checked;
                cb.Unchecked -= Cb_Checked;
                cb.IsChecked = false;
                cb.Checked += Cb_Checked;
                cb.Unchecked += Cb_Checked;
            }
            SetChecked();
        }
        private void cbRoundStrIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var id = (int)((ComboBox)sender).SelectedItem - 1;
            using (var db = new SQLiteDB())
            {
                RecordsInSession = db.BetScoreRecords.Where(b => b.SessionIndex == id).OrderBy(r=>r.RoundIndex).ToList();
                if (RecordsInSession == null)
                {
                    return;
                }
                SetChecked();
            }
        }

    }
}
