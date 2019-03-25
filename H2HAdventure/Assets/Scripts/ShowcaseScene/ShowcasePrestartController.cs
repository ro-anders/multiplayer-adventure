using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowcasePrestartController : MonoBehaviour
{

    public ShowcaseController parent;
    public ShowcaseTransport xport;

    private ToggleGroup needHelpToggleGrp;
    private ToggleGroup needGuideToggleGrp;
    private Button okButton;
    private Text startText;


    // Start is called before the first frame update
    void Start()
    {
        needHelpToggleGrp = transform.Find("PrestartPanel/Q1ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        needGuideToggleGrp = transform.Find("PrestartPanel/Q2ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        okButton = transform.Find("PrestartPanel/OkButton").gameObject.GetComponent<Button>();
        startText = transform.Find("PrestartPanel/StartText").gameObject.GetComponent<Text>();

        okButton.interactable = true;
        startText.gameObject.SetActive(true);

    }

    // ----- Button and Other UI Handlers -----------------------------------------------------

    public void OnOkpressed()
    {
        IEnumerator<Toggle> enumerator = needHelpToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        Toggle selected = enumerator.Current;
        SessionInfo.ThisPlayerInfo.needsPopupHelp = (selected.gameObject.name == "Q1YesToggle");
        enumerator = needHelpToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        selected = enumerator.Current;
        SessionInfo.ThisPlayerInfo.needsMazeGuides = (selected.gameObject.name == "Q2YesToggle");
        okButton.interactable = false;
        startText.gameObject.SetActive(true);
    }

    // --- Network Action Handlers -----------------------------------------------------------------------

    public void OnStartGame()
    {
        parent.StartGame();
    }

}
