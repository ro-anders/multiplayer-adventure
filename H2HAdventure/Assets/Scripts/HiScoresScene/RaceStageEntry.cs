using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceStageEntry : MonoBehaviour
{
    public Text titleText;

    public string Title
    {
        set { titleText.text = value; }
    }
}
