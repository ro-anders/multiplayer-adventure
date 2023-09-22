using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using Dissonance;

public class ChatPanelController : MonoBehaviour
{
    private const string VOICE_LABEL_ON = "Voice:";
    private const string VOICE_LABEL_OFF = "Voice Chat: Disabled";
    private const string VOICE_LABEL_OFF_NARROW = "Voice: Disabled";
    private const string VOICE_LABEL_DISABLED_BY_HOST = "Voice Chat: Disabled on host";
    private const string VOICE_LABEL_DISABLED_BY_HOST_NARROW = "Voice disabled on host";
    private string voiceLabelOff = VOICE_LABEL_OFF;
    public GameObject chatPrefab;
    public bool narrow;
    //private GameObject dissonanceSetup;
    //private DissonanceComms voiceController;
    //private VoiceBroadcastTrigger voiceBroadcast;
    //private VoiceReceiptTrigger voiceReceipt;
    private Text voiceLabel;
    private TalkButton talkButton;
    private Button lockButton;
    private Button silenceButton;

    private InputField chatInput;
    private ChatSubmitter submitter;
    private ChatSync localChatSync;
    private bool isHost = false;
    private bool voiceChatEnabled = false;
    private bool voiceChatSilenced = true;

    public ChatSubmitter ChatSubmitter
    {
        set { submitter = value; }
    }

    public ChatSync ChatSync
    {
        set { localChatSync = value; }
    }

    // Use this for initialization
    void Start()
    {
        voiceLabelOff = (narrow ? VOICE_LABEL_OFF_NARROW : VOICE_LABEL_OFF);
        //dissonanceSetup = transform.Find("Dissonance").gameObject;
        //voiceController = dissonanceSetup.GetComponent<DissonanceComms>();
        //voiceBroadcast = dissonanceSetup.GetComponent<VoiceBroadcastTrigger>();
        //voiceReceipt = dissonanceSetup.GetComponent<VoiceReceiptTrigger>();
        GameObject voiceLabelGameObject = transform.Find("Voice Text").gameObject;
        voiceLabel = voiceLabelGameObject.GetComponent<Text>();
        GameObject talkButtonGameObject = transform.Find("Talk Button").gameObject;
        talkButton = talkButtonGameObject.GetComponent<TalkButton>();
        GameObject lockButtonGameObject = transform.Find("Lock Button").gameObject;
        lockButton = lockButtonGameObject.GetComponent<Button>();
        GameObject silenceButtonGameObject = transform.Find("Silence Button").gameObject;
        silenceButton = silenceButtonGameObject.GetComponent<Button>();
        GameObject chatInputGameObject = transform.Find("Chat Input").gameObject;
        chatInput = chatInputGameObject.GetComponent<InputField>();
        voiceLabel.text = (narrow ? VOICE_LABEL_DISABLED_BY_HOST_NARROW : VOICE_LABEL_DISABLED_BY_HOST);
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE)
        {
            silenceButton.interactable = false;
        }
    }

    private bool enterKeySubmits;
    void Update()
    {
        if (enterKeySubmits && Input.GetKey(KeyCode.Return))
        {
            OnPostPressed();
            enterKeySubmits = false;
        }
        else
        {
            enterKeySubmits = chatInput.isFocused;
        }
    }

    public void EnableVoiceChat()
    {
        voiceChatEnabled = true;
        voiceChatSilenced = false;
        //dissonanceSetup.SetActive(true);
        voiceLabel.text = VOICE_LABEL_ON;
        talkButton.gameObject.SetActive(voiceChatEnabled);
        lockButton.gameObject.SetActive(voiceChatEnabled);
        silenceButton.GetComponentInChildren<Text>().text = "Disable";
    }

    public void SetSilenced(bool isSilenced)
    {
        if (voiceChatEnabled)
        {
            voiceChatSilenced = isSilenced;
            if (voiceChatSilenced)
            {
                talkButton.Reset();
            }
            //voiceController.IsMuted = voiceChatSilenced;
            //voiceController.IsDeafened = voiceChatSilenced;
            voiceLabel.text = (voiceChatSilenced ? voiceLabelOff : VOICE_LABEL_ON);
            talkButton.gameObject.SetActive(!voiceChatSilenced);
            lockButton.gameObject.SetActive(!voiceChatSilenced);
            silenceButton.GetComponentInChildren<Text>().text = (voiceChatSilenced ? "Enable" : "Disable");
        }
    }

    // Only called on server
    public void ServerSetup()
    {
        // The scene has just been created on the host.  So network the chat now.
        GameObject chatSyncGO = Instantiate(chatPrefab);
        isHost = true;
        OnTalkEnabledOnHost();
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

    public void OnTalkEnabledOnHost()
    {
        voiceLabel.text = voiceLabelOff;
        silenceButton.gameObject.SetActive(true);
    }

    public void OnTalkPressed()
    {
        //voiceBroadcast.Mode = CommActivationMode.VoiceActivation;
    }

    public void OnTalkReleased()
    {
        //voiceBroadcast.Mode = CommActivationMode.None;
    }

    public void OnSilencePressed()
    {
        if (!voiceChatEnabled)
        {
            EnableVoiceChat();
            if (isHost)
            {
                submitter.AnnounceVoiceEnabledByHost();
            }
        }
        else
        {
            SetSilenced(!voiceChatSilenced);
        }
    }

}
