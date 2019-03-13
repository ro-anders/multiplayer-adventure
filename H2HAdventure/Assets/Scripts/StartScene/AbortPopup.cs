using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbortPopup : MonoBehaviour {

    public Text errorMessage;
    public Text errorLink;
    public GameObject overlay;
    private static string errorMessageText = "";
    private static string linkText = "";

	// Use this for initialization
	void Start () {
        errorMessage.text = errorMessageText;
        errorLink.text = linkText;
    }

    public static void Show(AbortPopup abortPopup, string message, string link)
    {
        errorMessageText = message;
        linkText = link;
        abortPopup.gameObject.SetActive(true);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }

    public void OnStatusLinkPressed()
    {
        Application.OpenURL(errorLink.text);
    }


}
