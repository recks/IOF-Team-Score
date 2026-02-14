using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOF_Team_Score.Model
{
    public class Country
    {
        public string Name { get; set; } = "UNK";

        // A total score per (event, class) using their names as keys.
        public Dictionary<Tuple<string, string>, int> Scores { get; set; } = [];

        public int TotalScore() { 
            return Scores.Sum(score => score.Value);
        }

        public int TotalScoreForEvent(string evt)
        {
            return Scores.Where(score => score.Key.Item1.Equals(evt)).ToList().Sum(score => score.Value);
        }

    }
}
