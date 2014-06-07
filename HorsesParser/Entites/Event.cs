using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorsesParser.Entites
{
    public class Event
    {
        public string ID { get; set; }
        public string Time { get; set; }
        public string Stadium { get; set; }
        public string HorseName { get; set; }
        public string ParticipantCount { get; set; }
        public string SP { get; set; }
        public string Place { get; set; }
        public string PlusMinus { get; set; }
        public string Summary { get; set; }
        public string PlaceCoef { get; set; }
        public string BSP { get; set; }
        public string DayLink { get; set; }
        public string RaceLink { get; set; }
        public bool IsBSPParsed { get; set; }
    }
}
