using UnityEngine;

namespace FYP
{
    public class RockFlight : MonoBehaviour
    {
        public Vector3 from;
        public Vector3 to;
        public float time;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(time, 0.01f));
            Vector3 p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * 2f;
            transform.position = p;
            transform.Rotate(400 * Time.deltaTime, 170 * Time.deltaTime, 0);
            if (k >= 1)
            {
                CombatFx.Dust(to, 2.5f);
                Destroy(gameObject);
            }
        }
    }
}
