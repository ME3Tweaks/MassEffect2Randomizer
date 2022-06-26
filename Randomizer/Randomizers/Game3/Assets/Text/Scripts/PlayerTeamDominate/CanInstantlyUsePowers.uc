public function bool CanInstantlyUsePowers()
{
    if (int(GetTeamNum()) != 0)
    {
        return FALSE;
    }
    if (MyBP != None && MyBP.GetTimeSinceLastRender() >= 0.25 || m_bAllowInstantPowerWhileVisible)
    {
        return TRUE;
    }
    return FALSE;
}