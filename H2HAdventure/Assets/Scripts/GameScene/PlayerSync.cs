using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class PlayerSync : MonoBehaviour
{
    public bool isLocalPlayer = true;
    public bool isServer = false;

    //[SyncVar(hook = "OnChangePlayerName")]
    public string playerName = "";

    //[SyncVar(hook = "OnChangePlayerId")]
    public uint playerId = GameInLobby.NO_PLAYER;

    //[SyncVar(hook = "OnChangeVoiceOnHost")]
    public bool voiceEnabledOnHost = false;

    private int slot = -1;

    private UnityTransport xport;
    private UnityAdventureView controller;

    // Use this for initialization
    void Start()
    {
        GameObject gameController = GameObject.FindGameObjectWithTag("GameController");
        xport = gameController.GetComponent<UnityTransport>();
        controller = gameController.GetComponent<UnityAdventureView>();
        bool ripped = true;
        if (ripped)
        {
            deduceSlot(SessionInfo.ThisPlayerId);
            CmdSetPlayerId(SessionInfo.ThisPlayerId);
            CmdSetPlayerName(SessionInfo.ThisPlayerName);
        } else if (playerId != GameInLobby.NO_PLAYER)
        {                // Not sure if this ever gets called.
            GameEngine.Logger.Debug("THIS GETS CALLED!!!!!!!!!  Called with " + playerName + "(" + playerId + ")");

            if (!SessionInfo.GameToPlay.IsInGame(playerId))
            {
                GameEngine.Logger.Debug("Unexpected player id:" + playerId);
            }
            else
            {
                deduceSlot(playerId);
                if (playerName != null)
                {
                    GameEngine.Logger.Debug("PlayerSync constructor registering player" + playerName + "(" + playerId + ")");
                    xport = controller.RegisterNewPlayer(this);
                }
            }
        } 
    }

    void OnChangePlayerId(uint newPlayerId)
    {
        GameEngine.Logger.Debug("Setting player id to " + newPlayerId);
        if (newPlayerId != playerId)
        {
            if (!SessionInfo.GameToPlay.IsInGame(newPlayerId))
            {
                GameEngine.Logger.Debug("Unexpected player id:" + newPlayerId);
            } else {
                playerId = newPlayerId;
                deduceSlot(playerId);
                if (playerName != "")
                {
                    GameEngine.Logger.Debug("Player \"" + playerName + "\"'s ID changed to " + newPlayerId + ". Registering player.");
                    xport = controller.RegisterNewPlayer(this);
                }
            }
        }
    }

    void OnChangePlayerName(string newPlayerName)
    {
        GameEngine.Logger.Debug("Setting player name to " + newPlayerName);
        if (newPlayerName != playerName)
        {
            playerName = newPlayerName;
            if (playerId != GameInLobby.NO_PLAYER)
            {
                xport = controller.RegisterNewPlayer(this);
            }
        }
    }

    private void deduceSlot(uint playerIdToCheck)
    {
        slot = -1;
        uint[] players = SessionInfo.GameToPlay.GetPlayersInGameOrder();
        for (int ctr = 0; (ctr < players.Length) && (slot == -1); ++ctr)
        {
            slot = (playerIdToCheck == players[ctr] ? ctr : -1);
        }
    }

    public int getSlot() {
        return slot;
    }

    //[Command]
    public void CmdSetPlayerId(uint id)
    {
        playerId = id;
    }

    //[Command]
    public void CmdSetPlayerName(string name)
    {
        playerName = name;
    }

    //[ClientRpc]
    public void RpcStartGame()
    {
        controller.StartGame();
    }

    //[Command]
    public void CmdBroadcast(int[] dataPacket)
    {
        RpcReceiveBroadcast(dataPacket);
    }

    //[ClientRpc]
    public void RpcReceiveBroadcast(int[] dataPacket)
    {
        xport.receiveBroadcast(slot, dataPacket);
    }

    //[Command]
    public void CmdPostChat(string message)
    {
        controller.GetChatPanelController().BroadcastChatMessage(playerName, message);
    }

    private void OnChangeVoiceOnHost(bool newValue)
    {
        if (voiceEnabledOnHost != newValue)
        {
            voiceEnabledOnHost = newValue;
            if (voiceEnabledOnHost)
            {
                controller.GetChatPanelController().OnTalkEnabledOnHost();
            }
        }
    }
}
