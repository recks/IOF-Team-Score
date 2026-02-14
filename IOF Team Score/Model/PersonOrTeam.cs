using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOF_Team_Score.Model
{
    public class PersonOrTeam {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required int Place { get; set; }
        public required int Time { get; set; }  // Time in seconds
        public required int Score { get; set; } = 0;
        public required string Country { get; set; }
        public required bool Counting { get; set; } = false;
    }
}
