using UnityEngine;
using System.Collections.Generic;

public class StairbuilderSlow : MonoBehaviour 
{
    List<Sector> targetSectors = new List<Sector>();

    public void Init(List<Sector> TargetSectors)
    {
        targetSectors = TargetSectors;

        foreach(Sector sector in TargetSectors)
        {
            GameObject audioPosition = new GameObject("Audio Position");
            audioPosition.transform.position = sector.floorObject.GetComponent<MeshFilter>().mesh.bounds.center;
            audioPosition.transform.SetParent(sector.floorObject.transform.transform, true);
            AudioSource audioSource = audioPosition.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.clip = SoundLoader.Instance.LoadSound("DSSTNMOV");
            audioSource.loop = true;
            audioSource.volume = .2f;

            //the first step
            {
                Rigidbody rb = sector.floorObject.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = sector.floorObject.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }

            foreach(Sidedef s in sector.Sidedefs)
            {
                if (s.Line.Back == null)
                    continue;

                if (s.Line.Back.Sector != sector)
                    continue;
                
                if (s.Line.BotFrontObject != null)
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

                if (s.Line.BotBackObject != null)
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

        enabled = false;
    }

    public float speed = .2f;

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
                foreach (Sector sector in targetSectors)
                    sector.floorObject.GetComponentInChildren<AudioSource>().Play();

                enabled = true;
            }
            else
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
            case State.Waiting:
            case State.Done:
                break;

            case State.Active:
                bool failed = true;
                foreach(Sector sector in targetSectors)
                {
                    if (sector.floorHeight < sector.maximumFloorHeight)
                        failed = false;
                    else
                        continue;

                    sector.floorHeight += Time.deltaTime * speed;

                    if (sector.floorHeight >= sector.maximumFloorHeight)
                    {
                        sector.floorHeight = sector.maximumFloorHeight;
                        sector.floorObject.GetComponentInChildren<AudioSource>().Stop();
                    }

                    sector.floorObject.transform.position = new Vector3(transform.position.x, sector.floorHeight - sector.minimumFloorHeight, transform.position.z);
                }

                if (failed)
                    CurrentState = State.Done;

                break;
        }
    }
}