using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ButtonPressed() {
        UnityEngine.Debug.Log("Logging message to debug log");
        System.Console.WriteLine("Logging message to console");
    }
}
