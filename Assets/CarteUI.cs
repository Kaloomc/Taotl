using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarteUI : MonoBehaviour
{
    [SerializeField] protected Vector3 velocity = Vector3.zero;

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

        Yoffset = !playable ? -1f : selected ? 1f : 0f;

        
        transform.GetChild(0).transform.localPosition = Vector3.SmoothDamp(transform.GetChild(0).transform.localPosition, new Vector3(0,Yoffset,0),ref velocity,.01f);
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
