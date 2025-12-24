using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Serializable]
    public class CurrencyEntry
    {
        public string currencyName = "Gold";
        public int amount;
    }

    // Inspector-friendly starting currencies (optional)
    [SerializeField] private List<CurrencyEntry> startingCurrencies = new();

    // Runtime storage
    private readonly Dictionary<string, int> _currencies = new(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        _currencies.Clear();
        foreach (var e in startingCurrencies)
        {
            if (string.IsNullOrWhiteSpace(e.currencyName)) continue;
            _currencies[e.currencyName] = Mathf.Max(0, e.amount);
        }
    }

    public int Get(string currencyName)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return 0;
        return _currencies.TryGetValue(currencyName, out var v) ? v : 0;
    }

    public void Add(string currencyName, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return;
        if (amount <= 0) return;

        _currencies.TryGetValue(currencyName, out var cur);
        _currencies[currencyName] = cur + amount;

        // Optional: UI / VFX hook here
        Debug.Log($"Added {amount} {currencyName}. Total: {_currencies[currencyName]}");
    }
}