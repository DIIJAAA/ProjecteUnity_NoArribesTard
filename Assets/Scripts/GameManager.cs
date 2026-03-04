using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameFlowState
{
    Menu = 0,
    Playing = 1,
    PauseMenu = 2,
    GameOver = 3
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Nom escenes")]
    public string escenaMenu = "MenuInici";
    public string escenaJoc = "Passadis";
    public string escenaGameOver = "GameOver";

    [Header("Bucle")]
    [Min(1)] public int nivellVictoria = 4;
    [Min(0f)] public float tempsTransicio = 0.12f;
    [Min(0f)] public float tempsArmarDecisio = 0.35f;

    [Header("Spawn")]
    public Vector3 spawnPos = new Vector3(0f, 1f, 1.5f);
    public float spawnRotY = 0f;

    [Header("Textos")]
    [TextArea(2, 4)]
    public string instruccionsPartida = "SI HI HA ANOMALIA: TORNA ENRERE.\nSI NO N'HI HA: ENDAVANT.";

    [Header("Referencies de joc")]
    public PlayerController jugador;
    public ZonaTrigger zonaInici;
    public ZonaTrigger zonaFinal;
    public CorridorLoopController loopController;
    public AnomalyManager anomalyManager;

    [Header("Cartell nivell (opcional)")]
    public TextMesh cartellNivellText;

    [Header("Pausa (overlay dins escena de joc)")]
    public GameObject panellPausa;

    [Header("Estat runtime")]
    [SerializeField] GameFlowState estat = GameFlowState.Menu;
    [SerializeField] int nivellActual = 0;
    [SerializeField] float tempsPartida = 0f;
    [SerializeField] bool rondaTeAnomalia = false;
    [SerializeField] bool enTransicio = false;
    [SerializeField] bool decisioArmada = false;
    [SerializeField] float tempsUltimaPartida = 0f;
    [SerializeField] float tempsRecord = -1f;

    bool triggersSubscrits;
    bool pendingResumeFromPause;
    string ultimMissatgeRonda = "COMENCA";
    Coroutine coroutineArmarDecisio;

    // Dades que es guarden quan la partida queda pausada o es tanca.
    bool teSnapshot;
    Vector3 snapshotPos;
    float snapshotRotY;
    float snapshotPitch;
    int snapshotNivell;
    float snapshotTemps;
    bool snapshotTeAnomalia;
    AnomalyKind snapshotTipus;
    int snapshotIndex;

    const string SavePrefix = "NoArribesTard_Save_";
    const string KeyHasSave = SavePrefix + "HasSave";
    const string KeyPosX = SavePrefix + "PosX";
    const string KeyPosY = SavePrefix + "PosY";
    const string KeyPosZ = SavePrefix + "PosZ";
    const string KeyRotY = SavePrefix + "RotY";
    const string KeyPitch = SavePrefix + "Pitch";
    const string KeyNivell = SavePrefix + "Nivell";
    const string KeyTemps = SavePrefix + "Temps";
    const string KeyTeAnomalia = SavePrefix + "TeAnomalia";
    const string KeyTipusAnomalia = SavePrefix + "TipusAnomalia";
    const string KeyIndexAnomalia = SavePrefix + "IndexAnomalia";
    const string KeyTempsRecord = SavePrefix + "TempsRecord";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCrearSiNoExisteix()
    {
        if (Instancia != null)
        {
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Instancia.AbsorbirConfiguracio(this);
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        CarregarGuardatPlayerPrefs();
        CarregarTempsRecordPlayerPrefs();
    }

    void AbsorbirConfiguracio(GameManager altre)
    {
        if (altre == null)
        {
            return;
        }

        escenaMenu = altre.escenaMenu;
        escenaJoc = altre.escenaJoc;
        escenaGameOver = altre.escenaGameOver;

        nivellVictoria = altre.nivellVictoria;
        tempsTransicio = altre.tempsTransicio;
        tempsArmarDecisio = altre.tempsArmarDecisio;

        spawnPos = altre.spawnPos;
        spawnRotY = altre.spawnRotY;
        instruccionsPartida = altre.instruccionsPartida;

        if (altre.jugador != null) jugador = altre.jugador;
        if (altre.zonaInici != null) zonaInici = altre.zonaInici;
        if (altre.zonaFinal != null) zonaFinal = altre.zonaFinal;
        if (altre.loopController != null) loopController = altre.loopController;
        if (altre.anomalyManager != null) anomalyManager = altre.anomalyManager;
        if (altre.cartellNivellText != null) cartellNivellText = altre.cartellNivellText;
        if (altre.panellPausa != null) panellPausa = altre.panellPausa;
    }

    void OnDestroy()
    {
        if (Instancia == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        DesubscriureTriggers();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            return;
        }

        GuardarPartidaSiEstaJugant();
    }

    void OnApplicationQuit()
    {
        GuardarPartidaSiEstaJugant();
    }

    void Update()
    {
        if (estat == GameFlowState.Playing)
        {
            tempsPartida += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UI_ObrirMenuPausa();
            }
            return;
        }

        if (estat == GameFlowState.PauseMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            UI_ReprendreDesdePausa();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (scene.name == escenaJoc)
        {
            PrepararEscenaJoc();

            if (pendingResumeFromPause && teSnapshot)
            {
                pendingResumeFromPause = false;
                AplicarSnapshot();
            }
            else
            {
                IniciarNovaPartidaEnEscena();
            }
            return;
        }

        if (scene.name == escenaMenu)
        {
            estat = GameFlowState.Menu;
            ForcarCursorUI();
            NotificarUIStatus();
            return;
        }

        if (scene.name == escenaGameOver)
        {
            estat = GameFlowState.GameOver;
            ForcarCursorUI();
        }
    }

    void PrepararEscenaJoc()
    {
        jugador = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        loopController = UnityEngine.Object.FindFirstObjectByType<CorridorLoopController>();

        zonaInici = null;
        zonaFinal = null;

        if (loopController != null)
        {
            loopController.SetJugador(jugador != null ? jugador.transform : null);
            loopController.ReiniciarBucle();
            zonaInici = loopController.ZonaInici;
            zonaFinal = loopController.ZonaFinal;
        }

        if (zonaInici == null || zonaFinal == null)
        {
            ZonaTrigger[] zones = UnityEngine.Object.FindObjectsByType<ZonaTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < zones.Length; i++)
            {
                ZonaTrigger z = zones[i];
                if (z == null)
                {
                    continue;
                }

                string nom = z.gameObject.name.ToLowerInvariant();
                if (z.Tipus == ZonaTipus.Inici)
                {
                    if (zonaInici == null || nom.Contains("zonainici"))
                    {
                        zonaInici = z;
                    }
                }
                else
                {
                    if (zonaFinal == null || nom.Contains("zonafinal"))
                    {
                        zonaFinal = z;
                    }
                }
            }
        }

        anomalyManager = UnityEngine.Object.FindFirstObjectByType<AnomalyManager>();
        if (anomalyManager == null)
        {
            GameObject root = GameObject.Find("Anomalies");
            if (root == null)
            {
                root = new GameObject("Anomalies");
            }

            anomalyManager = root.GetComponent<AnomalyManager>();
            if (anomalyManager == null)
            {
                anomalyManager = root.AddComponent<AnomalyManager>();
            }
        }

        anomalyManager.RecarregarAnomalies();
        anomalyManager.DesactivarTot();

        AutoTrobarCartellNivell();
        AutoTrobarPanellPausa();
        MostrarPanellPausa(false);

        DesubscriureTriggers();
        SubscriureTriggers();
    }

    void SubscriureTriggers()
    {
        if (triggersSubscrits)
        {
            return;
        }

        if (zonaInici != null) zonaInici.OnJugadorEntra += OnJugadorEntraZona;
        if (zonaFinal != null) zonaFinal.OnJugadorEntra += OnJugadorEntraZona;
        triggersSubscrits = true;
    }

    void DesubscriureTriggers()
    {
        if (!triggersSubscrits)
        {
            return;
        }

        if (zonaInici != null) zonaInici.OnJugadorEntra -= OnJugadorEntraZona;
        if (zonaFinal != null) zonaFinal.OnJugadorEntra -= OnJugadorEntraZona;
        triggersSubscrits = false;
    }

    void IniciarNovaPartidaEnEscena()
    {
        estat = GameFlowState.Playing;
        enTransicio = false;
        decisioArmada = false;

        nivellActual = 0;
        tempsPartida = 0f;
        rondaTeAnomalia = false;
        ultimMissatgeRonda = "COMENCA";

        if (loopController != null)
        {
            loopController.SetJugador(jugador != null ? jugador.transform : null);
            loopController.ReiniciarBucle();
        }

        TeleportarASpawn();
        BloquejarJugador(false);
        PrepararRonda();
        MostrarMissatgeRonda("COMENCA");
        ArmarDecisio();
    }

    void AplicarSnapshot()
    {
        estat = GameFlowState.Playing;
        enTransicio = false;
        decisioArmada = false;

        nivellActual = Mathf.Max(0, snapshotNivell);
        tempsPartida = Mathf.Max(0f, snapshotTemps);
        rondaTeAnomalia = snapshotTeAnomalia;

        if (jugador != null)
        {
            jugador.TeletransportarA(snapshotPos, snapshotRotY, snapshotPitch);
        }

        if (anomalyManager != null)
        {
            anomalyManager.AplicarGuardat(snapshotTeAnomalia, snapshotTipus, snapshotIndex);
            rondaTeAnomalia = anomalyManager.TeAnomaliaActiva;
        }

        MostrarMissatgeRonda("REPRESA");
        BloquejarJugador(false);
        ArmarDecisio();
    }

    void OnJugadorEntraZona(ZonaTrigger zona)
    {
        if (estat != GameFlowState.Playing || zona == null)
        {
            return;
        }

        if (loopController != null)
        {
            loopController.MoureSegonsZona(zona.Tipus);
        }

        if (enTransicio || !decisioArmada)
        {
            return;
        }

        bool haTriatEnrere = zona.Tipus == ZonaTipus.Inici;
        bool correcte = rondaTeAnomalia ? haTriatEnrere : !haTriatEnrere;

        if (correcte)
        {
            nivellActual++;
            ultimMissatgeRonda = "CORRECTE";

            if (nivellActual > nivellVictoria)
            {
                FinalitzarPartida(true);
                return;
            }
        }
        else
        {
            nivellActual = 0;
            ultimMissatgeRonda = "T'HAS EQUIVOCAT";
        }

        StartCoroutine(CanviarRonda());
    }

    IEnumerator CanviarRonda()
    {
        enTransicio = true;
        decisioArmada = false;
        BloquejarJugador(true);

        if (tempsTransicio > 0f)
        {
            yield return new WaitForSeconds(tempsTransicio);
        }

        PrepararRonda();
        MostrarMissatgeRonda(ultimMissatgeRonda);
        BloquejarJugador(false);
        ArmarDecisio();
        enTransicio = false;
    }

    void PrepararRonda()
    {
        if (nivellActual <= 0)
        {
            if (anomalyManager != null)
            {
                anomalyManager.RecarregarAnomalies();
                anomalyManager.DesactivarTot();
            }

            rondaTeAnomalia = false;
            NotificarUIStatus();
            return;
        }

        if (anomalyManager != null)
        {
            anomalyManager.RecarregarAnomalies();
            anomalyManager.PrepararRonda(nivellActual, false);
            rondaTeAnomalia = anomalyManager.TeAnomaliaActiva;
        }
        else
        {
            rondaTeAnomalia = false;
        }

        NotificarUIStatus();
    }

    void ArmarDecisio()
    {
        if (coroutineArmarDecisio != null)
        {
            StopCoroutine(coroutineArmarDecisio);
        }

        coroutineArmarDecisio = StartCoroutine(CoroutineArmarDecisio());
    }

    IEnumerator CoroutineArmarDecisio()
    {
        decisioArmada = false;
        if (tempsArmarDecisio > 0f)
        {
            yield return new WaitForSeconds(tempsArmarDecisio);
        }

        decisioArmada = estat == GameFlowState.Playing && !enTransicio;
    }

    void TeleportarASpawn()
    {
        if (jugador == null)
        {
            return;
        }

        jugador.TeletransportarA(spawnPos, Quaternion.Euler(0f, spawnRotY, 0f));
    }

    void BloquejarJugador(bool bloquejat)
    {
        if (jugador == null)
        {
            return;
        }

        jugador.SetInputEnabled(!bloquejat, true);
    }

    void FinalitzarPartida(bool guanyada)
    {
        Time.timeScale = 1f;
        tempsUltimaPartida = Mathf.Max(0f, tempsPartida);
        estat = GameFlowState.GameOver;
        enTransicio = false;
        decisioArmada = false;
        pendingResumeFromPause = false;
        BloquejarJugador(true);
        MostrarPanellPausa(false);

        if (anomalyManager != null)
        {
            anomalyManager.DesactivarTot();
        }

        if (guanyada)
        {
            if (tempsRecord <= 0f || tempsUltimaPartida < tempsRecord)
            {
                tempsRecord = tempsUltimaPartida;
                GuardarTempsRecordPlayerPrefs();
            }

            teSnapshot = false;
            EsborrarGuardatPlayerPrefs();
        }

        NotificarUIStatus();

        if (!string.IsNullOrWhiteSpace(escenaGameOver))
        {
            SceneManager.LoadScene(escenaGameOver, LoadSceneMode.Single);
        }
    }

    void CapturarSnapshotPartida()
    {
        if (jugador == null)
        {
            return;
        }

        teSnapshot = true;
        snapshotPos = jugador.transform.position;
        snapshotRotY = jugador.GetYaw();
        snapshotPitch = jugador.GetPitch();
        snapshotNivell = nivellActual;
        snapshotTemps = tempsPartida;
        snapshotTeAnomalia = rondaTeAnomalia;
        snapshotTipus = anomalyManager != null ? anomalyManager.TipusAnomaliaActiva : AnomalyKind.None;
        snapshotIndex = anomalyManager != null ? anomalyManager.IndexAnomaliaActiva : -1;
    }

    void GuardarPartidaSiEstaJugant()
    {
        if (estat != GameFlowState.Playing)
        {
            return;
        }

        CapturarSnapshotPartida();
        GuardarGuardatPlayerPrefs();
    }

    void GuardarGuardatPlayerPrefs()
    {
        if (!teSnapshot)
        {
            return;
        }

        PlayerPrefs.SetInt(KeyHasSave, 1);
        PlayerPrefs.SetFloat(KeyPosX, snapshotPos.x);
        PlayerPrefs.SetFloat(KeyPosY, snapshotPos.y);
        PlayerPrefs.SetFloat(KeyPosZ, snapshotPos.z);
        PlayerPrefs.SetFloat(KeyRotY, snapshotRotY);
        PlayerPrefs.SetFloat(KeyPitch, snapshotPitch);
        PlayerPrefs.SetInt(KeyNivell, snapshotNivell);
        PlayerPrefs.SetFloat(KeyTemps, snapshotTemps);
        PlayerPrefs.SetInt(KeyTeAnomalia, snapshotTeAnomalia ? 1 : 0);
        PlayerPrefs.SetInt(KeyTipusAnomalia, (int)snapshotTipus);
        PlayerPrefs.SetInt(KeyIndexAnomalia, snapshotIndex);
        PlayerPrefs.Save();
    }

    bool CarregarGuardatPlayerPrefs()
    {
        if (PlayerPrefs.GetInt(KeyHasSave, 0) != 1)
        {
            return false;
        }

        teSnapshot = true;
        snapshotPos = new Vector3(
            PlayerPrefs.GetFloat(KeyPosX, spawnPos.x),
            PlayerPrefs.GetFloat(KeyPosY, spawnPos.y),
            PlayerPrefs.GetFloat(KeyPosZ, spawnPos.z));
        snapshotRotY = PlayerPrefs.GetFloat(KeyRotY, spawnRotY);
        snapshotPitch = PlayerPrefs.GetFloat(KeyPitch, 0f);
        snapshotNivell = Mathf.Max(0, PlayerPrefs.GetInt(KeyNivell, 0));
        snapshotTemps = Mathf.Max(0f, PlayerPrefs.GetFloat(KeyTemps, 0f));
        snapshotTeAnomalia = PlayerPrefs.GetInt(KeyTeAnomalia, 0) == 1;
        snapshotTipus = (AnomalyKind)PlayerPrefs.GetInt(KeyTipusAnomalia, (int)AnomalyKind.None);
        snapshotIndex = PlayerPrefs.GetInt(KeyIndexAnomalia, -1);
        return true;
    }

    void CarregarTempsRecordPlayerPrefs()
    {
        tempsRecord = PlayerPrefs.GetFloat(KeyTempsRecord, -1f);
        if (tempsRecord <= 0f)
        {
            tempsRecord = -1f;
        }
    }

    void GuardarTempsRecordPlayerPrefs()
    {
        if (tempsRecord <= 0f)
        {
            return;
        }

        PlayerPrefs.SetFloat(KeyTempsRecord, tempsRecord);
        PlayerPrefs.Save();
    }

    void EsborrarGuardatPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(KeyHasSave);
        PlayerPrefs.DeleteKey(KeyPosX);
        PlayerPrefs.DeleteKey(KeyPosY);
        PlayerPrefs.DeleteKey(KeyPosZ);
        PlayerPrefs.DeleteKey(KeyRotY);
        PlayerPrefs.DeleteKey(KeyPitch);
        PlayerPrefs.DeleteKey(KeyNivell);
        PlayerPrefs.DeleteKey(KeyTemps);
        PlayerPrefs.DeleteKey(KeyTeAnomalia);
        PlayerPrefs.DeleteKey(KeyTipusAnomalia);
        PlayerPrefs.DeleteKey(KeyIndexAnomalia);
        PlayerPrefs.Save();
    }

    void MostrarMissatgeRonda(string prefix)
    {
        ultimMissatgeRonda = string.IsNullOrWhiteSpace(prefix)
            ? "COMENCA"
            : prefix.ToUpperInvariant();
        NotificarUIStatus();
    }

    void NotificarUIStatus()
    {
        ActualitzarCartellNivell();
    }

    void AutoTrobarCartellNivell()
    {
        if (cartellNivellText != null)
        {
            return;
        }

        GameObject go = GameObject.Find("CartellNivell_Text");
        if (go != null)
        {
            cartellNivellText = go.GetComponent<TextMesh>();
        }
    }

    void AutoTrobarPanellPausa()
    {
        if (panellPausa != null)
        {
            return;
        }

        PauseOverlayController overlay = UnityEngine.Object.FindFirstObjectByType<PauseOverlayController>();
        if (overlay != null)
        {
            panellPausa = overlay.gameObject;
            return;
        }

        GameObject go = GameObject.Find("PausaOverlay");
        if (go != null)
        {
            panellPausa = go;
        }
    }

    void MostrarPanellPausa(bool visible)
    {
        if (panellPausa == null)
        {
            return;
        }

        if (panellPausa.activeSelf != visible)
        {
            panellPausa.SetActive(visible);
        }
    }

    void ActualitzarCartellNivell()
    {
        if (cartellNivellText == null)
        {
            return;
        }

        int nivell = Mathf.Max(0, nivellActual);
        string textNivellZero = "NIVELL 0.\nMEMORITZA ELS DETALLS!";
        string textRonda = string.IsNullOrWhiteSpace(instruccionsPartida)
            ? "SI HI HA ANOMALIA: TORNA ENRERE.\nSI NO N'HI HA: ENDAVANT."
            : instruccionsPartida;
        string textCartell = nivell <= 0
            ? textNivellZero
            : $"NIVELL {nivell}\n{textRonda}";

        cartellNivellText.color = Color.white;
        cartellNivellText.text = textCartell;
    }

    public float GetTempsUltimaPartida() => Mathf.Max(0f, tempsUltimaPartida);
    public float GetTempsRecord() => tempsRecord > 0f ? tempsRecord : -1f;

    public void UI_ComencarPartidaNova()
    {
        Time.timeScale = 1f;
        MostrarPanellPausa(false);
        UI_EsborrarGuardat();
        pendingResumeFromPause = false;
        SceneManager.LoadScene(escenaJoc, LoadSceneMode.Single);
    }

    public void UI_ContinuarPartida()
    {
        Time.timeScale = 1f;
        MostrarPanellPausa(false);

        if (!teSnapshot)
        {
            CarregarGuardatPlayerPrefs();
        }

        if (!teSnapshot)
        {
            return;
        }

        pendingResumeFromPause = true;
        SceneManager.LoadScene(escenaJoc, LoadSceneMode.Single);
    }

    public void UI_EsborrarGuardat()
    {
        teSnapshot = false;
        pendingResumeFromPause = false;
        EsborrarGuardatPlayerPrefs();
    }

    public void UI_ObrirMenuPausa()
    {
        if (estat != GameFlowState.Playing)
        {
            return;
        }

        AutoTrobarPanellPausa();
        estat = GameFlowState.PauseMenu;
        Time.timeScale = 0f;
        BloquejarJugador(true);
        MostrarPanellPausa(true);
    }

    public void UI_ReprendreDesdePausa()
    {
        if (estat != GameFlowState.PauseMenu)
        {
            return;
        }

        Time.timeScale = 1f;
        estat = GameFlowState.Playing;
        MostrarPanellPausa(false);
        BloquejarJugador(false);
        NotificarUIStatus();
    }

    public void UI_GuardarISortirMenu()
    {
        if (jugador != null)
        {
            CapturarSnapshotPartida();
            GuardarGuardatPlayerPrefs();
        }

        Time.timeScale = 1f;
        estat = GameFlowState.Menu;
        MostrarPanellPausa(false);
        BloquejarJugador(true);
        ForcarCursorUI();
        pendingResumeFromPause = false;
        SceneManager.LoadScene(escenaMenu, LoadSceneMode.Single);
    }

    public void UI_TornarMenuDesDeGameOver()
    {
        Time.timeScale = 1f;
        ForcarCursorUI();
        SceneManager.LoadScene(escenaMenu, LoadSceneMode.Single);
    }

    public void UI_SortirAplicacio()
    {
        GuardarPartidaSiEstaJugant();
        Application.Quit();
    }

    public bool TePartidaGuardada()
    {
        return teSnapshot || PlayerPrefs.GetInt(KeyHasSave, 0) == 1;
    }

    void ForcarCursorUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
