using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Controller;
using model;
using UnityEngine;

public class GameController : MonoBehaviour {
    private CardController _cardController;
    private List<Player> _players;
    private TextController _textController;

    public enum RoleType {
        SingleCard, //单牌
        DoubleCard, //对子
        ThreeCard, //3不带
        BombCard, //炸弹
        KingBombCard, //WANGZHA
        ThreeOneCard, //三带一
        ThreeTwoCard, //三带二
        FourTwoCard, //四带俩单
        FourTwotwoCard, //四带俩对
        LianCard, //顺子
        LianDuiCard, //连队
        FeijiNoCard, //飞机不带
        FeijiSingleCard, //飞机带单
        FeijiDoubleCard, //飞机带对
        ErrorCard //错误类型
    }

    // Use this for initialization
    void Start() {
        _cardController = transform.GetComponent<CardController>();
        _textController = transform.GetComponent<TextController>();
        _players = _cardController.PlayersList;
    }

    // Update is called once per frame
    void Update() {
    }

    public void PlayCard(Player currentPlayer) {
        //隐藏出过的牌,复制(准备出的牌)到(出的牌),清空(准备出的牌)
        currentPlayer.PlayCard();
        int preplayerId = GetPrePlayer(currentPlayer); //上一个玩家的id -1则重新开始出牌 -2则略过
        bool isOvercome = false; //本次要出的牌是否能出
        if (preplayerId == -1) {
            ClearPlayerChuPai();
            isOvercome = true;
        } else if (preplayerId == -2) {
            isOvercome = false;
        }
        Debug.Log("preplayerId =====   " + preplayerId);
        Debug.Log("isOvercome =====   " + isOvercome);

        List<CardObject> currentPlayerPlayCards = currentPlayer.PlayCards; //当前玩家出的牌
        if (currentPlayerPlayCards != null && currentPlayerPlayCards.Count == 0) {
            //空牌点了出牌按钮
            Debug.Log("空牌点了出牌按钮");
            _textController.ShowTextOn(TextController.TextEnum.NoCardRoleType, currentPlayer);
            return;
        }
        RoleType roleType = GetCardType(currentPlayerPlayCards); //当前玩家牌型
        Debug.Log("roleType:" + roleType);
        if (preplayerId > 0) {
            var prePlayCards = _players[preplayerId].PlayCards;
            var preRoleType = GetCardType(prePlayCards);

            isOvercome = IsOvercomePrev(currentPlayerPlayCards, roleType, prePlayCards, preRoleType);
        }

        if (isOvercome) {
            Debug.Log("我出的牌大过上家。或者这是第一次出牌");
        } else {
            Debug.Log("我出的牌不能大过上家");
            _textController.ShowTextOn(TextController.TextEnum.NoCardOverCome, currentPlayer);
        }
        if (roleType == RoleType.ErrorCard) {
            _textController.ShowTextOn(TextController.TextEnum.NoCardRoleType, currentPlayer);
        }

        if (isOvercome && roleType != RoleType.ErrorCard) {
            foreach (var cardObject in currentPlayerPlayCards) {
                if (currentPlayer.CurrentCards.Contains(cardObject)) {
                    currentPlayer.CurrentCards.Remove(cardObject);
                }
            }
//            currentPlayer.ChuPaiCishu += 1;
            currentPlayer.PlayedStatus = Player.PlayerStatus.Played;
            currentPlayer.SortPlayCards();
            _cardController.PlayedClearWork();

            _textController.HideText(TextController.TextEnum.NoCardOverCome, currentPlayer);
            _textController.HideText(TextController.TextEnum.NoCardRoleType, currentPlayer);
        }
    }


    #region 规则判断

