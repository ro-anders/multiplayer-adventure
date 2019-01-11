using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatPanelController : MonoBehaviour {

    public GameObject chatPrefab;

    private InputField chatInput;
    private ChatSubmitter submitter;
    private ChatSync localChatSync;

    public ChatSubmitter ChatSubmitter
    {
        set { submitter = value; }
    }

    public ChatSync ChatSync
    {
        set { localChatSync = value; }
    }

    // Use this for initialization
    void Start () {
        GameObject chatInputGameObject = GameObject.Find("Chat Input").gameObject;
        chatInput = chatInputGameObject.GetComponent<InputField>();
	}

    // Only called on server
    public void ServerSetup()
    {
        // The scene has just been created on the host.  So network the chat now.
        GameObject chatSyncGO = Instantiate(chatPrefab);
        NetworkServer.Spawn(chatSyncGO);
    }

    // Only called on server
    public void BroadcastChatMessage(string playerName, string message)
    {
        if (localChatSync != null)
        {
            localChatSync.BroadcastMessage(playerName, message);
        }
    }


    public void OnPostPressed()
    {
        if (chatInput.text != "")
        {
            submitter.PostChat(chatInput.text);
            chatInput.text = "";
        }
    }
}
