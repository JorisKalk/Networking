using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using NetworkConnections;

public class GameSystem
{
    [SerializeField]
    private CardDeck pile;

    [Header("Players")]
    [SerializeField]
    private int playerAmount = 4;

    [Header("Gameplay values")]
    [SerializeField]
    private int startingAmountCards = 7;

    [Header("Test values during gameplay")]
    public string currentTopCardInspector;
    private bool winner = false;

    private Card currentTopCard;
    private int reverseMultiplier = 1;
    private CardColor currentWildColor = CardColor.NULL;

    private int activePlayer = 1;
    private int skipTurnModifier = 1;

    [Header("References")]
    [SerializeField]
    private DisplayCards cardDisplay;
    [SerializeField]
    private GameObject pileCardButton;

    public delegate void TopCardChangedEvent(Card card, CardColor color = CardColor.NULL);
    public event TopCardChangedEvent OnTopCardChanged;

    public delegate void ActivePlayerChangedEvent(int player);
    public event ActivePlayerChangedEvent OnActivePlayerChanged;

    public delegate void PlayerDrawsCardsEvent(int player, int amount);
    public event PlayerDrawsCardsEvent OnPlayerDrawsCards;

    public delegate void PlayerReceivesCardsEvent(int amount, List<Card> cards, TcpNetworkConnection connection);
    public event PlayerReceivesCardsEvent OnPlayerReceivesCards;

    public delegate void GameOverEvent(int winner);
    public event GameOverEvent OnGameOver;


    public void StartSystem()
    {
        pile = new CardDeck();
        pile.Initialize();

        currentTopCard = pile.GetSingleCard();
        if (currentTopCard.cardType == Card.CardType.WILD)
        {
            currentWildColor = CardColor.RED;
        }
        OnActivePlayerChanged?.Invoke(activePlayer);
        OnTopCardChanged?.Invoke(currentTopCard, currentWildColor);
    }

    public void HandOutStartingCards(TcpNetworkConnection connection)
    {
        PullNewCards(startingAmountCards, connection);
    }
    
    public void PlayColoredCard(Card card)
    {
        CardPlayed(card);
    }

    public void PlayWildCard(Card card, CardColor wildColorChoice)
    {
        CardPlayed(card, wildColorChoice);
    }

    public void DrawPileCard(int player)
    {
        DrawCards(player, 1);
        NextPlayer(reverseMultiplier);
    }

    public void DrawCards(int player, int amount)
    {
        OnPlayerDrawsCards?.Invoke(player, amount);
    }

    public void PullNewCards(int amount, TcpNetworkConnection connection)
    {
        List<Card> cards = pile.GetCards(amount);
        OnPlayerReceivesCards?.Invoke(amount, cards, connection);
    }

    public void GameOver(int winner)
    {
        OnGameOver?.Invoke(winner);
    }

    public bool CheckActivePlayer(int player)
    {
        return activePlayer == player;
    }

    public void CardPlayed(Card card, CardColor wildColorChoice = CardColor.NULL)
    {
        if (wildColorChoice == CardColor.NULL)
        {
            currentWildColor = CardColor.NULL;
        }
        else
        {
            currentWildColor = wildColorChoice;
        }

        CheckAction(card);

        pile.ReturnCardToPile(currentTopCard);
        currentTopCard = card;
        currentTopCardInspector = currentTopCard.ToString();
        OnTopCardChanged?.Invoke(currentTopCard, wildColorChoice);

        NextPlayer(reverseMultiplier);

        skipTurnModifier = 1;
    }

    private void CheckAction(Card card)
    {
        if (card.cardType == Card.CardType.ACTION)
        {
            ActionCard currentCard = (ActionCard)card;

            switch (currentCard.action)
            {
                case Actions.SKIP:
                    skipTurnModifier = 2;
                    break;
                case Actions.REVERSE:
                    reverseMultiplier *= -1;
                    break;
                case Actions.DRAW_TWO:
                    skipTurnModifier = 2;
                    OnPlayerDrawsCards?.Invoke(GetNextPlayer(reverseMultiplier), 2);
                    break;
            }
        }
        else if (card.cardType == Card.CardType.WILD)
        {
            WildCard currentCard = (WildCard)card;

            if (currentCard.wildAction == WildActions.DRAW_FOUR)
            {
                skipTurnModifier = 2;
                OnPlayerDrawsCards?.Invoke(GetNextPlayer(reverseMultiplier), 4);
            }
        }
    }

    private void NextPlayer(int turn)
    {
        activePlayer += turn * skipTurnModifier;
        if (activePlayer > playerAmount) activePlayer -= playerAmount;
        else if (activePlayer < 1) activePlayer += playerAmount;

        if (!UnoServer.instance.PlayerExists(activePlayer))
        {
            activePlayer += turn;
            if (activePlayer > playerAmount) activePlayer -= playerAmount;
            else if (activePlayer < 1) activePlayer += playerAmount;
        }

        OnActivePlayerChanged?.Invoke(activePlayer);
    }

    public int GetNextPlayer(int turn)
    {
        int nextPlayer = activePlayer + turn;
        if (nextPlayer > playerAmount) nextPlayer -= playerAmount;
        else if (nextPlayer < 1) nextPlayer += playerAmount;
        return nextPlayer;
    }

    public bool CheckCardPlayable(Card card)
    {
        if (winner) return false;

        CardColor checkedColor;

        if (currentWildColor == CardColor.NULL)
        {
            checkedColor = currentTopCard.color;
        }
        else
        {
            checkedColor = currentWildColor;
        }

        if (card.cardType == Card.CardType.WILD) return true;
        else if (checkedColor == card.color) return true;
        else if (currentTopCard.cardType == Card.CardType.NUMBER && card.cardType == Card.CardType.NUMBER)
        {
            NumberCard currentCard = (NumberCard)currentTopCard;
            NumberCard checkedCard = (NumberCard)card;
            if (currentCard.numberValue == checkedCard.numberValue) return true;
            else return false;
        }
        else if (currentTopCard.cardType == Card.CardType.ACTION && card.cardType == Card.CardType.ACTION)
        {
            ActionCard currentCard = (ActionCard)currentTopCard;
            ActionCard checkedCard = (ActionCard)card;
            if (currentCard.action == checkedCard.action) return true;
            else return false;
        }
        else return false;
    }
}
