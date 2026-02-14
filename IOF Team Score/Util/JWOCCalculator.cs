using IOF_Team_Score.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace IOF_Team_Score.Util
{
    internal class JWOCCalculator : IScoreCalculator
    {
        /* 
         * IOF Rules 24.14 (2025 edition):
         * A team score is calculated for each Federation (to place the Federations in a team competition) by adding the placings of each Federation’s 
         * three best competitors in all three individual competitions for men and women, the official placings—multiplied by three—of its placed men’s 
         * and women’s relay teams and the official placing—multiplied by six—of its best-placed sprint relay team. 
         * If a Federation has fewer than three finishers in any individual competition, every missing competitor is treated as though they finished 
         * one place behind the last finisher. 
         * If a Federation has no place in the relay competitions, it is treated as if they finished one place behind the last official placed team. 
         * The lowest score wins.
         */

        private static readonly int RELAY_MULTIPLIER = 3;
        private static readonly int SPRINT_RELAY_MULTIPLIER = 6;

        private List<string> extractCountries(List<Event> events)
        {
            List<string> countries = new List<string>();
            foreach (Event evt in events)
            {
                foreach (Clazz cls in evt.Clazzes)
                {
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        if (!countries.Contains(pot.Country))
                        {
                            countries.Add(pot.Country);
                        }
                    }
                }
            }
            return countries;
        }

        void IScoreCalculator.CalculateScores(List<Event> events)
        {
            Dictionary<string, Dictionary<Event, Dictionary<Clazz, int>>> countingPerCountry = 
                new Dictionary<string, Dictionary<Event, Dictionary<Clazz, int>>>();  // contains the number of counting persons or teams per event from each country
            foreach (Event evt in events)
            {
                int factor = (evt.CompType == CompetitionType.Relay ? (evt.Name.ToLower().Contains("sprint") ? SPRINT_RELAY_MULTIPLIER : RELAY_MULTIPLIER) : 1);
                foreach (Clazz cls in evt.Clazzes)
                {
                    cls.PersonsOrTeams.Sort((a, b) => { return a.Time.CompareTo(b.Time); });
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        if (!countingPerCountry.Keys.Contains(pot.Country))
                        {
                            countingPerCountry.Add(pot.Country, new Dictionary<Event, Dictionary<Clazz, int>>());
                        }
                        if (!countingPerCountry[pot.Country].Keys.Contains(evt))
                        {
                            countingPerCountry[pot.Country].Add(evt, new Dictionary<Clazz, int>());
                        }
                        if (!countingPerCountry[pot.Country][evt].Keys.Contains(cls))
                        {
                            countingPerCountry[pot.Country][evt].Add(cls, 0);
                        }
                        if (countingPerCountry[pot.Country][evt][cls] < (evt.CompType == CompetitionType.Individual ? 3 : 1)) {
                            // The current person or team should count
                            pot.Score = pot.Place * factor;
                            pot.Counting = true;
                            countingPerCountry[pot.Country][evt][cls]++;
                        }
                    }
                }
            }
            // Check to see if there are countries with less than needed runners. In that case a score of last place + 1 is used.
            foreach (string country in countingPerCountry.Keys)
            {
                Dictionary<Event, Dictionary<Clazz, int>> countingPerEvent = countingPerCountry[country];
                foreach (Event evt in events)
                {
                    if (!countingPerEvent.ContainsKey(evt))
                    {
                        // The country didn't have any runners in the event
                        countingPerEvent.Add(evt, new Dictionary<Clazz, int>());
                    }
                    int factor = (evt.CompType == CompetitionType.Relay ? (evt.Name.ToLower().Contains("sprint") ? SPRINT_RELAY_MULTIPLIER : RELAY_MULTIPLIER) : 1);
                    foreach (Clazz cls in evt.Clazzes)
                    {
                        if (!countingPerEvent[evt].ContainsKey(cls))
                        {
                            // The country didn't have any runners in this classe
                            countingPerEvent[evt].Add(cls, 0);
                        }
                        // Last place (only look at the real runners, i.e. with a real running time)
                        var places = cls.PersonsOrTeams.Where(p => p.Time<int.MaxValue).Select(p => p.Place);
                        var last_place = places.Any() ? places.Max(p => p) : 0;
                        // Check to see if the number of counting runners is too low
                        var limit = (evt.CompType == CompetitionType.Individual ? 3 : 1);
                        // Add more, if there is too few
                        for (int ix = countingPerEvent[evt][cls]; ix<limit; ix++) {
                            PersonOrTeam personOrTeam = new PersonOrTeam()
                            {
                                Id = new Random().Next().ToString(),
                                Name = $"{country}, {cls.Name}",
                                Place = last_place + 1,
                                Time = int.MaxValue,
                                Score = (last_place + 1) * factor,
                                Country = country,
                                Counting = true
                            };
                            cls.PersonsOrTeams.Add(personOrTeam);
                        }
                    }
                }
            }

        }

        List<Country> IScoreCalculator.CalculateTotalScores(List<Event> events)
        {
            var countryScores = new List<Country>();  // Reset
            foreach (Event evt in events)
            {
                foreach (Clazz cls in evt.Clazzes)
                {
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        if (pot.Counting)
                        {
                            if (countryScores.Any(cs => cs.Name == pot.Country))
                            {
                                // Country exists
                                Country country = countryScores.First(cs => cs.Name == pot.Country);
                                Tuple<string, string> scoreTuple = new Tuple<string, string>(evt.Name, cls.Name);
                                if (country.Scores.ContainsKey(scoreTuple))
                                {
                                    country.Scores[scoreTuple] += pot.Score;
                                }
                                else
                                {
                                    country.Scores.Add(new Tuple<string, string>(evt.Name, cls.Name), pot.Score);
                                }
                            }
                            else
                            {
                                // Country doesn't exist
                                countryScores.Add(new Country()
                                {
                                    Name = pot.Country,
                                    Scores = new Dictionary<Tuple<string, string>, int>() { { new Tuple<string, string>(evt.Name, cls.Name), pot.Score } }
                                });
                            }
                        }
                    }
                }
            }

            return countryScores;
        }
    }
}
