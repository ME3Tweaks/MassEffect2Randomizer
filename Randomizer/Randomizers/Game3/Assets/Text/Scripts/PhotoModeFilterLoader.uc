public function InitializeFilters()
{
    local int idx;
    local MaterialInterface MIC;
    local MaterialEffect Effect;
    local Package P;
    local string PackageName;
    local string MaterialName;
    
    if (FilterMaterialPaths.Length > 0)
    {
        FilterEffects.AddItem(None);
    }
    for (idx = 0; idx < FilterMaterialPaths.Length; idx++)
    {
        MIC = MaterialInterface(DynamicLoadObject(FilterMaterialPaths[idx], Class'MaterialInstanceConstant'));
        if (MIC != None)
        {
            Effect = new (Self) Class'MaterialEffect';
            Effect.EffectName = Name("PhotoMode_Filter" $ idx);
            Effect.bMergePostUber = TRUE;
            Effect.Material = MIC;
            FilterEffects.AddItem(Effect);
            continue;
        }
        PackageName = Left(FilterMaterialPaths[idx], InStr(FilterMaterialPaths[idx], ".", FALSE, , ));
        MaterialName = Right(FilterMaterialPaths[idx], (Len(FilterMaterialPaths[idx]) - Len(PackageName)) - 1);
        LogInternal((("ATTEMPT Load filter from package: " $ PackageName) $ " ") $ MaterialName, );
        Class'SFXGame'.static.LoadPackage(PackageName);
        MIC = MaterialInterface(DynamicLoadObject(MaterialName, Class'MaterialInterface'));
        if (MIC != None)
        {
            LogInternal((("Load filter from package: " $ PackageName) $ " ") $ MaterialName, );
            Effect = new (Self) Class'MaterialEffect';
            Effect.EffectName = Name("PhotoMode_Filter" $ idx);
            Effect.bMergePostUber = TRUE;
            Effect.Material = MIC;
            FilterEffects.AddItem(Effect);
            continue;
        }
    }
}