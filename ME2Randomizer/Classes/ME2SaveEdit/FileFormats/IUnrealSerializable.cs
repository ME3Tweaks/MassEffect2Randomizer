namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public interface IUnrealSerializable
    {
        void Serialize(IUnrealStream stream);
    }
}
