using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textMissatge;
    public TMP_Text textTemps;
    public TMP_Text textRecord;
    public Button botoTornarMenu;

    [Header("Textos")]
    [TextArea(2, 4)]
    public string missatgeVictoria = "Has arribat pels pels a classe!";

    [Header("So final (opcional)")]
    public AudioClip soVictoria;
    public AudioSource audioVictoria;
    [Range(0f, 1f)] public float volumSoVictoria = 0.8f;
    public bool reproduirSoAlEntrar = true;

    void OnEnable()
    {
        ActualitzarUI();
        ConfigurarBoto();
        ReproduirSoFinal();
    }

    void ConfigurarBoto()
    {
        if (botoTornarMenu == null)
        {
            return;
        }

        botoTornarMenu.onClick.RemoveAllListeners();
        botoTornarMenu.onClick.AddListener(OnClickTornarMenu);
    }

    void ActualitzarUI()
    {
        GameManager gm = GameManager.Instancia;
        float tempsFinal = gm != null ? gm.GetTempsUltimaPartida() : 0f;
        float tempsRecord = gm != null ? gm.GetTempsRecord() : -1f;

        if (textMissatge != null)
        {
            textMissatge.text = missatgeVictoria;
        }

        if (textTemps != null)
        {
            textTemps.text = $"Temps d'aquesta partida: {FormatTemps(tempsFinal)}";
        }

        if (textRecord != null)
        {
            if (tempsRecord > 0f)
            {
                textRecord.text = $"Record de temps: {FormatTemps(tempsRecord)}";
            }
            else
            {
                textRecord.text = "Record de temps: --:--.--";
            }
        }
    }

    string FormatTemps(float segonsTotals)
    {
        int minuts = Mathf.FloorToInt(segonsTotals / 60f);
        float segons = segonsTotals - minuts * 60f;
        return $"{minuts:00}:{segons:00.00}";
    }

    void ReproduirSoFinal()
    {
        if (!reproduirSoAlEntrar || soVictoria == null)
        {
            return;
        }

        if (audioVictoria == null)
        {
            audioVictoria = GetComponent<AudioSource>();
        }

        if (audioVictoria == null)
        {
            audioVictoria = gameObject.AddComponent<AudioSource>();
        }

        audioVictoria.playOnAwake = false;
        audioVictoria.loop = false;
        audioVictoria.spatialBlend = 0f;
        audioVictoria.clip = soVictoria;
        audioVictoria.volume = volumSoVictoria;
        audioVictoria.Stop();
        audioVictoria.Play();
    }

    public void OnClickTornarMenu()
    {
        if (GameManager.Instancia == null)
        {
            return;
        }

        GameManager.Instancia.UI_TornarMenuDesDeGameOver();
    }
}
