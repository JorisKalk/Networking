using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using NetworkConnections;
using OSCTools;

/// <summary>
/// The Server is the class that manages network connections with all clients, and 
/// communicates with the game code (Model classes).
/// </summary>
public class Server : MonoBehaviour
{
	// ----- General server code:
	TcpListener listener;
	List<TcpNetworkConnection> connections;
	OSCDispatcher dispatcher;

	/// ------ TicTacToe Server code:
	//TicTacToeBoard board;
	Dictionary<TcpNetworkConnection, int> playerIDs = new Dictionary<TcpNetworkConnection, int>();

	void Start()
    {
		// This server starts with a listener:
		int port = 50006;
		Debug.Log("Starting server at " + port);
		listener = new TcpListener(IPAddress.Any, port);
		listener.Start();

		connections = new List<TcpNetworkConnection>();

		// Initialize the dispatcher and callbacks for incoming OSC messages:
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

    void Update()
    {
		AcceptNewConnections();
		UpdateConnections();
		CleanupConnections();
    }

	void AcceptNewConnections() {
		if (listener.Pending()) {
			TcpClient client = listener.AcceptTcpClient();
			TcpNetworkConnection connection = new TcpNetworkConnection(client);
			connections.Add(connection);
			Debug.Log("Server: Adding new connection from " + connection.Remote);
			ClientJoined(connection);
		}
	}
	void ClientJoined(TcpNetworkConnection newClient) {
		if (playerIDs.Count < 2) {
			// We had fewer than 2 players, so this new client will be a player.
			playerIDs[newClient] = playerIDs.Count + 1;
			Debug.Log($"Registering new player: {newClient.Remote} = player {playerIDs[newClient]}");
			if (playerIDs.Count == 2) { // start game
				Debug.Log("Server: starting game");
				foreach (var pid in playerIDs.Keys) {
					SendPrivateInformationCommand(playerIDs[pid], pid);
				}
			}
		} else {
			Debug.Log("Sorry - already have two players");
			// Note: this client is still allowed to join as spectator, but not as player!
			// TODO: Send a message to this client
		}
	}

	void UpdateConnections() {
		foreach (TcpNetworkConnection conn in connections) {
			// The connection will call HandlePacket when a packet is available:
			while (conn.Available()>0) {
				HandlePacket(conn.GetPacket(), conn.Remote);
			}
		}
	}

	void HandlePacket(byte[] packet, IPEndPoint remote) {
		OSCMessageIn mess = new OSCMessageIn(packet);
		Debug.Log("Message arrives on server: " + mess);

		dispatcher.HandlePacket(packet, remote);
	}

	void CleanupConnections() {
		// TODO
	}

	void Initialize() {
		//board = new TicTacToeBoard();
		// Subscribe to game model events:
		// (Note: we try to keep the game code independent from networking details.)
		//board.OnActivePlayerChange += ActivePlayerChangeRpc;
		//board.OnCellChange += CellChangeRpc;
		//board.OnGameOver += GameOverRpc;
		// (Note: no unsubscribe needed in OnDestroy, since the server owns the private board variable.)

		// Subscribe listeners for incoming messages:
		// The (optional) list of parameter types (OSCUtil.INT) lets the dispatcher filter
		//  messages that do not satisfy the expected signature (=parameter list):
		dispatcher.AddListener("/Reset", ResetRpc);
		dispatcher.AddListener("/MakeMove", MakeMoveRpc, 
			OSCUtil.INT, OSCUtil.INT);
	}

	// ----- Handle incoming RPCs (called by dispatcher):

	void MakeMoveRpc(OSCMessageIn message, IPEndPoint remote) {
		int row = message.ReadInt();
		int col = message.ReadInt();
		Debug.Log($"S: Make move {row},{col}. Remote={remote}");
		if (playerIDs.Count<2) {
			Debug.Log("Waiting for more players");
			return;
		}
		// Looping over all players to find the player ID:
		//  a bit ugly, but acceptable since we only have two players.
		foreach (var conn in playerIDs.Keys) {
			Debug.Log("Checking " + conn.Remote);
			// Warning: must use Equals, not == !
			// https://stackoverflow.com/questions/2782973/comparison-of-ipendpoint-objects-not-working !!!
			if (conn.Remote.Equals(remote)) { 
				Debug.Log("This client is a player - allowed to make moves");
				//board.MakeMove(row, col, playerIDs[conn]);
			}
		}
	}
	void ResetRpc(OSCMessageIn message, IPEndPoint remote) {
		// Only allow reset when game is over, and only when sent by one of the two active players:
		// (Note: this is a LINQ query using a lambda function. Writing a for loop
		//  and if statement is fine too, but more code.)
		//if (board.activePlayer == 0 && playerIDs.Keys.Select((a) => a.Remote).Contains(remote)) {
		//	board.Reset();
		//} else {
		//	Debug.Log("Won't reset active game / spectators cannot reset!");
		//}
	}

	// ----- Outgoing RPCs:
	// This RPC is called when a client joins who is a player:
	void SendPrivateInformationCommand(int playerID, TcpNetworkConnection connection) {
		OSCMessageOut message = new OSCMessageOut("/PlayerInfo").AddInt(playerID);
		connection.Send(message.GetBytes()); // private message
	}
	// These three RPCs are called by game model events (TicTacToeBoard):
	public void CellChangeRpc(int row, int col, int value) {
		OSCMessageOut message = new OSCMessageOut("/CellChange").AddInt(row).AddInt(col).AddInt(value);
		Broadcast(message.GetBytes());
	}
	public void ActivePlayerChangeRpc(int player) {
		OSCMessageOut message = new OSCMessageOut("/ActivePlayer").AddInt(player);
		Broadcast(message.GetBytes());
	}
	public void GameOverRpc(int winner) {
		OSCMessageOut message = new OSCMessageOut("/GameOver").AddInt(winner);
		Broadcast(message.GetBytes());
	}
	void Broadcast(byte[] packet) {
		foreach (var conn in connections) {
			conn.Send(packet);
		}
	}
}
