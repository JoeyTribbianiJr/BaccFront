using System;
using WsFSM;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections;
using System.Windows;
using Bacc_front.Properties;
using System.Linq;
using WsUtils.SqliteEFUtils;
using WsUtils;
using System.Threading;

namespace Bacc_front
{
    public enum XYindex
    {
        tie = 0,
        banker = 1,
        player = 2
    }
    public delegate void delegateGameStart();
    public delegate void delegateRoundOver();
    public delegate void delegateDealCard();
    [PropertyChanged.ImplementPropertyChanged]
    public class Game
    {
        #region 游戏参数
        public const int _frame_rate = 1;
        //public const float _bet_rate = 0.1f;

        public bool _isInBetting = false;
        public bool _isIn3 = false;
        #endregion
        #region 游戏对象
        public bool _isGameStarting = false;
        public bool _isShulffling = false;
        /// <summary>
        /// 动画生成器
        /// </summary>
        public GameAnimationGenerator Animator { get; set; }
        /// <summary>
        /// 音效播放器
        /// </summary>
        public MediaPlayer WavPlayer { get; set; }
        /// <summary>
        /// 游戏打印机
        /// </summary>
        public Printer GamePrinter { get; set; }
        /// <summary>
        /// 定时器
        /// </summary>
        public GameTimer CoreTimer;
        /// <summary>
        /// 倒计时器
        /// </summary>
        public int CountDown { get; set; }
        /// <summary>
        /// 上一秒倒计时
        /// </summary>
        public int PriorCountDown { get; set; }
        /// <summary>
        /// 当前游戏状态
        /// </summary>
        public GameState CurrentState { get; set; }
        /// <summary>
        /// 上一个游戏状态
        /// </summary>
        public GameState PriorState { get; set; }
        /// <summary>
        /// http客户端
        /// </summary>
        public SuperServer WebServer { get; set; }
        /// <summary>
        /// 状态显示器
        /// </summary>
        public string StateText { get; set; }
        public string BankerStateText { get; set; }
        public string PlayerStateText { get; set; }
        public string BetState { get; set; }
        /// <summary>
        /// 轮次计数器
        /// </summary>
        public int RoundIndex { get; set; }
        public string _roundStrIndex { get { return (RoundIndex + 1).ToString(); } }
        public Round CurrentRound
        {
            get
            {
                return CurrentSession.RoundsOfSession[RoundIndex == -1 ? 0 : RoundIndex];
            }
        }
        /// <summary>
        /// 局数计数器
        /// </summary>
        public int SessionIndex { get; set; }
        public string _sessionStrIndex { get { return (SessionIndex + 1).ToString(); } }
        public Session CurrentSession { get; set; }
        /// <summary>
        /// 后台发送过来的局集合
        /// </summary>
        public ObservableCollection<Session> LocalSessions { get; set; }
        /// <summary>
        /// 后台发送过来局集合时的SessionIndex。固定，直到下次后台发送时再变化.
        /// </summary>
        //public int LocalSessionIndex { get; set; }
        /// <summary>
        /// 当前局路单
        /// </summary>
        public ObservableCollection<WhoWin> Waybill { get; set; }
        /// <summary>
        /// 截至上一场的路单
        /// </summary>
        public ArrayList HistoryWaybill { get; set; }
        public GameDataManager Manager { get; set; }


