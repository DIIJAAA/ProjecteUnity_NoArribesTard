using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Moviment")]
    public float velocitatCaminar = 4.8f;
    public float velocitatCorrer = 7.2f;
    public float gravetat = -18f;

    [Header("Camera")]
    public Camera camara;
    public float sensibilitatMouse = 2.2f;
    public float limitMirarAmunt = -80f;
    public float limitMirarAvall = 80f;

    [Header("Cursor")]
    public bool bloquejarCursorQuanActiu = true;

    [Header("So de passos")]
    public AudioSource audioPassos;
    public AudioClip[] clipsPassos;
    [Range(0f, 1f)] public float volumPassos = 0.35f;
    [Min(0.05f)] public float intervalPassosCaminar = 0.52f;
    [Min(0.05f)] public float intervalPassosCorrer = 0.36f;

    CharacterController controller;
    Vector3 velocitatInterna;
    float rotacioVertical;
    bool inputActiu = true;
    float timerPassos = 0f;

    public bool InputActiu => inputActiu;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (camara == null)
        {
            camara = GetComponentInChildren<Camera>();
        }

        if (camara == null)
        {
            GameObject camObj = new GameObject("Camara");
            camObj.transform.SetParent(transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camara = camObj.AddComponent<Camera>();
        }

        if (camara != null && camara.GetComponent<AudioListener>() == null)
        {
            camara.gameObject.AddComponent<AudioListener>();
        }

        if (audioPassos == null)
        {
            audioPassos = GetComponent<AudioSource>();
        }
        if (audioPassos == null)
        {
            audioPassos = gameObject.AddComponent<AudioSource>();
        }
        audioPassos.playOnAwake = false;
        audioPassos.spatialBlend = 0.75f;
        audioPassos.minDistance = 1f;
        audioPassos.maxDistance = 12f;
    }

    void Start()
    {
        SetInputEnabled(true);
    }

    void Update()
    {
        if (!inputActiu)
        {
            return;
        }

        GestionarMoviment();
        GestionarCamera();
        GestionarCursorClick();
    }

    void GestionarMoviment()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 direccio = (transform.right * inputX + transform.forward * inputZ).normalized;
        bool estaCorrent = Input.GetKey(KeyCode.LeftShift);
        float velocitatObjectiu = estaCorrent ? velocitatCorrer : velocitatCaminar;
        Vector3 velocitatPlana = direccio * velocitatObjectiu;
        bool teInputMoviment = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputZ) > 0.01f;

        if (controller.isGrounded && velocitatInterna.y < 0f)
        {
            velocitatInterna.y = -2f;
        }

        velocitatInterna.x = velocitatPlana.x;
        velocitatInterna.z = velocitatPlana.z;
        velocitatInterna.y += gravetat * Time.deltaTime;

        controller.Move(velocitatInterna * Time.deltaTime);
        GestionarSoPassos(teInputMoviment, estaCorrent);
    }

    void GestionarCamera()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * sensibilitatMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilitatMouse;

        transform.Rotate(Vector3.up * mouseX);

        rotacioVertical -= mouseY;
        rotacioVertical = Mathf.Clamp(rotacioVertical, limitMirarAmunt, limitMirarAvall);
        camara.transform.localRotation = Quaternion.Euler(rotacioVertical, 0f, 0f);
    }

    void GestionarCursorClick()
    {
        if (!bloquejarCursorQuanActiu)
        {
            return;
        }

        if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void GestionarSoPassos(bool teInputMoviment, bool estaCorrent)
    {
        if (audioPassos == null || clipsPassos == null || clipsPassos.Length == 0)
        {
            return;
        }

        if (!controller.isGrounded || !teInputMoviment)
        {
            timerPassos = 0f;
            return;
        }

        timerPassos -= Time.deltaTime;
        if (timerPassos > 0f)
        {
            return;
        }

        int idx = Random.Range(0, clipsPassos.Length);
        AudioClip clip = clipsPassos[idx];
        if (clip != null)
        {
            audioPassos.pitch = Random.Range(0.95f, 1.05f);
            audioPassos.PlayOneShot(clip, volumPassos);
        }

        timerPassos = estaCorrent ? intervalPassosCorrer : intervalPassosCaminar;
    }

    public void SetInputEnabled(bool enabled, bool gestionarCursor = true)
    {
        inputActiu = enabled;
        if (!enabled)
        {
            velocitatInterna = Vector3.zero;
            timerPassos = 0f;
        }

        if (!gestionarCursor)
        {
            return;
        }

        if (enabled && bloquejarCursorQuanActiu)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TeletransportarA(Vector3 novaPosicio)
    {
        TeletransportarA(novaPosicio, transform.rotation);
    }

    public void TeletransportarA(Vector3 novaPosicio, float rotacioY, float pitchCamera)
    {
        AplicarTeleport(novaPosicio, rotacioY, pitchCamera);
    }

    public void TeletransportarA(Vector3 novaPosicio, Quaternion novaRotacio)
    {
        float rotacioY = novaRotacio.eulerAngles.y;
        float pitchCamera = AngleSigned(novaRotacio.eulerAngles.x);
        AplicarTeleport(novaPosicio, rotacioY, pitchCamera);
    }

    void AplicarTeleport(Vector3 novaPosicio, float rotacioY, float pitchCamera)
    {
        bool ccActiu = controller != null && controller.enabled;
        if (ccActiu)
        {
            controller.enabled = false;
        }

        transform.SetPositionAndRotation(novaPosicio, Quaternion.Euler(0f, rotacioY, 0f));
        rotacioVertical = Mathf.Clamp(pitchCamera, limitMirarAmunt, limitMirarAvall);
        if (camara != null)
        {
            camara.transform.localRotation = Quaternion.Euler(rotacioVertical, 0f, 0f);
        }

        velocitatInterna = Vector3.zero;

        if (ccActiu)
        {
            controller.enabled = true;
        }
    }

    public float GetYaw()
    {
        return transform.eulerAngles.y;
    }

    public float GetPitch()
    {
        return rotacioVertical;
    }

    public bool EstaEnMoviment()
    {
        Vector3 pla = new Vector3(velocitatInterna.x, 0f, velocitatInterna.z);
        return pla.sqrMagnitude > 0.01f;
    }

    static float AngleSigned(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
