using System;
using System.Collections.Generic;
using WsUtils;
using WsFSM;
using System.Timers;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Windows.Threading;

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
    public class Game
    {
        #region 游戏参数
        public const float _frame_rate = 0.2f;
        public const float _bet_rate = 0.4f;
        public bool _is3SecOn = false;
        public bool _isBetting = false;
        public bool _isIn3 = false;
        public double animationTime = 3;
        #endregion
        #region 游戏对象
        public DispatcherTimer dTimer;
        public int Counter { get; set; }
        public List<List<int>> keymap = new List<List<int>>();
        public KeyDownModel[] KeyMap = new KeyDownModel[300];
        public NetServer scr_transfer;
        public event delegateGameStart NoticeGameStart;
        public event delegateRoundOver NoticeRoundOver;
        public event delegateDealCard NoticeDealCard;
        #endregion
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

        public ObservableCollection<Session> AllSessions { get; private set; }
        public int SessionIndex { get; private set; }

        private Session _curSession;
        #region 游戏开始
        private Game()
        {

        }
        public void Start()
        {
            InitFsm();

            dTimer =dTimer?? new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_frame_rate)
            };
            dTimer.Tick += Update;

            dTimer.Start();

            //thread = new Thread(ScanAndHandleKeyMap);
            //thread.IsBackground = true;
            //thread.Start();

            scr_transfer = new NetServer();
            scr_transfer.StartServer();
        }

        private void ScanAndHandleKeyMap()
        {
            while (true)
            {
                if (_isBetting)
                {
                    foreach (var key in _all_userkeys_lst)
                    {
                        var keymodel = KeyMap[key];
                        if (keymodel.Pressed)
                        {
                            if (keymodel.Timer < _bet_rate)
                            {
                                keymodel.Timer += _frame_rate;
                            }
                            else
                            {
                                keymodel.Handler();
                                keymodel.Timer = 0;
                            }
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(_frame_rate));
            }
        }
        public void HandleKeyMap()
        {
            while (true)
            {
                if (_isBetting)
                {
                    foreach (var key in _all_userkeys_lst)
                    {
                        var keymodel = KeyMap[key];
                        if (keymodel.Pressed)
                        {
                            if (keymodel.Timer < _bet_rate)
                            {
                                keymodel.Timer += _frame_rate;
                            }
                            else
                            {
                                keymodel.Handler();
                                keymodel.Timer = 0;
                            }
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(_frame_rate));
            }
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
        private void Update(object sender, EventArgs e)
        {
            if (_isGameInited)
            {
                AddKeyTimer();
            }
            _fsm.UpdateCallback(_frame_rate);
        }
        private void InitFsm()
        {
            //资源初始化
            _preparingState = new WsState("preparing");
            //_preparingState.OnUpdate += StartGame;
            //洗牌中
            _shufflingState = new WsState("shuffling");
            _shufflingState.OnEnter += GetShufTime;
            _shufflingState.OnUpdate += Shuffle;
            _shufflingState.OnExit += ShuffleOver;
            //押注中
            _bettingState = new WsState("betting");
            _bettingState.OnEnter += Start1Round;
            _bettingState.OnUpdate += Bet;
            _bettingState.OnExit += BetOver;
            //开牌中
            _dealingState = new WsState("dealing");
            _dealingState.OnEnter += GetWinner;
            _dealingState.OnUpdate += StartDealing;
            _dealingState.OnExit += RoundOrSessionOver;

            //初始化切换到洗牌
            _prepareShuffle = new WsTransition("preShuffle", _preparingState, _shufflingState);
            _prepareShuffle.OnTransition += InitGame;
            _prepareShuffle.OnCheck += IsGameStarted;
            _preparingState.AddTransition(_prepareShuffle);

            //洗牌切换到押注
            _shuffleBet = new WsTransition("shuffleBet", _shufflingState, _bettingState);
            _shuffleBet.OnCheck += IsShuffled;
            _shufflingState.AddTransition(_shuffleBet);

            //押注切换到开牌
            _betDeal = new WsTransition("betDeal", _bettingState, _dealingState);
            _betDeal.OnCheck += IsBetted;
            _bettingState.AddTransition(_betDeal);

            //开牌后切换到下一局
            _dealNextBet = new WsTransition("dealNextBet", _dealingState, _bettingState);
            _dealNextBet.OnCheck += CanGoNextBet;

            //开牌后本场结束，验单，重新洗牌,切换到下一场
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
            //_fsm.AddState(_accountingState);
        }

        
        private void StartGame(IState state)
        {
            _isGameStart = true;
        }
        bool InitGame()
        {
            if (_isGameInited)
            {
                return true;
            }
            else
            {
                if (_isGameIniting)
                {
                    return false;
                }
                else
                {
                    _isGameIniting = true;
                    Desk.Instance.InitDeskForGame();
                    NoticeGameStart();

                    AllSessions = new ObservableCollection<Session>();
                    _betKeyDownList = new List<KeyDownModel>();

                    //押注时间有几秒
                    _betTime = _setting.GetIntSetting("bet_tm");
                    _betTime = 3;
                    _is3SecOn = _setting.GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;

                    //提前几秒通知服务器押注结束
                    _send_to_svr = 5;
                    InitKeymap();

                    SessionIndex = 0;
                    NewSession();
                    _isGameInited = true;
                    _isGameIniting = false;
                    return true;
                }
            }
        }
        public void NewSession()
        {
            var id = SessionIndex;
            Session _curSession;
            if (!_isRoundsFromBack)
            {
                _curSession = new Session(id);
                AllSessions.Add(_curSession);
            }
            else
            {
                _curSession = new Session(id);
            }

            Desk.Instance.ResetWaybill();
            Desk.Instance.RoundIndex = 1;
        }
        
        /// <summary>
        /// 初始化键盘映射
        /// </summary>
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
                        //var x = Convert.ToInt32(arr[0].Substring(arr[0].Length - 1, 1));

                        var keycode = Convert.ToInt32(arr[1]);
                        var na = new KeyDownModel(keycode)
                        {
                            IsKey = true,
                            Pressed = false,
                            Handler = SetKeyAction(func,p)
                        };
                        KeyMap[keycode] = na;
                        func++;
                        _all_userkeys_lst.Add(keycode);
                        //keys.Add(keycode);
                    }
                }
                p++;
                //keymap.Add(keys);
            }

            //_bet_userkeys_lst = new List<int>();
            //_bet_userkeys_map = new List<List<int>>();
            //_opt_userkeys_map = new List<List<int>>();
            //foreach (var map in keymap)
            //{
            //    _bet_userkeys_lst.AddRange(map.GetRange(0, 3));

            //    _bet_userkeys_map.Add(map.GetRange(0, 3));
            //    _opt_userkeys_map.Add(map.GetRange(3, 3));
            //}

        }
        Action SetKeyAction(int func_id,int p_id)
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
        private bool IsGameStarted()
        {
            return _isGameStart = true;
        }
        #endregion
        #region 键盘回调
        /// <summary>
        /// 松开按键回调
        /// </summary>
        /// <param name="key"></param>
        internal void HandleKeyUp(int key)
        {
            //if (_continuously_bet)
            //{
            //    _betKeyDownList.RemoveAll(k => k.keycode == key);
            //}
        }
        /// <summary>
        /// 按下按键回调
        /// </summary>
        /// <param name="key"></param>
        internal void HandleKeyDown(int key)
        {
            //HandleBetKey(key);

            //if (_all_userkeys_lst.Contains(key))
            //{
            //    if (_bet_userkeys_lst.Contains(key))
            //    {
            //        if (_continuously_bet) //如果不是连续押分，直接执行
            //        {
            //            _betKeyDownList.Add(new KeyDownModel(key));
            //        }
            //        //连续押分要缓存按键
            //        HandleBetKey(key);
            //    }
            //    else
            //    {
            //        HandleOptionKey(key);
            //    }
            //}
        }
        /// <summary>
        /// 连续押注按键处理，在每一帧执行
        /// </summary>
        private void HandleBetKeyList()
        {
            foreach (var keydown in _betKeyDownList)
            {
                //第一次按键 或 在_bet_rate时间内未松开
                if (keydown.Timer == 0 || keydown.Timer >= _bet_rate)
                {
                    HandleBetKey(keydown.keycode);
                    keydown.Timer = 0;
                }
                keydown.Timer += _frame_rate;
            }
        }
        /// <summary>
        /// 玩家押注
        /// </summary>
        /// <param name="key"></param>
        private void HandleBetKey(int key)
        {
            var p_idx = GetBetUser(key, out KeyFunc func);
            var player = Desk.Instance.Players[p_idx];

                player.Bet((BetSide)func);
        }
        /// <summary>
        /// 玩家配置
        /// </summary>
        /// <param name="key"></param>
        private void HandleOptionKey(int key)
        {
            var p_idx = GetOptUser(key, out KeyFunc func);
            var player = Desk.Instance.Players[p_idx];
            switch (func)
            {
                //设置押注面额
                case KeyFunc.toggle_denomination:
                    {
                        player.SetDenomination();
                        break;
                    }
                //取消所有押注
                case KeyFunc.cancle_bet:
                    {
                            player.CancleBet();
                        break;
                    }
                //是否隐藏压了哪家
                case KeyFunc.hide_bet:
                    {
                        player.SetHide();
                        break;
                    }
            }
        }
        /// <summary>
        /// 获取按下押注键的玩家
        /// </summary>
        /// <param name="key"></param>
        /// <returns>玩家索引</returns>
        int GetBetUser(int key, out KeyFunc func)
        {
            func = KeyFunc.bet_bank;
            int p_idx = 0, f_idx = 0;
            for (int i = 0; i < _bet_userkeys_map.Count; i++)
            {
                var map = _bet_userkeys_map[i];
                f_idx = map.IndexOf(key);
                if (f_idx > -1)
                {
                    func = (KeyFunc)f_idx;
                    return p_idx;
                }
                p_idx++;
            }
            return -1;
        }
        /// <summary>
        /// 获取按下配置键的玩家索引
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        int GetOptUser(int key, out KeyFunc func)
        {
            func = KeyFunc.bet_bank;
            var f_idx = 0;
            int p_idx = 0;
            for (int i = 0; i < _opt_userkeys_map.Count; i++)
            {
                var map = _opt_userkeys_map[i];
                f_idx = map.IndexOf(key);
                if (f_idx > -1)
                {
                    func = (KeyFunc)(f_idx + 3);
                    return p_idx;
                }
                p_idx++;
            }
            return -1;
        }
        #endregion

        private void GetShufTime(IState state)
        {
            _shufTime = 4;
        }
        /// <summary>
        /// 洗牌中
        /// </summary>
        /// <param name="f"></param>
        private void Shuffle(float f)
        {
            var timer = _shufflingState.Timer;
            if (timer > _shufTime + 1)
            {
                _isShuffled = true;
                return;
            }
            SetCountDownWithFloat(timer, _shufTime);
            SetStateText("洗牌中");
        }
        /// <summary>
        /// 是否洗完牌
        /// </summary>
        /// <returns></returns>
        private bool IsShuffled()
        {
            return _isShuffled;
        }
        /// <summary>
        /// 洗牌结束重置标志
        /// </summary>
        /// <param name="state"></param>
        private void ShuffleOver(IState state)
        {
            _isShuffled = false;
        }
        /// <summary>
        /// 开始新一局
        /// </summary>
        /// <param name="state"></param>
        private void Start1Round(IState state)
        {
            _isBetting = true;
        }
        /// <summary>
        /// 押注中
        /// </summary>
        /// <param name="f"></param>
        private void Bet(float f)
        {
            HandleBetKeyList();
            _betTimer = _bettingState.Timer;
            SetCountDownWithFloat(_betTimer, _betTime + 1);
            SetStateText("押注中");
            SendToServer(_bettingState, Desk.Instance.CountDown);

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
        /// <summary>
        /// 向服务器发送同步时间消息
        /// </summary>
        /// <param name="bettingState"></param>
        /// <param name="countDown"></param>
        private void SendToServer(WsState bettingState, int countDown)
        {
        }
        /// <summary>
        /// 是否押注结束
        /// </summary>
        /// <returns></returns>
        private bool IsBetted()
        {
            return _betFinished;
        }
        /// <summary>
        /// 押注结束重置标志
        /// </summary>
        /// <param name="state"></param>
        private void BetOver(IState state)
        {
           
            _isBetting = false;
            _betFinished = false;
            _isIn3 = false;
        }
        /// <summary>
        /// 开牌前获得赢家
        /// </summary>
        /// <param name="state"></param>
        private void GetWinner(IState state)
        {
            //var winner = Desk.Instance.GetWinner(_curHandCards);
            var r_idx = Desk.Instance.RoundIndex - 1;
            var cur_round = AllSessions[SessionIndex].AllRounds[r_idx];

            Desk.Instance._curHandCards = cur_round.hand_card;
            Desk.Instance.GetWinner(Desk.Instance._curHandCards);

            
        }
        /// <summary>
        /// 切换到开牌时播放开牌动画，不依赖计时器
        /// 动画结束后直接结算
        /// 然后判断牌局状态
        /// </summary>
        /// <param name="state"></param>
        private void StartDealing(float f)
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
                if (_aniTimer < 3)
                {
                    SetWinStateText();
                    _aniTimer += f;
                    return;
                }
                var num = AllSessions[SessionIndex].AllRounds.Count;
                if (Desk.Instance.RoundIndex >= num)
                {
                    _sessionOver = true;
                }
                else
                {
                    _sessionOver = false;
                    _betRoundOver = true;
                }
                return;
            }
        }

        private void SetWinStateText()
        {
            var r_idx = Desk.Instance.RoundIndex - 1;
            var cur_round = AllSessions[SessionIndex].AllRounds[r_idx];

            var winner = Desk.Instance.CurRoundWinner;
            var str =GetWinStr(winner);
            SetStateText(str);
        }
        private string GetWinStr(Tuple<BetSide,int> winner)
        {
            switch (winner.Item1)
            {
                case BetSide.banker:
                    return "庄赢 " + winner.Item2 + " 点";
                case BetSide.tie:
                    return "和";
                case BetSide.player:
                    return "闲赢 " + winner.Item2 + " 点";
                default:
                    return "";
            }
        }

        /// <summary>
        /// 是否直接开下一局
        /// </summary>
        /// <returns></returns>
        private bool CanGoNextBet()
        {
            return _betRoundOver && !_sessionOver;
        }
        /// <summary>
        /// 是否本场全部结束
        /// </summary>
        /// <returns></returns>
        private bool IsSessionOver()
        {
            return _sessionOver;
        }
        /// <summary>
        /// 离开状态时重置变量
        /// </summary>
        /// <param name="state"></param>
        private void RoundOrSessionOver(IState state)
        {
            NoticeRoundOver();
            Desk.Instance.CalcAllPlayersEarning();
            Desk.Instance.ClearDeskAmount();
            if (_sessionOver)
            {
                Desk.Instance.RoundIndex = 1;
                _sessionOver = false;
            }
            Desk.Instance.RoundIndex++;
            _betRoundOver = false;
            _firstAnimation = true;
            _animating = false;
        }
        /// <summary>
        /// 切换状态到检查路单，此时趁机保存数据等耗时操作
        /// </summary>
        /// <returns></returns>
        private bool ExamineWaybill()
        {
            SetStateText("检查路单中");
            SessionIndex++;
            NewSession();
            return true;
        }
       
        #region 开牌动画
        private void DealCard()
        {
            SetClockText("0");
            SetStateText("开牌动画中");

            NoticeDealCard();
        }
        #endregion
        #region 倒计时 
        void SetClockText(string time)
        {
            Desk.Instance.CountDown = Convert.ToInt32(time);
        }
        void SetCountDownWithFloat(float cur_timer, int seconds)
        {
            var count_down = seconds - Math.Floor(cur_timer);
            count_down = count_down < 0 ? 0 : count_down;
            SetClockText(count_down.ToString());
        }
        void SetStateText(string state)
        {
            Desk.Instance.StateText = state;
        }
        #endregion
        void FullScreen()
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
        private WsTransition _dealBet;   //开牌后直接开下一局
        private WsTransition _dealShuffle;   //开牌后重新洗牌,洗牌前先对单

        private bool _isGameStart = false;
        private bool _isShuffled = false;
        private bool _betFinished = false;
        private bool _enableBet = false;
        private bool _isAccounted = false;
        private bool _betRoundOver = false;
        private bool _sessionOver = false;
        public bool _animating = false;
        private bool _animationDone = false;

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
        private bool _isRoundsFromBack = false;
        private bool _firstAnimation= true;
        public float _aniTimer = 0;
        private bool _isGameInited = false;
        private bool _isGameIniting;
        #endregion
    }
    public enum KeyFunc
    {
        bet_bank = 0,
        bet_player,
        bet_tie,
        toggle_denomination,
        cancle_bet,
        hide_bet
    }
    public class KeyDownModel
    {
        public int keycode { get; set; }
        public float Timer { get; set; }
        public Action Handler { get; set; }
        public bool Pressed { get; internal set; }
        public bool IsKey{ get; internal set; }

        public KeyDownModel(int key)
        {
            IsKey = false;
            keycode = key;
            Timer = 0;
        }
    }
}
