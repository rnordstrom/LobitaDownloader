using LobitaDownloader.Index.Interfaces;

namespace LobitaDownloader.Index.Models
{
    public class Series : ModelBase, Model
    {
        public string Name { get; set; }
        public int PostCount { get; set; }

        public Series(int id, string name, int postCount)
        {
            Id = id;
            Name = name;
            PostCount = postCount;
        }

        public string GetName()
        {
            return Name;
        }

        public int GetCount()
        {
            return PostCount;
        }
    }
}
