using UnityEngine;

namespace DoomLoader.Weapons
{
    public class ChainsawWeapon : PlayerWeapon
    {
        public float dispersion = .02f;
        public float maxRange = 2f;

        public float _idleSoundTime = .1f;
        float idleSoundTime;

        protected override void OnUpdate()
        {
            if (LowerAmount < .5f)
                if (fireTime <= 0f)
                    if (idleSoundTime < 0f)
                    {
                        audioSource.clip = Sounds[1];
                        audioSource.Play();
                        idleSoundTime = _idleSoundTime;
                    }
                    else
                        idleSoundTime -= Time.deltaTime;
        }

        public override bool Fire()
        {
            if (LowerAmount > .2f)
                return false;

            //small offset to allow continous fire animation
            if (fireTime > 0.05f) return false;
            fireTime = _fireRate + .05f;

            frameTime = 0f;
            animationFrameIndex = 0;

            if (fireAnimation.Length > 0)
                currentFrame = fireAnimation[0];

            SetSprite();

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

                audioSource.clip = Sounds[2];
                audioSource.Play();
            }
            else
            {
                audioSource.clip = Sounds[0];
                audioSource.Play();
            }

            return true;
        }
    }
}
