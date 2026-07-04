using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class ToyHammerVFX : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject boomVfxPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0.8f);
    [SerializeField] private Vector3 localScale = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 localRotation = new Vector3(0, 0, 0);
    
    [Header("Timing Settings")]
    [SerializeField] private float playDelay = 0f;

    private GameObject currentVfxInstance;
    private VideoPlayer videoPlayer;
    private MCAnimationController animController;

    private void Start()
    {
        if (boomVfxPrefab != null)
        {
            currentVfxInstance = Instantiate(boomVfxPrefab);
            currentVfxInstance.transform.localScale = localScale;
            currentVfxInstance.SetActive(false);

            videoPlayer = currentVfxInstance.GetComponentInChildren<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping   = false;
                videoPlayer.loopPointReached += OnVideoEnd;
            }
            else
            {
                Debug.LogWarning("[ToyHammerVFX] No VideoPlayer found inside the boom VFX prefab.");
            }
        }

        animController = FindFirstObjectByType<MCAnimationController>();
        if (animController != null)
            animController.OnHammerAttack2Fired += PlayVFX;
        else
            Debug.LogWarning("[ToyHammerVFX] MCAnimationController not found — VFX won't trigger.");
    }

    private void OnDestroy()
    {
        if (animController != null)
            animController.OnHammerAttack2Fired -= PlayVFX;

        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
            
        if (currentVfxInstance != null)
            Destroy(currentVfxInstance);
    }


    public void PlayVFX()
    {
        if (currentVfxInstance == null || videoPlayer == null) return;

        StopAllCoroutines(); 
        
        if (playDelay > 0f)
        {
            StartCoroutine(PlayVFXWithDelay());
        }
        else
        {
            ExecutePlayVFX();
        }
    }

    private IEnumerator PlayVFXWithDelay()
    {
        yield return new WaitForSeconds(playDelay);
        ExecutePlayVFX();
    }

    private void ExecutePlayVFX()
    {
        if (currentVfxInstance == null || videoPlayer == null) return;

        currentVfxInstance.transform.position = transform.TransformPoint(offset);
        
        if (Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - currentVfxInstance.transform.position;
            currentVfxInstance.transform.rotation = Quaternion.LookRotation(-directionToCamera) * Quaternion.Euler(localRotation);
        }
        else
        {
            currentVfxInstance.transform.rotation = transform.rotation * Quaternion.Euler(localRotation);
        }
        
        currentVfxInstance.transform.localScale = localScale;
        
        currentVfxInstance.SetActive(true);
        videoPlayer.time = 0;
        videoPlayer.Play();
    }
    
    public void StopVFX()
    {
        StopAllCoroutines();
        if (currentVfxInstance != null)
        {
            currentVfxInstance.SetActive(false);
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (currentVfxInstance != null)
            currentVfxInstance.SetActive(false);
    }
}