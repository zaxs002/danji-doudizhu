using System;

namespace model {
	public class Card {
		public GameConst.Suit cardSuit;
		public GameConst.CardNumber cardNumber;
		public GameConst.CardWeight cardWeight;


		public Card(GameConst.Suit cardSuit, GameConst.CardNumber cardNumber, GameConst.CardWeight cardWeight) {
			this.cardSuit = cardSuit;
			this.cardNumber = cardNumber;
			this.cardWeight = cardWeight;
		}

		public Card() {
		}

		public override string ToString() {
			return string.Format("CardSuit: {0}, CardNumber: {1}, CardWeight: {2}", cardSuit, cardNumber,
				(int) cardWeight);
		}

	    public static GameConst.CardWeight GetWeightByNumber(GameConst.CardNumber cardNumber) {
	        GameConst.CardWeight weight = (GameConst.CardWeight) GameConst.CardWeightList[(int) cardNumber - 1];
	        return weight;
	    }
	}
}