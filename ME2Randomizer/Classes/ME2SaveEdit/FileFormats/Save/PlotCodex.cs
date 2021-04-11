﻿using System.Collections.Generic;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats.Save
{
    // 00BAEED0
    public class PlotCodex : IUnrealSerializable
    {
        public List<PlotCodexPage> Pages;

        public void Serialize(IUnrealStream stream)
        {
            stream.Serialize<PlotCodexPage>(ref this.Pages);
        }
    }
}
