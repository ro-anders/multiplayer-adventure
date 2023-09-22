using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatSync : MonoBehaviour
{
    private const string NO_CHAT_YET = "No messages";

    //[SyncVar(hook = "OnChangeChatText")]
    public string chatText;

    private Text chatTextUI;
    private List<string> chatPosts = new List<string>();
    private AudioSource newChatAudioSource;

    // Use this for initialization
    void Start () {
        chatText = NO_CHAT_YET;
        GameObject chatTextGameObject = GameObject.Find("Chat Text").gameObject;
        chatTextUI = chatTextGameObject.GetComponent<Text>();
        gameObject.transform.SetParent(chatTextGameObject.transform.parent, false);
        GameObject chatAudioGameObject = GameObject.Find("NewChatAudioSource").gameObject;
        newChatAudioSource = chatAudioGameObject.GetComponent<AudioSource>();

        GameObject chatGameObject = GameObject.FindGameObjectWithTag("ChatController");
        ChatPanelController chatPanelController = chatGameObject.GetComponent<ChatPanelController>();
        chatPanelController.ChatSync = this;

        RefreshGraphic();
    }

    // Only called on server
    public void BroadcastMessage(string playerName, string message) {
        // TODO: Escape markdown tags
        chatPosts.Add("<b>" + playerName + ":</b> " + message);
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
        if ((newChatText != chatText) && (newChatText != NO_CHAT_YET))
        {
            chatText = newChatText;
            if (newChatAudioSource != null)
            {
                newChatAudioSource.Play();
            }
            RefreshGraphic();
        }
    }

    private void RefreshGraphic() {
        if (chatTextUI != null) {
            chatTextUI.text = chatText;
        }
    }
}
