using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour
{

    public bool isLocalPlayer = true;
    public bool isServer = false;

    //[SyncVar(hook = "OnChangePlayerName")]
    public string playerName = "";

    //[SyncVar(hook = "OnChangeVoiceOnHost")]
    public bool voiceEnabledOnHost = false;

    private static uint lastId = 0;

    public uint Id
    {
        get {
            lastId += 1;
            return lastId;
        }
    }

    void Start()
    {
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform, false);
        // It will put the new player at the bottom of the list, but we don't want it 
        // to go below the end note about other players
        gameObject.transform.SetSiblingIndex(gameObject.transform.GetSiblingIndex() - 1);
        // The lobby controller needs someway to talk to the server, so it uses
        // the LobbyPlayer representing the local player
        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        if (isLocalPlayer)
        {
            CmdSetPlayerName("Phil");
        }
        if (voiceEnabledOnHost)
        {
            UnityEngine.Debug.Log("VoiceOnHost started true");
        }
        RefreshDisplay();
    }

    //[Command]
    public void CmdSetPlayerName(string name) {
        playerName = name;
    }

    //[Command]
    public void CmdHostGame(int numPlayers, int gameNumber, bool diff1, bool diff2,
        uint hostPlayerId, string hostPlayerName)
    {
        System.Random rand = new System.Random();
        GameObject gameGO = Instantiate((GameObject)null);
        GameInLobby game = gameGO.GetComponent<GameInLobby>();
        game.numPlayers = numPlayers;
        game.gameNumber = gameNumber;
        game.diff1 = (diff1 ? DIFF.A : DIFF.B);
        game.diff2 = (diff2 ? DIFF.A : DIFF.B);
        game.playerOne = hostPlayerId;
        game.playerOneName = hostPlayerName;
        game.playerMapping = rand.Next(0, (numPlayers == 2 ? 2 : 6));
        if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER) {
            game.connectionkey = "h2h-" + hostPlayerId + "-" + rand.Next(100);
        } else {
            game.connectionkey = (30000 + rand.Next(100) * 100 + hostPlayerId).ToString();
        }
        // RIPPED
    }

    //[Command]
    public void CmdJoinGame(uint gameId) {
    }

    //[Command]
    public void CmdSignalStartingGame(uint gameId) {
        Debug.Log(this.playerName + " got start game command");
    }

    //[Command]
    public void CmdLeaveGame(uint gameId) {
    }

    //[Command]
    public void CmdPostChat(string message) {
    }

    private void RefreshDisplay() {
        Text thisText = this.GetComponent<Text>();
        thisText.text = ((playerName != null) && (playerName != "") ? playerName : "unknown-" + Id);
    }

    private void OnChangePlayerName(string newPlayerName) {
        playerName = newPlayerName;
        Debug.Log(newPlayerName + " connected to lobby");
        RefreshDisplay();
    }

    private void OnChangeVoiceOnHost(bool newValue)
    {
        if (voiceEnabledOnHost != newValue)
        {
            voiceEnabledOnHost = newValue;
            if (voiceEnabledOnHost)
            {
            }
        }
    }
}
