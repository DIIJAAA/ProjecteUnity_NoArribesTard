using UnityEngine;
using UnityEngine.UI;

public class PauseOverlayController : MonoBehaviour
{
    [Header("Botons")]
    public Button botoReprendre;
    public Button botoGuardarISortir;

    void OnEnable()
    {
        if (botoReprendre != null)
        {
            botoReprendre.onClick.RemoveAllListeners();
            botoReprendre.onClick.AddListener(OnClickReprendre);
        }

        if (botoGuardarISortir != null)
        {
            botoGuardarISortir.onClick.RemoveAllListeners();
            botoGuardarISortir.onClick.AddListener(OnClickGuardarISortir);
        }
    }

    public void OnClickReprendre()
    {
        if (GameManager.Instancia == null)
        {
            return;
        }

        GameManager.Instancia.UI_ReprendreDesdePausa();
    }

    public void OnClickGuardarISortir()
    {
        if (GameManager.Instancia == null)
        {
            return;
        }

        GameManager.Instancia.UI_GuardarISortirMenu();
    }
}
