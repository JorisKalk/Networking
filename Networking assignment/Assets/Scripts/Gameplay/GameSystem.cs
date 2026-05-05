using UnityEngine;
using System.Collections.Generic;

public class GameSystem : MonoBehaviour
{
    [SerializeField]
    private CardDeck pile;

    [Header("Players")]
    [SerializeField]
    private int playerAmount = 4;
    [SerializeField]
    private List<PlayerData> players = new List<PlayerData>();

    [Header("Gameplay values")]
    [SerializeField]
    private int startingAmountCards = 7;

    [Header("Test values during gameplay")]
    public string currentTopCardInspector;
    private bool winner = false;

    private Card currentTopCard;
    private int currentPlayer = 1;
    private int reverseMultiplier = 1;

    void Start()
    {
        pile.Initialize();
        InitializePlayers();
        currentTopCard = pile.GetSingleCard();

        Debug.Log("Game starts with: " + currentTopCard.ToString());
        currentTopCardInspector = currentTopCard.ToString();
    }

    private void InitializePlayers()
    {
        for (int i = 1; i <= playerAmount; i++)
        {
            PlayerData player = new PlayerData(i);
            player.AddCardsToHand(pile.GetCards(startingAmountCards));
            players.Add(player);
        }
    }

    private void CardPlayed(Card card)
    {
        pile.ReturnCardToPile(currentTopCard);
        currentTopCard = card;
        currentTopCardInspector = currentTopCard.ToString();
    }

    void Update()
    {
        if (!winner) GameTest();
    }

    private void GameTest()
    {
        int currentPlayerIndex = currentPlayer - 1;

        foreach (Card card in players[currentPlayerIndex].heldCards)
        {
            if (CheckCardPlayable(card))
            {
                CardPlayed(card);
                players[currentPlayerIndex].CardPlayed(card);
                Debug.Log("Player " + currentPlayer.ToString() + " played: " + card.ToString() + "\n" +
                    "Player " + currentPlayer.ToString() + " has " + players[currentPlayerIndex].heldCards.Count + " cards left.");
                if (players[currentPlayerIndex].heldCards.Count == 0)
                {
                    Debug.Log("Player " + currentPlayer.ToString() + " has won the game!");
                    winner = true;
                    return;
                }
                if (card.cardType == Card.CardType.ACTION)
                {
                    ActionCard currentCard = (ActionCard)card;
                    switch (currentCard.action)
                    {
                        case Actions.SKIP:
                            NextPlayer(reverseMultiplier);
                            Debug.Log("Player " + currentPlayer.ToString() + " skips their turn.");
                            break;
                        case Actions.REVERSE:
                            reverseMultiplier *= -1;
                            Debug.Log("The turn order is now reversed.");
                            break;
                        case Actions.DRAW_TWO:
                            NextPlayer(reverseMultiplier);
                            currentPlayerIndex = currentPlayer - 1;
                            players[currentPlayerIndex].AddCardsToHand(pile.GetCards(2));
                            Debug.Log("Player " + currentPlayer.ToString() + " drew 2 cards and skipped their turn.\n" +
                                "They now have " + players[currentPlayerIndex].heldCards.Count.ToString() + " cards left.");
                            break;
                    }
                }
                else if (card.cardType == Card.CardType.WILD)
                {
                    WildCard currentCard = (WildCard)card;
                    if (currentCard.wildAction == WildActions.DRAW_FOUR)
                    {
                        NextPlayer(reverseMultiplier);
                        currentPlayerIndex = currentPlayer - 1;
                        players[currentPlayerIndex].AddCardsToHand(pile.GetCards(4));
                        Debug.Log("Player " + currentPlayer.ToString() + " drew 4 cards and skipped their turn.\n" +
                            "They now have " + players[currentPlayerIndex].heldCards.Count.ToString() + " cards left.");
                    }
                }
                    NextPlayer(reverseMultiplier);
                return;
            }
        }

        players[currentPlayerIndex].AddSingleCardToHand(pile.GetSingleCard());
        Debug.Log("Player " + currentPlayer.ToString() + " drew a card from the pile, they now have " + 
            players[currentPlayerIndex].heldCards.Count.ToString() + " cards left.");
        NextPlayer(reverseMultiplier);
    }

    private void NextPlayer(int turn)
    {
        currentPlayer += turn;
        if (currentPlayer > playerAmount) currentPlayer -= playerAmount;
        else if (currentPlayer < 1) currentPlayer += playerAmount;
    }

    private bool CheckCardPlayable(Card card)
    {
        if (currentTopCard.cardType == Card.CardType.WILD || card.cardType == Card.CardType.WILD) return true;
        else if (currentTopCard.color == card.color) return true;
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
