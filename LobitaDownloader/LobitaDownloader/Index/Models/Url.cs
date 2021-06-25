namespace LobitaDownloader.Index.Models
{
    public class Url : ModelObject
    {
        public string Link { get; set; }

        public Url(int id, string link)
        {
            Id = id;
            Link = link;
        }
    }
}
