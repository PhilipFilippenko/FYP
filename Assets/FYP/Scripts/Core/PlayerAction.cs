namespace FYP
{
    // all actions the boss can see. add new ones at the end
    public enum PlayerAction
    {
        LightAttack,
        HeavyAttack,
        DodgeLeft,
        DodgeRight,
        DodgeBack,
        DodgeForward,
        MoveCloser,
        MoveAway,
        UseCover
    }

    // big groups of actions, used while there is not much data yet
    public enum ActionCategory
    {
        Attack,
        SideDodge,
        Retreat,
        Approach,
        Defend
    }

    public static class ActionCategories
    {
        // a new action only needs one line here
        public static ActionCategory Of(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.LightAttack: return ActionCategory.Attack;
                case PlayerAction.HeavyAttack: return ActionCategory.Attack;
                case PlayerAction.DodgeLeft: return ActionCategory.SideDodge;
                case PlayerAction.DodgeRight: return ActionCategory.SideDodge;
                case PlayerAction.DodgeBack: return ActionCategory.Retreat;
                case PlayerAction.MoveAway: return ActionCategory.Retreat;
                case PlayerAction.DodgeForward: return ActionCategory.Approach;
                case PlayerAction.MoveCloser: return ActionCategory.Approach;
                case PlayerAction.UseCover: return ActionCategory.Defend;
            }
            return ActionCategory.Defend;
        }
    }
}
