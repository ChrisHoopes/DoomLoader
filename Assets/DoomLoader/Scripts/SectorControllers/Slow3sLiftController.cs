using UnityEngine;

/// <summary>
/// Used by linedefs 10, 21, 62 and 88
/// </summary>
public class Slow3sLiftController : MonoBehaviour 
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
        }

        foreach (Sidedef s in sector.Sidedefs)
        {
            if (s.Line.Back == null)
                continue;

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

                if (s.Line.Front.Sector.floorHeight < targetHeight)
                    targetHeight = s.Line.Front.Sector.floorHeight;
            }

            if (s.Line.Front.Sector == sector)
            {
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

                if (s.Line.Back.Sector.floorHeight < targetHeight)
                    targetHeight = s.Line.Back.Sector.floorHeight;
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
        Rising
    }

    private State _currentState = State.AtTop;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Lowering || value == State.Rising)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSPSTART");
                audioSource.Play();

                enabled = true;
            }
            else if (value == State.AtTop || value == State.AtBottom)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSPSTOP");
                audioSource.Play();

                if (value == State.AtTop)
                    enabled = false;
            }

            _currentState = value;
        }
    }
    public float waitTime;

    void Update()
    {
        if (GameManager.Paused)
            return;

        switch (CurrentState)
        {
            default:
            case State.AtTop:
                break;

            case State.Lowering:
                currentHeight -= Time.deltaTime * speed;
                if (currentHeight < targetHeight)
                {
                    currentHeight = targetHeight;
                    CurrentState = State.AtBottom;
                    waitTime = 3f;
                }
                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.floorHeight = currentHeight;
                break;

            case State.AtBottom:
                waitTime -= Time.deltaTime;
                if (waitTime <= 0)
                    CurrentState = State.Rising;
                break;

            case State.Rising:
                currentHeight += Time.deltaTime * speed;
                if (currentHeight > originalHeight)
                {
                    currentHeight = originalHeight;
                    CurrentState = State.AtTop;
                }
                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.floorHeight = currentHeight;
                break;
        }
    }
}
