using UnityEngine;

// Controla el loop de teletransport entre la zona d'inici i la zona final en execucio.
public class CorridorLoopController : MonoBehaviour
{
    [Header("Config")]
    [Min(0f)] public float movimentCooldown = 0.18f;
    [Tooltip("Si esta actiu, qualsevol entrada per qualsevol zona reapareix sempre a ZonaInici.")]
    public bool sempreReapareixerAInici = true;
    [Tooltip("Si esta actiu, despres del teleport sempre aplica una direccio canonica.")]
    public bool forcarIniciCanonicSempre = true;
    [Tooltip("Si esta actiu, usa una rotacio Y fixa en graus per evitar inversions de direccio.")]
    public bool usarRotacioCanonicaManual = false;
    public float rotacioCanonicaY = 0f;

    [Header("So Teleport")]
    public AudioClip soTeleport;
    [Range(0f, 1f)] public float volumSoTeleport = 0.55f;
    [Min(0.1f)] public float pitchMinSoTeleport = 0.96f;
    [Min(0.1f)] public float pitchMaxSoTeleport = 1.04f;

    [Header("Sortida de cada zona")]
    [Tooltip("Direccio local cap al passadis quan es teletransporta a ZonaInici.")]
    public Vector3 direccioSortidaIniciLocal = Vector3.forward;
    [Tooltip("Direccio local cap al passadis quan es teletransporta a ZonaFinal.")]
    public Vector3 direccioSortidaFinalLocal = Vector3.back;

    [Header("Runtime Refs")]
    [SerializeField] Transform jugador;
    [SerializeField] ZonaTrigger zonaInici;
    [SerializeField] ZonaTrigger zonaFinal;
    [SerializeField] float margeSortidaZona = 0.02f;
    AudioSource audioSoTeleport;

    float ultimMoviment = -999f;

    public ZonaTrigger ZonaInici => zonaInici;
    public ZonaTrigger ZonaFinal => zonaFinal;

    void Awake()
    {
        AutoTrobarTriggersSiCal();
        AutoTrobarJugadorSiCal();
        PrepararAudioTeleport();
    }

    public void SetJugador(Transform nouJugador)
    {
        jugador = nouJugador;
    }

    public void ReiniciarBucle()
    {
        AutoTrobarTriggersSiCal();
        AutoTrobarJugadorSiCal();
        ultimMoviment = -999f;
    }

    public void MoureSegonsZona(ZonaTipus tipus)
    {
        if (!PotMoure())
        {
            return;
        }

        AutoTrobarTriggersSiCal();
        AutoTrobarJugadorSiCal();

        if (jugador == null || zonaInici == null || zonaFinal == null)
        {
            return;
        }

        if (sempreReapareixerAInici)
        {
            if (tipus == ZonaTipus.Final)
            {
                TeleportarACanonic(zonaFinal, zonaInici);
            }
            else
            {
                TeleportarACanonic(zonaInici, zonaInici);
            }

            ultimMoviment = Time.time;
            return;
        }

        if (tipus == ZonaTipus.Final)
        {
            TeleportarEntreZones(zonaFinal, zonaInici);
        }
        else
        {
            TeleportarEntreZones(zonaInici, zonaFinal);
        }

        ultimMoviment = Time.time;
    }

    bool PotMoure()
    {
        return Time.time >= ultimMoviment + movimentCooldown;
    }

