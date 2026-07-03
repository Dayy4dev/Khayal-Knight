using UnityEngine;
using System.Collections;

public class BalloonSwordVFX : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject slashVfxPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 1f);
    [SerializeField] private Vector3 localScale = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 localRotation = Vector3.zero;

    [Header("Timing")]
    [SerializeField] private float playDelay = 0f;

    [Header("Lifetime")]
    [SerializeField] private float destroyAfter = 0.5f;

    private void Start()
    {
        
    }

    public void PlayVFX()
    {
        StopAllCoroutines();

        if (playDelay > 0f)
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
        if (slashVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(
            slashVfxPrefab,
            transform.TransformPoint(offset),
            Quaternion.identity);

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

        Animator animator = vfx.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("VFX_SlashSingle", 0, 0f);
        }

        Destroy(vfx, destroyAfter);
    }


    public void StopVFX()
    {
        
    }
}