using UnityEngine;

public class DisableAfterAnimation : MonoBehaviour
{
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}