    void TeleportarACanonic(ZonaTrigger zonaOrigen, ZonaTrigger zonaDestiCanonic)
    {
        if (jugador == null || zonaOrigen == null || zonaDestiCanonic == null)
        {
            return;
        }

        Transform origen = zonaOrigen.transform;
        Transform desti = zonaDestiCanonic.transform;

        Vector3 localOffsetOrigen = origen.InverseTransformPoint(jugador.position);
        Vector3 localDesti = new Vector3(localOffsetOrigen.x, localOffsetOrigen.y, 0f);

        BoxCollider bcDesti = zonaDestiCanonic.GetComponent<BoxCollider>();
        if (bcDesti != null)
        {
            float halfX = Mathf.Abs(bcDesti.size.x) * 0.5f;
            localDesti.x = Mathf.Clamp(localDesti.x, -halfX, halfX);
        }

        Vector3 basePos = desti.TransformPoint(localDesti);
        Vector3 dirCapCorredor = ObtenirDireccioSortida(zonaDestiCanonic);
        float push = ObtenirPushForaTrigger(zonaDestiCanonic, margeSortidaZona);

        Vector3 novaPos = basePos + dirCapCorredor * push;
        Quaternion novaRot = CalcularRotacioTeleport(origen, desti);
        TeletransportarJugador(novaPos, novaRot);
    }

    void TeleportarEntreZones(ZonaTrigger zonaOrigen, ZonaTrigger zonaDesti)
    {
        if (jugador == null || zonaOrigen == null || zonaDesti == null)
        {
            return;
        }

        Transform origen = zonaOrigen.transform;
        Transform desti = zonaDesti.transform;
        BoxCollider colOrigen = zonaOrigen.GetComponent<BoxCollider>();
        BoxCollider colDesti = zonaDesti.GetComponent<BoxCollider>();

        Vector3 localOffset = origen.InverseTransformPoint(jugador.position);

        if (colOrigen != null && colDesti != null)
        {
            float origenHalfZ = Mathf.Abs(colOrigen.size.z) * 0.5f;
            float destiHalfZ = Mathf.Abs(colDesti.size.z) * 0.5f;

            float normalizedOffset = Mathf.Clamp(localOffset.z, -origenHalfZ, origenHalfZ);
            float ratio = origenHalfZ > 0.0001f ? normalizedOffset / origenHalfZ : 0f;
            localOffset.z = ratio * destiHalfZ;

            Vector3 dirCapCorredor = ObtenirDireccioSortida(zonaDesti);
            Vector3 novaPos = desti.TransformPoint(localOffset);
            novaPos += dirCapCorredor * (destiHalfZ + margeSortidaZona);

            Quaternion novaRot = CalcularRotacioTeleport(origen, desti);
            TeletransportarJugador(novaPos, novaRot);
            return;
        }

        Vector3 fallbackPos = desti.TransformPoint(localOffset);
        Quaternion fallbackRot = CalcularRotacioTeleport(origen, desti);
        TeletransportarJugador(fallbackPos, fallbackRot);
    }

    float ObtenirPushForaTrigger(ZonaTrigger zona, float extra)
    {
        if (zona == null)
        {
            return Mathf.Max(0.02f, extra);
        }

        BoxCollider bc = zona.GetComponent<BoxCollider>();
        if (bc == null)
        {
            return Mathf.Max(0.02f, extra);
        }

        float halfZ = Mathf.Abs(bc.size.z) * 0.5f;
        return halfZ + Mathf.Max(0.02f, extra);
    }

    Quaternion CalcularRotacioTeleport(Transform origen, Transform desti)
    {
        if (!forcarIniciCanonicSempre)
        {
            Quaternion rel = desti.rotation * Quaternion.Inverse(origen.rotation);
            return rel * jugador.rotation;
        }

        if (usarRotacioCanonicaManual)
        {
            return Quaternion.Euler(0f, rotacioCanonicaY, 0f);
        }

        ZonaTrigger zonaRef = zonaInici != null ? zonaInici : zonaFinal;
        Vector3 dirCanonica = ObtenirDireccioSortida(zonaRef);
        if (dirCanonica.sqrMagnitude > 0.0001f)
        {
            return Quaternion.LookRotation(dirCanonica.normalized, Vector3.up);
        }

        return jugador.rotation;
    }

