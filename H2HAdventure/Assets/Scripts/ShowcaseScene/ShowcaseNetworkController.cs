using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ShowcaseNetworkController : MonoBehaviour
{
    private const string DEFAULT_PROFILE = "default";
    private const string HOST_MODE = "host";
    private const string CLIENT_MODE = "client";

    public ShowcaseController parent;
    public NetworkManager networkManager;
    private Toggle hostToggle;
    private InputField hostPortInput;
    private Toggle clientToggle;
    private InputField clientIpInput;
    private InputField clientPortInput;
    private Toggle fullscreenToggle;
    private Text errorText;
    private Button okButton;
    private string profile = DEFAULT_PROFILE;
    private string localIp = "127.0.0.1";

    // A flag to indicate we've tried to connect to the Host.  Used when deciding to report an error.
    private bool waitingForSuccess = false;

    // Start is called before the first frame update
    void Start()
    {
        hostToggle = transform.Find("HostToggle").gameObject.GetComponent<Toggle>();
        hostPortInput = transform.Find("HostPortInput").gameObject.GetComponent<InputField>();
        clientToggle = transform.Find("ClientToggle").gameObject.GetComponent<Toggle>();
        clientIpInput = transform.Find("ClientIpInput").gameObject.GetComponent<InputField>();
        clientPortInput = transform.Find("ClientPortInput").gameObject.GetComponent<InputField>();
        fullscreenToggle = transform.Find("FullscreenToggle").gameObject.GetComponent<Toggle>();
        errorText = transform.Find("ErrorText").gameObject.GetComponent<Text>();
        okButton = transform.Find("NetworkOkButton").gameObject.GetComponent<Button>();

        errorText.text = "";

        if (!SessionInfo.WORK_OFFLINE)
        {
            localIp = GetLocalIp();
        }

        bool allSpecified = LoadConfig();
        // IN Dev mode we always wait to confirm the network settings
        if (allSpecified && !SessionInfo.DEV_MODE)
        {
            Connect();
        }
    }

    public void OnOkPressed()
    {
        Connect();
    }

    public void PlayerStarted()
    {
        if (waitingForSuccess)
        {
            parent.NetworkHasBeenSetup();
            waitingForSuccess = false;
        }
    }

    private bool LoadConfig()
    {
        if (Application.isEditor)
        {
            // If we're in the editor we don't attempt to load anything and
            // always show the network panel.
            profile = "";
            hostToggle.isOn = true;
            hostPortInput.text = "1981";
            clientIpInput.text = localIp;
            clientPortInput.text = "1981";
            fullscreenToggle.isOn = false;
            return false;
        }

        // See if we can load the initial settings from preferences
        bool clearProfile = false;
        List<string> args = new List<string>(System.Environment.GetCommandLineArgs());
        for (int ctr = 1; ctr < args.Count; ++ctr)
        {
            if (args[ctr] == "-c")
            {
                clearProfile = true;
            }
            if ((args[ctr] == "-p") && (ctr <= args.Count - 2))
            {
                profile = args[ctr + 1];
            }
        }
        bool allSpecified = false;
        if (clearProfile)
        {
            PlayerPrefs.SetString(profile + "." + "setup.mode", "");
            PlayerPrefs.SetString(profile + "." + "setup.hostip", "");
            PlayerPrefs.SetInt(profile + "." + "setup.hostport", -1);
            PlayerPrefs.SetInt(profile + "." + "setup.fullscreen", -1);
        }
        else
        {
            string networkMode = PlayerPrefs.GetString(profile + "." + "setup.mode", "");
            clientToggle.isOn = (networkMode == CLIENT_MODE);
            hostToggle.isOn = !clientToggle.isOn;
            string hostIp = PlayerPrefs.GetString(profile + "." + "setup.hostip", "");
            clientIpInput.text = (hostIp == "" ? localIp : hostIp);
            int hostPort = PlayerPrefs.GetInt(profile + "." + "setup.hostport", -1);
            hostPortInput.text = (hostPort > 0 ? hostPort.ToString() : "1981");
            clientPortInput.text = (hostPort > 0 ? hostPort.ToString() : "1981");
            int fullScreen = PlayerPrefs.GetInt(profile + "." + "setup.fullscreen", -1);
            fullscreenToggle.isOn = (fullScreen == 1);
            allSpecified =
                (networkMode != "") &&
                (hostPort != -1) &&
                (fullScreen != -1) &&
                ((hostIp != "") || (networkMode == "host"));
        }

        return allSpecified;
    }

    private void Connect()
    {
        // Save the settings for next time
        if (profile != "")
        {
            PlayerPrefs.SetString(profile + "." + "setup.mode", (hostToggle.isOn ? HOST_MODE : CLIENT_MODE));
            PlayerPrefs.SetString(profile + "." + "setup.hostip", clientIpInput.text);
            PlayerPrefs.SetInt(profile + "." + "setup.hostport", 
                (hostToggle.isOn ? int.Parse(hostPortInput.text) : int.Parse(clientPortInput.text)));
            PlayerPrefs.SetInt(profile + "." + "setup.fullscreen", 
                (fullscreenToggle.isOn ? 1 : 0));
        }

        hostToggle.interactable = false;
        hostPortInput.interactable = false;
        clientToggle.interactable = false;
        clientIpInput.interactable = false;
        clientPortInput.interactable = false;
        fullscreenToggle.interactable = false;
        okButton.interactable = false;
        Screen.fullScreen = fullscreenToggle.isOn;
        if (hostToggle.isOn)
        {
            networkManager.networkPort = int.Parse(hostPortInput.text);
            networkManager.serverBindAddress = localIp;
            networkManager.serverBindToIP = true;
            networkManager.StartHost();
        }
        else
        {
            networkManager.networkPort = int.Parse(clientPortInput.text);
            networkManager.networkAddress = clientIpInput.text;
            networkManager.StartClient();
        }
        waitingForSuccess = true;
    }


    private string GetLocalIp()
    {
        string output = "";

        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
            NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

            if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif 
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        output = ip.Address.ToString();
                    }
                }
            }
        }
        return output;
    }
}
