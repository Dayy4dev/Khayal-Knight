using UnityEngine;
using System.Collections.Generic;
using System;

public class PartnerSystem : MonoBehaviour
{
    public event Action OnCandySelected;

    [Header("Child Box Components")]
    [SerializeField] private CandyBox[] candyBoxes;

    [Header("Animation Settings")]
    [SerializeField] private Animator guideAnimator; 

    private List<string> candyPool = new List<string> { "Heal", "MaxHP", "BuffDamage", "InstantDamage" };
    private List<string> selectedCandies = new List<string>();

    void Start()
    {
        SetupRandomCandies();

        if (guideAnimator == null)
        {
            guideAnimator = GetComponentInChildren<Animator>();
        }

        // --- UBAH DI SINI ----------------------------------------------------
        // Saat game mulai, Guide akan otomatis idle (karena Idle adalah Entry state).
        // Kita tidak perlu mengatur bool apa-apa di sini.
        // ---------------------------------------------------------------------
    }

    void SetupRandomCandies()
    {
        List<string> tempPool = new List<string>(candyPool);
        int indexToRemove = UnityEngine.Random.Range(0, tempPool.Count);
        tempPool.RemoveAt(indexToRemove);
        selectedCandies = tempPool;

        for (int i = 0; i < candyBoxes.Length; i++)
        {
            if (candyBoxes[i] != null)
            {
                candyBoxes[i].InitializeBox(selectedCandies[i], this);
            }
        }
    }

    public void ProcessBoxSelection(string chosenEffect, CandyBox chosenBox)
    {
        if (LootboxManager.Instance != null)
        {
            LootboxManager.Instance.SetCandyEffectStatus(chosenEffect);
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        PlayerHealth healthComponent = player.GetComponent<PlayerHealth>();
        PlayerBuffManager buffManager = player.GetComponent<PlayerBuffManager>();

        switch (chosenEffect)
        {
            case "Heal":
                if (healthComponent != null) healthComponent.Heal(healthComponent.GetMaxHealth());
                break;

            case "MaxHP":
                if (buffManager != null)
                {
                    buffManager.ApplyHpAndHealBuff();
                }
                break;

            case "BuffDamage":
                if (buffManager != null)
                {
                    buffManager.ApplyDamageBuff();
                }
                break;

            case "InstantDamage":
                if (healthComponent != null)
                {
                    int currentHp = healthComponent.GetCurrentHealth();
                    int maxHp = healthComponent.GetMaxHealth();
                    if (currentHp < (maxHp * 0.10f))
                    {
                        Debug.Log("[Partner Effect] Proteksi Aktif.");
                    }
                    else
                    {
                        healthComponent.TakeDamage(Mathf.RoundToInt(currentHp * 0.50f));
                    }
                }
                break;
        }

        if (LootboxManager.Instance != null)
        {
            LootboxManager.Instance.SetCandyEffectStatus(chosenEffect);
        }

        // --- UBAH DI SINI (LOGIKA TRIGGER) -----------------------------------
        // Saat permen dipilih, kita tembak Trigger 'PlayInteract'.
        // Guide akan pindah ke anim Interaction, putar sampai habis, lalu balik Idle.
        if (guideAnimator != null)
        {
            guideAnimator.SetTrigger("PlayInteract");
            Debug.Log("[PartnerSystem] Candy dipilih, memutar animasi Interaction sekali.");
        }
        // ---------------------------------------------------------------------

        foreach (CandyBox box in candyBoxes)
        {
            if (box != null)
            {
                bool isThisTheChosenOne = (box == chosenBox);
                box.RevealAndCleanUp(isThisTheChosenOne);
            }
        }

        OnCandySelected?.Invoke();
    }
}