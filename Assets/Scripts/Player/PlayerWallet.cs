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

    [SerializeField] private List<CurrencyEntry> startingCurrencies = new();

    // Case-insensitive keys
    private readonly Dictionary<string, int> _currencies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fires whenever a currency changes: (currencyName, newAmount)</summary>
    public event Action<string, int> CurrencyChanged;

    void Awake()
    {
        _currencies.Clear();

        foreach (var e in startingCurrencies)
        {
            if (string.IsNullOrWhiteSpace(e.currencyName)) continue;

            // Merge duplicates in the list
            _currencies.TryGetValue(e.currencyName, out var cur);
            _currencies[e.currencyName] = cur + Mathf.Max(0, e.amount);
        }

        // Push initial values so HUD can display immediately
        foreach (var kv in _currencies)
            CurrencyChanged?.Invoke(kv.Key, kv.Value);
    }

    public int Get(string currencyName)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return 0;
        return _currencies.TryGetValue(currencyName, out var v) ? v : 0;
    }

    public void Add(string currencyName, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return;
        if (amount == 0) return;

        _currencies.TryGetValue(currencyName, out var cur);
        int next = cur + amount;
        if (next < 0) next = 0; // prevent negative totals unless you want debt

        _currencies[currencyName] = next;
        CurrencyChanged?.Invoke(currencyName, next);

        Debug.Log($"Added {amount} {currencyName}. Total: {next}");
    }

    public bool TrySpend(string currencyName, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return false;
        if (amount <= 0) return true;

        int cur = Get(currencyName);
        if (cur < amount) return false;

        _currencies[currencyName] = cur - amount;
        CurrencyChanged?.Invoke(currencyName, _currencies[currencyName]);
        return true;
    }

    public void Set(string currencyName, int amount)
    {
        if (string.IsNullOrWhiteSpace(currencyName)) return;

        int next = Mathf.Max(0, amount);
        _currencies[currencyName] = next;
        CurrencyChanged?.Invoke(currencyName, next);
    }
}