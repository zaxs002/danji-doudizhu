using System.Collections;
using System.Collections.Generic;
using System.Linq;
using model;
using UnityEngine;

namespace Controller {
    public class CardController : MonoBehaviour {
        /// <summary>
        ///   <para>牌桌</para>
        /// </summary>
        public GameObject table;

        public GameObject playerLeft;
        public GameObject playerRight;
        public GameObject playerSelf;
        public Transform DiPai;
        public UIButton TipsBt;
        public UIButton PassBt;
        public UIButton PlayBt;
        public UIButton CallLandLordBt;
        public UIButton PassLandLordBt;
        public UIButton StartBt;

        public UIWidget GameButtons;
        public UIWidget MenuButtons;

        public List<UISprite> TimerList;
        public Transform PlaySelfZone;
        public Transform PlayRightZone;
        public Transform PlayLeftZone;

        private int _currentTime = 0;

        public int CurrentTime {
            get { return _currentTime; }
            set { _currentTime = value; }
        }

        private enum GameStatus {
            GameNoStart,
            GameStart,
            GameCall,
            GameGiveLandLordCard,
            Gaming
        }

        private GameStatus _gameStatus = GameStatus.GameNoStart;

        //倍数
        private int _timers = 15;

        /// <summary>
        ///   <para>底牌列表</para>
        /// </summary>
        private List<Card> cardList = new List<Card>();

        /// <summary>
        ///   <para>地主牌列表</para>
        /// </summary>
        private List<CardObject> DiPaiList = new List<CardObject>();

        /// <summary>
        ///   <para>玩家列表</para>
        /// </summary>
        private List<Player> Players = new List<Player>();

        public List<Player> PlayersList {
            get { return Players; }
            set { Players = value; }
        }

        /// <summary>
        ///   <para>玩家列表</para>
        /// </summary>
        private List<CardObject> PlayedCards = new List<CardObject>();

        /// <summary>
        ///   <para>发牌顺序</para>
        /// </summary>
        private int dealStatus = 0;

        private Vector3 _selfPos;
        private Vector3 _rightPos;
        private Vector3 _leftPos;

        private Player currentPlayer;
        private Player firstOne;
        private List<Player> calledList = new List<Player>();
        private TextController _textController;
        private Player _diZhuPlayer;
        private GameController _gameController;

        private void Start() {
            Player player1 = new Player(Player.PlayerPosition.Self);
            player1.AddPlayZone(PlaySelfZone);
            Player player2 = new Player(Player.PlayerPosition.Right);
            player2.AddPlayZone(PlayRightZone);
            Player player3 = new Player(Player.PlayerPosition.Left);
            player3.AddPlayZone(PlayLeftZone);

            Players.Add(player1);
            Players.Add(player2);
            Players.Add(player3);
            _selfPos = Players[0].GetPlayerRealPosition();
            _rightPos = Players[1].GetPlayerRealPosition();
            _leftPos = Players[2].GetPlayerRealPosition();

            UIEventListener.Get(StartBt.gameObject).onClick = StartGameButtonClick;
            UIEventListener.Get(CallLandLordBt.gameObject).onClick = CallLandLordButtonClick;
            UIEventListener.Get(PassLandLordBt.gameObject).onClick = PassLandLordButtonClick;
            UIEventListener.Get(PlayBt.gameObject).onClick = PlayButtonClick;
            UIEventListener.Get(PassBt.gameObject).onClick = PassButtonClick;
            UIEventListener.Get(TipsBt.gameObject).onClick = TipsButtonClick;

            //得到TextController
            _textController = transform.GetComponent<TextController>();
            _gameController = transform.GetComponent<GameController>();
        }

