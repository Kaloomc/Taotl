using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UImessage : MonoBehaviour
{

    TextMeshProUGUI text;
    [SerializeField] float opacityTarget;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        


        text.faceColor = new Color(1,1,1,1);
    }


    public void Message(string message)
    {

        text.text = message;
        
    }

    
    //testgithub
}
