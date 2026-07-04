using UnityEngine;
using System.Collections;
using TMPro;

public class CandyBox : MonoBehaviour
{
    private string myEffect;
    private PartnerSystem partnerSystem;
    private bool isSelected = false;
    private bool isPlayerNearby = false; 

    [SerializeField] private GameObject interactionCanvasUI;

    [Header("UI / Visual Components")]
    [SerializeField] private TextMeshPro textMesh;

  [Header("Candy Models Setup")]
    [SerializeField] private GameObject healCandyPrefab;
    [SerializeField] private GameObject maxHpCandyPrefab;
    [SerializeField] private GameObject buffCandyPrefab;
    [SerializeField] private GameObject damageCandyPrefab;

    // --- TAMBAHKAN AUDIO SETTING DI SINI ---
    [Header("Audio Settings")]
    [SerializeField] private AudioClip openCandyboxSound;

    private GameObject activeCandyModel;
    public void InitializeBox(string effect, PartnerSystem system)
    {
        myEffect = effect;
        partnerSystem = system;
        isSelected = false;
        isPlayerNearby = false;

        if (textMesh != null)
        {
            textMesh.text = "???";
        }

        SpawnCorrespondingCandy();
    }

    private void SpawnCorrespondingCandy()
    {
        GameObject prefabToSpawn = null;
        switch (myEffect)
        {
            case "Heal": prefabToSpawn = healCandyPrefab; break;
            case "MaxHP": prefabToSpawn = maxHpCandyPrefab; break;
            case "BuffDamage": prefabToSpawn = buffCandyPrefab; break;
            case "InstantDamage": prefabToSpawn = damageCandyPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            activeCandyModel = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            activeCandyModel.transform.parent = transform;
            activeCandyModel.SetActive(false);
        }
    }

  void Update()
    {
        if (isPlayerNearby && !isSelected && partnerSystem != null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isSelected = true;
                if (interactionCanvasUI != null) interactionCanvasUI.SetActive(false); 

                // --- TAMBAHAN KODE AUDIO UTK CANDYBOX ---
                if (openCandyboxSound != null)
                {
                    AudioSource.PlayClipAtPoint(openCandyboxSound, transform.position);
                }
                // -----------------------------------------

                partnerSystem.ProcessBoxSelection(myEffect, this);
            }
        }
    }

    public void RevealAndCleanUp(bool chosenByPlayer)
    {
        if (textMesh != null)
        {
            switch (myEffect)
            {
                case "Heal": textMesh.text = "Full Heal"; textMesh.color = Color.green; break;
                case "MaxHP": textMesh.text = "HP +100 & Heal 30%"; textMesh.color = Color.cyan; break;
                case "BuffDamage": textMesh.text = "Buff ATK +50"; textMesh.color = Color.red; break;
                case "InstantDamage": textMesh.text = "-50% HP"; textMesh.color = Color.black; break;
            }
        }

        if (activeCandyModel != null)
        {
            activeCandyModel.SetActive(true);
        }

        StartCoroutine(FlyUpRoutine());
    }

    private IEnumerator FlyUpRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(0, 1f, 0);
        float duration = 1f;
        float elapsed = 0f;

        if (activeCandyModel != null) activeCandyModel.transform.parent = null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            if (activeCandyModel != null)
            {
                activeCandyModel.transform.position = Vector3.Lerp(startPos, targetPos, percent);
                activeCandyModel.transform.Rotate(Vector3.up, 360f * Time.deltaTime);
            }
            yield return null;
        }

        if (activeCandyModel != null) Destroy(activeCandyModel);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isSelected)
        {
            isPlayerNearby = true;
            if (interactionCanvasUI != null) interactionCanvasUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactionCanvasUI != null) interactionCanvasUI.SetActive(false);
        }
    }
}