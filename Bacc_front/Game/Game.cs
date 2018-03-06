using System;
using System.Collections.Generic;
using WsUtils;
using WsFSM;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;

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
        public bool _isBetting = false;
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
            _betTime = 1;
            _shufTime = 10;
            _is3SecOn = _setting.GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;
            _roundNumPerSession = _setting.GetIntSetting("round_num_per_session");
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
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.player));
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
                }
                else
                {
                    keymodel.Timer = 0;
                }
            }
        }

        private void InitFsm()
        {
            //直接开始
            _preparingState = new WsState("preparing");
            //洗牌中
            _shufflingState = new WsState("shuffling");
            _shufflingState.OnEnter += StartNewSession;
            _shufflingState.OnUpdate += Shufflling;
            _shufflingState.OnExit += ShuffleOver;
            //押注中
            _bettingState = new WsState("betting");
            _bettingState.OnEnter += Start1Round;
            _bettingState.OnUpdate += Betting;
            _bettingState.OnExit += BetOver;
            //开牌中
            _dealingState = new WsState("dealing");
            _dealingState.OnUpdate += Dealing;
            _dealingState.OnExit += RoundOrSessionOver;

            //初始化切换到洗牌
            _prepareShuffle = new WsTransition("preShuffle", _preparingState, _shufflingState);
            _prepareShuffle.OnCheck += () => { return true; };
            _preparingState.AddTransition(_prepareShuffle);

            //洗牌切换到押注
            _shuffleBet = new WsTransition("shuffleBet", _shufflingState, _bettingState);
            _shuffleBet.OnCheck += IsShuffled;
            _shufflingState.AddTransition(_shuffleBet);

            //押注切换到开牌
            _betDeal = new WsTransition("betDeal", _bettingState, _dealingState);
            _betDeal.OnCheck += IsBetted;
            _bettingState.AddTransition(_betDeal);

            //开牌后切换到下一轮
            _dealNextBet = new WsTransition("dealNextBet", _dealingState, _bettingState);
            _dealNextBet.OnCheck += CanGoNextBet;

            //开牌后本局结束，验单，重新洗牌,切换到下一局
            _dealShuffle = new WsTransition("dealShuffle", _dealingState, _shufflingState);
            _dealShuffle.OnCheck += IsSessionOver;
            _dealShuffle.OnTransition += ExamineWaybill;

            _dealingState.AddTransition(_dealNextBet);
            _dealingState.AddTransition(_dealShuffle);

            _fsm = new WsStateMachine("baccarat", _preparingState);
            _fsm.AddState(_preparingState);
            _fsm.AddState(_shufflingState);
            _fsm.AddState(_bettingState);
            _fsm.AddState(_dealingState);
        }

        #endregion

        #region 状态机运行
        private void StartNewSession(IState state)
        {
        }
        private void Shufflling(float f)
        {
            var timer = _shufflingState.Timer;
            SetCountDownWithFloat(timer, _shufTime + 1);
            SetStateText("洗牌中");

            if (timer > _shufTime + 1)
            {
                _isShuffled = true;
                return;
            }
        }
        private bool IsShuffled()
        {
            return _isShuffled;
        }
        private void ShuffleOver(IState state)
        {
            _isShuffled = false;
        }

        private void Start1Round(IState state)
        {
            RoundIndex++;
            _isBetting = true;
        }
        private void Betting(float f)
        {
            _betTimer = _bettingState.Timer;

            SetCountDownWithFloat(_betTimer, _betTime + 1);
            SetStateText("押注中");
            SendToServer(_bettingState, CountDown);

            if (_betTimer > _betTime + 1)
            {
                _betFinished = true;
                return;
            }
            if (_is3SecOn)
            {
                if (_betTimer >= _betTime - 3)
                {
                    _isIn3 = true;
                    return;
                }
            }
        }
        private bool IsBetted()
        {
            return _betFinished;
        }
        private void BetOver(IState state)
        {
            _isBetting = false;
            _betFinished = false;
            _isIn3 = false;
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
                if (_aniTimer < 0.2)
                {
                    SetWinStateText();
                    _aniTimer += f;
                    return;
                }
                var round_num = LocalSessions[SessionIndex].RoundNumber;
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
        private bool IsSessionOver()
        {
            return _sessionOver;
        }
        private void RoundOrSessionOver(IState state)
        {
            Waybill[RoundIndex].Winner = (int)CurrentRound.Winner.Item1;
            NoticeRoundOver();

            Desk.Instance.CalcAllPlayersEarning(CurrentRound);
            Desk.Instance.ClearDeskAmount();

            _betRoundOver = false;
            _firstAnimation = true;
            _animating = false;
            BankerStateText = "庄";
            PlayerStateText = "闲";

            if (_sessionOver)
            {
                _sessionOver = false;
                NewSession();
            }
            
        }

        private bool ExamineWaybill()
        {
            //var timer = _dealingState.Timer;
            SetStateText("检查路单中");
            return true;
        }
        #endregion

        #region 开牌动画
        private void DealCard()
        {
            SetClockText("0");
            SetStateText("开牌中");

            NoticeDealCard();
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
                    break;
                case BetSide.tie:
                    str = "和";
                    BankerStateText = "和牌退注";
                    PlayerStateText = "和牌退注";
                    break;
                case BetSide.player:
                    str = "闲赢 " + (winner.Item3 - winner.Item2) + " 点";
                    PlayerStateText = str;
                    break;
                default:
                    str = "";
                    break;
            }
        }
        #endregion
        #region 倒计时 
        void SetClockText(string time)
        {
            CountDown = Convert.ToInt32(time);
        }
        void SetCountDownWithFloat(float cur_timer, int seconds)
        {
            var count_down = seconds - Math.Floor(cur_timer);
            count_down = count_down < 0 ? 0 : count_down;
            SetClockText(count_down.ToString());
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
        #region 状态机
        private WsStateMachine _fsm;

        private WsState _preparingState;  //点击开始按钮后
        private WsState _shufflingState;    //洗牌中
        private WsState _bettingState;  //押注中
        private WsState _dealingState;  //开牌中

        private WsTransition _prepareShuffle;
        private WsTransition _shuffleBet;
        private WsTransition _betDeal;
        private WsTransition _dealNextBet;
        private WsTransition _dealShuffle;   //开牌后重新洗牌,洗牌前先对单

        private bool _isShuffled = false;
        private bool _betFinished = false;
        private bool _betRoundOver = false;
        private bool _sessionOver = false;
        public bool _animating = false;

        private int _shufTime;
        private int _betTime;
        private int _send_to_svr;
        private float _betTimer;
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
            keycode = key;
            Timer = 0;
        }
    }
}
