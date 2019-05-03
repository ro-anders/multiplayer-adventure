using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HiscoreEntry : MonoBehaviour
{
    public Text nameText;
    public Text winsText;
    public Text lossesText;

    public string Name
    {
        set { nameText.text = value; }
    }
    public int Wins
    {
        set { winsText.text = value.ToString(); }
    }
    public int Losses
    {
        set { lossesText.text = value.ToString(); }
    }
}
