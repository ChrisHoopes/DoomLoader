using UnityEngine;

/// <summary>
/// Currently used only by Baron of Hell fireball (has 4-way directional sprite option)
/// </summary>
public class AnimatedProjectile : MonoBehaviour 
{
    public Vector2 SizeMultiplier = new Vector2(1, 1);
    MeshRenderer mr;
    protected MaterialPropertyBlock materialProperties;

    [System.Serializable]
    public struct FrameAssigment
    {
        public string[] directionSpriteNames;

    }

    [System.Serializable]
    public struct Frame
    {
        public Texture[] directionSprites;

        public void Assign(FrameAssigment assigment)
        {
            directionSprites = new Texture[assigment.directionSpriteNames.Length];
            for (int i = 0; i < directionSprites.Length; i++)
                directionSprites[i] = TextureLoader.Instance.GetSpriteTexture(assigment.directionSpriteNames[i]);
        }
    }

    public void CacheSprites(FrameAssigment[] assigments, ref Frame[] frames)
    {
        frames = new Frame[assigments.Length];
        for (int i = 0; i < assigments.Length; i++)
            frames[i].Assign(assigments[i]);
    }

    public FrameAssigment[] _frames = new FrameAssigment[0];

    [HideInInspector]
    public Frame[] Frames;

    public float animationSpeed = 1f;
    public bool oscillates;
    public int direction = 1;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();

        CacheSprites(_frames, ref Frames);
        frametime = Random.Range(0, animationSpeed);
    }

    private void Start()
    {
        materialProperties = new MaterialPropertyBlock();

        if (Frames.Length == 0)
            enabled = false;
        else
            SetSprite(Frames[0]);
    }

    float frametime;
    int index = 0;

    void Update()
    {
        frametime += Time.deltaTime;
        if (frametime > animationSpeed)
        {
            index += direction;
            frametime = 0;

            if (index >= Frames.Length)
                if (oscillates)
                {
                    direction = -1;
                    index--;
                }
                else
                    index = 0;

            if (index < 0)
                if (oscillates)
                {
                    direction = 1;
                    index++;
                }
                else
                    index = Frames.Length - 1;

            SetSprite(Frames[index]);
        }
    }

    public void SetSprite(Frame frame)
    {
        int directionNumber = 0;

        if (frame.directionSprites.Length < 4)
            goto skipangle;

        //calculate sprite number
        Vector3 toTarget = (Camera.main.transform.position - transform.position).normalized;

        int angle1 = Mathf.RoundToInt((Mathf.Atan2(transform.forward.z, transform.forward.x) + Mathf.PI) / (Mathf.PI * 2) * 8) % 8;
        int angle2 = Mathf.RoundToInt((Mathf.Atan2(toTarget.z, toTarget.x) + Mathf.PI) / (Mathf.PI * 2) * 8) % 8;

        directionNumber = angle2 - angle1;
        if (directionNumber < 0) directionNumber += 8;

        if (frame.directionSprites.Length == 8)
        {
            materialProperties.SetVector("_UvTransform", new Vector4(0, 0, 0, 0));
        }
        else
        {
            if (frame.directionSprites.Length == 4 && directionNumber == 4)
                directionNumber = 0;

            if (directionNumber > 4)
            {
                directionNumber = 8 - directionNumber;
                materialProperties.SetVector("_UvTransform", new Vector4(1, 0, 0, 0)); //mirror U axis
            }
            else
                materialProperties.SetVector("_UvTransform", new Vector4(0, 0, 0, 0));
        }

        skipangle:
        Texture sprite = frame.directionSprites[directionNumber];

        materialProperties.SetFloat("_ScaleX", (float)sprite.width / MapLoader.sizeDividor * SizeMultiplier.x);
        materialProperties.SetFloat("_ScaleY", (float)sprite.height / MapLoader.sizeDividor * SizeMultiplier.y);
        materialProperties.SetTexture("_MainTex", sprite);
        mr.SetPropertyBlock(materialProperties);
    }
}