        private void Update() {
            Debug.Log("当前状态:" + _gameStatus);
            switch (_gameStatus) {
                case GameStatus.GameNoStart:
                    MenuButtons.gameObject.SetActive(true);
                    GameButtons.gameObject.SetActive(false);
                    break;
                case GameStatus.GameStart:
                    //延迟执行
                    InvokeRepeating("WaitForCardReady", 2, 1);
                    break;
                case GameStatus.GameCall:
                    PlayBt.isEnabled = false;
                    TipsBt.isEnabled = false;
                    PassBt.isEnabled = false;
                    CheckLoadLord();
                    break;
                case GameStatus.GameGiveLandLordCard:
                    _textController.HideAllTexts();
                    foreach (Player player in Players) {
                        if (player.IsLord) {
                            Debug.Log(player.Position + "是地主");
                            _diZhuPlayer = player;
                        }
                    }
                    foreach (CardObject co in DiPaiList) {
                        co.ReverseCard();
                        CardObject instantiate = InstantiateCard(co.Card, table.transform, true);
                        _diZhuPlayer.AddCard(instantiate);
                    }
                    _diZhuPlayer.SortCard(true);
                    _gameStatus = GameStatus.Gaming;
                    currentPlayer = _diZhuPlayer;
                    currentPlayer.IsTurn = true;
                    break;
                case GameStatus.Gaming:
                    Debug.Log("游戏开了");
                    Debug.Log("该" + currentPlayer.Position + "出了");
                    GameButtons.gameObject.SetActive(true);
                    MenuButtons.gameObject.SetActive(false);
                    CallLandLordBt.gameObject.SetActive(false);
                    PassLandLordBt.gameObject.SetActive(false);

                    if (currentPlayer.IsTurn && currentPlayer.Position == Player.PlayerPosition.Self) {
                        TipsBt.isEnabled = true;
                        PassBt.isEnabled = true;
                        PlayBt.isEnabled = true;
                    }

                    var playerId = Players.FindIndex(p => p.Position == currentPlayer.Position);
                    int preId1;
                    if (playerId - 1 < 0) {
                        preId1 = 2;
                    } else {
                        preId1 = playerId - 1;
                    }
                    int preId2;
                    if (preId1 - 1 < 0) {
                        preId2 = 2;
                    } else {
                        preId2 = preId1 - 1;
                    }
                    Player preplayer1 = Players[preId1];
                    Player preplayer2 = Players[preId2];

//                    //第一次出牌
//                    if (currentPlayer.ChuPaiCishu == 0 && preplayer1.ChuPaiCishu == 0 && preplayer2.ChuPaiCishu == 0) {
//                        TipsBt.isEnabled = false;
//                        PassBt.isEnabled = false;
//                        PlayBt.isEnabled = true;
//                    }
//                    //其他人都不要, 自己重出
//                    if (currentPlayer.ChuPaiCishu > 0 && preplayer1.ChuPaiCishu == -1 && preplayer2.ChuPaiCishu == -1) {
//                        TipsBt.isEnabled = true;
//                        PassBt.isEnabled = false;
//                        PlayBt.isEnabled = true;
//                    }
                    if (currentPlayer.PlayedStatus == Player.PlayerStatus.Passed
                        && preplayer1.PlayedStatus == Player.PlayerStatus.Passed
                        && preplayer2.PlayedStatus == Player.PlayerStatus.Passed) {
                        TipsBt.isEnabled = false;
                        PassBt.isEnabled = false;
                        PlayBt.isEnabled = true;
                    }
                    if (currentPlayer.PlayedStatus == Player.PlayerStatus.Played
                        && preplayer1.PlayedStatus == Player.PlayerStatus.Passed
                        && preplayer2.PlayedStatus == Player.PlayerStatus.Passed) {
                        TipsBt.isEnabled = true;
                        PassBt.isEnabled = false;
                        PlayBt.isEnabled = true;
                    }

                    if (!currentPlayer.IsTurn) {
                        Player.PlayerPosition playerPosition = currentPlayer.Position + 1;
                        int next = (int) playerPosition % Players.Count;
                        currentPlayer = Players[next];
                    } else {
                        if (_currentTime == 0) {
                            PlayCard(currentPlayer);
                        }
                    }
                    break;
            }
        }

