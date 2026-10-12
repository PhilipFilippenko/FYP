using System.Collections.Generic;
using UnityEngine;

namespace FYP
{
    // white flash and damage numbers on hit
    public class DamageFeedback : MonoBehaviour
    {
        public Color flashColor = Color.white;
        public float flashTime = 0.12f;

        [Header("Damage numbers")]
        public float numberHeight = 2f;
        public float numberLifetime = 0.9f;
        public float numberRiseSpeed = 1.5f;

        public bool isFlashing;

        // one floating number
        class DamageNumber
        {
            public float amount;
            public Vector3 position;
            public float age;
        }

        Health health;
        Renderer body;
        Color normalColor;
        float flashTimer;
        List<DamageNumber> numbers = new List<DamageNumber>();

        // remember color, subscribe
        void Start()
        {
            health = GetComponent<Health>();
            body = GetComponent<Renderer>();
            normalColor = body.material.color;
            health.onDamaged += OnDamaged;
        }

        // start flash, add a number
        void OnDamaged(float amount)
        {
            flashTimer = flashTime;

            DamageNumber number = new DamageNumber();
            number.amount = amount;
            // move each number a bit at random so fast hits do not overlap
            number.position = transform.position + Vector3.up * numberHeight + Random.insideUnitSphere * 0.4f;
            numbers.Add(number);
        }

        // run flash timer, age numbers
        void Update()
        {
            if (flashTimer > 0)
            {
                flashTimer -= Time.deltaTime;
                body.material.color = flashColor;
                isFlashing = true;
            }
            else if (isFlashing)
            {
                body.material.color = normalColor;
                isFlashing = false;
            }

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                numbers[i].age += Time.deltaTime;
                if (numbers[i].age > numberLifetime)
                    numbers.RemoveAt(i);
            }
        }

        // draw numbers above the target
        void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            foreach (DamageNumber number in numbers)
            {
                Vector3 worldPosition = number.position + Vector3.up * number.age * numberRiseSpeed;
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
                if (screenPosition.z < 0)
                    continue;

                style.normal.textColor = new Color(1f, 0.85f, 0.2f, 1f - number.age / numberLifetime);
                // on screen y goes up, in gui y goes down, so flip it
                Rect rect = new Rect(screenPosition.x - 40, Screen.height - screenPosition.y - 15, 80, 30);
                GUI.Label(rect, number.amount.ToString("0"), style);
            }
        }
    }
}
