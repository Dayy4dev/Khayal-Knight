using UnityEngine;
using System.Collections;

public class ToyHammerVFX_V2 : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject boomVfxPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0.8f);
    [SerializeField] private Vector3 localScale = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 localRotation = Vector3.zero;

    [Header("Timing")]
    [SerializeField] private float playDelay = 0f;

    [Header("Lifetime")]
    [SerializeField] private float destroyAfter = 0.7f;

    private MCAnimationController animController;

    private void Start()
    {
        animController = FindFirstObjectByType<MCAnimationController>();

        if (animController != null)
        {
            animController.OnHammerAttack2Fired += PlayVFX;
        }
        else
        {
            Debug.LogWarning("[ToyHammerVFX] MCAnimationController not found.");
        }
    }

    private void OnDestroy()
    {
        if (animController != null)
            animController.OnHammerAttack2Fired -= PlayVFX;
    }

    public void PlayVFX()
    {
        StopAllCoroutines();

        if (playDelay > 0)
            StartCoroutine(PlayAfterDelay());
        else
            SpawnVFX();
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(playDelay);
        SpawnVFX();
    }

    private void SpawnVFX()
    {
        if (boomVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(
            boomVfxPrefab,
            transform.TransformPoint(offset),
            Quaternion.identity);

        // Menghadap kamera
        if (Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.position - vfx.transform.position;
            vfx.transform.rotation =
                Quaternion.LookRotation(-dir) *
                Quaternion.Euler(localRotation);
        }
        else
        {
            vfx.transform.rotation =
                transform.rotation *
                Quaternion.Euler(localRotation);
        }

        vfx.transform.localScale = localScale;

        // Restart animasi dari frame pertama
        Animator animator = vfx.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("vfx_boom", 0, 0f);
        }

        Destroy(vfx, destroyAfter);
    }
}