        #region 按钮事件

        public void StartGameButtonClick(GameObject button) {
            _gameStatus = GameStatus.GameStart;
            CreateCard();
            Deal();
        }

        private void CallLandLordButtonClick(GameObject go) {
            StopAllCoroutines();
            foreach (Player player in Players) {
                if (player.Position == Player.PlayerPosition.Self) {
                    TimerList[(int) player.Position].gameObject.SetActive(false);
                    CallLandLordBt.gameObject.SetActive(false);
                    PassLandLordBt.gameObject.SetActive(false);
                    player.CallWeight += 2;
                    player.IsCalled = true;
                    _currentTime = 0;
                    calledList.Add(player);

                    _textController.ShowTextOn(TextController.TextEnum.CallLandLord, player);
                }
            }
        }

        private void PassLandLordButtonClick(GameObject go) {
            StopAllCoroutines();
            foreach (Player player in Players) {
                if (player.Position == Player.PlayerPosition.Self) {
                    Debug.Log(player.Position + " 没有叫");
                    CallLandLordBt.gameObject.SetActive(false);
                    PassLandLordBt.gameObject.SetActive(false);
                    TimerList[(int) player.Position].gameObject.SetActive(false);
                    player.CallWeight += 1;
                    _textController.ShowTextOn(TextController.TextEnum.PassLandLord, player);
                    player.IsCalled = true;
                    _currentTime = 0;
                }
            }
        }

        private void CardClick(GameObject go) {
            Debug.Log("点击了");
            List<CardObject> readyPlayCards = PlayersList[0].ReadyPlayCards;
            CardObject co = go.GetComponent<CardObject>();
            //如果要出的牌里面有选中的牌，则变成不出的牌
            if (readyPlayCards.Contains(co)) {
                readyPlayCards.Remove(co);
                co.transform.localPosition = new Vector3(
                    co.transform.localPosition.x,
                    co.transform.localPosition.y - 20,
                    co.transform.localPosition.z);
            } else {
                //如果要出的牌里面没有选中的牌，则变成要出的牌
                readyPlayCards.Add(co);
                co.transform.localPosition = new Vector3(
                    co.transform.localPosition.x,
                    co.transform.localPosition.y + 20,
                    co.transform.localPosition.z);
            }
        }

        private void PlayButtonClick(GameObject go) {
            if (currentPlayer.Position == Player.PlayerPosition.Self) {
//                Players[0].PlayCard();
//                _currentTime = 0;

                _gameController.PlayCard(currentPlayer);
            } else {
                Debug.Log("不该出牌");
            }
        }

        private void PassButtonClick(GameObject go) {
            StopAllCoroutines();

            _textController.HideText(TextController.TextEnum.NoCardOverCome, currentPlayer);
            _textController.HideText(TextController.TextEnum.NoCardRoleType, currentPlayer);

            _textController.ShowTextOn(TextController.TextEnum.Pass, currentPlayer);

            TimerList[(int) Player.PlayerPosition.Self].gameObject.SetActive(false);
            TipsBt.isEnabled = false;
            PassBt.isEnabled = false;
            PlayBt.isEnabled = false;
            currentPlayer.IsTurn = false;
            //不出,减一
//            currentPlayer.ChuPaiCishu = -1;
            currentPlayer.PlayedStatus = Player.PlayerStatus.Passed;
            _currentTime = 0;
            Players[1].IsTurn = true;
        }
        private void TipsButtonClick(GameObject go) {
            var cardObjects = _gameController.Tips(currentPlayer);
            if (cardObjects!=null && cardObjects.Count==0) {
                _textController.ShowTextOn(TextController.TextEnum.NoCardOverCome, currentPlayer);
            }
            if (cardObjects != null && cardObjects.Count > 0) {
                currentPlayer.SelectedCards.Clear();
                currentPlayer.SelectedCards.AddRange(cardObjects);
            }
        }

