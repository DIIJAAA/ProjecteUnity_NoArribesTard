using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Botons")]
    public Button botoContinuar;

    [Header("Audio (opcional)")]
    public AudioClip clipMusica;
    public AudioClip clipAmbient;
    [Range(0f, 1f)] public float volumMusica = 0.5f;
    [Range(0f, 1f)] public float volumAmbient = 0.35f;
    public bool reproduirAudiosAlInici = true;

    AudioSource audioMusica;
    AudioSource audioAmbient;

    void OnEnable()
    {
        ActualitzarEstatBotoContinuar();
        ConfigurarAudioMenu();
    }

    public void ActualitzarEstatBotoContinuar()
    {
        if (botoContinuar == null)
        {
            return;
        }

        bool teSave = GameManager.Instancia != null && GameManager.Instancia.TePartidaGuardada();
        botoContinuar.interactable = teSave;
    }

    public void OnClickComencar()
    {
        if (GameManager.Instancia == null)
        {
            return;
        }

        GameManager.Instancia.UI_ComencarPartidaNova();
    }

    public void OnClickContinuar()
    {
        if (GameManager.Instancia == null || !GameManager.Instancia.TePartidaGuardada())
        {
            ActualitzarEstatBotoContinuar();
            return;
        }

        GameManager.Instancia.UI_ContinuarPartida();
    }

    public void OnClickSortir()
    {
        if (GameManager.Instancia == null)
        {
            Application.Quit();
            return;
        }

        GameManager.Instancia.UI_SortirAplicacio();
    }

    void ConfigurarAudioMenu()
    {
        if (!reproduirAudiosAlInici)
        {
            return;
        }

        PrepararAudioSources();

        if (audioMusica != null && clipMusica != null)
        {
            audioMusica.clip = clipMusica;
            audioMusica.volume = volumMusica;
            audioMusica.spatialBlend = 0f;
            audioMusica.playOnAwake = false;
            audioMusica.loop = true;
            if (!audioMusica.isPlaying)
            {
                audioMusica.Play();
            }
        }

        if (audioAmbient != null && clipAmbient != null)
        {
            audioAmbient.clip = clipAmbient;
            audioAmbient.volume = volumAmbient;
            audioAmbient.spatialBlend = 0f;
            audioAmbient.playOnAwake = false;
            audioAmbient.loop = true;
            if (!audioAmbient.isPlaying)
            {
                audioAmbient.Play();
            }
        }
    }

    void PrepararAudioSources()
    {
        AudioSource[] fonts = GetComponents<AudioSource>();

        if (fonts.Length == 0)
        {
            audioMusica = gameObject.AddComponent<AudioSource>();
            audioAmbient = gameObject.AddComponent<AudioSource>();
            return;
        }

        audioMusica = fonts[0];
        if (fonts.Length > 1)
        {
            audioAmbient = fonts[1];
        }
        else
        {
            audioAmbient = gameObject.AddComponent<AudioSource>();
        }
    }
}
