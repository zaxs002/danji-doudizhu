using System.Collections;
using System.Collections.Generic;

public class GameConst {

	public static float CardBetween = 86;
	/// <summary>
	///   <para>花色</para>
	/// </summary>
	public enum Suit {
		None = 0, //大小王花色
		Dianmond = 1, //方片
		Clubs = 2, //梅花
		Heart = 3, //红心
		Spade = 4 //黑桃
	}

	/// <summary>
	///   <para>花色列表</para>
	/// </summary>
	public static ArrayList CardSuitList = new ArrayList() {
		Suit.Dianmond, //方片
		Suit.Clubs, //梅花
		Suit.Heart, //红心
		Suit.Spade //黑桃
	};

	/// <summary>
	///   <para>牌的数字</para>
	/// </summary>
	public enum CardNumber {
		A = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		J = 11,
		Q = 12,
		K = 13,
		SmallJoker = 14,
		BigJoker = 15,
	}

	/// <summary>
	///   <para>牌的数字列表</para>
	/// </summary>
	public static ArrayList CardNumberList = new ArrayList() {
		CardNumber.A,
		CardNumber.Two,
		CardNumber.Three,
		CardNumber.Four,
		CardNumber.Five,
		CardNumber.Six,
		CardNumber.Seven,
		CardNumber.Eight,
		CardNumber.Nine,
		CardNumber.Ten,
		CardNumber.J,
		CardNumber.Q,
		CardNumber.K,
		CardNumber.SmallJoker,
		CardNumber.BigJoker
	};

	/// <summary>
	///   <para>牌的权重</para>
	/// </summary>
	public enum CardWeight {
		A = 14,
		Two = 15,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		J = 11,
		Q = 12,
		K = 13,
		SmallJoker = 16,
		BigJoker = 17
	}

	/// <summary>
	///   <para>牌的权重列表</para>
	/// </summary>
	public static ArrayList CardWeightList = new ArrayList() {
		CardWeight.A,
		CardWeight.Two,
		CardWeight.Three,
		CardWeight.Four,
		CardWeight.Five,
		CardWeight.Six,
		CardWeight.Seven,
		CardWeight.Eight,
		CardWeight.Nine,
		CardWeight.Ten,
		CardWeight.J,
		CardWeight.Q,
		CardWeight.K,
		CardWeight.SmallJoker,
		CardWeight.BigJoker
	};
}