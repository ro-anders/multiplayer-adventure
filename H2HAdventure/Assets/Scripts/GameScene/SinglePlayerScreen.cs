using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerScreen : MonoBehaviour
{
    public GameObject gamePanel;
    public SinglePlayerAdvView advView;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartClicked()
    {
        gamePanel.SetActive(true);
        advView.PlayGame();
    }

}
