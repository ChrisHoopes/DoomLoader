using UnityEngine;

//this will be placed on the donut pillar
public class Donut9SectorController : MonoBehaviour
{
    public Sector pillarSector;
    public Sector ringSector;
    public Sector modelSector;
    AudioSource audioSource;

    public float speed = .25f;

    public float pOriginalHeight;
    public float pCurrentHeight;

    public float rOriginalHeight;
    public float rCurrentHeight;

    public float targetHeight;

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        enabled = false;
    }

    public void Init(Sector PillarSector, Sector RingSector)
    {
        pillarSector = PillarSector;
        ringSector = RingSector;
        pCurrentHeight = pOriginalHeight = pillarSector.floorHeight;

        audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null)
        {
            GameObject audioPosition = new GameObject("Audio Position");
            audioPosition.transform.position = GetComponent<MeshFilter>().mesh.bounds.center;
            audioPosition.transform.SetParent(transform, true);
            audioSource = audioPosition.AddComponent<AudioSource>();
            //audioSource.minDistance = 5f;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.loop = true;
        }

        Rigidbody ringrb = RingSector.floorObject.GetComponent<Rigidbody>();

        if (ringrb == null)
        {
            ringrb = RingSector.floorObject.gameObject.AddComponent<Rigidbody>();
            ringrb.isKinematic = true;
            ringrb.useGravity = false;
        }


        foreach (Sidedef s in pillarSector.Sidedefs)
        {
            if (s.Line.Back == null)
                continue;

            if (s.Line.Front.Sector == pillarSector)
                if (s.Line.BotBackObject != null)
                {
                    s.Line.BotBackObject.transform.SetParent(transform);

                    Rigidbody rb = s.Line.BotBackObject.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = s.Line.BotBackObject.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                }

            if (s.Line.Back.Sector == pillarSector)
                if (s.Line.BotFrontObject != null)
                {
                    s.Line.BotFrontObject.transform.SetParent(transform);

                    Rigidbody rb = s.Line.BotFrontObject.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = s.Line.BotFrontObject.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                }
        }

        rCurrentHeight = rOriginalHeight = ringSector.floorHeight;

        foreach(Sidedef s in ringSector.Sidedefs)
        {
            if (s.Line.Back == null)
                continue;

            if (s.Line.Front.Sector == ringSector && s.Line.Back.Sector != pillarSector)
            {
                modelSector = s.Line.Back.Sector;
                break;
            }

            if (s.Line.Back.Sector == ringSector && s.Line.Front.Sector != pillarSector)
            {
                modelSector = s.Line.Front.Sector;
                break;
            }
        }

        if (modelSector == null)
        {
            Debug.LogError("Donut9SectorController: Init: No model sector found!");
            return;
        }

        targetHeight = modelSector.floorHeight;
    }

    public enum State
    {
        Waiting,
        Active,
        Done
    }

    private State _currentState = State.Waiting;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Active)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSSTNMOV");
                audioSource.Play();
                enabled = true;
            }
            else if (value == State.Done)
            {
                audioSource.Stop();
                enabled = false;
            }

            _currentState = value;
        }
    }

    bool pillarDone = false;
    bool ringDone = false;

    void Update()
    {
        if (GameManager.Paused)
            return;

        switch (CurrentState)
        {
            default:
            case State.Waiting:
            case State.Done:
                break;

            case State.Active:
                if (!pillarDone)
                    pCurrentHeight -= Time.deltaTime * speed;
                if (!ringDone)
                    rCurrentHeight += Time.deltaTime * speed;

                if (pCurrentHeight <= targetHeight)
                {
                    pCurrentHeight = targetHeight;
                    pillarDone = true;
                }

                if (rCurrentHeight > targetHeight)
                {
                    rCurrentHeight = targetHeight;
                    ringDone = true;

                    TextureAnimation floorAnim = ringSector.floorObject.GetComponent<TextureAnimation>();
                    if (floorAnim != null)
                        floorAnim.enabled = false;

                    ringSector.floorTexture = modelSector.floorTexture;
                    MeshRenderer mr = ringSector.floorObject.GetComponent<MeshRenderer>();
                    MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
                    mr.GetPropertyBlock(materialProperties);
                    materialProperties.SetTexture("_MainTex", TextureLoader.Instance.GetFlatTexture(modelSector.floorTexture));
                    ringSector.specialType = modelSector.specialType;
                    mr.SetPropertyBlock(materialProperties);
                }

                if (pillarDone && ringDone)
                    CurrentState = State.Done;

                transform.position = new Vector3(transform.position.x, pCurrentHeight - pOriginalHeight, transform.position.z);
                pillarSector.floorHeight = pCurrentHeight;

                ringSector.floorObject.transform.position = new Vector3(transform.position.x, rCurrentHeight - rOriginalHeight, transform.position.z);
                ringSector.floorHeight = rCurrentHeight;
                break;
        }
    }
}
