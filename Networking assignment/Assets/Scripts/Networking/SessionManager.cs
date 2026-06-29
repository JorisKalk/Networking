using UnityEngine;

/// <summary>
/// The scene starts with a SessionManager, which allows use to choose whether this instance
/// will be client, server or both (=host).
/// </summary>
public class SessionManager : MonoBehaviour {
	Player controller;

	bool IsClient = false;
	bool IsServer = false;

	private void OnGUI() {
		GUILayout.BeginArea(new Rect(300, 10, 300, 300));
		if (!IsClient && !IsServer) {
			StartButtons();
		}
		GUILayout.EndArea();
	}

	void StartButtons() {
		if (GUILayout.Button("Host")) {
			StartServer();
			StartClient();
		} 
		if (GUILayout.Button("Client")) {
			StartClient();
		}
		if (GUILayout.Button("Server")) {
			StartServer();
		}
	}

	void StartServer() {
		Debug.Log("Starting server: creating board");

		UnoServer server = GetComponent<UnoServer>();
		server.enabled = true;

		IsServer = true;
	}
	void StartClient() {
		Debug.Log($"Starting client: enabling controller");

		UnoClient client = GetComponent<UnoClient>();
		client.enabled = true;

		controller = FindAnyObjectByType<Player>();
		controller.enabled = true;

		IsClient = true;
	}	
}