    Vector3 ObtenirDireccioSortida(ZonaTrigger zonaDesti)
    {
        if (zonaDesti == null)
        {
            return Vector3.forward;
        }

        Vector3 local = zonaDesti.Tipus == ZonaTipus.Inici
            ? direccioSortidaIniciLocal
            : direccioSortidaFinalLocal;

        if (zonaDesti == zonaInici && zonaFinal != null)
        {
            Vector3 entreZones = zonaFinal.transform.position - zonaInici.transform.position;
            entreZones.y = 0f;
            if (entreZones.sqrMagnitude > 0.01f)
            {
                return entreZones.normalized;
            }
        }

        if (local.sqrMagnitude < 0.0001f)
        {
            local = Vector3.forward;
        }

        Vector3 world = zonaDesti.transform.TransformDirection(local.normalized);
        world.y = 0f;
        if (world.sqrMagnitude < 0.0001f)
        {
            world = zonaDesti.transform.forward;
            world.y = 0f;
        }

        if (world.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return world.normalized;
    }

    void TeletransportarJugador(Vector3 novaPosicio, Quaternion novaRotacio)
    {
        if (jugador == null)
        {
            return;
        }

        PlayerController pc = jugador.GetComponent<PlayerController>();
        if (pc == null)
        {
            pc = jugador.GetComponentInParent<PlayerController>();
        }

        if (pc != null)
        {
            pc.TeletransportarA(novaPosicio, novaRotacio);
            ReproduirSoTeleport(novaPosicio);
            return;
        }

        CharacterController cc = jugador.GetComponent<CharacterController>();
        bool reactiva = cc != null && cc.enabled;
        if (reactiva)
        {
            cc.enabled = false;
        }

        jugador.SetPositionAndRotation(novaPosicio, novaRotacio);

        if (reactiva)
        {
            cc.enabled = true;
        }

        ReproduirSoTeleport(novaPosicio);
    }

    void PrepararAudioTeleport()
    {
        if (audioSoTeleport == null)
        {
            audioSoTeleport = GetComponent<AudioSource>();
        }
        if (audioSoTeleport == null)
        {
            audioSoTeleport = gameObject.AddComponent<AudioSource>();
        }

        audioSoTeleport.playOnAwake = false;
        audioSoTeleport.loop = false;
        audioSoTeleport.spatialBlend = 0f;
    }

    void ReproduirSoTeleport(Vector3 posicio)
    {
        if (soTeleport == null)
        {
            return;
        }

        float minPitch = Mathf.Min(pitchMinSoTeleport, pitchMaxSoTeleport);
        float maxPitch = Mathf.Max(pitchMinSoTeleport, pitchMaxSoTeleport);
        float pitch = Random.Range(minPitch, maxPitch);

        if (audioSoTeleport != null)
        {
            audioSoTeleport.pitch = pitch;
            audioSoTeleport.PlayOneShot(soTeleport, volumSoTeleport);
            return;
        }

        AudioSource.PlayClipAtPoint(soTeleport, posicio, volumSoTeleport);
    }

    void AutoTrobarTriggersSiCal()
    {
        if (zonaInici == null)
        {
            zonaInici = TrobarTrigger("zonainici", ZonaTipus.Inici);
        }

        if (zonaFinal == null)
        {
            zonaFinal = TrobarTrigger("zonafinal", ZonaTipus.Final);
        }
    }

    void AutoTrobarJugadorSiCal()
    {
        if (jugador != null)
        {
            return;
        }

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            jugador = pc.transform;
            return;
        }

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            jugador = go.transform;
        }
    }

    ZonaTrigger TrobarTrigger(string nomBuscat, ZonaTipus tipus)
    {
        ZonaTrigger[] tots = Object.FindObjectsByType<ZonaTrigger>(FindObjectsSortMode.None);
        ZonaTrigger fallback = null;
        for (int i = 0; i < tots.Length; i++)
        {
            ZonaTrigger z = tots[i];
            if (z == null || z.Tipus != tipus)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = z;
            }

            string n = z.gameObject.name.ToLowerInvariant();
            if (n.Contains(nomBuscat))
            {
                return z;
            }
        }

        return fallback;
    }
}
