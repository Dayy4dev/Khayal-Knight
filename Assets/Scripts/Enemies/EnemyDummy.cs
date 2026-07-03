using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 300;
    private int currentHealth;

    [Header("Visual Feedback")]
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.red;

    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private Color[][] originalColors;
    private Coroutine hitFlashCoroutine;
    private bool renderersCached = false;

    private string colorPropertyName = "_BaseColor";

    [Header("UI")]
    public EnemyHealthBar healthBar;

    private EnemySpawner spawner;

    // -------------------------------------------------------------------------
    // AUDIO SETUP
    // -------------------------------------------------------------------------
    [Header("Audio Setup")]
    [SerializeField] private AudioSource audioSource;
    
    // Variasi suara hurt (prioritas array, fallback ke single clip)
    [SerializeField] private AudioClip[] enemyHurtSounds = new AudioClip[3]; 
    [SerializeField] private AudioClip enemyHurtSound; // Fallback single clip
    
    [SerializeField] private AudioClip enemyDeathSound; 

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        spawner = GetComponentInParent<EnemySpawner>();
        if (spawner == null)
        {
            // Kompatibel untuk Unity versi lama dan baru
            #if UNITY_2023_1_OR_NEWER
            spawner = FindFirstObjectByType<EnemySpawner>();
            #else
            spawner = FindObjectOfType<EnemySpawner>();
            #endif
        }

        CacheRenderers();
    }

    private void CacheRenderers()
    {
        if (renderersCached) return;

        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderers.Length == 0)
        {
            Debug.LogWarning($"[EnemyDummy] No SkinnedMeshRenderer found on '{gameObject.name}' or its children!");
            return;
        }

        originalColors = new Color[skinnedMeshRenderers.Length][];
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            Material[] materials = skinnedMeshRenderers[i].materials;
            originalColors[i] = new Color[materials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j].HasProperty(colorPropertyName))
                    originalColors[i][j] = materials[j].GetColor(colorPropertyName);
            }
        }
        renderersCached = true;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // Sudah mati, abaikan

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

    if (audioSource != null && enemyHurtSounds != null)
        audioSource.PlayOneShot(enemyHurtSounds[Random.Range(0, enemyHurtSounds.Length)]);

    if (!renderersCached) CacheRenderers();

        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
        {
            // Hentikan coroutine flash sebelumnya agar tidak overlap jika di-hit cepat
            if (hitFlashCoroutine != null)
                StopCoroutine(hitFlashCoroutine);
                
            hitFlashCoroutine = StartCoroutine(HitFlash());
        }

    // --- KNOCKBACK ---
    // Cari objek player di dalam map
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {

        if (player.TryGetComponent<PlayerAttack>(out PlayerAttack pAttack))
        {
            audioSource.PlayOneShot(enemyHurtSounds[Random.Range(0, enemyHurtSounds.Length)]);
        }
        // ------------------------------------------

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator HitFlash()
    {
        ChangeMeshColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        ResetMeshColor();
        hitFlashCoroutine = null;
    }

    private void ChangeMeshColor(Color color)
    {
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            if (skinnedMeshRenderers[i] == null) continue;

            Material[] materials = skinnedMeshRenderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j].HasProperty(colorPropertyName))
                {
                    materials[j].SetColor(colorPropertyName, color);
                }
            }
        }
    }

    private void ResetMeshColor()
    {
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            if (skinnedMeshRenderers[i] == null) continue;

            Material[] materials = skinnedMeshRenderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j].HasProperty(colorPropertyName))
                {
                    materials[j].SetColor(colorPropertyName, originalColors[i][j]);
                }
            }
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} mati!");

        if (spawner != null)
            spawner.NotifyEnemyDestroyed(gameObject);

        // Update counter tutorial jika ada
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.enemiesDefeated++;
        }

        float destroyDelay = 0.1f;
        if (audioSource != null && enemyDeathSound != null)
        {
            audioSource.PlayOneShot(enemyDeathSound);
            // Kasih waktu suara mati untuk selesai dulu sebelum objek dihancurkan.
            destroyDelay = Mathf.Max(destroyDelay, enemyDeathSound.length);
        }

        Destroy(gameObject, destroyDelay);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
        }
    }
}