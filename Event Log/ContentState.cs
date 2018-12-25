using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Event_Log
{
    public enum PageActive { Years, Months, Events}
    public class ContentState
    {

        public MenuSelection MenuSelected;
        public State PastEvents;
        public State ActiveEvents;
        public int Year
        {
            get
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    return ActiveEvents.Year;
                else
                    return PastEvents.Year;
            }
            set
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    ActiveEvents.Year = value;
                else
                    PastEvents.Year = value;
            }
        }
        public int Month
        {
            get
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    return ActiveEvents.Month;
                else
                    return PastEvents.Month;
            }
            set
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    ActiveEvents.Month = value;
                else
                    PastEvents.Month = value;
            }
        }
        public PageActive ActivePage
        {
            get
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    return ActiveEvents.Page;
                else
                    return PastEvents.Page;
            }
            set
            {
                if (MenuSelected == MenuSelection.Scheduled)
                    ActiveEvents.Page = value;
                else
                    PastEvents.Page = value;
            }
        }
        public bool GoBack()
        {
            switch (ActivePage)
            {
                case PageActive.Events:
                    ActivePage = PageActive.Months;
                    return true;
                case PageActive.Months:
                    ActivePage = PageActive.Years;
                    return true;
                default: return false;
            }
        }
        public string Serialize()
        {
            DataContractSerializer serializer = new DataContractSerializer(GetType());
            MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, this);
            var ret = Encoding.ASCII.GetString(stream.ToArray());
            stream.Dispose();
            return ret;
        }
        public static ContentState Deserialize(string str)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(ContentState));
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(str));
            var ret = serializer.ReadObject(stream) as ContentState;
            stream.Dispose();
            return ret;
        }
    }
}