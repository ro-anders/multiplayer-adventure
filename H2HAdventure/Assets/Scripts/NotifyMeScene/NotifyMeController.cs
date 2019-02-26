using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class SubscriptionEntry
{
    public string PK;
    public string SK;
    public string Contact;
    public string Type;
    public bool OnNewSchedule;
    public bool OnCallOut;
    public bool Blacklist;
    public bool Unsubscribe; // Not ever stored in the database but used by lambda
    public SubscriptionEntry(string inContact, string inType)
    {
        PK = "Subscription";
        SK = inContact;
        Contact = inContact;
        Type = inType;
        OnNewSchedule = false;
        OnCallOut = false;
        Blacklist = false;
        Unsubscribe = false;
    }
}

public class NotifyMeController : MonoBehaviour {

    public const string EMAIL_TYPE = "EMAIL";
    public const string NEW_SUBSCRIPTION_LAMBDA = "UpsertSubscription";

    public AWS awsUtil;
    public Toggle sendCallToggle;
    public Toggle newScheduleToggle;
    public Toggle unsubscribeToggle;
    public InputField emailInput;
    public Text errorText;

	// Use this for initialization
	void Start () {
		
	}
	
    public void OnUnsubscribeToggleChecked()
    {
        if (unsubscribeToggle.isOn)
        {
            sendCallToggle.isOn = false;
            newScheduleToggle.isOn = false;
        }
    }

    public void OnOtherToggleChecked()
    {
        if (sendCallToggle.isOn || newScheduleToggle.isOn)
        {
            unsubscribeToggle.isOn = false;
        }
    }

    public void OnSubmitPressed()
    {
        if (sendCallToggle.isOn || newScheduleToggle.isOn || unsubscribeToggle.isOn)
        {
            if ((emailInput.text == null) || (emailInput.text.Trim().Length == 0))
            {
                errorText.text = "Please enter an email";
                errorText.gameObject.SetActive(true);
            }
            else
            {
                errorText.gameObject.SetActive(false);
                SubscriptionEntry newSub = new SubscriptionEntry(emailInput.text.Trim(), EMAIL_TYPE);
                if (unsubscribeToggle.isOn)
                {
                    newSub.Unsubscribe = true;
                }
                else
                {
                    newSub.OnNewSchedule = newScheduleToggle.isOn;
                    newSub.OnCallOut = sendCallToggle.isOn;
                }
                UpsertSubscription(newSub);
            }
        }
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Start");
    }
    
    private void UpsertSubscription(SubscriptionEntry newEntry)
    {
        AmazonLambdaClient lambdaClient = awsUtil.lambdaClient;
        string jsonStr = JsonUtility.ToJson(newEntry);
        Debug.Log("Sending lambda event " + jsonStr);
        lambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
        {
            FunctionName = NEW_SUBSCRIPTION_LAMBDA,
            Payload = jsonStr
        },
        (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                try
                {
                    UnityEngine.Debug.Log("error code = " + responseObject.Response.FunctionError);
                    if ((responseObject.Response.FunctionError != null) && !responseObject.Response.FunctionError.Equals(""))
                    {
                        string payloadStr = Encoding.ASCII.GetString(responseObject.Response.Payload.ToArray());
                        LambdaError errorResponse = JsonUtility.FromJson<LambdaError>(payloadStr);
                        Debug.LogError("Error calling " + NEW_SUBSCRIPTION_LAMBDA +
                        " lambda returned error message " + errorResponse.errorMessage);
                        OnUpsertReturn(false, "Unexpected error.");
                    }
                    else
                    {
                        Debug.Log("AWS reported processing lambda.");
                        OnUpsertReturn(true, "");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error calling lambda:" + e);
                    OnUpsertReturn(false, "Unexpected response.");
                }
            }
            else
            {
                Debug.LogError(responseObject.Exception.ToString());
            }
        }
        );
    }

    private void OnUpsertReturn(bool worked, string error) { 
        if (worked)
        {
            SceneManager.LoadScene("Start");
        }
        else
        {
            errorText.text = error + " Maybe try again later.";
            errorText.gameObject.SetActive(true);
        }
    }

}
