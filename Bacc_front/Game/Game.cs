using System;
using System.Collections.Generic;
using WsUtils;
using WsFSM;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;

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
        public const float _frame_rate = 0.1f;
        public const float _bet_rate = 0.1f;
        public bool _is3SecOn = false;
        public bool _isInBetting = false;
        public bool _isIn3 = false;
        public double animationTime = 3;
        #endregion
        #region 游戏对象
        public MediaPlayer WavPlayer { get; set; }
        /// <summary>
        /// 定时器
        /// </summary>
        public DispatcherTimer dTimer;
        /// <summary>
        /// 倒计时器
        /// </summary>
        public int CountDown { get; set; }
        /// <summary>
        /// 状态显示器
        /// </summary>
        public string StateText { get; set; }
        public string BankerStateText { get; set; }
        public string PlayerStateText { get; set; }
        /// <summary>
        /// 受控指示器
        /// </summary>
        private bool IsUnderControl { get; set; }
        /// <summary>
        /// 轮次计数器
        /// </summary>
        public int RoundIndex { get; set; }
        public string _roundStrIndex { get { return (RoundIndex + 1).ToString(); } }
        public Round CurrentRound { get { return LocalSessions[SessionIndex].RoundsOfSession[RoundIndex]; } }
        /// <summary>
        /// 局数计数器
        /// </summary>
        public int SessionIndex { get; set; }
        public string _sessionStrIndex { get { return (SessionIndex + 1).ToString(); } }
        /// <summary>
        /// 本地局集合
        /// </summary>
        public ObservableCollection<Session> LocalSessions { get; private set; }
        /// <summary>
        /// 当前局路单
        /// </summary>
        public ObservableCollection<WhoWin> Waybill { get; set; }
        /// <summary>
        /// 键盘映射表
        /// </summary>
        public KeyDownModel[] KeyMap = new KeyDownModel[300];
        /// <summary>
        /// 通知窗口游戏开始事件，进行路单绑定
        /// </summary>
        public event delegateGameStart NoticeWindowBind;
        /// <summary>
        /// 通知窗口本局结束事件，重置路单
        /// </summary>
        public event delegateRoundOver NoticeRoundOver;
        /// <summary>
        /// 通知窗口开始发牌动画事件
        /// </summary>
        public event delegateDealCard NoticeDealCard;
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

        #endregion
        public Game()
        {
            LocalSessions = new ObservableCollection<Session>();
        }

        #region 游戏开始
        public void Start()
        {
            InitGame();
            dTimer = dTimer ?? new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_frame_rate)
            };
            dTimer.Tick += Update;

            dTimer.Start();

            InitFsm();
        }
        void InitGame()
        {
            BankerStateText = "庄";
            PlayerStateText = "闲";
            _betTime = _setting.GetIntSetting("bet_tm");    //押注时间有几秒
            _betTime = 6;
            _shufTime = 4;
            _is3SecOn = _setting.GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;
            _roundNumPerSession = _setting.GetIntSetting("round_num_per_session");
            _roundNumPerSession = 3;
            _send_to_svr = 5;   //提前几秒通知服务器押注结束
            InitKeymap();

            WavPlayer = new MediaPlayer();

            SessionIndex = -1;

            NewSession();
            NoticeWindowBind();
            _isGameInited = true;
        }

        void InitKeymap()
        {
            var content = FileUtils.getInstance().readStreamingFile("keymap.txt");
            var lines = content.Split('\n');

            for (int i = 0; i < KeyMap.Length; i++)
            {
                KeyMap[i] = new KeyDownModel(i);
            }

            _all_userkeys_lst = new List<int>();
            int p = 0;
            for (int i = 0; i < lines.Length - 1; i += 8)
            {
                List<int> keys = new List<int>();
                int func = 0;
                for (int j = 0; j < 8; j++)
                {
                    var arr = lines[i + j].Split('=');
                    if (0 < j && j < 7 && arr.Length == 2)
                    {
                        var keycode = Convert.ToInt32(arr[1]);

                        var na = new KeyDownModel(keycode)
                        {
                            IsKey = true,
                            Pressed = false,
                            Handler = SetKeyAction(func, p)
                        };
                        KeyMap[keycode] = na;

                        _all_userkeys_lst.Add(keycode);

                        func++;
                    }
                }
                p++;
            }
        }
        Action SetKeyAction(int func_id, int p_id)
        {
            Action action;
            switch (func_id)
            {
                case 0:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.banker));
                    break;
                case 1:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.tie));
                    break;
                case 2:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.player));
                    break;
                case 3:
                    action = new Action(() => Desk.Instance.Players[p_id].SetDenomination());
                    break;
                case 4:
                    action = new Action(() => Desk.Instance.Players[p_id].CancleBet());
                    break;
                case 5:
                    action = new Action(() => Desk.Instance.Players[p_id].SetHide());
                    break;
                default:
                    action = new Action(() => { });
                    break;
            }
            return action;
        }

        public void NewSession()
        {
            SessionIndex++;
            RoundIndex = -1;

            Session newSession;
            try
            {
                newSession = LocalSessions[SessionIndex];
            }
            catch (ArgumentOutOfRangeException e)
            {
                newSession = new Session(SessionIndex);
                LocalSessions.Add(newSession);
            }
            finally
            {
                ResetWaybill();
            }
        }
        public void ResetWaybill()
        {
            var rounds = LocalSessions[SessionIndex].RoundsOfSession;
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

        private void Update(object sender, EventArgs e)
        {
            if (_isGameInited)
            {
                AddKeyTimer();
            }
            _fsm.UpdateCallback(_frame_rate);
        }
        private void AddKeyTimer()
        {
            foreach (var key in _all_userkeys_lst)
            {
                var keymodel = KeyMap[key];
                if (keymodel.Pressed)
                {
                    keymodel.Timer += _frame_rate;
                    var rate = Setting.Instance.GetIntSetting("bet_rate");
                    if (keymodel.Timer >= rate / 1000)
                    {
                        keymodel.Handler();
                        var space = Desk.Instance.CancleSpace();
                        Instance.BankerStateText = "庄押" + space[0] + "分可撤注";
                        Instance.PlayerStateText = "闲押" + space[1] + "分可撤注";
                    }
                }
                else
                {
                    keymodel.Timer = 0;
                }

            }
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
            _dealExamine.OnTransition += ExamineWaybill;
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

        }
        private void OnShufflling(float f)
        {
            var timer = _shufflingState.Timer;

            SetCountDownWithFloat(timer, _shufTime);
            SetStateText("洗牌中");

            if (!_place1Played && IsTimerInCountdown(timer, 1, _shufTime))
            {
                PlayWav("place1-");
                _place1Played = true;
            }
            if (IsStateTimeOver(timer, _shufTime))
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
            _place1Played = false;
            _isShuffled = false;
        }


        private void OnBetEnter(IState state)
        {
            RoundIndex++;
            SendToServer(_bettingState, CountDown);
            _isInBetting = true;
        }
        private void Betting(float f)
        {
            var timer = _bettingState.Timer;
            SetCountDownWithFloat(timer, _betTime);
            SetStateText("押注中");

            if (IsStateTimeOver(timer, _betTime))
            {
                _isInBetting = false;
                return;
            }
            if (!_stopBetPlayed && IsTimerInCountdown(timer, 1, _betTime))
            {
                PlayWav("StopBet");
                _stopBetPlayed = true;
            }
            if (_is3SecOn)
            {
                if (IsTimerInCountdown(timer, 3, _betTime))
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

        }
        private void Dealing(float f)
        {
            if (_firstAnimation)
            {
                _firstAnimation = false;
                _animating = true;
                DealCard();
                return;
            }
            if (_animating)
            {
                return;
            }
            else
            {
                if (_aniTimer < 5)
                {
                    SetWinStateText();
                    _aniTimer += f;
                    return;
                }
                //var round_num = LocalSessions[SessionIndex].RoundNumber;
                var round_num = 4;
                if (RoundIndex + 1 >= round_num)
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
            float timer = 0;
            if (!_place1Played)
            {
                PlayWav("place1-");
                _place1Played = true;
            }
            if (timer < 2)
            {
                timer += _frame_rate;
                return false;
            }
            return true;
        }
        private bool IsSessionOver()
        {
            return _sessionOver;
        }
        private void OnDealExit(IState state)
        {
            Waybill[RoundIndex].Winner = (int)CurrentRound.Winner.Item1;
            NoticeRoundOver();

            Desk.Instance.CalcAllPlayersEarning(CurrentRound);
            Desk.Instance.ClearDeskAmount();

            _betRoundOver = false;
            _firstAnimation = true;
            _animating = false;
            _winbPlayed = false;
            _winpPlayed = false;
            _winTPlayed = false;

            BankerStateText = "庄";
            PlayerStateText = "闲";

            if (_sessionOver)
            {
                _sessionOver = false;
                NewSession();
            }

        }
        private bool IsExamineOver()
        {
            return true;
        }

        private void OnExamineExit(IState state)
        {
        }

        private void OnExamining(float f)
        {
        }

        private void OnExamineEnter(IState state)
        {
        }




        private bool ExamineWaybill()
        {
            var check_tm = Setting.Instance.GetIntSetting("check_waybill_tm");
            float timer = 0;
            if (_firstExamine)
            {
                timer = 0;
                _firstExamine = false;
                return false;
            }
            else if (timer < check_tm)
            {
                SetCountDownWithFloat(timer, check_tm);
                SetStateText("检查路单中");
                timer += _frame_rate;
                return false;
            }
            _firstExamine = true;
            return true;
        }
        #endregion

        #region 开牌动画
        private void DealCard()
        {
            var timer = _dealingState.Timer;
            SetStateText("开牌中");

            DoOnceInTimespan(timer, ref _isPinCardDone, ref _isInPinCard, 2, 2, PinCardAnimation);
            //NoticeDealCard();
        }
        
        private bool DoOnceInTimespan(float timer, ref bool isActionDone, ref bool hasStartAction, float start, float time, Action action)
        {
            if (isActionDone)
            {
                return true;
            }
            //isFuncOver = false;
            if (start <= timer)
            {
                if (!hasStartAction)
                {
                    hasStartAction = true;
                    action();
                }
                return false;
            }
            if (time >= start + time)
            {
                isActionDone = true;
            }
            return false;
        }
        private void SetWinStateText()
        {
            var winner = CurrentRound.Winner;
            string str;
            switch (winner.Item1)
            {
                case BetSide.banker:
                    str = "庄赢 " + (winner.Item2 - winner.Item3) + " 点";
                    BankerStateText = str;
                    if (!_winbPlayed)
                    {
                        PlayWav("winB");
                        _winbPlayed = true;
                    }
                    break;
                case BetSide.tie:
                    str = "和";
                    BankerStateText = "和牌退注";
                    PlayerStateText = "和牌退注";
                    if (_winTPlayed)
                    {
                        _winTPlayed = true;
                        PlayWav("winT");
                    }
                    break;
                case BetSide.player:
                    str = "闲赢 " + (winner.Item3 - winner.Item2) + " 点";
                    PlayerStateText = str;
                    if (!_winpPlayed)
                    {
                        _winbPlayed = true;
                        PlayWav("winP");
                    }
                    break;
                default:
                    str = "";
                    break;
            }
        }
        #endregion
        #region 倒计时 
        bool IsTimerInCountdown(float timer, int countdown, int totaltime)
        {
            if (totaltime - timer <= countdown)
            {
                return true;
            }
            return false;
        }
        bool IsStateTimeOver(float timer, int totaltime)
        {
            if (totaltime - timer <= -1)
                return true;
            return false;
        }
        void SetCountDownWithFloat(float cur_timer, int seconds)
        {
            var count_down = seconds - Math.Floor(cur_timer);
            count_down = count_down < 0 ? 0 : count_down;
            CountDown = Convert.ToInt32(count_down);
        }
        void SetStateText(string state)
        {
            StateText = state;
        }
        #endregion
        #region 音乐播放器
        private void PlayWav(string name)
        {
            WavPlayer.Open(new Uri("Wav/" + name + ".wav", UriKind.Relative));
            WavPlayer.Play();
        }
        #endregion
        void FullScreen()
        {

        }
        private void SendToServer(WsState bettingState, int countDown)
        {
        }

        private WsTransition _examineShuffle;
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

        private bool _isShuffled = false;
        //private bool _betFinished = false;
        private bool _betRoundOver = false;
        private bool _sessionOver = false;
        public bool _animating = false;

        private int _shufTime;
        private int _betTime;
        private int _send_to_svr;
        private int _roundNumPerSession;

        private List<int> _all_userkeys_lst;    //所有要监听的按键,查询用
        private List<int> _bet_userkeys_lst;     //用于押注的按键，查询用
        private List<List<int>> _bet_userkeys_map;
        private List<KeyDownModel> _betKeyDownList; //按下且尚未抬起的押注按键，每一帧轮询
        private List<List<int>> _opt_userkeys_map;  //用于暗注、修改额度或者取消押注的按键
        private Thread thread;
        private static Game instance;
        private Setting _setting = Setting.Instance;
        private bool _firstAnimation = true;
        public float _aniTimer = 0;
        private bool _isGameInited = false;
        private bool _isGameIniting;
        private bool _firstExamine = true;
        private bool _stopBetPlayed = false;
        private bool _place1Played = false;
        private bool _winbPlayed = false;
        private bool _winpPlayed = false;
        private bool _winTPlayed = false;
        private bool _isInPinCard = false;
        private bool _isPinCardDone = false;
        #endregion
    }
    public class KeyDownModel
    {
        public int keycode { get; set; }
        public float Timer { get; set; }
        public Action Handler { get; set; }
        public bool Pressed { get; internal set; }
        public bool IsKey { get; internal set; }

        public KeyDownModel(int key)
        {
            IsKey = false;
            Pressed = false;
            keycode = key;
            Timer = 0;
        }
    }
}
