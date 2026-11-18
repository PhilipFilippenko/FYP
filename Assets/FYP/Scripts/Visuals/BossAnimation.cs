// AI assistance:
//   This script was written with help from
//   Anthropic (2026) Claude [Large language model].
//   Available at: https://claude.ai (Accessed: 18 November 2026).

using UnityEngine;

namespace FYP
{
    // plays the knell model's clips from the boss state. visual only: gameplay never reads it
    public class BossAnimation : MonoBehaviour
    {
        public Animator animator;
        public float fade = 0.25f;

        [Header("Clips")]
        public string idleClip = "idle";
        public string walkClip = "walk";
        public string hitClip = "hit";
        public string deathClip = "death";
        // walk clip played at 1x matches this speed
        public float walkClipSpeed = 3f;

        // frame where each attack clip hits (30 fps, from Knell_animation_events.json).
        // the clip is sped up or slowed down so this frame lands when the windup ends
        [Header("Attack hit frames, in profile order")]
        public int[] attackHitFrames = { 36, 20, 23, 41 };

        BossController boss;
        Health health;
        string current;
        float oneShotUntil;

        // find links, subscribe
        void Awake()
        {
            boss = GetComponent<BossController>();
            health = GetComponent<Health>();
            boss.onAttackStarted += OnAttackStarted;
            boss.attempts.onAttemptStarted += OnAttemptStarted;
            health.onDamaged += OnDamaged;
            health.onDied += OnDied;
        }

        // idle or walk when no attack, hit or death is playing
        void Update()
        {
            if (health.IsDead() || Time.time < oneShotUntil)
                return;
            if (boss.state == BossState.Walking)
                Play(walkClip, boss.profile.walkSpeed / walkClipSpeed);
            else if (boss.state == BossState.Idle)
                Play(idleClip, 1);
        }

        // new attempt: snap back to idle
        void OnAttemptStarted(int attempt)
        {
            oneShotUntil = 0;
            animator.speed = 1;
            animator.SetFloat("Speed", 1);
            animator.Play(idleClip, 0, 0);
            current = idleClip;
        }

        // attack clip named like the attack, timed to the windup
        void OnAttackStarted(int index)
        {
            BossAttack attack = boss.profile.attacks[index];
            float hitTime = (attackHitFrames[index] - 1) / 30f;
            float speed = hitTime / Mathf.Max(attack.windup, 0.05f);
            Play(attack.name.ToLower(), speed, true);
            // hold the attack clip until the boss is back to idle
            oneShotUntil = Time.time + attack.windup + attack.activeTime + attack.recovery;
        }

        // flinch only when not attacking, a giant does not stop a swing
        void OnDamaged(float amount)
        {
            if (health.IsDead() || (boss.state != BossState.Idle && boss.state != BossState.Walking))
                return;
            Play(hitClip, 1, true);
            oneShotUntil = Time.time + 0.6f;
        }

        // death clip, stays in its last pose
        void OnDied()
        {
            Play(deathClip, 1, true);
        }

        // crossfade to a clip, restart it if asked
        void Play(string clip, float speed, bool restart = false)
        {
            animator.SetFloat("Speed", speed);
            if (clip == current && !restart)
                return;
            animator.CrossFadeInFixedTime(clip, fade, 0, 0);
            current = clip;
        }
    }
}
