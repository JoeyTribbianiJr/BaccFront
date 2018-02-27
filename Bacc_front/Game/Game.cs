using System;
using System.Collections.Generic;
using WsUtils;
using WsFSM;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;

namespace Bacc_front
{
    public enum XYindex
    {
        tie = 0,
        banker = 1,
        player = 2
    }
    public class Game
    {
        #region 游戏参数
        private const float rate = 0.1f;
        public bool _is3SecOn = false;
        public bool _isBetting = false;
        public bool _isIn3 = false;
        #endregion
        #region 游戏对象
        public DispatcherTimer dTimer;
        public int Counter { get; set; }
        public List<List<int>> keymap = new List<List<int>>();
        #endregion
        public Game()
        {
            InitFsm();

            dTimer = new DispatcherTimer();
            dTimer.Interval = TimeSpan.FromSeconds(rate);
            dTimer.Tick += Update;

            dTimer.Start();
        }
        private void Update(object sender, EventArgs e)
        {
            _fsm.UpdateCallback(rate);
        }
        private void InitFsm()
        {
            //资源初始化
            _preparingState = new WsState("preparing");
            //_preparingState.OnUpdate += InitGame;
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
            _dealingState.OnExit += RoundOrSessionOver;

            //初始化切换到洗牌
            _prepareShuffle = new WsTransition("preShuffle", _preparingState, _shufflingState);
            _prepareShuffle.OnTransition += InitGame;
            _prepareShuffle.OnCheck += IsGameInited;
            _preparingState.AddTransition(_prepareShuffle);

            //洗牌切换到押注
            _shuffleBet = new WsTransition("shuffleBet", _shufflingState, _bettingState);
            _shuffleBet.OnCheck += IsShuffled;
            _shufflingState.AddTransition(_shuffleBet);

            //押注切换到开牌
            _betDeal = new WsTransition("betDeal", _bettingState, _dealingState);
            _betDeal.OnCheck += IsBetted;
            _betDeal.OnTransition += StartDealing;
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
            //_fsm.AddState(_accountingState);
        }

        internal void HandleKeyUp(int key)
        {
            _betKeyDownList.RemoveAll(k => k.keycode == key);
        }

        internal void HandleKeyDown(int key)
        {
            if (_all_userkeys_lst.Contains(key))
            {
                if(_bet_userkeys_lst.Contains(key))
                {
                    _betKeyDownList.Add(new KeyDownModel(key));
                }
                else
                {
                    HandleOptionKey(key);
                }
            }
        }

        private void HandleOptionKey(int key)
        {
            throw new NotImplementedException();
        }
        #region 游戏初始化
        /// <summary>
        /// 初始化键盘映射
        /// </summary>
        void InitKeymap()
        {
            var content = FileUtils.getInstance().readStreamingFile("keymap.txt");
            var lines = content.Split('\n');

            _all_userkeys_lst = new List<int>();
            for (int i = 0; i < lines.Length - 1; i += 8)
            {
                List<int> keys = new List<int>();
                for (int j = 0; j < 8; j++)
                {
                    var arr = lines[i + j].Split('=');
                    if (0 < j && j < 7 && arr.Length == 2)
                    {
                        //var x = Convert.ToInt32(arr[0].Substring(arr[0].Length - 1, 1));
                        var keycode = Convert.ToInt32(arr[1]);
                        keys.Add(keycode);

                        _all_userkeys_lst.Add(keycode);
                    }
                }
                keymap.Add(keys);
            }

            _bet_userkeys_lst = new List<int>();
            _opt_userkeys_map = new List<List<int>>();
            foreach(var map in keymap)
            {
                _bet_userkeys_lst.AddRange(map.GetRange(0, 3));
                _opt_userkeys_map.Add(map.GetRange(2,3));
            }
            
        }
        #endregion

