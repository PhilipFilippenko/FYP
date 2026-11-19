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
        Dodge,
        Hit
    }

    // numbers for one attack
    [Serializable]
    public class AttackSettings
    {
        public string clip;
        public float speed = 1f;
        public Vector2Int[] hitFrames = new Vector2Int[0];
        public int endFrame;
        public bool continuesPrevious;
        public int chargeFrame;
        public bool hyperArmor;
        public float focusCost;
        public float staminaCost;
        public float damage;
        public float poiseDamage;
        public float reach;
        public float hitRadius;
        public int releaseFrame;
        public AnimationCurve rootMotion = new AnimationCurve();

        public AttackSettings()
        {
        }

        public AttackSettings(string clip, float speed, int endFrame, float staminaCost, float damage, float poiseDamage, float reach, float hitRadius, params Vector2Int[] hitFrames)
        {
            this.clip = clip;
            this.speed = speed;
            this.endFrame = endFrame;
            this.staminaCost = staminaCost;
            this.damage = damage;
            this.poiseDamage = poiseDamage;
            this.reach = reach;
            this.hitRadius = hitRadius;
            this.hitFrames = hitFrames;
        }

        public float TimeOf(int frame)
        {
            return frame / 30f / speed;
        }
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

        [Header("Attacks: clip, speed, end frame, stamina, damage, poise, reach, radius, hit frames")]
        public AttackSettings[] lightCombo =
        {
            new AttackSettings("combo_1", 1.5f, 36, 15, 10, 10, 1.6f, 1.2f, new Vector2Int(24, 27)),
            new AttackSettings("combo_2", 1.5f, 25, 15, 10, 10, 1.6f, 1.3f, new Vector2Int(16, 18)) { continuesPrevious = true },
            new AttackSettings("combo_3", 1.5f, 28, 15, 15, 15, 1.5f, 1.3f, new Vector2Int(17, 20)) { continuesPrevious = true }
        };
        public AttackSettings[] heavyCombo =
        {
            new AttackSettings("slash_atk_3", 1.25f, 27, 30, 25, 30, 1.7f, 1.4f, new Vector2Int(19, 21)) { chargeFrame = 15, hyperArmor = true },
            new AttackSettings("slash_atk", 1.3f, 35, 30, 25, 30, 1.6f, 1.5f, new Vector2Int(26, 29)) { chargeFrame = 20, hyperArmor = true }
        };
        public AttackSettings rollAttack = new AttackSettings("slash_atk_2", 1.4f, 35, 20, 15, 15, 1.2f, 1.4f, new Vector2Int(21, 24));
        public AttackSettings finisher = new AttackSettings("spin_atk", 1.3f, 44, 35, 10, 12, 0, 2.4f, new Vector2Int(11, 14), new Vector2Int(21, 25), new Vector2Int(32, 36)) { hyperArmor = true };
        public AttackSettings runningLight = new AttackSettings("slide_atk", 1.3f, 56, 20, 15, 15, 1.7f, 1.3f, new Vector2Int(36, 40));
        public AttackSettings jumpAttack = new AttackSettings("jump_atk", 1.25f, 53, 35, 30, 40, 1.8f, 1.5f, new Vector2Int(31, 36)) { hyperArmor = true };
        public AttackSettings[] kickCombo =
        {
            new AttackSettings("kick", 1.3f, 34, 15, 5, 35, 1f, 1f, new Vector2Int(16, 19)),
            new AttackSettings("kick_2", 1.3f, 44, 15, 7, 40, 1f, 1f, new Vector2Int(20, 25))
        };
        public AttackSettings spell = new AttackSettings("spell_cast", 1.2f, 34, 10, 15, 10, 0, 0) { releaseFrame = 11, focusCost = 15 };
        public AttackSettings groundSlam = new AttackSettings("casting", 1.4f, 99, 20, 40, 60, 0.6f, 4f, new Vector2Int(69, 73)) { hyperArmor = true, focusCost = 40 };
        public AttackSettings critical = new AttackSettings("slash_atk_3", 1f, 27, 0, 100, 0, 1.7f, 2.5f, new Vector2Int(19, 21)) { hyperArmor = true };
        public float criticalRange = 4.5f;
        public float comboWindow = 0.6f;
        public float bufferTime = 0.5f;
        public float maxChargeTime = 1f;
        public float chargeBonus = 0.6f;
        public int rollCancelFrames = 3;
        public float hitStop = 0.06f;
        public float heavyHitStop = 0.1f;
        public float attackTurnTime = 0.1f;

        [Header("Focus (spells)")]
        public float maxFocus = 100f;
        public float focusRegen = 4f;

        [Header("Spell")]
        public float spellSpeed = 20f;
        public float spellRange = 30f;
        public float spellRadius = 0.35f;
        public Vector3 spellSpawn = new Vector3(0.2f, 0.27f, 0.9f);

        [Header("Dodge")]
        public float dodgeStaminaCost = 20f;
        public float dodgeDistance = 4f;
        public float dodgeDuration = 0.6f;
        public float dodgeInvulnerableTime = 0.25f;

        [Header("Hit reaction")]
        public float hitStun = 0.42f;
        public float heavyHitStun = 0.47f;
        public float heavyHitDamage = 30f;

        [Header("State (read only)")]
        public float stamina;
        public bool isLockedOn;
        public bool isSprinting;
        public bool isBusy;
        public CombatState state;
        public PlayerAction currentAction;
        public Vector3 hitCenter;
        public float hitRadius;
        public AttackSettings currentAttack;
        public bool isCharging;
        public float focus;
        public bool inHitStop;

        // links to other scripts
        public Health health;
        public Action<PlayerAction> onActionPerformed;
        // sent when a swing ends, with the damage it did (0 = miss)
        public Action<PlayerAction, float> onAttackResolved;
        public Action<AttackSettings, int> onHitWindowOpened;
        public Action<float> onStunned;
        public Action<GameObject> onSpellReleased;
        public Action<Vector3, Vector3, float> onHitLanded;
        public int attackFrame = -1;

        // internal
        enum Command { None, Light, Heavy, Dodge, Kick, Spell, Slam }

        CharacterController controller;
        float verticalSpeed;
        float lastStaminaUseTime;
        bool sprintBlocked;
        Command buffered;
        float bufferedAt;
        string lastChain = "";
        int lastStep;
        float lastActionEnd = -10;
        Coroutine actionRoutine;
        bool canRollCancel;
        string runningChain = "";
        int runningStep;
        float lastFocusUseTime;

        // find components, full stamina
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            stamina = maxStamina;
            focus = maxFocus;
            health.onDamaged += OnDamaged;
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
            RegenFocus();

            Command pressed = ReadCommand();
            if (pressed != Command.None)
            {
                buffered = pressed;
                bufferedAt = Time.time;
            }

            Vector2 input = GetMoveInput();
            isSprinting = false;
            bool buffer = buffered != Command.None && Time.time - bufferedAt <= bufferTime;
            if (isBusy && canRollCancel && buffer && buffered == Command.Dodge)
            {
                StopCoroutine(actionRoutine);
                EndAction(runningChain, runningStep);
            }
            if (!isBusy)
            {
                Move(input);
                if (buffer)
                    Perform(buffered, input);
                buffered = Command.None;
            }
        }

        // back to start for a new attempt
        public void ResetForAttempt(Vector3 position, Quaternion rotation)
        {
            StopAllCoroutines();
            isBusy = false;
            isCharging = false;
            inHitStop = false;
            canRollCancel = false;
            state = CombatState.Idle;
            currentAttack = null;
            buffered = Command.None;
            lastChain = "";
            verticalSpeed = 0;
            stamina = maxStamina;
            focus = maxFocus;
            health.ResetHealth();

            // unity's character controller blocks moving the player directly, so turn it off for a moment
            controller.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            controller.enabled = true;
        }

        Command ReadCommand()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) return Command.Light;
            if (Mouse.current.rightButton.wasPressedThisFrame) return Command.Heavy;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return Command.Dodge;
            if (Keyboard.current.fKey.wasPressedThisFrame) return Command.Kick;
            if (Keyboard.current.qKey.wasPressedThisFrame) return Command.Spell;
            if (Keyboard.current.eKey.wasPressedThisFrame) return Command.Slam;
            return Command.None;
        }

        void Perform(Command command, Vector2 input)
        {
            bool chaining = Time.time - lastActionEnd <= comboWindow;
            switch (command)
            {
                case Command.Dodge:
                    Dodge(input);
                    break;
                case Command.Light:
                    if (CanCritical())
                        Critical();
                    else if (isSprinting)
                        Attack(runningLight, PlayerAction.SprintAttack, "", 0);
                    else if (chaining && lastChain == "dodge")
                        Attack(rollAttack, PlayerAction.LightAttack, "", 0);
                    else
                    {
                        int step = chaining && lastChain == "light" ? (lastStep + 1) % lightCombo.Length : 0;
                        Attack(lightCombo[step], PlayerAction.LightAttack, "light", step);
                    }
                    break;
                case Command.Heavy:
                    if (isSprinting)
                        Attack(jumpAttack, PlayerAction.JumpAttack, "", 0);
                    else if (chaining && lastChain == "light")
                        Attack(finisher, PlayerAction.HeavyAttack, "", 0);
                    else
                    {
                        int step = chaining && lastChain == "heavy" ? (lastStep + 1) % heavyCombo.Length : 0;
                        Attack(heavyCombo[step], PlayerAction.HeavyAttack, "heavy", step);
                    }
                    break;
                case Command.Kick:
                {
                    int step = chaining && lastChain == "kick" ? (lastStep + 1) % kickCombo.Length : 0;
                    Attack(kickCombo[step], PlayerAction.Kick, "kick", step);
                    break;
                }
                case Command.Spell:
                    Cast(spell, PlayerAction.Spell);
                    break;
                case Command.Slam:
                    Cast(groundSlam, PlayerAction.GroundSlam);
                    break;
            }
        }

        void Cast(AttackSettings cast, PlayerAction action)
        {
            if (focus < cast.focusCost)
                return;
            focus -= cast.focusCost;
            lastFocusUseTime = Time.time;
            Attack(cast, action, "", 0);
        }

        bool CanCritical()
        {
            if (lockTarget == null)
                return false;
            BossController boss = lockTarget.GetComponent<BossController>();
            if (boss == null || boss.state != BossState.Staggered || boss.criticalTaken)
                return false;
            Vector3 fromBoss = transform.position - lockTarget.position;
            fromBoss.y = 0;
            return fromBoss.magnitude <= criticalRange && Vector3.Angle(lockTarget.forward, fromBoss) <= 60;
        }

        void Critical()
        {
            lockTarget.GetComponent<BossController>().criticalTaken = true;
            Vector3 toBoss = lockTarget.position - transform.position;
            toBoss.y = 0;
            transform.rotation = Quaternion.LookRotation(toBoss);
            Attack(critical, PlayerAction.HeavyAttack, "", 0);
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

        void RegenFocus()
        {
            if (state != CombatState.Idle && currentAttack != null && currentAttack.focusCost > 0)
                return;
            if (Time.time - lastFocusUseTime < 1f)
                return;
            focus = Mathf.Min(maxFocus, focus + focusRegen * Time.deltaTime);
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
        void Attack(AttackSettings attack, PlayerAction action, string chain, int step)
        {
            if (!UseStamina(attack.staminaCost))
                return;

            currentAttack = attack;
            currentAction = action;
            if (onActionPerformed != null)
                onActionPerformed(action);
            actionRoutine = StartCoroutine(AttackRoutine(attack, action, chain, step));
        }

        void EndAction(string chain, int step)
        {
            lastStaminaUseTime = Time.time;
            attackFrame = -1;
            state = CombatState.Idle;
            isBusy = false;
            isCharging = false;
            inHitStop = false;
            canRollCancel = false;
            currentAttack = null;
            lastChain = chain;
            lastStep = step;
            lastActionEnd = Time.time;
        }

        // windup, hit, recovery
        IEnumerator AttackRoutine(AttackSettings attack, PlayerAction action, string chain, int step)
        {
            isBusy = true;
            canRollCancel = false;
            runningChain = chain;
            runningStep = step;
            state = CombatState.Windup;
            int lastHit = attack.hitFrames.Length > 0 ? attack.hitFrames[attack.hitFrames.Length - 1].y : attack.releaseFrame;
            float charge = 0;
            bool chargeDone = attack.chargeFrame <= 0;

            int firstHit = attack.hitFrames.Length > 0 ? attack.hitFrames[0].x : attack.releaseFrame;
            float aimTime = attackTurnTime;
            Quaternion startRotation = transform.rotation;
            Vector2 held = GetMoveInput();
            Quaternion aimRotation = held != Vector2.zero ? Quaternion.LookRotation(GetWorldDirection(held)) : startRotation;

            // each target can be hit only once per swing
            List<Health> alreadyHit = new List<Health>();
            int openWindow = -1;
            float damageDealt = 0;
            bool released = false;
            float clipTime = 0;
            float end = attack.TimeOf(attack.endFrame);
            float timer = 0;
            while (timer < end)
            {
                if (!chargeDone && Mathf.FloorToInt(timer * attack.speed * 30) >= attack.chargeFrame)
                {
                    if (Mouse.current.rightButton.isPressed && charge < maxChargeTime)
                    {
                        isCharging = true;
                        charge += Time.deltaTime;
                        yield return null;
                        continue;
                    }
                    chargeDone = true;
                    isCharging = false;
                }

                timer += Time.deltaTime;
                if (!isLockedOn && timer < aimTime + Time.deltaTime)
                    transform.rotation = Quaternion.Slerp(startRotation, aimRotation, Mathf.Clamp01(timer / aimTime));

                float newClipTime = Mathf.Min(timer, end) * attack.speed;
                controller.Move(transform.forward * (attack.rootMotion.Evaluate(newClipTime) - attack.rootMotion.Evaluate(clipTime)));
                clipTime = newClipTime;

                int frame = Mathf.FloorToInt(clipTime * 30);
                attackFrame = frame;
                int window = -1;
                for (int i = 0; i < attack.hitFrames.Length; i++)
                {
                    if (frame >= attack.hitFrames[i].x && frame <= attack.hitFrames[i].y)
                        window = i;
                }
                if (window != openWindow)
                {
                    openWindow = window;
                    alreadyHit.Clear();
                    if (window >= 0 && onHitWindowOpened != null)
                        onHitWindowOpened(attack, window);
                }

                if (window >= 0)
                {
                    state = CombatState.Active;
                    float dealt = HitInSphere(attack, alreadyHit, 1 + chargeBonus * Mathf.Clamp01(charge / maxChargeTime));
                    damageDealt += dealt;
                    if (dealt > 0)
                    {
                        inHitStop = true;
                        yield return new WaitForSeconds(dealt >= 25 ? heavyHitStop : hitStop);
                        inHitStop = false;
                    }
                }
                else
                    state = frame < firstHit ? CombatState.Windup : CombatState.Recovery;
                canRollCancel = frame > lastHit + rollCancelFrames;

                if (attack.releaseFrame > 0 && !released && frame >= attack.releaseFrame)
                {
                    released = true;
                    ShootSpell(attack);
                }
                yield return null;
            }

            if (attack.releaseFrame == 0 && onAttackResolved != null)
                onAttackResolved(action, damageDealt);

            EndAction(chain, step);
        }

        float HitInSphere(AttackSettings attack, List<Health> alreadyHit, float power)
        {
            float dealt = 0;
            hitRadius = attack.hitRadius;
            hitCenter = transform.position + transform.forward * attack.reach;
            Collider[] hits = Physics.OverlapSphere(hitCenter, attack.hitRadius);
            foreach (Collider hit in hits)
            {
                Health target = hit.GetComponentInParent<Health>();
                if (target == null || target == health || alreadyHit.Contains(target))
                    continue;
                alreadyHit.Add(target);
                if (target.TakeDamage(attack.damage * power))
                {
                    dealt += attack.damage * power;
                    if (onHitLanded != null)
                    {
                        Vector3 point = hit.ClosestPoint(hitCenter);
                        onHitLanded(point, (transform.position - point).normalized, attack.damage * power);
                    }
                    BossController boss = target.GetComponent<BossController>();
                    if (boss != null)
                        boss.TakePoiseDamage(attack.poiseDamage * power);
                }
            }
            return dealt;
        }

        void ShootSpell(AttackSettings attack)
        {
            GameObject ball = new GameObject("SpellBall");
            ball.transform.position = transform.TransformPoint(spellSpawn);
            Vector3 direction = transform.forward;
            if (isLockedOn)
                direction = (lockTarget.position + Vector3.up * 2f - ball.transform.position).normalized;
            ball.transform.rotation = Quaternion.LookRotation(direction);

            Projectile projectile = ball.AddComponent<Projectile>();
            projectile.owner = health;
            projectile.speed = spellSpeed;
            projectile.range = spellRange;
            projectile.radius = spellRadius;
            projectile.damage = attack.damage;
            projectile.poiseDamage = attack.poiseDamage;
            projectile.onResolved = dealt =>
            {
                if (onAttackResolved != null)
                    onAttackResolved(PlayerAction.Spell, dealt);
            };
            if (onSpellReleased != null)
                onSpellReleased(ball);
        }

        void OnDamaged(float amount)
        {
            if (health.IsDead())
                return;
            if (currentAttack != null && currentAttack.hyperArmor && (state == CombatState.Windup || state == CombatState.Active))
                return;
            StopAllCoroutines();
            actionRoutine = StartCoroutine(HitRoutine(amount >= heavyHitDamage ? heavyHitStun : hitStun));
            if (onStunned != null)
                onStunned(amount);
        }

        IEnumerator HitRoutine(float time)
        {
            isBusy = true;
            isCharging = false;
            inHitStop = false;
            canRollCancel = false;
            state = CombatState.Hit;
            currentAttack = null;
            health.invulnerable = false;
            yield return new WaitForSeconds(time);
            EndAction("", 0);
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
            currentAttack = null;
            currentAction = action;
            if (onActionPerformed != null)
                onActionPerformed(action);
            actionRoutine = StartCoroutine(DodgeRoutine(direction, action));
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
            EndAction("dodge", 0);
        }
    }
}
