using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScheduledGame : MonoBehaviour {

    public Text text;

    private string key;
    public string Key
    {
        get { return key; }
        set { key = value;}
    }
    private long timestamp;
    public long Timestamp
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
    private string comments;
    public string Comments
    {
        get { return comments; }
        set { comments = value; Refresh(); }
    }
    private int duration;
    public int Duration
    {
        get { return duration; }
        set { duration = value; Refresh(); }
    }
    private List<string> others = new List<string>();
    public string[] Others
    {
        get { return others.ToArray(); }
    }
    public void AddOther(string other)
    {
        if (!others.Contains(other))
        {
            others.Add(other);
        }
        Refresh();
    }
    public void AddOthers(string[] more)
    {
        if (more != null)
        {
            foreach(string other in more)
            {
                if (!others.Contains(other))
                {
                    others.Add(other);
                }
            }
        }
        Refresh();
    }
    public void RemoveOther(string other)
    {
        others.Remove(other);
        Refresh();
    }

    private ScheduleController controller;
    public ScheduleController Controller
    {
        set { controller = value; }
    }

    private void Refresh()
    {
        DateTime start = new DateTime(timestamp);
        DateTime end = start.AddMinutes(duration);
        string summary = start.ToString("ddd MMM d h:mmtt") + "-" +
            end.ToString("h:mm") + " - ";
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

    public void OnGameClicked()
    {
        controller.DisplayScheduledGame(this);
    }
}
