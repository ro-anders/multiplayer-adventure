using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FauxGamePlayer : NetworkBehaviour
{

    [SyncVar(hook = "OnChangePlayerName")]
    public string playerName = "";

    // Use this for initialization
    void Start () {
        GameObject playerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(playerList.transform);
        if (isLocalPlayer)
        {
            CmdSetPlayerName(SessionInfo.ThisPlayerName);
        }
        RefreshDisplay();
    }

    [Command]
    public void CmdSetPlayerName(string name)
    {
        playerName = name;
    }

    private void OnChangePlayerName(string newPlayerName)
    {
        playerName = newPlayerName;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        Text thisText = this.GetComponent<Text>();
        thisText.text = playerName;
    }


}
