using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Event_Log
{
    public class EventStorage
    {
        public List<YearEvents> CallendarActive = new List<YearEvents>();
        public List<YearEvents> CallendarPast = new List<YearEvents>();
        public List<string> Names = new List<string>();
        public List<Guid> Guids = new List<Guid>();

        public Event GetEvent(string name)
        {
            var list = CallendarActive;
            for (ushort c = 0; c < 2; c++)
            {
                foreach (var y in list)
                {
                    foreach (var m in y.Months)
                    {
                        foreach (var e in m.Events)
                        {
                            if (string.Equals(e.Name, name, StringComparison.CurrentCultureIgnoreCase))
                                return e;
                        }
                    }
                }
                list = CallendarPast;
            }
            return null;
        }

        public bool IsEmpty()
        {
            if (CallendarActive.Count == 0 && CallendarPast.Count == 0)
                return true;
            else
                return false;
        }

        public bool Exist(Guid guid)
        {
            if (Guids.Contains(guid))
                return true;
            else
                return false;
        }

        public void AddEvent(Event evnt)
        {
            Names.Add(evnt.Name);
            Guids.Add(evnt.Guid);
            List<YearEvents> collection = evnt.IsPast ? CallendarPast : CallendarActive;
            YearEvents year = collection.Where(i => i.Year == evnt.Date.Year).FirstOrDefault();
            if (year == null)
            {
                var last = collection.LastOrDefault();
                if (last == null)
                {
                    collection.Add(new YearEvents(evnt));
                    return;
                }
                foreach (var y in collection)
                {
                    if (evnt.Date.Year < y.Year)
                    {
                        collection.Insert(collection.IndexOf(y), new YearEvents(evnt));
                        return;
                    }
                    else if (last == y)
                    {
                        collection.Add(new YearEvents(evnt));
                        break;
                    }
                }
            }
            else
                year.AddSorted(evnt);
        }

        public async Task<bool> RemoveAsync(params Event[] evnts)
        {
            StorageFile xml = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            XmlDocument doc = await XmlDocument.LoadFromFileAsync(xml);
            List<YearEvents> collection = evnts[0].IsPast ? CallendarPast : CallendarActive;
            YearEvents year = collection.Where(i => i.Year == evnts[0].Date.Year).FirstOrDefault();
            MonthEvents month = year.Months.Where(i => i.Month == evnts[0].Date.Month).FirstOrDefault();
            foreach (Event evnt in evnts)
            {
                if (!evnt.IconPath.AbsoluteUri.StartsWith("ms-appx"))
                    await (await StorageFile.GetFileFromApplicationUriAsync(evnt.IconPath)).DeleteAsync();
                if (evnt.MediaMessageType != MediaMessageType.None)
                    await (await StorageFile.GetFileFromApplicationUriAsync(evnt.MediaMessageUri)).DeleteAsync();
                if (evnt.HasStroke)
                    await (await (await ApplicationData.Current.LocalFolder.
                        GetFolderAsync("Inks")).GetFileAsync(evnt.Guid.ToString() + ".gif")).DeleteAsync();
                var root = doc.FirstChild;
                foreach (var enode in root.ChildNodes)
                {
                    if (enode.FirstChild.InnerText == evnt.Guid.ToString())
                    {
                        root.RemoveChild(enode);
                        break;
                    }
                }
                Names.Remove(evnt.Name);
                Guids.Remove(evnt.Guid);
                if (evnt.Alarm != null)
                {
                    var notifier = ToastNotificationManager.CreateToastNotifier();
                    var notifs = notifier.GetScheduledToastNotifications();
                    var notif = notifs.Where(i => i.DeliveryTime == evnt.Alarm.Value).FirstOrDefault();
                    if (notif != null)
                        notifier.RemoveFromSchedule(notif);
                }
                month.Events.Remove(evnt);
            }
            await doc.SaveToFileAsync(xml);
            if (month.Events.Count > 0)
                return false;
            else
            {
                year.Months.Remove(month);
                if (year.Months.Count > 0)
                {
                    App.State.ActivePage = PageActive.Months;
                    return true;
                }
                else
                {
                    collection.Remove(year);
                    App.State.ActivePage = PageActive.Years;
                    return true;
                }
            }
        }

        public async static Task<EventStorage> DeserializeAsync()
        {
            EventStorage storage = new EventStorage();
            StorageFile file = null;
            XmlDocument doc;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            }
            catch
            {
                doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("root"));
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync("data.xml");
                await doc.SaveToFileAsync(file);
                return storage;
            }
            doc = await XmlDocument.LoadFromFileAsync(file);
            var root = doc.FirstChild;
            List<IXmlNode> list = new List<IXmlNode>();
            foreach (var node in root.ChildNodes)
            {
                Event evnt = new Event(node);
                if (evnt.IsPast && !evnt.Keep)
                {
                    if (!evnt.IconPath.AbsoluteUri.StartsWith("ms-appx:"))
                        await (await StorageFile.GetFileFromApplicationUriAsync(evnt.IconPath)).DeleteAsync();
                    if (evnt.MediaMessageType != MediaMessageType.None)
                        await (await StorageFile.GetFileFromApplicationUriAsync(evnt.MediaMessageUri)).DeleteAsync();
                    if (evnt.HasStroke)
                        await (await (await ApplicationData.Current.LocalFolder.GetFolderAsync("Inks")).GetFileAsync(evnt.Guid.ToString() + ".gif")).DeleteAsync();
                    list.Add(node);
                }
                else
                    storage.AddEvent(evnt);
            }
            foreach (var node in list)
                root.RemoveChild(node);
            await doc.SaveToFileAsync(file);
            return storage;
        }
    }
}