namespace LobitaDownloader
{
    public interface IConfigManager
    {
        public string GetItemByName(string name);
        public void ChangeItemByName(string name, string value);
    }
}
