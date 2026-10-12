using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP
{
    public class FYPCamera : MonoBehaviour
    {
        public FYPPlayerController player;

        [Header("Orbit")]
        public float distance = 9f;
        public float height = 1.5f;
        public float mouseSensitivity = 0.15f;
        public float minPitch = -20f;
        public float maxPitch = 70f;

        [Header("Lock-on")]
        public float lockedPitch = 15f;
        public float lockTurnSpeed = 5f;
        // look a bit higher so the big boss stays on screen
        public float lockLookHeight = 4f;

        [Header("When the player is hit")]
        public float shakeStrength = 0.4f;
        public float shakeTime = 0.3f;
        public Color flashColor = new Color(0.8f, 0f, 0f, 0.45f);
        public float flashTime = 0.5f;

        float yaw;
        float pitch = 20f;
        float shakeTimer;
        float flashTimer;

        // start, not awake: player.health is set in the player's awake
        void Start()
        {
            yaw = player.transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            player.health.onDamaged += OnPlayerDamaged;
        }

        // player hit: start shake and flash
        void OnPlayerDamaged(float amount)
        {
            shakeTimer = shakeTime;
            flashTimer = flashTime;
        }

        // red full-screen flash that fades out
        void OnGUI()
        {
            if (flashTimer <= 0)
                return;

            Color color = flashColor;
            color.a = flashColor.a * (flashTimer / flashTime);
            GUI.color = color;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // place the camera after everything moved
        void LateUpdate()
        {
            // escape shows the mouse in unity editor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool wasLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = wasLocked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = wasLocked;
            }

            Vector3 pivot = player.transform.position + Vector3.up * height;

            if (player.isLockedOn)
            {
                // move slowly behind the player, looking at the boss
                Vector3 toBoss = player.lockTarget.position - player.transform.position;
                float bossYaw = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
                yaw = Mathf.LerpAngle(yaw, bossYaw, lockTurnSpeed * Time.deltaTime);
                pitch = Mathf.Lerp(pitch, lockedPitch, lockTurnSpeed * Time.deltaTime);
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouse = Mouse.current.delta.ReadValue();
                yaw += mouse.x * mouseSensitivity;
                pitch -= mouse.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            transform.position = pivot - Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * distance;

            if (player.isLockedOn)
            {
                Vector3 lookPoint = Vector3.Lerp(pivot, player.lockTarget.position, 0.35f);
                if (lookPoint.y < lockLookHeight)
                    lookPoint.y = lockLookHeight;
                transform.LookAt(lookPoint);
            }
            else
            {
                transform.LookAt(pivot);
            }

            if (flashTimer > 0)
                flashTimer -= Time.deltaTime;

            // shake is strong right after the hit and gets weaker
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                transform.position += Random.insideUnitSphere * shakeStrength * (shakeTimer / shakeTime);
            }
        }
    }
}
