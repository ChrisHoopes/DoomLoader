using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// All switch linedefs use this controller with lambda functions on activation
/// </summary>
public class SwitchLinedefController : MonoBehaviour, Pokeable
{
    AudioSource audioSource;
    public string CurrentTexture = "";

    public bool activated = false;
    public UnityEvent OnActivate = new UnityEvent();

    public bool Repeatable = false;
    public bool AutoReturn = false;
    public float AutoReturnTime = 1f;

    //requirement to be allowed to activate the switch
    public Func<bool> Prereq = new Func<bool>(() => { return true; });

    public bool Poke(GameObject caller)
    {
        if (!Prereq())
            return false;

        if (!Repeatable)
            if (activated)
                return false;

        if (AutoReturn)
            time = AutoReturnTime;

        activated = !activated;

        TextureLoader.Instance.SwapSwitchTexture(GetComponent<MeshRenderer>());

        //only actual switches make sound
        if (CurrentTexture.Substring(0, 2) == "SW")
            audioSource.Play();

        OnActivate.Invoke();
        return true;
    }

    private void Start()
    {
        //"invisible" switch
        if (CurrentTexture.Substring(0, 2) != "SW")
            return;

        GameObject audioPosition = new GameObject("Audio Position");
        audioPosition.transform.position = GetComponent<MeshFilter>().mesh.bounds.center;
        audioPosition.transform.SetParent(transform, true);
        audioSource = audioPosition.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.clip = SoundLoader.Instance.LoadSound("DSSWTCHN");

        if (!AutoReturn)
            enabled = false;
    }

    public float time = 0f;
    private void Update()
    {
        if (GameManager.Paused)
            return;

        if (time <= 0)
            return;
        else
        {
            time -= Time.deltaTime;
            if (time <= 0)
            {
                activated = !activated;
                TextureLoader.Instance.SwapSwitchTexture(GetComponent<MeshRenderer>());
            }
        }
    }

    public bool AllowMonsters()
    {
        return false;
    }
}
