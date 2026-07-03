using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class ShootVFXInstance : MonoBehaviour
{
    private IObjectPool<ShootVFXInstance> pool;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Coroutine returnCoroutine;
    
    // Variabel untuk menyimpan status apakah VFX harus menghadap kamera
    private bool shouldFaceCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void SetPool(IObjectPool<ShootVFXInstance> pool)
    {
        this.pool = pool;
    }

    private Transform rotationTarget;

    public void Play(
        Transform parent,
        Vector3 localPosition,
        Vector3 localRotation,
        Vector3 localScale,
        string sortingLayer,
        int sortingOrder,
        bool flipWithParent,
        bool followParent,
        float lifetime,
        bool faceCamera = false,
        Transform followRotationTarget = null)
    {
        StopReturnTimer();
        shouldFaceCamera = faceCamera;
        rotationTarget = followRotationTarget;

        if (followParent && parent != null)
        {
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localRotation);
        }
        else
        {
            transform.SetParent(null, true);
            if (parent != null)
            {
                transform.position = parent.TransformPoint(localPosition);
                transform.rotation = parent.rotation * Quaternion.Euler(localRotation);
            }
            else
            {
                transform.localPosition = localPosition;
                transform.localRotation = Quaternion.Euler(localRotation);
            }
        }

        Vector3 finalScale = localScale;
        if (flipWithParent && parent != null && parent.lossyScale.x < 0f)
        {
            finalScale.x *= -1f;
        }

        transform.localScale = finalScale;

        if (spriteRenderer != null)
        {
            if (!string.IsNullOrEmpty(sortingLayer))
                spriteRenderer.sortingLayerName = sortingLayer;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (animator != null)
        {
            animator.Play(0, 0, 0f);
        }

        gameObject.SetActive(true);

        if (lifetime > 0f)
        {
            returnCoroutine = StartCoroutine(ReturnAfterDelay(lifetime));
        }
    }

    private void LateUpdate()
    {
        if (shouldFaceCamera && Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (rotationTarget != null)
        {
            transform.rotation = rotationTarget.rotation;
        }
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private void StopReturnTimer()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    public void ReturnToPool()
    {
        StopReturnTimer();
        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopReturnTimer();
        // Reset status saat objek dinonaktifkan / dikembalikan ke pool
        shouldFaceCamera = false;
        rotationTarget = null;
    }
}