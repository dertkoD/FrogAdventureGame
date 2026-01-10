using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text goldText;

    [Header("Player")]
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerWallet playerWallet;

    [Header("Config")]
    [SerializeField] string goldCurrencyName = "Gold";

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged += OnHealthChanged;

        if (playerWallet != null)
            playerWallet.CurrencyChanged += OnCurrencyChanged;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;

        if (playerWallet != null)
            playerWallet.CurrencyChanged -= OnCurrencyChanged;
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"HP: {current}/{max}";
    }

    void OnCurrencyChanged(string currency, int amount)
    {
        if (currency != goldCurrencyName) return;

        if (goldText != null)
            goldText.text = $"Gold: {amount}";
    }
}