        #endregion

        #region GameNoStart(创建牌,发牌,排序牌)

        //生成全部牌到List
        public void CreateCard() {
            foreach (GameConst.Suit suit in GameConst.CardSuitList) {
                int i = 0;
                foreach (GameConst.CardNumber cardNumber in GameConst.CardNumberList) {
                    if (cardNumber == GameConst.CardNumber.BigJoker || cardNumber == GameConst.CardNumber.SmallJoker) {
                        continue;
                    }
                    Card card = new Card(suit, cardNumber,
                        GameConst.CardWeightList[i] is GameConst.CardWeight
                            ? (GameConst.CardWeight) GameConst.CardWeightList[i]
                            : (GameConst.CardWeight) 0);
                    i++;
                    cardList.Add(card);
                }
            }
            Card smallJoker = new Card(GameConst.Suit.None, GameConst.CardNumber.SmallJoker,
                GameConst.CardWeight.SmallJoker);
            Card bigJoker = new Card(GameConst.Suit.None, GameConst.CardNumber.BigJoker, GameConst.CardWeight.BigJoker);
            cardList.Add(smallJoker);
            cardList.Add(bigJoker);
        }


        //面向对象生成方法
        private CardObject InstantiateCard(int rdm, Transform parent) {
            GameObject cardTemp = Instantiate(Resources.Load("Prefabs/Card")) as GameObject;
            if (cardTemp != null) {
                CardObject cardObj = cardTemp.GetComponent<CardObject>();
                UIEventListener.Get(cardTemp).onClick = CardClick;
                Card card = cardList[rdm];
                cardObj.Card = card;

                cardObj.Init(parent, false);
                return cardObj;
            }
            return null;
        }


        //面向对象生成方法,根据是否地主牌做处理
        private CardObject InstantiateCard(Card c, Transform parent, bool diZhuPai) {
            GameObject cardTemp = Instantiate(Resources.Load("Prefabs/Card")) as GameObject;
            if (cardTemp != null) {
                CardObject cardObj = cardTemp.GetComponent<CardObject>();
                UIEventListener.Get(cardTemp).onClick = CardClick;
                cardObj.Card = c;

                cardObj.Init(parent, diZhuPai);
                return cardObj;
            }
            return null;
        }

        //发牌
        public void Deal() {
            dealStatus = 0;
            InvokeRepeating("RealDeal", 0f, 0.01f);
        }


        public void RealDeal() {
            if (cardList.Count > 3) {
                //选择要发的牌
                int rdm = Random.Range(0, cardList.Count);
//				depth++;
                CardObject card = InstantiateCard(rdm, table.transform);
                //给自己发牌
                if (dealStatus % 3 == 0) {
                    Players[0].AddCard(card);
                }
                //左边(left)发牌
                if (dealStatus % 3 == 2) {
                    Players[2].AddCard(card);
                }
                //右边(right)发牌
                if (dealStatus % 3 == 1) {
                    Players[1].AddCard(card);
                }
                dealStatus++;
                cardList.RemoveAt(rdm);
                card.ReverseCard();
            }
            if (cardList.Count <= 3) {
                for (int i = 0; i < cardList.Count; i++) {
                    CardObject card = InstantiateCard(i, DiPai);
                    card.transform.localPosition = DiPai.position;

                    if (i == 0) {
                        card.transform.localPosition = DiPai.position - new Vector3(90, 0, 0);
                    }
                    if (i == 1) {
                        card.transform.localPosition = DiPai.position;
                    }
                    if (i == 2) {
                        card.transform.localPosition = DiPai.position + new Vector3(90, 0, 0);
                    }
                    DiPaiList.Add(card);
                    //反转地主牌
                    card.ReverseCard();
                }
                CancelInvoke("RealDeal");
                //最后排序每个玩家的手牌
                Players.ForEach(p => p.SortCard(false));
                _gameStatus = GameStatus.GameStart;
            }
        }

