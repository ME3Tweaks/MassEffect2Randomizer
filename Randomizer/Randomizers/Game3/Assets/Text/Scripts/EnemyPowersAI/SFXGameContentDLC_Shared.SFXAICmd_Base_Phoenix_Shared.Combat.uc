auto state Combat extends InCombat 
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
    if (Outer.FireTarget != None)
    {
        Outer.Focus = Outer.FireTarget;
    }
    Outer.Attack();
    if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Melee) == FALSE)
    {
        Class'SFXAICmd_MoveToMeleeRange'.static.MoveToMeleeRange(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, TRUE);
    }
    else if (Outer.ShouldMelee(Outer.FireTarget))
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_Phoenix_Melee_Shared');
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};