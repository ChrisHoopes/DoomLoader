using UnityEngine;

/// <summary>
/// Used by linedefs 36, 70, 71 and 98
/// </summary>
public class Floor36SectorController : MonoBehaviour 
{
    public Sector targetSector;
    AudioSource audioSource;

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

    public void Init(Sector sector)
    {
        targetSector = sector;
        targetHeight = currentHeight = originalHeight = sector.floorHeight;

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

        foreach (Sidedef s in sector.Sidedefs)
        {
            if (s.Line.Back == null)
                continue;

            if (s.Line.Front.Sector == sector)
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

            if (s.Line.Back.Sector == sector)
            {
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

                if (targetHeight == sector.floorHeight)
                    targetHeight = s.Line.Front.Sector.floorHeight + MapLoader._8units;

                if (s.Line.Front.Sector.floorHeight + MapLoader._8units > targetHeight)
                    targetHeight = s.Line.Front.Sector.floorHeight + MapLoader._8units;
            }
        }
    }

    public float speed = 2;

    public float originalHeight;
    public float targetHeight;
    public float currentHeight;

    public enum State
    {
        AtTop,
        AtBottom,
        Lowering,
    }

    private State _currentState = State.AtTop;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Lowering)
            {
                currentHeight = targetSector.floorHeight; //E1M7 has a column with floor 36 and 5

                audioSource.clip = SoundLoader.Instance.LoadSound("DSSTNMOV");
                audioSource.Play();

                enabled = true;
            }
            else if (value == State.AtTop || value == State.AtBottom)
            {
                audioSource.Stop();
                enabled = false;
            }

            _currentState = value;
        }
    }

    void Update()
    {
        if (GameManager.Paused)
            return;

        switch (CurrentState)
        {
            default:
            case State.AtTop:
            case State.AtBottom:
                break;

            case State.Lowering:
                currentHeight -= Time.deltaTime * speed;
                if (currentHeight < targetHeight)
                {
                    currentHeight = targetHeight;
                    CurrentState = State.AtBottom;
                }
                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.floorHeight = currentHeight;
                break;
        }
    }
}
