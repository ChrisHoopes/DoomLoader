using System.Collections.Generic;
using UnityEngine;

public class PlayerThing : ThingController, Damageable
{
    AudioSource audioSource;

    protected override void OnAwake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    //will be used for multiplayer later on
    public override void Init() { }
    protected override void CreateBillboard() { }

    public Sector LastSector;

    public bool Dead { get { return hitpoints <= 0; } }
    public bool Bleed { get { return true; } }

    public int hitpoints = 100;
    public int armor = 0;
    public float painTime = 0f;
    public bool finished = false;

    /// <summary>
    /// Used by monsters to cast ray into
    /// </summary>
    public float RayCastSphereRadius = .5f;

    public BreathArea playerBreath = new BreathArea(30);
    public List<ThingController> breathMonsters = new List<ThingController>();

    //apply environment damage
    public float environmentDamageTime;
    public float environmentDamageCooldown;
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        SectorController sc = hit.collider.GetComponent<SectorController>();
        if (sc != null)
            if (sc.sector.specialType > 0)
                switch (sc.sector.specialType)
                {
                    case 4:
                        {
                            environmentDamageCooldown = 1f;

                            if (environmentDamageTime <= 0f)
                            {
                                Damage(20, DamageType.Environment);
                                environmentDamageTime = 1f;
                            }
                        }
                        break;

                    case 5:
                        {
                            environmentDamageCooldown = 1f;

                            if (environmentDamageTime <= 0f)
                            {
                                Damage(10, DamageType.Environment);
                                environmentDamageTime = 1f;
                            }
                        }
                        break;


                    case 7:
                        {
                            environmentDamageCooldown = 1f;

                            if (environmentDamageTime <= 0f)
                            {
                                Damage(5, DamageType.Environment);
                                environmentDamageTime = 1f;
                            }
                        }
                        break;

                    case 9:
                        {
                            PlayerInfo.Instance.unfoundSecrets.Remove(sc.sector);
                            PlayerInfo.Instance.foundSecrets.Add(sc.sector);
                            sc.sector.specialType = 0; //so we don't loop through the lists every frame
                        }
                        break;

                    case 11:
                        {
                            //TODO: remove godmode on touch and end level if player dies (this is the end of the shareware campaign)
                            //currently the end is handled in the OnDamage method

                            environmentDamageCooldown = 1f;

                            if (environmentDamageTime <= 0f)
                            {
                                Damage(20, DamageType.Environment);
                                environmentDamageTime = 1f;
                            }
                        }
                        break;


                    case 16:
                        {
                            environmentDamageCooldown = 1f;

                            if (environmentDamageTime <= 0f)
                            {
                                Damage(20, DamageType.Environment);
                                environmentDamageTime = 1f;
                            }
                        }
                        break;
                }
    }

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (Dead)
        {
            PlayerInfo.Instance.painFlashTime = 2f;

            if (currentSector != null)
                if (transform.position.y > currentSector.floorHeight)
                    transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, currentSector.floorHeight, transform.position.z), Time.deltaTime);
        }

        if (painTime > 0f)
            painTime -= Time.deltaTime;

        if (environmentDamageTime > 0f)
            if (environmentDamageCooldown > 0f)
                environmentDamageTime -= Time.deltaTime;

        if (environmentDamageCooldown > 0f)
            environmentDamageCooldown -= Time.deltaTime;

        if (environmentDamageCooldown <= 0f)
            environmentDamageTime = .5f;

        int cx = Mathf.FloorToInt(transform.position.x);
        int cy = Mathf.FloorToInt(transform.position.z);
        if (cx != cell.x || cy != cell.y)
        {
            RemoveFromGrid();
            cell.x = cx;
            cell.y = cy;
            AddToGrid();

            OnEnterCell();
        }
    }

    public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
    {
        if (Dead)
            return;

        if (damageType != DamageType.Environment)
            amount = Mathf.RoundToInt(amount * GameManager.Instance.PlayerDamageReceive);

        if (amount <= 0)
            return;

        if (armor > 0)
        {
            int subjectiveToMega = Mathf.Min(Mathf.Max(armor - 100, 0), amount);
            int subjectiveToNormal = Mathf.Min(armor, amount - subjectiveToMega);
            int absorbed = Mathf.Max(subjectiveToMega / 2 + subjectiveToNormal / 3, 1);

            armor -= absorbed;
            amount -= absorbed;
        }

        hitpoints -= amount;

        if (amount > 60)
            PlayerInfo.Instance.painFlashTime = 2.5f;
        else if (amount > 40)
            PlayerInfo.Instance.painFlashTime = 2f;
        else if (amount > 20)
            PlayerInfo.Instance.painFlashTime = 1.5f;
        else
            PlayerInfo.Instance.painFlashTime = 1f;

        if (hitpoints <= 0)
        {
            //end game when dying in the final room
            if (currentSector.specialType == 11)
                PlayerInfo.GameFinished = true;

            if (PlayerWeapon.Instance != null)
                PlayerWeapon.Instance.putAway = true;

            audioSource.clip = SoundLoader.Instance.LoadSound(Random.value > .5f ? "DSPLDETH" : "DSPDIEHI");
            audioSource.Play();
        }
        else if (painTime <= 0f)
        {
            audioSource.clip = SoundLoader.Instance.LoadSound("DSPLPAIN");
            audioSource.Play();
            painTime = 1f;
        }
    }

    public void Impulse(Vector3 direction, float force) { }

    public void CastNoise(int range)
    {
        foreach (ThingController monster in breathMonsters)
        {
            Breath b = playerBreath.GetBreath(monster.cell);
            if (b == null) //can happen when shooting while moving (very rare)
                continue; 

            if (b.distance > range)
                continue;

            if (monster.CurrentBehavior == null)
               continue;

            monster.CurrentBehavior.AlertByNoise();
        }
    }

    public void RecastBreath()
    {
        playerBreath.position = cell;
        AI.FillPlayerBreath(ref playerBreath, ref breathMonsters, true);
    }

    protected void OnEnterCell()
    {
        RecastBreath();

        //draw breath
        //DebugObjects.DestroySprites();
        //DebugObjects.DrawBreathArea(playerBreath);
    }

    //alternative way to draw breath
    /*void LateUpdate()
    {
        playerBreath.BreathList.Perform((n) =>
        {
            Vec2I start = n.Data.position;
            Vec2I target = start + Vec2I.directions[n.Data.direction];

            Vector3 s = new Vector3(start.x + .5f, 12, start.y + .5f);
            Vector3 e = new Vector3(target.x + .5f, 12, target.y + .5f);

            Debug.DrawLine(s, e);
        });
    }*/
}
