using HorsesParser.Entites;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HorsesParser
{
    public class Parser
    {
        public const string BETFAIR_COOKIE = @"betexPtk=betexCurrency%3DGBP%7EbetexTimeZone%3DEurope%2FLondon%7EbetexRegion%3DGBR%7EbetexLocale%3Den; betexPtkSess=betexCurrencySessionCookie%3DGBP%7EbetexRegionSessionCookie%3DGBR%7EbetexTimeZoneSessionCookie%3DEurope%2FLondon%7EbetexLocaleSessionCookie%3Den%7EbetexSkin%3Dstandard%7EbetexBrand%3Dbetfair;";
        public static bool FullParse(string link, List<DayEvents> dayEventsList, int pageNumber)
        {
            int attempt = 1;
            while (attempt <= 3)
            {
                try
                {
                attempt++;
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                client.Encoding = Encoding.UTF8;
                
                    string s = HttpUtility.HtmlDecode(client.DownloadString(link));

                    dynamic result = JsonValue.Parse(s);
                    dynamic rows = result["rows"];
                    int totalPages = Int32.Parse(result["total"].ToString());
                    if (totalPages < pageNumber) return false;
                    foreach (var row in rows)
                    {
                        string rowID = row["id"];
                        JsonArray cell = row["cell"];
                        string cellID = (string)cell[0];
                        string cellDateTime = (string)cell[1];
                        string stadium = (string)cell[2];
                        string horseName = (string)cell[3];
                        string participantCount = (string)cell[4];
                        string SP = (string)cell[5];
                        string internalLink = (string)cell[6];
                        string place = (string)cell[7];
                        string plusMinus = (string)cell[8];
                        string summary = (string)cell[9];

                        string time = null;
                        string eventDate = GetEventDateAndTime(cellDateTime, out time);
                        DayEvents events = dayEventsList.FirstOrDefault(x => x.EventDate == eventDate);
                        if (events == null)
                        {
                            events = new DayEvents() { EventDate = eventDate, Events = new List<Event>() };
                            dayEventsList.Add(events);
                        }

                        Event thisEvent = events.Events.FirstOrDefault(x => x.Time == time);
                        events.Events.Remove(thisEvent);
                        Event newEvent = new Event()
                        {
                            HorseName = horseName,
                            ID = cellID,
                            ParticipantCount = participantCount,
                            Place = place,
                            PlusMinus = plusMinus,
                            SP = SP,
                            Stadium = stadium,
                            Summary = summary,
                            Time = time
                        };
                        Parse_BSP_PLACE_COEF(events.EventDate, newEvent);
                        events.Events.Add(newEvent);
                        
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            return false;
        }

        public static bool ParseNewData(string link, List<DayEvents> dayEventsList, int pageNumber)
        {
            int attempt = 1;
            while (attempt <= 3)
            {
                try
                {
                    attempt++;
                    DayEvents lastDayEvents = null;
                    int pos = 0;

                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    client.Encoding = Encoding.UTF8;

                    string s = HttpUtility.HtmlDecode(client.DownloadString(link));

                    dynamic result = JsonValue.Parse(s);
                    dynamic rows = result["rows"];
                    int totalPages = Int32.Parse(result["total"].ToString());
                    if (totalPages < pageNumber) return false;
                    foreach (var row in rows)
                    {
                        string rowID = row["id"];
                        JsonArray cell = row["cell"];
                        string cellID = (string)cell[0];
                        string cellDateTime = (string)cell[1];
                        string stadium = (string)cell[2];
                        string horseName = (string)cell[3];
                        string participantCount = (string)cell[4];
                        string SP = (string)cell[5];
                        string internalLink = (string)cell[6];
                        string place = (string)cell[7];
                        string plusMinus = (string)cell[8];
                        string summary = (string)cell[9];

                        string time = null;
                        string eventDate = GetEventDateAndTime(cellDateTime, out time);
                        DayEvents events = dayEventsList.FirstOrDefault(x => x.EventDate == eventDate);
                        if (events == null)
                        {
                            events = new DayEvents() { EventDate = eventDate, Events = new List<Event>() };
                            dayEventsList.Add(events);
                        }

                        if ((lastDayEvents == null) || (lastDayEvents.EventDate != events.EventDate))
                        {
                            lastDayEvents = events;
                            pos = 0;
                        }

                        Event thisEvent = events.Events.FirstOrDefault(x => x.Time == time);
                        if (thisEvent != null)
                        {
                            return false;
                        }
                        else
                        {
                            if (pos == 0)
                            {
                                events.Events = new List<Event>();
                                pos = 1;
                            }
                        }
                        Event newEvent = new Event()
                        {
                            HorseName = horseName,
                            ID = cellID,
                            ParticipantCount = participantCount,
                            Place = place,
                            PlusMinus = plusMinus,
                            SP = SP,
                            Stadium = stadium,
                            Summary = summary,
                            Time = time
                        };
                        Parse_BSP_PLACE_COEF(events.EventDate, newEvent);
                        events.Events.Add(newEvent);
                    }
                    return true;
                }

                catch (Exception ex)
                {
                    continue;
                }
            }
            return false;
        }

        private static string GetEventDateAndTime(string cellDateTime, out string time)
        {
            var dateAndTime = cellDateTime.Split(' ');
            time = dateAndTime[1];
            return dateAndTime[0];
        }

        private  static string lastLink = null;
        private static HtmlDocument lastDocument = null;
        private static int attempts = 0;
        public static void Parse_BSP_PLACE_COEF(string date, Event justEvent)
        {
            using (StreamWriter writer = new StreamWriter("log.txt",false))
            {
                try
                {
                    justEvent.BSP = "0";
                    justEvent.PlaceCoef = "0";
                    HtmlDocument document = new HtmlDocument();
                    string link = String.Format(MainWindow.BSP_PLACE_LINK_TEMPLATE, GetStringDate(date));
                    if (link == lastLink)
                    {
                        document = lastDocument;
                    }
                    else
                    {
                        WebClient client = new WebClient();
                        writer.WriteLine("\n\n\ngo to link:" + link);
                        client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                        client.Headers.Add("cookie", BETFAIR_COOKIE);
                        string s = HttpUtility.HtmlDecode(client.DownloadString(link));
                        //writer.WriteLine("\n\n\nreturned string:" + s);


                        document.LoadHtml(s);
                        lastDocument = document;
                        lastLink = link;
                    }

                    justEvent.DayLink = link;
                    
                    var countries = document.DocumentNode.ChildNodes[9].ChildNodes[5].ChildNodes[1].ChildNodes[9].ChildNodes[1].ChildNodes[5].ChildNodes[1].ChildNodes[1];
                    var GB_IRE = countries.ChildNodes.FirstOrDefault(x => x.Attributes["data-location"] != null && x.Attributes["data-location"].Value == MainWindow.DATA_LOCATION_VALUE);
                    var races = GB_IRE.ChildNodes[3].ChildNodes[1].ChildNodes.Where(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "course").ToList();
                    foreach (var stadiumRaces in races)
                    {
                       // writer.WriteLine("\n\n\ncurrent race for parsing" + stadiumRaces.InnerHtml);
                        var stadium = stadiumRaces.ChildNodes[1].ChildNodes[1].InnerText.Trim();
                        if (justEvent.Stadium.ToUpper().Contains(stadium.ToUpper()) || stadium.ToUpper().Contains(justEvent.Stadium.ToUpper()))
                        {
                            writer.WriteLine("\n\n\nStart to parsing race");
                            string eventTime = GetEventTimeWithoutSeconds(justEvent.Time);
                            writer.WriteLine("PARSED: eventTime:" + eventTime);

                            var stadiumRacesTime = stadiumRaces.ChildNodes[3].ChildNodes.Where(x => x.Name == "li");
                            writer.WriteLine("PARSED: stadiumRacesTime:" + stadiumRacesTime.Count());

                            foreach (var time in stadiumRacesTime)
                            {
                                writer.WriteLine("PARSED: time:" + time.InnerText.Trim());
                            }

                            HtmlNode currentRace = null;
       //                    currentRace = stadiumRacesTime.FirstOrDefault(x => x.InnerText.Trim() == eventTime);

                            if (currentRace == null)
                            {
                                foreach (var race in stadiumRacesTime)
                                {
                                    var curTime = race.InnerText.Trim();
                                    var hour = curTime.Split(':')[0];
                                    var newHour = (int.Parse(hour) + MainWindow.TIME_DIFF);
                                    if (newHour <= 0) newHour += 24;
                                    curTime = newHour.ToString() + curTime.Substring(2);
                                    if (eventTime == curTime)
                                    {
                                        currentRace = race;
                                        break;
                                    }
                                }
                            }

                            writer.WriteLine("PARSED: currentRace:" + currentRace.InnerHtml);

                            var raceLink = currentRace.ChildNodes[1].Attributes["href"].Value;
                            writer.WriteLine("PARSED: raceLink:" + raceLink);

                            var fullRaceLink = MainWindow.MAIN_RACE_LINK + raceLink;
                            GetBSPAndPlaceInfo(fullRaceLink, justEvent, writer);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {

                    writer.WriteLine("\n\n\nERROR:" + ex.Message + " Stack trace:"+ex.StackTrace);
                    writer.WriteLine("Cannot parse day data - let's start next attempt");

                    lastLink = null;
                    lastDocument = null;
                    writer.Close();
                    attempts++;
                    if(attempts<10)
                    Parse_BSP_PLACE_COEF(date, justEvent);

                    attempts = 0;

                }
                //var stadium = races[0].ChildNodes[1].ChildNodes[1].InnerText;
                //var racesTime = races[0].ChildNodes[3].ChildNodes.Where(x=>x.Name=="li").ToList();
                justEvent.IsBSPParsed = true;
            }
        }

        private static void GetBSPAndPlaceInfo(string link, Event justEvent, StreamWriter stream)
        {
            try
            {

                justEvent.RaceLink = link;
                WebClient client = new WebClient();

                //stream.WriteLine("\n\n\nparse by link:" + link);

                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                string s = HttpUtility.HtmlDecode(client.DownloadString(link));
                client.Headers.Add("cookie", BETFAIR_COOKIE);
                //stream.WriteLine("\n\n\nreturned string:" + s);

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(s);
                var mainDiv = document.GetElementbyId("market-main");
                var div = mainDiv.ChildNodes[5].ChildNodes[1].ChildNodes.FirstOrDefault(x => x.Attributes["class"] != null && x.Attributes["class"].Value == "results-container open");
                var table = div.ChildNodes[1].ChildNodes[1].ChildNodes.FirstOrDefault(x => x.Name == "tbody");
                var currentRow = table.ChildNodes.FirstOrDefault(
                    x => x.Name == "tr" &&
                    x.ChildNodes.FirstOrDefault(
                        z => z.Name == "td" &&
                        z.Attributes["class"] != null &&
                        z.Attributes["class"].Value == "horse" &&
                        z.ChildNodes.FirstOrDefault(
                            t => t.Name == "a" &&
                            (t.InnerText.Trim().ToUpper().Contains(justEvent.HorseName.Trim().ToUpper()) || justEvent.HorseName.Trim().ToUpper().Contains(t.InnerText.Trim().ToUpper()))
                        ) != null
                    ) != null);

                justEvent.IsBSPParsed = true;

                if (currentRow == null)
                {
                    stream.WriteLine("Can not parse horse string");
                    justEvent.BSP = "0";
                    justEvent.PlaceCoef = "0";
                    return;
                }
                var BSP = currentRow.ChildNodes.FirstOrDefault(x => x.Name == "td" && x.Attributes["class"] != null && x.Attributes["class"].Value == "bsp-perc").ChildNodes
                    .FirstOrDefault(z => z.Attributes["class"] != null && z.Attributes["class"].Value == "bsp").InnerText.Trim();
                var Place = currentRow.ChildNodes.FirstOrDefault(x => x.Name == "td" && x.Attributes["class"] != null && x.Attributes["class"].Value == "place").InnerText.Trim();

                justEvent.PlaceCoef = Place;
                justEvent.BSP = BSP;
            }
            catch (Exception ex)
            {
                stream.WriteLine("Error. Start next attempt to parse race data");
                GetBSPAndPlaceInfo(link, justEvent, stream);
            }
        }

        private static string GetEventTimeWithoutSeconds(string time)
        {
            return time.Remove(time.Length -3);
        }

        private static string GetStringDate(string date)
        {
            return date.Replace("-",String.Empty);
        }

        internal static bool ParseDataForMonth(string link, List<DayEvents> dayEventsList, int pageNumber, int year, int month)
        {
            int attempt = 1;
            while (attempt <= 3)
            {
                try
                {
                    attempt++;
                    DayEvents lastDayEvents = null;
                    int pos = 0;

                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    client.Encoding = Encoding.UTF8;

                    string s = HttpUtility.HtmlDecode(client.DownloadString(link));

                    dynamic result = JsonValue.Parse(s);
                    dynamic rows = result["rows"];
                    int totalPages = Int32.Parse(result["total"].ToString());
                    if (totalPages < pageNumber) return false;
                    foreach (var row in rows)
                    {
                        string rowID = row["id"];
                        JsonArray cell = row["cell"];
                        string cellID = (string)cell[0];
                        string cellDateTime = (string)cell[1];
                        string stadium = (string)cell[2];
                        string horseName = (string)cell[3];
                        string participantCount = (string)cell[4];
                        string SP = (string)cell[5];
                        string internalLink = (string)cell[6];
                        string place = (string)cell[7];
                        string plusMinus = (string)cell[8];
                        string summary = (string)cell[9];

                        string time = null;
                        string eventDate = GetEventDateAndTime(cellDateTime, out time);
                        int currentYear = GetCurrentYear(cellDateTime);
                        int currentMonth = GetCurrentMonth(cellDateTime);
                        if (currentYear > year)
                        {
                            return true;
                        }
                        if(currentYear == year)
                        {
                            if (currentMonth > month)
                                return true;
                            if (currentMonth < month)
                                return false;
                        }
                        if (currentYear < year)
                            return false;
                        
                        DayEvents events = dayEventsList.FirstOrDefault(x => x.EventDate == eventDate);
                        if (events == null)
                        {
                            events = new DayEvents() { EventDate = eventDate, Events = new List<Event>() };
                            dayEventsList.Add(events);
                        }

                        if ((lastDayEvents == null) || (lastDayEvents.EventDate != events.EventDate))
                        {
                            lastDayEvents = events;
                            pos = 0;
                        }
  
                            if (pos == 0)
                            {
                                events.Events = new List<Event>();
                                pos = 1;
                            }

                        Event newEvent = new Event()
                        {
                            HorseName = horseName,
                            ID = cellID,
                            ParticipantCount = participantCount,
                            Place = place,
                            PlusMinus = plusMinus,
                            SP = SP,
                            Stadium = stadium,
                            Summary = summary,
                            Time = time
                        };

                        Parse_BSP_PLACE_COEF(events.EventDate, newEvent);
                        events.Events.Add(newEvent);
                    }
                    return true;
                }

                catch (Exception ex)
                {
                    continue;
                }
            }
            return false;
        }

        private static int GetCurrentMonth(string cellDateTime)
        {
            string time = String.Empty;
            string date = GetEventDateAndTime(cellDateTime, out time);

            String[] elements= date.Split('-');
            return Int32.Parse(elements[1]);
        }

        private static int GetCurrentYear(string cellDateTime)
        {
            string time = String.Empty;
            string date = GetEventDateAndTime(cellDateTime, out time);

            String[] elements = date.Split('-');
            return Int32.Parse(elements[0]);
        }
    }
}