        /// <summary>
        /// 本场的牌，用于发给服务器
        /// </summary>
        public int[] CardsForDeal { get; set; }
        /// <summary>
        /// 键盘映射表
        /// </summary>
        public KeyHandler KeyListener;
        /// <summary>
        /// 通知窗口游戏开始事件，进行路单绑定
        /// </summary>
        public event delegateGameStart NoticeWindowBind;
        /// <summary>
        /// 通知窗口本局结束事件，重置路单
        /// </summary>
        public event delegateRoundOver NoticeRoundOver;
        /// <summary>
        /// 向后台发送
        /// </summary>
        public BackLiveData BetBackLiveData { get; set; }
        public ObservableCollection<BetScoreRecord> BetRecordJsonDataToBack { get; set; }
        public static Game Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Game();
                }
                return instance;
            }
        }

        internal void SetGameUnlock()
        {
            IsGameLocked = false;
            Settings.Default.LGroup = 0;
            Settings.Default.Save();
        }

        internal void SetGameLock()
        {
            IsGameLocked = true;
            Settings.Default.LGroup = 1;
            Settings.Default.Save();
        }

        public bool IsGameLocked { get; private set; }

        #endregion
        public Game()
        {
            IsGameLocked = Settings.Default.LGroup == 1;
            PriorCountDown = 0;
            PriorState = GameState.Preparing;
            CurrentState = GameState.Preparing;

            //上次局数
            SessionIndex = _setting.CurSessionIndex;

            BetBackLiveData = new BackLiveData();
            CoreTimer = new GameTimer();
            KeyListener = new KeyHandler();
            WavPlayer = new MediaPlayer();
            GamePrinter = new Printer();
            Animator = new GameAnimationGenerator();

            WebServer = new SuperServer();
            WebServer.StartServer();

            Manager = new GameDataManager();
            Manager.InitLocalSessions();    //如果上次游戏后台有路单传送过来,且定义了下局，读取
            LocalSessions = Manager.LocalSessions;
            //生成新一局路单，可以在点开始按钮之前传送到后台
            NewSession();   //此函数在每局结束时调用，不在洗牌时调用，因此可以在洗牌时将新一局路单传送到后台
        }

        #region 游戏开始
        public void Start()
        {
            if (IsGameLocked)
            {
                return;
            }
            Game.Instance._isGameStarting = true;
            InitGame();
            InitFsm();
        }
        void InitGame()
        {
            _isSendingToServer = true;

            NoticeWindowBind();

            KeyListener.StartListen();
            CoreTimer.StartCountdownTimer(TimeSpan.FromSeconds(_frame_rate), Update);

            SetCancleSpace();
            MainWindow.Instance.txtSessionIndex.Visibility = Visibility.Visible;
            MainWindow.Instance.txtAllLimit.Text = _setting._total_limit_red.ToString();
            MainWindow.Instance.txtLeastBet.Text = _setting._min_limit_bet.ToString();
            MainWindow.Instance.txtTieMost.Text = _setting._tie_limit_red.ToString();
        }
        public void Update(object sender, EventArgs e)
        {
            _fsm.UpdateCallback(_frame_rate);
        }
        public void NewSession()
        {
            SessionIndex++;
            if (SessionIndex >= 1000)
            {
                SessionIndex = 0;
            }

            RoundIndex = -1;
            if (HistoryWaybill == null)
            {
                HistoryWaybill = new ArrayList();
            }

            try
            {
                CurrentSession = LocalSessions.First(s => s.SessionId == SessionIndex);
            }
            catch (Exception e)
            {
                CurrentSession = new Session(SessionIndex);
                //LocalSessions.Insert(SessionIndex, newSession);
                //LocalSessions.Add(newSession);
            }
            finally
            {
                WebServer.SendFrontPasswordToBack();
                WebServer.SendFrontSettingToBack();
                WebServer.SendCurSessionToBack();

                ResetWaybill();
            }
        }
        private void InitFsm()
        {
            _preparingState = new WsState("preparing");
            _shufflingState = new WsState("shuffling");
            _printingState = new WsState("printing");
            _bettingState = new WsState("betting");
            _dealingState = new WsState("dealing");
            _examineState = new WsState("examine");

            //初始化切换到洗牌
            _prepareShuffle = new WsTransition("preShuffle", _preparingState, _shufflingState);
            _prepareShuffle.OnCheck += () => { return true; };
            _preparingState.AddTransition(_prepareShuffle);

            //洗牌切换到打印
            _shufflingState.OnEnter += OnShuffleEnter;
            _shufflingState.OnUpdate += OnShufflling;
            _shufflingState.OnExit += OnShuffleExit;
            _shuffleBet = new WsTransition("shufflePrint", _shufflingState, _printingState);
            _shuffleBet.OnCheck += IsShuffled;
            _shufflingState.AddTransition(_shuffleBet);

            //打印切换到押注
            _printingState.OnEnter += _printingState_OnEnter;
            _printingState.OnUpdate += _printingState_OnUpdate;
            _printingState.OnExit += _printingState_OnExit;
            _printBet = new WsTransition("printBet", _printingState, _bettingState);
            _printBet.OnCheck += () => { return _isPrinted; };
            _printingState.AddTransition(_printBet);

            //押注切换到开牌
            _bettingState.OnEnter += OnBetEnter;
            _bettingState.OnUpdate += Betting;
            _bettingState.OnExit += OnBetExit;
            _betDeal = new WsTransition("betDeal", _bettingState, _dealingState);
            _betDeal.OnCheck += IsBetted;
            _bettingState.AddTransition(_betDeal);

            //开牌后切换到下一轮
            _dealingState.OnEnter += OnDealEnter;
            _dealingState.OnUpdate += Dealing;
            _dealingState.OnExit += OnDealExit;
            _dealNextBet = new WsTransition("dealNextBet", _dealingState, _bettingState);
            _dealNextBet.OnCheck += CanGoNextBet;
            _dealNextBet.OnTransition += GotoNextBet;

            //开牌后本局结束，验单，重新洗牌,切换到下一局
            _dealExamine = new WsTransition("dealExamine", _dealingState, _examineState);
            _dealExamine.OnCheck += IsSessionOver;
            _dealingState.AddTransition(_dealNextBet);
            _dealingState.AddTransition(_dealExamine);

            //验单
            _examineState.OnEnter += OnExamineEnter;
            _examineState.OnUpdate += OnExamining;
            _examineState.OnExit += OnExamineExit;
            _examineShuffle = new WsTransition("examineShuffle", _examineState, _shufflingState);
            _examineShuffle.OnCheck += IsExamineOver;
            _examineState.AddTransition(_examineShuffle);

            _fsm = new WsStateMachine("baccarat", _preparingState);
            _fsm.AddState(_preparingState);
            _fsm.AddState(_shufflingState);
            _fsm.AddState(_bettingState);
            _fsm.AddState(_dealingState);
            _fsm.AddState(_examineState);
        }
        #endregion
        #region 状态机运行
        private void OnShuffleEnter(IState state)
        {
            _isShulffling = true;
            CurrentState = GameState.Shuffling;
            MainWindow.Instance.bdPrepare.Visibility = Visibility.Visible;
            MainWindow.Instance.txtFrontStateTitle.Text = "正在洗牌";
            SetStateText("洗牌中");
            WebServer.SendToHttpServer();

            if (Setting.Instance._is_print_bill == "打印路单" || Setting.Instance._is_print_bill == "打印不监控")
            {
                Printer.OpenEPSONCashBox(1);
            }
        }

        private void OnShufflling(float f)
        {
            var timer = _shufflingState.Timer;

            if (_setting._check_waybill_tm != 0)
            {
                CoreTimer.SetCountDownWithTimer(timer, _setting._check_waybill_tm);
                MainWindow.Instance.txtShuffleCountdown.Text = CountDown.ToString() + " 秒后开始下一局";
                if (timer == 10)
                {
                    ControlBoard.Instance.Dispatcher.Invoke(new Action(() =>
                    {
                        ControlBoard.Instance.btnStartGame.IsEnabled = true;
                    }));
                }
                if (CoreTimer.IsStateTimeOver(timer, _setting._check_waybill_tm))
                {
                    BreakShuffle();
                    return;
                }
            }
            else
            {
                MainWindow.Instance.txtShuffleCountdown.Text = " 手动开始下一局";
                if (timer >= 3)
                {
                    ControlBoard.Instance.Dispatcher.Invoke(new Action(() =>
                    {
                        ControlBoard.Instance.btnStartGame.IsEnabled = true;
                    }));
                }
            }
            WebServer.SendToHttpServer();
        }
        private bool IsShuffled()
        {
            return _isShuffled;
        }
        public void BreakShuffle()
        {
            ControlBoard.Instance.Dispatcher.Invoke(new Action(() =>
            {
                ControlBoard.Instance.btnStartGame.IsEnabled = false;
            }), System.Windows.Threading.DispatcherPriority.Send);
            if (_isGameStarting)
            {
                _isShulffling = false;
                _isShuffled = true;
            }
        }
        private void OnShuffleExit(IState state)
        {
            ControlBoard.Instance.Dispatcher.Invoke(new Action(() =>
               {
                   ControlBoard.Instance.btnStartGame.IsEnabled = false;
               }), System.Windows.Threading.DispatcherPriority.Send);
            Manager.SaveNewSession(CurrentSession);
            MainWindow.Instance.bdPrepare.Visibility = Visibility.Hidden;
            HistoryWaybill = new ArrayList();
            _isShuffled = false;

            _setting.CurSessionIndex = SessionIndex;
            Settings.Default.CurrentSessionIndex = SessionIndex;
            Settings.Default.Save();
        }

        private void _printingState_OnEnter(IState state)
        {
            CurrentState = GameState.Printing;
            _isPrinting = true;
            _waybillPrinted = false;
            MainWindow.Instance.txtCountDown.Text = 0.ToString();
            WebServer.SendToHttpServer();
        }
        private void _printingState_OnUpdate(float f)
        {
            var timer = _printingState.Timer;
            try
            {
                if (Setting.Instance._is_print_bill == "打印路单" || Setting.Instance._is_print_bill == "打印不监控")
                {
                    if (!_waybillPrinted)
                    {
                        SetStateText("打印路单");
                        Printer.DoorTest();
                        if (!Printer.IsDoorClosed)
                        {
                            if (timer % 3 == 0)
                            {
                                MessageBox.Show("打印机门开!等待处理");
                            }
                            return;
                        }
                        Printer.PaperTest();
                        if (!Printer.HasPaper)
                        {
                            if (timer % 3 == 0)
                            {
                                MessageBox.Show("打印机缺纸!等待处理");
                            }
                            return;
                        }
                        GamePrinter.PrintWaybill();
                        _waybillPrinted = true;
                        _printingState.ResetTimer(0);
                    }
                    if (_waybillPrinted && timer >= 8)
                    {
                        _waybillPrinted = false;
                        _isPrinted = true;
                        _isPrinting = false;
                    }
                }
                else
                {
                    _isPrinted = true;
                    _isPrinting = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }
        private void _printingState_OnExit(IState state)
        {
            //if (!_place1Played && CoreTimer.IsCountdownTick(timer, 0, _setting._check_waybill_tm))
            //{
            _isPrinted = false;
            PlayWav("place1-");
            //_place1Played = true;
            //}
        }

        private void OnBetEnter(IState state)
        {
            _isKillBig = false;
            CurrentState = GameState.Betting;
            WebServer.SendBetStartToHttpServer();
            RoundIndex++;
            _isInBetting = true;
        }
        private void Betting(float f)
        {
            var timer = _bettingState.Timer;
            CoreTimer.SetCountDownWithTimer(timer, _setting._betTime);
            SetStateText("押注中");

            if (CoreTimer.IsStateTimeOver(timer, _setting._betTime))
            {
                _isInBetting = false;
                return;
            }
            WebServer.SendToHttpServer();
            if (Animator.btnLst != null)
            {
                foreach (var card in Animator.btnLst)
                {
                    if (!_hideCards && CoreTimer.IsCountdownTick(timer, 10, _setting._betTime))
                    {
                        card.Visibility = Visibility.Hidden;
                        _hideCards = true;
                    }
                    else
                    {
                        card.Visibility = Visibility.Visible;
                    }
                }
            }
            if (!_stopBetPlayed && CoreTimer.IsCountdownTick(timer, 1, _setting._betTime))
            {
                _stopBetPlayed = true;
                PlayWav("StopBet");
            }
            var left5Secs = _setting._betTime - 6;
            //DoOneThingInTimespan(timer, ref _hasNoticeServerRoundStart, ref _startSentMessage, left5Secs, left5Secs + 1, SendToServer);
            if (_setting._is3SecOn)
            {
                if (CoreTimer.IsCountdownTick(timer, 3, _setting._betTime))
                {
                    KillBig();
                    _isIn3 = true;
                    return;
                }
            }
        }
        private void KillBig()
        {
            try
            {
                if (!_isKillBig)
                {
                    return;
                }
                var b_profit = Desk.GetProfit((int)BetSide.banker, Desk.Instance.desk_amount[BetSide.banker],
                    Desk.Instance.desk_amount[BetSide.player], Desk.Instance.desk_amount[BetSide.tie]);
                var p_profit = Desk.GetProfit((int)BetSide.player, Desk.Instance.desk_amount[BetSide.banker],
                    Desk.Instance.desk_amount[BetSide.player], Desk.Instance.desk_amount[BetSide.tie]);
                //var t_profit = Desk.GetProfit((int)BetSide.player, Desk.Instance.desk_amount[BetSide.banker],
                //    Desk.Instance.desk_amount[BetSide.player], Desk.Instance.desk_amount[BetSide.tie]);
                BetSide winner = BetSide.banker;
                if (b_profit == p_profit)
                {
                    return;
                }
                if (b_profit < p_profit && CurrentRound.Winner.Item1 == BetSide.banker)
                {
                    winner = BetSide.player;
                }
                if (p_profit < b_profit && CurrentRound.Winner.Item1 == BetSide.player)
                {
                    winner = BetSide.banker;
                }
                var round = Session.CreateRoundByWinner(winner);
                var roundidx = Game.Instance.RoundIndex == -1 ? 0 : Game.Instance.RoundIndex;
                Game.Instance.CurrentSession.RoundsOfSession[roundidx] = round;
            }
            catch (Exception ex)
            {
            }

        }
        private bool IsBetted()
        {
            return !_isInBetting;
        }
        private void OnBetExit(IState state)
        {
            _stopBetPlayed = false;
            _hideCards = false;
            _isIn3 = false;
            BankerStateText = "庄";
            PlayerStateText = "闲";
            Desk.Instance.CancelHide();
            KeyListener.CanclePressed();
            if (!_setting._is3SecOn)
            {
                KillBig();
            }
            _isKillBig = false;
        }

        private void OnDealEnter(IState state)
        {
            CurrentState = GameState.Dealing;
            CardsForDeal = ConvertHandCardForServerSB();
            WebServer.SendDealStartToHttpServer();
            SetStateText("开牌中");
            MainWindow.Instance.txtCountDown.Text = "0";
        }
        private void Dealing(float f)
        {
            var timer = _dealingState.Timer;
            CountDown = (int)timer;

            CoreTimer.DoOneThingInTimespan(timer, ref _isPinCardDone, ref _isInPinCard, 0f, 1f, Animator.PinCardAnimation);

            float endtime = 17f;
            if (CurrentRound.HandCard[1].Count == 2)
            {
                endtime -= 3f;
            }
            if (0 < timer && timer <= endtime)
            {
                WebServer.SendToHttpServer();
            }
            //Animator.visibleDuration = _setting._betTime - 10 + endtime + 8;
            Animator.visibleDuration = 40;
            //CoreTimer.DoOneThingInTimespan(timer, ref _isDeal4CardDone, ref _isDealing4Card, 2f, endtime, Animator.StartDealAnimation);
            CoreTimer.DoDealAnimationInTimespan(timer, ref _isDeal4CardDone, ref _isDealing4Card, 2f, endtime, Animator.StartDealAnimation);
            CoreTimer.DoOneThingInTimespan(timer, ref _isSetWinstatDone, ref _isSettingWinStat, endtime + 1f, endtime + 5f, CalcEarning);

            if (timer > endtime + 5f)
            {
                Desk.Instance.SettleEarnToBalance();
                CheckBoom();
                if (RoundIndex + 1 >= _setting._round_num_per_session)
                {
                    _sessionOver = true;
                }
                else
                {
                    _sessionOver = false;
                    _betRoundOver = true;
                }
            }
        }

        #region 开牌
        private void CalcEarning()
        {
            Waybill[RoundIndex].Winner = (int)CurrentRound.Winner.Item1;
            HistoryWaybill = ConvertWaybillForServerSB();
            WebServer.SendDealEndToHttpServer();
            NoticeRoundOver();

            ThreadPool.QueueUserWorkItem(saveBetRecordsCallback);

            Desk.Instance.CalcAllPlayersEarning(CurrentRound);
            SetWinStateText();

            Desk.Instance.ClearDeskAmount();
        }

        private void saveBetRecordsCallback(object state)
        {
            Manager.SaveBetRecords();
            WebServer.SendSummationBetRecordToBack();
        }

        private void saveBetRecordsCallback()
        {
        }
        private void CheckBoom()
        {
            if (CurrentSession.BoomAcc >= _setting._boom || _isServerBoom)
            {
                var window = MainWindow.Instance;
                window.imgBoom.Visibility = Visibility.Visible;

                window.g3.Visibility = Visibility.Visible;
                window.g2.Visibility = Visibility.Hidden;
                window.bg.Visibility = Visibility.Hidden;
                window.pg.Visibility = Visibility.Hidden;

                WebServer.SendBoomToHttpServer();

                CoreTimer.StopTimer();
                ControlBoard.Instance.btnStartGame.IsEnabled = false;
                _isServerBoom = false;
            }
        }
        public void SetWinStateText()
        {
            var winner = CurrentRound.Winner;
            var window = MainWindow.Instance;
            string b_str, p_str;
            switch (winner.Item1)
            {
                case BetSide.banker:
                    b_str = "庄赢" + (winner.Item2) + " 点";
                    p_str = "闲 " + (winner.Item3) + " 点";
                    BankerStateText = b_str;
                    PlayerStateText = p_str;
                    if (!_winbPlayed)
                    {
                        PlayWav("winB");
                        _winbPlayed = true;
                    }
                    SetStateText("庄赢！");
                    window.g3.Visibility = Visibility.Collapsed;
                    window.g2.Visibility = Visibility.Visible;
                    window.bg.Visibility = Visibility.Visible;
                    window.pg.Visibility = Visibility.Hidden;
                    break;
                case BetSide.tie:
                    b_str = "和";
                    p_str = "和";
                    BankerStateText = "和牌退注";
                    PlayerStateText = "和牌退注";
                    if (!_winTPlayed)
                    {
                        _winTPlayed = true;
                        PlayWav("winT");
                    }
                    SetStateText("和");
                    break;
                case BetSide.player:
                    b_str = "庄 " + (winner.Item2) + " 点";
                    p_str = "闲赢" + (winner.Item3) + " 点";
                    BankerStateText = b_str;
                    PlayerStateText = p_str;
                    if (!_winpPlayed)
                    {
                        _winbPlayed = true;
                        PlayWav("winP");
                    }
                    SetStateText("闲赢！");
                    window.g3.Visibility = Visibility.Collapsed;
                    window.g2.Visibility = Visibility.Visible;
                    window.bg.Visibility = Visibility.Hidden;
                    window.pg.Visibility = Visibility.Visible;
                    break;
                default:
                    b_str = "";
                    p_str = "";
                    break;
            }
        }
        public void Set4CardState()
        {
            var player = CurrentRound.HandCard[0].Take(2).Sum(c => Desk.Instance.GetCardValue(c)) % 10;
            var banker = CurrentRound.HandCard[1].Take(2).Sum(c => Desk.Instance.GetCardValue(c)) % 10;
            var b_str = "庄 " + banker + " 点";
            var p_str = "闲 " + player + " 点";
            BankerStateText = b_str;
            PlayerStateText = p_str;
        }
        void SetStateText(string state)
        {
            StateText = state;
        }
        #endregion

        private void OnDealExit(IState state)
        {
            //WebServer.SendDealEndToHttpServer();
            var window = MainWindow.Instance;
            window.g3.Visibility = Visibility.Visible;
            window.g2.Visibility = Visibility.Collapsed;
            window.bg.Visibility = Visibility.Hidden;
            window.pg.Visibility = Visibility.Hidden;

            _betRoundOver = false;
            _winbPlayed = false;
            _winpPlayed = false;
            _place1Played = false;
            _winTPlayed = false;
            _isInPinCard = false;
            _isPinCardDone = false;
            _isDeal4CardDone = false;
            _isDealing4Card = false;
            _isSetWinstatDone = false;
            _isSettingWinStat = false;

            SetCancleSpace();
            _sessionOver = false;
            _betRoundOver = false;
        }
        private bool CanGoNextBet()
        {
            return _betRoundOver && !_sessionOver;
        }
        private bool GotoNextBet()
        {
            if (!_place1Played)
            {
                PlayWav("place1-");
                _place1Played = true;
            }
            return true;
        }
        private bool IsSessionOver()
        {
            return _sessionOver;
        }
        private void OnExamineEnter(IState state)
        {
            NewSession();
            //if (_setting._check_waybill_tm == 0)
            //{
            //    SetStateText("检查路单中");
            //    MainWindow.Instance.txtCountDown.Text = "0";
            //    ControlBoard.Instance.btnStartGame.IsEnabled = true;
            //    CoreTimer.StopTimer();
            //    return;
            //}
        }
        private void OnExamining(float f)
        {
            //var check_tm = Setting.Instance._check_waybill_tm;
            //float timer = 0;
            //if (_firstExamine)
            //{
            //    timer = 0;
            //    _firstExamine = false;
            //}
            //else if (timer < check_tm)
            //{
            //    CoreTimer.SetCountDownWithTimer(timer, check_tm);
            //    SetStateText("检查路单中");
            //    timer += _frame_rate;
            //}
            //_firstExamine = true;
        }
        private bool IsExamineOver()
        {
            return true;
        }
        private void OnExamineExit(IState state)
        {
        }
        #endregion
        public void SetCancleSpace()
        {
            var space = Desk.Instance.CancleSpace();

            BankerStateText = "庄押" + space[0] + "分可撤注";
            PlayerStateText = "闲押" + space[1] + "分可撤注";
            if (space[0] == 0)
            {
                BetState = "庄 满 注";
                MainWindow.Instance.Dispatcher.Invoke(new Action(() => MainWindow.Instance.txtBetState.Foreground = new SolidColorBrush(Colors.Red)));
            }
            else if (space[1] == 0)
            {
                BetState = "闲 满 注";
                MainWindow.Instance.Dispatcher.Invoke(new Action(() => MainWindow.Instance.txtBetState.Foreground = new SolidColorBrush(Colors.Blue)));
            }
            else if (space[2] == 0)
            {
                BetState = "和 满 注";
                MainWindow.Instance.Dispatcher.Invoke(new Action(() => MainWindow.Instance.txtBetState.Foreground = new SolidColorBrush(Colors.Green)));
            }
            else
            {
                BetState = "";
            }
        }
        #region 音乐播放器
        public void PlayWav(string name)
        {
            WavPlayer.Open(new Uri("Wav/" + name + ".wav", UriKind.Relative));
            WavPlayer.Play();
        }
        #endregion
        public ArrayList ConvertWaybillForServerSB()
        {
            ArrayList arr = new ArrayList();
            for (int i = 0; i <= RoundIndex; i++)
            {
                arr.Add(Waybill[i].Winner);
            }
            return arr;
        }
        public int[] ConvertHandCardForServerSB()
        {
            int[] arr = new int[6];
            var p_cards = CurrentRound.HandCard[0];
            for (int i = 0; i < 2; i++)
            {
                arr[i] = p_cards[i].PngName;
            }
            var b_cards = CurrentRound.HandCard[1];
            for (int i = 3; i < 5; i++)
            {
                arr[i] = b_cards[i - 3].PngName;
            }
            arr[2] = p_cards.Count == 3 ? p_cards[2].PngName : 0;
            arr[5] = b_cards.Count == 3 ? b_cards[2].PngName : 0;
            return arr;
        }
        public void ResetWaybill()
        {
            var rounds = CurrentSession.RoundsOfSession;
            if (Waybill == null)
            {
                Waybill = new ObservableCollection<WhoWin>();
                for (int i = 0; i < rounds.Count; i++)
                {
                    Waybill.Add(new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    });
                }
            }
            else
            {
                for (int i = 0; i < rounds.Count; i++)
                {
                    Waybill[i] = new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    };
                }
            }
        }
        public void DisplayAllWaybill()
        {
            var rounds = CurrentSession.RoundsOfSession;
            for (int i = 0; i < rounds.Count; i++)
            {
                Waybill[i] = new WhoWin()
                {
                    Winner = (int)rounds[i].Winner.Item1
                };
            }
            NoticeRoundOver();
        }
        #region 状态机
        private WsStateMachine _fsm;

        private WsState _preparingState;  //点击开始按钮后
        private WsState _shufflingState;    //洗牌中
        private WsState _printingState;    //洗牌中
        private WsState _bettingState;  //押注中
        private WsState _dealingState;  //开牌中
        private WsState _examineState;
        private WsTransition _prepareShuffle;
        private WsTransition _printBet;
        private WsTransition _shuffleBet;
        private WsTransition _betDeal;
        private WsTransition _dealNextBet;
        private WsTransition _dealExamine;   //开牌后重新洗牌,洗牌前先对单
        private WsTransition _examineShuffle;

        private bool _isShuffled = false;
        private bool _betRoundOver = false;
        private bool _sessionOver = false;

        private static Game instance;
        private Setting _setting = Setting.Instance;
        private bool _firstSessionStart = true;
        private bool _firstExamine = true;
        private bool _stopBetPlayed = false;
        private bool _place1Played = false;
        private bool _winbPlayed = false;
        private bool _winpPlayed = false;
        private bool _winTPlayed = false;
        private bool _isInPinCard = false;
        private bool _isPinCardDone = false;
        private bool _isDeal4CardDone = false;
        private bool _isDealing4Card = false;
        private bool _isSetWinstatDone = false;
        private bool _isSettingWinStat = false;
        private bool _waybillPrinted = false;
        internal bool _isSendingToServer = false;
        private bool _hideCards = false;
        private bool _isPrinted = false;
        public bool _isPrinting = false;
        public bool _isServerBoom = false;
        public bool _isKillBig = false;
        #endregion
    }
}