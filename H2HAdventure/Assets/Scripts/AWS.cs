using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Lambda;
using UnityEngine;
using System.Security.Cryptography;
using System;
using System.IO;
using System.Text;

[Serializable]
class LambdaPayload
{
    public int statusCode;
    public string body;
}

class LambdaError
{
    public string errorMessage;
    public string errorType;
    public string stackTrace;
}

public class AWS : MonoBehaviour {

    private static byte[] RijndaelKey = { 72, 127, 153, 45, 111, 94, 69, 91, 36, 248, 149, 7, 166, 80, 210, 47, 30, 192, 20, 200, 73, 238, 78, 136, 116, 101, 223, 56, 119, 15, 129, 127 };
    private static byte[] RijndaelIV = { 216, 105, 230, 72, 146, 231, 225, 103, 160, 49, 132, 32, 100, 194, 131, 107 };
    
    private AmazonLambdaClient lambdaClient;
    public AmazonLambdaClient LambdaClient
    {
        get
        { return lambdaClient;}
    }

    private bool isReady = false;
    private Action callOnReady = null;

    // Use this for initialization
    void Start () {
        UnityInitializer.AttachToGameObject(gameObject);
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        // Initialize the Amazon Cognito credentials provider
        string idPoolId = decryptCredentials();
        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            idPoolId, // Identity pool ID
            RegionEndpoint.USEast2 // Region
            );
            lambdaClient = new AmazonLambdaClient(credentials, RegionEndpoint.USEast2);
        isReady = true;
        if (callOnReady != null)
        {
            Action action = callOnReady;
            callOnReady = null;
            action();
        }
    }

    public void CallOnReady(Action action)
    {
        if (isReady) {
            action();
        } else
        {
            callOnReady = action;
        }
    }

    private string decryptCredentials()
    {
        using (RijndaelManaged myRijndael = new RijndaelManaged())
        {
            myRijndael.Key = RijndaelKey;
            myRijndael.IV = RijndaelIV;
            // Rather than have credentials checked into source code, put it in a config file
            byte[] encrypted = InitFile.ReadEncryptedKey();
            // Decrypt the bytes to a string. 
            string roundtrip = DecryptStringFromBytes(encrypted, myRijndael.Key, myRijndael.IV);
            return roundtrip;
        }
    }

    private string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        // Check arguments.
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");

        // Declare the string used to hold
        // the decrypted text.
        string plaintext = null;

        // Create an RijndaelManaged object
        // with the specified key and IV.
        using (RijndaelManaged rijAlg = new RijndaelManaged())
        {
            rijAlg.Key = Key;
            rijAlg.IV = IV;

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            return plaintext;

        }
    }

    public void CallLambdaAsync(string lambdaName, string inputStr)
    {
        CallLambdaAsync(lambdaName, inputStr, dummyCallback);
    }

    public void CallLambdaAsync(string lambdaName, string inputStr, Action<bool, string> callback)
    {
        try
        {
            LambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
            {
                FunctionName = lambdaName,
                Payload = inputStr
            },
            (responseObject) =>
            {
                if (responseObject.Exception != null)
                {
                    Debug.LogError("Error calling " + lambdaName +
                            " lambda returned thrown exception " + responseObject.Exception.ToString());
                    callback(false, null);

                }
                else if ((responseObject.Response.FunctionError != null) && !responseObject.Response.FunctionError.Equals(""))
                {
                    string payloadStr = Encoding.ASCII.GetString(responseObject.Response.Payload.ToArray());
                    LambdaError errorResponse = JsonUtility.FromJson<LambdaError>(payloadStr);
                    Debug.LogError("Error calling " + lambdaName +
                    " lambda returned error message: " + errorResponse.errorMessage);
                    callback(false, null);
                }
                else
                {
                    string payloadStr = Encoding.ASCII.GetString(responseObject.Response.Payload.ToArray());
                    LambdaPayload lambdaResponse = JsonUtility.FromJson<LambdaPayload>(payloadStr);
                    if (lambdaResponse.statusCode != 200)
                    {
                        Debug.LogError("Error calling " + lambdaName +
                        " lambda returned status code " + lambdaResponse.statusCode + ":" +
                            lambdaResponse.body);
                        callback(false, null);
                    }
                    else
                    {
                        Debug.Log("Call to " + lambdaName + " successful.");
                        callback(true, lambdaResponse.body);
                    }
                }
            }
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Invoking " + lambdaName +
                    " lambda threw exception " + e.ToString());
            callback(false, null);
        }
    }

    private void dummyCallback(bool success, string payload)
    {
        // Do nothing.
    }
}
