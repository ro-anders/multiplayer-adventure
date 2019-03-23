using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowcaseController : MonoBehaviour
{
    private ShowcaseNetworkController networkController;
    private ShowcaseTitleController titleController;
    private ShowcaseLobbyController lobbyController;

    // Start is called before the first frame update
    void Start()
    {
        networkController = transform.Find("NetworkPanel").gameObject.GetComponent<ShowcaseNetworkController>();
        titleController = transform.Find("ShowcasePanel").gameObject.GetComponent<ShowcaseTitleController>();
        lobbyController = transform.Find("LobbyPanel").gameObject.GetComponent<ShowcaseLobbyController>();

        networkController.gameObject.SetActive(true);
        titleController.gameObject.SetActive(false);
        lobbyController.gameObject.SetActive(false);
    }

    public void NetworkHasBeenSetup()
    {
        networkController.gameObject.SetActive(false);
        titleController.gameObject.SetActive(true);
        lobbyController.gameObject.SetActive(true);
    }

    public void TitleHasBeenDismissed()
    {
        titleController.gameObject.SetActive(false);
        lobbyController.gameObject.SetActive(true);
    }

}
