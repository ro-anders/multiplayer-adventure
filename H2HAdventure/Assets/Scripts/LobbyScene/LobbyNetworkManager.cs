using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LobbyNetworkManager : NetworkManager {

    public LobbyController lobbyController;

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        // The only time this gets called is if we get disconnected from the Host
        Debug.Log("Disconnected from host.");
        base.OnClientDisconnect(conn);
        lobbyController.OnHostDropped();
    }
}