    /**
	 * 判断我选择出的牌和上家的牌的大小，决定是否可以出牌
	 *
	 * @param myCards
	 *            我想出的牌
	 *
	 * @param myCardType
	 *            我的牌的类型
	 * @param prevCards
	 *            上家的牌
	 * @param prevCardType
	 *            上家的牌型
	 * @return 可以出牌，返回true；否则，返回false。
	 */
    public bool IsOvercomePrev(List<CardObject> myCards, RoleType myRoleType,
        List<CardObject> prevCards, RoleType prevRoleType) {
        // 我的牌和上家的牌都不能为null
        if (myCards == null || prevCards == null) {
            return false;
        }

        if (prevRoleType == RoleType.ErrorCard) {
            Debug.Log("上家出的牌不合法，所以不能出。");
            return false;
        }

        // 上一首牌的个数
        int prevSize = prevCards.Count;
        int mySize = myCards.Count;

        // 我先出牌，上家没有牌
        if (prevSize == 0 && mySize != 0) {
            return true;
        }

        // 集中判断是否王炸，免得多次判断王炸
        if (prevRoleType == RoleType.KingBombCard) {
            Debug.Log("上家王炸，肯定不能出。");
            return false;
        }
        if (myRoleType == RoleType.KingBombCard) {
            Debug.Log("我王炸，肯定能出。");
            return true;
        }

        // 集中判断对方不是炸弹，我出炸弹的情况
        if (prevRoleType != RoleType.BombCard && myRoleType == RoleType.BombCard) {
            return true;
        }

        // 默认情况：上家和自己想出的牌都符合规则
        SortCards(myCards); // 对牌排序
        SortCards(prevCards); // 对牌排序

        GameConst.CardWeight myCardWeight = GetWeight(myCards[0]);
        GameConst.CardWeight prevCardWeight = GetWeight(prevCards[0]);
        RoleTypeIndex myRoleTypeIndex = CardIndexCompute(myCards);
        var prevRoleTypeIndex = CardIndexCompute(prevCards);

        // 比较2家的牌，主要有2种情况，
        // 1.我出和上家一种类型的牌，即对子管对子；
        // 2.我出炸弹，此时，和上家的牌的类型可能不同
        // 王炸的情况已经排除

        // 单
        if (prevRoleType == RoleType.SingleCard && myRoleType == RoleType.SingleCard) {
            // 一张牌可以大过上家的牌
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 对子
        if (prevRoleType == RoleType.DoubleCard && myRoleType == RoleType.DoubleCard) {
            // 2张牌可以大过上家的牌
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 3不带
        if (prevRoleType == RoleType.ThreeCard && myRoleType == RoleType.ThreeCard) {
            // 3张牌可以大过上家的牌
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 炸弹
        if (prevRoleType == RoleType.BombCard && myRoleType == RoleType.BombCard) {
            // 4张牌可以大过上家的牌
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 3带1
        if (prevRoleType == RoleType.ThreeOneCard && myRoleType == RoleType.ThreeOneCard) {
            // 3带1只需比较第2张牌的大小
//            myCardWeight = GetWeight(myCards[1]);
//            prevCardWeight = GetWeight(prevCards[1]);
            myCardWeight = GetWeight(myRoleTypeIndex.ThreeIndex[0]);
            prevCardWeight = GetWeight(prevRoleTypeIndex.ThreeIndex[0]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 4带2
        if (prevRoleType == RoleType.FourTwoCard && myRoleType == RoleType.FourTwoCard) {
            // 4带2只需比较第3张牌的大小
//            myCardWeight = GetWeight(myCards[2]);
//            prevCardWeight = GetWeight(prevCards[2]);
            myCardWeight = GetWeight(myRoleTypeIndex.FourIndex[0]);
            prevCardWeight = GetWeight(prevRoleTypeIndex.FourIndex[0]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 顺子
        else if (prevRoleType == RoleType.LianCard && myRoleType == RoleType.LianCard) {
            if (mySize != prevSize) {
                return false;
            }
            // 顺子只需比较最大的1张牌的大小
            myCardWeight = GetWeight(myCards[mySize - 1]);
            prevCardWeight = GetWeight(prevCards[prevSize - 1]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 连对
        else if (prevRoleType == RoleType.LianDuiCard && myRoleType == RoleType.LianDuiCard) {
            if (mySize != prevSize) {
                return false;
            }
            // 顺子只需比较最大的1张牌的大小
            myCardWeight = GetWeight(myCards[mySize - 1]);
            prevCardWeight = GetWeight(prevCards[prevSize - 1]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 飞机budai
        else if (prevRoleType == RoleType.FeijiNoCard && myRoleType == RoleType.FeijiNoCard) {
            if (mySize != prevSize) {
                return false;
            }
            // 333 444 || 555 666
            // 顺子只需比较第5张牌的大小(特殊情况333444555666没有考虑，即12张的飞机，可以有2种出法)
//                myCardWeight = GetWeight(myCards[4]);
//                prevCardWeight = GetWeight(prevCards[4]);
            myCardWeight = GetWeight(myRoleTypeIndex.ThreeIndex[0]);
            prevCardWeight = GetWeight(prevRoleTypeIndex.ThreeIndex[0]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 飞机dai dan
        else if (prevRoleType == RoleType.FeijiSingleCard && myRoleType == RoleType.FeijiSingleCard) {
            if (mySize != prevSize) {
                return false;
            }
            // 顺子只需比较第5张牌的大小(特殊情况333444555666没有考虑，即12张的飞机，可以有2种出法)
//                myCardWeight = GetWeight(myCards[4]);
//                prevCardWeight = GetWeight(prevCards[4]);
            myCardWeight = GetWeight(myRoleTypeIndex.ThreeIndex[0]);
            prevCardWeight = GetWeight(prevRoleTypeIndex.ThreeIndex[0]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 飞机dai dui
        else if (prevRoleType == RoleType.FeijiDoubleCard && myRoleType == RoleType.FeijiDoubleCard) {
            if (mySize != prevSize) {
                return false;
            }
            // 顺子只需比较第5张牌的大小(特殊情况333444555666没有考虑，即12张的飞机，可以有2种出法)
            myCardWeight = GetWeight(myRoleTypeIndex.ThreeIndex[0]);
            prevCardWeight = GetWeight(prevRoleTypeIndex.ThreeIndex[0]);
            return CompareWeight(myCardWeight, prevCardWeight);
        }
        // 默认不能出牌
        return false;
    }

    private bool CompareWeight(GameConst.CardWeight weight1, GameConst.CardWeight weight2) {
        return weight1 > weight2;
    }

    #endregion

    #region 牌型判断

    public GameConst.CardNumber GetNumber(CardObject card) {
        return card.Card.cardNumber;
    }

    public GameConst.CardWeight GetWeight(CardObject card) {
        return card.Card.cardWeight;
    }

    public GameConst.CardWeight GetWeight(GameConst.CardNumber cardNumber) {
        GameConst.CardWeight cardWeight = (GameConst.CardWeight) GameConst.CardWeightList[(int) cardNumber - 1];
        return cardWeight;
    }

    public bool IsDan(List<CardObject> cards) {
        return cards != null && cards.Count == 1;
    }

    public bool IsDuiZi(List<CardObject> cards) {
        bool flag = false;

        if (cards != null && cards.Count == 2) {
            GameConst.CardNumber grade0 = GetNumber(cards[0]);
            GameConst.CardNumber grade1 = GetNumber(cards[1]);
            if (grade0 == grade1) {
                flag = true;
            }
        }
        return flag;
    }

    public int IsSanDaiYi(List<CardObject> cards) {
        int flag = -1;
        // 默认不是3带1
        if (cards != null && cards.Count == 4) {
            // 对牌进行排序
            SortCards(cards);

            GameConst.CardNumber[] numbers = new GameConst.CardNumber[4];
            numbers[0] = GetNumber(cards[0]);
            numbers[1] = GetNumber(cards[1]);
            numbers[2] = GetNumber(cards[2]);
            numbers[3] = GetNumber(cards[3]);

            // 暂时认为炸弹不为3带1
            if ((numbers[1] == numbers[0]) && (numbers[2] == numbers[0])
                && (numbers[3] == numbers[0])) {
                return -1;
            }
            // 3带1，被带的牌在牌头
            if ((numbers[1] == numbers[0] && numbers[2] == numbers[0])) {
                return 0;
            }
            // 3带1，被带的牌在牌尾
            if (numbers[1] == numbers[3] && numbers[2] == numbers[3]) {
                return 3;
            }
        }
        return flag;
    }


    /**
     * 判断牌是否为3带2
     *
     * @param cards
     *            牌的集合
     * @return 如果为3带1，被带牌的位置，0或3，否则返回-1。炸弹返回-1。
     */
    public int IsSanDaiEr(List<CardObject> cards) {
        int flag = -1;
        // 默认不是3带2
        if (cards != null && cards.Count == 5) {
            // 对牌进行排序
            SortCards(cards);

            GameConst.CardNumber[] numbers = new GameConst.CardNumber[5];
            numbers[0] = GetNumber(cards[0]);
            numbers[1] = GetNumber(cards[1]);
            numbers[2] = GetNumber(cards[2]);
            numbers[3] = GetNumber(cards[3]);
            numbers[4] = GetNumber(cards[4]);

            // 3带2，被带的牌在牌头
            if ((numbers[1] == numbers[0] && numbers[2] == numbers[3] && numbers[4] == numbers[3])) {
                return 0;
            }
            // 3带，被带的牌在牌尾
            if (numbers[0] == numbers[1] && numbers[2] == numbers[1] && numbers[4] == numbers[3]) {
                return 3;
            }
        }
        return flag;
    }

    /**
     * 判断牌是否为3不带
     *
     * @param cards
     *            牌的集合
     * @return 如果为3不带，返回true；否则，返回false。
     */
    public bool IsSanBuDai(List<CardObject> cards) {
        // 默认不是3不带
        bool flag = false;

        if (cards != null && cards.Count == 3) {
            GameConst.CardNumber[] numbers = new GameConst.CardNumber[3];
            numbers[0] = GetNumber(cards[0]);
            numbers[1] = GetNumber(cards[1]);
            numbers[2] = GetNumber(cards[2]);
            if (numbers[0] == numbers[1] && numbers[2] == numbers[0]) {
                flag = true;
            }
        }
        return flag;
    }

    /**
	 * 判断牌是否为顺子
	 *
	 * @param cards
	 *            牌的集合
	 * @return 如果为顺子，返回true；否则，返回false。
	 */
    public bool IsShunZi(List<CardObject> cards) {
        // 默认是顺子
        bool flag = true;

        if (cards != null) {
            int size = cards.Count;
            // 顺子牌的个数在5到12之间
            if (size < 5 || size > 12) {
                return false;
            }

            // 对牌进行排序
            SortCardsByWeight(cards);
            for (int n = 0; n < size - 1; n++) {
                GameConst.CardWeight prev = GetWeight(cards[n]);
                GameConst.CardWeight next = GetWeight(cards[n + 1]);
                // 小王、大王、2不能加入顺子
                if ((int) prev == 16 || (int) prev == 17 || (int) prev == 15
                    || (int) next == 16 || (int) next == 17 || (int) next == 15) {
                    flag = false;
                    Debug.Log("有大小王或2");
                    break;
                }
                if (prev - next != -1) {
                    flag = false;
                    break;
                }
            }
        }
        return flag;
    }

    /**
	 * 判断牌是否为炸弹
	 *
	 * @param cards
	 *            牌的集合
	 * @return 如果为炸弹，返回true；否则，返回false。
	 */
    public bool IsZhaDan(List<CardObject> cards) {
        // 默认不是炸弹
        bool flag = false;
        if (cards != null && cards.Count == 4) {
            GameConst.CardNumber[] numbers = new GameConst.CardNumber[4];
            numbers[0] = GetNumber(cards[0]);
            numbers[1] = GetNumber(cards[1]);
            numbers[2] = GetNumber(cards[2]);
            numbers[3] = GetNumber(cards[3]);
            if ((numbers[1] == numbers[0])
                && (numbers[2] == numbers[0])
                && (numbers[3] == numbers[0])) {
                flag = true;
            }
        }
        return flag;
    }

    /**
     * 判断牌是否为王炸
     *
     * @param cards
     *            牌的集合
     * @return 如果为王炸，返回true；否则，返回false。
     */
    public bool IsDuiWang(List<CardObject> cards) {
        // 默认不是对王
        bool flag = false;

        if (cards != null && cards.Count == 2) {
            // 只有小王和大王的等级之后才可能是33
            GameConst.CardNumber[] numbers = new GameConst.CardNumber[2];
            numbers[0] = GetNumber(cards[0]);
            numbers[1] = GetNumber(cards[1]);
            if ((int) numbers[0] + (int) numbers[1] == 29) {
                flag = true;
            }
        }
        return flag;
    }

    /**
	 * 判断牌是否为连对
	 *
	 * @param cards
	 *            牌的集合
	 * @return 如果为连对，返回true；否则，返回false。
	 */
    public bool IsLianDui(List<CardObject> cards) {
        // 默认是连对
        bool flag = true;
        if (cards == null) {
            return false;
        }

        int size = cards.Count;
        if (size < 6 || size % 2 != 0) {
            return false;
        }
        // 对牌进行排序
        SortCardsByWeight(cards);
        for (int i = 0; i < size; i = i + 2) {
            if ((int) GetWeight(cards[i]) == 15) {
                flag = false;
                break;
            }
            if (GetWeight(cards[i]) != GetWeight(cards[i + 1])) {
                flag = false;
                break;
            }

            if (i < size - 2) {
                if (GetWeight(cards[i]) - GetWeight(cards[i + 2]) != -1) {
                    flag = false;
                    break;
                }
            }
        }

        return flag;
    }

    /**
	 * 判断牌是否为飞机不带
	 *
	 * @param cards
	 *            牌的集合
	 * @return 如果为飞机不带，返回true；否则，返回false。
	 */
    public bool IsFeiJiBuDai(List<CardObject> cards) {
        bool flag = false;
        if (cards != null && cards.Count % 3 == 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(cards);
            if (roleTypeIndex.ThreeIndex.Contains(GameConst.CardNumber.Two)) //飞机主体不能带2
            {
                return false;
            }

            SortCards(roleTypeIndex.ThreeIndex);
            for (int i = 0; i < roleTypeIndex.ThreeIndex.Count - 1; i++) //飞机的主体必须是连续的，如333 555就不行
            {
                if (GetWeight(roleTypeIndex.ThreeIndex[i + 1]) - GetWeight(roleTypeIndex.ThreeIndex[i]) != 1) {
                    return false;
                }
            }

            //飞机不带的模型是 4=0，3！=0，2=0，1=0
            if (roleTypeIndex.FourIndex.Count == 0 && roleTypeIndex.ThreeIndex.Count != 0 &&
                roleTypeIndex.DoubleIndex.Count == 0 && roleTypeIndex.SingleIndex.Count == 0) {
                flag = true;
            }
        }
        return flag;
    }

    /**
     * 判断牌是否为飞机带单
     *
     * @param cards
     *            牌的集合
     * @return 如果为飞机不带，返回true；否则，返回false。
     */
    public bool IsFeiJiDaiDan(List<CardObject> cards) {
        bool flag = false;
        if (cards != null && cards.Count % 4 == 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(cards);
            //飞机主体不能带2
            if (roleTypeIndex.ThreeIndex.Contains(GameConst.CardNumber.Two)) {
                return false;
            }

            SortCards(roleTypeIndex.ThreeIndex);
            //飞机的主体必须是连续的，如333 555就不行
            for (int i = 0; i < roleTypeIndex.ThreeIndex.Count - 1; i++) {
                if (roleTypeIndex.ThreeIndex[i + 1] - roleTypeIndex.ThreeIndex[i] != 1) {
                    return false;
                }
            }

            //飞机带单的模型是 4=0，3！=0，2*2 + 1 = 主体数（即比如 333 444 555 可以带 667 678）
            if (roleTypeIndex.FourIndex.Count == 0 && roleTypeIndex.ThreeIndex.Count != 0) {
                if (roleTypeIndex.DoubleIndex.Count * 2 + roleTypeIndex.SingleIndex.Count ==
                    roleTypeIndex.ThreeIndex.Count)
                    flag = true;
            }
        }
        return flag;
    }


    /**
     * 判断牌是否为飞机带对
     *
     * @param cards
     *            牌的集合
     * @return 如果为飞机不带，返回true；否则，返回false。
     */
    public bool IsFeiJiDaiDui(List<CardObject> cards) {
        bool flag = false;
        if (cards != null && cards.Count % 5 == 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(cards);
            //飞机主体不能带2
            if (roleTypeIndex.ThreeIndex.Contains(GameConst.CardNumber.Two)) {
                return false;
            }

            SortCards(roleTypeIndex.ThreeIndex);
            //飞机的主体必须是连续的，如333 555就不行
            for (int i = 0; i < roleTypeIndex.ThreeIndex.Count - 1; i++) {
                if (roleTypeIndex.ThreeIndex[i + 1] - roleTypeIndex.ThreeIndex[i] != 1) {
                    return false;
                }
            }

            //飞机带对的模型是 4=0，3！=0，2！=0且 2=3, 1 = 0
            if (roleTypeIndex.FourIndex.Count == 0 && roleTypeIndex.ThreeIndex.Count != 0) {
                if (roleTypeIndex.DoubleIndex.Count == roleTypeIndex.ThreeIndex.Count &&
                    roleTypeIndex.SingleIndex.Count == 0)
                    flag = true;
            }
        }
        return flag;
    }

    /**
     * 判断牌是否为4带2
     *
     * @param cards
     *            牌的集合
     * @return 如果为4带2，返回true；否则，返回false。
     */
    public bool IsSiDaiEr(List<CardObject> cards) {
        bool flag = false;
        if (cards != null && cards.Count == 6) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(cards);
            if (roleTypeIndex.FourIndex.Count == 1 && roleTypeIndex.ThreeIndex.Count == 0) {
                if (roleTypeIndex.SingleIndex.Count == 2 || roleTypeIndex.DoubleIndex.Count == 1) {
                    flag = true;
                }
            }
        }
        return flag;
    }

    /**
     * 判断牌是否为4带对子
     *
     * @param cards
     *            牌的集合
     * @return 如果为4带2，返回true；否则，返回false。
     */
    public bool IsSiDaiErDui(List<CardObject> cards) {
        bool flag = false;
        if (cards != null && cards.Count == 8) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(cards);
            if (roleTypeIndex.FourIndex.Count == 1 && roleTypeIndex.ThreeIndex.Count == 0) {
                if (roleTypeIndex.DoubleIndex.Count == 2) {
                    flag = true;
                }
            }
        }
        return flag;
    }


    //计算牌中的4，3，2，1个数
    public RoleTypeIndex CardIndexCompute(List<CardObject> cards) {
        RoleTypeIndex roleTypeIndex;
        roleTypeIndex.SingleIndex = new List<GameConst.CardNumber>();
        roleTypeIndex.DoubleIndex = new List<GameConst.CardNumber>();
        roleTypeIndex.ThreeIndex = new List<GameConst.CardNumber>();
        roleTypeIndex.FourIndex = new List<GameConst.CardNumber>();
        List<GameConst.CardNumber> pre = cards.Select(GetNumber).ToList();

        var vs = from GameConst.CardNumber p in pre group p by p into g select new {g, num = g.Count()};
        foreach (var v in vs) {
//			Debug.Log(v.g.Key + "=====" + v.num);
            if (v.num == 4) {
                roleTypeIndex.FourIndex.Add(v.g.Key);
            } else if (v.num == 3) {
                roleTypeIndex.ThreeIndex.Add(v.g.Key);
            } else if (v.num == 2) {
                roleTypeIndex.DoubleIndex.Add(v.g.Key);
            } else if (v.num == 1) {
                roleTypeIndex.SingleIndex.Add(v.g.Key);
            }
        }
        return roleTypeIndex;
    }

    //牌型个数结构体
    public struct RoleTypeIndex {
        public List<GameConst.CardNumber> SingleIndex;
        public List<GameConst.CardNumber> DoubleIndex;
        public List<GameConst.CardNumber> ThreeIndex;
        public List<GameConst.CardNumber> FourIndex;
    }

    //牌型结构体
    public struct RoleTypeIndexAvd {
        public List<RoleTypeAvd> SingleCards;
        public List<RoleTypeAvd> DoubleCards;
        public List<RoleTypeAvd> ThreeCards;
        public List<RoleTypeAvd> LianCards;
        public List<RoleTypeAvd> LianDuiCards;
        public List<RoleTypeAvd> FeijiCards;
        public List<RoleTypeAvd> BombCards;
        public List<RoleTypeAvd> RocketCards;
    }

    public struct RoleTypeAvd {
        public List<CardObject> cards;
        public int Weight;
        public RoleType MyRoleType;
    }


//    public RoleTypeIndexAvd RoleTypeIndexAvdCompute(List<CardObject> cards) {
//        RoleTypeIndexAvd roleTypeIndex;
//        roleTypeIndex.SingleCards = new List<RoleTypeAvd>();
//        roleTypeIndex.DoubleCards = new List<RoleTypeAvd>();
//        roleTypeIndex.ThreeCards = new List<RoleTypeAvd>();
//        roleTypeIndex.LianCards = new List<RoleTypeAvd>();
//        roleTypeIndex.LianDuiCards = new List<RoleTypeAvd>();
//        roleTypeIndex.FeijiCards = new List<RoleTypeAvd>();
//        roleTypeIndex.BombCards = new List<RoleTypeAvd>();
//
//        var vs = from CardObject c in cards group c by c.Card.cardNumber into g select new {g, num = g.Count()};
//        foreach (var v in vs) {
////			Debug.Log(v.g.Key + "=====" + v.num);
//            if (v.num == 4) {
//                roleTypeIndex.SingleCards.Add(v.g.Key);
//            } else if (v.num == 3) {
//                roleTypeIndex.ThreeIndex.Add(v.g.Key);
//            } else if (v.num == 2) {
//                roleTypeIndex.DoubleIndex.Add(v.g.Key);
//            } else if (v.num == 1) {
//                roleTypeIndex.SingleIndex.Add(v.g.Key);
//            }
//        }
//        return roleTypeIndex;
//    }

    #endregion

    #region 排序

    public void SortCards(List<CardObject> cards) {
        cards.Sort(ComparatorByNumber);
    }

    public void SortCards(List<GameConst.CardNumber> cards) {
        cards.Sort(ComparatorByNumber);
    }

    public void SortCardsByWeight(List<CardObject> cards) {
        cards.Sort(ComparatorByWeight);
    }


    private int ComparatorByWeight(CardObject card1, CardObject card2) {
        int result = 1;

        var card1Weight = card1.Card.cardWeight;
        var card2Weight = card2.Card.cardWeight;

        if (card1Weight > card2Weight) {
            result = 1;
        } else if (card1Weight < card2Weight) {
            result = -1;
        } else {
            result = 1;
        }

        return result;
    }

    private int ComparatorByNumber(CardObject card1, CardObject card2) {
        int result = 1;

        var card1Weight = card1.Card.cardNumber;
        var card2Weight = card2.Card.cardNumber;

        if (card1Weight > card2Weight) {
            result = 1;
        } else if (card1Weight < card2Weight) {
            result = -1;
        } else {
            result = 1;
        }

        return result;
    }

    private int ComparatorByNumber(GameConst.CardNumber card1, GameConst.CardNumber card2) {
        int result = 1;

        var card1Number = card1;
        var card2Number = card2;

        if (card1Number > card2Number) {
            result = 1;
        } else if (card1Number < card2Number) {
            result = -1;
        } else {
            result = 1;
        }

        return result;
    }

    #endregion

    #region 工具方法


    private int GetPrePlayer(Player currentPlayer) {
        var playerId = _players.FindIndex(p => p.Position == currentPlayer.Position);
        int preId1 = 1;
        switch (playerId) {
            case 0:
                preId1 = 2;
                break;
            case 1:
                preId1 = 0;
                break;
            case 2:
                preId1 = 1;
                break;
        }
        int preId2 = 2;
        switch (playerId) {
            case 0:
                preId2 = 1;
                break;
            case 1:
                preId2 = 2;
                break;
            case 2:
                preId2 = 0;
                break;
        }
        Player preplayer1 = _players[preId1];
        Player preplayer2 = _players[preId2];

//        //1 如果次数都为0，则是第一次开始出牌
//        if (currentPlayer.ChuPaiCishu == 0 && preplayer1.ChuPaiCishu == 0 && preplayer2.ChuPaiCishu == 0) {
//            return -1;
//        }
//        //2 如果前面两个次数为0，this不为0，则this最大，再开始出牌
//        if (currentPlayer.ChuPaiCishu > 0 && preplayer1.ChuPaiCishu <= 0 && preplayer2.ChuPaiCishu <= 0) {
//            return -1;
//        }
//        //3 前面两个至少有一个不为0，都可以获得pre
//        if (currentPlayer.ChuPaiCishu != -1) {
//            if (preplayer1.ChuPaiCishu > 0) {
//                return preId1;
//            }
//
//            if (preplayer2.ChuPaiCishu > 0) {
//                return preId2;
//            }
//        }
//        return -2;
        if (currentPlayer.PlayedStatus == Player.PlayerStatus.Passed
            && preplayer1.PlayedStatus == Player.PlayerStatus.Passed
            && preplayer2.PlayedStatus == Player.PlayerStatus.Passed) {
            return -1;
        }
        if (currentPlayer.PlayedStatus == Player.PlayerStatus.Played
            && preplayer1.PlayedStatus == Player.PlayerStatus.Passed
            && preplayer2.PlayedStatus == Player.PlayerStatus.Passed) {
            return -1;
        }
        if (currentPlayer.PlayedStatus == Player.PlayerStatus.Passed) {
            if (preplayer1.PlayedStatus == Player.PlayerStatus.Played) {
                return preId1;
            }
            if (preplayer2.PlayedStatus == Player.PlayerStatus.Played) {
                return preId2;
            }
        }
        if (currentPlayer.PlayedStatus == Player.PlayerStatus.Played) {
            if (preplayer1.PlayedStatus == Player.PlayerStatus.Played) {
                return preId1;
            }
            if (preplayer2.PlayedStatus == Player.PlayerStatus.Played) {
                return preId2;
            }
        }
        return -2;
    }

    private RoleType GetCardType(List<CardObject> readyPlayCards) {
        RoleType cardType = RoleType.ErrorCard;
        string talk = "不能出啊";
        if (readyPlayCards != null) {
            if (IsDan(readyPlayCards)) {
                cardType = RoleType.SingleCard;
                talk = "一张" + (GetNumber(readyPlayCards[0]));
            } else if (IsDuiWang(readyPlayCards)) {
                cardType = RoleType.KingBombCard;
                talk = "王炸";
            } else if (IsDuiZi(readyPlayCards)) {
                cardType = RoleType.DoubleCard;
                talk = "对" + (GetNumber(readyPlayCards[0]));
            } else if (IsZhaDan(readyPlayCards)) {
                cardType = RoleType.BombCard;
                talk = "炸弹!";
            } else if (IsSanDaiYi(readyPlayCards) != -1) {
                cardType = RoleType.ThreeOneCard;
                talk = "三带一";
            } else if (IsSanDaiEr(readyPlayCards) != -1) {
                cardType = RoleType.ThreeTwoCard;
                talk = "三带二";
            } else if (IsSanBuDai(readyPlayCards)) {
                cardType = RoleType.ThreeCard;
                talk = "三张" + (GetNumber(readyPlayCards[0]));
            } else if (IsShunZi(readyPlayCards)) {
                cardType = RoleType.LianCard;
                talk = "顺子";
            } else if (IsLianDui(readyPlayCards)) {
                cardType = RoleType.LianDuiCard;
                talk = "连对";
            } else if (IsSiDaiEr(readyPlayCards)) {
                cardType = RoleType.FourTwoCard;
                talk = "四带俩单";
            } else if (IsSiDaiErDui(readyPlayCards)) {
                cardType = RoleType.FourTwotwoCard;
                talk = "四带俩对";
            } else if (IsFeiJiBuDai(readyPlayCards)) {
                cardType = RoleType.FeijiNoCard;
                talk = "飞机没翅膀";
            } else if (IsFeiJiDaiDan(readyPlayCards)) {
                cardType = RoleType.FeijiSingleCard;
                talk = "飞机单翅膀";
            } else if (IsFeiJiDaiDui(readyPlayCards)) {
                cardType = RoleType.FeijiDoubleCard;
                talk = "飞机双翅膀！";
            }
        }
        Debug.Log("====       " + cardType + talk);
        return cardType;
    }


    private void ClearPlayerChuPai() {
//        _players[0].ChuPaiCishu = 0;
//        _players[1].ChuPaiCishu = 0;
//        _players[2].ChuPaiCishu = 0;

        _players[0].PlayedStatus = Player.PlayerStatus.Passed;
        _players[1].PlayedStatus = Player.PlayerStatus.Passed;
        _players[2].PlayedStatus = Player.PlayerStatus.Passed;

        _textController.HideAllTexts();
    }

    #endregion

    #region AI

    public List<CardObject> AvdAi(Player player) {
        int preplayerId = GetPrePlayer(player); //上一个玩家的id -1则重新开始出牌 -2则略过
        List<CardObject> prevCards; //上一个玩家出的牌
        List<CardObject> allCards; //我所有的牌
        RoleType preRoleType; //上一个玩一家出的牌型

        List<CardObject> aIpai = new List<CardObject>();
        //本次AI牌
        allCards = player.CurrentCards;
        if (preplayerId == -1) {
            ClearPlayerChuPai();

            Player.PlayerPosition playerPosition = player.Position + 2;
            int next = (int) playerPosition % _players.Count;

            aIpai = AvdAiPlayCards(preplayerId, true, player, _players[next], allCards, _players[next].CurrentCards,
                _players[next].PlayCards, RoleType.ErrorCard); //如果从AI开始出牌的话，出一个最小的单
        } else if (preplayerId == -2) {
            ClearPlayerChuPai();

            Player.PlayerPosition playerPosition = player.Position + 2;
            int next = (int) playerPosition % _players.Count;

            aIpai = AvdAiPlayCards(preplayerId, true, player, _players[next], allCards, _players[next].CurrentCards,
                _players[next].PlayCards, RoleType.ErrorCard); //如果从AI开始出牌的话，出一个最小的单

            var prevPlayer = _players[next];
            var prevPlayCards = prevPlayer.PlayCards;
            var prePlayRoleType = GetCardType(prevPlayCards);
            var myType = GetCardType(aIpai);

            if (prePlayRoleType != myType) {
                aIpai.Clear();

                Debug.Log("===================!!!!!!!!!!出牌有错误!!!!!!!!!!=================");
            }
        }

        prevCards = null;
        preRoleType = RoleType.ErrorCard;
        if (preplayerId >= 0) {
            //如果上家有牌出
            prevCards = _players[preplayerId].PlayCards;
            var prevCurrentCards = _players[preplayerId].CurrentCards;
            preRoleType = GetCardType(prevCards);
            aIpai = AvdAiPlayCards(preplayerId, false, player, _players[preplayerId], allCards, prevCurrentCards,
                prevCards,
                preRoleType); // AI选择要出的牌
//            aIpai = AiPlayCards(allCards, prevCards, preRoleType); // AI选择要出的牌
        }

        if (aIpai.Count > 0) {
            Debug.Log("电脑出牌，比上家大，或者是重新出");

//            player.ChuPaiCishu += 1;
            player.PlayedStatus = Player.PlayerStatus.Played;
            foreach (var cardObject in aIpai) {
                if (player.CurrentCards.Contains(cardObject)) {
                    player.CurrentCards.Remove(cardObject);
                }
            }
        } else {
//            player.ChuPaiCishu = -1;
            player.PlayedStatus = Player.PlayerStatus.Passed;
            Debug.Log("电脑没牌出");
            _textController.ShowTextOn(TextController.TextEnum.Pass, player);
        }

        if (player.Cards.Count == 0) {
            Debug.Log("没牌了, 赢了");
        }
        return aIpai;
    }

    private List<CardObject> AvdAiPlayCards(int preplayerId, bool firstPlay, Player mine, Player prev,
        List<CardObject> myAllCards,
        List<CardObject> prevCurrentCards, List<CardObject> prevCards, RoleType preRoleType) {
        List<CardObject> aiCards = new List<CardObject>();

        SortCardsByWeight(myAllCards);
        var myRoleTypeIndexAvd = AnalysisCards(myAllCards);
        SortCardsByWeight(prevCurrentCards);
        var prevRoleTypeIndexAvd = AnalysisCards(prevCurrentCards);

        if (firstPlay || prevCards.Count == 0 || preplayerId == -1) {
            if (myRoleTypeIndexAvd.FeijiCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.FeijiCards[0].cards;
                var tmpCards = new List<CardObject>();
                tmpCards.AddRange(aiCards);
                if (myRoleTypeIndexAvd.SingleCards.Count >= tmpCards.Count / 3) {
                    for (var i = 0; i < tmpCards.Count; i += 3) {
                        aiCards.AddRange(myRoleTypeIndexAvd.SingleCards[i / 3].cards);
                    }
                    return aiCards;
                }
                if (myRoleTypeIndexAvd.DoubleCards.Count >= tmpCards.Count / 3) {
                    for (var i = 0; i < tmpCards.Count; i += 3) {
                        aiCards.AddRange(myRoleTypeIndexAvd.DoubleCards[i / 3].cards);
                    }
                    return aiCards;
                }
                return aiCards;
            }
            if (myRoleTypeIndexAvd.LianDuiCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.LianDuiCards[0].cards;
                return aiCards;
            }
            if (myRoleTypeIndexAvd.LianCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.LianCards[0].cards;
                return aiCards;
            }
            if (myRoleTypeIndexAvd.ThreeCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.ThreeCards[0].cards;
                if (myRoleTypeIndexAvd.SingleCards.Count >= myRoleTypeIndexAvd.ThreeCards.Count) {
                    aiCards.AddRange(myRoleTypeIndexAvd.SingleCards[0].cards);
                    return aiCards;
                }
                if (myRoleTypeIndexAvd.DoubleCards.Count >= myRoleTypeIndexAvd.ThreeCards.Count) {
                    aiCards.AddRange(myRoleTypeIndexAvd.DoubleCards[0].cards);
                    return aiCards;
                }
                return aiCards;
            }
            if (myRoleTypeIndexAvd.DoubleCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.DoubleCards[0].cards;
                return aiCards;
            }
            if (myRoleTypeIndexAvd.SingleCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.SingleCards[0].cards;
                return aiCards;
            }
            if (myRoleTypeIndexAvd.BombCards.Count > 0) {
                aiCards = myRoleTypeIndexAvd.BombCards[0].cards;
            }
            return aiCards;
        } else {
            if (mine.IsLord) {
                //是地主
                aiCards = ChooseCards(true, myRoleTypeIndexAvd, prevRoleTypeIndexAvd, myAllCards, prevCards,
                    preRoleType);
            } else {
                //不是地主,看上家是不是地主
                if (prev.IsLord) {
                    //上家是地主
                    aiCards = ChooseCards(true, myRoleTypeIndexAvd, prevRoleTypeIndexAvd, myAllCards, prevCards,
                        preRoleType);
                    return aiCards;
                } else {
                    //上家不是地主,配合出牌
                    aiCards = ChooseCards(false, myRoleTypeIndexAvd, prevRoleTypeIndexAvd, myAllCards, prevCards,
                        preRoleType);
                    return aiCards;
                }
            }
        }

        //以上都不匹配
//        if (aiCards.Count == 0) {
//            aiCards = AiPlayCards(myAllCards, prevCards, preRoleType);
//        }

        return aiCards;
    }

    //分析什么牌型最多
    private RoleType WhatIsMax(RoleTypeIndexAvd roleTypeIndexAvd) {
        var rocketCardsCount = roleTypeIndexAvd.RocketCards.Count;
        var bombCardsCount = roleTypeIndexAvd.BombCards.Count;
        var threeCardsCount = roleTypeIndexAvd.ThreeCards.Count;
        var feijiCardsCount = roleTypeIndexAvd.FeijiCards.Count;
        var lianCardsCount = roleTypeIndexAvd.LianCards.Count;
        var lianDuiCardsCount = roleTypeIndexAvd.LianDuiCards.Count;
        var doubleCardsCount = roleTypeIndexAvd.DoubleCards.Count;
        var singleCardsCount = roleTypeIndexAvd.SingleCards.Count;

        var max = Math.Max(bombCardsCount, lianCardsCount);
        max = Math.Max(max, feijiCardsCount);
        max = Math.Max(max, lianDuiCardsCount);
        max = Math.Max(max, threeCardsCount);
        max = Math.Max(max, singleCardsCount);
        max = Math.Max(max, rocketCardsCount);
        max = Math.Max(max, doubleCardsCount);

        if (roleTypeIndexAvd.RocketCards.Count == max) {
            return RoleType.KingBombCard;
        }
        if (roleTypeIndexAvd.BombCards.Count == max) {
            return RoleType.BombCard;
        }
        if (roleTypeIndexAvd.LianCards.Count == max) {
            return RoleType.LianCard;
        }
        if (roleTypeIndexAvd.DoubleCards.Count == max) {
            return RoleType.DoubleCard;
        }
        if (roleTypeIndexAvd.FeijiCards.Count == max) {
            return RoleType.FeijiNoCard;
        }
        if (roleTypeIndexAvd.LianDuiCards.Count == max) {
            return RoleType.LianDuiCard;
        }
        if (roleTypeIndexAvd.ThreeCards.Count == max) {
            return RoleType.ThreeCard;
        }
        if (roleTypeIndexAvd.SingleCards.Count == max) {
            return RoleType.SingleCard;
        }
        return RoleType.SingleCard;
    }

    private RoleTypeIndexAvd AnalysisCards(List<CardObject> myAllCards) {
        RoleTypeIndexAvd roleTypeIndexAvd = new RoleTypeIndexAvd();
        roleTypeIndexAvd.SingleCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.DoubleCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.ThreeCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.LianCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.LianDuiCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.FeijiCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.BombCards = new List<RoleTypeAvd>();
        roleTypeIndexAvd.RocketCards = new List<RoleTypeAvd>();

        if (myAllCards.Count == 1) {
            var roleTypeAvd = new RoleTypeAvd();
            roleTypeAvd.cards = new List<CardObject>();
            roleTypeAvd.cards.Add(myAllCards[0]);
            roleTypeAvd.Weight = (int) myAllCards[0].Card.cardWeight;
            roleTypeIndexAvd.SingleCards.Add(roleTypeAvd);
            return roleTypeIndexAvd;
        }

        var remainCards = myAllCards.GetRange(0, myAllCards.Count);

        var rocketCards = FindRocketCards(remainCards);
        if (rocketCards.Count == 2) {
            var roleTypeAvd = new RoleTypeAvd();
            roleTypeAvd.cards = new List<CardObject>();
            foreach (var rocketCard in rocketCards) {
                roleTypeAvd.Weight += (int) rocketCard.Card.cardWeight;
                roleTypeAvd.cards.Add(rocketCard);
            }
            roleTypeIndexAvd.RocketCards.Add(roleTypeAvd);
        }
        WipeCards(remainCards, rocketCards);
        var s1 = rocketCards.Aggregate("火箭牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s1);


        var bombCards = FindBombCards(remainCards);
        if (bombCards.Count >= 4) {
            SortCardsByWeight(bombCards);
            var cardIndexCompute = CardIndexCompute(bombCards);
            for (int i = 0; i < cardIndexCompute.FourIndex.Count; i++) {
                var roleTypeAvd = new RoleTypeAvd();
                roleTypeAvd.cards = bombCards.GetRange(i * 4, 4);
                foreach (var co in roleTypeAvd.cards) {
                    roleTypeAvd.Weight += (int) co.Card.cardWeight;
                }
                roleTypeIndexAvd.BombCards.Add(roleTypeAvd);
            }
        }
        WipeCards(remainCards, bombCards);
        var s2 = bombCards.Aggregate("炸弹牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s2);

        //复制一份没有火箭和炸弹的牌,用来计算顺子
        var noBombCards = remainCards.GetRange(0, remainCards.Count);

        var sanTiaoCards = FindSanTiaoCards(remainCards);
        if (sanTiaoCards.Count >= 3) {
            SortCardsByWeight(sanTiaoCards);
            var cardIndexCompute = CardIndexCompute(sanTiaoCards);
            for (int i = 0; i < cardIndexCompute.ThreeIndex.Count; i++) {
                var roleTypeAvd = new RoleTypeAvd();
                roleTypeAvd.cards = sanTiaoCards.GetRange(i * 3, 3);
                foreach (var co in roleTypeAvd.cards) {
                    roleTypeAvd.Weight += (int) co.Card.cardWeight;
                }
                roleTypeIndexAvd.ThreeCards.Add(roleTypeAvd);
            }
        }
        WipeCards(remainCards, sanTiaoCards);
        var s3 = sanTiaoCards.Aggregate("三条牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s3);

        List<CardObject> feiJiCards = new List<CardObject>();
        if (sanTiaoCards.Count >= 6) {
            feiJiCards = FindFeiJiCards(sanTiaoCards);
            if (feiJiCards.Count >= 6) {
                SortCardsByWeight(feiJiCards);
                // 333 444 555
                // 777 888
                // Count 15
                // ThreeIndex.Count 5
                for (var i = feiJiCards.Count - 1; i >= 0; i--) {
                    var co = feiJiCards[i];
                    List<CardObject> tmp = new List<CardObject> {co};
                    for (var j = i; j >= 0; j--) {
                        if (j == i) {
                            continue;
                        }
                        var nextCo = feiJiCards[j];
                        tmp.Add(nextCo);
                        if (tmp.Count >= 6) {
                            if (IsFeiJiBuDai(tmp)) {
                                var roleTypeAvd = new RoleTypeAvd();
                                roleTypeAvd.cards = tmp.GetRange(0, tmp.Count);
                                foreach (var co2 in roleTypeAvd.cards) {
                                    roleTypeAvd.Weight += (int) co2.Card.cardWeight;
                                }
                                roleTypeIndexAvd.FeijiCards.Add(roleTypeAvd);

                                if (roleTypeIndexAvd.FeijiCards.Count > 1) {
                                    for (var i1 = 0; i1 < roleTypeIndexAvd.FeijiCards.Count; i1++) {
                                        foreach (var cardObject in tmp) {
                                            if (roleTypeIndexAvd.FeijiCards[i1].cards.Contains(cardObject)) {
                                                var feijiCard = roleTypeIndexAvd.FeijiCards[i1];
                                                feijiCard.cards = tmp.GetRange(0, tmp.Count);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            WipeCards(remainCards, feiJiCards);
            var s4 = feiJiCards.Aggregate("飞机牌====", (current, v) => current + (v.Card.cardNumber + " "));
            Debug.Log(s4);
        }

        //三条少的话,找连对
        var lianduiCards = FindLianDuiCards(noBombCards);
        if (lianduiCards.Count >= 6) {
            SortCardsByWeight(lianduiCards);
            // 333 444 555
            // 777 888
            // Count 15
            // ThreeIndex.Count 5
            for (var i = lianduiCards.Count - 1; i >= 0; i--) {
                var co = lianduiCards[i];
                List<CardObject> tmp = new List<CardObject> {co};
                for (var j = i; j >= 0; j--) {
                    if (j == i) {
                        continue;
                    }
                    var nextCo = lianduiCards[j];
                    tmp.Add(nextCo);
                    if (tmp.Count >= 6) {
                        if (IsLianDui(tmp)) {
                            var roleTypeAvd = new RoleTypeAvd();
                            roleTypeAvd.cards = tmp.GetRange(0, tmp.Count);
                            foreach (var co2 in roleTypeAvd.cards) {
                                roleTypeAvd.Weight += (int) co2.Card.cardWeight;
                            }
                            roleTypeIndexAvd.LianDuiCards.Add(roleTypeAvd);

                            if (roleTypeIndexAvd.LianDuiCards.Count > 1) {
                                for (var i1 = 0; i1 < roleTypeIndexAvd.LianDuiCards.Count; i1++) {
                                    foreach (var cardObject in tmp) {
                                        if (roleTypeIndexAvd.LianDuiCards[i1].cards.Contains(cardObject)) {
                                            var Card = roleTypeIndexAvd.LianDuiCards[i1];
                                            Card.cards = tmp.GetRange(0, tmp.Count);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        WipeCards(remainCards, lianduiCards);
        var s5 = lianduiCards.Aggregate("连对牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s5);

        if (lianduiCards.Count > 0) {
            //TODO 如果有连对 而且连对的牌里用到了三条的牌
            Debug.Log("如果有连对");
            foreach (var sanTiaoCard in sanTiaoCards) {
                if (lianduiCards.Contains(sanTiaoCard)) {
                    lianduiCards.Clear();
                    Debug.Log("如果有连对 而且连对的牌里用到了三条的牌");
                    break;
                }
            }
        }

        var shunZiCards = FindShunZiCards(noBombCards);
        if (shunZiCards.Count >= 5) {
            SortCardsByWeight(shunZiCards);
            // 333 444 555
            // 777 888
            // Count 15
            // ThreeIndex.Count 5
            for (var i = shunZiCards.Count - 1; i >= 0; i--) {
                var co = shunZiCards[i];
                List<CardObject> tmp = new List<CardObject> {co};
                for (var j = i; j >= 0; j--) {
                    if (j == i) {
                        continue;
                    }
                    var nextCo = shunZiCards[j];
                    tmp.Add(nextCo);
                    if (tmp.Count >= 5) {
                        if (IsShunZi(tmp)) {
                            var roleTypeAvd = new RoleTypeAvd();
                            roleTypeAvd.cards = tmp.GetRange(0, tmp.Count);
                            foreach (var co2 in roleTypeAvd.cards) {
                                roleTypeAvd.Weight += (int) co2.Card.cardWeight;
                            }
                            roleTypeIndexAvd.LianCards.Add(roleTypeAvd);

                            if (roleTypeIndexAvd.LianCards.Count > 1) {
                                for (var i1 = 0; i1 < roleTypeIndexAvd.LianCards.Count; i1++) {
                                    foreach (var cardObject in tmp) {
                                        if (roleTypeIndexAvd.LianCards[i1].cards.Contains(cardObject)) {
                                            var Card = roleTypeIndexAvd.LianCards[i1];
                                            Card.cards = tmp.GetRange(0, tmp.Count);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        WipeCards(remainCards, shunZiCards);
        var s6 = shunZiCards.Aggregate("顺子牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s6);

        //如果只有一个三条,有顺子, 顺子和三条有重复的牌
        if (sanTiaoCards.Count <= 3 && shunZiCards.Count != 0) {
            foreach (var shunZiCard in shunZiCards) {
                if (sanTiaoCards.Contains(shunZiCard)) {
                    sanTiaoCards.Clear();
                    break;
                }
            }
        }

        var duiZiCards = FindDuiZiCards(remainCards);
        if (duiZiCards.Count >= 2) {
            SortCardsByWeight(duiZiCards);
            for (var i = 0; i < duiZiCards.Count; i += 2) {
                var roleTypeAvd = new RoleTypeAvd();
                roleTypeAvd.cards = new List<CardObject>();
                roleTypeAvd.cards.Add(duiZiCards[i]);
                roleTypeAvd.cards.Add(duiZiCards[i + 1]);
                foreach (var co2 in roleTypeAvd.cards) {
                    roleTypeAvd.Weight += (int) co2.Card.cardWeight;
                }
                roleTypeIndexAvd.DoubleCards.Add(roleTypeAvd);
            }
        }
        WipeCards(remainCards, duiZiCards);
        var s7 = duiZiCards.Aggregate("对子牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s7);

        var s8 = remainCards.Aggregate("剩下的牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s8);

        SortCardsByWeight(remainCards);
        for (var i = 0; i < remainCards.Count; i++) {
            var roleTypeAvd = new RoleTypeAvd();
            roleTypeAvd.cards = new List<CardObject>();
            roleTypeAvd.cards.Add(remainCards[i]);
            foreach (var co2 in roleTypeAvd.cards) {
                roleTypeAvd.Weight += (int) co2.Card.cardWeight;
            }
            roleTypeIndexAvd.SingleCards.Add(roleTypeAvd);
        }

        int shouShu = 0;
        if (rocketCards.Count > 0) {
            shouShu += 1;
            Debug.Log("有火箭,手数:" + shouShu);
        }
        if (bombCards.Count > 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(bombCards);
            for (var i = 0; i < roleTypeIndex.FourIndex.Count; i++) {
                shouShu += 1;
                Debug.Log("有炸弹" + roleTypeIndex.FourIndex[i] + " 手数:" + shouShu);
            }
        }
        if (sanTiaoCards.Count > 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(sanTiaoCards);
            for (var i = 0; i < roleTypeIndex.ThreeIndex.Count; i++) {
                shouShu += 1;
                Debug.Log("有三条" + roleTypeIndex.ThreeIndex[i] + " 手数:" + shouShu);
            }
        }
        if (feiJiCards.Count > 0) {
            //333 444
            //666 777

            //333 444 666 777
            RoleTypeIndex roleTypeIndex = CardIndexCompute(feiJiCards);
            for (var i = 0; i < roleTypeIndex.ThreeIndex.Count - 1; i++) {
                if (roleTypeIndex.ThreeIndex[i] - roleTypeIndex.ThreeIndex[i + 1] != 1
                    || roleTypeIndex.ThreeIndex[i] - roleTypeIndex.ThreeIndex[i + 1] != -1) {
                    Debug.Log("有飞机" + roleTypeIndex.ThreeIndex[i] + "和" + roleTypeIndex.ThreeIndex[i + 1] + " 手数:" +
                              shouShu);
                    continue;
                }
                shouShu += 1;
            }
            shouShu += 1;
        }
        if (lianduiCards.Count > 0) {
            //33 44 55
            //77 88 99

            //33 44 55 77 88 99
            RoleTypeIndex roleTypeIndex = CardIndexCompute(lianduiCards);
            for (var i = 0; i < roleTypeIndex.DoubleIndex.Count - 1; i++) {
                if (roleTypeIndex.DoubleIndex[i] - roleTypeIndex.DoubleIndex[i + 1] != 1
                    || roleTypeIndex.DoubleIndex[i] - roleTypeIndex.DoubleIndex[i + 1] != -1) {
                    Debug.Log("有连对" + roleTypeIndex.DoubleIndex[i] + "和" + roleTypeIndex.DoubleIndex[i + 1] + " 手数:" +
                              shouShu);
                    continue;
                }
                shouShu += 1;
            }
            shouShu += 1;
        }
        if (shunZiCards.Count > 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(shunZiCards);
            for (var i = 0; i < roleTypeIndex.SingleIndex.Count - 1; i++) {
                if (roleTypeIndex.SingleIndex[i] - roleTypeIndex.SingleIndex[i + 1] != 1
                    || roleTypeIndex.SingleIndex[i] - roleTypeIndex.SingleIndex[i + 1] != -1) {
                    Debug.Log("有顺子" + roleTypeIndex.SingleIndex[i] + "和" + roleTypeIndex.SingleIndex[i + 1] + " 手数:" +
                              shouShu);
                    continue;
                }
                shouShu += 1;
            }
            shouShu += 1;
        }
        if (duiZiCards.Count > 0) {
            RoleTypeIndex roleTypeIndex = CardIndexCompute(duiZiCards);
            for (var i = 0; i < roleTypeIndex.DoubleIndex.Count; i++) {
                shouShu += 1;
                Debug.Log("有对子" + roleTypeIndex.DoubleIndex[i] + " 手数:" + shouShu);
            }
        }
        if (remainCards.Count > 0) {
            foreach (var remainCard in remainCards) {
                shouShu += 1;
                Debug.Log("有单子" + remainCard.Card.cardNumber + ",手数:" + shouShu);
            }
        }

        Debug.Log("方案二总手数:" + shouShu);
        return roleTypeIndexAvd;
    }

    private List<CardObject> FindFeiJiCards(List<CardObject> cards) {
        List<CardObject> feijiList = new List<CardObject>();

        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject> {co};
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                tmp.Add(nextCo);
                if (tmp.Count >= 6) {
                    if (IsFeiJiBuDai(tmp)) {
                        feijiList.AddRange(tmp);
                    }
                }
            }
        }
        var vs2 = feijiList.GroupBy(c => c.Card.cardNumber);
        var cardObjects = vs2.SelectMany(group => @group);
        var objects = cardObjects.Distinct(new CardNumberComparer()).ToList();
        List<CardObject> tmps = cards.GetRange(0, cards.Count);
        WipeCards(tmps, objects);
        foreach (var co in tmps) {
            foreach (var o in objects) {
                var flag = false;
                foreach (var cardNumber in CardIndexCompute(objects).ThreeIndex) {
                    if (cardNumber == o.Card.cardNumber || cardNumber == co.Card.cardNumber) {
                        flag = true;
                    }
                }
                if (flag) {
                    break;
                }
                if (co.Card.cardNumber == o.Card.cardNumber
//                    && co.Card.cardSuit != o.Card.cardSuit
                ) {
                    objects.Add(co);
                    break;
                }
            }
        }
        return objects;
//        return feijiList;
    }

    private void WipeCards(List<CardObject> source, List<CardObject> some) {
        foreach (var card in some) {
            if (source.Contains(card)) {
                source.Remove(card);
            }
        }
    }

    private List<CardObject> FindRocketCards(List<CardObject> cards) {
        List<CardObject> rocketCards = new List<CardObject>();
        if (cards.Count >= 2) {
            if (cards[cards.Count - 1].Card.cardNumber == GameConst.CardNumber.BigJoker &&
                cards[cards.Count - 2].Card.cardNumber == GameConst.CardNumber.SmallJoker) {
                rocketCards.Add(cards[cards.Count - 1]);
                rocketCards.Add(cards[cards.Count - 2]);
            }
        }
        return rocketCards;
    }

    private List<CardObject> FindDuiZiCards(List<CardObject> cards) {
        List<CardObject> duiZiList = new List<CardObject>();
        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject> {co};
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                tmp.Add(nextCo);
                if (tmp.Count == 2) {
                    if (IsDuiZi(tmp)) {
                        duiZiList.AddRange(tmp);
                    }
                }
            }
        }
//        var vs2 = duiZiList.GroupBy(c => c.Card.cardNumber);
//        var cardObjects = vs2.SelectMany(group => @group);
//        return cardObjects.Distinct(new CardNumberComparer()).ToList();
        return duiZiList;
    }

    private List<CardObject> FindSanTiaoCards(List<CardObject> cards) {
        List<CardObject> sanList = new List<CardObject>();
//        var cCards = cards.GetRange(0, cards.Count);
        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject> {co};
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                tmp.Add(nextCo);
                if (tmp.Count == 3) {
                    if (IsSanBuDai(tmp)) {
//                        Debug.Log("一个三条");
//                        foreach (var cardObject in tmp) {
//                            cCards.RemoveAll(p => p.Card.cardNumber == cardObject.Card.cardNumber);
//                        }
                        sanList.AddRange(tmp);
                    }
                }
            }
        }
//        var vs2 = sanList.GroupBy(c => c.Card.cardNumber);
//        var cardObjects = vs2.SelectMany(group => @group);
//        return cardObjects.Distinct(new CardNumberComparer()).ToList();
        return sanList;
    }

    private List<CardObject> FindBombCards(List<CardObject> cards) {
        List<CardObject> bombList = new List<CardObject>();
//        var cCards = cards.GetRange(0, cards.Count);
        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject> {co};
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                tmp.Add(nextCo);
                if (tmp.Count == 4) {
                    if (IsZhaDan(tmp)) {
//                        Debug.Log("一个炸弹");
                        bombList.AddRange(tmp);
                    }
                }
            }
        }
//        var vs2 = bombList.GroupBy(c => c.Card.cardNumber);
//        var cardObjects = vs2.SelectMany(group => @group);
//        return cardObjects.Distinct(new CardNumberComparer()).ToList();
        return bombList;
    }

    private List<CardObject> FindNoneCards(List<CardObject> cards) {
        var tmpCards = new List<CardObject>();
        tmpCards.AddRange(cards);

        var findShunZi = FindShunZiCards(cards);
        var findLianDui = FindLianDuiCards(cards);
        var s = findLianDui.Aggregate("findLianDui====", (current, v) => current + (v.Card.cardNumber + " "));
        string s2 = "findShunZi====";
        foreach (var v in findShunZi) {
            s2 += v.Card.cardNumber + " ";
        }
        Debug.Log(s);
        Debug.Log(s2);

        foreach (var cardObject in cards) {
            foreach (var o in findShunZi) {
                if (cardObject.Card.cardNumber == o.Card.cardNumber) {
                    tmpCards.Remove(cardObject);
                }
            }
            foreach (var o in findLianDui) {
                if (cardObject.Card.cardNumber == o.Card.cardNumber) {
                    tmpCards.Remove(cardObject);
                }
            }
        }
        var s3 = tmpCards.Aggregate("一副牌中只能组成一种牌型的牌====", (current, v) => current + (v.Card.cardNumber + " "));
        Debug.Log(s3);
        return tmpCards;
    }

    private List<CardObject> FindLianDuiCards(List<CardObject> cards) {
        List<CardObject> shunZiList = new List<CardObject>();
        var cCards = cards.GetRange(0, cards.Count);
        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject> {co};
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                tmp.Add(nextCo);
                if (tmp.Count >= 6) {
                    if (IsLianDui(tmp)) {
//                        Debug.Log("一个连对");
                        foreach (var cardObject in tmp) {
                            cCards.RemoveAll(p => p.Card.cardNumber == cardObject.Card.cardNumber);
                        }
                        shunZiList.AddRange(tmp);
                    }
                }
            }
        }
        var vs2 = shunZiList.GroupBy(c => c.Card.cardNumber);
        var cardObjects = vs2.SelectMany(group => @group);
        var objects = cardObjects.Distinct(new CardNumberComparer()).ToList();
        List<CardObject> tmps = cards.GetRange(0, cards.Count);
        WipeCards(tmps, objects);
        foreach (var co in tmps) {
            foreach (var o in objects) {
                var flag = false;
                foreach (var cardNumber in CardIndexCompute(objects).DoubleIndex) {
                    if (cardNumber == o.Card.cardNumber || cardNumber == co.Card.cardNumber) {
                        flag = true;
                    }
                }
                if (flag) {
                    break;
                }
                if (co.Card.cardNumber == o.Card.cardNumber) {
                    objects.Add(co);
                    break;
                }
            }
        }
        return objects;
//        return shunZiList;
    }

    private List<CardObject> FindShunZiCards(List<CardObject> cards) {
        //找到了顺子
        List<CardObject> shunZiList = new List<CardObject>();
        var cCards = cards.GetRange(0, cards.Count);
        for (var i = cards.Count - 1; i >= 0; i--) {
            var co = cards[i];
            List<CardObject> tmp = new List<CardObject>();
            tmp.Add(co);
            for (var j = i; j >= 0; j--) {
                if (j == i) {
                    continue;
                }
                var nextCo = cards[j];
                if (nextCo.Card.cardNumber == co.Card.cardNumber) {
                    continue;
                }
                var f = false;
                foreach (var cardObject in tmp) {
                    if (cardObject.Card.cardNumber == nextCo.Card.cardNumber) {
                        f = true;
                        break;
                    }
                }
                if (f) {
                    continue;
                }
                tmp.Add(nextCo);
                if (tmp.Count >= 5) {
                    if (IsShunZi(tmp)) {
//                        Debug.Log("一个顺子");
                        foreach (var cardObject in tmp) {
                            cCards.RemoveAll(p => p.Card.cardNumber == cardObject.Card.cardNumber);
                        }
                        shunZiList.AddRange(tmp);
                    }
                }
            }
        }

//        var vs2 =from o in shunZiList group o by o.Card.cardNumber into g select new {g};
        var vs2 = shunZiList.GroupBy(c => c.Card.cardNumber);
        var cardObjects = vs2.SelectMany(group => @group);
        return cardObjects.Distinct(new CardNumberComparer()).ToList();
//        return shunZiList;
    }


    public List<CardObject> AiPlay(Player player) {
        int preplayerId = GetPrePlayer(player); //上一个玩家的id -1则重新开始出牌 -2则略过
        List<CardObject> prevCards; //上一个玩家出的牌
        List<CardObject> allCards; //我所有的牌
        RoleType preRoleType; //上一个玩一家出的牌型

        List<CardObject> aIpai = new List<CardObject>();
        //本次AI牌
        allCards = player.Cards;
        if (preplayerId == -1) {
            ClearPlayerChuPai();
            aIpai.Add(allCards[allCards.Count - 1]); //如果从AI开始出牌的话，出一个最小的单
        } else if (preplayerId == -2) {
            //如果为-2 没牌可出
        }

        prevCards = null;
        preRoleType = RoleType.ErrorCard;
        if (preplayerId >= 0) {
            //如果上家有牌出
            prevCards = _players[preplayerId].PlayCards;
            preRoleType = GetCardType(prevCards);
            aIpai = AiPlayCards(allCards, prevCards, preRoleType); // AI选择要出的牌
        }
//        Debug.Log("上家是" + _players[preplayerId%_players.Count].Position);


        if (aIpai.Count > 0) {
            Debug.Log("电脑出牌，比上家大，或者是重新出");
//            player.ChuPaiCishu += 1;
            player.PlayedStatus = Player.PlayerStatus.Played;
            foreach (var cardObject in aIpai) {
                if (player.CurrentCards.Contains(cardObject)) {
                    player.CurrentCards.Remove(cardObject);
                }
            }
        } else {
//            player.ChuPaiCishu = -1;
            player.PlayedStatus = Player.PlayerStatus.Passed;
            Debug.Log("电脑没牌出");
            _textController.ShowTextOn(TextController.TextEnum.Pass, player);
        }

        //isFinished
        if (player.Cards.Count == 0) {
            Debug.Log("没牌了, 赢了");
        }
        return aIpai;
    }

    public List<CardObject> Tips(Player player) {
        int preplayerId = GetPrePlayer(player); //上一个玩家的id -1则重新开始出牌 -2则略过

        List<CardObject> aIpai = new List<CardObject>();
        //本次AI牌
        var allCards = player.Cards;
        if (preplayerId == -1) {
            ClearPlayerChuPai();

            Player.PlayerPosition playerPosition = player.Position + 2;
            int next = (int) playerPosition % _players.Count;

            aIpai = AvdAiPlayCards(preplayerId, true, player, _players[next], allCards, _players[next].CurrentCards,
                _players[next].PlayCards, RoleType.ErrorCard);
        }

        if (preplayerId >= 0) {
            //如果上家有牌出
            var prevCards = _players[preplayerId].PlayCards;
            var preRoleType = GetCardType(prevCards);

            var prevCurrentCards = _players[preplayerId].CurrentCards;
            aIpai = AvdAiPlayCards(preplayerId, false, player, _players[preplayerId], allCards, prevCurrentCards,
                prevCards,preRoleType);
        }

//        Player.PlayerPosition playerPosition = player.Position + 1;
//        int next = (int) playerPosition % _players.Count;
//        var prevPlayer = _players[next];
//
//        var prevPlayCards = prevPlayer.PlayCards;
//        var prePlayRoleType = GetCardType(prevPlayCards);
//
//        var myType = GetCardType(aIpai);
//
//        if (prePlayRoleType != myType) {
//            Debug.Log("===================!!!!!!!!!!出牌有错误!!!!!!!!!!=================");
//            aIpai.Clear();
//        }

        return aIpai;
    }

    /**
     * AIchupai
     *
     * @param myCards
     *            我所有的牌 *
     * @param prevCards
     *            上家的牌
     * @param prevCardType
     *            上家牌的类型
     * @return 可以出牌，返回true；否则，返回false。
     */
    private List<CardObject> AiPlayCards(List<CardObject> myCards, List<CardObject> prevCards, RoleType preRoleType) {
        List<CardObject> AIpai = new List<CardObject>();
        // 我的牌和上家的牌都不能为null
        if (myCards == null || prevCards == null) {
            return AIpai;
        }

        if (preRoleType == null) {
            Debug.Log("上家出的牌不合法，所以不能出。");
            return AIpai;
        }

        // 默认情况：上家和自己想出的牌都符合规则
        SortCards(myCards); // 对牌排序
        SortCards(prevCards); // 对牌排序

        // 上一首牌的个数
        int prevSize = prevCards.Count;
        int mySize = myCards.Count;

        // 我先出牌，上家没有牌
        if (prevSize == 0 && mySize != 0) {
            AIpai.Add(myCards[0]);
            return AIpai;
        }

        // 集中判断是否王炸，免得多次判断王炸
        if (preRoleType == RoleType.KingBombCard) {
            Debug.Log("上家王炸，肯定不能出。");
            return AIpai;
        }

        GameConst.CardWeight prevWeight = GetWeight(prevCards[0]);

        // 比较2家的牌，主要有2种情况，1.我出和上家一种类型的牌，即对子管对子；
        // 2.我出炸弹，此时，和上家的牌的类型可能不同
        // 王炸的情况已经排除

        // 上家出单
        if (preRoleType == RoleType.SingleCard) {
            // 一张牌可以大过上家的牌
            Debug.Log("上家出的单");
            //			for (int i = mySize - 1; i >= 0; i--) {
            for (int i = mySize - 1; i >= 0; i--) {
                GameConst.CardWeight grade = GetWeight(myCards[i]);
                if (grade > prevWeight) {
                    // 只要有1张牌可以大过上家，则返回true
                    AIpai.Add(myCards[i]);
                    Debug.Log("AI chu dan 11111111====" + myCards[i]);
                    return AIpai;
                }
            }
        }
        // 上家出对子
        else if (preRoleType == RoleType.DoubleCard) {
            // 2张牌可以大过上家的牌
            for (int i = 0; i < mySize - 2; i++) {
                GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);

                if (grade0 == grade1) {
                    if (grade0 > prevWeight) {
                        // 只要有1对牌可以大过上家，则返回true
                        AIpai.Add(myCards[i]);
                        AIpai.Add(myCards[i + 1]);
                        return AIpai;
                    }
                }
            }
        }
        // 上家出3不带
        else if (preRoleType == RoleType.ThreeCard) {
            // 3张牌可以大过上家的牌
            Debug.Log("sanzhang!!!");
            for (int i = 0; i < mySize - 3; i++) {
                GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);

                if (grade0 == grade1 && grade0 == grade2) {
                    if (grade0 > prevWeight) {
                        // 只要3张牌可以大过上家，则返回true
                        AIpai.Add(myCards[i]);
                        AIpai.Add(myCards[i + 1]);
                        AIpai.Add(myCards[i + 2]);
                        return AIpai;
                    }
                }
            }
        }
        // 上家出3带1
        else if (preRoleType == RoleType.ThreeOneCard) {
            // 3带1 3不带 比较只多了一个判断条件
            if (mySize < 4) {
                //				return AIpai;
            }

            // 3张牌可以大过上家的牌
            for (int i = 0; i < mySize - 3; i++) {
                GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);

                prevWeight = GetWeight(prevCards[1]);
                if (grade0 == grade1 && grade0 == grade2) {
//CardType_Index carT_index = CardIndexCompute (myCards);
                    if (grade0 > prevWeight) {
                        // 只要3张牌可以大过上家，则返回true
                        //						AIpai.Add(myCards[i]);
                        //						AIpai.Add(myCards[i-1]);
                        //						AIpai.Add(myCards[i-2]);
                        //						AIpai.Add(myCards[i-3]);
                        //						CardType_Index carT_index = CardIndexCompute (myCards);
                        List<CardObject> MYcards = new List<CardObject>();
                        foreach (CardObject a in myCards) {
                            MYcards.Add(a);
                        }
                        MYcards.Remove(myCards[i]);
                        MYcards.Remove(myCards[i + 1]);
                        MYcards.Remove(myCards[i + 2]);
                        SortCards(MYcards);
                        for (int j = 0; j < MYcards.Count - 1; j++) {
                            if (GetWeight(MYcards[j]) != grade0) {
                                AIpai.Add(myCards[i]);
                                AIpai.Add(myCards[i + 1]);
                                AIpai.Add(myCards[i + 2]);
                                AIpai.Add(MYcards[j]);
                                return AIpai;
                            }
                        }

                        return AIpai;
                    }
                }
            }
        }
        // 上家出3带2
        else if (preRoleType == RoleType.ThreeTwoCard) {
            // 3带2
            if (mySize < 5) {
                //				return AIpai;
            }

            // 3张牌可以大过上家的牌
            for (int i = 0; i < mySize - 3; i++) {
                GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);

                prevWeight = GetWeight(prevCards[2]);
                if (grade0 == grade1 && grade0 == grade2) {
//CardType_Index carT_index = CardIndexCompute (myCards);
                    if (grade0 > prevWeight) {
                        // 只要3张牌可以大过上家，则返回true
                        //						AIpai.Add(myCards[i]);
                        //						AIpai.Add(myCards[i-1]);
                        //						AIpai.Add(myCards[i-2]);
                        //						AIpai.Add(myCards[i-3]);
                        //						CardType_Index carT_index = CardIndexCompute (myCards);
                        List<CardObject> MYcards = new List<CardObject>();
                        foreach (CardObject a in myCards) {
                            MYcards.Add(a);
                        }
                        MYcards.Remove(myCards[i]);
                        MYcards.Remove(myCards[i + 1]);
                        MYcards.Remove(myCards[i + 2]);
                        SortCards(MYcards);
                        for (int j = 0; j < MYcards.Count - 2; j++) {
                            GameConst.CardWeight grade5 = GetWeight(MYcards[j]);
                            GameConst.CardWeight grade6 = GetWeight(MYcards[j + 1]);
                            if (grade5 == grade6) {
                                AIpai.Add(myCards[i]);
                                AIpai.Add(myCards[i + 1]);
                                AIpai.Add(myCards[i + 2]);
                                AIpai.Add(MYcards[j]);
                                AIpai.Add(MYcards[j + 1]);
                                return AIpai;
                            }
                        }

                        return AIpai;
                    }
                }
            }
        }
        // 上家出炸弹
        else if (preRoleType == RoleType.BombCard) {
            // 4张牌可以大过上家的牌
            if (mySize >= 4) {
                for (int i = 0; i < mySize - 4; i++) {
                    GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                    GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                    GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);
                    GameConst.CardWeight grade3 = GetWeight(myCards[i + 3]);

                    if (grade0 == grade1 && grade0 == grade2 && grade0 == grade3) {
                        if (grade0 > prevWeight) {
                            // 只要有4张牌可以大过上家，则返回true
                            AIpai.Add(myCards[i]);
                            AIpai.Add(myCards[i + 1]);
                            AIpai.Add(myCards[i + 2]);
                            AIpai.Add(myCards[i + 3]);
                            return AIpai;
                        }
                    }
                }
            }
        }
        // 上家出4带2
        else if (preRoleType == RoleType.FourTwoCard) {
            // 4张牌可以大过上家的牌
            if (mySize >= 6) {
                for (int i = 0; i < mySize - 4; i++) {
                    GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                    GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                    GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);
                    GameConst.CardWeight grade3 = GetWeight(myCards[i + 3]);

                    if (grade0 == grade1 && grade0 == grade2 && grade0 == grade3) {
                        // 只要有炸弹，则返回true
                        if (grade0 > prevWeight) {
                            List<CardObject> MYcards = new List<CardObject>();
                            foreach (CardObject a in myCards) {
                                MYcards.Add(a);
                            }
                            MYcards.Remove(myCards[i]);
                            MYcards.Remove(myCards[i + 1]);
                            MYcards.Remove(myCards[i + 2]);
                            MYcards.Remove(myCards[i + 3]);
                            SortCards(MYcards);

                            AIpai.Add(myCards[i]);
                            AIpai.Add(myCards[i + 1]);
                            AIpai.Add(myCards[i + 2]);
                            AIpai.Add(myCards[i + 3]);
                            AIpai.Add(MYcards[0]);
                            AIpai.Add(MYcards[1]);

                            return AIpai;
                        }
                    }
                }
            }
        }
        // 上家出4带2 dui --***********
        else if (preRoleType == RoleType.FourTwotwoCard) {
            // 4张牌可以大过上家的牌
            if (mySize < prevSize) {
            } else {
                RoleTypeIndex roletypeIndex = CardIndexCompute(myCards);
                if (roletypeIndex.FourIndex.Count > 0 &&
                    roletypeIndex.DoubleIndex.Count + roletypeIndex.ThreeIndex.Count +
                    roletypeIndex.FourIndex.Count - 1 >= 2) //ruguo bao han 4 dai 2 dui
                {
                    int four_grade_index = 0;
                    for (int i = 0; i < roletypeIndex.FourIndex.Count; i++) {
                        if (Card.GetWeightByNumber(roletypeIndex.FourIndex[i]) > prevWeight) four_grade_index = i;
                        break;
                        if (i == roletypeIndex.FourIndex.Count) return AIpai;
                    }

                    List<CardObject> MYcards = new List<CardObject>();
                    foreach (CardObject a in myCards) {
                        MYcards.Add(a);
                    }
                    //qu chu 4
                    for (int i = 0; i < mySize - 3; i++) {
                        if (GetWeight(myCards[i]) ==
                            Card.GetWeightByNumber(roletypeIndex.FourIndex[four_grade_index])) {
                            AIpai.Add(myCards[i]);
                            AIpai.Add(myCards[i + 1]);
                            AIpai.Add(myCards[i + 2]);
                            AIpai.Add(myCards[i + 3]);
                            MYcards.Remove(myCards[i]);
                            MYcards.Remove(myCards[i + 1]);
                            MYcards.Remove(myCards[i + 2]);
                            MYcards.Remove(myCards[i + 3]);
                            break;
                            //						Debug.Log("4dai2   "+ myCards[i]+myCards[i+1]+myCards[i+2]+myCards[i+3]);
                        }
                    }
                    //qu chu liang dui
                    SortCards(MYcards);
                    for (int i = 0; i < MYcards.Count - 1; i++) {
                        GameConst.CardWeight grade1 = GetWeight(MYcards[i]);
                        GameConst.CardWeight grade2 = GetWeight(MYcards[i + 1]);

                        if (grade1 == grade2) {
                            AIpai.Add(MYcards[i]);
                            AIpai.Add(MYcards[i + 1]);
                            i += 1;
                        }

                        if (AIpai.Count == prevSize) return AIpai;
                    }
                }
            }
        }
        // 上家出顺子
        else if (preRoleType == RoleType.LianCard) {
            if (mySize < prevSize) {
                //				return AIpai;
            } else {
                List<GameConst.CardWeight> removeBigList = RemoveBigCard(myCards); //fan hui de shi grade
                List<GameConst.CardWeight> danlistgrade = removeBigList.Distinct().ToList();
                List<CardObject> danlist = new List<CardObject>();
                for (int i = 0; i < danlistgrade.Count; i++) //fan hui dan pai list
                {
                    for (int j = 0; j < mySize; j++) {
                        if (GetWeight(myCards[j]) == danlistgrade[i]) {
                            danlist.Add(myCards[j]);
                            break;
                        }
                    }
                }
                SortCards(danlist);
                for
                (int i = 0;
                    i < danlist.Count - prevSize + 1;
                    i++) //zai list zhong zhao neng da guo shang jia de shun zi
                {
                    List<CardObject> list = danlist.GetRange(i, prevSize);

                    if (IsShunZi(list) && GetWeight(list[0]) > prevWeight) return list;
                }
            }
            return AIpai;
        }
        // 上家出连对
        else if (preRoleType == RoleType.LianDuiCard) {
            if (mySize < prevSize) {
                //				return AIpai;
            } else {
                List<GameConst.CardWeight> removeBigList = RemoveBigCard(myCards); //fan hui de shi grade
                List<GameConst.CardWeight> danlistgrade = removeBigList.Distinct().ToList();
                List<CardObject> danlist = new List<CardObject>();
                for (int i = 0; i < danlistgrade.Count; i++) //fan hui dan pai list
                {
                    for (int j = 0; j < mySize - 1; j++) {
                        if (GetWeight(myCards[j]) == danlistgrade[i] &&
                            GetWeight(myCards[j]) == GetWeight(myCards[j + 1])) {
                            danlist.Add(myCards[j]);
                            danlist.Add(myCards[j + 1]);
                            break;
                        }
                    }
                }
                SortCards(danlist);
                for (int i = 0;
                    i < danlist.Count - prevSize + 1;
                    i += 2) //zai list zhong zhao neng da guo shang jia de shun zi
                {
                    List<CardObject> list = danlist.GetRange(i, prevSize);

                    if (IsLianDui(list) && GetWeight(list[0]) > prevWeight) return list;
                }
            }
        }
        // 上家出飞机budai
        else if (preRoleType == RoleType.FeijiNoCard) {
            if (mySize < prevSize) {
                //				return AIpai;
            } else {
                List<GameConst.CardWeight> removeBigList = RemoveBigCard(myCards); //fan hui de shi grade
                List<GameConst.CardWeight> danlistgrade = removeBigList.Distinct().ToList();
                List<CardObject> danlist = new List<CardObject>();
                for (int i = 0; i < danlistgrade.Count; i++) //fan hui dan pai list
                {
                    for (int j = 0; j < mySize - 2; j++) {
                        if (GetWeight(myCards[j]) == danlistgrade[i] &&
                            GetWeight(myCards[j]) == GetWeight(myCards[j + 1]) &&
                            GetWeight(myCards[j + 1]) == GetWeight(myCards[j + 2])) {
                            danlist.Add(myCards[j]);
                            danlist.Add(myCards[j + 1]);
                            danlist.Add(myCards[j + 2]);
                            break;
                        }
                    }
                }
                SortCards(danlist);
                for (int i = 0;
                    i < danlist.Count - prevSize + 1;
                    i += 3) //zai list zhong zhao neng da guo shang jia de shun zi
                {
                    List<CardObject> list = danlist.GetRange(i, prevSize);

                    if (IsFeiJiBuDai(list) && GetWeight(list[0]) > prevWeight) return list;
                }
            }
        }
        // 上家出飞机dai dan
        else if (preRoleType == RoleType.FeijiSingleCard) {
            if (mySize < prevSize) {
                //				return AIpai;
            } else {
                List<GameConst.CardWeight> removeBigList = RemoveBigCard(myCards); //fan hui de shi grade
                List<GameConst.CardWeight> danlistgrade = removeBigList.Distinct().ToList();
                List<CardObject> danlist = new List<CardObject>();
                for (int i = 0; i < danlistgrade.Count; i++) //fan hui dan pai list
                {
                    for (int j = 0; j < mySize - 2; j++) {
                        if (GetWeight(myCards[j]) == danlistgrade[i] &&
                            GetWeight(myCards[j]) == GetWeight(myCards[j + 1]) &&
                            GetWeight(myCards[j + 1]) == GetWeight(myCards[j + 2])) {
                            danlist.Add(myCards[j]);
                            danlist.Add(myCards[j + 1]);
                            danlist.Add(myCards[j + 2]);
                            break;
                        }
                    }
                }
                //				Debug.Log("danlist ==  " + danlist.Count);
                SortCards(danlist);
                for (int i = 0;
                    i < danlist.Count - prevSize + 1;
                    i += 3) //zai list zhong zhao neng da guo shang jia de shun zi
                {
                    List<CardObject> list = danlist.GetRange(i, prevSize - (prevSize / 4));
                    if (IsFeiJiBuDai(list) && GetWeight(list[0]) > prevWeight) {
                        //--
                        List<CardObject> MYcards = new List<CardObject>();
                        //						foreach(int a in list)
                        //						{
                        //							Debug.Log("feiji dai dan =====   " + a);
                        //						}
                        //						return list;
                        foreach (CardObject a in myCards) {
                            MYcards.Add(a);
                        }
                        for (int j = 0; j < list.Count; j++) //zai MYcards li mian chu diao feiji
                        {
                            MYcards.Remove(list[j]);
                        }

                        for
                        (int j = 0;
                            j < MYcards.Count;
                            j++) // chu diao he fei ji xiang tong de pai ,bi mian dai zha dan
                        {
                            foreach (CardObject a in list) {
                                if (GetWeight(a) == GetWeight(MYcards[j])) MYcards.Remove(MYcards[j]);
                            }
                        }

                        SortCards(MYcards);
                        int chibangnum = list.Count / 3;
                        for (int j = 0; j < chibangnum; j++) {
                            list.Add(MYcards[j]);
                        }
                        if (IsFeiJiDaiDan(list)) return list;
                    }
                }
            }
        }

        // 上家出飞机dai dui
        else if (preRoleType == RoleType.FeijiDoubleCard) {
            if (mySize < prevSize) {
                //				return AIpai;
            } else {
                List<GameConst.CardWeight> removeBigList = RemoveBigCard(myCards); //fan hui de shi grade
                List<GameConst.CardWeight> danlistgrade = removeBigList.Distinct().ToList();
                List<CardObject> danlist = new List<CardObject>();
                //				if(Globe.Id == 2){
                //					Debug.Log("danlist 111=== ");
                //				}
                for (int i = 0; i < danlistgrade.Count; i++) //fan hui dan pai list
                {
                    for (int j = 0; j < mySize - 2; j++) {
                        if (GetWeight(myCards[j]) == danlistgrade[i] &&
                            GetWeight(myCards[j]) == GetWeight(myCards[j + 1]) &&
                            GetWeight(myCards[j + 1]) == GetWeight(myCards[j + 2])) {
                            danlist.Add(myCards[j]);
                            danlist.Add(myCards[j + 1]);
                            danlist.Add(myCards[j + 2]);
                            break;
                        }
                    }
                }
                SortCards(danlist);
                //				Debug.Log("wei sha mei you === " +danlist)
                //				Debug.Log("feiji dai dui !!==   " + danlist.Count + prevSize + (prevSize/5)*2);
                for (int i = 0;
                    i < danlist.Count - prevSize + (prevSize / 5) * 2;
                    i += 3) //zai list zhong zhao neng da guo shang jia de shun zi
                {
                    List<CardObject> list = danlist.GetRange(i, prevSize - (prevSize / 5) * 2);
                    //					Debug.Log("feiji dai dui !!==   " + list.Count + prevSize + (prevSize/5)*2);
                    if (IsFeiJiBuDai(list) && GetWeight(list[0]) > prevWeight) {
                        //--
                        //						if(Globe.Id == 2){
                        //						Debug.Log("fei ji dai dui !!4444==== ");
                        //						}
                        List<CardObject> MYcards = new List<CardObject>();
                        foreach (CardObject a in myCards) {
                            MYcards.Add(a);
                        }

                        for (int j = 0; j < list.Count; j++) //zai MYcards li mian chu diao feiji
                        {
                            MYcards.Remove(list[j]);
                        }

                        SortCards(MYcards);
                        //						if(Globe.Id == 2){
                        //						Debug.Log("fei ji dai dui !!4.555...==== " + MYcards.Count);
                        //							return AIpai;
                        //						}
                        for (int j = 0; j < MYcards.Count - 1; j++) {
                            GameConst.CardWeight grade1 = GetWeight(MYcards[j]);
                            GameConst.CardWeight grade2 = GetWeight(MYcards[j + 1]);
                            if (grade1 == grade2) {
                                list.Add(MYcards[j]);
                                list.Add(MYcards[j + 1]);
                                j += 2;
                            }

                            if (list.Count == prevSize && IsFeiJiDaiDui(list)) {
                                return list;
                            }
                        }
                    }
                }
            }
        }


        // 集中判断对方不是炸弹，我出炸弹的情况
        if (preRoleType != RoleType.BombCard) {
            if (mySize < 4) {
                //				return AIpai;
            } else {
                for (int i = 0; i < mySize - 3; i++) {
                    GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                    GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                    GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);
                    GameConst.CardWeight grade3 = GetWeight(myCards[i + 3]);

                    if (grade1 == grade0 && grade2 == grade0
                        && grade3 == grade0) {
                        AIpai.Add(myCards[i]);
                        AIpai.Add(myCards[i + 1]);
                        AIpai.Add(myCards[i + 2]);
                        AIpai.Add(myCards[i + 3]);
                        return AIpai;
                    }
                }
            }
        }

        if (mySize >= 2) {
            List<CardObject> cards = new List<CardObject>();
            cards.Add(myCards[mySize - 1]);
            cards.Add(myCards[mySize - 2]);
            if (IsDuiWang(cards)) {
                //				return cards;
                AIpai = cards;
            }
        }

        // 默认不能出牌
        return AIpai;
    }

    private List<CardObject> ChooseCards(bool against, RoleTypeIndexAvd myRoleTypeIndexAvd,
        RoleTypeIndexAvd prevRoleTypeIndexAvd,
        List<CardObject> myCards, List<CardObject> prevCards, RoleType preRoleType) {
        List<CardObject> aIpai = new List<CardObject>();
        // 我的牌和上家的牌都不能为null
        if (myCards == null || prevCards == null) {
            return aIpai;
        }

        // 默认情况：上家和自己想出的牌都符合规则
        SortCards(myCards); // 对牌排序
        SortCards(prevCards); // 对牌排序

        // 上一首牌的个数
        int prevSize = prevCards.Count;
        int mySize = myCards.Count;

        // 我先出牌，上家没有牌
        if (prevSize == 0 && mySize != 0) {
            aIpai.Add(myCards[0]);
            return aIpai;
        }

        // 集中判断是否王炸，免得多次判断王炸
        if (preRoleType == RoleType.KingBombCard) {
            Debug.Log("上家王炸，肯定不能出。");
            return aIpai;
        }

        GameConst.CardWeight prevWeight = GetWeight(prevCards[0]);

        // 比较2家的牌，主要有2种情况，1.我出和上家一种类型的牌，即对子管对子；
        // 2.我出炸弹，此时，和上家的牌的类型可能不同
        // 王炸的情况已经排除

        // 上家出单
        if (preRoleType == RoleType.SingleCard) {
            // 一张牌可以大过上家的牌
            Debug.Log("上家出的单");
            if (against) {
                RoleType roleType = WhatIsMax(prevRoleTypeIndexAvd);
                switch (roleType) {
                    case RoleType.SingleCard:
                        //对抗上家, 上家单多, 多个大牌顶一下
                        if (myRoleTypeIndexAvd.SingleCards.Count != 0) {
                            var cardObject = myRoleTypeIndexAvd.SingleCards[myRoleTypeIndexAvd.SingleCards.Count - 1]
                                .cards[0];
                            GameConst.CardWeight grade = GetWeight(cardObject);
                            if (grade > prevWeight) {
                                // 只要有1张牌可以大过上家，则返回true
                                aIpai.Add(cardObject);
                                Debug.Log("AI出单" + cardObject);
                                return aIpai;
                            }
                        }
                        break;
                }
                for (var i = 0; i < myRoleTypeIndexAvd.SingleCards.Count; i++) {
                    var roleTypeAvd = myRoleTypeIndexAvd.SingleCards[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight) {
                        // 只要有1张牌可以大过上家，则返回true
                        aIpai.Add(roleTypeAvd.cards[0]);
                        Debug.Log("AI出单" + roleTypeAvd.cards[0]);
                        return aIpai;
                    }
                }
            } else {
                if ((int) prevCards[0].Card.cardWeight > 12) {
                    return aIpai;
                } else {
                    for (var i = 0; i < myRoleTypeIndexAvd.SingleCards.Count; i++) {
                        var roleTypeAvd = myRoleTypeIndexAvd.SingleCards[i];
                        GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                        if (grade > prevWeight) {
                            // 只要有1张牌可以大过上家，则返回true
                            aIpai.Add(roleTypeAvd.cards[0]);
                            Debug.Log("AI出单" + roleTypeAvd.cards[0]);
                            return aIpai;
                        }
                    }
                    return aIpai;
                }
            }
        }
        // 上家出对子
        else if (preRoleType == RoleType.DoubleCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.DoubleCards;
            if (against) {
                RoleType roleType = WhatIsMax(prevRoleTypeIndexAvd);
                switch (roleType) {
                    case RoleType.DoubleCard:
                        //对抗上家, 上家对多, 多个大牌顶一下
                        if (roleTypeAvds.Count != 0) {
                            var cardObject = roleTypeAvds[roleTypeAvds.Count - 1]
                                .cards[0];
                            GameConst.CardWeight grade = GetWeight(cardObject);
                            if (grade > prevWeight) {
                                // 只要有1张牌可以大过上家，则返回true
                                aIpai.AddRange(roleTypeAvds[roleTypeAvds.Count - 1].cards);
                                Debug.Log("AI出对子" + cardObject);
                                return aIpai;
                            }
                        }
                        break;
                }
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight) {
                        // 只要有1张牌可以大过上家，则返回true
                        aIpai.AddRange(roleTypeAvd.cards);
                        Debug.Log("AI出对子" + roleTypeAvd.cards[0]);
                        return aIpai;
                    }
                }
            } else {
                if ((int) prevCards[0].Card.cardWeight > 12) {
                    return aIpai;
                } else {
                    for (var i = 0; i < roleTypeAvds.Count; i++) {
                        var roleTypeAvd = roleTypeAvds[i];
                        GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                        if (grade > prevWeight) {
                            // 只要有1张牌可以大过上家，则返回true
                            aIpai.AddRange(roleTypeAvd.cards);
                            Debug.Log("AI出对" + roleTypeAvd.cards[0]);
                            return aIpai;
                        }
                    }
                    return aIpai;
                }
            }
        }
        // 上家出3不带
        else if (preRoleType == RoleType.ThreeCard) {
            // 3张牌可以大过上家的牌
            Debug.Log("三条");

            var roleTypeAvds = myRoleTypeIndexAvd.ThreeCards;
            if (against) {
                RoleType roleType = WhatIsMax(prevRoleTypeIndexAvd);
                switch (roleType) {
                    case RoleType.FeijiNoCard:
                        //对抗上家, 上家有飞机
                        if (roleTypeAvds.Count != 0) {
                            var cardObject = roleTypeAvds[roleTypeAvds.Count - 1]
                                .cards[0];
                            GameConst.CardWeight grade = GetWeight(cardObject);
                            if (grade > prevWeight) {
                                // 只要有1张牌可以大过上家，则返回true
                                aIpai.AddRange(roleTypeAvds[roleTypeAvds.Count - 1].cards);
                                Debug.Log("AI出三条" + cardObject);
                                return aIpai;
                            }
                        }
                        break;
                }
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight) {
                        // 只要有1张牌可以大过上家，则返回true
                        aIpai.AddRange(roleTypeAvd.cards);
                        Debug.Log("AI出三条" + roleTypeAvd.cards[0]);
                        return aIpai;
                    }
                }
            } else {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight && grade - prevWeight == 1) {
                        // 只要有1张牌可以大过上家，则返回true
                        aIpai.AddRange(roleTypeAvd.cards);
                        Debug.Log("AI出三条" + roleTypeAvd.cards[0]);
                        return aIpai;
                    }
                }
                return aIpai;
            }
        }
        // 上家出3带1
        else if (preRoleType == RoleType.ThreeOneCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.ThreeCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);
                    if (grade > cardWeight) {
                        // 只要有1张牌可以大过上家，则返回true
                        if (myRoleTypeIndexAvd.SingleCards.Count > 0) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.Add(myRoleTypeIndexAvd.SingleCards[0].cards[0]);
                            Debug.Log("AI出三条" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);
                    if (grade > cardWeight && (grade - cardWeight <= 2)) {
                        if (myRoleTypeIndexAvd.SingleCards.Count > 0) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.Add(myRoleTypeIndexAvd.SingleCards[0].cards[0]);
                            Debug.Log("AI出三条" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
                return aIpai;
            }
        }
        // 上家出3带2
        else if (preRoleType == RoleType.ThreeTwoCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.ThreeCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);
                    if (grade > cardWeight) {
                        // 只要有1张牌可以大过上家，则返回true
                        if (myRoleTypeIndexAvd.DoubleCards.Count > 0) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[0].cards);
                            Debug.Log("AI出三带二" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                var cardIndexCompute = CardIndexCompute(prevCards);
                if ((int) cardIndexCompute.ThreeIndex[0] < 7) {
                    for (var i = 0; i < roleTypeAvds.Count; i++) {
                        var roleTypeAvd = roleTypeAvds[i];
                        GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                        var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);
                        if (grade > cardWeight && (grade - prevWeight <= 2)) {
                            // 只要有1张牌可以大过上家，则返回true
                            if (myRoleTypeIndexAvd.DoubleCards.Count > 0) {
                                aIpai.AddRange(roleTypeAvd.cards);
                                aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[0].cards);
                                Debug.Log("AI出三带二" + roleTypeAvd.cards[0]);
                            }
                        }
                    }
                } else {
                    //不出
                }

                return aIpai;
            }
        }
        // 上家出炸弹
        else if (preRoleType == RoleType.BombCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.BombCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight) {
                        aIpai.AddRange(roleTypeAvd.cards);
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }
        // 上家出4带2
        else if (preRoleType == RoleType.FourTwoCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.BombCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.FourIndex[0]);
                    if (grade > cardWeight) {
                        if (myRoleTypeIndexAvd.SingleCards.Count > 1) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.SingleCards[0].cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.SingleCards[1].cards);
                            Debug.Log("AI出四带二" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }
        // 上家出4带2 dui --***********
        else if (preRoleType == RoleType.FourTwotwoCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.BombCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.FourIndex[0]);
                    if (grade > cardWeight) {
                        if (myRoleTypeIndexAvd.DoubleCards.Count > 1) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[0].cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[1].cards);
                            Debug.Log("AI出四带二对" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }
        // 上家出顺子
        else if (preRoleType == RoleType.LianCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.LianCards;
            var prevCount = prevCards.Count;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    SortCardsByWeight(roleTypeAvd.cards);
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight && prevCount == roleTypeAvd.cards.Count) {
                        if (myRoleTypeIndexAvd.DoubleCards.Count > 1) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            Debug.Log("AI出连" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
            return aIpai;
        }
        // 上家出连对
        else if (preRoleType == RoleType.LianDuiCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.LianDuiCards;
            var prevCount = prevCards.Count;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    SortCardsByWeight(roleTypeAvd.cards);
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight && prevCount == roleTypeAvd.cards.Count) {
                        if (myRoleTypeIndexAvd.DoubleCards.Count > 1) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            Debug.Log("AI出连对" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }
        // 上家出飞机budai
        else if (preRoleType == RoleType.FeijiNoCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.FeijiCards;
            var prevCount = prevCards.Count;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    if (grade > prevWeight && prevCount == roleTypeAvd.cards.Count) {
                        aIpai.AddRange(roleTypeAvd.cards);
                        Debug.Log("AI出飞机" + roleTypeAvd.cards[0]);
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }
        // 上家出飞机dai dan
        else if (preRoleType == RoleType.FeijiSingleCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.FeijiCards;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    SortCardsByWeight(roleTypeAvd.cards);
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);

                    if (grade > cardWeight && cardIndexCompute.ThreeIndex.Count * 3 == roleTypeAvd.cards.Count) {
                        if (myRoleTypeIndexAvd.SingleCards.Count >= roleTypeAvds.Count) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.SingleCards[0].cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.SingleCards[1].cards);
                            Debug.Log("AI出飞机带单" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }

        // 上家出飞机dai dui
        else if (preRoleType == RoleType.FeijiDoubleCard) {
            var roleTypeAvds = myRoleTypeIndexAvd.FeijiCards;
            var prevCount = prevCards.Count;
            if (against) {
                for (var i = 0; i < roleTypeAvds.Count; i++) {
                    var roleTypeAvd = roleTypeAvds[i];
                    SortCardsByWeight(roleTypeAvd.cards);
                    GameConst.CardWeight grade = GetWeight(roleTypeAvd.cards[0]);
                    var cardIndexCompute = CardIndexCompute(prevCards);
                    var cardWeight = GetWeight(cardIndexCompute.ThreeIndex[0]);

                    if (grade > cardWeight && cardIndexCompute.ThreeIndex.Count * 3 == roleTypeAvd.cards.Count) {
                        if (myRoleTypeIndexAvd.DoubleCards.Count >= roleTypeAvds.Count) {
                            aIpai.AddRange(roleTypeAvd.cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[0].cards);
                            aIpai.AddRange(myRoleTypeIndexAvd.DoubleCards[1].cards);
                            Debug.Log("AI出飞机带对" + roleTypeAvd.cards[0]);
                        }
                        return aIpai;
                    }
                }
            } else {
                return aIpai;
            }
        }


        // 集中判断对方不是炸弹，我出炸弹的情况
        if (preRoleType != RoleType.BombCard) {
            if (mySize < 4) {
                //				return AIpai;
            } else {
                for (int i = 0; i < mySize - 3; i++) {
                    GameConst.CardWeight grade0 = GetWeight(myCards[i]);
                    GameConst.CardWeight grade1 = GetWeight(myCards[i + 1]);
                    GameConst.CardWeight grade2 = GetWeight(myCards[i + 2]);
                    GameConst.CardWeight grade3 = GetWeight(myCards[i + 3]);

                    if (grade1 == grade0 && grade2 == grade0
                        && grade3 == grade0) {
                        aIpai.Add(myCards[i]);
                        aIpai.Add(myCards[i + 1]);
                        aIpai.Add(myCards[i + 2]);
                        aIpai.Add(myCards[i + 3]);
                        return aIpai;
                    }
                }
            }
        }

        if (mySize >= 2) {
            List<CardObject> cards = new List<CardObject>();
            cards.Add(myCards[mySize - 1]);
            cards.Add(myCards[mySize - 2]);
            if (IsDuiWang(cards)) {
                //				return cards;
                aIpai = cards;
            }
        }

        // 默认不能出牌
        return aIpai;
    }

    private List<GameConst.CardWeight> RemoveBigCard(List<CardObject> cards) {
        List<GameConst.CardWeight> list = new List<GameConst.CardWeight>();
        if (cards.Count != 0) {
            for (int i = 0; i < cards.Count - 1; i++) {
                GameConst.CardWeight grade = GetWeight(cards[i]);
                if ((int) grade < 14) list.Add(grade);
            }
            return list;
        }
        return list;
    }

    #endregion
}

internal class CardNumberComparer : IEqualityComparer<CardObject> {
    public bool Equals(CardObject x, CardObject y) {
        return x.Card.cardNumber == y.Card.cardNumber;
    }

    public int GetHashCode(CardObject obj) {
        return obj.Card.cardNumber.GetHashCode();
    }
}                