using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;

public class UnityTransport : MonoBehaviour, Transport
{
    public enum ConnectionStates
    {
        INITIATING,
        CONNECTED,
        SHUTDOWN
    }

    public NetworkManager networkManager;
    private string matchName; // The unique name of the game in Unity's matchmaker
    private bool needMatch = false; // Whether we are using matchmaker and need to setup a match
    private bool waitingOnListMatch = false; // Whether we are waiting for Unity to setup a match
    private ulong matchNetwork; // Identifies the matchmaker match so we can disconnect cleanly later
    private NodeID matchNode;  // Identifies the matchmaker host so we can disconnect cleanly later
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
                networkManager.networkPort = int.Parse(SessionInfo.GameToPlay.connectionkey);
                networkManager.serverBindAddress = "127.0.0.1";
                networkManager.serverBindToIP = true;
                networkManager.StartHost();
                state = ConnectionStates.CONNECTED;
            }
            else
            {
                networkManager.networkPort = int.Parse(SessionInfo.GameToPlay.connectionkey);
                networkManager.StartClient();
                state = ConnectionStates.CONNECTED;
            }
        }
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER)
        {
            matchName = SessionInfo.GameToPlay.connectionkey;
            if (networkManager.matchMaker == null)
            {
                networkManager.StartMatchMaker();
            }
            if (isHosting)
            {
                networkManager.matchMaker.CreateMatch(matchName, (uint)100, true,
                                    "", "", "", 0, 1, OnMatchCreate);
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
            networkManager.matchMaker.ListMatches(0, 20, "", true, 0, 1, OnMatchList);
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
            if ((thisPlayer == null) || (networkManager.matchMaker == null))
            {
                // Host has already disconnected.
                ShutdownNetworkManager();
                state = ConnectionStates.SHUTDOWN;
            }
            else if (thisPlayer.isServer)
            {
                networkManager.matchMaker.DestroyMatch((NetworkID)matchNetwork, 0, OnDestroyMatch);
            }
            else
            {
                networkManager.matchMaker.DropConnection((NetworkID)matchNetwork, matchNode, 0, OnDropConnection);
            }
        }
    }

    private IEnumerator ShutdownLocalNetwork()
    {
        yield return new WaitForSeconds(0.5f);
        if ((thisPlayer != null) && (thisPlayer.isServer))
        {
            networkManager.StopHost();
        }
        else
        {
            networkManager.StopClient();
        }
        ShutdownNetworkManager();
        state = ConnectionStates.SHUTDOWN;
    }

    private void ShutdownNetworkManager()
    {
        Destroy(networkManager);
        NetworkManager.Shutdown();
    }

    public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (!success)
        {
            Debug.Log("Error creating Adventure game match.");
            // TODO: FIX0001
        }
        else
        {
            matchNetwork = (ulong)matchInfo.networkId;
            networkManager.OnMatchCreate(success, extendedInfo, matchInfo);
            Debug.Log("Now hosting h2h game " + matchName);
        }
    }

    private void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (!success)
        {
            Debug.Log("Error looking for match.");
            // Try again
            waitingOnListMatch = false;
        }
        else
        {
            Debug.Log("Queried and found " + matchList.Count + " matches");
            MatchInfoSnapshot found = null;
            if (matchList.Count > 0)
            {
                // There should be only one match ever - the default one
                // but we check just in case
                foreach (MatchInfoSnapshot match in matchList)
                {
                    if (match.name.Equals(matchName))
                    {
                        found = match;
                        break;
                    }
                    else
                    {
                        Debug.Log("Ignoring match named " + match.name);
                    }
                }
            }
            if (found == null)
            {
                // No one has hosted yet.  Try again.
                waitingOnListMatch = false;
            }
            else
            {
                networkManager.matchMaker.JoinMatch(found.networkId, "", "", "", 0, 1, OnMatchJoined);
            }
        }
    }

    public virtual void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (!success)
        {
            Debug.Log("Error joining lobby");
            // TODO: FIX0001
        }
        else
        {
            matchNetwork = (ulong)matchInfo.networkId;
            matchNode = matchInfo.nodeId;
            networkManager.OnMatchJoined(success, extendedInfo, matchInfo);
            Debug.Log("Now joined h2h game");
            needMatch = false;
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
        networkManager.OnDropConnection(success, extendedInfo);
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
        networkManager.OnDestroyMatch(success, extendedInfo);
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
