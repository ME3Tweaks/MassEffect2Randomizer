public function PeriodicMoveCheck()
{
    Super(SFXAICommand).PeriodicMoveCheck();
    if (IsInBansheeMeleeRange())
    {
        Outer.MoveGoal = None;
        Outer.bReachedMoveGoal = TRUE;
    }
    else if (Outer.ShouldTeleport())
    {
        if (Outer.RouteCache.Length > 0)
        {
            Outer.CachedPath = Outer.RouteCache;
            if (Outer.MoveTarget != None)
            {
                Outer.CachedPath.InsertItem(0, Outer.MoveTarget);
            }
        }
        Outer.MoveGoal = None;
        Outer.bReachedMoveGoal = TRUE;
    } else {
        Outer.Attack();
    }
}