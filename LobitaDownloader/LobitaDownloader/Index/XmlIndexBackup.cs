﻿using LobitaDownloader.Index.Models;
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
        private XmlDocument charactersDoc = null;
        private XmlDocument seriesDoc = null;

        public XmlIndexBackup(IConfigManager config)
        {
            _backupLocation = config.GetItemByName("BackupLocation");
            CharactersFileName = Path.Join(_backupLocation, "characters.xml");
            SeriesFileName = Path.Join(_backupLocation, "series.xml");
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

        public void BackupCharacterData(IDictionary<string, Character> index)
        {
            if (charactersDoc == null)
            {
                charactersDoc = LoadDocument(CharactersFileName);
            }

            XmlNode characterNode;
            XmlElement urlElement;

            foreach (string characterName in index.Keys)
            {
                characterNode = charactersDoc.SelectSingleNode("characters").SelectSingleNode("character[@name=\"" + ReplaceDoubleQuotes(characterName) + "\"]");

                characterNode.Attributes["id"].Value = index[characterName].Id.ToString();

                while (characterNode.HasChildNodes)
                {
                    characterNode.RemoveChild(characterNode.FirstChild);
                }

                foreach (Url url in index[characterName].Urls)
                {
                    urlElement = charactersDoc.CreateElement(string.Empty, "url", string.Empty);

                    urlElement.AppendChild(charactersDoc.CreateTextNode(url.Link));
                    urlElement.SetAttribute("id", url.Id.ToString());
                    characterNode.AppendChild(urlElement);
                }

                characterNode.Attributes["status"].Value = ModificationStatus.DONE.ToString();
            }

            charactersDoc.Save(CharactersFileName);
        }

        public void BackupSeriesData(IDictionary<string, Series> index)
        {
            if (seriesDoc == null)
            {
                seriesDoc = LoadDocument(SeriesFileName);
            }

            XmlNode seriesNode;
            XmlElement characterElement;

            foreach (string seriesName in index.Keys)
            {
                seriesNode = seriesDoc.SelectSingleNode("all_series").SelectSingleNode("series[@name=\"" + ReplaceDoubleQuotes(seriesName) + "\"]");

                foreach (Character character in index[seriesName].Characters)
                {
                    if (seriesNode.SelectSingleNode("tag[@name=\"" + ReplaceDoubleQuotes(character.Name) + "\"]") == null)
                    {
                        characterElement = seriesDoc.CreateElement(string.Empty, "character", string.Empty);

                        characterElement.SetAttribute("name", ReplaceDoubleQuotes(character.Name));
                        characterElement.SetAttribute("id", character.Id.ToString());
                        seriesNode.AppendChild(characterElement);

                    }
                }
            }
            
            seriesDoc.Save(SeriesFileName);
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

        public IDictionary<string, Character> GetCharacterIndex(ModificationStatus status, int batchSize = -1)
        {
            if (charactersDoc == null)
            {
                charactersDoc = LoadDocument(CharactersFileName);
            }

            Dictionary<string, Character> characterIndex = new Dictionary<string, Character>();
            List<Url> tempUrls;
            string characterName;

            XmlNodeList characterNodes = charactersDoc.SelectSingleNode("characters").SelectNodes("character");

            foreach (XmlElement character in characterNodes)
            {
                if (character.GetAttribute("status") == status.ToString())
                {
                    tempUrls = new List<Url>();
                    characterName = character.GetAttribute("name");

                    foreach (XmlElement urlTag in character.SelectNodes("url"))
                    {
                        tempUrls.Add(new Url(int.Parse(urlTag.GetAttribute("id")), urlTag.InnerText));
                    }

                    characterIndex.Add(characterName, new Character(int.Parse(character.GetAttribute("id")), characterName, tempUrls));
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
            List<Character> characters;

            seriesDoc.Load(SeriesFileName);

            XmlNodeList seriesNodes = seriesDoc.SelectSingleNode("all_series").SelectNodes("series");
            string seriesName;

            foreach (XmlElement seriesNode in seriesNodes)
            {
                characters = new List<Character>();
                seriesName = seriesNode.GetAttribute("name");

                foreach (XmlElement characterTag in seriesNode.SelectNodes("character"))
                {
                    characters.Add(new Character(int.Parse(characterTag.GetAttribute("id")), characterTag.GetAttribute("name"), new List<Url>()));
                }

                seriesIndex.Add(seriesName, new Series(int.Parse(seriesNode.GetAttribute("id")), seriesName, characters));
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
