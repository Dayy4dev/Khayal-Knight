using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings")]
    [SerializeField] private float defaultDuration   = 0.3f;
    [SerializeField] private float defaultAmplitude   = 0.15f;
    [SerializeField] private float defaultFrequency   = 25f;

    private CinemachineImpulseSource impulseSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Find the Cinemachine Impulse Source on the CinemachineCamera
        CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam != null)
        {
            impulseSource = cam.GetComponent<CinemachineImpulseSource>();
        }

        if (impulseSource == null)
        {
            Debug.LogWarning("[CameraShake] No CinemachineImpulseSource found on CinemachineCamera!");
        }
    }

    public void Shake(float duration, float amplitude, float frequency)
    {
        if (impulseSource == null) return;

        Vector3 velocity = new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.5f, 0.5f),
            0f
        ).normalized * amplitude * 10f;

        impulseSource.GenerateImpulseWithVelocity(velocity);
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultAmplitude, defaultFrequency);
    }
    
    public void StopShake()
    {
        // Impulse shakes are self-terminating based on the impulse shape.
        // Nothing to stop.
    }
}
