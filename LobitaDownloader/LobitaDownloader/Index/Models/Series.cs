using System.Collections.Generic;

namespace LobitaDownloader.Index.Models
{
    public class Series : ModelObject
    {
        public string Name { get; set; }
        public ICollection<Character> Characters { get; set; }

        public Series(int id, string name, ICollection<Character> characters)
        {
            Id = id;
            Name = name;
            Characters = characters;
        }
    }
}
