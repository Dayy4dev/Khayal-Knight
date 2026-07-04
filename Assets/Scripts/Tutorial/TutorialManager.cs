using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI Dialog Persona Style")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI textNama;
    public TextMeshProUGUI textIsi;
    public GameObject nextIcon;

    [Header("Slot Potret Terpisah (Kiri & Kanan)")]
    public Image portraitLeftMC;    // Drag UI Image sebelah KIRI ke sini
    public Image portraitRightGuide; // Drag UI Image sebelah KANAN ke sini

    [Header("Referensi Sprite")]
    public Sprite mcDiam;
    public Sprite mcHeran;
    public Sprite wajahGuide;

    [Header("Setting Efek Teks & Visual")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private Color warnaAktif = Color.white;          // Terang (100% transparan)
    [SerializeField] private Color warnaTidakAktif = new Color(0.5f, 0.5f, 0.5f, 1f); // Agak gelap/abu-abu
    
    private bool isTyping = false;
    private string fullTextHolder = "";

    [Header("Grup Objek (Tinggal Drag)")]
    public GameObject itemsTutorial;
    public GameObject meleeGroup;
    public GameObject rangedGroup;

    [Header("Pergerakan Guide")]
    public NavMeshAgent guideAgent;
    public Transform titikSenjata;

    [Header("Animation Settings")]
    [SerializeField] private Animator guideAnimator;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Status (Otomatis dari Script Lain)")]
    public bool sudahAmbilPedang = false;
    public bool sudahAmbilMeriam = false;
    public bool sudahBuangSenjata = false;
    public int enemiesDefeated = 0;

    private Transform playerTransform;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (itemsTutorial) itemsTutorial.SetActive(false);
        if (meleeGroup) meleeGroup.SetActive(false);
        if (rangedGroup) rangedGroup.SetActive(false);
        if (nextIcon) nextIcon.SetActive(false);

        // Sembunyikan gambar portrait di awal game sebelum dialog mulai
        if (portraitLeftMC) portraitLeftMC.gameObject.SetActive(false);
        if (portraitRightGuide) portraitRightGuide.gameObject.SetActive(false);

        if (guideAnimator == null && guideAgent != null)
        {
            guideAnimator = guideAgent.GetComponentInChildren<Animator>();
        }

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
        // ─── FASE 1: DIALOG AWAL ───
        if (guideAnimator != null)
        {
            Quaternion arahSemula = guideAgent.transform.rotation;
            if (playerTransform != null) yield return StartCoroutine(HadapTarget(playerTransform.position));

            guideAnimator.SetTrigger("PlayInteract");

            yield return StartCoroutine(TampilkanDialog("MC", "Huh? Where am I? Why are my old toys so colossally huge?!", "diam"));
            yield return StartCoroutine(TampilkanDialog("MC", "Wait... you're my favorite toy from when I was a kid! You're alive?!", "heran"));
            yield return StartCoroutine(TampilkanDialog("Guide", "Welcome back to the Astral Playroom, partner! It's been a long time. Let's get you moving."));

            yield return StartCoroutine(HadapArah(arahSemula));
        }
        else
        {
            yield return StartCoroutine(TampilkanDialog("MC", "Huh? Where am I? Why are my old toys so colossally huge?!", "diam"));
            yield return StartCoroutine(TampilkanDialog("MC", "Wait... you're my favorite toy from when I was a kid! You're alive?!", "heran"));
            yield return StartCoroutine(TampilkanDialog("Guide", "Welcome back to the Astral Playroom, partner! It's been a long time. Let's get you moving."));
        }

        // Sembunyikan potret saat gameplay tutorial dimulai agar tidak menutupi layar
        StatusPotret(false);

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

        // --- FASE 5: DIALOG AKHIR ───
        SetInstruksiLayar("Guide", "Remember, press 1, 2, 3 to switch weapons. Unarmed makes you run faster to escape!");
        yield return new WaitForSeconds(4f);

        if (guideAnimator != null)
        {
            Quaternion arahSemulaAkhir = guideAgent.transform.rotation;
            if (playerTransform != null) yield return StartCoroutine(HadapTarget(playerTransform.position));

            guideAnimator.SetTrigger("PlayInteract");

            yield return StartCoroutine(TampilkanDialog("Guide", "Your imagination built this place, but neglect twisted it. It's time to wake up. Let's move!"));
            
            yield return StartCoroutine(HadapArah(arahSemulaAkhir));
        }
        else
        {
            yield return StartCoroutine(TampilkanDialog("Guide", "Your imagination built this place, but neglect twisted it. It's time to wake up. Let's move!"));
        }

        // Matikan potret lagi di akhir game
        StatusPotret(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene("PlayScene", "CircleWipe");
        else
            SceneManager.LoadScene("PlayScene");
    }

    private IEnumerator TampilkanDialog(string nama, string isi, string namaWajah = "")
    {
        dialoguePanel.SetActive(true);
        if (nextIcon) nextIcon.SetActive(false);
        
        textNama.text = nama;
        AturVisualPortrait(nama, namaWajah); // Atur siapa yang terang/gelap dan ganti sprite

        isTyping = true;
        fullTextHolder = isi;
        textIsi.text = "";

        foreach (char huruf in isi.ToCharArray())
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                textIsi.text = fullTextHolder;
                break;
            }
            textIsi.text += huruf;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if (nextIcon) nextIcon.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));
    }

    private void SetInstruksiLayar(string nama, string isi)
    {
        dialoguePanel.SetActive(true);
        if (nextIcon) nextIcon.SetActive(false);
        textNama.text = nama;
        textIsi.text = isi;
        AturVisualPortrait(nama, "");
    }

    // ─── LOGIKA BARU: PENGATURAN DUA PORTRAIT (KIRI & KANAN) ───
    private void AturVisualPortrait(string namaYangBicara, string namaWajahMC)
    {
        // 1. Jika MC yang bicara
        if (namaYangBicara == "MC")
        {
            if (portraitLeftMC != null)
            {
                portraitLeftMC.gameObject.SetActive(true);
                portraitLeftMC.color = warnaAktif; // MC Terang

                // Ganti ekspresi wajah MC
                if (string.IsNullOrEmpty(namaWajahMC) || namaWajahMC == "diam")
                    portraitLeftMC.sprite = mcDiam;
                else if (namaWajahMC == "heran")
                    portraitLeftMC.sprite = mcHeran;
            }

            // Guide dibikin agak gelap (kalau objeknya lagi aktif/ada)
            if (portraitRightGuide != null && portraitRightGuide.gameObject.activeSelf)
            {
                portraitRightGuide.color = warnaTidakAktif;
            }
        }
        // 2. Jika Guide yang bicara
        else if (namaYangBicara == "Guide")
        {
            if (portraitRightGuide != null)
            {
                portraitRightGuide.gameObject.SetActive(true);
                portraitRightGuide.color = warnaAktif; // Guide Terang
                if (wajahGuide != null) portraitRightGuide.sprite = wajahGuide;
            }

            // MC dibikin agak gelap
            if (portraitLeftMC != null && portraitLeftMC.gameObject.activeSelf)
            {
                portraitLeftMC.color = warnaTidakAktif;
            }
        }
    }

    // Fungsi pembantu untuk mematikan/menghidupkan kedua portrait sekaligus
    private void StatusPotret(bool status)
    {
        if (portraitLeftMC) portraitLeftMC.gameObject.SetActive(status);
        if (portraitRightGuide) portraitRightGuide.gameObject.SetActive(status);
    }

    // ─── COROUTINE ROTASI HALUS ───
    private IEnumerator HadapTarget(Vector3 posisiTarget)
    {
        if (guideAgent == null) yield break;
        Transform guideTransform = guideAgent.transform;
        Vector3 arahTarget = (posisiTarget - guideTransform.position);
        arahTarget.y = 0;

        if (arahTarget.sqrMagnitude > 0.001f)
        {
            Quaternion rotasiTarget = Quaternion.LookRotation(arahTarget);
            while (Quaternion.Angle(guideTransform.rotation, rotasiTarget) > 1f)
            {
                guideTransform.rotation = Quaternion.Slerp(guideTransform.rotation, rotasiTarget, rotationSpeed * Time.deltaTime);
                yield return null;
            }
            guideTransform.rotation = rotasiTarget;
        }
    }

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