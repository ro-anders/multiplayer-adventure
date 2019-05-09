using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceEntry : MonoBehaviour
{
    public Text nameText;
    public Text dateText;

    public string Name
    {
        set { nameText.text = value; }
    }
    public int Time
    {
        set
        {
            DateTime date = new DateTime(value);
            dateText.text = date.ToString("MMM dd, yyyy");
        }
    }
}
