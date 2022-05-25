using UnityEngine;

/// <summary>
/// Used by linedefs 18 and 20
/// </summary>
public class Floor20SectorController : MonoBehaviour
{
    public Sector targetSector;
    public Sector modelSector;
    AudioSource audioSource;

    public float speed = .25f;

    public float originalHeight;
    public float currentHeight;
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

    public void Init(Sector sector)
    {
        targetSector = sector;
        currentHeight = originalHeight = targetSector.floorHeight;
        targetHeight = float.MaxValue;

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

        foreach (Sidedef s in targetSector.Sidedefs)
        {
            if (s.Line.Back == null)
                continue;

            if (s.Line.Back.Sector == s.Line.Front.Sector)
                continue;

            if (s.Line.Front.Sector == targetSector)
            {
                if (s.Line.Back.Sector.floorHeight > originalHeight && s.Line.Back.Sector.floorHeight < targetHeight)
                {
                    modelSector = s.Line.Back.Sector;
                    targetHeight = modelSector.floorHeight;
                }

                if (s.Line.BotFrontObject != null)
                    if (s.Line.Back.Sector.floorHeight < targetSector.maximumFloorHeight)
                    {
                        s.Line.BotFrontObject.transform.SetParent(sector.floorObject.transform);

                        Rigidbody rb = s.Line.BotFrontObject.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = s.Line.BotFrontObject.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                    }
            }

            if (s.Line.Back.Sector == targetSector)
            {
                if (s.Line.Front.Sector.floorHeight > originalHeight && s.Line.Front.Sector.floorHeight < targetHeight)
                {
                    modelSector = s.Line.Front.Sector;
                    targetHeight = modelSector.floorHeight;
                }

                if (s.Line.BotBackObject != null)
                    if (s.Line.Front.Sector.floorHeight < targetSector.maximumFloorHeight)
                    {
                        s.Line.BotBackObject.transform.SetParent(sector.floorObject.transform);

                        Rigidbody rb = s.Line.BotBackObject.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = s.Line.BotBackObject.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            rb.useGravity = false;
                        }
                    }
            }
        }

        if (modelSector == null)
        {
            Debug.LogError("Floor20SectorController: Init: No model sector found!");
            return;
        }
    }

    public enum State
    {
        AtBottom,
        Rising,
        AtTop
    }

    private State _currentState = State.AtBottom;
    public State CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == State.Rising)
            {
                audioSource.clip = SoundLoader.Instance.LoadSound("DSSTNMOV");
                audioSource.Play();

                TextureAnimation floorAnim = targetSector.floorObject.GetComponent<TextureAnimation>();
                if (floorAnim != null)
                    floorAnim.enabled = false;

                targetSector.floorTexture = modelSector.floorTexture;
                MeshRenderer mr = targetSector.floorObject.GetComponent<MeshRenderer>();
                MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
                mr.GetPropertyBlock(materialProperties);
                materialProperties.SetTexture("_MainTex", TextureLoader.Instance.GetFlatTexture(modelSector.floorTexture));
                targetSector.specialType = modelSector.specialType;
                mr.SetPropertyBlock(materialProperties);

                enabled = true;
            }
            else if (value == State.AtTop)
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

            case State.Rising:
                currentHeight += Time.deltaTime * speed;
                if (currentHeight > targetHeight)
                {
                    currentHeight = targetHeight;
                    CurrentState = State.AtTop;
                }

                transform.position = new Vector3(transform.position.x, currentHeight - originalHeight, transform.position.z);
                targetSector.floorHeight = currentHeight;

                break;
        }
    }
}
