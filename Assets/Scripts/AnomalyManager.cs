using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum AnomalyKind
{
    None = 0,
    NormalExtraDoors = 1,
    NormalLightFlicker = 2,
    NormalLightColorStrong = 3,
    NormalRadiatorColor = 4,
    NormalLightsOff = 5,
    SubtleWcSign = 6,
    SubtleElevatorButtons = 7,
    SubtlePosterColor = 8,
    SubtleExtraPoster = 9,
    SubtleLightTint = 10,
    SubtleMissingPillar = 11,
    SubtleDoorAtEnd = 12,
    SubtleMissingLight = 13,
    SubtleWeirdSoundMiddle = 14,
    SubtleDoorColor = 15
}

public class AnomalyManager : MonoBehaviour
{
    [Header("Anomalia de llum")]
    [Tooltip("Opcional: llista de llums fixes per les anomalies.")]
    public List<Light> llumsCandidates = new List<Light>();
    [Tooltip("Quan esta actiu, prioritza llums properes al jugador.")]
    public bool prioritzarLlumsProperes = true;
    [Min(2f)] public float radiMaximLlumPropera = 28f;
    [Tooltip("Radi maxim per prioritzar objectes propers.")]
    [Min(4f)] public float radiMaximAnomaliaPropera = 42f;

    [Header("Config")]
    [Range(0f, 1f)] public float probabilitatAnomalia = 0.85f;
    public bool evitarRepeticioConsecutiva = true;
    public bool permetreAnomaliaLlum = true;
    public bool permetreAnomaliaSo = true;
    public bool permetreAnomaliaSubtil = true;

    readonly List<Transform> portesArrel = new List<Transform>();
    readonly List<Renderer> portesPanell = new List<Renderer>();
    readonly List<Light> llums = new List<Light>();
    readonly List<Transform> cartellsLavabo = new List<Transform>();
    readonly List<Renderer> papersPanell = new List<Renderer>();
    readonly List<GameObject> botonsAscensor = new List<GameObject>();
    readonly List<Renderer> radiadors = new List<Renderer>();
    readonly List<GameObject> pilars = new List<GameObject>();
    readonly List<int> indexosPropers = new List<int>();

    readonly List<AnomalyKind> bufferTipus = new List<AnomalyKind>();

    int indexActiu = -1;
    int ultimIndex = -1;
    AnomalyKind tipusActiu = AnomalyKind.None;
    AnomalyKind ultimTipus = AnomalyKind.None;

    struct LightState
    {
        public Light light;
        public bool enabled;
        public float intensity;
        public Color color;
    }

    readonly List<LightState> llumsMultiplesModificades = new List<LightState>();

    Light llumActiva;
    bool llumEnabledOriginal;
    float llumIntensitatOriginal = -1f;
    Color llumColorOriginal = Color.white;
    Coroutine flickerCoroutine;

    Renderer rendererActiu;
    Color rendererColorOriginal;

    Transform cartellWcActiu;
    Vector3 cartellWcScaleOriginal;
    TextMesh cartellWcTextMesh;
    string cartellWcTextOriginal;
    TMP_Text cartellWcTMP;
    string cartellWcTMPOriginal;

    GameObject botoAscensorActiu;
    bool botoVisibleOriginal;

    GameObject pilarActiu;
    bool pilarVisibleOriginal;

    readonly List<GameObject> instanciesTemporals = new List<GameObject>();

    GameObject soNodeActiu;
    AudioSource soActiu;
    AudioClip soClipActiu;

    MaterialPropertyBlock propertyBlock;

