using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using NetworkConnections;
using OSCTools;
using System;

public class UnoServer : MonoBehaviour
{
    public static UnoServer instance;
    TcpListener listener;
    List<TcpNetworkConnection> connections;
    OSCDispatcher dispatcher;

    CardEnumsConverter enumConverter;

    [SerializeField]
    private int playerCount = 4;

    private GameSystem gameSystem;
    Dictionary<TcpNetworkConnection, PlayerData> playerIDs = new Dictionary<TcpNetworkConnection, PlayerData>();

    private Card currentWildCard;

    private bool hasWinner = false;

    private void Start()
    {
        instance = this;
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
            playerIDs[newClient] = new PlayerData(playerIDs.Count + 1);
            Debug.Log($"Registering new player: {newClient.Remote} = player {playerIDs[newClient].GetPlayerID()}");
            if (playerIDs.Count == playerCount)
            {
                gameSystem.StartSystem();
                Debug.Log("Server: starting game");
                foreach(var pid in playerIDs.Keys)
                {
                    SendPrivateInformationCommand(playerIDs[pid].GetPlayerID(), pid);
                    gameSystem.HandOutStartingCards(pid);
                }
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
        try
        {
            OSCMessageIn mess = new OSCMessageIn(packet);
            Debug.Log("Message arrives on server: " + mess);

            dispatcher.HandlePacket(packet, remote);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private void CleanupConnections()
    {
        List<TcpNetworkConnection> conns = new List<TcpNetworkConnection>(connections);
        foreach (TcpNetworkConnection conn in conns)
        {
            if (conn.Status != ConnectionStatus.Connected && conn.Status != ConnectionStatus.Connecting)
            {
                Debug.Log("client removed: " + conn.Remote.Address + ":" + conn.Remote.Port);
                conn.Close();
                connections.Remove(conn);
                playerIDs.Remove(conn);
            }
        }
    }

    private void Initialize()
    {
        gameSystem = new GameSystem();

        hasWinner = false;

        gameSystem.OnTopCardChanged += TopCardChangedRpc;
        gameSystem.OnActivePlayerChanged += ActivePlayerChangedRpc;
        gameSystem.OnPlayerDrawsCards += PlayerDrawsCardsRpc;
        gameSystem.OnPlayerReceivesCards += PlayerReceivesCards;
        gameSystem.OnGameOver += GameOverRpc;

        dispatcher.AddListener("/MakeMove", MakeMoveRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT); //player cardColor cardType cardValue
        dispatcher.AddListener("/ColorChosen", ColorChosenRpc, OSCUtil.INT, OSCUtil.INT); //player cardColor
        dispatcher.AddListener("/DrawCard", DrawCardRpc, OSCUtil.INT); //player
        dispatcher.AddListener("/MovedMouse", ActiveMouseMovedRpc, OSCUtil.FLOAT, OSCUtil.FLOAT); //mouseX mouseY
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

        PlayerData tempPlayerData = GetPlayerData(player);

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
                    if (!CheckIfPlayerHasCard(player, card))
                    {
                        MoveRefused(player, "You do not have this card!");
                        PlayerReceivesCards(tempPlayerData.heldCards, GetPlayerConnection(player));
                        return;
                    }
                    gameSystem.PlayColoredCard(card);
                    RemoveCardFromPlayer(card, GetPlayerConnection(player));
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
                    if (!CheckIfPlayerHasCard(player, card))
                    {
                        MoveRefused(player, "You do not have this card!");
                        PlayerReceivesCards(tempPlayerData.heldCards, GetPlayerConnection(player));
                        return;
                    }
                    gameSystem.PlayColoredCard(card);
                    RemoveCardFromPlayer(card, GetPlayerConnection(player));
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
            if (!CheckIfPlayerHasCard(player, new WildCard(enumConverter.ConvertToWildActionsEnum(cardValueInt))))
            {
                MoveRefused(player, "You do not have this card!");
                PlayerReceivesCards(tempPlayerData.heldCards, GetPlayerConnection(player));
                return;
            }
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
        RemoveCardFromPlayer(currentWildCard, GetPlayerConnection(player));
        currentWildCard = null;


        MoveAccepted(player);
    }

    private void RemoveCardFromPlayer(Card card, TcpNetworkConnection connection)
    {
        foreach (Card playerCard in playerIDs[connection].heldCards)
        {
            if (playerCard.CompareCard(card))
            {
                playerIDs[connection].CardPlayed(playerCard);
                PlayerCardsRefreshedRpc(playerIDs[connection], connection);
                if (playerIDs[connection].heldCards.Count == 0)
                {
                    gameSystem.GameOver(playerIDs[connection].GetPlayerID());
                }
                return;
            }
        }
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

    private bool CheckIfPlayerHasCard(int player, Card card)
    {
        PlayerData currentPlayerData = GetPlayerData(player);

        //this should not be possible
        if (currentPlayerData == null) return false;

        foreach (Card playerCard in currentPlayerData.heldCards)
        {
            if (playerCard.CompareCard(card)) return true;
        }
        return false;
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

    private void ActiveMouseMovedRpc(OSCMessageIn message, IPEndPoint remote)
    {
        float mouseX = message.ReadFloat();
        float mouseY = message.ReadFloat();
        SendNewMousePosRpc(mouseX, mouseY);
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

    private void PlayerReceivesCards(List<Card> cards, TcpNetworkConnection connection)
    {
        playerIDs[connection].AddCardsToHand(cards);

        PlayerCardsRefreshedRpc(playerIDs[connection], connection);
    }

    private void PlayerCardsRefreshedRpc(PlayerData data, TcpNetworkConnection connection)
    {
        OSCMessageOut message = new OSCMessageOut("/ReceiveCards").AddInt(data.heldCards.Count);

        foreach (Card card in playerIDs[connection].heldCards)
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

    private void SendNewMousePosRpc(float mouseX, float mouseY)
    {
        OSCMessageOut message = new OSCMessageOut("/NewActiveMousePos").AddFloat(mouseX).AddFloat(mouseY);
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

    public bool PlayerExists(int player)
    {
        foreach (PlayerData playerData in playerIDs.Values)
        {
            if (playerData.GetPlayerID() == player) return true;
        }
        return false;
    }

    private TcpNetworkConnection GetPlayerConnection(int player)
    {
        TcpNetworkConnection connection = null;
        foreach (var playerID in playerIDs.Keys)
        {
            if (playerIDs[playerID].GetPlayerID().Equals(player))
            {
                connection = playerID;
                break;
            }
        }
        return connection;
    }

    private PlayerData GetPlayerData(int player)
    {
        PlayerData tempPlayerData = null;
        foreach (PlayerData data in playerIDs.Values)
        {
            if (data.GetPlayerID() == player)
            {
                tempPlayerData = data;
                break;
            }
        }
        return tempPlayerData;
    }
}
