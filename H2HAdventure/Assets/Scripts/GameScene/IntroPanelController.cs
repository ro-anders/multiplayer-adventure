using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroPanelController : MonoBehaviour {

    public Text gameDescription;
    public Text p1Description;
    public Text p2Description;
    public Text p3Description;
    public Text helpMessage;

    // Use this for initialization
    void Start () {
        Debug.Log("Showing intro panel");
        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