    public bool TeAnomaliaActiva => tipusActiu != AnomalyKind.None;
    public int IndexAnomaliaActiva => indexActiu;
    public AnomalyKind TipusAnomaliaActiva => tipusActiu;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        RecarregarAnomalies();
        DesactivarTot();
    }

    void OnDisable()
    {
        DesactivarTot();
    }

    public void RecarregarAnomalies()
    {
        portesArrel.Clear();
        portesPanell.Clear();
        llums.Clear();
        cartellsLavabo.Clear();
        papersPanell.Clear();
        botonsAscensor.Clear();
        radiadors.Clear();
        pilars.Clear();

        if (llumsCandidates != null && llumsCandidates.Count > 0)
        {
            for (int i = 0; i < llumsCandidates.Count; i++)
            {
                Light ll = llumsCandidates[i];
                if (ll != null)
                {
                    llums.Add(ll);
                }
            }
        }

        Transform[] tots = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < tots.Length; i++)
        {
            Transform tr = tots[i];
            if (tr == null)
            {
                continue;
            }

            string nom = tr.name.ToLowerInvariant();

            if (llumsCandidates == null || llumsCandidates.Count == 0)
            {
                Light ll = tr.GetComponent<Light>();
                if (ll != null && (nom.Contains("llum") || nom.Contains("puntllum") || nom.Contains("light")))
                {
                    llums.Add(ll);
                }
            }

            if ((nom.StartsWith("porta_") || nom.Contains("porta")) && tr.Find("Marc_Esq") != null)
            {
                portesArrel.Add(tr);

                Transform panell = tr.Find("Porta_Blanque");
                if (panell == null)
                {
                    panell = tr.Find("Porta_Lavabo");
                }

                if (panell != null)
                {
                    Renderer rPanell = panell.GetComponent<Renderer>();
                    if (rPanell == null)
                    {
                        rPanell = panell.GetComponentInChildren<Renderer>(true);
                    }
                    if (rPanell != null)
                    {
                        portesPanell.Add(rPanell);
                    }
                }
            }

            if (nom == "cartelllavabo_text")
            {
                cartellsLavabo.Add(tr);
            }

            if (nom.StartsWith("paper_"))
            {
                Renderer r = tr.GetComponent<Renderer>();
                if (r != null)
                {
                    papersPanell.Add(r);
                }
            }

            if (nom.Contains("ascensor_boto"))
            {
                botonsAscensor.Add(tr.gameObject);
            }

            if (nom.Contains("radiador"))
            {
                Renderer r = tr.GetComponent<Renderer>();
                if (r != null)
                {
                    radiadors.Add(r);
                }
            }

            if (nom.StartsWith("pilar") || nom.Contains("pilar_"))
            {
                pilars.Add(tr.gameObject);
            }
        }

        for (int i = llums.Count - 1; i >= 0; i--)
        {
            if (llums[i] == null)
            {
                llums.RemoveAt(i);
            }
        }
    }

    public void PrepararRonda(int nivell, bool forcarSenseAnomalia)
    {
        DesactivarTot();

        if (nivell <= 0 || forcarSenseAnomalia)
        {
            return;
        }

        if (Random.value > probabilitatAnomalia)
        {
            return;
        }

        ConstruirTipusDisponibles(bufferTipus);
        if (bufferTipus.Count == 0)
        {
            return;
        }

        if (evitarRepeticioConsecutiva && bufferTipus.Count > 1)
        {
            bufferTipus.Remove(ultimTipus);
            if (bufferTipus.Count == 0)
            {
                ConstruirTipusDisponibles(bufferTipus);
            }
        }

        int intentsMaxims = Mathf.Min(48, bufferTipus.Count * 5);
        for (int intent = 0; intent < intentsMaxims && bufferTipus.Count > 0; intent++)
        {
            int idxTipus = Random.Range(0, bufferTipus.Count);
            AnomalyKind tipus = bufferTipus[idxTipus];
            int index = TriarIndexPerTipus(tipus);

            if (evitarRepeticioConsecutiva && tipus == ultimTipus && index == ultimIndex && ComptarCandidats(tipus) > 1)
            {
                index = (index + 1) % Mathf.Max(1, ComptarCandidats(tipus));
            }

            if (ActivarAnomalia(tipus, index))
            {
                ultimTipus = tipus;
                ultimIndex = index;
                return;
            }

            bufferTipus.RemoveAt(idxTipus);
        }
    }

    public void AplicarGuardat(bool teAnomalia, AnomalyKind tipus, int index)
    {
        DesactivarTot();
        if (!teAnomalia || tipus == AnomalyKind.None)
        {
            return;
        }

        ActivarAnomalia(tipus, index);
    }

    public void DesactivarTot()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        RestaurarEstatLlumUnica();
        RestaurarLlumsMultiples();
        RestaurarRendererActiu();
        RestaurarCartellWc();
        RestaurarBotoAscensor();
        RestaurarPilar();
        EliminarInstanciesTemporals();
        AturarSo();

        tipusActiu = AnomalyKind.None;
        indexActiu = -1;
    }

    void ConstruirTipusDisponibles(List<AnomalyKind> desti)
    {
        desti.Clear();

        if (portesArrel.Count > 0) desti.Add(AnomalyKind.NormalExtraDoors);
        if (permetreAnomaliaLlum && llums.Count > 0) desti.Add(AnomalyKind.NormalLightFlicker);
        if (permetreAnomaliaLlum && llums.Count > 0) desti.Add(AnomalyKind.NormalLightColorStrong);
        if (radiadors.Count > 0) desti.Add(AnomalyKind.NormalRadiatorColor);
        if (permetreAnomaliaLlum && llums.Count > 0) desti.Add(AnomalyKind.NormalLightsOff);

        if (!permetreAnomaliaSubtil)
        {
            return;
        }

        if (cartellsLavabo.Count > 0) desti.Add(AnomalyKind.SubtleWcSign);
        if (botonsAscensor.Count > 0) desti.Add(AnomalyKind.SubtleElevatorButtons);
        if (papersPanell.Count > 0) desti.Add(AnomalyKind.SubtlePosterColor);
        if (papersPanell.Count > 0) desti.Add(AnomalyKind.SubtleExtraPoster);
        if (permetreAnomaliaLlum && llums.Count > 0) desti.Add(AnomalyKind.SubtleLightTint);
        if (pilars.Count > 0) desti.Add(AnomalyKind.SubtleMissingPillar);
        if (portesArrel.Count > 0) desti.Add(AnomalyKind.SubtleDoorAtEnd);
        if (permetreAnomaliaLlum && llums.Count > 0) desti.Add(AnomalyKind.SubtleMissingLight);
        if (permetreAnomaliaSo) desti.Add(AnomalyKind.SubtleWeirdSoundMiddle);
        if (portesPanell.Count > 0) desti.Add(AnomalyKind.SubtleDoorColor);
    }

    int ComptarCandidats(AnomalyKind tipus)
    {
        switch (tipus)
        {
            case AnomalyKind.NormalExtraDoors: return portesArrel.Count;
            case AnomalyKind.NormalLightFlicker:
            case AnomalyKind.NormalLightColorStrong:
            case AnomalyKind.NormalLightsOff:
            case AnomalyKind.SubtleLightTint:
            case AnomalyKind.SubtleMissingLight:
                return llums.Count;
            case AnomalyKind.NormalRadiatorColor: return radiadors.Count;
            case AnomalyKind.SubtleWcSign: return cartellsLavabo.Count;
            case AnomalyKind.SubtleElevatorButtons: return botonsAscensor.Count;
            case AnomalyKind.SubtlePosterColor:
            case AnomalyKind.SubtleExtraPoster:
                return papersPanell.Count;
            case AnomalyKind.SubtleMissingPillar: return pilars.Count;
            case AnomalyKind.SubtleDoorAtEnd: return portesArrel.Count;
            case AnomalyKind.SubtleDoorColor: return portesPanell.Count;
            case AnomalyKind.SubtleWeirdSoundMiddle: return 3;
            default: return 0;
        }
    }

    int TriarIndexPerTipus(AnomalyKind tipus)
    {
        int quantitat = ComptarCandidats(tipus);
        if (quantitat <= 0)
        {
            return -1;
        }

        switch (tipus)
        {
            case AnomalyKind.NormalExtraDoors:
            case AnomalyKind.SubtleDoorAtEnd:
                {
                    int idx = TriarIndexTransformList(portesArrel, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.NormalLightFlicker:
            case AnomalyKind.NormalLightColorStrong:
            case AnomalyKind.NormalLightsOff:
            case AnomalyKind.SubtleLightTint:
            case AnomalyKind.SubtleMissingLight:
                return TriarIndexLlum();
            case AnomalyKind.NormalRadiatorColor:
                {
                    int idx = TriarIndexRendererList(radiadors, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtleWcSign:
                {
                    int idx = TriarIndexTransformList(cartellsLavabo, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtleElevatorButtons:
                {
                    int idx = TriarIndexGameObjectList(botonsAscensor, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtlePosterColor:
            case AnomalyKind.SubtleExtraPoster:
                {
                    int idx = TriarIndexRendererList(papersPanell, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtleMissingPillar:
                {
                    int idx = TriarIndexGameObjectList(pilars, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtleDoorColor:
                {
                    int idx = TriarIndexRendererList(portesPanell, radiMaximAnomaliaPropera);
                    return idx >= 0 ? idx : Random.Range(0, quantitat);
                }
            case AnomalyKind.SubtleWeirdSoundMiddle:
                return Random.Range(0, 3);
        }

        return Random.Range(0, quantitat);
    }

    int TriarIndexLlum()
    {
        if (llums.Count == 0)
        {
            return -1;
        }

        if (!prioritzarLlumsProperes)
        {
            return Random.Range(0, llums.Count);
        }

        Transform trJugador = TrobarTransformJugador();
        if (trJugador == null)
        {
            return Random.Range(0, llums.Count);
        }

        float radi2 = radiMaximLlumPropera * radiMaximLlumPropera;
        Vector3 pj = trJugador.position;
        indexosPropers.Clear();

        for (int i = 0; i < llums.Count; i++)
        {
            Light l = llums[i];
            if (l == null)
            {
                continue;
            }

            float d2 = (l.transform.position - pj).sqrMagnitude;
            if (d2 <= radi2)
            {
                indexosPropers.Add(i);
            }
        }

        if (indexosPropers.Count > 0)
        {
            return indexosPropers[Random.Range(0, indexosPropers.Count)];
        }

        return Random.Range(0, llums.Count);
    }

    int TriarIndexTransformList(List<Transform> llista, float radi)
    {
        if (llista == null || llista.Count == 0)
        {
            return -1;
        }

        Transform trJugador = TrobarTransformJugador();
        if (trJugador == null)
        {
            return Random.Range(0, llista.Count);
        }

        float radi2 = radi * radi;
        Vector3 pj = trJugador.position;
        indexosPropers.Clear();

        for (int i = 0; i < llista.Count; i++)
        {
            Transform t = llista[i];
            if (t == null)
            {
                continue;
            }

            float d2 = (t.position - pj).sqrMagnitude;
            if (d2 <= radi2)
            {
                indexosPropers.Add(i);
            }
        }

        if (indexosPropers.Count > 0)
        {
            return indexosPropers[Random.Range(0, indexosPropers.Count)];
        }

        return Random.Range(0, llista.Count);
    }

    int TriarIndexGameObjectList(List<GameObject> llista, float radi)
    {
        if (llista == null || llista.Count == 0)
        {
            return -1;
        }

        Transform trJugador = TrobarTransformJugador();
        if (trJugador == null)
        {
            return Random.Range(0, llista.Count);
        }

        float radi2 = radi * radi;
        Vector3 pj = trJugador.position;
        indexosPropers.Clear();

        for (int i = 0; i < llista.Count; i++)
        {
            GameObject go = llista[i];
            if (go == null)
            {
                continue;
            }

            float d2 = (go.transform.position - pj).sqrMagnitude;
            if (d2 <= radi2)
            {
                indexosPropers.Add(i);
            }
        }

        if (indexosPropers.Count > 0)
        {
            return indexosPropers[Random.Range(0, indexosPropers.Count)];
        }

        return Random.Range(0, llista.Count);
    }

    int TriarIndexRendererList(List<Renderer> llista, float radi)
    {
        if (llista == null || llista.Count == 0)
        {
            return -1;
        }

        Transform trJugador = TrobarTransformJugador();
        if (trJugador == null)
        {
            return Random.Range(0, llista.Count);
        }

        float radi2 = radi * radi;
        Vector3 pj = trJugador.position;
        indexosPropers.Clear();

        for (int i = 0; i < llista.Count; i++)
        {
            Renderer r = llista[i];
            if (r == null)
            {
                continue;
            }

            float d2 = (r.transform.position - pj).sqrMagnitude;
            if (d2 <= radi2)
            {
                indexosPropers.Add(i);
            }
        }

        if (indexosPropers.Count > 0)
        {
            return indexosPropers[Random.Range(0, indexosPropers.Count)];
        }

        return Random.Range(0, llista.Count);
    }

    bool ActivarAnomalia(AnomalyKind tipus, int index)
    {
        bool ok = false;

        switch (tipus)
        {
            case AnomalyKind.NormalExtraDoors:
                ok = ActivarMesPortesNormal(index);
                break;
            case AnomalyKind.NormalLightFlicker:
                ok = ActivarLlumParpelleigNormal(index);
                break;
            case AnomalyKind.NormalLightColorStrong:
                ok = ActivarColorLlumFortNormal(index);
                break;
            case AnomalyKind.NormalRadiatorColor:
                ok = ActivarColorRadiadorNormal(index);
                break;
            case AnomalyKind.NormalLightsOff:
                ok = ActivarLlumsApagadesNormal(index);
                break;
            case AnomalyKind.SubtleWcSign:
                ok = ActivarCartellWcSubtil(index);
                break;
            case AnomalyKind.SubtleElevatorButtons:
                ok = ActivarBotonsAscensorSubtil(index);
                break;
            case AnomalyKind.SubtlePosterColor:
                ok = ActivarColorCartellsSubtil(index);
                break;
            case AnomalyKind.SubtleExtraPoster:
                ok = ActivarMesCartellsSubtil(index);
                break;
            case AnomalyKind.SubtleLightTint:
                ok = ActivarColorLlumSubtil(index);
                break;
            case AnomalyKind.SubtleMissingPillar:
                ok = ActivarFaltaPilarSubtil(index);
                break;
            case AnomalyKind.SubtleDoorAtEnd:
                ok = ActivarPortaFinalSubtil(index);
                break;
            case AnomalyKind.SubtleMissingLight:
                ok = ActivarFaltaLlumSubtil(index);
                break;
            case AnomalyKind.SubtleWeirdSoundMiddle:
                ok = ActivarSoEstranyMigSubtil(index);
                break;
            case AnomalyKind.SubtleDoorColor:
                ok = ActivarColorPortaSubtil(index);
                break;
        }

        if (ok)
        {
            tipusActiu = tipus;
            indexActiu = index;
        }

        return ok;
    }

    bool ActivarMesPortesNormal(int index)
    {
        Transform basePorta = ObtenirPorta(index);
        if (basePorta == null || basePorta.parent == null)
        {
            return false;
        }

        GameObject p1 = Instantiate(basePorta.gameObject, basePorta.parent);
        p1.name = basePorta.name + "_Extra_N1";
        p1.transform.localPosition = basePorta.localPosition + new Vector3(0f, 0f, 2.2f);
        p1.transform.localRotation = basePorta.localRotation;
        p1.transform.localScale = basePorta.localScale;

        GameObject p2 = Instantiate(basePorta.gameObject, basePorta.parent);
        p2.name = basePorta.name + "_Extra_N2";
        p2.transform.localPosition = basePorta.localPosition + new Vector3(0f, 0f, -2.2f);
        p2.transform.localRotation = basePorta.localRotation;
        p2.transform.localScale = basePorta.localScale;

        instanciesTemporals.Add(p1);
        instanciesTemporals.Add(p2);
        return true;
    }

    bool ActivarLlumParpelleigNormal(int index)
    {
        Light llum = ObtenirLlum(index);
        if (llum == null)
        {
            return false;
        }

        GuardarEstatLlumUnica(llum);
        flickerCoroutine = StartCoroutine(CoroutineLlumParpelleigNormal());
        return true;
    }

    IEnumerator CoroutineLlumParpelleigNormal()
    {
        while (tipusActiu == AnomalyKind.NormalLightFlicker && llumActiva != null)
        {
            float baseIntensity = Mathf.Max(0.05f, llumIntensitatOriginal);
            float mul = Random.Range(0.05f, 1.30f);
            llumActiva.intensity = baseIntensity * mul;
            llumActiva.enabled = Random.value > 0.08f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.14f));
        }
    }

    bool ActivarColorLlumFortNormal(int index)
    {
        Light llum = ObtenirLlum(index);
        if (llum == null)
        {
            return false;
        }

        GuardarEstatLlumUnica(llum);

        Color[] variants =
        {
            new Color(1f, 0.2f, 0.2f),
            new Color(0.2f, 1f, 0.3f),
            new Color(0.2f, 0.55f, 1f),
            new Color(1f, 0.2f, 1f)
        };

        llum.color = variants[Random.Range(0, variants.Length)];
        llum.intensity = Mathf.Max(0.3f, llumIntensitatOriginal * 1.35f);
        return true;
    }

    bool ActivarColorRadiadorNormal(int index)
    {
        if (index < 0 || index >= radiadors.Count)
        {
            return false;
        }

        Renderer r = radiadors[index];
        if (r == null)
        {
            return false;
        }

        if (!GuardarColorRenderer(r))
        {
            return false;
        }

        Color[] variants =
        {
            new Color(0.70f, 0.16f, 0.16f),
            new Color(0.18f, 0.28f, 0.70f),
            new Color(0.55f, 0.14f, 0.55f)
        };

        AssignarColorRenderer(r, variants[Random.Range(0, variants.Length)]);
        return true;
    }

    bool ActivarLlumsApagadesNormal(int index)
    {
        Light center = ObtenirLlum(index);
        if (center == null)
        {
            return false;
        }

        List<Light> candidates = new List<Light>();
        for (int i = 0; i < llums.Count; i++)
        {
            if (llums[i] != null)
            {
                candidates.Add(llums[i]);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        candidates.Sort((a, b) =>
        {
            float da = (a.transform.position - center.transform.position).sqrMagnitude;
            float db = (b.transform.position - center.transform.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        int quantitat = Mathf.Clamp(candidates.Count >= 5 ? 3 : 2, 1, candidates.Count);
        for (int i = 0; i < quantitat; i++)
        {
            Light l = candidates[i];
            if (l == null)
            {
                continue;
            }

            LightState st = new LightState
            {
                light = l,
                enabled = l.enabled,
                intensity = l.intensity,
                color = l.color
            };

            llumsMultiplesModificades.Add(st);
            l.enabled = false;
        }

        return llumsMultiplesModificades.Count > 0;
    }

    bool ActivarCartellWcSubtil(int index)
    {
        if (index < 0 || index >= cartellsLavabo.Count)
        {
            return false;
        }

        Transform t = cartellsLavabo[index];
        if (t == null)
        {
            return false;
        }

        cartellWcActiu = t;
        cartellWcScaleOriginal = t.localScale;

        cartellWcTextMesh = t.GetComponent<TextMesh>();
        cartellWcTMP = t.GetComponent<TMP_Text>();
        if (cartellWcTMP == null)
        {
            cartellWcTMP = t.GetComponentInChildren<TMP_Text>(true);
        }

        cartellWcTextOriginal = cartellWcTextMesh != null ? cartellWcTextMesh.text : null;
        cartellWcTMPOriginal = cartellWcTMP != null ? cartellWcTMP.text : null;

        bool variantMirall = Random.value < 0.5f;

        if (!variantMirall && cartellWcTextMesh == null && cartellWcTMP == null)
        {
            variantMirall = true;
        }

        if (variantMirall)
        {
            Vector3 s = t.localScale;
            float sx = Mathf.Abs(s.x) <= 0.0001f ? 1f : Mathf.Abs(s.x);
            s.x = -sx;
            t.localScale = s;
            return true;
        }

        if (cartellWcTextMesh != null)
        {
            cartellWcTextMesh.text = "WC HOMES";
        }
        if (cartellWcTMP != null)
        {
            cartellWcTMP.text = "WC HOMES";
        }

        return true;
    }

    bool ActivarBotonsAscensorSubtil(int index)
    {
        if (index < 0 || index >= botonsAscensor.Count)
        {
            return false;
        }

        GameObject go = botonsAscensor[index];
        if (go == null)
        {
            return false;
        }

        bool variantFalta = Random.value < 0.5f;

        if (variantFalta)
        {
            botoAscensorActiu = go;
            botoVisibleOriginal = go.activeSelf;
            go.SetActive(false);
            return true;
        }

        if (go.transform.parent == null)
        {
            return false;
        }

        GameObject extra = Instantiate(go, go.transform.parent);
        extra.name = go.name + "_Extra";
        extra.transform.localPosition = go.transform.localPosition + new Vector3(0f, 0.14f, 0f);
        extra.transform.localRotation = go.transform.localRotation;
        extra.transform.localScale = go.transform.localScale;
        instanciesTemporals.Add(extra);
        return true;
    }

    bool ActivarColorCartellsSubtil(int index)
    {
        if (index < 0 || index >= papersPanell.Count)
        {
            return false;
        }

        Renderer r = papersPanell[index];
        if (r == null || !GuardarColorRenderer(r))
        {
            return false;
        }

        Color[] variants =
        {
            new Color(0.95f, 0.92f, 0.70f),
            new Color(0.86f, 0.94f, 0.78f),
            new Color(0.84f, 0.90f, 0.96f)
        };

        AssignarColorRenderer(r, variants[Random.Range(0, variants.Length)]);
        return true;
    }

    bool ActivarMesCartellsSubtil(int index)
    {
        if (index < 0 || index >= papersPanell.Count)
        {
            return false;
        }

        Renderer r = papersPanell[index];
        if (r == null)
        {
            return false;
        }

        Transform baseTr = r.transform;
        if (baseTr.parent == null)
        {
            return false;
        }

        GameObject extra = Instantiate(baseTr.gameObject, baseTr.parent);
        extra.name = baseTr.name + "_Extra";
        extra.transform.localPosition = baseTr.localPosition + new Vector3(0.17f, 0.11f, 0f);
        extra.transform.localRotation = baseTr.localRotation;
        extra.transform.localScale = baseTr.localScale;
        instanciesTemporals.Add(extra);
        return true;
    }

    bool ActivarColorLlumSubtil(int index)
    {
        Light llum = ObtenirLlum(index);
        if (llum == null)
        {
            return false;
        }

        GuardarEstatLlumUnica(llum);

        Color[] variants =
        {
            new Color(1f, 0.98f, 0.92f),
            new Color(0.95f, 0.98f, 1f),
            new Color(1f, 0.97f, 0.88f)
        };

        llum.color = variants[Random.Range(0, variants.Length)];
        llum.intensity = Mathf.Max(0.2f, llumIntensitatOriginal * Random.Range(0.95f, 1.06f));
        return true;
    }

    bool ActivarFaltaPilarSubtil(int index)
    {
        if (index < 0 || index >= pilars.Count)
        {
            return false;
        }

        GameObject go = pilars[index];
        if (go == null)
        {
            return false;
        }

        pilarActiu = go;
        pilarVisibleOriginal = go.activeSelf;
        go.SetActive(false);
        return true;
    }

    bool ActivarPortaFinalSubtil(int index)
    {
        Transform basePorta = ObtenirPorta(index);
        if (basePorta == null || basePorta.parent == null)
        {
            return false;
        }

        Vector3 offset = new Vector3(0f, 0f, 2.2f);

        Transform zonaFinal = ObtenirZonaFinalTransform();
        if (zonaFinal != null)
        {
            Vector3 dirWorld = (zonaFinal.position - basePorta.position);
            dirWorld.y = 0f;
            if (dirWorld.sqrMagnitude > 0.001f)
            {
                Vector3 dirLocal = basePorta.parent.InverseTransformDirection(dirWorld.normalized);
                if (Mathf.Abs(dirLocal.x) > Mathf.Abs(dirLocal.z))
                {
                    offset = new Vector3(Mathf.Sign(dirLocal.x) * 2.2f, 0f, 0f);
                }
                else
                {
                    offset = new Vector3(0f, 0f, Mathf.Sign(dirLocal.z) * 2.2f);
                }
            }
        }

        GameObject extra = Instantiate(basePorta.gameObject, basePorta.parent);
        extra.name = basePorta.name + "_FinalSubtil";
        extra.transform.localPosition = basePorta.localPosition + offset;
        extra.transform.localRotation = basePorta.localRotation;
        extra.transform.localScale = basePorta.localScale;
        instanciesTemporals.Add(extra);
        return true;
    }

    bool ActivarFaltaLlumSubtil(int index)
    {
        Light llum = ObtenirLlum(index);
        if (llum == null)
        {
            return false;
        }

        GuardarEstatLlumUnica(llum);
        llum.enabled = false;
        return true;
    }

    bool ActivarSoEstranyMigSubtil(int indexVariant)
    {
        if (soNodeActiu == null)
        {
            soNodeActiu = new GameObject("Anomalia_So");
            soNodeActiu.transform.SetParent(transform, false);
        }

        if (soActiu == null)
        {
            soActiu = soNodeActiu.AddComponent<AudioSource>();
        }

        soNodeActiu.transform.position = ObtenirPosicioMigPassadis();

        soClipActiu = CrearClipEstrany(Mathf.Abs(indexVariant) % 3);
        if (soClipActiu == null)
        {
            return false;
        }

        soActiu.clip = soClipActiu;
        soActiu.loop = true;
        soActiu.playOnAwake = false;
        soActiu.spatialBlend = 1f;
        soActiu.minDistance = 0.8f;
        soActiu.maxDistance = 14f;
        soActiu.volume = 0.2f;
        soActiu.Play();
        return true;
    }

    bool ActivarColorPortaSubtil(int index)
    {
        if (index < 0 || index >= portesPanell.Count)
        {
            return false;
        }

        Renderer r = portesPanell[index];
        if (r == null || !GuardarColorRenderer(r))
        {
            return false;
        }

        Color baseColor = rendererColorOriginal;
        float delta = 0.07f;
        Color nou = new Color(
            Mathf.Clamp01(baseColor.r + Random.Range(-delta, delta)),
            Mathf.Clamp01(baseColor.g + Random.Range(-delta, delta)),
            Mathf.Clamp01(baseColor.b + Random.Range(-delta, delta)),
            baseColor.a);

        AssignarColorRenderer(r, nou);
        return true;
    }

    Transform ObtenirPorta(int index)
    {
        if (index < 0 || index >= portesArrel.Count)
        {
            return null;
        }

        return portesArrel[index];
    }

    Light ObtenirLlum(int index)
    {
        if (index < 0 || index >= llums.Count)
        {
            return null;
        }
        return llums[index];
    }

    void GuardarEstatLlumUnica(Light llum)
    {
        llumActiva = llum;
        llumEnabledOriginal = llum.enabled;
        llumIntensitatOriginal = llum.intensity;
        llumColorOriginal = llum.color;
    }

    void RestaurarEstatLlumUnica()
    {
        if (llumActiva != null)
        {
            llumActiva.enabled = llumEnabledOriginal;
            if (llumIntensitatOriginal >= 0f)
            {
                llumActiva.intensity = llumIntensitatOriginal;
            }
            llumActiva.color = llumColorOriginal;
        }

        llumActiva = null;
        llumEnabledOriginal = true;
        llumIntensitatOriginal = -1f;
        llumColorOriginal = Color.white;
    }

    void RestaurarLlumsMultiples()
    {
        for (int i = 0; i < llumsMultiplesModificades.Count; i++)
        {
            LightState st = llumsMultiplesModificades[i];
            if (st.light == null)
            {
                continue;
            }

            st.light.enabled = st.enabled;
            st.light.intensity = st.intensity;
            st.light.color = st.color;
        }

        llumsMultiplesModificades.Clear();
    }

    bool GuardarColorRenderer(Renderer r)
    {
        if (r == null || r.sharedMaterial == null)
        {
            return false;
        }

        rendererActiu = r;
        rendererColorOriginal = LlegirColorRenderer(r);
        return true;
    }

    void RestaurarRendererActiu()
    {
        if (rendererActiu != null)
        {
            AssignarColorRenderer(rendererActiu, rendererColorOriginal);
        }

        rendererActiu = null;
    }

    void RestaurarCartellWc()
    {
        if (cartellWcActiu != null)
        {
            cartellWcActiu.localScale = cartellWcScaleOriginal;
        }

        if (cartellWcTextMesh != null && cartellWcTextOriginal != null)
        {
            cartellWcTextMesh.text = cartellWcTextOriginal;
        }

        if (cartellWcTMP != null && cartellWcTMPOriginal != null)
        {
            cartellWcTMP.text = cartellWcTMPOriginal;
        }

        cartellWcActiu = null;
        cartellWcTextMesh = null;
        cartellWcTextOriginal = null;
        cartellWcTMP = null;
        cartellWcTMPOriginal = null;
    }

    void RestaurarBotoAscensor()
    {
        if (botoAscensorActiu != null)
        {
            botoAscensorActiu.SetActive(botoVisibleOriginal);
        }

        botoAscensorActiu = null;
        botoVisibleOriginal = true;
    }

    void RestaurarPilar()
    {
        if (pilarActiu != null)
        {
            pilarActiu.SetActive(pilarVisibleOriginal);
        }

        pilarActiu = null;
        pilarVisibleOriginal = true;
    }

    void EliminarInstanciesTemporals()
    {
        for (int i = 0; i < instanciesTemporals.Count; i++)
        {
            GameObject go = instanciesTemporals[i];
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        instanciesTemporals.Clear();
    }

    void AturarSo()
    {
        if (soActiu != null)
        {
            soActiu.Stop();
        }

        if (soClipActiu != null)
        {
            if (Application.isPlaying)
            {
                Destroy(soClipActiu);
            }
            else
            {
                DestroyImmediate(soClipActiu);
            }
            soClipActiu = null;
        }

        if (soNodeActiu != null)
        {
            if (Application.isPlaying)
            {
                Destroy(soNodeActiu);
            }
            else
            {
                DestroyImmediate(soNodeActiu);
            }
        }

        soNodeActiu = null;
        soActiu = null;
    }

    Vector3 ObtenirPosicioMigPassadis()
    {
        Transform zonaInici = ObtenirZonaIniciTransform();
        Transform zonaFinal = ObtenirZonaFinalTransform();

        if (zonaInici != null && zonaFinal != null)
        {
            return (zonaInici.position + zonaFinal.position) * 0.5f + Vector3.up * 1.2f;
        }

        Transform jugador = TrobarTransformJugador();
        if (jugador != null)
        {
            return jugador.position + jugador.forward * 4f + Vector3.up * 1.2f;
        }

        return transform.position + Vector3.up * 1.2f;
    }

    Transform ObtenirZonaIniciTransform()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.zonaInici != null)
        {
            return GameManager.Instancia.zonaInici.transform;
        }

        ZonaTrigger[] zones = Object.FindObjectsByType<ZonaTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null && zones[i].Tipus == ZonaTipus.Inici)
            {
                return zones[i].transform;
            }
        }

        return null;
    }

    Transform ObtenirZonaFinalTransform()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.zonaFinal != null)
        {
            return GameManager.Instancia.zonaFinal.transform;
        }

        ZonaTrigger[] zones = Object.FindObjectsByType<ZonaTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null && zones[i].Tipus == ZonaTipus.Final)
            {
                return zones[i].transform;
            }
        }

        return null;
    }

    AudioClip CrearClipEstrany(int variant)
    {
        int sampleRate = 22050;
        float duracio = 2.4f;
        int total = Mathf.CeilToInt(sampleRate * duracio);
        if (total <= 0)
        {
            return null;
        }

        AudioClip clip = AudioClip.Create("anomalia_so", total, 1, sampleRate, false);
        float[] data = new float[total];

        float freqBase = variant == 0 ? 102f : (variant == 1 ? 68f : 152f);
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Clamp01(1f - Mathf.Abs((t / duracio) * 2f - 1f));
            float wobble = Mathf.Sin(t * (variant == 2 ? 7.3f : 4.9f)) * 0.16f;

            float senyal =
                Mathf.Sin(2f * Mathf.PI * (freqBase + wobble * 15f) * t) * 0.36f +
                Mathf.Sin(2f * Mathf.PI * (freqBase * 0.5f) * t) * 0.17f +
                (Random.value * 2f - 1f) * 0.04f;

            data[i] = Mathf.Clamp(senyal * env * 0.85f, -1f, 1f);
        }

        clip.SetData(data, 0);
        return clip;
    }

    Color LlegirColorRenderer(Renderer r)
    {
        if (r == null || r.sharedMaterial == null)
        {
            return Color.white;
        }

        if (r.sharedMaterial.HasProperty("_BaseColor"))
        {
            return r.sharedMaterial.GetColor("_BaseColor");
        }
        if (r.sharedMaterial.HasProperty("_Color"))
        {
            return r.sharedMaterial.GetColor("_Color");
        }

        return Color.white;
    }

    void AssignarColorRenderer(Renderer r, Color color)
    {
        if (r == null || r.sharedMaterial == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        r.GetPropertyBlock(propertyBlock);
        if (r.sharedMaterial.HasProperty("_BaseColor"))
        {
            propertyBlock.SetColor("_BaseColor", color);
        }
        else if (r.sharedMaterial.HasProperty("_Color"))
        {
            propertyBlock.SetColor("_Color", color);
        }
        r.SetPropertyBlock(propertyBlock);
    }

    Transform TrobarTransformJugador()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.jugador != null)
        {
            return GameManager.Instancia.jugador.transform;
        }

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            return pc.transform;
        }

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }

}

