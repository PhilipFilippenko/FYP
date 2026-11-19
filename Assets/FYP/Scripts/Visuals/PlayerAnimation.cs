using UnityEngine;

namespace FYP
{
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator animator;
        public float fade = 0.12f;
        public float attackToAttackFade = 0.15f;

        [Header("Locomotion (clip name and the speed it shows at 1x, m/s)")]
        public string idleClip = "idle";
        public string forwardClip = "run_fwd";
        public float forwardClipSpeed = 2.9f;
        public string backClip = "run_bkw";
        public float backClipSpeed = 2f;
        public string leftClip = "strafe_left";
        public string rightClip = "strafe_right";
        public float strafeClipSpeed = 1.85f;

        [Header("Turning (clip and the degrees it turns, measured from the clips)")]
        public string turnLeftClip = "turn_left";
        public float turnLeftDegrees = -91f;
        public string turnRightClip = "turn_right";
        public float turnRightDegrees = 98f;
        public string turn180Clip = "turn_180";
        public float turn180Degrees = 189f;
        public float turnClipSpeed = 1.2f;
        public string runTurnClip = "run_turn_180";
        public float runTurnDegrees = 182f;
        public float runTurnClipSpeed = 1.6f;

        [Header("Idle")]
        public string[] idleVariants = { "idle_2", "idle_3" };
        public float idleVariantAfter = 8f;

        [Header("Other")]
        public Transform hips;
        public string hitClip = "impact";
        public string heavyHitClip = "impact_2";
        public string deathClip = "impact_2";

        FYPPlayerController player;
        Health health;
        string current;
        Vector3 lastPosition;
        Vector3 velocity;
        float lastYaw;
        bool wasBusy;
        bool wasCharging;
        bool dodgeChosen;
        float oneShotUntil;
        float attackClipEnd;
        bool attackPlaying;
        float idleSince;
        int nextIdleVariant;
        bool turning;
        float turnStartYaw;
        Vector3 hipsRest;
        string pending;
        float pendingSince;
        const float switchDelay = 0.15f;

        void Awake()
        {
            player = GetComponent<FYPPlayerController>();
            health = GetComponent<Health>();
            player.onActionPerformed += OnAction;
            player.onStunned += OnStunned;
            health.onDied += OnDied;
            lastPosition = transform.position;
            lastYaw = transform.eulerAngles.y;
            if (hips != null)
                hipsRest = hips.localPosition;
        }

        void LateUpdate()
        {
            if (turning && Time.time >= oneShotUntil)
                turning = false;
            float modelYaw = turning ? Mathf.DeltaAngle(transform.eulerAngles.y, turnStartYaw) : 0;
            animator.transform.localRotation = Quaternion.Euler(0, modelYaw, 0);
            if (hips == null)
                return;
            Vector3 p = hips.localPosition;
            hips.localPosition = new Vector3(hipsRest.x, p.y, hipsRest.z);
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 step = (transform.position - lastPosition) / dt;
            step.y = 0;
            if (step.magnitude > 30)
                step = Vector3.zero;
            velocity = Vector3.Lerp(velocity, step, 15 * Time.deltaTime);
            lastPosition = transform.position;
            if (wasBusy && !player.isBusy)
                velocity = Vector3.zero;
            wasBusy = player.isBusy;
            float previousYaw = lastYaw;
            lastYaw = transform.eulerAngles.y;

            if (health.IsDead())
                return;

            if (player.state == CombatState.Dodge)
            {
                if (!dodgeChosen && step.sqrMagnitude > 0.01f)
                {
                    dodgeChosen = true;
                    PlayDodge(step);
                }
                return;
            }
            if (attackPlaying && player.currentAttack != null)
            {
                if (player.isCharging || player.inHitStop)
                {
                    animator.SetFloat("Speed", 0);
                    attackClipEnd += Time.deltaTime;
                    wasCharging = true;
                }
                else if (wasCharging)
                {
                    animator.SetFloat("Speed", player.currentAttack.speed);
                    wasCharging = false;
                }
            }
            if (player.isBusy || Time.time < oneShotUntil)
                return;

            if (attackPlaying)
            {
                bool moving = velocity.magnitude >= 0.3f;
                if (!moving && Time.time < attackClipEnd)
                    return;
                attackPlaying = false;
                animator.SetFloat("Speed", 1);
                animator.CrossFadeInFixedTime(moving ? ClipFor(transform.InverseTransformDirection(velocity)) : idleClip, moving ? 0.2f : 0.3f, 0, 0);
                current = moving ? ClipFor(transform.InverseTransformDirection(velocity)) : idleClip;
                pending = null;
                idleSince = Time.time;
                return;
            }
            if (StartTurnIfNeeded(step, previousYaw))
                return;
            PlayLocomotion();
        }

