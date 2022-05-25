using System.Collections.Generic;
using UnityEngine;

public class FormerHumanTrooperBehavior : BehaviorBase
{
    const float AggroChance = .6f;
    const float EyeHeight = 1.55f;
    const float ViewFrustumMin = -.15f;
    const float SeeDistance = 400f;
    const float IdleChance = .1f;
    const float NearSoundChanceSleeping = .02f;
    const float NearSoundChanceAwake = .03f;
    const float PokeChance = .5f;

    const float AttackStartTime = 1f;
    const float AttackHappenTime = .5f;

    protected virtual float attackSpread { get { return .04f; } }
    protected virtual int shotCount { get { return 1; } }

    const int DamageMin = 5;
    const int DamageMax = 15;

    const float randomMoveChance = .1f;
    const float closestMoveChance = .4f;

    MonsterController mc;

    public override bool alert
    {
        get { return base.alert; }

        set
        {
            if (!base.alert && value)
                if (mc.AlertSounds.Length > 0)
                {
                    mc.audioSource.Stop();
                    mc.audioSource.clip = mc.AlertSounds[Random.Range(0, mc.AlertSounds.Length)];
                    mc.audioSource.Play();
                }

            base.alert = value;
        }
    }

    public override bool attacking
    {
        get
        {
            return attackTime > 0f;
        }
    }

    public override void Init()
    {
        mc = owner.GetComponent<MonsterController>();
        if (mc == null)
            Debug.LogError("FormerHumanTrooperBehavior: Init: Could not get component \"MonsterController\" from owner \"" + owner.thingName + "\"");

        decisionTime = Random.Range(0f, .5f);
    }

    bool SeePlayerRay(int attempts = 1)
    {
        Ray ray;
        return SeePlayerRay(out ray, attempts);
    }

    bool SeePlayerRay(out Ray ray, int attempts = 1)
    {
        PlayerThing player = GameManager.Instance.Player[0];
        if (player == null)
        {
            ray = new Ray();
            return false;
        }

        Vector3 eyePos = owner.transform.position + Vector3.up * EyeHeight;
        Vector3 toPlayer = ((player.transform.position + Random.onUnitSphere * player.RayCastSphereRadius) - eyePos).normalized;
        ray = new Ray(eyePos, toPlayer);

        if (Vector3.Dot(owner.transform.forward, toPlayer) < ViewFrustumMin)
            return false;

        while (attempts > 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, SeeDistance, ~((1 << 9) | (1 << 11) | (1 << 14)), QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<PlayerThing>() != null)
                    return true;

            toPlayer = ((player.transform.position + Random.onUnitSphere * player.RayCastSphereRadius) - eyePos).normalized;
            ray = new Ray(eyePos, toPlayer);
            attempts--;
        }

