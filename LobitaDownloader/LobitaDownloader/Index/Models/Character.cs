using LobitaDownloader.Index.Interfaces;
using System.Collections.Generic;

namespace LobitaDownloader.Index.Models
{
    public class Character : ModelBase, Model
    {
        public string Name { get; set; }
        public int PostCount { get; set; }
        public ICollection<Series> Series { get; set; }
        public ICollection<Url> Urls { get; set; }

        public Character(int id, string name, int postCount, ICollection<Series> series, ICollection<Url> urls)
        {
            Id = id;
            Name = name;
            PostCount = postCount;
            Series = series;
            Urls = urls;
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
