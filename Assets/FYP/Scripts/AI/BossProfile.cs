using System;
using UnityEngine;

namespace FYP
{
    // the hit area is a piece of a ring around the boss: between two distances and inside an angle
    // how well a boss attack punishes each player action: 0 = not at all, 1 = perfectly.
    // set once before playtests, never changed for one player
    [Serializable]
    public class CounterValues
    {
        public float lightAttack;
        public float heavyAttack;
        public float dodgeLeft;
        public float dodgeRight;
        public float dodgeBack;
        public float dodgeForward;
        public float moveCloser;
        public float moveAway;
        public float useCover;
        public float sprintAttack;
        public float kick;
        public float spell;
        public float groundSlam;
        public float jumpAttack;

        // counter value for one action
        public float Get(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.LightAttack: return lightAttack;
                case PlayerAction.HeavyAttack: return heavyAttack;
                case PlayerAction.DodgeLeft: return dodgeLeft;
                case PlayerAction.DodgeRight: return dodgeRight;
                case PlayerAction.DodgeBack: return dodgeBack;
                case PlayerAction.DodgeForward: return dodgeForward;
                case PlayerAction.MoveCloser: return moveCloser;
                case PlayerAction.MoveAway: return moveAway;
                case PlayerAction.UseCover: return useCover;
                case PlayerAction.SprintAttack: return sprintAttack;
                case PlayerAction.Kick: return kick;
                case PlayerAction.Spell: return spell;
                case PlayerAction.GroundSlam: return groundSlam;
                case PlayerAction.JumpAttack: return jumpAttack;
            }
            return 0;
        }
    }

    public enum BossAttackKind
    {
        Melee,
        Leap,
        Throw
    }

    [Serializable]
    public class BossAttack
    {
        public string name;
        public string clip;
        public BossAttackKind kind;
        public float damage;
        public float windup;
        public float activeTime;
        public float recovery;

        [Header("Hit area")]
        public float innerRadius;
        public float outerRadius;
        public float halfAngle;
        public float sweepFrom;
        public float sweepTo;

        [Header("Combo")]
        public bool chainOnly;
        public int chainNext = -1;
        public float chainAt;

        [Header("Leap and throw")]
        public float launchTime;
        public float landingReach;
        public float rockSpeed;
        public float spotRadius;

        public CounterValues counters;

        public float AimAngle()
        {
            if (halfAngle >= 180 || HalfRange() >= 180)
                return 0;
            return (sweepFrom + sweepTo) / 2;
        }

        public float HalfRange()
        {
            return Mathf.Abs(sweepTo - sweepFrom) / 2 + halfAngle;
        }
    }

    // all data for one boss in one file, so the same code can run another boss
    [CreateAssetMenu(menuName = "FYP/Boss Profile")]
    public class BossProfile : ScriptableObject
    {
        public BossAttack[] attacks;

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float turnSpeed = 90f;
        public float windupTurnSpeed = 45f;
        public float preferredDistance = 6f;
        public float idleTime = 0.6f;

        [Header("Between attacks")]
        public float backOffDistance = 3f;
        public float strafeSpeed = 1.5f;

        [Header("Poise")]
        public float poise = 100f;
        public float poiseRegenDelay = 4f;
        public float staggerTime = 3.7f;

        [Header("Half health")]
        public int roarAttack = -1;
        [Range(0f, 1f)]
        public float roarAtHealth = 0.5f;

        [Header("Intro")]
        public float wakeDistance = 25f;
        public float introTime = 5.97f;
    }
}
