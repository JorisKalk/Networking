using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Player : MonoBehaviour
{
    UnoClient client;
    DisplayCards cardDisplay;
    PlayerData player;


    Card currentAttemptedCard = null;

    [SerializeField]
    private TextMeshProUGUI playerIndicator;

    void Start()
    {
        client = FindAnyObjectByType<UnoClient>();
        cardDisplay = FindAnyObjectByType<DisplayCards>();
        player = new PlayerData();

        Initialize();
    }

    private void Initialize()
    {
        client.OnPlayerReceivesCards += OnReceiveCards;
        client.OnMoveVerified += OnMoveVerified;
        client.OnActivePlayerChanged += OnChangeActivePlayer;
        client.OnGameOver += OnGameOver;

        cardDisplay.SubscribeEvents(client);
    }

    public void MakeMove(Card card)
    {
        Debug.Log("Tried to play card: " + card.ToString());
        if (client != null && client.enabled)
        {
            currentAttemptedCard = card;
            client.MakeMoveRequest(card);
        }
    }

    public void ChooseColor(CardColor color)
    {
        Debug.Log("Tried to choose color: " + color.ToString());
        if (client != null && client.enabled)
        {
            client.ColorChoice(color);
        }
    }

    public void DrawCard()
    {
        Debug.Log("Tried to draw a card");
        if (client != null && client.enabled)
        {
            client.DrawCardRequest();
        }
    }

    private void OnReceiveCards(List<Card> cards)
    {
        player.AddCardsToHand(cards);
        cardDisplay.DisplayNewCards(player);
    }

    private void OnMoveVerified(bool isValidMove, string reason)
    {
        if (isValidMove)
        {
            if (currentAttemptedCard != null)
            {
                Debug.Log("Card succesfully played");
                player.CardPlayed(currentAttemptedCard);
                if (player.heldCards.Count == 0)
                {
                    client.NoCardsLeft();
                }
            }
            else
            {
                Debug.Log("Could not find the played card");
            }
            
        }
        else
        {
            Debug.Log("Move denied for reason: " + reason);
        }

        currentAttemptedCard = null;

        cardDisplay.DisplayNewCards(player);
    }

    private void OnChangeActivePlayer(int player)
    {
        playerIndicator.text = "Player: " + player.ToString();
    }

    private void OnGameOver(int winner)
    {
        playerIndicator.text = "Winner: " + winner.ToString();
    }
}
