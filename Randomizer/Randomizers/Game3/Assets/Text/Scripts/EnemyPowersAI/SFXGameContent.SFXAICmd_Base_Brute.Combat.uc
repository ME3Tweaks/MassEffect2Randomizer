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
    if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Melee) == FALSE)
    {
        Outer.m_bClearVelocityAfterMove = FALSE;
        Class'SFXAICmd_MoveToMeleeRange'.static.MoveToMeleeRange(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, FALSE);
    }
    else if (Outer.ShouldMelee(Outer.FireTarget))
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_Brute_Melee');
    }
    else if (Rand(5) == 0)
    {
        Outer.Attack();
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};