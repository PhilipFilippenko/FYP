using System;
using UnityEngine;

namespace WillingBlade.FYP
{
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
            }
            return 0;
        }
    }

    // the hit area is a piece of a ring around the boss: between two distances and inside an angle
    [Serializable]
    public class BossAttack
    {
        public string name;
        public float damage;
        public float windup;
        public float activeTime;
        public float recovery;

        [Header("Hit area")]
        public float innerRadius;
        public float outerRadius;
        public float halfAngle;

        public CounterValues counters;
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
    }
}
