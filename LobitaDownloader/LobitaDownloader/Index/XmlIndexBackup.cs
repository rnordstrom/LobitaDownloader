using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LobitaDownloader
{
    public class XmlIndexBackup : IIndexBackup
    {
        private string _backupLocation;
        public string TagsFileName { get; set; }
        public string SeriesFileName { get; set; }
        private XmlDocument tagsDoc = null;
        private XmlDocument seriesDoc = null;

        public XmlIndexBackup(IConfigManager config)
        {
            _backupLocation = config.GetItemByName("BackupLocation");
            TagsFileName = Path.Join(_backupLocation, "tags.xml");
            SeriesFileName = Path.Join(_backupLocation, "series.xml");
        }
        
        private void CleanTagLinks()
        {
            FileInfo tagsFileInfo = new FileInfo(TagsFileName);

            if (tagsFileInfo.Exists)
            {
                tagsFileInfo.Delete();
            }
        }

        private void CleanSeries()
        {
            FileInfo seriesFileInfo = new FileInfo(SeriesFileName);

            if (seriesFileInfo.Exists)
            {
                seriesFileInfo.Delete();
            }
        }

        public void MarkForUpdate(List<string> tagNames)
        {
            if (tagsDoc == null)
            {
                tagsDoc = LoadDocument(TagsFileName);
            }

            XmlNode tagNode;

            foreach (string tagName in tagNames)
            {
                try
                {
                    tagNode = tagsDoc.SelectSingleNode("tags").SelectSingleNode("tag[@name=\"" + ReplaceDoubleQuotes(tagName) + "\"]");

                    tagNode.Attributes["status"].Value = ModificationStatus.UNMODIFIED.ToString();
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine($"{tagName} is not a valid tag.");
                }
            }

            tagsDoc.Save(TagsFileName);
        }

        public void BackupTagLinks(IDictionary<string, List<string>> index)
        {
            if (tagsDoc == null)
            {
                tagsDoc = LoadDocument(TagsFileName);
            }

            XmlNode tagNode;
            XmlElement linkElement;

            foreach (string tagName in index.Keys)
            {
                tagNode = tagsDoc.SelectSingleNode("tags").SelectSingleNode("tag[@name=\"" + ReplaceDoubleQuotes(tagName) + "\"]");

                while (tagNode.HasChildNodes)
                {
                    tagNode.RemoveChild(tagNode.FirstChild);
                }

                foreach (string link in index[tagName])
                {
                    linkElement = tagsDoc.CreateElement(string.Empty, "url", string.Empty);

                    linkElement.AppendChild(tagsDoc.CreateTextNode(link));
                    tagNode.AppendChild(linkElement);
                }

                tagNode.Attributes["status"].Value = ModificationStatus.DONE.ToString();
            }

            tagsDoc.Save(TagsFileName);
        }

        public void BackupSeriesTags(IDictionary<string, HashSet<string>> index)
        {
            if (seriesDoc == null)
            {
                seriesDoc = LoadDocument(SeriesFileName);
            }

            XmlNode seriesNode;
            XmlElement tagElement;

            foreach (string seriesName in index.Keys)
            {
                seriesNode = seriesDoc.SelectSingleNode("all_series").SelectSingleNode("series[@name=\"" + ReplaceDoubleQuotes(seriesName) + "\"]");

                foreach (string tagName in index[seriesName])
                {
                    if (seriesNode.SelectSingleNode("tag[@name=\"" + ReplaceDoubleQuotes(tagName) + "\"]") == null)
                    {
                        tagElement = seriesDoc.CreateElement(string.Empty, "tag", string.Empty);

                        tagElement.SetAttribute("name", ReplaceDoubleQuotes(tagName));
                        seriesNode.AppendChild(tagElement);

                    }
                }
            }
            
            seriesDoc.Save(SeriesFileName);
        }

        public void BackupTagNames(List<string> tagNames)
        {
            CleanTagLinks();

            tagsDoc = InitializeDocument();

            XmlElement tagsElement = tagsDoc.CreateElement(string.Empty, "tags", string.Empty);
            XmlElement tagElement;

            tagsDoc.AppendChild(tagsElement);

            int i = 1;
            string output;

            foreach (string tagName in tagNames)
            {
                output = $"Backing up tag ({i++}).";
                PrintUtils.PrintRow(output, 0, 0);

                tagElement = tagsDoc.CreateElement(string.Empty, "tag", string.Empty);

                tagElement.SetAttribute("name", ReplaceDoubleQuotes(tagName));
                tagElement.SetAttribute("status", ModificationStatus.UNMODIFIED.ToString());
                tagsElement.AppendChild(tagElement);
            }

            tagsDoc.Save(TagsFileName);
        }

        public void BackupSeriesNames(List<string> seriesNames)
        {
            CleanSeries();

            seriesDoc = InitializeDocument();

            XmlElement allSeriesElement = seriesDoc.CreateElement(string.Empty, "all_series", string.Empty);
            XmlElement seriesElement;

            seriesDoc.AppendChild(allSeriesElement);

            int i = 1;
            string output;

            foreach (string seriesName in seriesNames)
            {
                output = $"Backing up series ({i++}).";
                PrintUtils.PrintRow(output, 0, 0);

                seriesElement = seriesDoc.CreateElement(string.Empty, "series", string.Empty);

                seriesElement.SetAttribute("name", ReplaceDoubleQuotes(seriesName));
                allSeriesElement.AppendChild(seriesElement);
            }

            seriesDoc.Save(SeriesFileName);
        }

        public IDictionary<string, List<string>> GetTagIndex(ModificationStatus status, int batchSize = -1)
        {
            if (tagsDoc == null)
            {
                tagsDoc = LoadDocument(TagsFileName);
            }

            Dictionary<string, List<string>> tagLinks = new Dictionary<string, List<string>>();
            List<string> links;

            XmlNodeList tagNodes = tagsDoc.SelectSingleNode("tags").SelectNodes("tag");

            foreach (XmlElement tag in tagNodes)
            {
                if (tag.GetAttribute("status") == status.ToString())
                {
                    links = new List<string>();

                    foreach (XmlElement link in tag.SelectNodes("url"))
                    {
                        links.Add(link.InnerText);
                    }

                    tagLinks.Add(tag.GetAttribute("name"), links);
                }

                if (batchSize > 0 && tagLinks.Count == batchSize)
                {
                    break;
                }
            }

            return tagLinks;
        }

        public IDictionary<string, HashSet<string>> GetSeriesIndex()
        {
            if (seriesDoc == null)
            {
                seriesDoc = LoadDocument(SeriesFileName);
            }

            Dictionary<string, HashSet<string>> seriesTags = new Dictionary<string, HashSet<string>>();
            HashSet<string> tags;

            seriesDoc.Load(SeriesFileName);

            XmlNodeList seriesNodes = seriesDoc.SelectSingleNode("all_series").SelectNodes("series");

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

        private XmlDocument InitializeDocument()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            return doc;
        }

        private XmlDocument LoadDocument(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(fileName);

            return doc;
        }

        private string ReplaceDoubleQuotes(string tagName)
        {
            if (tagName.Contains("\""))
            {
                tagName = tagName.Replace("\"", "'");
            }

            return tagName;
        }
    }
}
