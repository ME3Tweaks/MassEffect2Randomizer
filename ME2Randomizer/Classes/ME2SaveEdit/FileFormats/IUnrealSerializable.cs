namespace RandomizerUI.Classes.ME2SaveEdit.FileFormats
{
    public interface IUnrealSerializable
    {
        void Serialize(IUnrealStream stream);
    }
}
