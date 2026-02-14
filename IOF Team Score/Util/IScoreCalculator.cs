using IOF_Team_Score.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOF_Team_Score.Util
{
    internal interface IScoreCalculator
    {
        public void CalculateScores(List<Event> events);

        public List<Country> CalculateTotalScores(List<Event> events);
    }
}
