using System.Collections;
using UnityEngine;

namespace FYP
{
    // what the boss is doing
    public enum BossState
    {
        Idle,
        Walking,
        Windup,
        Active,
        Recovery,
        Dormant,
        Intro,
        Staggered,
        Strafing,
        BackingOff
    }

    // moves the boss body: walks, turns and plays the attack the brain picked
    public class BossController : MonoBehaviour
    {
        public BossProfile profile;
        public BossBrain brain;
        public FYPPlayerController player;
        public AttemptManager attempts;

        [Header("State (read only)")]
        public BossState state;
        public int currentAttack = -1;
        public float poise;
        public int strafeDirection = 1;
        public bool criticalTaken;

        // sent when an attack starts, with its number in the profile
        public System.Action<int> onAttackStarted;
        // sent when an attack ends: attack name, result ("Hit", "Dodged" or "Missed"), damage
        public System.Action<string, string, float> onAttackResolved;
        public System.Action<Vector3, Vector3, float> onRockThrown;
        public System.Action onStaggered;
        public System.Action onIntroStarted;

        // internal
        Health health;
        float idleTimer;
        float lastPoiseHit;
        bool introDone;
        bool roared;

        // find health, subscribe
        void Awake()
        {
            health = GetComponent<Health>();
            attempts.onAttemptStarted += OnAttemptStarted;
            health.onDamaged += OnDamaged;
        }

        // stop everything for a new attempt
        void OnAttemptStarted(int attempt)
        {
            StopAllCoroutines();
            state = introDone ? BossState.Idle : BossState.Dormant;
            currentAttack = -1;
            idleTimer = profile.idleTime;
            poise = profile.poise;
            roared = false;
            brain.ResetForAttempt();
        }

        // turn, walk or pick the next attack
        void Update()
        {
            if (!attempts.attemptActive || health.IsDead() || player.health.IsDead())
                return;

            if (Time.time - lastPoiseHit > profile.poiseRegenDelay)
                poise = profile.poise;

            // an attack is already running
            if (state == BossState.Windup || state == BossState.Active || state == BossState.Recovery
                || state == BossState.Intro || state == BossState.Staggered)
                return;

            float distance = GetDistanceToPlayer();
            if (state == BossState.Dormant)
            {
                if (distance <= profile.wakeDistance)
                    StartCoroutine(IntroRoutine());
                return;
            }

            if (!roared && profile.roarAttack >= 0 && health.currentHealth <= health.maxHealth * profile.roarAtHealth)
            {
                roared = true;
                StartCoroutine(AttackRoutine(profile.roarAttack));
                return;
            }

            TurnTowardPlayer(profile.turnSpeed, 0);
            Vector3 toPlayer = Flat(player.transform.position - transform.position);
            float angle = Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up);

            // short pause between attacks: the moment the player can hit back.
            // if the player is far, the boss walks closer during the pause
            if (idleTimer > 0)
            {
                idleTimer -= Time.deltaTime;
                if (distance > profile.preferredDistance)
                    WalkTowardPlayer();
                else if (distance < profile.backOffDistance)
                    StepBack();
                else if (Mathf.Abs(angle) > 45)
                    state = BossState.Idle;
                else
                    Strafe();
                return;
            }

            int choice = brain.ChooseAttack(distance, angle);
            if (choice < 0)
            {
                WalkTowardPlayer();
                return;
            }

            StartCoroutine(AttackRoutine(choice));
        }

        void OnDamaged(float amount)
        {
            if (state == BossState.Dormant && attempts.attemptActive && !health.IsDead())
                StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            state = BossState.Intro;
            introDone = true;
            if (onIntroStarted != null)
                onIntroStarted();
            yield return new WaitForSeconds(profile.introTime);
            state = BossState.Idle;
            idleTimer = profile.idleTime;
        }

        public void TakePoiseDamage(float amount)
        {
            if (health.IsDead() || state == BossState.Dormant || state == BossState.Intro || state == BossState.Staggered)
                return;
            lastPoiseHit = Time.time;
            poise -= amount;
            if (poise > 0)
                return;

            StopAllCoroutines();
            if (currentAttack >= 0 && onAttackResolved != null)
                onAttackResolved(profile.attacks[currentAttack].name, "Interrupted", 0);
            StartCoroutine(StaggerRoutine());
        }

        IEnumerator StaggerRoutine()
        {
            state = BossState.Staggered;
            currentAttack = -1;
            criticalTaken = false;
            if (onStaggered != null)
                onStaggered();
            yield return new WaitForSeconds(profile.staggerTime);
            poise = profile.poise;
            state = BossState.Idle;
            idleTimer = profile.idleTime;
        }

