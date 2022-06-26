public event simulated function BeginState(Name PreviousStateName)
{
    local SFXModule_Marker ModuleMarkerIter;
    local SFXModule ModuleIter;
    local SFXModule_MarkerPlayer ModuleMarkerPlayer;
    
    Super.BeginState(PreviousStateName);
    foreach Modules(ModuleIter, )
    {
        ModuleMarkerIter = SFXModule_Marker(ModuleIter);
        if (ModuleMarkerIter != None && ModuleMarkerIter.bActive)
        {
            ModuleMarkerIter.Deactivate();
        }
    }
    ModuleMarkerPlayer = new Class'SFXModule_MarkerPlayer';
    if (ModuleMarkerPlayer != None)
    {
        ModuleMarkerPlayer.GUIMarkerClass = Class'SFXGUIValue_MarkerHenchman';
        ModuleMarkerPlayer.MarkerType = "Henchman";
        AddSFXModule(ModuleMarkerPlayer);
        ModuleMarkerPlayer.Activate();
        ModuleMarkerPlayer.UpdatePlayerDownState(TRUE);
    }
    if (BioAiController(Controller) != None)
    {
        BioAiController(Controller).SetTeam(0);
    }
}