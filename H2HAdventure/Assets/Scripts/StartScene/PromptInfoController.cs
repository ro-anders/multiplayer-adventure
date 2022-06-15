using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromptInfoController : MonoBehaviour {

    private static readonly Color ACTIVE_TEXT_COLOR = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color INACTIVE_TEXT_COLOR = new Color(0.4f, 0.4f, 0.4f);

    public GameObject thisPanel;
    public InputField nameInput;
    public StartScreen parent;
    private Toggle q1YesToggle;
    private Toggle q1NoToggle;
    private Text q2Text;
    private Toggle q2YesToggle;
    private Toggle q2NoToggle;

    void Start()
    {
        q1YesToggle = transform.Find("Q1ToggleGroup/Q1YesToggle").gameObject.GetComponent<Toggle>();
        q1NoToggle = transform.Find("Q1ToggleGroup/Q1NoToggle").gameObject.GetComponent<Toggle>();
        q2YesToggle = transform.Find("Q2ToggleGroup/Q2YesToggle").gameObject.GetComponent<Toggle>();
        q2NoToggle = transform.Find("Q2ToggleGroup/Q2NoToggle").gameObject.GetComponent<Toggle>();
        q2Text = transform.Find("Q2Text").gameObject.GetComponent<Text>() ;
        string prevName = PlayerPrefs.GetString(SessionInfo.PLAYER_NAME_PREF, "");
        if (!prevName.Equals(""))
        {
            nameInput.text = prevName;
        }
        
    }

    public void OnQ1Changed()
    {
        if (q1YesToggle.isOn)
        {
            q2YesToggle.isOn = true;
            q2NoToggle.isOn = false;
            q2Text.color = INACTIVE_TEXT_COLOR;
            q2YesToggle.interactable = false;
            q2NoToggle.interactable = false;
        }
        else
        {
            q2Text.color = ACTIVE_TEXT_COLOR;
            q2YesToggle.interactable = true;
            q2NoToggle.interactable = true;
        }
    }

    public void OnOkPressed() {
        if (nameInput.text.Trim() != "")
        {
            PlayerPrefs.SetString(SessionInfo.PLAYER_NAME_PREF, nameInput.text.Trim());
            parent.GotPromptInfo(nameInput.text.Trim(), q1YesToggle.isOn, q2YesToggle.isOn);
        }
    }
}
