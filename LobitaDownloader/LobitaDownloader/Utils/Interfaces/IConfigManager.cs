namespace LobitaDownloader
{
    public interface IConfigManager
    {
        public AutoMode CheckAutoMode(string cmdHandle);
        public string GetItemByName(string name);
        public void ChangeItemByName(string name, string value);
    }
}
