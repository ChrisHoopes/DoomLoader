using UnityEngine;

/// <summary>
/// Common door used just about everywhere
/// </summary>
public class SlowRepeatableDoorController : MonoBehaviour
{
    public Sector targetSector;
    public AudioSource audioSource;
    public SectorController sectorController;

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
        sectorController = sector.floorObject;

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
        Opening,
        Closing
    }

    private State _currentState = State.Closed;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Opening)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSDOROPN");
                audioSource.Play();

                enabled = true;
            }
            else if (value == State.Closing)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSDORCLS");
                audioSource.Play();

                enabled = true;
            }
            else if (value == State.Closed)
                enabled = false;

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

            case State.Open:
                waitTime -= Time.deltaTime;
                if (waitTime <= 0)
                    CurrentState = State.Closing;
                break;

            case State.Closing:
                foreach (ThingController tc in sectorController.DynamicThings)
                    if (tc.thingType == ThingController.ThingType.Monster)
                    {
                        CurrentState = State.Opening;
                        waitTime = 4f;
                    }

                currentHeight -= Time.deltaTime * speed;
                if (currentHeight < originalHeight)
                {
                    currentHeight = originalHeight;
                    CurrentState = State.Closed;
                }
                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.ceilingHeight = currentHeight;
                break;
        }
    }
}
