using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewScheduleController : MonoBehaviour {

    public ScheduleController scheduleController;
    public InputField dateInput;
    public InputField timeInput;
    public InputField durationInput;
    public InputField commentsInput;
    public Text errorText;

	// Use this for initialization
	void Start () {
        errorText.text = "";
	}
	
    public void OnOkPressed()
    {
        // Convert date and time to a DateTime
        DateTime start = ParseDate();
        if (start.Equals(DateTime.MinValue))
        {
            return;
        }
        int duration = ParseDuration();
        if (duration < 0)
        {
            return;
        }
        string errorMessage = scheduleController.ScheduleNewGame(SessionInfo.ThisPlayerName, start, duration, commentsInput.text);
        if ((errorMessage != null) && !errorMessage.Equals(""))
        {
            errorText.text = errorMessage;
            return;
        }
        errorText.text = "";
        scheduleController.DismissNewSchedulePanel();
    }

    private DateTime ParseDate()
    {
        DateTime result;
        // Verify the date string is a valid date string
        string dateStr = dateInput.text;
        if (dateStr.Split('/').Length <= 2)
        {
            // No year was specified.  Add the current year.
            dateStr += "/" + DateTime.Now.Year;
        }
        try
        {
            DateTime.Parse(dateStr + " 3:30PM");
        }
        catch (FormatException)
        {
            errorText.text = "Cannot parse Date \"" + dateStr + "\".  Please specify Date in the form 5/12 or 5/12/19";
            return DateTime.MinValue;
        }

        // Verify the time string is a valid time string
        // For some reason can parse "9:30PM" and "9 PM" but not "9PM"
        // So throw a space between the time and the "PM"
        string timeStr = timeInput.text.Trim();
        string timeEnding = timeStr.Substring(timeStr.Length - 2, 2);
        if (timeEnding.ToUpper().Equals("AM") || timeEnding.ToUpper().Equals("PM")) {
            timeStr = timeStr.Substring(0, timeStr.Length - 2) + " " + timeEnding;
        }
        try
        {
            DateTime.Parse("12/31/2019 " + timeStr);
        }
        catch (FormatException)
        {
            errorText.text = "Cannot parse Time \"" + timeInput.text + "\".  Please specify time in the form 5:30 PM";
            return DateTime.MinValue;
        }

        // Now concatenate the two and try to make a date & time
        try
        {
            result = DateTime.Parse(dateStr + " " + timeStr);
        }
        catch (FormatException)
        {
            errorText.text = "Not recognizable Date and Time fields.";
            return DateTime.MinValue;
        }
        if (DateTime.Compare(result, DateTime.Now ) < 0)
        {
            errorText.text = "Scheduled time has already past.  Please enter a Date and Time in the future.";
            return DateTime.MinValue;
        }
        return result;
    }

    private int ParseDuration()
    {
        int MIN = 10;
        int MAX = 480;
        int result;
        try
        {
            result = int.Parse(durationInput.text);
        }
        catch (FormatException)
        {
            errorText.text = "Cannot parse Duration field.  Please enter number of minutes you intend to play.";
            return -1;
        }
        if (result < MIN)
        {
            errorText.text = "Not enough time.  Please enter a Duration between " +
                MIN + " and " + MAX + " minutes.";
            return -1;
        }
        if (result > MAX)
        {
            errorText.text = "Too much time.  Please enter a Duration between " +
                MIN + " and " + MAX + " minutes.";
            return -1;
        }
        return result;
    }

    public void OnCancelPressed()
    {
        errorText.text = "";
        scheduleController.DismissNewSchedulePanel();
    }
}
