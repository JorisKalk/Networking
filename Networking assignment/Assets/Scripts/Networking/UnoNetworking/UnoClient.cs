using UnityEngine;
using System.Net;
using System.Net.Sockets;
using NetworkConnections;
using OSCTools;
using NUnit.Framework;
using System.Collections.Generic;

public class UnoClient : MonoBehaviour
{
    public IPAddress ServerIP = IPAddress.Loopback;
    TcpNetworkConnection connection;
    OSCDispatcher dispatcher;

    CardEnumsConverter enumConverter;

    //TODO: subscribe display scripts to these events
    public delegate void TopCardChangedEvent(Card card, CardColor wildCardColorChoice = CardColor.NULL);
    public event TopCardChangedEvent OnTopCardChanged;

    public delegate void ActivePlayerChangedEvent(int player);
    public event ActivePlayerChangedEvent OnActivePlayerChanged;

    public delegate void PlayerDrawsCardsEvent(int player, int amount);
    public event PlayerDrawsCardsEvent OnPlayerDrawsCards;

    public delegate void PlayerReceivesCardsEvent(List<Card> cards);
    public event PlayerReceivesCardsEvent OnPlayerReceivesCards;

    public delegate void ColorChoiceEvent();
    public event ColorChoiceEvent OnColorChoice;

    public delegate void MoveVerifiedEvent(bool isVerified, string reason);
    public event MoveVerifiedEvent OnMoveVerified;

    public delegate void PlayerInfoReceivedEvent(int playerID);
    public event PlayerInfoReceivedEvent OnPlayerInfoReceived;

    public delegate void GameOverEvent(int winner);
    public event GameOverEvent OnGameOver;

    private int playerID = 0;

    void Start()
    {
        TcpClient client = new TcpClient();
        client.Connect(new IPEndPoint(ServerIP, 50006));
        connection = new TcpNetworkConnection(client);
        // TODO: error handling

        enumConverter = new CardEnumsConverter();

        Debug.Log("Starting client, connecting to " + ServerIP);

        // Initialize the dispatcher and callbacks for incoming OSC messages:
        dispatcher = new OSCDispatcher();
        dispatcher.ShowIncomingMessages = true;
        Initialize();
    }

    void HandlePacket(byte[] packet, IPEndPoint remote)
    {
        OSCMessageIn mess = new OSCMessageIn(packet);
        Debug.Log("Message arrives on client: " + mess);
        dispatcher.HandlePacket(packet, remote);
    }

    void Update()
    {
        while (connection.Available() > 0)
        {
            HandlePacket(connection.GetPacket(), connection.Remote);
        }
    }

    private void Initialize()
    {
        dispatcher.AddListener("/TopCardChanged", TopCardChangedRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT); //cardColor cardType cardValue wildColorChoice
        dispatcher.AddListener("/ActivePlayerChanged", ActivePlayerChangedRpc, OSCUtil.INT); //player
        dispatcher.AddListener("/PlayerDrawsCards", PlayerDrawsCardsRpc, OSCUtil.INT, OSCUtil.INT); //player amount
        dispatcher.AddListener("/ReceiveCards", ReceiveCardsRpc); //will receive a variable amount of ints that should always be divisable by 3 so that they can be converted to cards
        dispatcher.AddListener("/ChooseColor", ChooseColorRpc);
        dispatcher.AddListener("/MoveVerification", VerifyMoveRpc, OSCUtil.BOOL, OSCUtil.STRING); //isValidMove reason
        dispatcher.AddListener("/PlayerInfo", PlayerInfoRpc, OSCUtil.INT); //playerID
        dispatcher.AddListener("/GameOver", GameOverRpc, OSCUtil.INT); //winner(=player)
    }

    private void TopCardChangedRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int cardColorInt = message.ReadInt();
        int cardTypeInt = message.ReadInt();
        int cardValueInt = message.ReadInt();
        int wildcolorChoice = message.ReadInt();

