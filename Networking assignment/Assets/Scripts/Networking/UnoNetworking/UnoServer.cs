using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using NetworkConnections;
using OSCTools;

public class UnoServer : MonoBehaviour
{
    TcpListener listener;
    List<TcpNetworkConnection> connections;
    OSCDispatcher dispatcher;

    CardEnumsConverter enumConverter;

    [SerializeField]
    private int playerCount = 4;

    private GameSystem gameSystem;
    Dictionary<TcpNetworkConnection, int> playerIDs = new Dictionary<TcpNetworkConnection, int>();

    private Card currentWildCard;

    private bool hasWinner = false;

    private void Start()
    {
        int port = 50006;
        Debug.Log("Starting server at " + port);
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        enumConverter = new CardEnumsConverter();

        connections = new List<TcpNetworkConnection>();

        // Initialize the dispatcher and callbacks for incoming OSC messages:
        dispatcher = new OSCDispatcher();
        dispatcher.ShowIncomingMessages = true;
        Initialize();
    }



    private void Update()
    {
        AcceptNewConnections();
        UpdateConnections();
        CleanupConnections();
    }

    private void AcceptNewConnections()
    {
        if (listener.Pending())
        {
            TcpClient client = listener.AcceptTcpClient();
            TcpNetworkConnection connection = new TcpNetworkConnection(client);
            connections.Add(connection);
            Debug.Log("Server: Adding new connection from " + connection.Remote);
            ClientJoined(connection);
        }
    }

    private void ClientJoined(TcpNetworkConnection newClient)
    {
        if (playerIDs.Count < playerCount)
        {
            playerIDs[newClient] = playerIDs.Count + 1;
            Debug.Log($"Registering new player: {newClient.Remote} = player {playerIDs[newClient]}");
            if (playerIDs.Count == playerCount)
            { // start game
                gameSystem.StartSystem();
                Debug.Log("Server: starting game");
                foreach (var pid in playerIDs.Keys)
                {
                    SendPrivateInformationCommand(playerIDs[pid], pid);
                    gameSystem.HandOutStartingCards(pid);
                }
            }
            else
            {
                //spectator?
            }
        }
    }

    private void UpdateConnections()
    {
        foreach (TcpNetworkConnection conn in connections)
        {
            // The connection will call HandlePacket when a packet is available:
            while (conn.Available() > 0)
            {
                HandlePacket(conn.GetPacket(), conn.Remote);
            }
        }
    }

    private void HandlePacket(byte[] packet, IPEndPoint remote)
    {
        OSCMessageIn mess = new OSCMessageIn(packet);
        Debug.Log("Message arrives on server: " + mess);

        dispatcher.HandlePacket(packet, remote);
    }

    private void CleanupConnections()
    {
        // TODO
    }

    private void Initialize()
    {
        gameSystem = new GameSystem();

        hasWinner = false;

        gameSystem.OnTopCardChanged += TopCardChangedRpc;
        gameSystem.OnActivePlayerChanged += ActivePlayerChangedRpc;
        gameSystem.OnPlayerDrawsCards += PlayerDrawsCardsRpc;
        gameSystem.OnPlayerReceivesCards += PlayerReceivesCardsRpc;
        gameSystem.OnGameOver += GameOverRpc;

        dispatcher.AddListener("/MakeMove", MakeMoveRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT); //player cardColor cardType cardValue
        dispatcher.AddListener("/ColorChosen", ColorChosenRpc, OSCUtil.INT, OSCUtil.INT); //player cardColor
        dispatcher.AddListener("/DrawCard", DrawCardRpc, OSCUtil.INT); //player
        dispatcher.AddListener("/WinnerFound", WinnerFoundRpc, OSCUtil.INT); //winner
        //cards going to be transfered as multiple ints
    }

    private void MakeMoveRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int player = message.ReadInt();

        if (hasWinner)
        {
            MoveRefused(player, "Match is over!");
            return;
        }

