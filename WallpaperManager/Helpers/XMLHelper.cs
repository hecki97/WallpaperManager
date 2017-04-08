using System;
using System.Collections.Generic;
using System.Xml;

namespace WallpaperManager
{
    class XMLHelper
    {
        public static void CreateXmlDocument(string documentSavePath, string documentName, string rootTagName) {
            XmlDocument xml = CreateXmlDocument(documentName, rootTagName);
            xml.Save(documentSavePath + @"\" + documentName);
        }

        public static XmlDocument CreateXmlDocument(string documentName, string rootTagName) {
            XmlDocument xml = new XmlDocument();
            AddXmlHeader(xml, rootTagName);
            return xml;
        }

        public static XmlDocument AddXmlHeader(XmlDocument xml, string rootTagName)
        {
            XmlNode xmlHead = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            xml.AppendChild(xmlHead);
            XmlNode xmlRoot = xml.CreateElement(rootTagName);
            xml.AppendChild(xmlRoot);
            return xml;
        }

        public static XmlDocument LoadXmlDocument(string xmlFilePath) {
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlFilePath);
            return xml;
        }

        public static XmlNodeList GetElementsByTagName(string xmlFilePath, string tagName) {
            XmlDocument xml = LoadXmlDocument(xmlFilePath);
            return xml.GetElementsByTagName(tagName);
        }

        //TODO: Do I really need an extra function for one line of code? I don't know
        public static XmlNodeList GetElementsByTagName(XmlDocument xmlFile, string tagName) {
            return xmlFile.GetElementsByTagName(tagName);
        }

        public static Dictionary<string, List<string>> GetAttributesByTagName(string xmlFilePath, string tagName, string[] attributes) {
            Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
            XmlNodeList nodeList = GetElementsByTagName(xmlFilePath, tagName);
            for (int i = 0; i < nodeList.Count; i++) {
                List<string> list = new List<string>();
                for (int j = 0; j < attributes.Length; j++) {
                    string currentValue = nodeList[i].Attributes[attributes[j]].Value;
                    list.Add((string.IsNullOrEmpty(currentValue)) ? string.Empty : currentValue);
                }
                dictionary.Add(nodeList[i].Value, list);
            }
            return dictionary;
        }

        public static Dictionary<string, Dictionary<string, string>> GetAttributesByTagName(XmlDocument xmlFile, string tagName, string[] attributes) {
            Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
            XmlNodeList nodeList = GetElementsByTagName(xmlFile, tagName);
            for (int i = 0; i < nodeList.Count; i++) {
                Dictionary<string, string> tmpDictionary = new Dictionary<string, string>();
                for (int j = 0; j < attributes.Length; j++) {
                    XmlAttribute currentAttribute = nodeList[i].Attributes[attributes[j]];
                    tmpDictionary.Add(currentAttribute.Name, (string.IsNullOrEmpty(currentAttribute.Value)) ? string.Empty : currentAttribute.Value);
                }
                dictionary.Add(nodeList[i].Name, tmpDictionary);
            }
            return dictionary;
        }

        public static void AddElement(string xmlFilePath, string tagName, Dictionary<string, string> attributes) {
            XmlDocument xml = LoadXmlDocument(xmlFilePath);
            XmlElement newElement = xml.CreateElement(tagName);
            foreach (string key in attributes.Keys) {
                newElement.SetAttribute(key, attributes[key]);
            }
            xml.DocumentElement.AppendChild(newElement);
            xml.Save(xmlFilePath);
        }

        public static XmlDocument AddElement(XmlDocument xmlFile, string tagName, Dictionary<string, string> attributes) {
            XmlElement newElement = xmlFile.CreateElement(tagName);
            foreach (string key in attributes.Keys) {
                newElement.SetAttribute(key, attributes[key]);
            }
            xmlFile.DocumentElement.AppendChild(newElement);
            return xmlFile;
        }

        public static void SaveXmlDocument(XmlDocument xml, string xmlFilePath) {
            xml.RemoveAll();
            xml = AddXmlHeader(xml, "GameList");
            xml.Save(xmlFilePath);
        }

        public static void SaveXmlDocument(string xmlFilePath, List<Dictionary<string, string>> elements)
        {
            XmlDocument xml = LoadXmlDocument(xmlFilePath);
            xml.RemoveAll();
            xml = AddXmlHeader(xml, "GameList");
            for (int i = 0; i < elements.Count; i++) {
                xml = AddElement(xml, "Game", elements[i]);
            }
            xml.Save(xmlFilePath);
        }
    }
}
