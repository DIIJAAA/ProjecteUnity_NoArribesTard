using UnityEngine;
using UnityEngine.Events;

public class DynamicTrigger : MonoBehaviour
{
    [Tooltip("Metodes que es criden quan el jugador entra al trigger.")]
    public UnityEvent onTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onTriggerEnter.Invoke();
        }
    }
}
