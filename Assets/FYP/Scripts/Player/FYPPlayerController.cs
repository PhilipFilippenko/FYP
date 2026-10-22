using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP
{
    // phase of the current action
    public enum CombatState
    {
        Idle,
        Windup,
        Active,
        Recovery,
        Dodge
    }

    // numbers for one attack
    [Serializable]
    public class AttackSettings
    {
        public float staminaCost;
        public float damage;
        public float windup;
        public float activeTime;
        public float recovery;
        public float reach;
        public float hitRadius;
    }

    public class FYPPlayerController : MonoBehaviour
    {
        [Header("Lock-on")]
        public Transform lockTarget;
        public float maxLockDistance = 60f;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float sprintSpeed = 8f;
        public float sprintStaminaPerSecond = 15f;
        public float turnSpeed = 10f;
        public float gravity = -20f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float staminaRegen = 30f;
        public float staminaRegenDelay = 0.6f;

        [Header("Attacks")]
        public AttackSettings lightAttack = new AttackSettings { staminaCost = 15, damage = 10, windup = 0.15f, activeTime = 0.1f, recovery = 0.35f, reach = 1.6f, hitRadius = 1.2f };
        public AttackSettings heavyAttack = new AttackSettings { staminaCost = 30, damage = 25, windup = 0.45f, activeTime = 0.15f, recovery = 0.6f, reach = 2f, hitRadius = 1.5f };

        [Header("Dodge")]
        public float dodgeStaminaCost = 20f;
        public float dodgeDistance = 4f;
        public float dodgeDuration = 0.6f;
        public float dodgeInvulnerableTime = 0.25f;

        [Header("State (read only)")]
        public float stamina;
        public bool isLockedOn;
        public bool isSprinting;
        public bool isBusy;
        public CombatState state;
        public PlayerAction currentAction;
        public Vector3 hitCenter;
        public float hitRadius;

        // links to other scripts
        public Health health;
        public Action<PlayerAction> onActionPerformed;
        // sent when a swing ends, with the damage it did (0 = miss)
        public Action<PlayerAction, float> onAttackResolved;

        // internal
        CharacterController controller;
        float verticalSpeed;
        float lastStaminaUseTime;
        bool sprintBlocked;

        // find components, full stamina
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            stamina = maxStamina;
        }

        // main loop
        void Update()
        {
            ApplyGravity();
            if (health.IsDead())
                return;

            if (Mouse.current.middleButton.wasPressedThisFrame)
                ToggleLockOn();
            if (isLockedOn && !CanLockOn())
                isLockedOn = false;
            if (isLockedOn)
                FaceTarget();

            RegenStamina();

            Vector2 input = GetMoveInput();
            isSprinting = false;
            if (!isBusy)
            {
                Move(input);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                    Attack(lightAttack, PlayerAction.LightAttack);
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                    Attack(heavyAttack, PlayerAction.HeavyAttack);
                else if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    Dodge(input);
            }
        }

        // back to start for a new attempt
        public void ResetForAttempt(Vector3 position, Quaternion rotation)
        {
            StopAllCoroutines();
            isBusy = false;
            state = CombatState.Idle;
            verticalSpeed = 0;
            stamina = maxStamina;
            health.ResetHealth();

            // unity's character controller blocks moving the player directly, so turn it off for a moment
            controller.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            controller.enabled = true;
        }

        // wasd to a direction
        Vector2 GetMoveInput()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            return input.normalized;
        }

        // locked: forward = toward the boss. free: forward = where the camera looks
        Vector3 GetWorldDirection(Vector2 input)
        {
            Transform reference = isLockedOn ? transform : Camera.main.transform;

            Vector3 forward = reference.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = reference.right;
            right.y = 0;
            right.Normalize();

            return (forward * input.y + right * input.x).normalized;
        }

        // walk or sprint
        void Move(Vector2 input)
        {
            Vector3 direction = GetWorldDirection(input);

            // when stamina runs out, let go of shift before you can sprint again
            bool shiftHeld = Keyboard.current.leftShiftKey.isPressed;
            if (!shiftHeld)
                sprintBlocked = false;
            isSprinting = shiftHeld && !sprintBlocked && direction != Vector3.zero && stamina > 0;
            float speed = moveSpeed;
            if (isSprinting)
            {
                speed = sprintSpeed;
                stamina -= sprintStaminaPerSecond * Time.deltaTime;
                if (stamina <= 0)
                {
                    stamina = 0;
                    sprintBlocked = true;
                }
                lastStaminaUseTime = Time.time;
            }

            controller.Move(direction * speed * Time.deltaTime);

            // free mode: the body turns to where it walks
            if (!isLockedOn && direction != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        // toggle lock-on
        void ToggleLockOn()
        {
            if (isLockedOn)
                isLockedOn = false;
            else if (CanLockOn())
                isLockedOn = true;
        }

        // can lock-on
        bool CanLockOn()
        {
            return lockTarget != null && Vector3.Distance(transform.position, lockTarget.position) <= maxLockDistance;
        }

        // turn toward the boss
        void FaceTarget()
        {
            Vector3 toTarget = lockTarget.position - transform.position;
            toTarget.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), turnSpeed * Time.deltaTime);
        }

        // gravity
        void ApplyGravity()
        {
            if (controller.isGrounded && verticalSpeed < 0)
                verticalSpeed = -2f;
            verticalSpeed += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
        }

        // stamina refill after pause
        void RegenStamina()
        {
            if (isBusy || Time.time - lastStaminaUseTime < staminaRegenDelay)
                return;

            stamina += staminaRegen * Time.deltaTime;
            if (stamina > maxStamina)
                stamina = maxStamina;
        }

        // can act if any stamina left, even if the action costs more
        bool UseStamina(float cost)
        {
            if (stamina <= 0)
                return false;

            stamina -= cost;
            if (stamina < 0)
                stamina = 0;
            lastStaminaUseTime = Time.time;
            return true;
        }

        // start an attack if there is stamina
        void Attack(AttackSettings attack, PlayerAction action)
        {
            if (!UseStamina(attack.staminaCost))
                return;

            if (onActionPerformed != null)
                onActionPerformed(action);
            StartCoroutine(AttackRoutine(attack, action));
        }

        // windup, hit, recovery
        IEnumerator AttackRoutine(AttackSettings attack, PlayerAction action)
        {
            isBusy = true;
            currentAction = action;

            state = CombatState.Windup;

            // free mode: turn toward the camera during the windup, so the swing goes where the player looks
            Quaternion startRotation = transform.rotation;
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            Quaternion aimRotation = Quaternion.LookRotation(cameraForward);

            float windupTimer = 0;
            while (windupTimer < attack.windup)
            {
                windupTimer += Time.deltaTime;
                if (!isLockedOn)
                    transform.rotation = Quaternion.Slerp(startRotation, aimRotation, windupTimer / attack.windup);
                yield return null;
            }

            state = CombatState.Active;
            hitRadius = attack.hitRadius;
            // each target can be hit only once per swing
            List<Health> alreadyHit = new List<Health>();
            float damageDealt = 0;
            float timer = 0;
            while (timer < attack.activeTime)
            {
                hitCenter = transform.position + transform.forward * attack.reach;
                Collider[] hits = Physics.OverlapSphere(hitCenter, attack.hitRadius);
                foreach (Collider hit in hits)
                {
                    Health target = hit.GetComponentInParent<Health>();
                    if (target != null && target != health && !alreadyHit.Contains(target))
                    {
                        if (target.TakeDamage(attack.damage))
                            damageDealt += attack.damage;
                        alreadyHit.Add(target);
                    }
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (onAttackResolved != null)
                onAttackResolved(action, damageDealt);

            state = CombatState.Recovery;
            yield return new WaitForSeconds(attack.recovery);

            state = CombatState.Idle;
            isBusy = false;
        }

        // start a dodge if there is stamina
        void Dodge(Vector2 input)
        {
            if (!UseStamina(dodgeStaminaCost))
                return;

            // no direction = step back
            Vector3 direction = -transform.forward;
            if (input != Vector2.zero)
                direction = GetWorldDirection(input);

            PlayerAction action = GetDodgeAction(direction);
            if (onActionPerformed != null)
                onActionPerformed(action);
            StartCoroutine(DodgeRoutine(direction, action));
        }

        // left, right, back and forward are from the boss's point of view, the same with and without lock-on
        PlayerAction GetDodgeAction(Vector3 direction)
        {
            Vector3 toBoss = lockTarget.position - transform.position;
            toBoss.y = 0;
            toBoss.Normalize();

            float forwardAmount = Vector3.Dot(direction, toBoss);
            if (forwardAmount > 0.7f)
                return PlayerAction.DodgeForward;
            if (forwardAmount < -0.7f)
                return PlayerAction.DodgeBack;

            Vector3 rightOfBoss = Vector3.Cross(Vector3.up, toBoss);
            if (Vector3.Dot(direction, rightOfBoss) < 0)
                return PlayerAction.DodgeLeft;
            return PlayerAction.DodgeRight;
        }

        // i-frames
        IEnumerator DodgeRoutine(Vector3 direction, PlayerAction action)
        {
            isBusy = true;
            currentAction = action;
            state = CombatState.Dodge;
            health.invulnerable = true;

            float speed = dodgeDistance / dodgeDuration;
            float timer = 0;
            while (timer < dodgeDuration)
            {
                if (timer > dodgeInvulnerableTime)
                    health.invulnerable = false;
                controller.Move(direction * speed * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }

            health.invulnerable = false;
            state = CombatState.Idle;
            isBusy = false;
        }
    }
}
