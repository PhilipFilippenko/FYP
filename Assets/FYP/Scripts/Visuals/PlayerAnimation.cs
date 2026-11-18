// AI assistance:
//   This script was written with help from
//   Anthropic (2026) Claude [Large language model].
//   Available at: https://claude.ai (Accessed: 18 November 2026).

using UnityEngine;

namespace FYP
{
    // plays the vessel model's clips from the player state. visual only: gameplay never reads it
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator animator;
        public float fade = 0.12f;

        [Header("Locomotion (clip name and the speed it shows at 1x, m/s)")]
        public string idleClip = "idle";
        public string forwardClip = "run_fwd";
        public float forwardClipSpeed = 4f;
        public string backClip = "run_bkw";
        public float backClipSpeed = 3f;
        public string leftClip = "strafe_left";
        public string rightClip = "strafe_right";
        public float strafeClipSpeed = 2.2f;

        [Header("Attacks: clip, hit frame (30 fps), playback speed")]
        // light attacks in a row cycle through the three combo parts
        public string[] lightClips = { "combo_1", "combo_2", "combo_3" };
        public int[] lightHitFrames = { 25, 16, 17 };
        public float lightSpeed = 1.5f;
        public float comboWindow = 0.5f;
        public string heavyClip = "slash_atk_2";
        public int heavyHitFrame = 25;
        public float heavySpeed = 1f;

        [Header("Other")]
        // the mixamo clips walk the hips forward; the controller already moves the body, so the hips stay over it
        public Transform hips;
        public string hitClip = "impact";
        // the vessel has no death clip yet, so it falls back on the bigger impact and holds it
        public string deathClip = "impact_2";

        FYPPlayerController player;
        Health health;
        string current;
        Vector3 lastPosition;
        Vector3 velocity;
        int lightIndex;
        float lastAttackEnd = -10;
        bool wasBusy;
        bool dodgeChosen;
        float oneShotUntil;
        Vector3 hipsRest;

        // find links, subscribe
        void Awake()
        {
            player = GetComponent<FYPPlayerController>();
            health = GetComponent<Health>();
            player.onActionPerformed += OnAction;
            health.onDamaged += OnDamaged;
            health.onDied += OnDied;
            lastPosition = transform.position;
            if (hips != null)
                hipsRest = hips.localPosition;
        }

        // runs after the animator: keep the hips over the body, height and bob stay animated
        void LateUpdate()
        {
            if (hips == null)
                return;
            Vector3 p = hips.localPosition;
            hips.localPosition = new Vector3(hipsRest.x, p.y, hipsRest.z);
        }

        void Update()
        {
            // flat speed from how far the body moved, smoothed so steps do not flicker the clip
            Vector3 step = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            step.y = 0;
            // a new attempt teleports the body: not a real speed
            if (step.magnitude > 30)
                step = Vector3.zero;
            velocity = Vector3.Lerp(velocity, step, 15 * Time.deltaTime);
            lastPosition = transform.position;

            if (health.IsDead())
                return;

            // an attack or dodge just ended: start of the combo window
            if (wasBusy && !player.isBusy)
                lastAttackEnd = Time.time;
            wasBusy = player.isBusy;

            if (player.state == CombatState.Dodge)
            {
                if (!dodgeChosen && step.sqrMagnitude > 0.01f)
                {
                    dodgeChosen = true;
                    PlayDodge(step);
                }
                return;
            }
            if (player.isBusy || Time.time < oneShotUntil)
                return;
            PlayLocomotion();
        }

        // attack or dodge started
        void OnAction(PlayerAction action)
        {
            oneShotUntil = 0;
            if (action == PlayerAction.LightAttack)
            {
                // next combo part if the last attack ended a moment ago
                lightIndex = Time.time - lastAttackEnd < comboWindow ? (lightIndex + 1) % lightClips.Length : 0;
                PlayAttack(lightClips[lightIndex], lightHitFrames[lightIndex], lightSpeed, player.lightAttack.windup);
            }
            else if (action == PlayerAction.HeavyAttack)
            {
                lightIndex = lightClips.Length - 1;
                PlayAttack(heavyClip, heavyHitFrame, heavySpeed, player.heavyAttack.windup);
            }
            else
            {
                // dodge: the clip is picked on the first frame the body moves
                dodgeChosen = false;
            }
        }

        // start the clip late enough that its hit frame lands when the windup ends
        void PlayAttack(string clip, int hitFrame, float speed, float windup)
        {
            float startTime = Mathf.Max(0, (hitFrame - 1) / 30f - windup * speed);
            animator.SetFloat("Speed", speed);
            animator.CrossFadeInFixedTime(clip, fade, 0, startTime);
            current = clip;
        }

        // side step or run clip squeezed into the dodge time
        void PlayDodge(Vector3 direction)
        {
            string clip = ClipFor(transform.InverseTransformDirection(direction));
            AnimationClip c = FindClip(clip);
            float speed = c != null ? c.length / player.dodgeDuration : 1;
            animator.SetFloat("Speed", speed);
            animator.CrossFadeInFixedTime(clip, 0.05f, 0, 0);
            current = clip;
        }

        // idle, run or strafe from the real speed and direction
        void PlayLocomotion()
        {
            float speed = velocity.magnitude;
            if (speed < 0.3f)
            {
                SetClip(idleClip, 1);
                return;
            }
            Vector3 local = transform.InverseTransformDirection(velocity);
            string clip = ClipFor(local);
            float clipSpeed = clip == forwardClip ? forwardClipSpeed : clip == backClip ? backClipSpeed : strafeClipSpeed;
            SetClip(clip, speed / clipSpeed);
        }

        // forward, back, left or right by the biggest part of the direction
        string ClipFor(Vector3 local)
        {
            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return local.z >= 0 ? forwardClip : backClip;
            return local.x < 0 ? leftClip : rightClip;
        }

        // flinch when standing or walking, attacks and dodges are not interrupted
        void OnDamaged(float amount)
        {
            if (health.IsDead() || player.isBusy)
                return;
            animator.SetFloat("Speed", 1);
            animator.CrossFadeInFixedTime(hitClip, 0.05f, 0, 0);
            current = hitClip;
            oneShotUntil = Time.time + 0.45f;
        }

        // no death clip yet: play the big impact and hold its last pose
        void OnDied()
        {
            animator.SetFloat("Speed", 1);
            animator.CrossFadeInFixedTime(deathClip, 0.05f, 0, 0);
            current = deathClip;
        }

        // crossfade only when the clip changes, speed always follows
        void SetClip(string clip, float speed)
        {
            animator.SetFloat("Speed", speed);
            if (clip == current)
                return;
            animator.CrossFadeInFixedTime(clip, fade, 0, 0);
            current = clip;
        }

        AnimationClip FindClip(string clipName)
        {
            foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
                if (c.name == clipName)
                    return c;
            return null;
        }
    }
}
