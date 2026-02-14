using IOF_Team_Score.Model;
using System.Xml.Linq;

namespace IOF_Team_Score.Util
{
    internal class EventFileParser
    {
        private static XNamespace? ns;

        public Event Parse(string filename, EventType eventType)
        {
            XDocument doc = XDocument.Load(File.OpenRead(filename));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
            try
            {
                ns = doc.Root.GetDefaultNamespace();

                // Build Event
                var evt = doc.Element(ns + "ResultList").Element(ns + "Event");
                if (evt.Element(ns + "Race") != null)
                {
                    evt = evt.Element(ns + "Race"); // If for some reason this file is from a multi-day event. Shouldn't happen, but...
                }
                CompetitionType compType = doc.Descendants(ns + "TeamResult").Count() > 0 ? CompetitionType.Relay : CompetitionType.Individual;  // If it is Relay the file contains "TeamResult" elements.
                Event @event = new Event()
                {
                    Id = evt.Element(ns + "Id") != null ? (int)evt.Element(ns + "Id") : new Random().Next(),
                    Date = DateOnly.Parse((string)evt.Element(ns + "StartTime").Element(ns + "Date")),
                    Name = (string)evt.Element(ns + "Name") ?? "An Event",
                    CompType = compType,
                    EventType = eventType
                };

                // Build Classes
                List<Clazz> classes = new List<Clazz>();
                foreach (XElement classResult in doc.Descendants(ns +"ClassResult"))
                {
                    XElement classElement = classResult.Element(ns + "Class");
                    Clazz clazz = new Clazz()
                    {
                        Id = classElement.Element(ns + "Id") != null ? (int)classElement.Element(ns + "Id") : new Random().Next(),
                        Name = (string)classElement.Element(ns + "Name") ?? "A Class"
                    };
                    classes.Add(clazz);

                    if (compType == CompetitionType.Individual)
                    {
                        List<PersonOrTeam> persons = new List<PersonOrTeam>();
                        foreach (XElement personResultElement in classResult.Elements(ns + "PersonResult"))
                        {
                            if (personResultElement.Element(ns + "Result").Element(ns + "Position") != null)
                            // We only want persons with a position, others don't count.
                            {
                                XElement personElement = personResultElement.Element(ns + "Person");
                                var country = GetCountry(personResultElement.Element(ns + "Organisation"));
                                PersonOrTeam person = new PersonOrTeam()
                                {
                                    Id = personElement.Element(ns + "Id") != null ? personElement.Element(ns + "Id").ToString() : new Random().Next().ToString(),
                                    Name = $"{(string)personElement.Element(ns + "Name").Element(ns + "Given")} {(string)personElement.Element(ns + "Name").Element(ns + "Family")} ",
                                    Place = (int)personResultElement.Element(ns + "Result").Element(ns + "Position"),
                                    Time = (int)personResultElement.Element(ns + "Result").Element(ns + "Time"),
                                    Score = 0,
                                    Country = country,
                                    Counting = false
                                };
                                persons.Add(person);
                            }
                        }
                        clazz.PersonsOrTeams = persons;
                    } else // EventType is Relay
                    {
                        List<PersonOrTeam> teams = new List<PersonOrTeam>();
                        foreach (XElement teamResultElement in classResult.Elements(ns + "TeamResult"))
                        {
                            var country = GetCountry(teamResultElement.Element(ns + "Organisation"));
                            // The final position of a team is in the last runner's TotalResult element
                            var overallResult = teamResultElement.Elements(ns + "TeamMemberResult").Elements(ns + "Result").Last<XElement>().Element(ns + "OverallResult");

                            if (overallResult.Element(ns + "Position") != null)
                            // We only want teams with a final position, others don't count.
                            {
                                PersonOrTeam team = new PersonOrTeam()
                                {
                                    Id = teamResultElement.Element(ns + "EntryId") != null ? teamResultElement.Element(ns + "EntryId").ToString() : new Random().Next().ToString(),
                                    Name = (string)teamResultElement.Element(ns + "Name") ?? "Unknown runner",
                                    Place = (int)overallResult.Element(ns + "Position"),
                                    Time = (int)overallResult.Element(ns + "Time"),
                                    Score = 0,
                                    Country = country,
                                    Counting = false
                                };
                                teams.Add(team);
                            }
                        }
                        clazz.PersonsOrTeams = teams;
                    }
                }
                @event.Clazzes = classes;
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                return @event;
            } catch (Exception)
            {
                throw new IofXmlParseException("Failed to parse IOF XML 3.0 format");
            }
        }

        private string GetCountry(XElement organisation) {
            // Country is _somewhere_ in the <Organisation> element. We look for a three letter code at different places:
            // - Organisation Name
            // - Country "code" attribute
            // - Country Name
            var candidate = organisation.Element(ns + "Name");
            if (candidate != null && candidate.Value.Length == 3) { 
                return candidate.Value.ToUpper();
            }
            candidate = organisation.Element(ns + "Country");
            if (candidate != null)
            {
                if (candidate.Attribute("code") != null && candidate.Attribute("code").Value.Length == 3)
                {
                    return candidate.Attribute("code").Value;
                }
                if (candidate.Value.Length == 3)
                {
                    return candidate.Value.ToUpper();
                }
            }
            return "UNK";  // We couldn't guess the country, so it is Unknown
        }
    }

    [Serializable]
    internal class IofXmlParseException : Exception
    {
        public IofXmlParseException(string? message) : base(message)
        {
        }
    }
}
