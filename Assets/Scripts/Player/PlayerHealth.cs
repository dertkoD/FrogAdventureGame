using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
        // Optional: UI / VFX hook here
        Debug.Log($"Healed {amount}. HP: {currentHp}/{maxHp}");
    }  
}
