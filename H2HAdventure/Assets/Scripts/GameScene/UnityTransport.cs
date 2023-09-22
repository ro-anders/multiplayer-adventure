using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class UnityTransport : MonoBehaviour, Transport
{
    public enum ConnectionStates
    {
        INITIATING,
        CONNECTED,
        SHUTDOWN
    }

    private string matchName; // The unique name of the game in Unity's matchmaker
    private bool needMatch = false; // Whether we are using matchmaker and need to setup a match
    private bool waitingOnListMatch = false; // Whether we are waiting for Unity to setup a match
    private ConnectionStates state = ConnectionStates.INITIATING;
    public ConnectionStates ConnectionState
    {
        get { return state; }
    }

    private PlayerSync thisPlayer;

    private List<PlayerSync> allPlayers = new List<PlayerSync>();

    private Queue<RemoteAction> receviedActions = new Queue<RemoteAction>();

    private void Start()
    {
        bool isHosting = SessionInfo.GameToPlay.playerOne == SessionInfo.ThisPlayerId;
        if (SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL)
        {
            if (isHosting)
            {
                // RIPPED
                state = ConnectionStates.CONNECTED;
            }
            else
            {
                // RIPPED
                state = ConnectionStates.CONNECTED;
            }
        }
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER)
        {
            matchName = SessionInfo.GameToPlay.connectionkey;
            // RIPPED
            if (isHosting)
            {
                // RIPPED
            }
            else
            {
                needMatch = true;
            }
        }
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE)
        {
            Debug.Log("Running in single player mode");
            needMatch = false;
            state = ConnectionStates.CONNECTED;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (needMatch && !waitingOnListMatch)
        {
            // RIPPED
            waitingOnListMatch = true;
        }
    }

    public void Disconnect()
    {
        // Disconnect from the match so we can return to the lobby
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE)
        {
            state = ConnectionStates.SHUTDOWN;
        }
        else if ((SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL) ||
            (SessionInfo.NetworkSetup == SessionInfo.Network.DIRECT_CONNECT))
        {
            StartCoroutine(ShutdownLocalNetwork());
        }
        else
        {
            if ((thisPlayer == null) /*  ||  RIPPED */)
            {
                // Host has already disconnected.
                ShutdownNetworkManager();
                state = ConnectionStates.SHUTDOWN;
            }
            else if (thisPlayer.isServer)
            {
                // RIPPED
            }
            else
            {
                // RIPPED
            }
        }
    }

    private IEnumerator ShutdownLocalNetwork()
    {
        yield return new WaitForSeconds(0.5f);
        if ((thisPlayer != null) && (thisPlayer.isServer))
        {
            // RIPPED
        }
        else
        {
            // RIPPED
        }
        ShutdownNetworkManager();
        state = ConnectionStates.SHUTDOWN;
    }

    private void ShutdownNetworkManager()
    {
        // RIPPED
    }

    public void OnMatchCreate(bool success, string extendedInfo/* RIPPED ,MatchInfo matchInfo */)
    {
        if (!success)
        {
            Debug.Log("Error creating Adventure game match.");
            // TODO: FIX0001
        }
        else
        {
            // RIPPED
            Debug.Log("Now hosting h2h game " + matchName);
        }
    }

    private void OnMatchList(bool success, string extendedInfo /* RIPPED, List<MatchInfoSnapshot> matchList */)
    {
        if (!success)
        {
            Debug.Log("Error looking for match.");
            // Try again
            waitingOnListMatch = false;
        }
        else
        {
            // RIPPED
        }
    }

    public virtual void OnMatchJoined(bool success, string extendedInfo /* RIPPED , MatchInfo matchInfo*/)
    {
        if (!success)
        {
            Debug.Log("Error joining lobby");
            // TODO: FIX0001
        }
        else
        {
            // RIPPED
        }
    }

    public void OnDropConnection(bool success, string extendedInfo)
    {
        if (!success)
        {
            // Just report and try to continue with leaving the game.
            // Theoretically possible that match has already been brought down because
            // player hosting match has moved back to lobby
            Debug.Log("Error trying to disconnect from match");
        }
        else
        {
            Debug.Log("Disconnected from match");
        }
        // RIPPED
        ShutdownNetworkManager();
        state = ConnectionStates.SHUTDOWN;
    }

    public void OnDestroyMatch(bool success, string extendedInfo)
    {
        if (!success)
        {
            // Just report and continue with joining game.
            Debug.Log("Error trying to shutdown match");
        }
        else
        {
            Debug.Log("Shutdown match");
        }
        // RIPPED
        ShutdownNetworkManager();
        state = ConnectionStates.SHUTDOWN;
    }


    public void registerSync(PlayerSync inPlayerSync)
    {
        Debug.Log("Registering " + (inPlayerSync.isLocalPlayer ? "local " : "remote ") + "player # " + inPlayerSync.getSlot());
        allPlayers.Add(inPlayerSync);
        if (inPlayerSync.isLocalPlayer)
        {
            thisPlayer = inPlayerSync;
            Debug.Log("Adventure game has been setup for player " + thisPlayer.getSlot());
        }
    }

    void Transport.send(RemoteAction action)
    {
        action.setSender(thisPlayer.getSlot());
        thisPlayer.CmdBroadcast(action.serialize());
    }

    public void receiveBroadcast(int slot, int[] dataPacket)
    {
        ActionType type = (ActionType)dataPacket[0];
        int sender = dataPacket[1];
        if (sender != thisPlayer.getSlot())
        {
            RemoteAction action=null;
            switch (type)
            {
                case ActionType.PLAYER_MOVE:
                    action = new PlayerMoveAction();
                    break;
                case ActionType.PLAYER_PICKUP:
                    action = new PlayerPickupAction();
                    break;
                case ActionType.PLAYER_RESET:
                    action = new PlayerResetAction();
                    break;
                case ActionType.PLAYER_WIN:
                    action = new PlayerWinAction();
                    break;
                case ActionType.DRAGON_MOVE:
                    action = new DragonMoveAction();
                    break;
                case ActionType.DRAGON_STATE:
                    action = new DragonStateAction();
                    break;
                case ActionType.PORTCULLIS_STATE:
                    action = new PortcullisStateAction();
                    break;
                case ActionType.BAT_MOVE:
                    action = new BatMoveAction();
                    break;
                case ActionType.BAT_PICKUP:
                    action = new BatPickupAction();
                    break;
                case ActionType.OBJECT_MOVE:
                    action = new ObjectMoveAction();
                    break;
                case ActionType.PING:
                    action = new PingAction();
                    break;
            }
            action.deserialize(dataPacket);
            receviedActions.Enqueue(action);
        }
    }

    RemoteAction Transport.get()
    {
        if (receviedActions.Count == 0)
        {
            return null;
        }
        else
        {
            RemoteAction nextAction = receviedActions.Dequeue();
            return nextAction;
        }
    }
}
