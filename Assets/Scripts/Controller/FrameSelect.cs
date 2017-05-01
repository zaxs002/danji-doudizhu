using System.Collections.Generic;
using Controller;
using model;
using UnityEngine;

public class FrameSelect : MonoBehaviour {
    private CardController _cardController;
    private Vector3 _startPos = Vector3.zero; //记下鼠标按下位置

    // Use this for initialization
    void Start() {
        _cardController = transform.parent.Find("CardController").GetComponent<CardController>();
    }


    // Update is called once per frame
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            _startPos = Input.mousePosition; //记录按下位置
        } else if (Input.GetMouseButtonUp(0)) {
            CheckSelection(_startPos, Input.mousePosition);

            List<CardObject> selectedCards = _cardController.PlayersList[0].SelectedCards;
            List<CardObject> readyPlayCards = _cardController.PlayersList[0].ReadyPlayCards;
            for (int i = 0; i < selectedCards.Count; i++) {
                CardObject selectedCard = _cardController.PlayersList[0].SelectedCards[i];
                //如果要出的牌里面有选中的牌，则变成不出的牌
                if (readyPlayCards.Contains(selectedCard)) {
                    readyPlayCards.Remove(selectedCard);
                    selectedCard.transform.localPosition =
                        new Vector3(
                            selectedCard.transform.localPosition.x,
                            selectedCard.transform.localPosition.y - 20,
                            selectedCard.transform.localPosition.z);
                } else {
                    //如果要出的牌里面没有选中的牌，则变成要出的牌
                    readyPlayCards.Add(selectedCard);
                    selectedCard.transform.localPosition =
                        new Vector3(
                            selectedCard.transform.localPosition.x,
                            selectedCard.transform.localPosition.y + 20,
                            selectedCard.transform.localPosition.z);
                }
            }

            _cardController.PlayersList[0].SelectedCards.Clear(); //清空选中的牌
        }
    }

    void CheckSelection(Vector3 start, Vector3 end) {
        //选中的牌置灰色
        Vector3 p1 = Vector3.zero;
        Vector3 p2 = Vector3.zero;
        if (start.x > end.x) {
            p1.x = end.x;
            p2.x = start.x;
        } else {
            p1.x = start.x;
            p2.x = end.x;
        }
        if (start.y > end.y) {
            p1.y = end.y;
            p2.y = start.y;
        } else {
            p1.y = start.y;
            p2.y = end.y;
        }

        if (Mathf.Abs(start.x - end.x) < 30) {
//			Debug.Log("点击");
            return;
        }

        List<CardObject> selectedCards = _cardController.PlayersList[0].SelectedCards;
        foreach (CardObject co in _cardController.PlayersList[0].Cards) {
            Vector3 location = UICamera.mainCamera.WorldToScreenPoint(co.transform.position);
            //在2560x1440分辨率下调整的,以这个分辨率为基准
            float aspWidth = GameConst.CardBetween / 2560f;
            float aspHeight = 150 / 1440f;
            float widthDiff = Screen.width * aspWidth;
            float heightDiff = Screen.height * aspHeight;
            if (location.x < p1.x || location.x - widthDiff > p2.x ||
                location.y + heightDiff < p1.y || location.y - heightDiff > p2.y) {
                if (selectedCards.Contains(co)) {
                    selectedCards.Remove(co);
                    Debug.Log("remove==  " + co.transform.name);
                }
            } else {
                if (!selectedCards.Contains(co)) {
                    selectedCards.Add(co);
                    Debug.Log("add==  " + co.transform.name);
                }
            }
        }
    }

    public void ClearCards() {
        List<CardObject> readyPlayCards = _cardController.PlayersList[0].ReadyPlayCards;
        for (int i = 0; i < readyPlayCards.Count; i++) {
            CardObject readyPlayCard = readyPlayCards[i];
            readyPlayCard.transform.localPosition =
                new Vector3(
                    readyPlayCard.transform.localPosition.x,
                    readyPlayCard.transform.localPosition.y - 20,
                    readyPlayCard.transform.localPosition.z);
        }
        readyPlayCards.Clear();
    }
}