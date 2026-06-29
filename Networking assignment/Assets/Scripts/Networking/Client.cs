using UnityEngine;
using System.Net;
using System.Net.Sockets;
using NetworkConnections;
using OSCTools;

/// <summary>
/// The client is the class that lets game code (Controller and View classes) communicate with 
/// the server, and handles network connections.
/// </summary>
public class Client : MonoBehaviour
{
	// ----- General client things:
	public IPAddress ServerIP = IPAddress.Loopback;
	TcpNetworkConnection connection;
	OSCDispatcher dispatcher;

	// ----- TicTacToe client things:

	// Views subscribe here, on any client:
	public delegate void CellChangeEvent(int row, int col, int value);
	public event CellChangeEvent OnCellChange;

	public delegate void ActivePlayerChangeEvent(int activePlayer);
	public event ActivePlayerChangeEvent OnActivePlayerChange;

	public event System.Action<int> OnPlayerInfoReceived;

	public delegate void GameOverEvent(int winner);
	public event GameOverEvent OnGameOver;

	void Start()
    {
		TcpClient client = new TcpClient();
		client.Connect(new IPEndPoint(ServerIP, 50006));
		connection = new TcpNetworkConnection(client);
		// TODO: error handling

		Debug.Log("Starting client, connecting to " + ServerIP);

		// Initialize the dispatcher and callbacks for incoming OSC messages:
		dispatcher = new OSCDispatcher();
		dispatcher.ShowIncomingMessages = true;
		Initialize();
    }

	/// <summary>
	/// Called from NetworkConnection callback (connection.Update), when a packet arrives:
	/// </summary>
	void HandlePacket(byte[] packet, IPEndPoint remote) {
		OSCMessageIn mess = new OSCMessageIn(packet);
		Debug.Log("Message arrives on client: " + mess);
		dispatcher.HandlePacket(packet, remote);
	}

	void Update()
    {
		// Check for incoming packets, and deal with them:
		while (connection.Available()>0) {
			HandlePacket(connection.GetPacket(), connection.Remote);
		}
		// TODO: disconnect handling
    }

	void Initialize() {
		// The (optional) list of parameter types (OSCUtil.INT) lets the dispatcher filter
		//  messages that do not satisfy the expected signature (=parameter list):
		dispatcher.AddListener("/CellChange", CellChangeRpc, OSCUtil.INT, OSCUtil.INT, OSCUtil.INT); 
		dispatcher.AddListener("/ActivePlayer", ActivePlayerChangeRpc, OSCUtil.INT);
		dispatcher.AddListener("/GameOver", GameOverRpc, OSCUtil.INT);
		dispatcher.AddListener("/PlayerInfo", PlayerInfoRpc, OSCUtil.INT);
	}

	// ----- Incoming RPCs (events are triggered, and View classes subscribe):

	void CellChangeRpc(OSCMessageIn message, IPEndPoint remote) {
		int row = message.ReadInt();
		int col = message.ReadInt();
		int value = message.ReadInt();
		OnCellChange?.Invoke(row, col, value);
	}
	void ActivePlayerChangeRpc(OSCMessageIn message, IPEndPoint remote) {
		int activePlayer = message.ReadInt();
		OnActivePlayerChange?.Invoke(activePlayer);
	}
	void GameOverRpc(OSCMessageIn message, IPEndPoint remote) {
		int winner = message.ReadInt();
		OnGameOver?.Invoke(winner);
	}
	void PlayerInfoRpc(OSCMessageIn message, IPEndPoint remote) {
		int playerIndex = message.ReadInt();
		OnPlayerInfoReceived?.Invoke(playerIndex);
	}

	// ----- Outgoing RPCs (called from Controller):

	public void MakeMoveRequest(int row, int col) {
		OSCMessageOut message = new OSCMessageOut("/MakeMove").AddInt(row).AddInt(col);
		connection.Send(message.GetBytes());
	}
	public void ResetRequest() {
		OSCMessageOut message = new OSCMessageOut("/Reset");
		connection.Send(message.GetBytes());
	}
}
