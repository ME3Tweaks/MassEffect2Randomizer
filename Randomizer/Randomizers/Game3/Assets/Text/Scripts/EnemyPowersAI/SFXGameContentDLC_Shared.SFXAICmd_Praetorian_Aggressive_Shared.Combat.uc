auto state Combat extends InCombat 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.IsFlying() == FALSE)
    {
        Outer.MyBP.StartCustomAction(212);
    }
    if (Outer.FireTarget == None)
    {
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    Outer.Attack();
    Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset);
    if (Outer.IsPraetorianInRange(Outer.EnemyDistance_Short))
    {
        AggressiveTimeout();
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};