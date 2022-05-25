using System.Collections.Generic;
using UnityEngine;

public class StupidWalkerBehavior : BehaviorBase
{
    bool painFrame;

    MonsterController mc;

    public override void Init()
    {
        mc = owner.GetComponent<MonsterController>();
        if (mc == null)
            Debug.LogError("StupidWalkerBehavior: Init: Could not get component \"MonsterController\" from owner \"" + owner.thingName + "\"");

        decisionTime = Random.Range(0f, .5f);
    }

    //Vector3 lastpos;

    float decisionTime;
    Vector3 wantDirection;

    public override void Tick()
    {
        if (!alert)
        {
            if (decisionTime > 0f)
            {
                decisionTime -= Time.deltaTime;
                return;
            }

            decisionTime = .5f;

            int distance = AxMath.WeightedDistance(owner.cell, GameManager.Instance.Player[0].cell);

            if (distance > 400)
                return;

            Vector3 eyePos = owner.transform.position + Vector3.up * 1.55f;
            Vector3 toPlayer = ((GameManager.Instance.Player[0].transform.position + Random.onUnitSphere * .5f) - eyePos).normalized;

            if (Vector3.Dot(owner.transform.forward, toPlayer) < -.15f)
                return;

            Ray ray = new Ray(eyePos, toPlayer);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 400, ~((1 << 9) | (1 << 11) | (1 << 14)), QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<PlayerThing>() != null)
                    alert = true;

            if (!alert)
                return;
        }

        /*float mag = (lastpos - owner.transform.position).magnitude;
        if (mag > 0.001f && mag < .02f)
            Debug.Log("hug");
        lastpos = owner.transform.position;*/

        if (painFrame)
        {
            mc.moveVector = Vector3.zero;
            painFrame = false;
            return;
        }

        if (wantDirection != Vector3.zero)
        {
            mc.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(mc.transform.forward, wantDirection, Time.deltaTime * mc.turnSpeed), Vector3.up);
            mc.moveVector = mc.transform.forward;
        }

        if (decisionTime > 0f)
        {
            decisionTime -= Time.deltaTime;
            return;
        }

        decisionTime = Random.Range(.6f, 2f);

        wantDirection = Vector3.zero;

        if (!MoveToRandomClosestBreath())
            if (!MoveTowardsBreath())
                MoveToRandomNearbyCell();
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

        if (AI.HasLedge(owner.cell, target))
            return false;

        wantDirection = new Vector3(d.x, 0, d.y).normalized;

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

        return true;
    }
}
