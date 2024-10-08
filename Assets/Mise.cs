using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Mise : MonoBehaviour
{
    Manager manager;
    int value;
    [SerializeField] TextMeshProUGUI textMeshPro;
    private void Start()
    {
        manager = GameObject.FindObjectOfType<Manager>();
       
    }

    public void Value(int value_)
    {
        value += value_;
        value = Mathf.Clamp(value, 0, manager.Plis);
        textMeshPro.text = value.ToString();
    }

}
