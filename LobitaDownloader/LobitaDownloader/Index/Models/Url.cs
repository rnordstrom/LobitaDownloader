using LobitaDownloader.Index.Interfaces;

namespace LobitaDownloader.Index.Models
{
    public class Url : ModelBase, Model
    {
        public string Link { get; set; }

        public Url(int id, string link)
        {
            Id = id;
            Link = link;
        }

        public string GetName()
        {
            return Link;
        }

        public int GetCount()
        {
            throw new System.NotImplementedException();
        }
    }
}
