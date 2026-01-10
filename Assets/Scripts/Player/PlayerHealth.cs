using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    /// <summary>Fires whenever HP changes: (current, max)</summary>
    public event Action<int, int> HealthChanged;

    void Awake()
    {
        // Clamp in case inspector values are off
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        // Push initial state so HUD can display immediately
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int before = currentHp;
        currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);

        if (currentHp != before)
            HealthChanged?.Invoke(currentHp, maxHp);

        Debug.Log($"Healed {amount}. HP: {currentHp}/{maxHp}");
    }

    // Optional but usually useful:
    public void Damage(int amount)
    {
        if (amount <= 0) return;

        int before = currentHp;
        currentHp = Mathf.Clamp(currentHp - amount, 0, maxHp);

        if (currentHp != before)
            HealthChanged?.Invoke(currentHp, maxHp);
    }

    // Optional setter if you ever change max at runtime:
    public void SetMaxHp(int newMax, bool keepPercent = true)
    {
        newMax = Mathf.Max(1, newMax);
        if (newMax == maxHp) return;

        float pct = maxHp > 0 ? (float)currentHp / maxHp : 1f;

        maxHp = newMax;
        currentHp = keepPercent ? Mathf.RoundToInt(pct * maxHp) : Mathf.Min(currentHp, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        HealthChanged?.Invoke(currentHp, maxHp);
    }
}
