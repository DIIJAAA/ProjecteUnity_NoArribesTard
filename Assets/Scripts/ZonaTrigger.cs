using System;
using UnityEngine;

public enum ZonaTipus
{
    Inici = 0,
    Final = 1
}

[RequireComponent(typeof(BoxCollider))]
public class ZonaTrigger : MonoBehaviour
{
    [Header("Configuracio")]
    [SerializeField] ZonaTipus tipus = ZonaTipus.Inici;
    [SerializeField] string tagJugador = "Player";
    [SerializeField, Min(0f)] float cooldownSegons = 0.2f;

    float ultimTriggerTime = -999f;

    public ZonaTipus Tipus => tipus;
    public event Action<ZonaTrigger> OnJugadorEntra;

    void Awake()
    {
        AssegurarColliderTrigger();
        InferirTipusPerNomSiCal();
    }

    void Reset()
    {
        AssegurarColliderTrigger();
        InferirTipusPerNomSiCal();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other))
        {
            return;
        }

        if (Time.time < ultimTriggerTime + cooldownSegons)
        {
            return;
        }

        ultimTriggerTime = Time.time;
        OnJugadorEntra?.Invoke(this);
    }

    bool EsJugador(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(tagJugador) && other.CompareTag(tagJugador))
        {
            return true;
        }

        Transform root = other.transform.root;
        if (root != null && !string.IsNullOrWhiteSpace(tagJugador) && root.CompareTag(tagJugador))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerController>() != null;
    }

    void AssegurarColliderTrigger()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            return;
        }

        bc.isTrigger = true;
    }

    void InferirTipusPerNomSiCal()
    {
        string nom = gameObject.name.ToLowerInvariant();
        if (nom.Contains("final"))
        {
            tipus = ZonaTipus.Final;
        }
        else if (nom.Contains("inici"))
        {
            tipus = ZonaTipus.Inici;
        }
    }

}
