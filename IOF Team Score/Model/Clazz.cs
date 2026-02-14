using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOF_Team_Score.Model
{
    public class Clazz {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public List<PersonOrTeam> PersonsOrTeams { get; set; } = new List<PersonOrTeam>();
    }
}