        // windup, hit, recovery
        IEnumerator AttackRoutine(int index)
        {
            BossAttack attack = profile.attacks[index];
            currentAttack = index;
            float startTime = Time.time;

            state = BossState.Windup;
            if (onAttackStarted != null)
                onAttackStarted(index);

            bool launched = false;
            Vector3 leapFrom = transform.position;
            Vector3 leapTo = transform.position;
            Vector3 spot = Vector3.zero;
            float timer = 0;
            while (timer < attack.windup)
            {
                if (attack.kind == BossAttackKind.Leap && timer >= attack.launchTime)
                {
                    if (!launched)
                    {
                        launched = true;
                        leapFrom = transform.position;
                        spot = player.transform.position;
                        Vector3 toPlayer = Flat(spot - transform.position);
                        leapTo = transform.position + toPlayer.normalized * Mathf.Max(0, toPlayer.magnitude - attack.landingReach);
                    }
                    transform.position = Vector3.Lerp(leapFrom, leapTo, (timer - attack.launchTime) / (attack.windup - attack.launchTime));
                }
                else
                {
                    // a giant turns slowly during the windup, so a fast player can get away
                    TurnTowardPlayer(profile.windupTurnSpeed, attack.AimAngle());
                }
                timer += Time.deltaTime;
                yield return null;
            }
            if (launched)
                transform.position = leapTo;

            // a dead boss stops its attack
            if (health.IsDead())
            {
                state = BossState.Idle;
                currentAttack = -1;
                yield break;
            }

            if (attack.kind == BossAttackKind.Throw)
            {
                spot = player.transform.position;
                Vector3 from = transform.position + Vector3.up * 6f + transform.forward * 1.5f;
                float flight = Vector3.Distance(from, spot) / attack.rockSpeed;
                if (onRockThrown != null)
                    onRockThrown(from, spot, flight);
                state = BossState.Recovery;
                yield return new WaitForSeconds(flight);
            }

            state = BossState.Active;
            bool playerHit = false;
            bool playerDodged = false;
            timer = 0;
            while (timer < attack.activeTime)
            {
                float sweep = Mathf.Lerp(attack.sweepFrom, attack.sweepTo, timer / attack.activeTime);
                bool inside = attack.kind == BossAttackKind.Melee ? IsPlayerInArea(attack, sweep) : IsPlayerNear(spot, attack.spotRadius);
                // once the player dodged through this swing, it can not hit them later in the same swing
                if (!playerHit && !playerDodged && inside)
                {
                    // while dodging the player can not be hit, the attack goes through
                    if (player.health.invulnerable)
                        playerDodged = true;
                    else if (player.health.TakeDamage(attack.damage))
                        playerHit = true;
                }
                timer += Time.deltaTime;
                yield return null;
            }
            string outcome = playerHit ? "Hit" : playerDodged ? "Dodged" : "Missed";

            if (onAttackResolved != null)
                onAttackResolved(attack.name, outcome, outcome == "Hit" ? attack.damage : 0);

            state = BossState.Recovery;
            if (attack.chainNext >= 0)
            {
                while (Time.time - startTime < attack.chainAt)
                    yield return null;
                StartCoroutine(AttackRoutine(attack.chainNext));
                yield break;
            }

            float end = startTime + attack.windup + attack.activeTime + attack.recovery;
            while (Time.time < end)
                yield return null;

            state = BossState.Idle;
            currentAttack = -1;
            idleTimer = profile.idleTime;
            strafeDirection = -strafeDirection;
        }

        // player inside the attack area
        public bool IsPlayerInArea(BossAttack attack, float middle)
        {
            Vector3 toPlayer = Flat(player.transform.position - transform.position);
            float distance = toPlayer.magnitude;

            // the player is about half a metre wide, so add that
            if (distance < attack.innerRadius - 0.5f || distance > attack.outerRadius + 0.5f)
                return false;
            if (attack.halfAngle >= 180)
                return true;
            float angle = Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up);
            return Mathf.Abs(Mathf.DeltaAngle(angle, middle)) <= attack.halfAngle;
        }

        bool IsPlayerNear(Vector3 spot, float radius)
        {
            return Flat(player.transform.position - spot).magnitude <= radius + 0.5f;
        }

        // flat distance, height ignored
        float GetDistanceToPlayer()
        {
            return Flat(player.transform.position - transform.position).magnitude;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0;
            return v;
        }

        // turn at a limited speed
        void TurnTowardPlayer(float degreesPerSecond, float aim)
        {
            Vector3 toPlayer = Flat(player.transform.position - transform.position);
            if (toPlayer == Vector3.zero)
                return;
            Quaternion target = Quaternion.LookRotation(toPlayer) * Quaternion.Euler(0, -aim, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        }

        // step toward the player
        void WalkTowardPlayer()
        {
            state = BossState.Walking;
            Vector3 toPlayer = Flat(player.transform.position - transform.position);
            transform.position += toPlayer.normalized * profile.walkSpeed * Time.deltaTime;
        }

        void StepBack()
        {
            state = BossState.BackingOff;
            Vector3 toPlayer = Flat(player.transform.position - transform.position);
            transform.position -= toPlayer.normalized * profile.strafeSpeed * Time.deltaTime;
        }

        void Strafe()
        {
            state = BossState.Strafing;
            Vector3 toPlayer = Flat(player.transform.position - transform.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer) * strafeDirection;
            transform.position += side * profile.strafeSpeed * Time.deltaTime;
        }
    }
}
