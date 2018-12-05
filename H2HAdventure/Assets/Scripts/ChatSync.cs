using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatSync : NetworkBehaviour
{
    [SyncVar(hook = "OnChangeChatText")]
    public string chatText;

    private Text chatTextUI;
    private List<string> chatPosts = new List<string>();

	// Use this for initialization
	void Start () {
        chatText = "No messages";
        GameObject textGameObj = GameObject.FindGameObjectWithTag("ChatText");
        gameObject.transform.SetParent(textGameObj.transform.parent, false);
        chatTextUI = textGameObj.GetComponent<Text>();
        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
        lobbyController.ChatSync = this;

        RefreshGraphic();
    }

    public void PostToChat(LobbyPlayer player, string message) {
        // TODO: Escape markdown tags
        chatPosts.Add("<b>" + player.playerName + ":</b> " + message);
        // We only keep the last 10 messages
        while (chatPosts.Count > 10) {
            chatPosts.RemoveAt(0);
        }
        string newChatText = "";
        for(int ctr=0; ctr<chatPosts.Count; ++ctr)
        {
            newChatText += chatPosts[ctr] + (ctr < chatPosts.Count - 1 ? "\n" : "");
        }
        chatText = newChatText;
    }

    private void OnChangeChatText(string newChatText) {
        chatText = newChatText;
        RefreshGraphic();
    }

    private void RefreshGraphic() {
        if (chatTextUI != null) {
            chatTextUI.text = chatText;
        }
    }
}
