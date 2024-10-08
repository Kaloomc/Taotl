using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PseudoCanvas : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    int ID;
    public void confirm()
    {
        PhotonView[] player = GameObject.FindObjectsOfType<PhotonView>();
        foreach (PhotonView p in player)
        {
            if(p.IsMine)
            {
                ID = p.ViewID; break;
            }
        }
        GameObject.FindObjectOfType<LobbyManager>().AddPseudo(inputField.text,ID);
        Destroy(gameObject);
    }
}
