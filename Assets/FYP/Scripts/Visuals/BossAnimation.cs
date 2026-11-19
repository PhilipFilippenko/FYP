using UnityEngine;

namespace FYP
{
    public class BossAnimation : MonoBehaviour
    {
        public Animator animator;
        public float fade = 0.25f;

        [Header("Clips")]
        public string idleClip = "idle";
        public string victoryIdleClip = "idle_2";
        public string walkClip = "walk";
        public string walkBackClip = "walk_back";
        public string strafeLeftClip = "strafe_left";
        public string strafeRightClip = "strafe_right";
        public string turnLeftClip = "turn_left";
        public string turnRightClip = "turn_right";
        public string hitClip = "hit";
        public string deathClip = "death";
        public string staggerClip = "stagger";
        public string introClip = "intro";
        public float walkClipSpeed = 3f;
        public float turnInPlaceSpeed = 25f;

        [Header("Thrown rock")]
        public string rockHandBone = "LeftHand";
        public int rockGrabFrame = 17;
        public float rockSize = 1.3f;
        public Material rockMaterial;

        BossController boss;
        Health health;
        string current;
        float oneShotUntil;
        string pending;
        float pendingSince;
        const float switchDelay = 0.2f;
        float lastYaw;
        float yawSpeed;
        Transform rockHand;
        GameObject heldRock;
        float grabAt = -1;

        void Awake()
        {
            boss = GetComponent<BossController>();
            health = GetComponent<Health>();
            boss.onAttackStarted += OnAttackStarted;
            boss.onRockThrown += OnRockThrown;
            boss.onStaggered += OnStaggered;
            boss.onIntroStarted += OnIntroStarted;
            boss.attempts.onAttemptStarted += OnAttemptStarted;
            health.onDamaged += OnDamaged;
            health.onDied += OnDied;
            foreach (Transform t in animator.GetComponentsInChildren<Transform>())
            {
                if (t.name == rockHandBone)
                    rockHand = t;
            }
            lastYaw = transform.eulerAngles.y;
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            yawSpeed = Mathf.Lerp(yawSpeed, Mathf.DeltaAngle(lastYaw, transform.eulerAngles.y) / dt, 10 * Time.deltaTime);
            lastYaw = transform.eulerAngles.y;

            if (grabAt > 0 && Time.time >= grabAt)
            {
                grabAt = -1;
                GrabRock();
            }

            if (health.IsDead() || Time.time < oneShotUntil)
                return;
            if (boss.state == BossState.Dormant)
            {
                animator.SetFloat("Speed", 0);
                if (current != introClip)
                    animator.Play(introClip, 0, 0);
                current = introClip;
                return;
            }
            if (boss.state == BossState.Walking)
                Play(walkClip, boss.profile.walkSpeed / walkClipSpeed);
            else if (boss.state == BossState.Strafing)
                Play(boss.strafeDirection > 0 ? strafeRightClip : strafeLeftClip, boss.profile.strafeSpeed / walkClipSpeed);
            else if (boss.state == BossState.BackingOff)
                Play(walkBackClip, boss.profile.strafeSpeed / walkClipSpeed);
            else if (boss.state == BossState.Idle && !OneShotStillPlaying())
            {
                if (boss.player.health.IsDead())
                    Play(victoryIdleClip, 1);
                else if (Mathf.Abs(yawSpeed) > turnInPlaceSpeed)
                    Play(yawSpeed < 0 ? turnLeftClip : turnRightClip, 1);
                else
                    Play(idleClip, 1);
            }
        }

        bool OneShotStillPlaying()
        {
            if (current == idleClip || current == walkClip || current == walkBackClip || current == strafeLeftClip
                || current == strafeRightClip || current == turnLeftClip || current == turnRightClip || current == victoryIdleClip)
                return false;
            AnimatorStateInfo s = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            return s.IsName(current) && s.normalizedTime < 0.97f;
        }

        void OnAttemptStarted(int attempt)
        {
            oneShotUntil = 0;
            grabAt = -1;
            if (heldRock != null)
                Destroy(heldRock);
            animator.speed = 1;
            animator.SetFloat("Speed", 1);
            animator.Play(idleClip, 0, 0);
            current = idleClip;
        }

        void OnAttackStarted(int index)
        {
            BossAttack attack = boss.profile.attacks[index];
            string clip = string.IsNullOrEmpty(attack.clip) ? attack.name.ToLower() : attack.clip;
            animator.SetFloat("Speed", 1);
            animator.CrossFadeInFixedTime(clip, attack.chainOnly ? 0.05f : fade, 0, 0);
            current = clip;
            pending = null;
            oneShotUntil = Time.time + attack.windup + attack.activeTime + attack.recovery;
            if (attack.kind == BossAttackKind.Throw)
                grabAt = Time.time + (rockGrabFrame - 1) / 30f;
        }

        void GrabRock()
        {
            if (heldRock != null)
                Destroy(heldRock);
            heldRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heldRock.name = "BossRock";
            Destroy(heldRock.GetComponent<Collider>());
            heldRock.transform.localScale = new Vector3(1f, 0.8f, 0.9f) * rockSize;
            if (rockMaterial != null)
                heldRock.GetComponent<Renderer>().material = rockMaterial;
            if (rockHand != null)
            {
                heldRock.transform.SetParent(rockHand, false);
                heldRock.transform.localPosition = Vector3.zero;
            }
            else
                heldRock.transform.position = transform.position + Vector3.up * 6f;
        }

        void OnRockThrown(Vector3 from, Vector3 to, float flight)
        {
            if (heldRock == null)
                GrabRock();
            GameObject rock = heldRock;
            heldRock = null;
            Vector3 start = rock.transform.position;
            rock.transform.SetParent(null, true);
            RockFlight f = rock.AddComponent<RockFlight>();
            f.from = start;
            f.to = to;
            f.time = flight;
        }

        void OnStaggered()
        {
            grabAt = -1;
            if (heldRock != null)
                Destroy(heldRock);
            AnimationClip c = FindClip(staggerClip);
            animator.SetFloat("Speed", c != null ? c.length / boss.profile.staggerTime : 1);
            animator.CrossFadeInFixedTime(staggerClip, 0.1f, 0, 0);
            current = staggerClip;
            oneShotUntil = Time.time + boss.profile.staggerTime;
        }

        void OnIntroStarted()
        {
            AnimationClip c = FindClip(introClip);
            animator.SetFloat("Speed", c != null ? c.length / boss.profile.introTime : 1);
            animator.Play(introClip, 0, 0);
            current = introClip;
            oneShotUntil = Time.time + boss.profile.introTime;
        }

        void OnDamaged(float amount)
        {
            if (health.IsDead())
                return;
            BossState s = boss.state;
            if (s != BossState.Idle && s != BossState.Walking && s != BossState.Strafing && s != BossState.BackingOff)
                return;
            Play(hitClip, 1, true);
            oneShotUntil = Time.time + 0.6f;
        }

        void OnDied()
        {
            Play(deathClip, 1, true);
        }

        void Play(string clip, float speed, bool restart = false)
        {
            animator.SetFloat("Speed", speed);
            if (clip == current && !restart)
            {
                pending = null;
                return;
            }
            bool fromLoop = current == idleClip || current == walkClip || current == walkBackClip || current == strafeLeftClip
                || current == strafeRightClip || current == turnLeftClip || current == turnRightClip || current == victoryIdleClip;
            if (!restart && fromLoop)
            {
                if (clip != pending)
                {
                    pending = clip;
                    pendingSince = Time.time;
                    return;
                }
                if (Time.time - pendingSince < switchDelay)
                    return;
            }
            pending = null;
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
