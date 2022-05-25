using System.Collections.Generic;
using UnityEngine;

public class FormerHumanSergeantBehavior : FormerHumanTrooperBehavior
{
    protected override float attackSpread { get { return .05f; } }
    protected override int shotCount { get { return 3; } }
}