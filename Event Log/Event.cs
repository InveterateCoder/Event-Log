using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace Event_Log
{
    public enum MediaMessageType { None, Voice, Video };

    public class Event
    {
        private DateTimeOffset _event_date;

        public Guid Guid { get; set; }

        public Event()
        {
            Guid = Guid.NewGuid();
        }
        public Event(Guid guid)
        {
            Guid = guid;
        }
        public Event(IXmlNode e)
        {
            var nodes = e.ChildNodes;
            Guid = Guid.Parse(nodes[0].InnerText);
            Name = nodes[1].InnerText;
            Date = DateTimeOffset.Parse(nodes[2].InnerText, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
            if (string.IsNullOrEmpty(nodes[3].InnerText))
                IconPath = new Uri("ms-appx:///Assets/item.png");
            else
                IconPath = new Uri($"ms-appdata:///local/Icons/{Guid.ToString()}.{nodes[3].InnerText}");
            Keep = nodes[4].InnerText == "t" ? true : false;
            Message = nodes[5].InnerText;
            MediaMessageType = string.IsNullOrEmpty(nodes[6].InnerText) ? MediaMessageType.None :
                nodes[6].InnerText == "V" ? MediaMessageType.Video : MediaMessageType.Voice;
            if (MediaMessageType != MediaMessageType.None)
            {
                string folder;
                if (MediaMessageType == MediaMessageType.Voice)
                    folder = "Audios";
                else
                    folder = "Videos";
                MediaMessageUri = new Uri($"ms-appdata:///local/{folder}/{Guid.ToString()}.{nodes[7].InnerText}");
            }
            if (!string.IsNullOrEmpty(nodes[8].InnerText))
            {
                var arr = nodes[2].InnerText.Split(' ');
                DateTimeOffset loc = DateTimeOffset.Parse($"{arr[0]} {nodes[8].InnerText} {arr[2]}", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
                if (loc > DateTimeOffset.Now)
                    Alarm = loc;
                else
                    nodes[8].InnerText = "";
            }
            HasStroke = nodes[9].InnerText == "t" ? true : false;
        }

        public Event Copy()
            => MemberwiseClone() as Event;
        

        public string Name { get; set; }
        public Uri IconPath { get; set; }
        public DateTimeOffset Date
        {
            get => _event_date;
            set
            {
                _event_date = value;
                if (value < DateTimeOffset.Now.Date)
                    IsPast = true;
                else
                    IsPast = false;
            }
        }
        public bool Keep { get; set; } = true;
        public string Message { get; set; } = "";
        public MediaMessageType MediaMessageType { get; set; }
        public Uri MediaMessageUri { get; set; }
        public bool IsPast { get; set; }
        public DateTimeOffset? Alarm { get; set; }
        public string DateS
        {
            get => Date.Date.ToString().Split(' ')[0];
        }
        public bool HasStroke { get; set; }

        public async Task SaveToFileAsync()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            XmlDocument doc = await XmlDocument.LoadFromFileAsync(file);
            var root = doc.FirstChild;
            var evnt = doc.CreateElement("it");
            evnt.AppendChild(doc.CreateElement("nm"));
            evnt.AppendChild(doc.CreateElement("gd"));
            evnt.AppendChild(doc.CreateElement("dt"));
            evnt.AppendChild(doc.CreateElement("ic"));
            evnt.AppendChild(doc.CreateElement("kp"));
            evnt.AppendChild(doc.CreateElement("mg"));
            evnt.AppendChild(doc.CreateElement("tp"));
            evnt.AppendChild(doc.CreateElement("md"));
            evnt.AppendChild(doc.CreateElement("nd"));
            evnt.AppendChild(doc.CreateElement("hs"));
            root.AppendChild(evnt);
            var nodes = evnt.ChildNodes;
            nodes[0].InnerText = Guid.ToString();
            nodes[1].InnerText = Name;
            nodes[2].InnerText = Date.ToString(CultureInfo.InvariantCulture.DateTimeFormat);
            nodes[3].InnerText = IconPath.AbsoluteUri.StartsWith("ms-appx") ? "" : IconPath.LocalPath.Substring(IconPath.LocalPath.LastIndexOf('.') + 1);
            nodes[4].InnerText = Keep ? "t" : "";
            nodes[5].InnerText = Message;
            nodes[6].InnerText = MediaMessageType == MediaMessageType.None ? "" :
                MediaMessageType == MediaMessageType.Video ? "V" : "A";
            nodes[7].InnerText = MediaMessageUri == null ? "" : MediaMessageUri.LocalPath.Substring(MediaMessageUri.LocalPath.LastIndexOf('.') + 1);
            nodes[8].InnerText = Alarm != null ? Alarm.Value.ToString(CultureInfo.InvariantCulture.DateTimeFormat).Split(' ')[1] : "";
            nodes[9].InnerText = HasStroke ? "t" : "";
            await doc.SaveToFileAsync(file);
        }
    }
}