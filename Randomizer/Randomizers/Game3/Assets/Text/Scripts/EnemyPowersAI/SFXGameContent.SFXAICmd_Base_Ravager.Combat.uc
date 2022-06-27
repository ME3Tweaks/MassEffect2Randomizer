auto state Combat extends InCombat 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.FireTarget == None)
    {
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    if (Outer.ShouldSpawnSwarmers())
    {
        Outer.MyBP.StartCustomAction(174);
        Outer.LastSwarmerSpawnTime = Outer.WorldInfo.GameTimeSeconds;
    }
    if (CanSuppressTarget() == FALSE)
    {
        MoveStartLocation = Outer.MyBP.location;
        Class'SFXAICmd_MoveToSuppressionPoint'.static.MoveToSuppressionPoint(Outer, Outer.FireTarget, 2, 1000.0, 3000.0, FALSE);
        if (!Outer.bReachedMoveGoal)
        {
            MoveStartLocation = Outer.MyBP.location;
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 500.0, FALSE);
        }
        Outer.Sleep(0.100000001);
    }
    Outer.Attack();
    if (Outer.m_nWeaponCompletionReason == 1)
    {
        Outer.Sleep(Outer.GetFireDelayTime());
    }
    else
    {
        Outer.Sleep(0.200000003);
    }
    goto 'Begin';
    stop;
};