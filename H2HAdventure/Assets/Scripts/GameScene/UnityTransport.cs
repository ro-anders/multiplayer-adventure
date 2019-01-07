using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class UnityTransport : MonoBehaviour, Transport
{
    public NetworkManager networkManager;
    private string matchName; // The unique name of the game in Unity's matchmaker
    private bool needMatch = false; // Whether we are using matchmaker and need to setup a match
    private bool waitingOnListMatch = false; // Whether we are waiting for Unity to setup a match

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
            }
            else
            {
                networkManager.networkPort = int.Parse(SessionInfo.GameToPlay.connectionkey);
                networkManager.StartClient();
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

    public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        networkManager.OnMatchCreate(success, extendedInfo, matchInfo);
        Debug.Log("Now hosting h2h game " + matchName);
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
        networkManager.OnMatchJoined(success, extendedInfo, matchInfo);
        Debug.Log("Now joined h2h game");
        needMatch = false;
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
