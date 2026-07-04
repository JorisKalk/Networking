using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    UnoClient client;
    DisplayCards cardDisplay;
    PlayerData player;
    MouseTracker mouseTracker;

    CanvasScaler scaler;

    Card currentAttemptedCard = null;

    [SerializeField]
    private TextMeshProUGUI playerIndicator;
    [SerializeField]
    private GameObject cursorIndicator;

    void Start()
    {
        client = FindAnyObjectByType<UnoClient>();
        cardDisplay = FindAnyObjectByType<DisplayCards>();
        player = new PlayerData();
        mouseTracker = new MouseTracker();
        scaler = GameObject.Find("Canvas").GetComponent<CanvasScaler>();
        mouseTracker.AddCanvas(FindAnyObjectByType<Canvas>(), scaler);

        Initialize();
    }

    private void Update()
    {
        if (client.IsActivePlayer())
        {
            mouseTracker.Update();
        }
    }

    private void Initialize()
    {
        client.OnPlayerReceivesCards += OnReceiveCards;
        client.OnMoveVerified += OnMoveVerified;
        client.OnActivePlayerChanged += OnChangeActivePlayer;
        client.OnGameOver += OnGameOver;
        client.OnNewMousePosReceived += MoveMouseIndicator;

        mouseTracker.OnMouseMoved += OnMouseMoved;
        cursorIndicator.transform.SetParent(cardDisplay.GetComponent<Transform>(), false);

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
        player.EmptyHand();
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

    private void MoveMouseIndicator(float mouseX, float mouseY)
    {
        cursorIndicator.transform.position = new Vector2((mouseX / 2 + Screen.width / 4) * scaler.referenceResolution.x, (mouseY / 2 + Screen.height / 4) * scaler.referenceResolution.y);
    }

    private void OnMouseMoved(float mouseX, float mouseY)
    {
        client.SendNewMousePos(mouseX, mouseY);
    }
}
