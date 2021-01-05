using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LobitaDownloader
{
    public class XmlIndexPersistence : IIndexPersistence
    {
        private string _backupLocation;
        public string TagsFileName { get; }
        public string SeriesFileName { get; }

        public XmlIndexPersistence(string backupLocation)
        {
            _backupLocation = backupLocation;
            TagsFileName = Path.Join(backupLocation, "tags.xml");
            SeriesFileName = Path.Join(backupLocation, "series.xml");
        }
        
        public void CleanTagLinks()
        {
            FileInfo tagsFileInfo = new FileInfo(TagsFileName);

            if (tagsFileInfo.Exists)
            {
                tagsFileInfo.Delete();
            }
        }

        public void CleanSeriesTags()
        {
            FileInfo seriesFileInfo = new FileInfo(SeriesFileName);

            if (seriesFileInfo.Exists)
            {
                seriesFileInfo.Delete();
            }
        }

        public void PersistTagLinks(IDictionary<string, List<string>> index)
        {
            XmlDocument tagsDoc = new XmlDocument();

            XmlDeclaration xmlDeclaration = tagsDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = tagsDoc.DocumentElement;
            tagsDoc.InsertBefore(xmlDeclaration, root);
    
            XmlElement tagsElement = tagsDoc.CreateElement(string.Empty, "tags", string.Empty);
            XmlElement tagElement;
            XmlElement linkElement;
            
            tagsDoc.AppendChild(tagsElement);

            int i = 1;
            string output;

            foreach (string tagName in index.Keys)
            {
                output = $"Backing up tag ({i++} / {index.Keys.Count}).";
                PrintUtils.PrintRow(output, 0, 0);

                tagElement = tagsDoc.CreateElement(string.Empty, "tag", string.Empty);

                tagElement.SetAttribute("name", tagName);

                foreach (string link in index[tagName])
                {
                    linkElement = tagsDoc.CreateElement(string.Empty, "url", string.Empty);

                    linkElement.AppendChild(tagsDoc.CreateTextNode(link));
                    tagElement.AppendChild(linkElement);
                }

                tagsElement.AppendChild(tagElement);
            }

            tagsDoc.Save(TagsFileName);
        }

        public void PersistSeriesTags(IDictionary<string, HashSet<string>> index)
        {
            XmlDocument seriesDoc = new XmlDocument();

            XmlDeclaration xmlDeclaration = seriesDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = seriesDoc.DocumentElement;
            seriesDoc.InsertBefore(xmlDeclaration, root);

            XmlElement allSeriesElement = seriesDoc.CreateElement(string.Empty, "all_series", string.Empty);
            XmlElement seriesElement;
            XmlElement tagElement;

            seriesDoc.AppendChild(allSeriesElement);

            int i = 1;
            string output;

            foreach (string seriesName in index.Keys)
            {
                output = $"Backing up series ({i++} / {index.Keys.Count}).";
                PrintUtils.PrintRow(output, 0, 0);

                seriesElement = seriesDoc.CreateElement(string.Empty, "series", string.Empty);

                seriesElement.SetAttribute("name", seriesName);

                foreach (string tagName in index[seriesName])
                {
                    tagElement = seriesDoc.CreateElement(string.Empty, "tag", string.Empty);

                    tagElement.SetAttribute("name", tagName);
                    seriesElement.AppendChild(tagElement);
                }

                allSeriesElement.AppendChild(seriesElement);
            }

            seriesDoc.Save(SeriesFileName);
        }

        public IDictionary<string, List<string>> GetTagIndex()
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
            List<string> links;

            doc.Load(TagsFileName);

            XmlNodeList tagNodes = doc.SelectSingleNode("tags").SelectNodes("tag");

            foreach (XmlElement tag in tagNodes)
            {
                links = new List<string>();

                foreach (XmlElement link in tag.SelectNodes("url"))
                {
                    links.Add(link.InnerText);
                }

                tagLinks.Add(tag.GetAttribute("name"), links);
            }

            return tagLinks;
        }

        public IDictionary<string, HashSet<string>> GetSeriesIndex()
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, HashSet<string>> seriesTags = new Dictionary<string, HashSet<string>>();
            HashSet<string> tags;

            doc.Load(SeriesFileName);

            XmlNodeList seriesNodes = doc.SelectSingleNode("all_series").SelectNodes("series");

            foreach (XmlElement series in seriesNodes)
            {
                tags = new HashSet<string>();

                foreach (XmlElement tag in series.SelectNodes("tag"))
                {
                    tags.Add(tag.GetAttribute("name"));
                }

                seriesTags.Add(series.GetAttribute("name"), tags);
            }

            return seriesTags;
        }

        public bool IsConnected()
        {
            return Directory.Exists(_backupLocation);
        }
    }
}
