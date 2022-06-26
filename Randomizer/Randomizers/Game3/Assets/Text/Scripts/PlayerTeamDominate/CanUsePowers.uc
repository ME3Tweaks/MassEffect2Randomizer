public function bool CanUsePowers(bool bCheckProfileOption)
{
    local SFXPawn Player;
    local PlayerController Controller;
    local SFXPRI PRI;
    
    if (m_bDelayPowerUse)
    {
        AILog_Internal("Unable to use powers - waiting for the delay to finish", 'Attack');
        return FALSE;
    }
    if (PreventSquadPowerUse)
    {
        AILog_Internal("Unable to use powers - they have been disabled from Kismet", 'Attack');
        return FALSE;
    }
    if (m_OtherHenchman != None)
    {
        if (m_OtherHenchman.FindCommandOfClass(Class'SFXAICmd_UsePower') != None || m_OtherHenchman.ArePowersCoolingDown())
        {
            AILog_Internal("Unable to use powers - the other henchman has powers cooling down", 'Attack');
            return FALSE;
        }
    }
    if (((bCheckProfileOption && MyBP != None) && MyBP.Squad != None) && int(GetTeamNum()) == 0)
    {
        Player = SFXPawn(MyBP.Squad.Members[0]);
        if (Player != None)
        {
            if (Player.DrivenVehicle == None && Player.DrivenAtlas == None)
            {
                Controller = PlayerController(Player.Controller);
                if (Controller != None)
                {
                    PRI = SFXPRI(Controller.PlayerReplicationInfo);
                    if (PRI != None && PRI.bSquadUsesPowers == FALSE)
                    {
                        AILog_Internal("Unable to use powers - profile option set to no power usage", 'Attack');
                        return FALSE;
                    }
                }
            }
        }
    }
    return TRUE;
}