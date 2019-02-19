﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScheduledGame : MonoBehaviour {

    public Text text;
    private int timestamp;
    public int Timestamp
    {
        get { return timestamp; }
        set { timestamp = value;  Refresh(); }
    }
    private string host;
    public string Host
    {
        get { return host; }
        set { host = value; Refresh(); }
    }
    private List<string> others = new List<string>();

    private void Refresh()
    {
        DateTime start = new DateTime(timestamp);
        string summary = start.ToShortDateString() + " - ";
        if ((host == null) || host.Equals(""))
        {
            if (others.Count == 0)
            {
                summary += "no one";
            }
            else
            {
                summary += others.Count + " players";
            }
        }
        else
        {
            if (others.Count == 0)
            {
                summary += host;
            }
            else
            {
                summary += host + " and " + others.Count + " others";
            }
        }
        text.text = summary;
    }

    private bool AddOther(string other)
    {
        if (others.Contains(other))
        {
            return false;
        }
        else
        {
            others.Add(other);
            return true;
        }
    }

    public void OnJoinClicked()
    {
        Debug.Log("Button clicked");
    }

    public void OnGameClicked()
    {
        Debug.Log("Whole game clicked");
    }
}
