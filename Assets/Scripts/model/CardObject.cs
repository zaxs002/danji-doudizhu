using UnityEngine;

namespace model {
	public class CardObject : MonoBehaviour {
		private Card _card;
		public UISprite CardSuitSprite;

		public UISprite CardNumberSprite;

//		public UISprite CardPicSprite;
		private bool _isFont = true;


		public CardObject() {
		}

		private void Awake() {
		}

		private void Start() {
		}

		public Card Card {
			get { return _card; }
			set {
				_card = value;
				CardSuitSprite.spriteName = "suit" + (int) _card.cardSuit;
				CardNumberSprite.spriteName = "card" + (int) _card.cardNumber;
//				if ((int) _card.cardNumber > 10) {
//					CardPicSprite.spriteName = "cardPic" + (int) _card.cardNumber;
//				}
			}
		}

		/// <summary>
		///   <para>初始化牌到parent上,对某些牌特殊处理</para>
		/// </summary>
		public void Init(Transform parent, bool diZhuPai) {
			transform.parent = parent;
			if (diZhuPai) {
				transform.position = new Vector3(-26, 447, 0);
			} else {
				transform.position = Vector3.zero;
			}
			transform.localScale = Vector3.one;
//			cardObj.cardSuitSprite.spriteName = "suit" + (int) cardList[rdm].cardSuit;
//			cardObj.cardNumberSprite.spriteName = "card" + (int) cardList[rdm].cardNumber;
			CardSuitSprite.gameObject.SetActive(true);
//			if ((int) _card.cardNumber > 10) {
//				CardPicSprite.gameObject.SetActive(true);
//				switch (_card.cardSuit) {
//					case GameConst.Suit.Dianmond:
//					case GameConst.Suit.Heart:
//						CardPicSprite.spriteName = "cardPicr" + (int) _card.cardNumber;
//						break;
//					case GameConst.Suit.Clubs:
//					case GameConst.Suit.Spade:
//						CardPicSprite.spriteName = "cardPicb" + (int) _card.cardNumber;
//						break;
//					case GameConst.Suit.None:
//						CardPicSprite.spriteName = "cardPic" + (int) _card.cardNumber;
//						break;
//				}
//			}
//			if ((int) _card.cardNumber <= 10) {
//				CardPicSprite.gameObject.SetActive(false);
//			}
			float aspectRadio = 44f / 35f;
			if ((int) _card.cardNumber == 10) {
				CardNumberSprite.aspectRatio = 0.95f;
				CardNumberSprite.width = (int)(35*aspectRadio);
			} else if ((int) _card.cardNumber == 1) {
				CardNumberSprite.GetComponent<UISprite>().aspectRatio = 0.75f;
				CardNumberSprite.GetComponent<UISprite>().width = (int)(30*aspectRadio);
				CardNumberSprite.GetComponent<UISprite>().height = (int)(40*aspectRadio);
			} else if ((int) _card.cardNumber > 13) {
				CardNumberSprite.transform.localPosition = new Vector3(-36.5f, 19, 0);
				CardNumberSprite.GetComponent<UISprite>().aspectRatio = 0.2083333f;
				CardNumberSprite.GetComponent<UISprite>().width = (int)(22*aspectRadio);
				CardNumberSprite.GetComponent<UISprite>().height = (int)(106*aspectRadio);
				CardSuitSprite.gameObject.SetActive(false);
			} else if ((int) _card.cardNumber == 7) {
				CardNumberSprite.GetComponent<UISprite>().aspectRatio = 0.6944f;
				CardNumberSprite.GetComponent<UISprite>().width = (int)(24*aspectRadio);
				CardNumberSprite.GetComponent<UISprite>().height = (int)(36*aspectRadio);
			} else {
				CardNumberSprite.width = (int)(26*aspectRadio);
				CardNumberSprite.height = (int)(40*aspectRadio);
			}
		}

		public void ReverseCard() {
			if (_isFont) {
				transform.GetComponent<UISprite>().spriteName = "card_back";
				CardNumberSprite.gameObject.SetActive(false);
				CardSuitSprite.gameObject.SetActive(false);
//				CardPicSprite.gameObject.SetActive(false);
			} else {
				transform.GetComponent<UISprite>().spriteName = "card_font";
				CardNumberSprite.gameObject.SetActive(true);
				CardSuitSprite.gameObject.SetActive(true);
//				if ((int) _card.cardNumber <= 10) {
//					CardPicSprite.gameObject.SetActive(false);
//				} else {
//					CardPicSprite.gameObject.SetActive(true);
//				}
			}
			_isFont = !_isFont;
		}
	}
}