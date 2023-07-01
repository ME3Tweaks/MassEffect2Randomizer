public function GenerateInventoryFromLoadout(SFXLoadoutData oLoadout)
{
    local SFXShield_Base Shields;
    local Class<SFXPower> PowerClass;
    local ShieldLoadout ShieldLoadout;
    
    if (oLoadout != None)
    {
        if (bShouldSpawnWeapons)
        {
            if (SFXLoadoutDataMER(oLoadout) != None)
            {
                SFXLoadoutDataMER(oLoadout).RandomizeWeapons();
            }
            CreateWeapons(oLoadout);
        }
        foreach oLoadout.ShieldLoadouts(ShieldLoadout, )
        {
            Shields = SFXShield_Base(CreateInventory(ShieldLoadout.Shields));
            if (Shields != None)
            {
                Shields.ShieldScale = oLoadout.ShieldScale;
                Shields.ShieldOffset = oLoadout.ShieldOffset;
                Shields.MaxShields.Level = int(Lerp(ShieldLoadout.ShieldLevelRange.X, ShieldLoadout.ShieldLevelRange.Y, float(GetScaledLevel() / 100)));
                if (ShieldLoadout.MaxShields.X != float(0) || ShieldLoadout.MaxShields.Y != float(0))
                {
                    Shields.MaxShields.X = ShieldLoadout.MaxShields.X;
                    Shields.MaxShields.Y = ShieldLoadout.MaxShields.Y;
                }
                Shields.ScaleShields();
                Shields.CurrentShields = Shields.MaxShields.Value;
                if (Shields.CurrentShields > float(0))
                {
                    bHasShields = bHasShields || TRUE;
                }
            }
        }
        PowerManager.MyPawn = Self;
        foreach oLoadout.Powers(PowerClass, )
        {
            PowerManager.AddPower(PowerClass);
        }
        StartFirstUsePowerDelay();
    }
}