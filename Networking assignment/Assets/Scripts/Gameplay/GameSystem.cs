using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;



//add check if its the players turn
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
    private int currentPlayerIndex;
    private int reverseMultiplier = 1;
    private CardColor currentWildColor = CardColor.NULL;

    [Header("References")]
    [SerializeField]
    private DisplayCards cardDisplay;
    [SerializeField]
    private GameObject pileCardButton;
    [SerializeField]
    private TextMeshProUGUI pileCardButtonText;
    [SerializeField]
    private TextMeshProUGUI playerIndicator;

    void Start()
    {
        pile.Initialize();
        InitializePlayers();
        currentTopCard = pile.GetSingleCard();
        if (currentTopCard.cardType == Card.CardType.WILD)
        {
            currentWildColor = CardColor.RED;
        }
        UpdateCurrentCardDisplay(currentWildColor);

        Debug.Log("Game starts with: " + currentTopCard.ToString());
        currentTopCardInspector = currentTopCard.ToString();
        playerIndicator.text = "Player: " + currentPlayer.ToString();
        cardDisplay.DisplayNewCards(players[0]);
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

    public void CardPlayed(Card card, CardColor wildColorChoice = CardColor.NULL)
    {
        players[currentPlayerIndex].CardPlayed(card);

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
        UpdateCurrentCardDisplay(wildColorChoice);
        if (players[currentPlayerIndex].heldCards.Count == 0)
        {
            Debug.Log("Player " + currentPlayer.ToString() + " has won the game!");
            winner = true;
            return;
        }
        NextPlayer(reverseMultiplier);
        playerIndicator.text = "Player: " + currentPlayer.ToString();
        cardDisplay.DisplayNewCards(players[currentPlayerIndex]);
    }

    public void DrawCardButtonPressed()
    {
        players[currentPlayerIndex].AddSingleCardToHand(pile.GetSingleCard());
        NextPlayer(reverseMultiplier);
        playerIndicator.text = "Player: " + currentPlayer.ToString();
        cardDisplay.DisplayNewCards(players[currentPlayerIndex]);
    }

    private void CheckAction(Card card)
    {
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
    }

    void Update()
    {
        currentPlayerIndex = currentPlayer - 1;
        //if (!winner) GameTest();
        if (players[currentPlayerIndex].heldCards.Count == 0)
        {
            Debug.Log("Player " + currentPlayer.ToString() + " has won the game!");
            winner = true;
            return;
        }
    }



    private void GameTest()
    {
        int currentPlayerIndex = currentPlayer - 1;

        playerIndicator.text = "Player: " + currentPlayer.ToString();

        cardDisplay.DisplayNewCards(players[currentPlayerIndex]);

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

        currentPlayerIndex = currentPlayer - 1;
    }

    private void UpdateCurrentCardDisplay(CardColor wildColorChoice = CardColor.NULL)
    {
        Image button = pileCardButton.GetComponent<Image>();

        CardColor checkedColor;

        if (wildColorChoice == CardColor.NULL)
        {
            checkedColor = currentTopCard.color;
        }
        else
        {
            checkedColor = wildColorChoice;
        }

        switch (checkedColor)
        {
            case CardColor.RED:
                button.color = Color.red;
                pileCardButtonText.color = Color.white;
                break;
            case CardColor.GREEN:
                button.color = Color.green;
                pileCardButtonText.color = Color.black;
                break;
            case CardColor.BLUE:
                button.color = Color.blue;
                pileCardButtonText.color = Color.white;
                break;
            case CardColor.YELLOW:
                button.color = Color.yellow;
                pileCardButtonText.color = Color.black;
                break;
            case CardColor.BLACK:
                button.color = Color.black;
                pileCardButtonText.color = Color.white;
                break;
        }

        pileCardButtonText.text = currentTopCard.ToString();
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
