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
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            UnityEngine.Debug.Log("Epoch is " + date.ToString("MMM dd, yyyy"));
            date = date.AddSeconds(value);
            UnityEngine.Debug.Log("Timestamp is " + date.ToString("MMM dd, yyyy"));
            date = date.ToLocalTime();
            UnityEngine.Debug.Log("Local Timestamp is " + date.ToString("MMM dd, yyyy"));
            UnityEngine.Debug.Log("Got timestamp " + value + " which is " + date.ToString("MMM dd, yyyy"));
            dateText.text = date.ToString("MMM d, yyyy");
        }
    }
}
