using UnityEngine;

[AddComponentMenu("Player/Player Movement")]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Kecepatan gerak player")]
    public float moveSpeed = 2f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Aim Indicator")]
    [Tooltip("Masukkan GameObject Lingkaran Merah di sini")]
    public Transform aimIndicator;

    private CharacterController controller;
    private Animator animator;

    // Perubahan logika gabungan gerakan
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 verticalVelocity = Vector3.zero;
    private bool isGrounded;

    public Vector3 GetMouseTargetPosition { get; private set; }

    private static readonly int AnimHorizontal = Animator.StringToHash("Horizontal");
    private static readonly int AnimVertical = Animator.StringToHash("Vertical");

    // ─────────────────────────────────────────────────────────────────────────
    // MODIFIKASI VARIABEL AUDIO (DUA VERSI: JALAN & LARI)
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Footstep Audio (Tanpa Animasi)")]
    [SerializeField] private AudioSource movementAudioSource;
    
    [Tooltip("Suara melangkah saat memegang senjata (Jalan biasa)")]
    [SerializeField] private AudioClip walkFootstepSound;
    [Tooltip("Jeda ketukan suara saat memegang senjata")]
    [SerializeField] private float walkFootstepInterval = 0.4f;

    [Tooltip("Suara melangkah saat tidak memegang senjata (Lari/Unarmed)")]
    [SerializeField] private AudioClip runFootstepSound;
    [Tooltip("Jeda ketukan suara saat Lari (lebih cepat/kecil angkanya)")]
    [SerializeField] private float runFootstepInterval = 0.25f;

    private float footstepTimer = 0f;
    private bool wasMovingLastFrame = false; 

    private void HandleFootstepSound()
    {
        // 1. PENGAMAN PAUSE: Saat buka inventory/pause, suara dipotong paksa.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (movementAudioSource != null && movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
            return;
        }

        // Gunakan Mathf.Abs untuk menghindari bug nilai input yang tidak bulat
        bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;

        if (isMoving)
        {
            // Cek apakah player UNARMED (tidak ada senjata) = LARI / ARMED (punya senjata) = JALAN
            bool isUnarmed = WeaponInventory.Instance != null && 
                            WeaponInventory.Instance.primaryWeapon == null && 
                            WeaponInventory.Instance.secondaryWeapon == null;
            
            bool isRunning = isUnarmed; // Jika unarmed, play run sound
            float currentInterval = isRunning ? runFootstepInterval : walkFootstepInterval;
            AudioClip currentClip = isRunning ? runFootstepSound : walkFootstepSound;

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= currentInterval)
            {
                if (movementAudioSource != null && currentClip != null && !movementAudioSource.isPlaying)
                {
                    movementAudioSource.PlayOneShot(currentClip);
                }
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;

            // FIX: Saat tombol arah dilepas, potong paksa suara langkah yang sedang berbunyi
            // agar tidak "nyangkut" sampai klipnya habis sendiri.
            if (movementAudioSource != null && movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
        }
    }
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        if (controller == null) return;

        // Jalankan pengecekan suara terlebih dahulu agar bisa merespon state pause dengan cepat
        HandleFootstepSound();

        // PENGAMAN PAUSE GERAKAN
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (aimIndicator != null && aimIndicator.gameObject.activeSelf)
            {
                aimIndicator.gameObject.SetActive(false);
            }
            return; 
        }

        if (aimIndicator != null && !aimIndicator.gameObject.activeSelf)
        {
            aimIndicator.gameObject.SetActive(true);
        }
        if (aimIndicator != null && !aimIndicator.gameObject.activeSelf)
        {
            aimIndicator.gameObject.SetActive(true);
        }

        CalculateGravity();
        CalculateMovementInput();
        CalculateMouseWorldPosition();
        HandleRotation();

        Vector3 finalVelocity = (moveDirection * moveSpeed) + verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void CalculateMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            GetMouseTargetPosition = mouseWorldPos;

            if (aimIndicator != null)
            {
                aimIndicator.position = new Vector3(mouseWorldPos.x, transform.position.y + 0.02f, mouseWorldPos.z);
                aimIndicator.Rotate(Vector3.forward, 50f * Time.deltaTime, Space.Self);
            }
        }
    }

    public void AimTowardsMouse()
    {
        Vector3 direction = new Vector3(
            GetMouseTargetPosition.x - transform.position.x,
            0f,
            GetMouseTargetPosition.z - transform.position.z
        );

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }
    }

    private void CalculateGravity()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f; 

        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void CalculateMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(x, 0f, z).normalized;

        if (animator == null) return;

        Vector3 localMove = transform.InverseTransformDirection(moveDirection);
        float smoothSpeed = Time.deltaTime * 5f;
        animator.SetFloat(AnimHorizontal, Mathf.MoveTowards(animator.GetFloat(AnimHorizontal), localMove.x, smoothSpeed));
        animator.SetFloat(AnimVertical, Mathf.MoveTowards(animator.GetFloat(AnimVertical), localMove.z, smoothSpeed));
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            AimTowardsMouse();
        }
        else
        {
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<WallFader>(out WallFader wall))
            wall.FadeToTransparent();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<WallFader>(out WallFader wall))
            wall.FadeToOpaque();
    }
}