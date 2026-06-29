using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using NetworkConnections;



//add check if its the players turn
public class GameSystem
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
    private int currentPlayerIndex;
    private int reverseMultiplier = 1;
    private CardColor currentWildColor = CardColor.NULL;

    private int activePlayer = 1;
    private int skipTurnModifier = 1;

    [Header("References")]
    [SerializeField]
    private DisplayCards cardDisplay;
    [SerializeField]
    private GameObject pileCardButton;
    [SerializeField]
    private TextMeshProUGUI pileCardButtonText;
    [SerializeField]
    private TextMeshProUGUI playerIndicator;

    //new variables
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


    //new code
    public void StartSystem()
    {
        pile = new CardDeck();
        pile.Initialize();

        pileCardButton = GameObject.Find("PileCardDisplay");
        pileCardButtonText = pileCardButton.GetComponent<TextMeshProUGUI>();

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
    //---------------------------------------------------------------------

    //void Start()
    //{
    //    pile.Initialize();
    //    InitializePlayers();
    //    currentTopCard = pile.GetSingleCard();
    //    if (currentTopCard.cardType == Card.CardType.WILD)
    //    {
    //        currentWildColor = CardColor.RED;
    //    }
    //    UpdateCurrentCardDisplay(currentWildColor);

    //    Debug.Log("Game starts with: " + currentTopCard.ToString());
    //    currentTopCardInspector = currentTopCard.ToString();
    //    playerIndicator.text = "Player: " + currentPlayer.ToString();
    //    cardDisplay.DisplayNewCards(players[0]);
    //}

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
        //players[currentPlayerIndex].CardPlayed(card);

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

        //active player change needs to be added
        //add the correct events to make the card play go correctly

        //if (players[currentPlayerIndex].heldCards.Count == 0)
        //{
        //    Debug.Log("Player " + currentPlayer.ToString() + " has won the game!");
        //    winner = true;
        //    return;
        //}
        NextPlayer(reverseMultiplier);
        //playerIndicator.text = "Player: " + activePlayer.ToString();
        //cardDisplay.DisplayNewCards(players[currentPlayerIndex]);

        skipTurnModifier = 1;
    }

    //client, probably move this to the Player script instead
    public void DrawCardButtonPressed()
    {
        players[currentPlayerIndex].AddSingleCardToHand(pile.GetSingleCard());
        NextPlayer(reverseMultiplier);
        playerIndicator.text = "Player: " + activePlayer.ToString();
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
                    skipTurnModifier = 2;
                    break;
                case Actions.REVERSE:
                    reverseMultiplier *= -1;
                    break;
                case Actions.DRAW_TWO:
                    skipTurnModifier = 2;
                    OnPlayerDrawsCards?.Invoke(GetNextPlayer(reverseMultiplier), 2);
                    currentPlayerIndex = activePlayer;
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
                currentPlayerIndex = activePlayer;
            }
        }
    }

    void Update()
    {
        currentPlayerIndex = activePlayer;
        //if (!winner) GameTest();
        if (players[currentPlayerIndex].heldCards.Count == 0)
        {
            Debug.Log("Player " + activePlayer.ToString() + " has won the game!");
            winner = true;
            return;
        }
    }



    private void GameTest()
    {
        int currentPlayerIndex = activePlayer;

        playerIndicator.text = "Player: " + activePlayer.ToString();

        cardDisplay.DisplayNewCards(players[currentPlayerIndex]);

        foreach (Card card in players[currentPlayerIndex].heldCards)
        {
            if (CheckCardPlayable(card))
            {
                CardPlayed(card);
                players[currentPlayerIndex].CardPlayed(card);
                Debug.Log("Player " + activePlayer.ToString() + " played: " + card.ToString() + "\n" +
                    "Player " + activePlayer.ToString() + " has " + players[currentPlayerIndex].heldCards.Count + " cards left.");
                if (players[currentPlayerIndex].heldCards.Count == 0)
                {
                    Debug.Log("Player " + activePlayer.ToString() + " has won the game!");
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
                            Debug.Log("Player " + activePlayer.ToString() + " skips their turn.");
                            break;
                        case Actions.REVERSE:
                            reverseMultiplier *= -1;
                            Debug.Log("The turn order is now reversed.");
                            break;
                        case Actions.DRAW_TWO:
                            NextPlayer(reverseMultiplier);
                            currentPlayerIndex = activePlayer                           ;
                            players[currentPlayerIndex].AddCardsToHand(pile.GetCards(2));
                            Debug.Log("Player " + activePlayer.ToString() + " drew 2 cards and skipped their turn.\n" +
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
                        currentPlayerIndex = activePlayer;
                        players[currentPlayerIndex].AddCardsToHand(pile.GetCards(4));
                        Debug.Log("Player " + activePlayer.ToString() + " drew 4 cards and skipped their turn.\n" +
                            "They now have " + players[currentPlayerIndex].heldCards.Count.ToString() + " cards left.");
                    }
                }
                    NextPlayer(reverseMultiplier);
                return;
            }
        }

        players[currentPlayerIndex].AddSingleCardToHand(pile.GetSingleCard());
        Debug.Log("Player " + activePlayer.ToString() + " drew a card from the pile, they now have " + 
            players[currentPlayerIndex].heldCards.Count.ToString() + " cards left.");
        NextPlayer(reverseMultiplier);
    }

    private void NextPlayer(int turn)
    {
        activePlayer += turn * skipTurnModifier;
        if (activePlayer > playerAmount) activePlayer -= playerAmount;
        else if (activePlayer < 1) activePlayer += playerAmount;

        currentPlayerIndex = activePlayer;

        OnActivePlayerChanged?.Invoke(activePlayer);
    }

    public int GetNextPlayer(int turn)
    {
        int nextPlayer = activePlayer + turn;
        if (nextPlayer > playerAmount) nextPlayer -= playerAmount;
        else if (nextPlayer < 1) nextPlayer += playerAmount;
        return nextPlayer;
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