        Card card;
        CardColor cardColor;
        if (enumConverter.ConvertToCardColorEnum(wildcolorChoice) == CardColor.NULL)
        {
            cardColor = enumConverter.ConvertToCardColorEnum(cardColorInt);
            Card.CardType cardType = enumConverter.ConvertToCardTypeEnum(cardTypeInt);
            if (cardType == Card.CardType.NUMBER)
            {
                card = new NumberCard(cardColor, cardValueInt);
                OnTopCardChanged?.Invoke(card);
            }
            else if (cardType == Card.CardType.ACTION)
            {
                card = new ActionCard(cardColor, enumConverter.ConvertToActionsEnum(cardValueInt));
                OnTopCardChanged?.Invoke(card);
            }
        }
        else
        {
            cardColor = enumConverter.ConvertToCardColorEnum(wildcolorChoice);
            card = new WildCard(enumConverter.ConvertToWildActionsEnum(cardValueInt));
            OnTopCardChanged?.Invoke(card, cardColor);
        }
    }

    private void ActivePlayerChangedRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int player = message.ReadInt();
        OnActivePlayerChanged?.Invoke(player);
    }

    private void PlayerDrawsCardsRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int player = message.ReadInt();
        int amount = message.ReadInt();
    }

    private void ReceiveCardsRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int amount = message.ReadInt();

        List<Card> cards = new List<Card>();

        for (int i = 0; i < amount; i++)
        {
            int cardColorInt = message.ReadInt();
            int cardTypeInt = message.ReadInt();
            int cardValueInt = message.ReadInt();

            Card card;
            CardColor cardColor = enumConverter.ConvertToCardColorEnum(cardColorInt);

            if (cardColor != CardColor.BLACK)
            {
                Card.CardType cardType = enumConverter.ConvertToCardTypeEnum(cardTypeInt);
                if (cardType == Card.CardType.NUMBER)
                {
                    card = new NumberCard(cardColor, cardValueInt);
                    cards.Add(card);
                }
                else if (cardType == Card.CardType.ACTION)
                {
                    card = new ActionCard(cardColor, enumConverter.ConvertToActionsEnum(cardValueInt));
                    cards.Add(card);
                }
            }
            else
            {
                card = new WildCard(enumConverter.ConvertToWildActionsEnum(cardValueInt));
                cards.Add(card);
            }
        }

        foreach(Card card in cards)
        {
            Debug.Log(card.ToString());
        }

        OnPlayerReceivesCards?.Invoke(cards);
    }

    private void ChooseColorRpc(OSCMessageIn message, IPEndPoint remote)
    {
        OnColorChoice?.Invoke();
    }

    private void VerifyMoveRpc(OSCMessageIn message, IPEndPoint remote)
    {
        bool validMove = message.ReadBool();
        string reason = message.ReadString("No reason given");

        OnMoveVerified?.Invoke(validMove, reason);
    }

    private void PlayerInfoRpc(OSCMessageIn message, IPEndPoint remote)
    {
        playerID = message.ReadInt();
        Debug.Log("playerID is: " + playerID.ToString());
        OnPlayerInfoReceived?.Invoke(playerID);
    }

    private void GameOverRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int winner = message.ReadInt();
        OnGameOver?.Invoke(winner);
    }


    public void MakeMoveRequest(Card card)
    {
        //maybe do a preemptive test if the card is playable here already
        OSCMessageOut message = new OSCMessageOut("/MakeMove").AddInt(playerID);
        switch (card.cardType)
        {
            case Card.CardType.NUMBER:
                NumberCard numberCard = (NumberCard)card;
                message.AddInt((int)numberCard.color).AddInt((int)numberCard.cardType).AddInt(numberCard.numberValue);
                break;
            case Card.CardType.ACTION:
                ActionCard actionCard = (ActionCard)card;
                message.AddInt((int)actionCard.color).AddInt((int)actionCard.cardType).AddInt((int)actionCard.action);
                break;
            case Card.CardType.WILD:
                WildCard wildCard = (WildCard)card;
                message.AddInt((int)wildCard.color).AddInt((int)wildCard.cardType).AddInt((int)wildCard.wildAction);
                break;
            default:
                Debug.Log("card did not have a valid type!");
                return;
        }

        connection.Send(message.GetBytes());
    }

    public void ColorChoice(CardColor color)
    {
        OSCMessageOut message = new OSCMessageOut("/ColorChosen").AddInt(playerID).AddInt((int)color);
        connection.Send(message.GetBytes());
    }

    public void DrawCardRequest()
    {
        OSCMessageOut message = new OSCMessageOut("/DrawCard").AddInt(playerID);
        connection.Send(message.GetBytes());
    }

    public void NoCardsLeft()
    {
        OSCMessageOut message = new OSCMessageOut("/WinnerFound").AddInt(playerID);
        connection.Send(message.GetBytes());
    }
}