        bool StartTurnIfNeeded(Vector3 step, float previousYaw)
        {
            Vector3 local = transform.InverseTransformDirection(step);
            if (!player.isLockedOn && step.magnitude > 2f && local.z < -Mathf.Abs(local.x))
            {
                StartTurn(runTurnClip, runTurnClipSpeed, previousYaw);
                return true;
            }
            if (player.isLockedOn && step.magnitude < 0.3f && player.lockTarget != null)
            {
                Vector3 toTarget = player.lockTarget.position - transform.position;
                toTarget.y = 0;
                float angle = Mathf.DeltaAngle(previousYaw, Quaternion.LookRotation(toTarget).eulerAngles.y);
                if (Mathf.Abs(angle) > 135)
                    StartTurn(turn180Clip, turnClipSpeed, previousYaw);
                else if (angle < -60)
                    StartTurn(turnLeftClip, turnClipSpeed, previousYaw);
                else if (angle > 60)
                    StartTurn(turnRightClip, turnClipSpeed, previousYaw);
                else
                    return false;
                return true;
            }
            return false;
        }

        void StartTurn(string clip, float speed, float startYaw)
        {
            AnimationClip c = FindClip(clip);
            animator.SetFloat("Speed", speed);
            animator.CrossFadeInFixedTime(clip, 0.05f, 0, 0);
            current = clip;
            pending = null;
            turning = true;
            turnStartYaw = startYaw;
            oneShotUntil = Time.time + (c != null ? c.length / speed : 0.5f);
        }

        void OnAction(PlayerAction action)
        {
            oneShotUntil = 0;
            AttackSettings attack = player.currentAttack;
            if (attack == null)
            {
                dodgeChosen = false;
                attackPlaying = false;
                return;
            }
            bool continues = attack.continuesPrevious && attackPlaying && Mathf.Abs(Time.time - attackClipEnd) < 0.1f;
            float blend = continues ? 0.03f : attackPlaying ? attackToAttackFade : fade;
            animator.SetFloat("Speed", attack.speed);
            animator.CrossFadeInFixedTime(attack.clip, blend, 0, 0);
            current = attack.clip;
            pending = null;
            attackPlaying = true;
            AnimationClip c = FindClip(attack.clip);
            attackClipEnd = Time.time + (c != null ? c.length / attack.speed : 0);
        }

        void PlayDodge(Vector3 direction)
        {
            string clip = ClipFor(transform.InverseTransformDirection(direction));
            AnimationClip c = FindClip(clip);
            float speed = c != null ? c.length / player.dodgeDuration : 1;
            animator.SetFloat("Speed", speed);
            animator.CrossFadeInFixedTime(clip, 0.1f, 0, 0);
            current = clip;
        }

        void PlayLocomotion()
        {
            float speed = velocity.magnitude;
            if (speed < 0.3f)
            {
                if (current == idleClip && Time.time - idleSince > idleVariantAfter && idleVariants.Length > 0)
                {
                    string variant = idleVariants[nextIdleVariant % idleVariants.Length];
                    nextIdleVariant++;
                    AnimationClip c = FindClip(variant);
                    animator.SetFloat("Speed", 1);
                    animator.CrossFadeInFixedTime(variant, 0.4f, 0, 0);
                    current = variant;
                    oneShotUntil = Time.time + (c != null ? c.length : 3);
                    idleSince = oneShotUntil;
                    return;
                }
                SetClip(idleClip, 1);
                return;
            }

            idleSince = Time.time;
            Vector3 local = transform.InverseTransformDirection(velocity);
            string clip = ClipFor(local);
            float clipSpeed = clip == forwardClip ? forwardClipSpeed : clip == backClip ? backClipSpeed : strafeClipSpeed;
            SetClip(clip, speed / clipSpeed);
        }

        string ClipFor(Vector3 local)
        {
            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return local.z >= 0 ? forwardClip : backClip;
            return local.x < 0 ? leftClip : rightClip;
        }

        void OnStunned(float amount)
        {
            if (health.IsDead())
                return;
            bool heavy = amount >= player.heavyHitDamage;
            string clip = heavy ? heavyHitClip : hitClip;
            AnimationClip c = FindClip(clip);
            float stun = heavy ? player.heavyHitStun : player.hitStun;
            animator.SetFloat("Speed", c != null ? c.length / stun : 1);
            animator.CrossFadeInFixedTime(clip, 0.05f, 0, 0);
            current = clip;
            attackPlaying = false;
            oneShotUntil = Time.time + stun;
        }

        void OnDied()
        {
            animator.SetFloat("Speed", 1);
            animator.CrossFadeInFixedTime(deathClip, 0.05f, 0, 0);
            current = deathClip;
            attackPlaying = false;
        }

        void SetClip(string clip, float speed)
        {
            animator.SetFloat("Speed", speed);
            if (clip == current)
            {
                pending = null;
                return;
            }
            if (clip != pending)
            {
                pending = clip;
                pendingSince = Time.time;
            }
            bool fromLoop = current == idleClip || current == forwardClip || current == backClip || current == leftClip || current == rightClip
                || current == turnLeftClip || current == turnRightClip;
            if (fromLoop && Time.time - pendingSince < switchDelay)
                return;
            bool keepPhase = fromLoop && clip != idleClip && current != idleClip && current != turnLeftClip && current != turnRightClip;
            float phase = keepPhase ? Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1) : 0;
            animator.CrossFade(clip, fade, 0, phase);
            current = clip;
            pending = null;
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
