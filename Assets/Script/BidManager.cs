using UnityEngine.UI;
using Mirror;
using UnityEngine;

public class BidManager : NetworkBehaviour
{
    int actualBid;
    int wantedBid;
    public Transform BidUi;

    public Sprite green;
    public Sprite grey;
    public Sprite red;

    public GameObject bidPrefab;

    [ClientRpc]
    public void SetUi(int bid_)
    {
        for (int i = BidUi.childCount - 1; i >= 0; i--)
        {
            Destroy(BidUi.GetChild(i).gameObject);
        }

        wantedBid = bid_;
        actualBid = 0;

        for (int i = 0; i < wantedBid; i++)
        {
            GameObject bid = Instantiate(bidPrefab,BidUi);
            bid.GetComponent<Image>().sprite = grey;
        }
    }

    [ClientRpc]
    public void NewTrick()
    {
        actualBid++;
        if(actualBid <= wantedBid)
        {
            BidUi.GetChild(actualBid - 1).GetComponent<Image>().sprite = green;
        }
        else
        {
            GameObject bid = Instantiate(bidPrefab,BidUi);
            bid.GetComponent<Image>().sprite = red;
        }
    }

}
