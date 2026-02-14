using IOF_Team_Score.Model;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace IOF_Team_Score.Util
{
    internal class EYOCCalculator : IScoreCalculator
    {
        // Point tables for calculation of team scores
        Scores[]? scores;

        public EYOCCalculator(IConfigurationRoot config) {
            ReadPointTables(config);
        }

        private void ReadPointTables(IConfigurationRoot config)
        {
            try
            {
                // Get Point tables for score calculation
                scores = config.GetRequiredSection("PointTables").Get<Scores[]>();
                if (scores == null || scores.Length == 0)
                {
                    throw new ConfigurationErrorsException("Point tables for score calculation couldn't be loaded from 'appsettings.json'.");
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new ConfigurationErrorsException("File 'appsettings.json' is not found.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new ConfigurationErrorsException("Point tables for score calculation couldn't be found in 'appsettings.json'.", ex);
            }
        }

        public void CalculateScores(List<Event> events)
        {
            if (scores == null)
            {
                return;  // Can't calculate anything without scores.
            }
            foreach (Event evt in events)
            {
                Scores? scoreTable = scores.Where(s => s.Type == evt.CompType).FirstOrDefault();
                if (scoreTable == null)
                {
                    continue;  // Not much to do, if we don't have any scores.
                }
                foreach (Clazz cls in evt.Clazzes)
                {
                    cls.PersonsOrTeams.Sort((a, b) => { return a.Time.CompareTo(b.Time); });
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        pot.Score = pot.Place > scoreTable.Points.Length ? scoreTable.Points[scoreTable.Points.Length - 1] : scoreTable.Points[pot.Place - 1];
                    }
                }
            }
        }

        public List<Country> CalculateTotalScores(List<Event> events)
        {
            var countryScores = new List<Country>();  // Reset
            foreach (Event evt in events)
            {
                int countingLimit = evt.CompType == CompetitionType.Relay ? 1 : 2;  // The first two counts in individual events, but only the first relay team counts.
                foreach (Clazz cls in evt.Clazzes)
                {
                    var countingPerCountry = new Dictionary<string, int>();  // Number of persons/teams are counting at this time
                    cls.PersonsOrTeams.Sort((a, b) => { return a.Time.CompareTo(b.Time); });
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        // Find potential score
                        var score = 0;
                        if (countingPerCountry.ContainsKey(pot.Country))
                        {
                            if (countingPerCountry.GetValueOrDefault(pot.Country) < countingLimit)
                            {
                                score = pot.Score;
                                countingPerCountry[pot.Country]++;
                                pot.Counting = true;  // This person/team counts towards the country's total
                            }
                        }
                        else
                        {
                            score = pot.Score;
                            countingPerCountry[pot.Country] = 1;
                            pot.Counting = true;  // This person/team counts towards the country's total
                        }

                        // Add to country's total
                        if (countryScores.Any(cs => cs.Name == pot.Country))
                        {
                            // Country exists
                            Country country = countryScores.First(cs => cs.Name == pot.Country);
                            Tuple<string, string> scoreTuple = new Tuple<string, string>(evt.Name, cls.Name);
                            if (country.Scores.ContainsKey(scoreTuple))
                            {
                                country.Scores[scoreTuple] += score;
                            }
                            else
                            {
                                country.Scores.Add(new Tuple<string, string>(evt.Name, cls.Name), score);
                            }
                        }
                        else
                        {
                            // Country doesn't exist
                            countryScores.Add(new Country()
                            {
                                Name = pot.Country,
                                Scores = new Dictionary<Tuple<string, string>, int>() { { new Tuple<string, string>(evt.Name, cls.Name), score } }
                            });
                        }
                    }
                }
            }

            return countryScores;
        }


    }
}
