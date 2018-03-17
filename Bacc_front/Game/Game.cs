using System;
using WsFSM;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections;
using System.Windows;
using WsUtils.SqliteEFUtils;
using Newtonsoft.Json;

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
        public PrintSth GamePrinter { get; set; }
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
        SuperServer WebServer { get; set; }
        /// <summary>
        /// 状态显示器
        /// </summary>
        public string StateText { get; set; }
        public string BankerStateText { get; set; }
        public string PlayerStateText { get; set; }
        /// <summary>
        /// 轮次计数器
        /// </summary>
        public int RoundIndex { get; set; }
        public string _roundStrIndex { get { return (RoundIndex + 1).ToString(); } }
        public Round CurrentRound
        {
            get
            {
                return CurrentSession.RoundsOfSession[RoundIndex == -1 ? 0 :RoundIndex ];
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
        public int LocalSessionIndex { get; set; }  
        /// <summary>
        /// 当前局路单
        /// </summary>
        public ObservableCollection<WhoWin> Waybill { get; set; }
        public GameDataManager Manager { get; set; }
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
        /// 向后台发送和入库
        /// </summary>
        public BetScoreRecord BetRecord { get; set; }
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
        public string ServerState { get; private set; }

        #endregion
        public Game()
        {
            Manager = new GameDataManager();
            PriorCountDown = 0;
            PriorState = GameState.Preparing;

            CoreTimer = new GameTimer();
            KeyListener = new KeyHandler();
            WavPlayer = new MediaPlayer();
            GamePrinter = new PrintSth();
            Animator = new GameAnimationGenerator();
            WebServer = new SuperServer();

            LocalSessions = new ObservableCollection<Session>();
        }

        #region 游戏开始
        public void Start()
        {
            InitGame();
            InitFsm();
        }
        void InitGame()
        {
            _isGameStarting = true;
            _isSendingToServer = true;
            SessionIndex = _setting.CurSessionIndex;

            NewSession();
            NoticeWindowBind();

            KeyListener.StartListen();
            CoreTimer.StartCountdownTimer(TimeSpan.FromSeconds(_frame_rate), Update);

            SetCancleSpace();
        }
        private void InitFsm()
        {
            _preparingState = new WsState("preparing");
            _shufflingState = new WsState("shuffling");
            _bettingState = new WsState("betting");
            _dealingState = new WsState("dealing");
            _examineState = new WsState("examine");

            //初始化切换到洗牌
            _prepareShuffle = new WsTransition("preShuffle", _preparingState, _shufflingState);
            _prepareShuffle.OnCheck += () => { return true; };
            _preparingState.AddTransition(_prepareShuffle);

            //洗牌切换到押注
            _shufflingState.OnEnter += OnShuffleEnter;
            _shufflingState.OnUpdate += OnShufflling;
            _shufflingState.OnExit += OnShuffleExit;
            _shuffleBet = new WsTransition("shuffleBet", _shufflingState, _bettingState);
            _shuffleBet.OnCheck += IsShuffled;
            _shufflingState.AddTransition(_shuffleBet);

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
        public void Update(object sender, EventArgs e)
        {
            Manager.SetBetRecord();
            WebServer.SendToHttpServer();
            WebServer.SendToBack();
            _fsm.UpdateCallback(_frame_rate);
        }
        #endregion

        #region 状态机运行
        private void OnShuffleEnter(IState state)
        {
            CurrentState = GameState.Shuffling;
            SetStateText("洗牌中");
        }
        private void OnShufflling(float f)
        {
            var timer = _shufflingState.Timer;

            CoreTimer.SetCountDownWithTimer(timer, _setting._shufTime);

            if (Setting.Instance.is_print_bill)
            {
                if (!_waybillPrinted && CoreTimer.IsCountdownTick(timer, 5, _setting._shufTime))
                {
                    GamePrinter.PrintWaybill();
                    _waybillPrinted = true;
                }
            }

            if (!_place1Played && CoreTimer.IsCountdownTick(timer, 0, _setting._shufTime))
            {
                PlayWav("place1-");
                _place1Played = true;
            }
            if (CoreTimer.IsStateTimeOver(timer, _setting._shufTime))
            {
                _isShuffled = true;
                return;
            }
        }
        private bool IsShuffled()
        {
            return _isShuffled;
        }
        private void OnShuffleExit(IState state)
        {
            _waybillPrinted = false;
            _place1Played = false;
            _isShuffled = false;
        }

        private void OnBetEnter(IState state)
        {
            CurrentState = GameState.Betting;
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
                    _isIn3 = true;
                    return;
                }
            }
        }
        private bool IsBetted()
        {
            return !_isInBetting;
        }
        private void OnBetExit(IState state)
        {
            _stopBetPlayed = false;
            _isIn3 = false;
        }

        private void OnDealEnter(IState state)
        {
            CurrentState = GameState.Dealing;
            CountDown = -1; //黑科技，让服务器能收到一条dealing
            SetStateText("");
            MainWindow.Instance.txtCountDown.Text = "开牌中";
        }
        private void Dealing(float f)
        {
            var timer = _dealingState.Timer;

            CoreTimer.DoOneThingInTimespan(timer, ref _isPinCardDone, ref _isInPinCard, 0f, 1f, Animator.PinCardAnimation);
            float endtime = 20.5f;
            if (CurrentRound.HandCard[1].Count == 2)
            {
                endtime -= 3.5f;
            }
            CoreTimer.DoOneThingInTimespan(timer, ref _isDeal4CardDone, ref _isDealing4Card, 2f, endtime, Animator.StartDealAnimation);
            CoreTimer.DoOneThingInTimespan(timer, ref _isSetWinstatDone, ref _isSettingWinStat, endtime + 1f, endtime + 5f, CalcEarning);

            if (timer > endtime + 5f)
            {
                if (RoundIndex + 1 >= _setting._roundNumPerSession)
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
        private void OnDealExit(IState state)
        {

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
            if (_sessionOver)
            {
                _sessionOver = false;
                NewSession();
            }
        }
        private void OnExamineEnter(IState state)
        {
        }

        private void OnExamining(float f)
        {
            var check_tm = Setting.Instance.GetIntSetting("check_waybill_tm");
            float timer = 0;
            if (_firstExamine)
            {
                timer = 0;
                _firstExamine = false;
            }
            else if (timer < check_tm)
            {
                CoreTimer.SetCountDownWithTimer(timer, check_tm);
                SetStateText("检查路单中");
                timer += _frame_rate;
            }
            _firstExamine = true;
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
        }

        public void NewSession()
        {
            SessionIndex++;
            RoundIndex = -1;

            try
            {
                CurrentSession = LocalSessions[SessionIndex - LocalSessionIndex];
            }
            catch (ArgumentOutOfRangeException e)
            {
                CurrentSession = new Session(SessionIndex);
                //LocalSessions.Insert(SessionIndex, newSession);
                //LocalSessions.Add(newSession);
            }
            finally
            {
                Manager.ResetWaybill();
            }
        }
        #region 开牌
        private void CalcEarning()
        {
            Waybill[RoundIndex].Winner = (int)CurrentRound.Winner.Item1;
            NoticeRoundOver();

            Manager.SavePlayerScoresAndBetRecords();

            WebServer.SendDealCommandToHttpServer();

            Desk.Instance.CalcAllPlayersEarning(CurrentRound);
            SetWinStateText();

            Desk.Instance.ClearDeskAmount();
        }
        private void SetWinStateText()
        {
            var winner = CurrentRound.Winner;
            var window = MainWindow.Instance;
            string b_str, p_str;
            switch (winner.Item1)
            {
                case BetSide.banker:
                    b_str = "庄 " + (winner.Item2) + " 点";
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
                    p_str = "闲 " + (winner.Item3) + " 点";
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
        #endregion
        void SetStateText(string state)
        {
            StateText = state;
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
                arr[i] = p_cards[i].GetPngName;
            }
            var b_cards = CurrentRound.HandCard[1];
            for (int i = 3; i < 5; i++)
            {
                arr[i] = b_cards[i - 3].GetPngName;
            }
            arr[2] = p_cards.Count == 3 ? p_cards[2].GetPngName : 0;
            arr[5] = b_cards.Count == 3 ? b_cards[2].GetPngName : 0;
            return arr;
        }

        #region 状态机
        private WsStateMachine _fsm;

        private WsState _preparingState;  //点击开始按钮后
        private WsState _shufflingState;    //洗牌中
        private WsState _bettingState;  //押注中
        private WsState _dealingState;  //开牌中
        private WsState _examineState;
        private WsTransition _prepareShuffle;
        private WsTransition _shuffleBet;
        private WsTransition _betDeal;
        private WsTransition _dealNextBet;
        private WsTransition _dealExamine;   //开牌后重新洗牌,洗牌前先对单
        private WsTransition _examineShuffle;

        private bool _isShuffled = false;
        //private bool _betFinished = false;
        private bool _betRoundOver = false;
        private bool _sessionOver = false;

        private static Game instance;
        private Setting _setting = Setting.Instance;
        public bool _isGameStarting = false;
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
        #endregion
    }
}
