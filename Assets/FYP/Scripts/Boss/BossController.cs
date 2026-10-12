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
        Recovery
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

        // sent when an attack starts, with its number in the profile
        public System.Action<int> onAttackStarted;
        // sent when an attack ends: attack name, result ("Hit", "Dodged" or "Missed"), damage
        public System.Action<string, string, float> onAttackResolved;

        // internal
        Health health;
        float idleTimer;

        // find health, subscribe
        void Awake()
        {
            health = GetComponent<Health>();
            attempts.onAttemptStarted += OnAttemptStarted;
        }

        // stop everything for a new attempt
        void OnAttemptStarted(int attempt)
        {
            StopAllCoroutines();
            state = BossState.Idle;
            currentAttack = -1;
            idleTimer = profile.idleTime;
            brain.ResetForAttempt();
        }

        // turn, walk or pick the next attack
        void Update()
        {
            if (!attempts.attemptActive || health.IsDead() || player.health.IsDead())
                return;

            // an attack is already running
            if (state == BossState.Windup || state == BossState.Active || state == BossState.Recovery)
                return;

            TurnTowardPlayer(profile.turnSpeed);
            float distance = GetDistanceToPlayer();

            // short pause between attacks: the moment the player can hit back.
            // if the player is far, the boss walks closer during the pause
            if (idleTimer > 0)
            {
                idleTimer -= Time.deltaTime;
                if (distance > profile.preferredDistance)
                    WalkTowardPlayer();
                else
                    state = BossState.Idle;
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            int choice = brain.ChooseAttack(distance, angle);
            if (choice < 0)
            {
                WalkTowardPlayer();
                return;
            }

            StartCoroutine(AttackRoutine(choice));
        }

        // windup, hit, recovery
        IEnumerator AttackRoutine(int index)
        {
            BossAttack attack = profile.attacks[index];
            currentAttack = index;

            state = BossState.Windup;
            if (onAttackStarted != null)
                onAttackStarted(index);
            float timer = 0;
            while (timer < attack.windup)
            {
                // a giant turns slowly during the windup, so a fast player can get away
                TurnTowardPlayer(profile.windupTurnSpeed);
                timer += Time.deltaTime;
                yield return null;
            }

            // a dead boss stops its attack
            if (health.IsDead())
            {
                state = BossState.Idle;
                currentAttack = -1;
                yield break;
            }

            state = BossState.Active;
            bool playerHit = false;
            bool playerDodged = false;
            timer = 0;
            while (timer < attack.activeTime)
            {
                // once the player dodged through this swing, it can not hit them later in the same swing
                if (!playerHit && !playerDodged && IsPlayerInArea(attack))
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

            if (onAttackResolved != null)
            {
                if (playerHit)
                    onAttackResolved(attack.name, "Hit", attack.damage);
                else if (playerDodged)
                    onAttackResolved(attack.name, "Dodged", 0);
                else
                    onAttackResolved(attack.name, "Missed", 0);
            }

            state = BossState.Recovery;
            yield return new WaitForSeconds(attack.recovery);

            state = BossState.Idle;
            currentAttack = -1;
            idleTimer = profile.idleTime;
        }

        // player inside the attack area
        public bool IsPlayerInArea(BossAttack attack)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0;
            float distance = toPlayer.magnitude;

            // the player is about half a metre wide, so add that
            if (distance < attack.innerRadius - 0.5f || distance > attack.outerRadius + 0.5f)
                return false;
            return Vector3.Angle(transform.forward, toPlayer) <= attack.halfAngle;
        }

        // flat distance, height ignored
        float GetDistanceToPlayer()
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0;
            return toPlayer.magnitude;
        }

        // turn at a limited speed
        void TurnTowardPlayer(float degreesPerSecond)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0;
            if (toPlayer == Vector3.zero)
                return;
            Quaternion target = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        }

        // step toward the player
        void WalkTowardPlayer()
        {
            state = BossState.Walking;
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0;
            transform.position += toPlayer.normalized * profile.walkSpeed * Time.deltaTime;
        }
    }
}
