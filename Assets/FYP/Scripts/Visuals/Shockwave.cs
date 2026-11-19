using UnityEngine;

namespace FYP
{
    public class Shockwave : MonoBehaviour
    {
        public float size = 8f;
        public float time = 0.35f;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            float s = Mathf.Lerp(0.5f, size, 1 - (1 - k) * (1 - k));
            transform.localScale = new Vector3(s, 0.02f, s);
            if (k >= 1)
                Destroy(gameObject);
        }
    }
}
