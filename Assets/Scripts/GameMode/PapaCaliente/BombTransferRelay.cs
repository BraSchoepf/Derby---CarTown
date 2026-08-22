using UnityEngine;

public class BombTransferRelay : MonoBehaviour
{
    public BombCarrier owner; // arrastrar el BombCarrier del auto padre en el Inspector

    void OnTriggerEnter(Collider other)
    {
        if (BombCarrierManager.Instance == null) return;
        owner.HandleTransferTrigger(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (BombCarrierManager.Instance == null) return;
        owner.HandleTransferTrigger(other);
    }
}