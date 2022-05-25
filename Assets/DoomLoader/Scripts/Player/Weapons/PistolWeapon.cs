using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolWeapon : PlayerWeapon
{
    public float dispersion = .02f;
    public float maxRange = 400f;

    protected override void OnUpdate()
    {
        if (PlayerInfo.Instance.Ammo[0] <= 0 && fireTime < .1f)
            putAway = true;
    }

    public override bool Fire()
    {
        if (LowerAmount > .2f)
            return false;

        //small offset to allow continous fire animation
        if (fireTime > 0.05f)
            return false;

        if (PlayerInfo.Instance.Ammo[0] <= 0)
            return false;

        PlayerInfo.Instance.Ammo[0]--;

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

        Vector3 d = Camera.main.transform.forward;
        Vector2 r = Random.insideUnitCircle * dispersion;
        d += Camera.main.transform.right * r.x + Camera.main.transform.up * r.y;
        d.Normalize();
        Ray ray = new Ray(Camera.main.transform.position, d);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRange, ~((1 << 9) | (1 << 10) | (1 << 14)), QueryTriggerInteraction.Ignore))
        {
            Damageable target = hit.collider.gameObject.GetComponent<Damageable>();
            if (target != null)
            {
                target.Damage(Random.Range(DamageMin, DamageMax + 1), DamageType.Generic, PlayerControls.Instance.gameObject);

                if (target.Bleed)
                {
                    GameObject blood = Instantiate(GameManager.Instance.BloodDrop);
                    blood.transform.position = hit.point - ray.direction * .2f;
                }
                else
                {
                    GameObject puff = Instantiate(GameManager.Instance.BulletPuff);
                    puff.transform.position = hit.point - ray.direction * .2f;
                }
            }
            else
            {
                GameObject puff = Instantiate(GameManager.Instance.BulletPuff);
                puff.transform.position = hit.point - ray.direction * .2f;
            }
        }

        return true;
    }
}
