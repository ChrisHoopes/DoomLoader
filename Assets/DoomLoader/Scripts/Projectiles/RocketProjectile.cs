using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public bool alwaysBright;

    public string spriteName;
    protected MeshRenderer mr;
    protected MaterialPropertyBlock materialProperties;

    public Vector2 pivot = new Vector2(.5f, .5f);

    public GameObject owner;
    public float speed = 4f;
    public int directDamageMin = 20;
    public int directDamageMax = 160;
    public int blastDamage = 128;
    public float explosionRadius = 4f;

    public GameObject OnDeathSpawn;
    public string _onDeathSound;

    public string[] _spriteNames = new string[5];
    [HideInInspector]
    public Texture[] sprites = new Texture[5];

    int lastDirection = 0;

    public bool exploding = false;
    public float pushForce = 500f;

    void Awake()
    {
        for (int i = 0; i < 5; i++)
            sprites[i] = TextureLoader.Instance.GetSpriteTexture(_spriteNames[i]);

        mr = GetComponent<MeshRenderer>();

        if (mr == null)
        {
            Destroy(gameObject);
            return;
        }

        materialProperties = new MaterialPropertyBlock();
        mr.GetPropertyBlock(materialProperties);

        Triangle sectorTriangle = TheGrid.GetExactTriangle(transform.position);
        if (sectorTriangle != null)
            materialProperties.SetFloat("_SectorLight", alwaysBright ? 1f : sectorTriangle.sector.brightness);

        CreateBillboard();
    }

    protected virtual void CreateBillboard()
    {
        if (mr == null)
            return;

        if (!string.IsNullOrEmpty(spriteName))
        {
            Texture tex = TextureLoader.Instance.GetSpriteTexture(spriteName);
            materialProperties.SetFloat("_ScaleX", (float)tex.width / MapLoader.sizeDividor);
            materialProperties.SetFloat("_ScaleY", (float)tex.height / MapLoader.sizeDividor);
            Mesh mesh = Mesher.CreateBillboardMesh(1, 1, .5f, 0);
            GetComponent<MeshFilter>().mesh = mesh;

            materialProperties.SetTexture("_MainTex", tex);
            mr.SetPropertyBlock(materialProperties);
        }
    }

    public void UpdateSprite()
    {
        int directionNumber = 0;

        //calculate sprite number
        Vector3 toTarget = (Camera.main.transform.position - transform.position).normalized;

        int angle1 = Mathf.RoundToInt((Mathf.Atan2(transform.forward.z, transform.forward.x) + Mathf.PI) / (Mathf.PI * 2) * 8) % 8;
        int angle2 = Mathf.RoundToInt((Mathf.Atan2(toTarget.z, toTarget.x) + Mathf.PI) / (Mathf.PI * 2) * 8) % 8;

        directionNumber = angle1 - angle2; //for some reason rocket is done in reverse
        if (directionNumber < 0) directionNumber += 8;

        if (directionNumber > 4)
        {
            directionNumber = 8 - directionNumber;
            materialProperties.SetVector("_UvTransform", new Vector4(1, 0, 0, 0)); //mirror U axis
        }
        else
            materialProperties.SetVector("_UvTransform", new Vector4(0, 0, 0, 0));

        if (lastDirection != directionNumber)
        {
            lastDirection = directionNumber;

            Texture sprite = sprites[directionNumber];

            materialProperties.SetFloat("_ScaleX", (float)sprite.width / MapLoader.sizeDividor);
            materialProperties.SetFloat("_ScaleY", (float)sprite.height / MapLoader.sizeDividor);
            materialProperties.SetTexture("_MainTex", sprite);
            mr.SetPropertyBlock(materialProperties);
        }
    }

    void Update ()
    {
        //check for collision
        float nearest = float.MaxValue;
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit[] hits = Physics.SphereCastAll(ray, .2f, speed * Time.deltaTime, ~((1 << 9) | (1 << 14)), QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == owner)
                    continue;

                if (hit.distance < nearest)
                    nearest = hit.distance;

                Damageable d = hit.collider.GetComponent<Damageable>();
                if (d != null)
                    d.Damage(Random.Range(directDamageMin, directDamageMax + 1), DamageType.Explosion, owner);
            }
        }

        //explosion
        if (nearest < float.MaxValue)
        {
            transform.position = transform.position + transform.forward * nearest;

            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, ~(1 << 9), QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                Damageable d = hit.GetComponent<Damageable>();
                if (d != null)
                {
                    float distance = (hit.transform.position - transform.position).magnitude;
                    d.Damage(AxMath.Lerp(blastDamage, 1, distance / explosionRadius), DamageType.Explosion, owner);
                    d.Impulse((hit.transform.position - transform.position).normalized, Mathf.Lerp(pushForce, 100, distance / explosionRadius));
                }
            }

            if (OnDeathSpawn != null)
            {
                GameObject go = Instantiate(OnDeathSpawn);
                go.transform.position = transform.position;
            }
            
            if (!string.IsNullOrEmpty(_onDeathSound))
                GameManager.Create3DSound(transform.position, _onDeathSound, 10f);

            Destroy(gameObject);

            return;
        }
        
        transform.position = transform.position + transform.forward * speed * Time.deltaTime;

        Triangle sectorTriangle = TheGrid.GetExactTriangle(transform.position.x, transform.position.z);
        if (sectorTriangle != null)
            SetBrightness(sectorTriangle.sector.brightness); //constant update since we are not in sector thing lists

        UpdateSprite();
    }

    public void SetBrightness(float value)
    {
        if (mr == null)
            return;

        if (alwaysBright)
            return;

        mr.GetPropertyBlock(materialProperties);
        materialProperties.SetFloat("_SectorLight", value);
        mr.SetPropertyBlock(materialProperties);
    }
}
