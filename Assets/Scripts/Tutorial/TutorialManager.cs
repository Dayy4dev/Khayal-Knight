using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI Dialog (Tinggal Drag)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI textNama;
    public TextMeshProUGUI textIsi;
    public Image potretUI;
    public Sprite wajahMC;
    public Sprite wajahGuide;

    [Header("Grup Objek (Tinggal Drag)")]
    public GameObject itemsTutorial;
    public GameObject meleeGroup;
    public GameObject rangedGroup;

    [Header("Pergerakan Guide")]
    public NavMeshAgent guideAgent;
    public Transform titikSenjata;

    // ─── ANIMATOR REFERENSI ───
    [Header("Animation Settings")]
    [SerializeField] private Animator guideAnimator;
    [SerializeField] private float rotationSpeed = 5f; // Kecepatan berputar Guide

    [Header("Status (Otomatis dari Script Lain)")]
    public bool sudahAmbilPedang = false;
    public bool sudahAmbilMeriam = false;
    public bool sudahBuangSenjata = false;
    public int enemiesDefeated = 0;

    private Transform playerTransform; // Menyimpan referensi Player otomatis

    private void Awake() => Instance = this;

    private void Start()
    {
        if (itemsTutorial) itemsTutorial.SetActive(false);
        if (meleeGroup) meleeGroup.SetActive(false);
        if (rangedGroup) rangedGroup.SetActive(false);

        if (guideAnimator == null && guideAgent != null)
        {
            guideAnimator = guideAgent.GetComponentInChildren<Animator>();
        }

        // Cari Player secara otomatis menggunakan Tag
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        StartCoroutine(AlurTutorialMudah());
    }

    private void Update()
    {
        if (guideAgent != null && guideAnimator != null)
        {
            guideAnimator.SetFloat("Speed", guideAgent.velocity.magnitude);
        }
    }

    private IEnumerator AlurTutorialMudah()
    {
        // ─── INTERACTION 1 (AWAL GAME) ───
        if (guideAnimator != null)
        {
            // Simpan arah hadap awal Guide sebelum tengok Player
            Quaternion arahSemula = guideAgent.transform.rotation;

            if (playerTransform != null)
            {
                // 1. Berputar menghadap Player
                yield return StartCoroutine(HadapTarget(playerTransform.position));
            }

            // 2. Mainkan Animasi Ngobrol
            guideAnimator.SetTrigger("PlayInteract");

            // --- FASE 1: DIALOG AWAL ---
            // --- FASE 1: DIALOG AWAL ---
            yield return StartCoroutine(TampilkanDialog("MC", "Huh? Where am I? Why are my old toys so colossally huge?!"));
            yield return StartCoroutine(TampilkanDialog("MC", "Wait... you're my favorite toy from when I was a kid! You're alive?!"));
            yield return StartCoroutine(TampilkanDialog("Guide", "Welcome back to the Astral Playroom, partner! It's been a long time. Let's get you moving."));

            // 3. Berputar balik ke arah semula sebelum mulai jalan
            yield return StartCoroutine(HadapArah(arahSemula));
        }
        else
        {
            // Jika animator null, jalankan dialog biasa tanpa rotasi animasi
            yield return StartCoroutine(TampilkanDialog("MC", "Huh? Where am I? Why are my old toys so colossally huge?!"));
            yield return StartCoroutine(TampilkanDialog("MC", "Wait... you're my favorite toy from when I was a kid! You're alive?!"));
            yield return StartCoroutine(TampilkanDialog("Guide", "Welcome back to the Astral Playroom, partner! It's been a long time. Let's get you moving."));
        }

        // --- FASE 2: DETEKSI GERAK (WASD) ---
        SetInstruksiLayar("Guide", "Use WASD to walk around this old dreamscape. Follow me, the other toys have changed...");

        if (guideAgent != null && titikSenjata != null)
        {
            guideAgent.SetDestination(titikSenjata.position);
        }

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D));
        yield return new WaitForSeconds(2f);

        // --- FASE 3: DETEKSI PICKUP PEDANG & SERANG ---
        if (itemsTutorial != null) itemsTutorial.SetActive(true);
        if (meleeGroup != null) meleeGroup.SetActive(true);

        SetInstruksiLayar("Guide", "Watch out! The abandoned toys are angry. Pick up your old Balloon Sword with F!");
        yield return new WaitUntil(() => sudahAmbilPedang == true);

        SetInstruksiLayar("Guide", "Great! Now Hold Right Click To Aim, Then Left Click to fight through these forgotten memories!");

        yield return new WaitUntil(() => meleeGroup == null || meleeGroup.GetComponentsInChildren<Transform>().Length <= 1);

        // --- FASE 4: DETEKSI BUANG & GANTI MERIAM ---
        if (rangedGroup != null) rangedGroup.SetActive(true);

        SetInstruksiLayar("Guide", "More of them ahead, and they're too far! Press G to drop your sword!");
        yield return new WaitUntil(() => sudahBuangSenjata == true);

        SetInstruksiLayar("Guide", "Now, press F to grab the Air Cannon, and blast them away!");
        yield return new WaitUntil(() => sudahAmbilMeriam == true);

        yield return new WaitUntil(() => rangedGroup == null || rangedGroup.GetComponentsInChildren<Transform>().Length <= 1);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // --- FASE 5: DIALOG AKHIR ---
        SetInstruksiLayar("Guide", "Remember, press 1, 2, 3 to switch weapons. Unarmed makes you run faster to escape!");
        yield return new WaitForSeconds(4f);

        // ─── INTERACTION 2 (SESAAT SEBELUM TRANSISI) ───
        if (guideAnimator != null)
        {
            Quaternion arahSemulaAkhir = guideAgent.transform.rotation;

            if (playerTransform != null)
            {
                // 1. Hadap Player lagi
                yield return StartCoroutine(HadapTarget(playerTransform.position));
            }

            // 2. Mainkan Animasi Ngobrol Terakhir
            guideAnimator.SetTrigger("PlayInteract");

            SetInstruksiLayar("Guide", "Your imagination built this place, but neglect twisted it. It's time to wake up. Let's move!");
            yield return new WaitForSeconds(3f);

            // 3. Kembalikan hadapannya
            yield return StartCoroutine(HadapArah(arahSemulaAkhir));
        }
        else
        {
            SetInstruksiLayar("Guide", "Your imagination built this place, but neglect twisted it. It's time to wake up. Let's move!");
            yield return new WaitForSeconds(3f);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene("PlayScene", "CircleWipe");
        }
        else
        {
            SceneManager.LoadScene("PlayScene");
        }
    }

    private IEnumerator TampilkanDialog(string nama, string isi)
    {
        UpdateUI(nama, isi);
        yield return new WaitForSeconds(0.4f);
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));
    }

    private void SetInstruksiLayar(string nama, string isi)
    {
        UpdateUI(nama, isi);
    }

    private void UpdateUI(string nama, string isi)
    {
        dialoguePanel.SetActive(true);
        textNama.text = nama;
        textIsi.text = isi;

        if (potretUI != null)
        {
            if (nama == "MC" && wajahMC != null)
                potretUI.sprite = wajahMC;
            else if (nama == "Guide" && wajahGuide != null)
                potretUI.sprite = wajahGuide;
        }
    }

    // ─── LOGIKA BARU: COROUTINE ROTASI HALUS ───

    // Mengubah rotasi Guide menghadap posisi target koordinat (Player)
    private IEnumerator HadapTarget(Vector3 posisiTarget)
    {
        if (guideAgent == null) yield break;

        Transform guideTransform = guideAgent.transform;
        Vector3 arahTarget = (posisiTarget - guideTransform.position);
        arahTarget.y = 0; // Kunci sumbu Y agar Guide tidak mendongak ke atas/bawah jika tinggi Player beda

        if (arahTarget.sqrMagnitude > 0.001f)
        {
            Quaternion rotasiTarget = Quaternion.LookRotation(arahTarget);

            // Berputar secara bertahap sampai sudutnya mendekati target (< 1 derajat)
            while (Quaternion.Angle(guideTransform.rotation, rotasiTarget) > 1f)
            {
                guideTransform.rotation = Quaternion.Slerp(guideTransform.rotation, rotasiTarget, rotationSpeed * Time.deltaTime);
                yield return null;
            }
            guideTransform.rotation = rotasiTarget; // Sempurnakan sudutnya di akhir
        }
    }

    // Mengubah rotasi Guide kembali ke bentuk nilai Quaternion semula
    private IEnumerator HadapArah(Quaternion rotasiSemula)
    {
        if (guideAgent == null) yield break;

        Transform guideTransform = guideAgent.transform;

        while (Quaternion.Angle(guideTransform.rotation, rotasiSemula) > 1f)
        {
            guideTransform.rotation = Quaternion.Slerp(guideTransform.rotation, rotasiSemula, rotationSpeed * Time.deltaTime);
            yield return null;
        }
        guideTransform.rotation = rotasiSemula;
    }
}