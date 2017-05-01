using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace model {
    public class Player {
        private List<CardObject> _cards = new List<CardObject>();
        private List<CardObject> _currentCards = new List<CardObject>();
        private PlayerPosition _position = PlayerPosition.Self;
        private Vector3 _tmpPos;
        private bool _isLord = false;
        private List<CardObject> _selectedCards = new List<CardObject>();
        private List<CardObject> _readyPlayCards = new List<CardObject>();
        private List<CardObject> _playCards = new List<CardObject>();
        private List<CardObject> _playedCards = new List<CardObject>();
        private Transform _playZone;
        public int ChuPaiCishu = 0;
        public PlayerStatus PlayedStatus = PlayerStatus.Passed;

        /// <summary>
        ///   <para>玩家的位置</para>
        /// </summary>
        public enum PlayerStatus {
            Played,
            Passed
        }

        public Player(PlayerPosition position) {
            CallWeight = 0;
            IsCalled = false;
            IsTurn = false;
            _position = position;
            _tmpPos = GetPlayerRealPosition();
        }

        public bool IsCalled { get; set; }
        public bool IsTurn { get; set; }
        public int CallWeight { get; set; }

        public PlayerPosition Position {
            get { return _position; }
            set { _position = value; }
        }

        public List<CardObject> Cards {
            get { return _cards; }
            set { _cards = value; }
        }

        public List<CardObject> SelectedCards {
            get { return _selectedCards; }
            set { _selectedCards = value; }
        }

        public List<CardObject> ReadyPlayCards {
            get { return _readyPlayCards; }
            set { _readyPlayCards = value; }
        }

        public List<CardObject> PlayCards {
            get { return _playCards; }
            set { _playCards = value; }
        }

        public List<CardObject> PlayedCards {
            get { return _playedCards; }
            set { _playedCards = value; }
        }

        public List<CardObject> CurrentCards {
            get { return _currentCards; }
            set { _currentCards = value; }
        }

        /// <summary>
        ///   <para>添加牌到玩家的手牌中</para>
        /// </summary>
        public void AddCard(CardObject card) {
            _cards.Add(card);
            _currentCards.Add(card);
        }

        /// <summary>
        ///   <para>当前玩家手牌总数</para>
        /// </summary>
        public int CardsCount() {
            return _cards.Count;
        }

        /// <summary>
        ///   <para>设置玩家的位置(self,left,right)</para>
        /// </summary>
        public void SetPlayerPosition(PlayerPosition pp) {
            _position = pp;
        }

        /// <summary>
        ///   <para>玩家的位置</para>
        /// </summary>
        public enum PlayerPosition {
            Self = 0,
            Right = 1,
            Left = 2,
        }

        /// <summary>
        ///   <para>获取玩家的坐标位置(根据PlayerPosition判断)</para>
        /// </summary>
        public Vector3 GetPlayerRealPosition() {
            Vector3 p = Vector3.zero;
            switch (_position) {
                case PlayerPosition.Self:
                    p = new Vector3(0, -388);
                    break;
                case PlayerPosition.Left:
                    p = new Vector3(-591, 76);
                    break;
                case PlayerPosition.Right:
                    p = new Vector3(591, 76);
                    break;
            }
            return p;
        }

        public void SortCard(bool dontReverse) {
            SortCard(dontReverse, 0.1f, 0.05f);
        }

        public void SortCard(bool dontReverse, float startTime, float interval) {
            if (_isLord) {
                var ss = "";
                foreach (CardObject co in _cards) {
                    ss += " " + co.Card;
                }
                Debug.Log(ss);
            }
            _cards.Sort(CardComparer);
            _tmpPos = GetPlayerRealPosition();
            int center = CardsCount() / 2;
            string s = _position + " ";
            _cards.ForEach(c => s += c.Card.cardNumber + " ");
            Debug.Log(s);
            int d = 0;
            switch (_position) {
                case PlayerPosition.Self:
                    d = 0;
                    break;
                case PlayerPosition.Left:
                    d = 50;
                    break;
                case PlayerPosition.Right:
                    d = 100;
                    break;
            }
            float t = startTime;
            foreach (CardObject co in _cards) {
                t += interval;
                int index = _cards.FindIndex(c => c.GetInstanceID() == co.GetInstanceID());
                int diff = index - center;
                d += 2;
                Vector3 newPos;
                switch (_position) {
                    case PlayerPosition.Self:
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(_tmpPos.x + diff * GameConst.CardBetween, _tmpPos.y, 0);
                        MoveTo(co, newPos, t, dontReverse, 0.2f);
                        break;
                    case PlayerPosition.Left:
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(_tmpPos.x + diff * 40f, _tmpPos.y + 35f * diff, 0);
                        MoveTo(co, newPos, t, dontReverse, 0.2f);
                        break;
                    case PlayerPosition.Right:
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(_tmpPos.x + diff * 40f, _tmpPos.y - 35f * diff, 0);
                        MoveTo(co, newPos, t, dontReverse, 0.2f);
                        break;
                }
            }
        }

        private static void MoveTo(CardObject co, Vector3 newPos, float t, bool dontReverse, float time) {
            if (dontReverse) {
                iTween.MoveTo(co.transform.gameObject, iTween.Hash("position", newPos, "time", time,
                    "easetype", iTween.EaseType.easeOutSine, "islocal", true, "delay", t));
            } else {
                iTween.MoveTo(co.transform.gameObject, iTween.Hash("position", newPos, "time", time,
                    "easetype", iTween.EaseType.easeOutSine, "islocal", true, "delay", t, "oncomplete", "ReverseCard",
                    "oncompleteparams", co));
            }
        }

        public void ReverseCard(CardObject co) {
            co.ReverseCard();
        }

        public int CardComparer(CardObject c1, CardObject c2) {
            return (int) c2.Card.cardWeight - (int) c1.Card.cardWeight;
        }

        public void SortPlayCards() {
            _tmpPos = GetPlayZone().position;
            if (_playCards.Count == 0) {
                return;
            }
            if (_playCards.Count <= 1) {
                iTween.MoveTo(_playCards[0].gameObject, _tmpPos, 0.2f);
                //加到已出牌中
                _playedCards.Add(_playCards[0]);
                //从手牌中删除
                _cards.Remove(_playCards[0]);
                return;
            }
            _playCards.Sort(CardComparer);
            int center = _playCards.Count / 2;
            int d = 0;
            float t = 0.1f;
            foreach (CardObject co in _playCards) {
                //加到已出牌中
                _playedCards.Add(co);
                //从手牌中删除
                _cards.Remove(co);
                t += 0.05f;
                int index = _playCards.FindIndex(c => c.GetInstanceID() == co.GetInstanceID());
                int diff = index - center;
                d = 50;
                Vector3 newPos;
                switch (_position) {
                    case PlayerPosition.Self:
                        d += 2;
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(_tmpPos.x + diff * GameConst.CardBetween, _tmpPos.y, 0);
                        MoveTo(co, newPos, t, true, 0.2f);
                        break;
                    case PlayerPosition.Left:
                        d -= 2;
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(GetPlayZone().localPosition.x - diff * GameConst.CardBetween, _tmpPos.y,
                            0);
                        MoveTo(co, newPos, t, true, 0.2f);
                        break;
                    case PlayerPosition.Right:
                        d += 2;
                        co.transform.GetComponent<UISprite>().depth = d;
                        co.CardNumberSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        co.CardSuitSprite.transform.GetComponent<UISprite>().depth = d + 1;
//						co.CardPicSprite.transform.GetComponent<UISprite>().depth = d + 1;
                        newPos = new Vector3(GetPlayZone().localPosition.x + diff * GameConst.CardBetween, _tmpPos.y,
                            0);
                        MoveTo(co, newPos, t, true, 0.2f);
                        break;
                }
            }
        }

        public bool IsLord {
            get { return _isLord; }
            set { _isLord = value; }
        }

        public void CallLandLord() {
            _isLord = true;
        }

        public void AddPlayZone(Transform playZone) {
            _playZone = playZone;
        }

        public Transform GetPlayZone() {
            return _playZone;
        }

        public void PlayCard() {
            foreach (var playedCard in _playedCards) {
                playedCard.gameObject.SetActive(false);
            }
            PlayCards = ReadyPlayCards.Skip(0).Take(ReadyPlayCards.Count).ToList();
        }

        public void PlayCard(CardObject[] cards) {
            PlayCards = cards.ToList();
            ReadyPlayCards.Clear();
            SortPlayCards();
            IsTurn = false;
        }
    }
}