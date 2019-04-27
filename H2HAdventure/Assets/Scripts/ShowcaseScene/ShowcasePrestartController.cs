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
    private Toggle needGuideYesToggle;
    private Toggle needGuideNoToggle;
    private Button okButton;
    private Text startText;



    // Start is called before the first frame update
    void Start()
    {
        needHelpToggleGrp = transform.Find("Q1ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        needGuideToggleGrp = transform.Find("Q2ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        needGuideYesToggle = transform.Find("Q2YesToggle").gameObject.GetComponent<Toggle>();
        needGuideNoToggle = transform.Find("Q2NoToggle").gameObject.GetComponent<Toggle>();
        okButton = transform.Find("OkButton").gameObject.GetComponent<Button>();
        startText = transform.Find("StartText").gameObject.GetComponent<Text>();

        Reset();
    }

    public void Reset()
    {
        okButton.interactable = true;
        startText.gameObject.SetActive(false);
    }

    // ----- Button and Other UI Handlers -----------------------------------------------------

    public void OnQ1Selected()
    {
        IEnumerator<Toggle> enumerator = needHelpToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        Toggle selected = enumerator.Current;
        if (selected.gameObject.name == "Q1YesToggle")
        {
            needGuideYesToggle.isOn = true;
            needGuideYesToggle.interactable = false;
            needGuideNoToggle.isOn = false;
            needGuideNoToggle.interactable = false;
        }
        else
        {
            needGuideYesToggle.interactable = true;
            needGuideNoToggle.interactable = true;
        }
    }

    public void OnOkpressed()
    {
        IEnumerator<Toggle> enumerator = needHelpToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        Toggle selected = enumerator.Current;
        SessionInfo.ThisPlayerInfo.needsPopupHelp = (selected.gameObject.name == "Q1YesToggle");
        enumerator = needGuideToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        selected = enumerator.Current;
        SessionInfo.ThisPlayerInfo.needsMazeGuides = (selected.gameObject.name == "Q2YesToggle");
        okButton.interactable = false;
        startText.gameObject.SetActive(true);
        xport.ReqReadyToStart();
    }

    // --- Network Action Handlers -----------------------------------------------------------------------

    public void OnStartGame(ProposedGame game, int thisClientSlot)
    {
        parent.StartGame(game, thisClientSlot);
    }

}
