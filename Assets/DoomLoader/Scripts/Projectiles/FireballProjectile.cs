using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    public GameObject owner;
    public float speed = 4f;
    public int damageMin = 3;
    public int damageMax = 24;
    public float explosionRadius = 2f;
    public DamageType damageType = DamageType.Generic;

    public GameObject OnDeathSpawn;
    public string _onDeathSound;

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
                    d.Damage(Random.Range(damageMin, damageMax + 1), damageType, owner);
            }

            if (OnDeathSpawn != null)
            {
                GameObject go = Instantiate(OnDeathSpawn);
                go.transform.position = transform.position;
            }

            if (!string.IsNullOrEmpty(_onDeathSound))
                GameManager.Create3DSound(transform.position, _onDeathSound, 5f);

            Destroy(gameObject);

            return;
        }
        
        transform.position = transform.position + transform.forward * speed * Time.deltaTime;
	}
}