        return false;
    }

    bool painFrame;
    float attackTime;
    bool attacked;
    float decisionTime;
    Vector3 wantDirection;
    bool wantMove;

    public override void Tick()
    {
        if (!alert)
        {
            if (decisionTime > 0f)
            {
                decisionTime -= Time.deltaTime;
                return;
            }
            decisionTime = Random.Range(.4f, .6f);

            int distance = AxMath.WeightedDistance(owner.cell, GameManager.Instance.Player[0].cell);
            if (distance > SeeDistance)
                return;

            if (Random.value <= NearSoundChanceSleeping)
                if (mc.NearSounds.Length > 0)
                    if (!mc.audioSource.isPlaying)
                    {
                        mc.audioSource.clip = mc.NearSounds[Random.Range(0, mc.NearSounds.Length)];
                        mc.audioSource.Play();
                    }

            if (SeePlayerRay())
            {
                alert = true;
                decisionTime = 0f;
            }

            if (!alert)
                return;
        }

        if (painFrame)
        {
            mc.moveVector = Vector3.zero;
            painFrame = false;
            attackTime = 0f;
            decisionTime = 0f;
            return;
        }

        if (attackTime > 0f)
        {
            attackTime -= Time.deltaTime;

            Vector3 aimAt = (GameManager.Instance.Player[0].transform.position - mc.transform.position).normalized;
            //mc.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(mc.transform.forward, new Vector3(aimAt.x, 0, aimAt.z), Time.deltaTime * mc.turnSpeed), Vector3.up);
            //instantenous rotation towards target
            mc.transform.rotation = Quaternion.LookRotation(new Vector3(aimAt.x, 0, aimAt.z), Vector3.up);

            if (attackTime < AttackHappenTime && !attacked)
            { 
                attacked = true;

                if (mc.AttackSounds.Length > 0)
                    GameManager.Create3DSound(mc.transform.position, mc.AttackSounds[Random.Range(0, mc.AttackSounds.Length)], 10f);

                PlayerThing player = GameManager.Instance.Player[0];
                if (player != null)
                {
                    //swap layers for a while to avoid hitting self
                    int originalLayer = owner.gameObject.layer;
                    owner.gameObject.layer = 9;

                      for (int i = 0; i < shotCount; i++)
                    {
                        Vector3 eyePos = owner.transform.position + Vector3.up * EyeHeight;
                        Vector3 toPlayer = ((player.transform.position + Random.onUnitSphere * player.RayCastSphereRadius) - eyePos).normalized;
                        toPlayer += Random.insideUnitSphere * attackSpread;
                        toPlayer.Normalize();
                        Ray ray = new Ray(eyePos, toPlayer);

                        /*GameObject visualLine = new GameObject();
                        LineRenderer lr = visualLine.AddComponent<LineRenderer>();
                        lr.positionCount = 2;
                        lr.SetPosition(0, ray.origin);
                        lr.SetPosition(1, ray.origin + ray.direction * 200);
                        lr.widthMultiplier = .02f;
                        visualLine.AddComponent<DestroyAfterTime>();*/

                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, 200, ~((1 << 9) | (1 << 14)), QueryTriggerInteraction.Ignore))
                        {
                            Damageable target = hit.collider.gameObject.GetComponent<Damageable>();
                            if (target != null)
                            {
                                target.Damage(Random.Range(DamageMin, DamageMax + 1), DamageType.Generic, owner.gameObject);

                                if (target.Bleed)
                                {
                                    GameObject blood = GameObject.Instantiate(GameManager.Instance.BloodDrop);
                                    blood.transform.position = hit.point - ray.direction * .2f;
                                }
                                else
                                {
                                    GameObject puff = GameObject.Instantiate(GameManager.Instance.BulletPuff);
                                    puff.transform.position = hit.point - ray.direction * .2f;
                                }
                            }
                            else
                            {
                                GameObject puff = GameObject.Instantiate(GameManager.Instance.BulletPuff);
                                puff.transform.position = hit.point - ray.direction * .2f;
                            }
                        }
                    }

                    owner.gameObject.layer = originalLayer;
                }
            }

            return;
        }

        if (wantDirection != Vector3.zero)
            mc.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(mc.transform.forward, wantDirection, Time.deltaTime * mc.turnSpeed), Vector3.up);

        if (decisionTime > 0f)
        {
            decisionTime -= Time.deltaTime;
            return;
        }

        if (Random.value <= PokeChance)
        {
            Ray ray = new Ray(owner.transform.position + Vector3.up, owner.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2, ~((1 << 9) | (1 << 11)), QueryTriggerInteraction.Ignore))
            {
                Pokeable lc = hit.collider.gameObject.GetComponent<Pokeable>();
                if (lc != null)
                    if (lc.AllowMonsters())
                        lc.Poke(owner.gameObject);
            }
        }

        if (Random.value <= NearSoundChanceAwake)
            if (mc.NearSounds.Length > 0)
                if (!mc.audioSource.isPlaying)
                {
                    mc.audioSource.clip = mc.NearSounds[Random.Range(0, mc.NearSounds.Length)];
                    mc.audioSource.Play();
                }

        decisionTime = Random.Range(.4f, .6f);
        wantDirection = Vector3.zero;

        bool aggro = false;
        if (Random.value < AggroChance)
        {
            Ray toPlayer;
            if (SeePlayerRay(out toPlayer))
            {
                wantDirection = new Vector3(toPlayer.direction.x, 0, toPlayer.direction.z);
                wantMove = false;
                aggro = true;
                attacked = false;
                attackTime = 1f;
                decisionTime = 0f;
                mc.InitAttackAnimation();
            }
        }

        if (!aggro)
        {
            float moveRoll = Random.value;
            if (moveRoll < randomMoveChance)
                MoveToRandomNearbyCell();
            else if (moveRoll < closestMoveChance)
            {
                if (!MoveToRandomClosestBreath())
                    MoveToRandomNearbyCell();
            }
            else
            {
                if (!MoveTowardsBreath())
                    if (!MoveToRandomClosestBreath())
                        MoveToRandomNearbyCell();
            }
        }

        if (Random.value < IdleChance)
            wantMove = false;

        if (wantMove)
            mc.moveVector.x = 1;
        else
            mc.moveVector.x = 0;
    }

    public override void Pain()
    {
        painFrame = true;
    }

    public override void AlertByNoise()
    {
        if (!deaf)
            alert = true;
    }

    private bool MoveTowardsBreath()
    {
        Breath b = GameManager.Instance.Player[0].playerBreath.GetBreath(owner.cell);

        if (b == null) return false;
        if (b.direction == 0) return false;

        Vec2I d = Vec2I.directions[b.direction];
        Vec2I target = owner.cell + d;

        if (AI.GetHeat(target).x >= 1f)
            return false;

        if (AI.HasLedge(owner.cell, target))
            return false;

        wantDirection = new Vector3(d.x, 0, d.y).normalized;
        wantMove = true;

        return true;
    }

    private bool MoveToRandomClosestBreath()
    {
        int lowest = int.MaxValue;
        List<Breath> bestBreaths = new List<Breath>();
        foreach (Breath b in GameManager.Instance.Player[0].playerBreath.GetNearbyBreaths(owner.cell, 1))
        {
            if (AI.GetHeat(b.position).x >= 1f)
                continue;

            if (AI.HasLedge(owner.cell, b.position))
                continue;

            if (b.steps < lowest)
            {
                bestBreaths.Clear();
                bestBreaths.Add(b);
                lowest = b.steps;
            }
            else if (b.steps == lowest)
                bestBreaths.Add(b);
        }

        if (bestBreaths.Count == 0)
            return false;

        Vec2I d = Vec2I.directions[AxMath.RogueDirection(owner.cell, bestBreaths[Random.Range(0, bestBreaths.Count)].position)];
        wantDirection = new Vector3(d.x, 0, d.y).normalized;
        wantMove = true;

        return true;
    }

    private bool MoveToRandomNearbyCell()
    {
        List<Vec2I> possible = new List<Vec2I>();

        foreach (Vec2I neighbor in owner.cell.neighbors)
        {
            if (!AI.CanPath(neighbor))
                continue;

            if (AI.GetHeat(neighbor).x >= 1f)
                continue;

            if (AI.HasLedge(owner.cell, neighbor))
                continue;

            possible.Add(neighbor);
        }

        if (possible.Count == 0)
            return false;

        Vec2I d = Vec2I.directions[AxMath.RogueDirection(owner.cell, possible[Random.Range(0, possible.Count)])];
        wantDirection = new Vector3(d.x, 0, d.y).normalized;
        wantMove = true;

        return true;
    }
}