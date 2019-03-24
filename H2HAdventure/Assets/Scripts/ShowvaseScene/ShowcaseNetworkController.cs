using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ShowcaseNetworkController : MonoBehaviour
{
    public ShowcaseController parent;
    public NetworkManager networkManager;
    private Toggle hostToggle;
    private InputField hostPortInput;
    private Toggle clientToggle;
    private InputField clientIpInput;
    private InputField clientPortInput;
    private Text errorText;

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
        errorText = transform.Find("ErrorText").gameObject.GetComponent<Text>();

        hostPortInput.text = "1981";
        clientIpInput.text = "127.0.0.1";
        clientPortInput.text = "1981";
        errorText.text = "";
    }

    public void OnOkPressed()
    {
        if (hostToggle.isOn)
        {
            networkManager.networkPort = int.Parse(hostPortInput.text);
            networkManager.serverBindAddress = "127.0.0.1";
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

    public void PlayerStarted()
    {
        if (waitingForSuccess)
        {
            parent.NetworkHasBeenSetup();
            waitingForSuccess = false;
        }
    }

}
