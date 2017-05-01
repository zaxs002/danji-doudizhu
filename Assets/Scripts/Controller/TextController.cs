using System.Collections.Generic;
using model;
using UnityEngine;

public class TextController : MonoBehaviour {
    public GameObject CallLandLord;
    public GameObject PassLandLord;
    public GameObject Pass;
    public GameObject NoCardOverCome;
    public GameObject NoCardRoleType;

    private List<GameObject> textList = new List<GameObject>();
    private List<GameObject> passList = new List<GameObject>();
    public GameObject passSelf;
    public GameObject passRight;
    public GameObject passLeft;

    void Start() {
        textList.Add(CallLandLord);
        textList.Add(PassLandLord);
        textList.Add(Pass);
        textList.Add(NoCardOverCome);
        textList.Add(NoCardRoleType);
//		textList.Add(QiangLandLord);

        passList.Add(passSelf);
        passList.Add(passRight);
        passList.Add(passLeft);
    }

    void Update() {
    }

    public void ShowTextOn(TextEnum textEnum, Player player) {
        if (textEnum == TextEnum.Pass) {
            passList[(int)player.Position].SetActive(true);
            return;
        }
        switch (player.Position) {
            case Player.PlayerPosition.Self:
                switch (textEnum) {
                    case TextEnum.NoCardOverCome:
                        textList[(int) TextEnum.NoCardRoleType].gameObject.SetActive(false);
                        textList[(int) textEnum].SetActive(true);
                        return;
                    case TextEnum.NoCardRoleType:
                        textList[(int) TextEnum.NoCardOverCome].gameObject.SetActive(false);
                        textList[(int) textEnum].SetActive(true);
                        return;
                }
                textList[(int) textEnum].transform.localPosition = new Vector3(0, -15, 0);
                textList[(int) textEnum].SetActive(true);
                break;
            case Player.PlayerPosition.Right:
                textList[(int) textEnum].transform.localPosition = new Vector3(310, 66, 0);
                textList[(int) textEnum].SetActive(true);
                break;
            case Player.PlayerPosition.Left:
                textList[(int) textEnum].transform.localPosition = new Vector3(-310, 66, 0);
                textList[(int) textEnum].SetActive(true);
                break;
        }
    }

    public void HideText(TextEnum textEnum, Player player) {
        if (textEnum == TextEnum.Pass) {
            passList[(int)player.Position].SetActive(true);
            return;
        }
        switch (player.Position) {
            case Player.PlayerPosition.Self:
                switch (textEnum) {
                    case TextEnum.NoCardOverCome:
                        textList[(int) textEnum].SetActive(false);
                        return;
                    case TextEnum.NoCardRoleType:
                        textList[(int) textEnum].SetActive(false);
                        return;
                }
                break;
            case Player.PlayerPosition.Right:
                break;
            case Player.PlayerPosition.Left:
                break;
        }
    }

    public void HideAllTexts() {
        foreach (GameObject o in textList) {
            if (o.activeSelf) {
                o.SetActive(false);
            }
        }
        foreach (var o in passList) {
            if (o.activeSelf) {
                o.SetActive(false);
            }
        }
    }

    public enum TextEnum {
        CallLandLord,
        PassLandLord,

//		QiangLandLord = 2
        Pass,
        NoCardOverCome,
        NoCardRoleType,
    }
}