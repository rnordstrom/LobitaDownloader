using LobitaDownloader.Index.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LobitaDownloader
{
    public class XmlIndexBackup : IIndexBackup
    {
        private string _backupLocation;
        public string CharactersFileName { get; set; }
        public string SeriesFileName { get; set; }
        public string DataFileName { get; set; }
        private const string extension = ".xml";
        private XmlDocument charactersDoc = null;
        private XmlDocument seriesDoc = null;
        private XmlDocument dataDoc = null;
        private int currentDoc = 0;

        public XmlIndexBackup(IConfigManager config)
        {
            _backupLocation = config.GetItemByName("BackupLocation");
            CharactersFileName = Path.Join(_backupLocation, "characters" + extension);
            SeriesFileName = Path.Join(_backupLocation, "series" + extension);
            DataFileName = Path.Join(_backupLocation, "data");
        }
        
        private void CleanTagLinks()
        {
            FileInfo tagsFileInfo = new FileInfo(CharactersFileName);

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

        public void DeleteDataDocuments()
        {
            DirectoryInfo di = new DirectoryInfo(_backupLocation);

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name.Contains("data"))
                {
                    file.Delete();
                }
            }
        }

        public void ResetDocumentStatus()
        {
            currentDoc = 0;
        }

        public void MarkForUpdate(List<string> characterNames)
        {
            if (charactersDoc == null)
            {
                charactersDoc = LoadDocument(CharactersFileName);
            }

            XmlNode characterNode;

            foreach (string characterName in characterNames)
            {
                try
                {
                    characterNode = charactersDoc.SelectSingleNode("characters").SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                    characterNode.Attributes["status"].Value = ModificationStatus.UNMODIFIED.ToString();
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine($"{characterName} is not a valid tag.");
                }
            }

            charactersDoc.Save(CharactersFileName);
        }

        public void MarkAsDone(List<string> characterNames)
        {
            if (charactersDoc == null)
            {
                charactersDoc = LoadDocument(CharactersFileName);
            }

            XmlNode characterNode;

            foreach (string characterName in characterNames)
            {
                characterNode = charactersDoc.SelectSingleNode("characters").SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                characterNode.Attributes["status"].Value = ModificationStatus.DONE.ToString();
            }

            charactersDoc.Save(CharactersFileName);
        }

        public void MarkAsSaved(List<string> characterNames)
        {
            if (dataDoc == null)
            {
                dataDoc = LoadDocument(DataFileName + currentDoc + extension);
            }

            XmlNode characterNode;

            foreach (string characterName in characterNames)
            {
                characterNode = dataDoc.SelectSingleNode("data").SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                characterNode.Attributes["status"].Value = PersistenceStatus.SAVED.ToString();
            }

            dataDoc.Save(DataFileName + currentDoc + extension);
        }

        public void MarkAsUnsaved(List<string> characterNames)
        {
            if (dataDoc == null)
            {
                dataDoc = LoadDocument(DataFileName + currentDoc + extension);
            }

            XmlNode characterNode;

            foreach (string characterName in characterNames)
            {
                characterNode = dataDoc.SelectSingleNode("data").SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                characterNode.Attributes["status"].Value = PersistenceStatus.UNSAVED.ToString();
            }

            dataDoc.Save(DataFileName + currentDoc + extension);
        }

        public void IndexCharacters(IDictionary<string, Character> index)
        {
            CleanTagLinks();

            charactersDoc = InitializeDocument();

            XmlElement charactersElement = charactersDoc.CreateElement(string.Empty, "characters", string.Empty);
            XmlElement characterElement;

            charactersDoc.AppendChild(charactersElement);

            int i = 1;
            string output;

            foreach (string characterName in index.Keys)
            {
                output = $"Backing up tag ({i++}).";
                PrintUtils.PrintRow(output, 0, 0);

                characterElement = charactersDoc.CreateElement(string.Empty, "character", string.Empty);

                characterElement.SetAttribute("name", ReplaceDoubleQuotes(characterName));
                characterElement.SetAttribute("id", index[characterName].Id.ToString());
                characterElement.SetAttribute("status", ModificationStatus.UNMODIFIED.ToString());

                charactersElement.AppendChild(characterElement);
            }

            charactersDoc.Save(CharactersFileName);
        }

        public void IndexSeries(IDictionary<string, Series> index)
        {
            CleanSeries();

            seriesDoc = InitializeDocument();

            XmlElement allSeriesElement = seriesDoc.CreateElement(string.Empty, "all_series", string.Empty);
            XmlElement seriesElement;

            seriesDoc.AppendChild(allSeriesElement);

            int i = 1;
            string output;

            foreach (string seriesName in index.Keys)
            {
                output = $"Backing up series ({i++}).";
                PrintUtils.PrintRow(output, 0, 0);

                seriesElement = seriesDoc.CreateElement(string.Empty, "series", string.Empty);

                seriesElement.SetAttribute("name", ReplaceDoubleQuotes(seriesName));
                seriesElement.SetAttribute("id", index[seriesName].Id.ToString());

                allSeriesElement.AppendChild(seriesElement);
            }

            seriesDoc.Save(SeriesFileName);
        }

        public void WriteCharacterData(IDictionary<string, Character> index)
        {
            currentDoc++;

            while (File.Exists(DataFileName + currentDoc + extension))
            {
                currentDoc++;
            }

            dataDoc = InitializeDocument();

            XmlElement dataElement = dataDoc.CreateElement(string.Empty, "data", string.Empty);
            dataDoc.AppendChild(dataElement);

            XmlElement characterElement;
            XmlElement urlElement;
            XmlElement seriesElement;

            foreach (string characterName in index.Keys)
            {
                characterElement = (XmlElement)dataElement.SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                if (characterElement == null)
                {
                    characterElement = dataDoc.CreateElement(string.Empty, "character", string.Empty);

                    characterElement.SetAttribute("name", ReplaceDoubleQuotes(characterName));
                    characterElement.SetAttribute("id", index[characterName].Id.ToString());
                    characterElement.SetAttribute("post_count", index[characterName].PostCount.ToString());
                    characterElement.SetAttribute("status", PersistenceStatus.UNSAVED.ToString());

                    dataElement.AppendChild(characterElement);
                }
                else
                {
                    while (characterElement.HasChildNodes)
                    {
                        characterElement.RemoveChild(characterElement.FirstChild);
                    }
                }

                foreach (Url url in index[characterName].Urls)
                {
                    urlElement = dataDoc.CreateElement(string.Empty, "url", string.Empty);

                    urlElement.AppendChild(dataDoc.CreateTextNode(url.Link));
                    urlElement.SetAttribute("id", url.Id.ToString());
                    characterElement.AppendChild(urlElement);
                }

                foreach (Series series in index[characterName].Series)
                {
                    seriesElement = dataDoc.CreateElement(string.Empty, "series", string.Empty);

                    seriesElement.SetAttribute("name", ReplaceDoubleQuotes(series.Name));
                    seriesElement.SetAttribute("id", series.Id.ToString());
                    seriesElement.SetAttribute("post_count", series.PostCount.ToString());
                    characterElement.AppendChild(seriesElement);
                }
            }

            dataDoc.Save(DataFileName + currentDoc + extension);
        }

        public bool ReadCharacterData(PersistenceStatus status, int batchSize, out IDictionary<string, Character> data)
        {
            currentDoc++;

            try
            {
                dataDoc = LoadDocument(DataFileName + currentDoc + extension);
            }
            catch (IOException)
            {
                data = null;

                return false;
            }

            Dictionary<string, Character> characterIndex = new Dictionary<string, Character>();
            List<Url> tempUrls;
            List<Series> tempSeries;
            string characterName;

            XmlNodeList characterNodes = dataDoc.SelectSingleNode("data").SelectNodes("character");

            foreach (XmlElement character in characterNodes)
            {
                if (character.GetAttribute("status") == status.ToString())
                {
                    tempUrls = new List<Url>();
                    tempSeries = new List<Series>();

                    characterName = character.GetAttribute("name");

                    foreach (XmlElement urlTag in character.SelectNodes("url"))
                    {
                        tempUrls.Add(new Url(int.Parse(urlTag.GetAttribute("id")), urlTag.InnerText));
                    }

                    foreach (XmlElement seriesTag in character.SelectNodes("series"))
                    {
                        tempSeries.Add(new Series(
                            int.Parse(seriesTag.GetAttribute("id")),
                            seriesTag.GetAttribute("name"),
                            int.Parse(seriesTag.GetAttribute("post_count"))));
                    }

                    characterIndex.Add(characterName, new Character(
                        int.Parse(character.GetAttribute("id")),
                        characterName,
                        int.Parse(character.GetAttribute("post_count")),
                        tempSeries,
                        tempUrls));
                }

                if (batchSize > 0 && characterIndex.Count == batchSize)
                {
                    break;
                }
            }

            data = characterIndex;

            return true;
        }

        public IDictionary<string, Character> GetCharacterIndex(ModificationStatus status, int batchSize)
        {
            if (charactersDoc == null)
            {
                charactersDoc = LoadDocument(CharactersFileName);
            }

            Dictionary<string, Character> characterIndex = new Dictionary<string, Character>();
            string characterName;

            XmlNodeList characterNodes = charactersDoc.SelectSingleNode("characters").SelectNodes("character");

            foreach (XmlElement character in characterNodes)
            {
                if (character.GetAttribute("status") == status.ToString())
                {
                    characterName = character.GetAttribute("name");

                    characterIndex.Add(characterName, new Character(
                        int.Parse(character.GetAttribute("id")),
                        characterName,
                        0,
                        new List<Series>(),
                        new List<Url>()));
                }

                if (batchSize > 0 && characterIndex.Count == batchSize)
                {
                    break;
                }
            }

            return characterIndex;
        }

        public IDictionary<string, Series> GetSeriesIndex()
        {
            if (seriesDoc == null)
            {
                seriesDoc = LoadDocument(SeriesFileName);
            }

            Dictionary<string, Series> seriesIndex = new Dictionary<string, Series>();

            seriesDoc.Load(SeriesFileName);

            XmlNodeList seriesNodes = seriesDoc.SelectSingleNode("all_series").SelectNodes("series");
            string seriesName;

            foreach (XmlElement seriesNode in seriesNodes)
            {
                seriesName = seriesNode.GetAttribute("name");

                seriesIndex.Add(seriesName, new Series(
                    int.Parse(seriesNode.GetAttribute("id")),
                    seriesName,
                    0));
            }

            return seriesIndex;
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
