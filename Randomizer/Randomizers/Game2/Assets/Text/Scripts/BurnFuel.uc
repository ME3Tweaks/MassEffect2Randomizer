// Makes normandy never run out of gas
public event function BurnFuel(float fFuel)
{
    local BioPlayerController oController;
    local SFXInventoryManager oInventory;
    
    oController = BioWorldInfo(Class'Engine'.static.GetCurrentWorldInfo()).GetLocalPlayerController();
    oInventory = SFXInventoryManager(oController.Pawn.InvManager);
    oInventory.CurrentFuel = FMax(oInventory.CurrentFuel - fFuel, 5.0);
    m_pAudioComponent.SetWwiseRTPC(m_sShipTravelSound_FuelQtyRTPCName, oInventory.CurrentFuel / oInventory.GetMaxFuel());
}