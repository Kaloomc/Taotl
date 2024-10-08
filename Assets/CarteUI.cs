using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarteUI : MonoBehaviour
{

    
    public float Yoffset;
    bool selected;
    bool playable;

    private void Start()
    {
        
    }

    private void Update()
    {
        playable = Mathf.FloorToInt(GetComponentInParent<LocalPlayerUI>().Textures.IndexOf(GetComponentInChildren<SpriteRenderer>().sprite) / 13) == GameObject.FindObjectOfType<Manager>().Couleur
             || GameObject.FindObjectOfType<Manager>().Couleur == -1;

        Yoffset = !playable ? -0.4f : selected ? 0.4f : 0f;

        

        //iTween.MoveTo(transform.GetChild(0).gameObject, transform.position + new Vector3(0, Yoffset, 0), 0.1f);
    }

    private void OnMouseDown()
    {
        GetComponentInParent<LocalPlayerUI>().Play(GetComponentInChildren<SpriteRenderer>().sprite);
        
    }

    private void OnMouseOver()
    {
        selected = true;
        
    }

    private void OnMouseExit()
    {
        selected = false;
    }
}
