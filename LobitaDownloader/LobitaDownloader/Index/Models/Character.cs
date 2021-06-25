using System.Collections.Generic;

namespace LobitaDownloader.Index.Models
{
    public class Character : ModelObject
    {
        public string Name { get; set; }
        public ICollection<Url> Urls { get; set; }

        public Character(int id, string name, ICollection<Url> urls)
        {
            Id = id;
            Name = name;
            Urls = urls;
        }
    }
}
