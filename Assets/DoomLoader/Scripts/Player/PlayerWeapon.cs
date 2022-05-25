using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public Texture crosshair;

    public int DamageMin = 5;
    public int DamageMax = 15;

    public float swapSpeed = 6f;

    [System.Serializable]
    public struct AnimationFrame
    {
        public string _spriteName;
        [HideInInspector]
        public Texture texture;
        public float frameTime;
        public Vector3 offset;
    }

    public int[] idleAnimation = new int[0];
    public int[] fireAnimation = new int[0];
    public int[] muzzleAnimation = new int[0];

    public AnimationFrame[] frames = new AnimationFrame[0];

    public string[] _sounds = new string[0];
    [HideInInspector]
    public AudioClip[] Sounds = new AudioClip[0];

    protected AudioSource audioSource;

    protected GameObject muzzleObject;

    public Vector3 Offset = new Vector3(0,-.3f, .5f);
    public Vector3 MuzzleOffset = new Vector3(0, 0.15f, 0);
    public Vector2 Size = new Vector2(0.003f, 0.003f);

    public static PlayerWeapon Instance;

    MeshRenderer mr;
    MaterialPropertyBlock materialProperties;

    MeshRenderer muzzlemr;
    MaterialPropertyBlock muzzleproperties;

    [HideInInspector]
    public float sectorLight = 1f;
    [HideInInspector]
    public float _sectorLight = 1f;

    public float MinimumBrightness = .15f;

    public float hBob = .1f;
    public float vBob = .08f;

    public float LowerOffset = -.3f;
    public float LowerAmount = 1f;

    public int Noise { get { return 1000; } }

    void Awake()
    {
        if (Instance != null)
            Destroy(Instance.gameObject);

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        mr = GetComponent<MeshRenderer>();
        materialProperties = new MaterialPropertyBlock();

        transform.SetParent(GameObject.Find("UICamera").transform);
        transform.localRotation = Quaternion.identity;

        for (int i = 0; i < frames.Length; i++)
            frames[i].texture = TextureLoader.Instance.GetSpriteTexture(frames[i]._spriteName);

        Sounds = new AudioClip[_sounds.Length];
        for (int i = 0; i < _sounds.Length; i++)
            Sounds[i] = SoundLoader.Instance.LoadSound(_sounds[i]);

        muzzleLight = GetComponentInChildren<Light>();

        if (!Options.UseMuzzleLight)
            if (muzzleLight != null)
                muzzleLight.enabled = false;
    }

    void Start()
    {
        mr.GetPropertyBlock(materialProperties);
        GetComponent<MeshFilter>().mesh = Mesher.CreateBillboardMesh(1, 1, .5f, 0f);

        if (idleAnimation.Length > 0)
        {
            currentFrame = idleAnimation[0];
            SetSprite();
        }

        if (muzzleAnimation.Length > 0)
        {
            muzzleObject = new GameObject("Muzzle");
            muzzleObject.SetActive(false);
            muzzleObject.transform.SetParent(transform);
            muzzleObject.transform.localRotation = Quaternion.identity;

            Mesh mesh = Mesher.CreateBillboardMesh(1, 1, .5f, 0f);
            muzzleObject.AddComponent<MeshFilter>().mesh = mesh;
            muzzlemr = muzzleObject.AddComponent<MeshRenderer>();
            muzzlemr.material = MaterialManager.Instance.transparentAxisBillboardMaterial;

            muzzleproperties = new MaterialPropertyBlock();
            muzzlemr.GetPropertyBlock(muzzleproperties);
            SetMuzzleSprite(frames[muzzleAnimation[0]].texture);
        }

        if (frames.Length == 0)
            enabled = false;
    }

    public bool bopActive;

    float interp;
    float xCoef;
    float yCoef;

    public int currentFrame;
    protected float frameTime = 0f;
    protected int animationFrameIndex = 0;
    protected float muzzleFrameTime = 0f;
    protected int muzzleFrameIndex;

    public bool putAway = false;
    public static void PutAway() { if (Instance != null) Instance.putAway = true; }

    //used for debugging
    public int forceFrame = -1;

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (Options.UseMuzzleLight)
            if (muzzleLight != null)
            {
                muzzleLight.intensity = Mathf.Max(Mathf.Lerp(muzzleLight.intensity, 0, Time.deltaTime * 5), 0);

                if (muzzleLight.intensity == 0)
                    muzzleLight.enabled = false;
            }

        if (fireTime <= 0f)
        {
            frameTime += Time.deltaTime;
            if (idleAnimation.Length > 0)
                if (frameTime >= frames[idleAnimation[animationFrameIndex]].frameTime)
                {
                    animationFrameIndex++;
                    frameTime = 0;

                    if (animationFrameIndex >= idleAnimation.Length)
                        animationFrameIndex = 0;

                    currentFrame = idleAnimation[animationFrameIndex];

                    SetSprite();
                }
        }
        else
        {
            fireTime -= Time.deltaTime;
            if (fireAnimation.Length > 0)
            {
                bopActive = false;
                if (fireTime <= 0)
                {
                    frameTime = 0;

                    animationFrameIndex = 0;
                    if (idleAnimation.Length > 0)
                        currentFrame = idleAnimation[animationFrameIndex];

                    SetSprite();
                }
                else
                {
                    frameTime += Time.deltaTime;
                    if (frameTime >= frames[fireAnimation[animationFrameIndex]].frameTime)
                    {
                        frameTime = 0;
                        animationFrameIndex++;
                        if (animationFrameIndex >= fireAnimation.Length)
                            animationFrameIndex = 0;

                        currentFrame = fireAnimation[animationFrameIndex];

                        SetSprite();
                    }
                }
            }
        }

        if (muzzleTimer > 0f)
        {
            if (muzzleAnimation.Length > 1)
            {
                muzzleFrameTime += Time.deltaTime;
                if (muzzleFrameTime > frames[muzzleAnimation[muzzleFrameIndex]].frameTime)
                {
                    muzzleFrameTime = 0;
                    muzzleFrameIndex++;

                    if (muzzleFrameIndex >= muzzleAnimation.Length)
                        muzzleFrameIndex = 0;

                    SetMuzzleSprite(frames[muzzleAnimation[muzzleFrameIndex]].texture);
                }
            }

            muzzleTimer -= Time.deltaTime;
            if (muzzleTimer <= 0f)
                if (muzzleObject != null)
                    muzzleObject.SetActive(false);
        }

        _sectorLight = Mathf.Lerp(_sectorLight, sectorLight, Time.deltaTime * 6f);
        materialProperties.SetFloat("_SectorLight", _sectorLight);
        materialProperties.SetFloat("_Illumination", MinimumBrightness);
        mr.SetPropertyBlock(materialProperties);

        if (bopActive)
            interp = Mathf.Lerp(interp, 1, Time.deltaTime * 2);
        else
            interp = Mathf.Lerp(interp, 0, Time.deltaTime * 4);

        xCoef = Mathf.Sin(Time.time * 3) * interp;
        yCoef = Mathf.Abs(Mathf.Cos(Time.time * 3)) * interp;

        if (putAway)
        {
            LowerAmount = Mathf.Lerp(LowerAmount, 1, Time.deltaTime * swapSpeed);
            if (LowerAmount > .99f)
                Destroy(gameObject);
        }
        else
            LowerAmount = Mathf.Lerp(LowerAmount, 0, Time.deltaTime * swapSpeed);

        transform.localPosition = new Vector3(xCoef * hBob, -yCoef * vBob + LowerOffset * LowerAmount, 0) + Offset + frames[currentFrame].offset;

        if (muzzleObject != null)
            muzzleObject.transform.localPosition = MuzzleOffset + frames[muzzleAnimation[muzzleFrameIndex]].offset;

        OnUpdate();

        if (forceFrame != -1)
            if (forceFrame != currentFrame)
                SetSprite();
    }

    protected virtual void OnUpdate() { }

    public void SetSprite()
    {
        if (forceFrame != -1)
            if (forceFrame >= 0 && forceFrame < frames.Length)
                currentFrame = forceFrame;

        if (currentFrame < 0 || currentFrame >= frames.Length)
            return;

        materialProperties.SetTexture("_MainTex", frames[currentFrame].texture);
        materialProperties.SetFloat("_ScaleX", frames[currentFrame].texture.width * Size.x);
        materialProperties.SetFloat("_ScaleY", frames[currentFrame].texture.height * Size.y);
        mr.SetPropertyBlock(materialProperties);
    }

    public void SetMuzzleSprite(Texture spriteTexture)
    {
        if (muzzleObject == null)
            return;

        muzzleproperties.SetTexture("_MainTex", spriteTexture);
        muzzleproperties.SetFloat("_ScaleX", spriteTexture.width * Size.x);
        muzzleproperties.SetFloat("_ScaleY", spriteTexture.height * Size.y);
        muzzleproperties.SetFloat("_Alpha", .5f);
        muzzleproperties.SetFloat("_Illumination", 1);
        muzzlemr.SetPropertyBlock(muzzleproperties);
    }

    public float _fireRate = .4f;
    [HideInInspector]
    public float fireTime = 0f;

    public float _muzzleTime = .1f;
    [HideInInspector]
    public float muzzleTimer = 0f;


    protected Light muzzleLight;

    public virtual bool Fire()
    {
        return false;
    }

    void OnGUI()
    {
        if (Options.Crosshair)
            if (crosshair != null)
                GUI.Label(new Rect(Screen.width / 2 - crosshair.width / 2, Screen.height / 2 - crosshair.height / 2, crosshair.width, crosshair.height), crosshair);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
