using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public enum CardColor
{
    RED,
    GREEN,
    BLUE,
    YELLOW,
    BLACK
}

public enum Actions
{
    SKIP,
    REVERSE,
    DRAW_TWO,
    NULL
}

public enum WildActions
{
    CHOOSE_COLOR,
    DRAW_FOUR,
    NULL
}

public class CardDeck : MonoBehaviour
{
    [SerializeField]
    private List<Card> cards = new List<Card>();

    [Header("Card generation values")]
    [SerializeField]
    private int minCardValue = 0;
    [SerializeField]
    private int maxCardValue = 9;
    [SerializeField]
    private int amountOfDifferentColors = 4;
    [SerializeField]
    private int amountOfDifferentActionCards = 3;
    [SerializeField]
    private int amountOfDifferentWildCards = 2;
    [SerializeField]
    private int cardDuplicateAmount = 2;
    [SerializeField]
    private int wildCardDuplicateAmount = 4;

    [Header("Cards lists")]
    [SerializeField]
    private List<NumberCard> numberCardsInputList = new List<NumberCard>();
    [SerializeField]
    private List<ActionCard> actionCardsInputList = new List<ActionCard>();
    [SerializeField]
    private List<WildCard> wildCardsInputList = new List<WildCard>();

    [Header("Random test values")]
    [SerializeField]
    private List<string> inspectorCardsList = new List<string>();

    public void Initialize()
    {
        AddNumberCardsToList();
        AddActionCardsToList();
        AddWildCardsToList();
        AddAllCardsToDeck();
    }

    private void AddNumberCardsToList()
    {
        for (int i = minCardValue; i <= maxCardValue; i++)
        {
            for (int j = 1; j <= amountOfDifferentColors; j++)
            {
                AddNumberCard(cardDuplicateAmount, i, GetCardColor(j));
            }
        }
    }

    private void AddActionCardsToList()
    {
        for (int i = 1; i <= amountOfDifferentActionCards; i++)
        {
            for (int j = 1; j <= amountOfDifferentColors; j++)
            {
                AddActionCard(cardDuplicateAmount, GetAction(i), GetCardColor(j));
            }
        }
    }

    private void AddWildCardsToList()
    {
        for(int i = 1; i <= amountOfDifferentWildCards; i++)
        {
            AddWildCard(wildCardDuplicateAmount, GetWildAction(i));
        }
    }

    private void AddAllCardsToDeck()
    {
        foreach(Card card in numberCardsInputList)
        {
            cards.Add(card);
        }
        foreach(Card card in actionCardsInputList)
        {
            cards.Add(card);
        }
        foreach(Card card in wildCardsInputList)
        {
            cards.Add(card);
        }

        UpdateInspectorDeck();
    }

    private void AddNumberCard(int amount, int value, CardColor color)
    {
        for (int i = 1; i <= amount; i++)
        {
            numberCardsInputList.Add(new NumberCard(color, value));
        }
    }

    private void AddActionCard(int amount, Actions action, CardColor color)
    {
        for (int i = 1; i <= amount; i++)
        {
            actionCardsInputList.Add(new ActionCard(color, action));
        }
    }

    private void AddWildCard(int amount, WildActions wildAction)
    {
        for (int i = 1; i <= amount; i++)
        {
            wildCardsInputList.Add(new WildCard(wildAction));
        }
    }

    private CardColor GetCardColor(int selector)
    {
        switch (selector)
        {
            case 1:
                return CardColor.RED;
            case 2:
                return CardColor.GREEN;
            case 3:
                return CardColor.BLUE;
            case 4:
                return CardColor.YELLOW;
            default:
                Debug.Log("If you see this message you did something wrong (color selector)");
                return CardColor.BLACK;
        }
    }

    private Actions GetAction(int selector)
    {
        switch (selector)
        {
            case 1:
                return Actions.SKIP;
            case 2:
                return Actions.REVERSE;
            case 3:
                return Actions.DRAW_TWO;
            default:
                Debug.Log("If you see this message you did something wrong (action selector)");
                return Actions.NULL;
        }
    }

    private WildActions GetWildAction(int selector)
    {
        switch (selector)
        {
            case 1:
                return WildActions.CHOOSE_COLOR;
            case 2:
                return WildActions.DRAW_FOUR;
            default:
                Debug.Log("If you see this message you did something wrong (wildAction selector)");
                return WildActions.NULL;
        }
    }

    public Card GetSingleCard()
    {
        int selectedCard = UnityEngine.Random.Range(0, cards.Count);
        Card cardToGive = cards[selectedCard];
        cards.RemoveAt(selectedCard);
        UpdateInspectorDeck();
        return cardToGive;
    }

    public List<Card> GetCards(int amount)
    {
        if (amount <= 0) return null;

        List<Card> cardsToGive = new List<Card>();
        for (int i = 1; i <= amount; i++)
        {
            int selectedCard = UnityEngine.Random.Range(0, cards.Count);
            cardsToGive.Add(cards[selectedCard]);
            cards.RemoveAt(selectedCard);
        }

        UpdateInspectorDeck();
        return cardsToGive;
    }

    public void ReturnCardToPile(Card card)
    {
        cards.Add(card);
        UpdateInspectorDeck();
    }

    private void UpdateInspectorDeck()
    {
        inspectorCardsList.Clear();
        foreach (Card card in cards)
        {
            inspectorCardsList.Add(card.ToString());
        }
    }
}




[Serializable]
public abstract class Card
{
    public enum CardType
    {
        NUMBER,
        ACTION,
        WILD
    }

    public CardType cardType = new CardType();
    public CardColor color = new CardColor();
}

[Serializable]
public class NumberCard : Card
{
    public int numberValue;

    public NumberCard(CardColor pColor, int pNumberValue)
    {
        color = pColor;
        numberValue = pNumberValue;
        cardType = CardType.NUMBER;
    }

    public override string ToString()
    {
        return color.ToString() + "\n" + numberValue.ToString();
    }
}

[Serializable]
public class ActionCard : Card
{
    public Actions action = new Actions();

    public ActionCard(CardColor pColor, Actions pAction)
    {
        color = pColor;
        action = pAction;
        cardType = CardType.ACTION;
    }

    public override string ToString()
    {
        return color.ToString() + "\n" + action.ToString();
    }
}

[Serializable]
public class WildCard : Card
{
    public WildActions wildAction = new WildActions();

    public WildCard(WildActions pWildAction)
    {
        color = CardColor.BLACK;
        wildAction = pWildAction;
        cardType = CardType.WILD;
    }

    public override string ToString()
    {
        return color.ToString() + "\n" + wildAction.ToString();
    }
}