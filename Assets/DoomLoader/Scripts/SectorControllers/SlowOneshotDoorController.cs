using UnityEngine;

/// <summary>
/// Used by linedefs 2, 31, 46, 61, 86 and 103
/// </summary>
public class SlowOneshotDoorController : MonoBehaviour
{
    public Sector targetSector;
    public AudioSource audioSource;

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
        targetHeight = currentHeight = originalHeight = sector.ceilingHeight;

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
            if (s.Line.Back != null)
            {
                if (s.Line.Back.Sector == sector)
                {
                    if (s.Line.TopFrontObject != null)
                    {
                        s.Line.TopFrontObject.transform.SetParent(transform);

                        Rigidbody rb = s.Line.TopFrontObject.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = s.Line.TopFrontObject.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                    }

                    if (targetHeight == originalHeight)
                        targetHeight = s.Line.Front.Sector.ceilingHeight - MapLoader._4units;

                    if (s.Line.Front.Sector.ceilingHeight - MapLoader._4units < targetHeight)
                        targetHeight = s.Line.Front.Sector.ceilingHeight - MapLoader._4units;
                }

                if (s.Line.Front.Sector == sector)
                {
                    if (s.Line.TopBackObject != null)
                    {
                        s.Line.TopBackObject.transform.SetParent(transform);

                        Rigidbody rb = s.Line.TopBackObject.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = s.Line.TopBackObject.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                    }

                    if (targetHeight == originalHeight)
                        targetHeight = s.Line.Back.Sector.ceilingHeight - MapLoader._4units;

                    if (s.Line.Back.Sector.ceilingHeight - MapLoader._4units < targetHeight)
                        targetHeight = s.Line.Back.Sector.ceilingHeight - MapLoader._4units;
                }
            }
    }

    public float speed = 2;

    public float originalHeight;
    public float targetHeight;
    public float currentHeight;

    public enum State
    {
        Closed,
        Open,
        Opening
    }

    private State _currentState = State.Closed;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Opening)
            {
                //these is to fix the double door behavior in E1M4 center column
                currentHeight = targetSector.ceilingHeight;
                if (currentHeight == targetHeight)
                {
                    _currentState = State.Open;
                    return;
                }

                if (currentHeight == originalHeight)
                {
                    audioSource.clip = SoundLoader.Instance.LoadSound("DSDOROPN");
                    audioSource.Play();
                }

                enabled = true;
            }
            else if (value == State.Open)
                enabled = false;

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
            case State.Open:
            case State.Closed:
                break;

            case State.Opening:
                currentHeight += Time.deltaTime * speed;
                if (currentHeight > targetHeight)
                {
                    currentHeight = targetHeight;
                    CurrentState = State.Open;
                }
                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.ceilingHeight = currentHeight;
                break;
        }
    }
}