        #endregion

        #region GameStart(等待发牌完毕)

        private void WaitForCardReady() {
            _gameStatus = GameStatus.GameCall;
            //随机叫地主
            GameButtons.gameObject.SetActive(true);
            MenuButtons.gameObject.SetActive(false);
//					int rdmPlayerIndex = Random.Range(0, Players.Count);
//					Player p = Players[rdmPlayerIndex];
            Player p = Players[0];
            currentPlayer = p;
            firstOne = currentPlayer;
            CallLandLord(p);
            CancelInvoke("WaitForCardReady");
        }

        #endregion

        #region GameCall(叫地主)

        private void CheckLoadLord() {
            bool isCalledT = true;
            foreach (Player player in Players) {
                isCalledT &= player.IsCalled;
            }
            if (isCalledT) {
                var s = calledList.Aggregate("", (current, player) => current + (" " + player.Position));
                Debug.Log(s);

                bool allNotCall = true;
                foreach (Player player in Players) {
                    allNotCall &= player.CallWeight == 1;
                }
                if (allNotCall) {
                    Debug.Log("没一个人叫,重开");
                }

                bool allCall = true;
                foreach (Player player in Players) {
                    allCall &= player.CallWeight == 2;
                }
                if (allCall && firstOne.IsCalled) {
                    Debug.Log("全叫地主了, 第一个再叫");
                    firstOne.IsCalled = false;
                }

                if (calledList.Count == 1) {
                    Debug.Log("只有一个人叫地主," + calledList[0].Position + "是地主");
                    calledList[0].IsLord = true;
                    _gameStatus = GameStatus.GameGiveLandLordCard;
                }
                if (calledList.Count == 2) {
                    if (calledList[0].CallWeight == 2 && calledList[1].CallWeight == 2) {
                        Debug.Log("2个人都叫了地主,让第一个人再叫");
                        calledList[0].IsCalled = false;
                    }
                    if (calledList[0].CallWeight == 3 && calledList[1].CallWeight == 2) {
                        Debug.Log("2个人都叫了地主,第1个人没叫,第2个人是地主:" + calledList[1].Position);
                        calledList[1].IsLord = true;
                        _gameStatus = GameStatus.GameGiveLandLordCard;
                    }
                }
                if (calledList.Count == 3) {
                    foreach (Player player in calledList) {
                        Debug.Log(player.Position + ":" + player.CallWeight);
                    }
                    if (calledList[0].Position == Player.PlayerPosition.Self &&
                        calledList[1].Position == Player.PlayerPosition.Right &&
                        calledList[2].Position == Player.PlayerPosition.Left) {
                        if (firstOne.IsCalled && calledList[2].CallWeight == 2 && calledList[1].CallWeight == 2 &&
                            calledList[0].CallWeight == 3) {
                            Debug.Log("这里");
                            Debug.Log(calledList[2].Position + "是地主");
                            calledList[2].IsLord = true;
                            _gameStatus = GameStatus.GameGiveLandLordCard;
                        }
                    }

                    int right, left;
                    int self = right = left = 0;
                    foreach (Player player in calledList) {
                        switch (player.Position) {
                            case Player.PlayerPosition.Self:
                                self++;
                                break;
                            case Player.PlayerPosition.Right:
                                right++;
                                break;
                            case Player.PlayerPosition.Left:
                                left++;
                                break;
                        }
                    }
                    if (self == 2) {
                        Debug.Log("self是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Self:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                    if (right == 2) {
                        Debug.Log("right是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Right:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                    if (left == 2) {
                        Debug.Log("left是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Left:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                }
                if (calledList.Count == 4) {
                    int right, left;
                    int self = right = left = 0;
                    foreach (Player player in calledList) {
                        switch (player.Position) {
                            case Player.PlayerPosition.Self:
                                self++;
                                break;
                            case Player.PlayerPosition.Right:
                                right++;
                                break;
                            case Player.PlayerPosition.Left:
                                left++;
                                break;
                        }
                    }
                    if (self == 2) {
                        Debug.Log("self是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Self:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                    if (right == 2) {
                        Debug.Log("right是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Right:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                    if (left == 2) {
                        Debug.Log("left是地主");
                        foreach (Player player in Players) {
                            switch (player.Position) {
                                case Player.PlayerPosition.Left:
                                    player.IsLord = true;
                                    _gameStatus = GameStatus.GameGiveLandLordCard;
                                    break;
                            }
                        }
                    }
                }
            } else {
                if (currentPlayer.IsCalled) {
                    Player.PlayerPosition playerPosition = currentPlayer.Position + 1;
                    int next = (int) playerPosition % Players.Count;
                    currentPlayer = Players[next];
                } else {
                    if (_currentTime == 0) {
                        CallLandLord(currentPlayer);
                    }
                }
            }
        }

        private void CallLandLord(Player player) {
            StopAllCoroutines();
            if (player.Position == Player.PlayerPosition.Self) {
                _currentTime = 3;
//				_currentTime = 16;
                StartCoroutine(CountDown(TimerList[(int) Player.PlayerPosition.Self], player));
                CallLandLordBt.gameObject.SetActive(true);
                PassLandLordBt.gameObject.SetActive(true);
            }
            if (player.Position == Player.PlayerPosition.Right) {
                //随机叫
                _currentTime = 2;
                StartCoroutine(CountDown
                (TimerList[
                        (int) Player.PlayerPosition.Right],
                    player));
            }
            if (player.Position == Player.PlayerPosition.Left) {
                //随机叫
                _currentTime = 2;
                StartCoroutine(CountDown(TimerList[(int) Player.PlayerPosition.Left], player));
            }
        }

        private IEnumerator CountDown(UISprite timer, Player player) {
            timer.gameObject.SetActive
                (true);
            while
                (_currentTime > 1) {
                _currentTime--;
                timer.transform.FindChild("time").GetComponent<UILabel>().text = _currentTime + "";
                yield return new WaitForSeconds(1);
            }
            timer.gameObject.SetActive
                (false);
            CountDownOver(player);
        }

        void CountDownOver(Player player) {
            if (player.Position != Player.PlayerPosition.Self) {
                bool[] t = {true, false};
                int index = Random.Range(0, t.Length);

                bool isCall = t[index];
//				//测试
//                if(player.Position == Player.PlayerPosition.Right) {
//                    if (player.CallWeight == 0) {
//                        isCall = true;
//                    }
//                    if (player.CallWeight == 2) {
//                        isCall = false;
//                    }
//                }
//                if (player.Position == Player.PlayerPosition.Left) {
//                    if (player.CallWeight == 0) {
//                        isCall = true;
//                    }
//                    if (player.CallWeight == 2) {
//                        isCall = true;
//                    }
//                }
                if (isCall) {
                    Debug.Log(player.Position + " 叫地主了");
                    calledList.Add(player);
                    player.CallWeight += 2;
                    _textController.ShowTextOn(TextController.TextEnum.CallLandLord, player);
                } else {
                    Debug.Log(player.Position + " 没有叫");
                    player.CallWeight += 1;
                    _textController.ShowTextOn(TextController.TextEnum.PassLandLord, player);
                }
                player.IsCalled = true;
                _currentTime = 0;
            }
            if (player.Position == Player.PlayerPosition.Self) {
                Debug.Log(player.Position + " 没有叫");
                CallLandLordBt.gameObject.SetActive(false);
                PassLandLordBt.gameObject.SetActive(false);
                player.CallWeight += 1;
                _textController.ShowTextOn(TextController.TextEnum.PassLandLord, player);
                player.IsCalled = true;
                _currentTime = 0;
            }
        }

        #endregion

        #region Gaming(游戏流程)

        private void PlayCard(Player player) {
            //该出牌了, 把出的牌隐藏
            foreach (var playedCard in player.PlayedCards) {
                playedCard.gameObject.SetActive(false);
            }
            StopAllCoroutines();
            if (player.Position == Player.PlayerPosition.Self) {
                _currentTime = 16;
                StartCoroutine(PlayCountDown(TimerList[(int) Player.PlayerPosition.Self], player));
            }
            if (player.Position == Player.PlayerPosition.Right) {
                //随机叫
                _currentTime = 2;
                StartCoroutine(PlayCountDown(TimerList[(int) Player.PlayerPosition.Right], player));
            }
            if (player.Position == Player.PlayerPosition.Left) {
                //随机叫
                _currentTime = 2;
                StartCoroutine(PlayCountDown(TimerList[(int) Player.PlayerPosition.Left], player));
            }
        }

        private IEnumerator PlayCountDown(UISprite timer, Player player) {
            timer.gameObject.SetActive(true);
            while (_currentTime > 1) {
                _currentTime--;
                timer.transform.FindChild("time").GetComponent<UILabel>().text = _currentTime + "";
                yield return new WaitForSeconds(1);
            }
            timer.gameObject.SetActive(false);
            PlayCountDownOver(player);
        }

        void PlayCountDownOver(Player player) {
            if (player.Position == Player.PlayerPosition.Self) {
//                player.SortCard(true, 0, 0);
//                TipsBt.isEnabled = false;
//                PassBt.isEnabled = false;
//                PlayBt.isEnabled = false;
//                player.IsTurn = false;
//                _currentTime = 0;
//                Players[1].IsTurn = true;
//                player.SelectedCards.Clear();
//                player.ReadyPlayCards.Clear();

//                var cardObjects = _gameController.AiPlay(player);
                var cardObjects = _gameController.AvdAi(player);
                if (cardObjects != null && cardObjects.Count > 0) {
                    player.PlayCards.Clear();
                    player.PlayCards.AddRange(cardObjects);
                    player.PlayedCards.AddRange(cardObjects);
                }
                player.PlayCard(cardObjects.ToArray());
                player.SortCard(true, 0, 0);

                TipsBt.isEnabled = false;
                PassBt.isEnabled = false;
                PlayBt.isEnabled = false;
                player.IsTurn = false;
                Players[1].IsTurn = true;
//                player.SelectedCards.Clear();
//                player.ReadyPlayCards.Clear();
                _currentTime = 0;
            }
            if (player.Position != Player.PlayerPosition.Self) {
                //测试
//                CardObject playerCard = player.Cards[player.Cards.Count - 1];
//                player.PlayCard(new[]{playerCard});

//                var cardObjects = _gameController.AiPlay(player);
                var cardObjects = _gameController.AvdAi(player);

                if (cardObjects != null && cardObjects.Count > 0) {
                    player.PlayCards.Clear();
                    player.PlayCards.AddRange(cardObjects);
                    player.PlayedCards.AddRange(cardObjects);
                }
                player.PlayCard(cardObjects.ToArray());

                player.SortCard(true, 0, 0);
                TipsBt.isEnabled = false;
                PassBt.isEnabled = false;
                PlayBt.isEnabled = false;
                player.IsTurn = false;
                _currentTime = 0;

                Player.PlayerPosition playerPosition = currentPlayer.Position + 1;
                int next = (int) playerPosition % Players.Count;
                Players[next].IsTurn = true;
            }
        }

        #endregion

        public void PlayedClearWork() {
            TimerList[(int) Player.PlayerPosition.Self].gameObject.SetActive(false);
            currentPlayer.SortCard(true, 0, 0);
            TipsBt.isEnabled = false;
            PassBt.isEnabled = false;
            PlayBt.isEnabled = false;
            currentPlayer.IsTurn = false;
            _currentTime = 0;
            Players[1].IsTurn = true;
            currentPlayer.ReadyPlayCards.Clear();
        }
    }
}