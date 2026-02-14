using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOF_Team_Score.Model
{
    public enum EventType
    {
        EYOC,
        JWOC
    }

    public enum CompetitionType
    {
        Individual,
        Relay
    }

    public class Event
    {
        public static string EYOC = "European Youth Orienteering Championships";
        public static string JWOC = "Junior World Orienteering Championships";

        public static Dictionary<string, EventType> StringToEventType = new Dictionary<string, EventType>
        {
            { Event.EYOC, EventType.EYOC },
            { Event.JWOC, EventType.JWOC },
        };

        public static Dictionary<EventType, string> EventTypeToString = new Dictionary<EventType, string>
        {
            { EventType.EYOC, Event.EYOC },
            { EventType.JWOC, Event.JWOC },
        };

        public required int Id { get; set; }
        public required string Name { get; set; }
        public required DateOnly Date { get; set; }
        public required EventType EventType { get; set; }
        public required CompetitionType CompType { get; set; }
        public List<Clazz> Clazzes { get; set; } = new List<Clazz>();

        public override string ToString()
        {
            return $"{Name} ({Date.ToString("o", CultureInfo.InvariantCulture)}), {CompType}";
        }
    }

}
