using UnityEngine;


[RequireComponent(typeof(Collider))]
public class GoldenFly : MonoBehaviour
{ 
    [SerializeField] private string currencyName = "Gold";
    [SerializeField] private int amount = 1;
    [SerializeField] private string playerTag = "Player";

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    { 
        if (!other.CompareTag(playerTag)) return;

        var wallet = other.GetComponentInParent<PlayerWallet>();
        if (wallet == null) return;

        wallet.Add(currencyName, amount);
        Destroy(gameObject);
        }
    }