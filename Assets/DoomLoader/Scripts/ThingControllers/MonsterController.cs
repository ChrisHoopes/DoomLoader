using UnityEngine;
using System.Collections.Generic;

//used by all monsters atm, might get separated in the future
public class MonsterController : ThingController, Damageable
{
    [System.Serializable]
    public struct FivewayFrame
    {
        public Texture[] directionSprites;

        public void Assign(FivewayFrameAssigment assigment)
        {
            directionSprites = new Texture[assigment.directionSpriteNames.Length];
            for (int i = 0; i < directionSprites.Length; i++)
                directionSprites[i] = TextureLoader.Instance.GetSpriteTexture(assigment.directionSpriteNames[i]);
        }
    }

    [System.Serializable]
    public struct FivewayFrameAssigment
    {
        public string[] directionSpriteNames;
    }

    public GameObject AttackProjectile;

    public FivewayFrameAssigment[] _idleFrames = new FivewayFrameAssigment[0];
    public FivewayFrameAssigment[] _painFrames = new FivewayFrameAssigment[0];
    public FivewayFrameAssigment[] _dieFrames = new FivewayFrameAssigment[0];
    public FivewayFrameAssigment[] _walkFrames = new FivewayFrameAssigment[0];
    public FivewayFrameAssigment[] _gibFrames = new FivewayFrameAssigment[0];
    public FivewayFrameAssigment[] _attackFrames = new FivewayFrameAssigment[0];

    [HideInInspector]
    public FivewayFrame[] IdleFrames;
    [HideInInspector]
    public FivewayFrame[] PainFrames;
    [HideInInspector]
    public FivewayFrame[] DieFrames;
    [HideInInspector]
    public FivewayFrame[] WalkFrames;
    [HideInInspector]
    public FivewayFrame[] GibFrames;
    [HideInInspector]
    public FivewayFrame[] AttackFrames;

    public string[] _painSounds;
    public string[] _dieSounds;
    public string[] _nearSounds;
    public string[] _alertSounds;
    public string[] _attackSounds;

    [HideInInspector]
    public AudioSource audioSource;
    [HideInInspector]
    public AudioClip[] PainSounds;
    [HideInInspector]
    public AudioClip[] DieSounds;
    [HideInInspector]
    public AudioClip[] NearSounds;
    [HideInInspector]
    public AudioClip[] AlertSounds;
    [HideInInspector]
    public AudioClip[] AttackSounds;

    public ThingController dropOnDeath;
    CharacterController controller;

    public void CacheSprites(FivewayFrameAssigment[] assigments, ref FivewayFrame[] frames)
    {
        frames = new FivewayFrame[assigments.Length];
        for (int i = 0; i < assigments.Length; i++)
            frames[i].Assign(assigments[i]);
    }

