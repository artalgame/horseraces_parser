using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorsesParser.Entites
{
    public class DayEvents
    {
        public List<Event> Events { get; set; }
        public string EventDate { get; set; }
        public int Category { get; set; }

        public DateTime RightDate
        {
            get
            {
                var components = EventDate.Split('-');
                return new DateTime(int.Parse(components[0]),int.Parse(components[1]),int.Parse(components[2]));
            }
        }
    }
}
