using UnityEngine;

namespace FYP
{
    public class Projectile : MonoBehaviour
    {
        public Health owner;
        public float speed = 20f;
        public float range = 30f;
        public float radius = 0.35f;
        public float damage;
        public float poiseDamage;
        public System.Action<float> onResolved;

        float travelled;

        void Update()
        {
            float step = speed * Time.deltaTime;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, step, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    continue;
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target == owner)
                    continue;
                float dealt = 0;
                if (target != null && target.TakeDamage(damage))
                {
                    dealt = damage;
                    BossController boss = target.GetComponent<BossController>();
                    if (boss != null)
                        boss.TakePoiseDamage(poiseDamage);
                }
                Finish(dealt);
                return;
            }

            transform.position += transform.forward * step;
            travelled += step;
            if (travelled >= range)
                Finish(0);
        }

        void Finish(float dealt)
        {
            if (onResolved != null)
                onResolved(dealt);
            Destroy(gameObject);
        }
    }
}