    public void CacheSounds(string[] soundNames, ref AudioClip[] audioClips)
    {
        audioClips = new AudioClip[soundNames.Length];
        for (int i = 0; i < soundNames.Length; i++)
            audioClips[i] = SoundLoader.Instance.LoadSound(soundNames[i]);
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        CacheSprites(_idleFrames, ref IdleFrames);
        CacheSprites(_painFrames, ref PainFrames);
        CacheSprites(_dieFrames, ref DieFrames);
        CacheSprites(_walkFrames, ref WalkFrames);
        CacheSprites(_gibFrames, ref GibFrames);
        CacheSprites(_attackFrames, ref AttackFrames);

        CacheSounds(_painSounds, ref PainSounds);
        CacheSounds(_dieSounds, ref DieSounds);
        CacheSounds(_nearSounds, ref NearSounds);
        CacheSounds(_alertSounds, ref AlertSounds);
        CacheSounds(_attackSounds, ref AttackSounds);

        frametime = Random.Range(0, idleAnimationSpeed);

        if (controller == null)
            enabled = false;
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

    int lastDirection = 0;
    public int hitpoints = 30;
    private bool dead = false;

    [HideInInspector]
    public float painTime = 0f;
    [HideInInspector]
    public float frametime = 0f;
    [HideInInspector]
    public int frameindex = 0;

    [HideInInspector]
    public bool refreshSprite;

    public float walkspeed = .8f;
    public float turnSpeed = 2f;

    public bool finished;
    public Vector3 moveVector = Vector3.zero;
    public Vector3 impulseVector = Vector3.zero;
    public float impulseGroundDampening = 4f;
    public float impulseAirDampening = 1f;

    Sector lastSector;
    [HideInInspector]
    float lastFloorHeight;

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (finished)
        {
            ApplySimpleGravity();
            return;
        }

        Triangle sectorTriangle = TheGrid.GetExactTriangle(transform.position.x, transform.position.z);
        if (sectorTriangle != null)
        {
            lastSector = currentSector;

            if (currentSector != sectorTriangle.sector)
            {
                if (currentSector != null)
                    currentSector.floorObject.DynamicThings.Remove(this);

                currentSector = sectorTriangle.sector;
                currentSector.floorObject.DynamicThings.AddLast(this);

                SetBrightness(currentSector.brightness);
            }
        }

        int cx = Mathf.FloorToInt(transform.position.x);
        int cy = Mathf.FloorToInt(transform.position.z);
        if (cx != cell.x || cy != cell.y)
        {
            RemoveFromGrid();
            cell.x = cx;
            cell.y = cy;
            AddToGrid();
        }

        /*if (dead)
        {
            ApplySimpleGravity();
            UpdateSpriteAnimation();
            return;
        }*/

        //smoother elevator movement
        bool sticktofloor = false;
        if (currentSector != null)
        {
            if (lastSector == currentSector)
                if (lastFloorHeight != currentSector.floorHeight)
                    if (Mathf.Abs(transform.position.y - currentSector.floorHeight) <= MapLoader._4units)
                        sticktofloor = true;
            
            if (transform.position.y < currentSector.floorHeight - 1f)
                transform.position = new Vector3(transform.position.x, currentSector.floorHeight, transform.position.z);

            lastFloorHeight = currentSector.floorHeight;
        }

        //fall down or hit ground
        if (controller.isGrounded)
            gravityAccumulator = 0f;
        else
            gravityAccumulator += Time.deltaTime * GameManager.Instance.gravity;

        //terminal velocity or in elevator
        if (gravityAccumulator > GameManager.Instance.terminalVelocity || sticktofloor)
            gravityAccumulator = GameManager.Instance.terminalVelocity;

        if (!dead)
        {
            if (painTime > 0f)
            {
                painTime -= Time.deltaTime;
                if (painTime <= 0)
                {
                    frameindex = 0;
                    frametime = 0f;
                    refreshSprite = true;
                }
                if (CurrentBehavior != null)
                    CurrentBehavior.Pain();
            }
            if (CurrentBehavior != null)
                CurrentBehavior.Tick();
        }

        //apply force vectors)
        if (controller.enabled)
        {
            Vector3 walk = transform.forward * moveVector.x * walkspeed;
            Vector3 gravity = Vector3.down * gravityAccumulator;
            controller.Move((walk + gravity + impulseVector) * Time.deltaTime);

            //dampen impulse
            if (impulseVector != Vector3.zero)
            {
                if (controller.isGrounded)
                    impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseGroundDampening * Time.deltaTime);
                else
                    impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseAirDampening * Time.deltaTime);

                if ((impulseVector).sqrMagnitude < .001f)
                    impulseVector = Vector3.zero;
            }
        }

        UpdateSpriteAnimation();
    }

    public bool Dead { get { return dead; } }
    public bool Bleed { get { return true; } }
    public float PainChance = .78f;
    public float _PainTime = .2f;

    public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
    {
        if (dead)
            return;

        if (CurrentBehavior != null)
            CurrentBehavior.ModifyDamage(ref amount, damageType);

        if (amount <= 0)
            return;

        hitpoints -= amount;
        if (hitpoints <= 0)
        {
            moveVector = Vector3.zero;
            gameObject.layer = 14; //ragdoll
            dead = true;

            if (DieFrames.Length > 0)
            {
                frametime = 0f;
                frameindex = 0;
                refreshSprite = true;
                SetSprite(DieFrames[0]);
            }

            if (DieSounds.Length > 0)
            {
                audioSource.Stop();
                audioSource.clip = DieSounds[Random.Range(0, DieSounds.Length)];
                audioSource.Play();
            }

            //change type into decoration
            RemoveFromGrid();
            thingType = ThingType.Decor;
            AddToGrid();

            if (dropOnDeath != null)
            {
                ThingController loot = Instantiate(dropOnDeath);
                loot.transform.position = transform.position + transform.forward * .1f;
                loot.transform.rotation = transform.rotation;
                loot.Init();
                loot.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
            }

            return;
        }

        if (Random.value <= PainChance)
        {
            if (painTime < .1f)
                if (PainSounds.Length > 0)
                {
                    audioSource.Stop();
                    audioSource.clip = PainSounds[Random.Range(0, PainSounds.Length)];
                    audioSource.Play();
                }

            painTime = _PainTime;

            if (PainFrames.Length > 0)
            {
                frameindex = 0;
                frametime = 0f;
                refreshSprite = true;
                SetSprite(PainFrames[0]);
            }
        }

        if (CurrentBehavior != null)
            CurrentBehavior.alert = true;

        //alert nearby enemies, since sound won't propagate here
        if (attacker != null)
            if (attacker.GetComponent<PlayerThing>() != null)
                if (GameManager.Instance.Player[0].playerBreath.GetBreath(cell) == null)
                {
                    BreathArea alarmBreath = new BreathArea(15) { position = cell };

                    List<ThingController> monsters = new List<ThingController>();
                    AI.FillBreath(ref alarmBreath, ref monsters, true);

                    foreach (ThingController tc in monsters)
                        if (tc.CurrentBehavior != null)
                            tc.CurrentBehavior.AlertByNoise();
                }
    }

    public float Mass = 100f;
    public void Impulse(Vector3 direction, float force)
    {
        float length = force / Mass;
        impulseVector += direction * length;
    }

    public void InitAttackAnimation()
    {
        if (AttackFrames.Length > 0)
        {
            frameindex = 0;
            frametime = 0f;
            refreshSprite = true;
            SetSprite(AttackFrames[0]);
        }
    }

    public float deathAnimationSpeed = .1f;
    public float idleAnimationSpeed = .4f;
    public float walkAnimationSpeed = .15f;
    public float attackAnimationSpeed = .5f;

    public void UpdateSpriteAnimation()
    {
        if (dead)
        {
            //if (impulseVector != Vector3.zero && !controller.isGrounded)
            //    return;

            frametime += Time.deltaTime;
            if (frametime > deathAnimationSpeed)
            {
                frametime = 0f;
                frameindex++;

                if (frameindex >= DieFrames.Length)
                {
                    if (impulseVector == Vector3.zero)
                    {
                        CapsuleCollider collider = GetComponent<CapsuleCollider>();
                        if (collider != null)
                            collider.enabled = false;

                        controller.enabled = false;
                        finished = true;

                        //corpse is out of map, make it static
                        if (currentSector == null)
                        {
                            dynamic = false;
                            enabled = false;
                        }
                        else
                        {
                            //check if corpse is near a dynamic sector
                            foreach (Sector s in TheGrid.GetMoreNearbySectors(transform.position, 1))
                                if (s.Dynamic)
                                    goto nosleepforthedead;

                            //make corpse into static object for optimization
                            dynamic = false;
                            currentSector.floorObject.DynamicThings.Remove(this);
                            currentSector.floorObject.StaticThings.Add(this);
                            enabled = false;

                            nosleepforthedead:;
                        }

                        //change sprite material type to omni
                        if (spriteType == SpriteType.Axis)
                        {
                            mr.material = MaterialManager.Instance.omniBillboardMaterial;
                            spriteType = SpriteType.Omni;
                        }

                        if (spriteType == SpriteType.TransparentAxis)
                        {
                            mr.material = MaterialManager.Instance.transparentOmniBillboardMaterial;
                            spriteType = SpriteType.TransparentOmni;
                        }
                    }

                    return;
                }

                refreshSprite = true;
                SetSprite(DieFrames[frameindex]);
            }

            return;
        }

        if (painTime > 0f)
        {
            if (PainFrames.Length > 0)
                SetSprite(PainFrames[0]);

            return;
        }

        if (CurrentBehavior != null)
            if (CurrentBehavior.attacking)
            {
                if (AttackFrames.Length == 0)
                    return;

                frametime += Time.deltaTime;
                if (frametime > attackAnimationSpeed)
                {
                    frametime = 0f;
                    frameindex++;

                    if (frameindex >= AttackFrames.Length)
                        frameindex = 0;

                    refreshSprite = true;
                    SetSprite(AttackFrames[frameindex]);
                }
            }
            else if (moveVector != Vector3.zero)
            {
                if (WalkFrames.Length == 0)
                    return;

                frametime += Time.deltaTime;
                if (frametime > walkAnimationSpeed)
                {
                    frametime = 0f;
                    frameindex++;

                    if (frameindex >= WalkFrames.Length)
                        frameindex = 0;

                    refreshSprite = true;
                    SetSprite(WalkFrames[frameindex]);
                }
            }
            else
            {
                if (IdleFrames.Length == 0)
                    return;

                frametime += Time.deltaTime;
                if (frametime > idleAnimationSpeed)
                {
                    frametime = 0f;
                    frameindex++;

                    if (frameindex >= IdleFrames.Length || CurrentBehavior.alert)
                        frameindex = 0;

                    refreshSprite = true;
                    SetSprite(IdleFrames[frameindex]);
                }
            }
    }

    public void SetSprite(FivewayFrame frame)
    {
        int directionNumber = 0;

        if (frame.directionSprites.Length < 5)
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
            if (directionNumber > 4)
            {
                directionNumber = 8 - directionNumber;
                materialProperties.SetVector("_UvTransform", new Vector4(1, 0, 0, 0)); //mirror U axis
            }
            else
                materialProperties.SetVector("_UvTransform", new Vector4(0, 0, 0, 0));
        }

        if (lastDirection != directionNumber)
        {
            refreshSprite = true;
            lastDirection = directionNumber;
        }

    skipangle:

        if (!refreshSprite)
            return;

        Texture sprite = frame.directionSprites[directionNumber];

        materialProperties.SetFloat("_ScaleX", (float)sprite.width / MapLoader.sizeDividor);
        materialProperties.SetFloat("_ScaleY", (float)sprite.height / MapLoader.sizeDividor);
        materialProperties.SetTexture("_MainTex", sprite);
        mr.SetPropertyBlock(materialProperties);

        refreshSprite = false;
    }
}