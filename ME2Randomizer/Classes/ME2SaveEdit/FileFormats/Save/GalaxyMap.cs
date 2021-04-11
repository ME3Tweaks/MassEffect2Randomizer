using System.Collections.Generic;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats.Save
{
    // 00BAE380
    public class GalaxyMap : IUnrealSerializable
    {
        public List<Planet> Planets;

        public void Serialize(IUnrealStream stream)
        {
            stream.Serialize(ref this.Planets);
        }
    }
}
