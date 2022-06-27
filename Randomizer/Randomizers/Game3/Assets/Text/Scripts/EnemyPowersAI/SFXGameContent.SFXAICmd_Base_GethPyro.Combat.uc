auto state Combat 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.FireTarget == None)
    {
        Outer.AILog_Internal("Lost enemy.  Re-acquiring target", 'Combat');
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    MoveTimeout = RandRange(MoveTimeoutInterval.X, MoveTimeoutInterval.Y);
    TotalMoveTime = 0.0;
    Class'SFXAICmd_MoveToMeleeRange'.static.MoveToMeleeRange(Outer, Outer.FireTarget, 200.0, TRUE);
    if (Outer.IsInWeaponRange(Outer.FireTarget))
    {
        Outer.ShootWeaponAtFireTarget();
        Outer.Sleep(1.0 + FRand());
    }
    else if (ShouldAttack())
    {
        Outer.Attack();
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};