using UnityEngine;

public class BombTransferRelay : MonoBehaviour
{
    public BombCarrier owner; // arrastrar el BombCarrier del auto padre en el Inspector

    void OnTriggerEnter(Collider other)
    {
        owner.HandleTransferTrigger(other);
    }

    void OnTriggerStay(Collider other)
    {
        owner.HandleTransferTrigger(other);
    }
}