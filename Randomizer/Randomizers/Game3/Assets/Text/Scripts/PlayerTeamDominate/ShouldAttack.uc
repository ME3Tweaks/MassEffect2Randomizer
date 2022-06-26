public function bool ShouldAttack()
{
    local int nPlayerIsFiring;
    
    if (MyBP == None)
    {
        return FALSE;
    }
    if (MyBP.IsInCover())
    {
        if (MyBP.GetHealthPct() < DamagePercentToRemainInCover)
        {
            AILog_Internal("Not attacking - health is too low", 'Attack');
            return FALSE;
        }
        if (IsInPlayerLineOfFire(nPlayerIsFiring) && int(GetTeamNum()) == 0)
        {
            AILog_Internal("Not attacking - in player line of fire", 'Attack');
            return FALSE;
        }
    }
    return TRUE;
}