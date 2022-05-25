namespace DoomLoader
{
    public class FormerHumanSergeantBehavior : FormerHumanTrooperBehavior
    {
        protected override float attackSpread { get { return .05f; } }
        protected override int shotCount { get { return 3; } }
    }
}