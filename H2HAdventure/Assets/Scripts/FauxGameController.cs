using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class FauxGameController : MonoBehaviour {

    public GameNetworkManager networkManager;
    public Text gameStartText;

    bool needMatch = false;
    string matchName;
    bool waitingOnListMatch = false;
    int numPlayersConnected = 0;

	// Use this for initialization
	void Start() {
        Debug.Log("Playing game " + SessionInfo.GameToPlay + " using network " + SessionInfo.NetworkSetup);
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
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.DIRECT_CONNECT)
        {
            if (SessionInfo.DirectConnectIp == SessionInfo.DIRECT_CONNECT_HOST_FLAG)
            {
                networkManager.networkPort = 1981;
                networkManager.serverBindAddress = "127.0.0.1";
                networkManager.serverBindToIP = true;
                networkManager.StartHost();
            }
            else
            {
                networkManager.networkPort = 1981;
                networkManager.networkAddress = SessionInfo.DirectConnectIp;
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
            gameStartText.text = "Running in single player mode";
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
        gameStartText.text = "Waiting for players to join " + matchName + " match";
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
                    } else {
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

    public void  RegisterNewPlayer(FauxGamePlayer player)
    {
        numPlayersConnected++;
        Debug.Log("Player connected.  Have " + numPlayersConnected + " of " + SessionInfo.GameToPlay.numPlayers);
        if (numPlayersConnected == SessionInfo.GameToPlay.numPlayers)
        {
            gameStartText.text = "All players joined.  Game started.";
        }
        else
        {
            gameStartText.text = "Waiting for " + 
                (SessionInfo.GameToPlay.numPlayers-numPlayersConnected) +
                " more players.";
        }
    }

}