        bool InitGame()
        {
            //每场几局
            _roundNumPerSession = Setting.Instance.GetIntSetting("round_num_per_session");
            _roundNumPerSession = 3;

            //押注时间有几秒
            _betTime = Setting.Instance.GetIntSetting("bet_tm");
            _betTime = 10;
            _is3SecOn = Setting.Instance.GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;

            //提前几秒通知服务器押注结束
            _send_to_svr = 5;

            InitKeymap();
            _isLoaded = true;
            return true;
        }
        private bool IsGameInited()
        {
            return _isLoaded = true;
        }
        private void GetShufTime(IState state)
        {
            _shufTime = 5;
        }
        /// <summary>
        /// 洗牌中
        /// </summary>
        /// <param name="f"></param>
        private void Shuffle(float f)
        {
            var timer = _shufflingState.Timer;
            if (timer > _shufTime)
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
            Desk.Instance.RoundIndex++;
            _isBetting = true;
        }
        /// <summary>
        /// 押注中
        /// </summary>
        /// <param name="f"></param>
        private void Bet(float f)
        {
            _betTimer = _bettingState.Timer;
            SetCountDownWithFloat(_betTimer, _betTime);
            SetStateText("押注中");

            SendToServer(_bettingState, Desk.Instance.CountDown);

            if (_betTimer > _betTime)
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
        /// 切换到开牌时播放开牌动画，不依赖计时器
        /// 动画结束后直接结算
        /// 然后判断牌局状态
        /// </summary>
        /// <param name="state"></param>
        private bool StartDealing()
        {
            if (_animating)
            {
                return false;
            }
            else
            {
                _animating = true;
                DealCard();
                _animating = false;
                return true;
            }
        }
        /// <summary>
        /// 开牌后结算
        /// </summary>
        /// <param name="state"></param>
        private void GetWinner(IState state)
        {
            var winner = Desk.Instance.GetWinner(_curHandCards);
            if (Desk.Instance.RoundIndex >= _roundNumPerSession)
            {
                _sessionOver = true;
            }
            else
            {
                _sessionOver = false;
                _betRoundOver = true;
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
            if (_sessionOver)
            {
                Desk.Instance.RoundIndex = 1;
                _sessionOver = false;
            }
            _betRoundOver = false;
        }
        /// <summary>
        /// 切换状态到检查路单，此时趁机保存数据等耗时操作
        /// </summary>
        /// <returns></returns>
        private bool ExamineWaybill()
        {
            SetStateText("检查路单中");
            //SetClockText("检查路单中");
            //new WaitForSeconds(3);
            return true;
        }
        #region 输入输出操作
        private void OnGUI()
        {
            if (_enableBet)
            {

                //if (GUI.Button(new Rect(10, 50, 100, 30), "client"))
                //{
                //	ClientConnectServerAction();
                //}
                //if (GUI.Button(new Rect(10, 50, 100, 30), "add player"))
                //{
                //	ClientScene.AddPlayer(31);
                //}
            }
        }
        void HandleKey(int keycode)
        {

        }

        //#region 网络监听
        //public NetworkClient svr_client;
        //public GameObject playerPre;
        //public void ClientConnectServerAction()
        //{
        //	svr_client = new NetworkClient();
        //	//连接服务器
        //	//注册客户端连接之后的回调方法
        //	svr_client.RegisterHandler(MsgType.Connect, ClientConnectServerCallback);
        //	print("已注册回调");
        //	svr_client.Connect("127.0.0.1", 1345);
        //}
        //void ClientConnectServerCallback(NetworkMessage msg)
        //{
        //	print("已连接");
        //	//告诉服务器准备完毕
        //	ClientScene.Ready(msg.conn);
        //	//向服务器注册预设体(预设体必须是网络对象)
        //	ClientScene.RegisterPrefab(playerPre);
        //	//向服务器发送添加游戏对象的请求
        //	//服务器在接收这个请求之后会自动调用 添加游戏玩家的回调方法
        //	ClientScene.AddPlayer(31);
        //}
        //#endregion
        #endregion
        #region 开牌动画
        private void DealCard()
        {
            SetClockText("00000000000");

            SetStateText("开牌动画中");
            //var singleDouble = Setting.Instance.GetStrSetting("single_double");
            //Test
            var singleDouble = "两张牌";

            if (singleDouble == "单张牌")
            {
                _curHandCards = Desk.Instance.DealSingleCard();
                OpenSingle();
            }
            if (singleDouble == "两张牌")
            {
                _curHandCards = Desk.Instance.DealTwoCard();
                OpenTwo();
            }
        }
        private void OpenSingle()
        {
            //var playerCard = _curHandCards[1][0];
            //var v2 = _cardPosition[3].transform.position;
            //OpenCard(playerCard, v2, 0);

            //var bankerCard = _curHandCards[0][0];
            //var v1 = _cardPosition[0].transform.position;
            //OpenCard(bankerCard, v1, 1);
        }
        private void OpenTwo()
        {
            //var playerCard1 = _curHandCards[0][0];
            //var v2 = _cardPosition[3].transform.position;
            //OpenCard(playerCard1, v2, 0);

            //var bankerCard1 = _curHandCards[1][0];
            //var v1 = _cardPosition[0].transform.position;
            //OpenCard(bankerCard1, v1, 1);

            //var playerCard2 = _curHandCards[0][1];
            //var v4 = _cardPosition[4].transform.position;
            //OpenCard(playerCard2, v4, 2);

            //var bankerCard2 = _curHandCards[1][1];
            //var v3 = _cardPosition[1].transform.position;
            //OpenCard(bankerCard2, v3, 3);

            //if (_curHandCards[1].Count == 3)
            //{
            //	var thdCard = _curHandCards[1][2];
            //	var bankerCard3 = thdCard;
            //	var v5 = _cardPosition[2].transform.position;
            //	OpenCard(bankerCard3, v5, 4);
            //}

            //if (_curHandCards[0].Count == 3)
            //{
            //	var fouCard = _curHandCards[0][2];
            //	var playerCard3 = fouCard;
            //	var v6 = _cardPosition[5].transform.position;
            //	OpenCard(playerCard3, v6, 5);
            //}
        }
        //private void OpenCard(Card card, Vector3 v, int cache_i)
        //{
        //var sprite = InitCardSprite(card);
        ////var sp_obj = Instantiate(cardPrefab);
        //var sp_obj = _handCardCache[cache_i];
        //sp_obj.transform.position = _cardPosition[6].transform.position;
        //sp_obj.GetComponent<SpriteRenderer>().sprite = sprite;

        //iTween.MoveTo(sp_obj, v, 2f);
        //}
        //private Sprite InitCardSprite(Card card)
        //{
        //var weight = card.GetCardWeight;
        //var suit = card.GetCardSuit;
        //if (suit == Suits.Back)
        //{
        //	return PPTextureManage.getInstance().LoadAtlasSprite("card", "back");
        //}
        //else
        //{
        //	var index = (int)suit * 13 + (int)weight;
        //	return PPTextureManage.getInstance().LoadAtlasSprite("card", index.ToString());
        //}
        //}
        #endregion
        #region 倒计时 
        void SetClockText(string time)
        {
            Desk.Instance.CountDown = Convert.ToInt32(time);
        }
        void SetCountDownWithFloat(float cur_timer, int seconds)
        {
            var count_down = seconds - (int)cur_timer;
            //count_down = count_down < 0 ? 0 : count_down;
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

        private bool _isLoaded = false;
        private int _shufTime;
        private bool _isShuffled = false;
        private int _betTime;
        private int _send_to_svr;
        private bool _betFinished;
        private bool _enableBet = false;
        private bool _isAccounted = false;
        private float _betTimer;
        private bool _betRoundOver = false;
        //private int _curRoundIndex = 1;
        private bool _sessionOver = false;
        private int _roundNumPerSession;
        private bool _isLoading;
        private bool _animating = false;
        private bool _animationDone;
        private List<Card>[] _curHandCards;
        private List<int> _all_userkeys_lst;    //所有要监听的按键,查询用
        private List<int>_bet_userkeys_lst;     //用于押注的按键，查询用
        private List<KeyDownModel> _betKeyDownList; //按下且尚未抬起的押注按键，每一帧轮询
        private List<List<int>> _opt_userkeys_map;  //用于暗注、修改额度或者取消押注的按键
        #endregion
    }
    internal class KeyDownModel
    {
        public int keycode { get; set; }
        public float timer { get; set; }
        public KeyDownModel(int key)
        {
            keycode = key;
            timer = 0;
        }
    }
}
