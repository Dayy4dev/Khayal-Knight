using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Camera cam; // assign main camera

    void Start()
{
    // Auto-cari slider jika belum di-assign
    if (slider == null)
        slider = GetComponentInChildren<Slider>();

    // Auto-cari main camera jika belum di-assign
    if (cam == null)
        cam = Camera.main;
}

    void LateUpdate()
    {
        if (cam == null) return;
        
        // Selalu menghadap kamera (billboard)
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                         cam.transform.rotation * Vector3.up);
    }

    public void SetMaxHealth(int max)
    {
        slider.maxValue = max;
        slider.value = max;
    }

    public void SetHealth(int current)
    {
        slider.value = current;
    }
}