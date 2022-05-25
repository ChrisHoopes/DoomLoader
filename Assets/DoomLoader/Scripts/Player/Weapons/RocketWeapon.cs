using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketWeapon : PlayerWeapon
{
    public float dispersion = .02f;

    public RocketProjectile AttackProjectile;
    public Vector3 spawnPos;
    public float attackHappenTime = .25f;
    bool attacking = false;

    protected override void OnUpdate()
    {
        if (PlayerInfo.Instance.Ammo[2] <= 0 && fireTime < .1f)
            putAway = true;

        if (attacking)
            if (fireTime < attackHappenTime)
            {
                attacking = false;
                Vector3 d = Camera.main.transform.forward;
                Vector2 r = Random.insideUnitCircle * dispersion;
                d += Camera.main.transform.right * r.x + Camera.main.transform.up * r.y;
                d.Normalize();

                RocketProjectile rocket = Instantiate(AttackProjectile);
                rocket.owner = PlayerInfo.Instance.gameObject;
                rocket.transform.position = Camera.main.transform.position + spawnPos;
                rocket.transform.rotation = Quaternion.LookRotation(d);
                rocket.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
            }
    }

    public override bool Fire()
    {
        if (LowerAmount > .2f)
            return false;

        //small offset to allow continous fire animation
        if (fireTime > 0.05f)
            return false;

        if (PlayerInfo.Instance.Ammo[2] <= 0)
            return false;

        PlayerInfo.Instance.Ammo[2]--;

        if (Options.UseMuzzleLight)
            if (muzzleLight != null)
            {
                muzzleLight.intensity = 1;
                muzzleLight.enabled = true;
            }

        if (muzzleObject != null)
        {
            muzzleObject.SetActive(true);
            muzzleTimer = _muzzleTime;

            muzzleFrameIndex = 0;
            muzzleFrameTime = 0f;

            if (muzzleAnimation.Length > 0)
                SetMuzzleSprite(frames[muzzleAnimation[0]].texture);
        }

        //maximum fire rate 20/s, unless you use negative number (please don't)
        fireTime = _fireRate + .05f;
        frameTime = 0f;
        animationFrameIndex = 0;

        if (fireAnimation.Length > 0)
            currentFrame = fireAnimation[0];

        SetSprite();

        if (Sounds.Length > 0)
        {
            audioSource.clip = Sounds[0];
            audioSource.Play();
        }

        attacking = true;

        return true;
    }
}
