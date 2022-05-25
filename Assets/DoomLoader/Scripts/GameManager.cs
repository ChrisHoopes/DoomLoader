using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string autoloadWad = "Doom1.WAD";
    public string autoloadMap = "E1M1";

    public static GameManager Instance;

    public float gravity = 30f;
    public float terminalVelocity = 100f;

    public const float maxStairHeight = MapLoader._24units;

    public bool deathmatch;

    public static bool Paused { get { return Instance.paused; } }

    public bool paused = true;

    public PlayerThing[] Player = new PlayerThing[1];
    public GameObject BulletPuff;
    public GameObject BloodDrop;
    public GameObject TeleportEffect;

    public Transform TemporaryObjectsHolder;

    public float PlayerDamageReceive = .5f;

    void Awake()
    {
        Instance = this;
    }

    void Start ()
    {
        if (!string.IsNullOrEmpty(autoloadWad))
            if (!WadLoader.LoadWad(autoloadWad))
                return;

        TextureLoader.Instance.LoadAndBuildAll();

        if (!string.IsNullOrEmpty(autoloadMap))
            ChangeMap = autoloadMap;
    }

    public string ChangeMap = "";

    bool ready = false;
    int skipFrames = 5;

	void Update ()
    {
        //skip frames are used to easen up Time.deltaTime after loading
        if (ready)
            if (skipFrames > 0)
            {
                skipFrames--;

                if (skipFrames == 0)
                    paused = false;
            }

        if (!string.IsNullOrEmpty(ChangeMap))
        {
            paused = true;

            skipFrames = 5;
            ready = false;

            MapLoader.Unload();
            if (MapLoader.Load(ChangeMap))
            {
                TheGrid.Init();
                Mesher.CreateMeshes();
                MapLoader.ApplyLinedefBehavior();
                ThingManager.Instance.CreateThings(false);
                AI.CreateHeatmap();

                //DebugObjects.DrawHeatmap();

                Init();
            }

            ChangeMap = "";
        }
    }

    public void Init()
    {
        if (PlayerStart.PlayerStarts[0] == null)
            Debug.LogError("PlayerStart1 == null");
        else
        {
            Player[0].transform.position = PlayerStart.PlayerStarts[0].transform.position + Vector3.up * PlayerControls.Instance.centerHeight;
            Player[0].transform.rotation = PlayerStart.PlayerStarts[0].transform.rotation;
            PlayerControls.Instance.viewDirection.y = Player[0].transform.rotation.eulerAngles.y;
        }

        if (PlayerWeapon.Instance == null)
            PlayerControls.Instance.SwapToBestWeapon();

        for (int i = 0; i < PlayerInfo.Instance.Keycards.Length; i++)
            PlayerInfo.Instance.Keycards[i] = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerControls.Instance.gravityAccumulator = 0f;

        MusicPlayer.Instance.Play(MapLoader.CurrentMap);

        ready = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        paused = !hasFocus;

        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public static void Create3DSound(Vector3 position, string soundName, float minDistance)
    {
        Create3DSound(position, SoundLoader.Instance.LoadSound(soundName), minDistance);
    }

    public static void Create3DSound(Vector3 position, AudioClip clip, float minDistance)
    {
        GameObject sound = new GameObject("3dsound");
        sound.transform.position = position;
        sound.transform.SetParent(Instance.TemporaryObjectsHolder);

        AudioSource audioSource = sound.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = minDistance;
        audioSource.Play();

        sound.AddComponent<DestroyAfterSoundPlayed>();
    }

    public static void PauseForever()
    {
        Instance.ready = false;
        Instance.paused = true;
    }
}