        if (!gameSystem.CheckActivePlayer(player))
        {
            MoveRefused(player, "It is not your turn!");
            return;
        }

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
                if (gameSystem.CheckCardPlayable(card))
                {
                    gameSystem.PlayColoredCard(card);
                    MoveAccepted(player);
                }
                else
                {
                    MoveRefused(player, "Card is not playable.");
                }
            }
            else if (cardType == Card.CardType.ACTION)
            {
                card = new ActionCard(cardColor, enumConverter.ConvertToActionsEnum(cardValueInt));
                if (gameSystem.CheckCardPlayable(card))
                {
                    gameSystem.PlayColoredCard(card);
                    MoveAccepted(player);
                }
                else
                {
                    MoveRefused(player, "Card is not playable.");
                }
            }
        }
        else
        {
            card = new WildCard(enumConverter.ConvertToWildActionsEnum(cardValueInt));
            OSCMessageOut messageOut = new OSCMessageOut("/ChooseColor");
            GetPlayerConnection(player).Send(messageOut.GetBytes());
            currentWildCard = card;
        }
    }

    private void ColorChosenRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int player = message.ReadInt();
        if (!gameSystem.CheckActivePlayer(player))
        {
            MoveRefused(player, "It is not your turn!");
            return;
        }

        int cardColorInt = message.ReadInt();
        CardColor chosenColor = enumConverter.ConvertToCardColorEnum(cardColorInt);

        gameSystem.PlayWildCard(currentWildCard, chosenColor);
        currentWildCard = null;

        MoveAccepted(player);
    }

    private void MoveRefused(int player, string reason = "Reason not given")
    {
        Debug.Log("Move denied for reason: " + reason);
        OSCMessageOut message = new OSCMessageOut("/MoveVerification").AddBool(false).AddString(reason);
        TcpNetworkConnection connection = GetPlayerConnection(player);
        connection.Send(message.GetBytes());
    }

    private void MoveAccepted(int player)
    {
        Debug.Log("Move accepted");
        OSCMessageOut message = new OSCMessageOut("/MoveVerification").AddBool(true).AddString("move accepted");
        TcpNetworkConnection connection = GetPlayerConnection(player);
        connection.Send(message.GetBytes());
    }

    private void DrawCardRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int player = message.ReadInt();

        if (hasWinner)
        {
            MoveRefused(player, "Match is over!");
            return;
        }

        if (!gameSystem.CheckActivePlayer(player))
        {
            MoveRefused(player, "It is not your turn!");
            return;
        }

        gameSystem.DrawPileCard(player);
    }

    private void WinnerFoundRpc(OSCMessageIn message, IPEndPoint remote)
    {
        int winner = message.ReadInt();
        hasWinner = true;
        gameSystem.GameOver(winner);
    }

    private void TopCardChangedRpc(Card card, CardColor wildColorChoice = CardColor.NULL)
    {
        OSCMessageOut message = new OSCMessageOut("/TopCardChanged");
        if (card.cardType == Card.CardType.NUMBER)
        {
            NumberCard numberCard = (NumberCard)card;
            message.AddInt((int)numberCard.color).AddInt((int)numberCard.cardType).AddInt(numberCard.numberValue);
        }
        else if(card.cardType == Card.CardType.ACTION)
        {
            ActionCard actionCard = (ActionCard)card;
            message.AddInt((int)actionCard.color).AddInt((int)actionCard.cardType).AddInt((int)actionCard.action);
        }
        else if(card.cardType == Card.CardType.WILD)
        {
            WildCard wildCard = (WildCard)card;
            message.AddInt((int)wildCard.color).AddInt((int)wildCard.cardType).AddInt((int)wildCard.wildAction);
        }

        message.AddInt((int)wildColorChoice);
        Broadcast(message.GetBytes());
    }

    private void ActivePlayerChangedRpc(int player)
    {
        OSCMessageOut message = new OSCMessageOut("/ActivePlayerChanged").AddInt(player);
        Broadcast(message.GetBytes());
    }

    private void PlayerDrawsCardsRpc(int player, int amount)
    {
        OSCMessageOut message = new OSCMessageOut("/PlayerDrawsCards").AddInt(player).AddInt(amount);
        Broadcast(message.GetBytes());

        gameSystem.PullNewCards(amount, GetPlayerConnection(player));
    }

    //maybe call for their entire hand every time their turn starts? keeps the server authoritive on the player hands
    //or likely will keep the players hand visible to them constantly
    //need to figure this out how i will do the displaying and the updating of it
    //maybe have the hands of the players saved on both the server and on the client and have the server check if the player actually has a card before playing it
    //then also need to remember that the card needs to be removed from both sides when played
    private void PlayerReceivesCardsRpc(int amount, List<Card> cards, TcpNetworkConnection connection)
    {
        OSCMessageOut message = new OSCMessageOut("/ReceiveCards").AddInt(amount);

        foreach(Card card in cards)
        {
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
        }

        connection.Send(message.GetBytes());
    }

    private void GameOverRpc(int winner)
    {
        OSCMessageOut message = new OSCMessageOut("/GameOver").AddInt(winner);
        Broadcast(message.GetBytes());
    }

    private void SendPrivateInformationCommand(int player, TcpNetworkConnection connection)
    {
        OSCMessageOut message = new OSCMessageOut("/PlayerInfo").AddInt(player);
        Debug.Log("sending playerID");
        connection.Send(message.GetBytes());
    }
    private void Broadcast(byte[] packet)
    {
        foreach (var conn in connections)
        {
            conn.Send(packet);
        }
    }

    private TcpNetworkConnection GetPlayerConnection(int player)
    {
        TcpNetworkConnection connection = null;
        foreach (var playerID in playerIDs.Keys)
        {
            if (playerIDs[playerID].Equals(player))
            {
                connection = playerID;
                break;
            }
        }
        return connection;
    }
}
