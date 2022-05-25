using UnityEngine;

public class BarrelController : ThingController, Damageable
{
    public int blastDamage = 100;
    public float explosionRadius = 4f;

    public string[] _idleSprites = new string[0];
    public string[] _explodeSprites = new string[0];

    public string _explodeSound;

    [HideInInspector]
    public Texture[] IdleTextures;
    [HideInInspector]
    public Texture[] ExplodeTextures;

    protected override void OnAwake()
    {
        base.OnAwake();

        IdleTextures = new Texture[_idleSprites.Length];
        for (int i = 0; i < _idleSprites.Length; i++)
            IdleTextures[i] = TextureLoader.Instance.GetSpriteTexture(_idleSprites[i]);

        ExplodeTextures = new Texture[_explodeSprites.Length];
        for (int i = 0; i < _explodeSprites.Length; i++)
            ExplodeTextures[i] = TextureLoader.Instance.GetSpriteTexture(_explodeSprites[i]);
    }

    protected override void CreateBillboard()
    {
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

    public int hitpoints = 20;
    private bool dead = false;

    float frametime = 0f;
    int frameindex = 0;

    public float pushForce = 1000;

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (dynamic)
            ApplySimpleGravity();

        if (dead)
        {
            frametime += Time.deltaTime;
            if (frametime > .15f)
            {
                frametime = 0f;

                frameindex++;

                if (frameindex == 2)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, ~(1 << 9), QueryTriggerInteraction.Ignore);
                    foreach (Collider hit in hits)
                    {
                        Damageable d = hit.GetComponent<Damageable>();
                        if (d != null)
                        {
                            float distance = (hit.transform.position - transform.position).magnitude;
                            d.Damage(AxMath.Lerp(blastDamage, 1, distance / explosionRadius), DamageType.Explosion, gameObject);
                            d.Impulse((hit.transform.position - transform.position).normalized, Mathf.Lerp(pushForce, 100, distance / explosionRadius));
                        }
                    }
                }

                if (frameindex >= ExplodeTextures.Length)
                {
                    Destroy(gameObject);

                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                            AI.CalculateHeat(cell.x + x, cell.y + y);

                    if (GameManager.Instance.Player[0].playerBreath.GetBreath(cell) != null)
                        GameManager.Instance.Player[0].RecastBreath();

                    return;
                }

                SetSprite(ExplodeTextures[frameindex]);
            }
            return;
        }

        frametime += Time.deltaTime;
        if (frametime > .25f)
        {
            frametime = 0;

            frameindex++;
            if (frameindex >= IdleTextures.Length)
                frameindex = 0;

            SetSprite(IdleTextures[frameindex]);
        }
    }

    public bool Dead { get { return dead; } }
    public bool Bleed { get { return false; } }

    public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
    {
        if (dead)
            return;

        hitpoints -= amount;
        if (hitpoints <= 0)
        {
            GetComponent<CapsuleCollider>().enabled = false;

            dead = true;
            SetSprite(ExplodeTextures[0]);
            frametime = 0f;
            frameindex = 0;

            GameManager.Create3DSound(transform.position, _explodeSound, 10f); 
            return;
        }
    }

    public void Impulse(Vector3 direction, float force) { }

    public void SetSprite(Texture sprite)
    {
        materialProperties.SetFloat("_ScaleX", (float)sprite.width / MapLoader.sizeDividor);
        materialProperties.SetFloat("_ScaleY", (float)sprite.height / MapLoader.sizeDividor);
        materialProperties.SetTexture("_MainTex", sprite);
        mr.SetPropertyBlock(materialProperties);
    }
}
