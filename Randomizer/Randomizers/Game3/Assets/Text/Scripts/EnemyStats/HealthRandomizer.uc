DmgMod = MyBP.GetModule(Class'SFXModule_Damage');
if (DmgMod != None)
{
LogInternal("HEALTH BEFORE WAS " $ DmgMod.MaxHealth.Y, );
DmgMod.InitializeMaxHealth((FRand() * 2.0) * DmgMod.MaxHealth.Y);
LogInternal("HEALTH SET TO " $ DmgMod.CurrentHealth, );
}