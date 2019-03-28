using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowcaseController : MonoBehaviour
{
    private ShowcaseNetworkController networkController;
    private ShowcaseTitleController titleController;
    private ShowcaseLobbyController lobbyController;
    private ShowcasePrestartController prestartController;
    private ShowcaseGameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        networkController = transform.Find("NetworkPanel").gameObject.GetComponent<ShowcaseNetworkController>();
        titleController = transform.Find("ShowcasePanel").gameObject.GetComponent<ShowcaseTitleController>();
        lobbyController = transform.Find("LobbyPanel").gameObject.GetComponent<ShowcaseLobbyController>();
        prestartController = transform.Find("PrestartPanel").gameObject.GetComponent<ShowcasePrestartController>();
        gameController = transform.Find("PlayGamePanel").gameObject.GetComponent<ShowcaseGameController>();

        networkController.gameObject.SetActive(true);
        titleController.gameObject.SetActive(false);
        lobbyController.gameObject.SetActive(false);
        prestartController.gameObject.SetActive(false);
        gameController.gameObject.SetActive(false);
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

    public void GameHasBeenAgreed()
    {
        titleController.gameObject.SetActive(false);
        lobbyController.gameObject.SetActive(false);
        prestartController.gameObject.SetActive(true);
    }

    public void StartGame(ProposedGame game, int thisClientSlot)
    {
        prestartController.gameObject.SetActive(false);
        gameController.gameObject.SetActive(true);
        gameController.Play(game, thisClientSlot);
    }

    public void GameHasBeenQuit()
    {
        gameController.gameObject.SetActive(false);
        lobbyController.gameObject.SetActive(true);
    }

}
