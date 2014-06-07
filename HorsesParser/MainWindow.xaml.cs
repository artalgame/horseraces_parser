using HorsesParser.Entites;
using JsonExSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HorsesParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string TOP_ID = "1";
        public const string OUT_ID = "2";

        public const float smallComission = 0.065f;
        public const float bigComission = 0.935f;
        //when current bank is smaller than start by this value
        public const float STOP_PERCENT = 0.8f;
        

        public const int MAX_WIN=2;

        public const string FILE_NAME_TOP = "top.txt";
        public const string FILE_NAME_OUT = "out.txt";

        public const string  LINK_TEMPLATE = @"http://rumao.ru/go/get.php/?q={0}&_search=false&nd=1391901873976&rows=500&page={1}&sidx=datetime&sord=desc";

        public const string BSP_PLACE_LINK_TEMPLATE = @"http://form.timeform.betfair.com/daypage?date={0}";
        public const string MAIN_RACE_LINK = @"http://form.timeform.betfair.com";
        public const string DATA_FORMAT_FOR_LINK = "yearmonthday";//20090112

        public const string DATA_LOCATION_VALUE = "RACING_COUNTRY_GB_IE";

        public List<DayEvents> topDayEvents;
        public List<DayEvents> outDayEvents;
        public List<DayEvents> currentDayEvents;

        public readonly string[] types = { "TOP", "OUT" };

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-Us");
            InitializeComponent();
            LoadDayLists();
            TypeListBox.ItemsSource = types;
        }

        private void LoadDayLists()
        {
            topDayEvents = LoadDayList(FILE_NAME_TOP);
            outDayEvents = LoadDayList(FILE_NAME_OUT);
            CalculateCategories(topDayEvents);
            CalculateCategories(outDayEvents);
        }

        private void CalculateCategories(List<DayEvents> daysEvents)
        {
            foreach (var dayEvents in daysEvents)
            {
                dayEvents.Events.Reverse();
                if (dayEvents.Events.Count == 0) continue;

                if (dayEvents.Events[0].Place != "1")
                {
                    dayEvents.Category = 0;
                    dayEvents.Events.Reverse();
                    continue;
                }

                int i=0;
                int lusCount = 0;
                int winCount = 0;
                while(i<dayEvents.Events.Count &&  dayEvents.Events[i].Place == "1")
                {
                    i++;
                    lusCount++;
                }
                while (i<dayEvents.Events.Count &&  dayEvents.Events[i].Place != "1")
                {
                    winCount++;
                    i++;
                }
                dayEvents.Events.Reverse();
                if (lusCount == 1 && winCount >= 3)
                {
                    dayEvents.Category = 0;
                    continue;
                }
                else
                {
                    if (lusCount == 1)
                    {
                        dayEvents.Category = 1;
                        continue;
                    }
                }

                if (lusCount == 2 && winCount >= 5)
                {
                    dayEvents.Category = 0;
                    continue;
                }
                else
                {
                    if (lusCount == 2)
                    {
                        dayEvents.Category = 2;
                        continue;
                    }
                }

                if (lusCount == 3 && winCount >= 6)
                {
                    dayEvents.Category = 0;
                    continue;
                }
                else
                {
                    if (lusCount == 3)
                    {
                        dayEvents.Category = 3;
                        continue;
                    }
                }
                dayEvents.Category = lusCount;
            }
        }

        private List<DayEvents> LoadDayList(string fileName)
        {
            List<DayEvents> events=new List<DayEvents>();   
            using (StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read)))
            {
                string seasonString = reader.ReadToEnd();
                Serializer jsonSerializer = new Serializer(typeof(List<DayEvents>));
                jsonSerializer.Deserialize(seasonString, events);
            }
            return events;
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            SetTimeDif();
            ParseData(topDayEvents, TOP_ID, FILE_NAME_TOP);
            ParseData(outDayEvents, OUT_ID, FILE_NAME_OUT);
        }

        private void ParseData(List<DayEvents> dayEventsList, string linkId, string fileName)
        {
            int pageNumber = 1;
            while(Parser.FullParse(String.Format(LINK_TEMPLATE, linkId, pageNumber), dayEventsList,pageNumber))
            {
                pageNumber++;
            }
            SaveEvents(fileName, dayEventsList);
        }

        private void SaveEvents(string fileName, List<DayEvents> events)
        {

            Serializer jsonSerializer = new Serializer(typeof(List<DayEvents>));
            string s = jsonSerializer.Serialize(events);
            using (StreamWriter writer = new StreamWriter(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                writer.WriteLine(s);
            }
        }

        private void DatePicker_SelectedDateChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var datePicker = sender as DatePicker;

            DateTime? date = datePicker.SelectedDate;
            if (date == null)
                return;
            string dateString = GetDateIdentifire(date);

            int selectedType = TypeListBox.SelectedIndex;
            var dayEvents = GetDayEvents(dateString, selectedType);

            if (dayEvents == null)
            {
                ResultsLabel.Content  = "Нет результата для этой даты";
                ResultsTable.Items.Clear();
                return;
            }
            UpdateDayEvents(dayEvents);

        }

        private void UpdateDayEvents(DayEvents dayEvents)
        {
            int lusCount = 0;
            int winCount = 0;
            int lastLus = 0;
            string stringResults="";
            List<TableData> spList = new List<TableData>(); 
            for (int i = dayEvents.Events.Count - 1; i >= 0; i--)
            {
                Event justEvent = dayEvents.Events[i];
                if (justEvent.Place == "1")
                {
                    lusCount++;
                    spList.Add(new TableData { Lus = "-", SP = justEvent.SP,BSP=justEvent.BSP, Place = justEvent.PlaceCoef, ParticipantCount=justEvent.ParticipantCount, PlaceInRace=justEvent.Place });
                    if (winCount != 0)
                    {
                        stringResults = stringResults + " (" + winCount + ") ";
                        winCount = 0;
                    }
                }
                else
                {
                    if(lusCount!=0)
                    {
                        stringResults = stringResults + lusCount;
                        lastLus = lusCount;
                        lusCount=0;
                    }
                    winCount++;
                    spList.Add(new TableData { Lus = "+", SP = justEvent.SP, BSP = justEvent.BSP, Place = justEvent.PlaceCoef, ParticipantCount = justEvent.ParticipantCount, PlaceInRace = justEvent.Place });
                }
            }
            if (lusCount != 0)
            {
                stringResults = stringResults + lusCount;
            }
            else
                if (winCount != 0)
                {
                    stringResults = stringResults + " (" + winCount + ") ";
                }
            ResultsLabel.Content = stringResults;
 
            ResultsTable.ItemsSource = spList;
        }

        private DayEvents GetDayEvents(string dateString, int selectedType)
        {
            List<DayEvents> list = null;
            if (selectedType == 0)
            {
                list = topDayEvents;
            }
            else
            {
                list = outDayEvents;
            }

            return list.FirstOrDefault(x => x.EventDate == dateString);
        }

        private string GetDateIdentifire(DateTime? date)
        {
            string result = date.Value.Year.ToString() +"-";
            if (date.Value.Month < 10)
            {
                result += "0";
            }
            result += date.Value.Month + "-";
            if (date.Value.Day < 10)
            {
                result += "0";
            }
            result += date.Value.Day;
            return result;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetTimeDif();
            ParseNewData(topDayEvents, TOP_ID, FILE_NAME_TOP);
            ParseNewData(outDayEvents, OUT_ID, FILE_NAME_OUT);
        }

        private void ParseNewData(List<DayEvents> dayEventsList, string linkId, string fileName)
        {
            int pageNumber = 1;
            while (Parser.ParseNewData(String.Format(LINK_TEMPLATE, linkId, pageNumber), dayEventsList, pageNumber))
            {
                pageNumber++;
            }
            SaveEvents(fileName, dayEventsList);
        }

        private void TypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = TypeListBox.SelectedIndex;
            if (selectedIndex == -1)
                return;

            if (selectedIndex == 0)
            {
                currentDayEvents = topDayEvents;
            }
            else
            {
                currentDayEvents = outDayEvents;
            }

            UpdateLusListView();
            UpdateYearMonthLists();
        }

        private void UpdateYearMonthLists()
        {
            YearListBox.ItemsSource = currentDayEvents.GroupBy(x=>x.RightDate.Year).Select(x => x.Key).OrderBy(x => x);
            MonthListBox.ItemsSource = currentDayEvents.GroupBy(x => x.RightDate.Month).Select(x=>x.Key).OrderBy(x => x);
            
        }

        private void UpdateLusListView()
        {
            var categoryDictionary = new Dictionary<int, int>();
            foreach (var dayEvents in currentDayEvents.Where(x=>x.Category!=0))
            {
                if (categoryDictionary.ContainsKey(dayEvents.Category))
                {
                    categoryDictionary[dayEvents.Category]++;
                }
                else
                {
                    categoryDictionary.Add(dayEvents.Category, 1);
                }
            }

            categoryDictionary.OrderBy(x => x.Key);

            var categoryItems = categoryDictionary.Select(x => x.Key.ToString() + " (" + x.Value.ToString()+")").OrderBy(x=>x);
            
            LusListBox.ItemsSource = categoryItems;
        }

        private void LusListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LusListBox.SelectedValue != null)
            {
                var value = int.Parse(((string)LusListBox.SelectedValue).Split(' ')[0]);
                var dayEvents = currentDayEvents.Where(x => x.Category == value);

                var dateItems = dayEvents.Select(x => x.RightDate).OrderBy(x => x);
                var dateReversItems = dateItems.Reverse();

                DateListBox.ItemsSource = dateReversItems;
            }
        }

        private void DateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateListBox.SelectedValue != null)
            {
                var value = (DateTime)DateListBox.SelectedValue;
                var dayEvents = currentDayEvents.FirstOrDefault(x => DateTime.Compare(x.RightDate, value) == 0);

                int countUnparsed = dayEvents.Events.Count(x => String.IsNullOrEmpty(x.BSP));
                if (countUnparsed != 0)
                {
                    foreach (var justEvent in dayEvents.Events)
                        if (!justEvent.IsBSPParsed)
                            if (String.IsNullOrEmpty(justEvent.BSP) || justEvent.BSP == "0")
                                Parser.Parse_BSP_PLACE_COEF(dayEvents.EventDate, justEvent);
                            else justEvent.IsBSPParsed = true;
                }
                UpdateDayEvents(dayEvents);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                
                var days = currentDayEvents.Where(x => x.RightDate.Year == year && x.RightDate.Month == month);
                int winCount = days.Count(x => x.Events[x.Events.Count - 1].Place != "1");
                WinCountLabel.Content = winCount;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            foreach (var eventsDays in topDayEvents)
            {
                foreach (var justEvent in eventsDays.Events)
                    if (!justEvent.IsBSPParsed)
                        if (String.IsNullOrEmpty(justEvent.BSP) || justEvent.BSP == "0")
                            Parser.Parse_BSP_PLACE_COEF(eventsDays.EventDate, justEvent);
                        else justEvent.IsBSPParsed = true;
            }
            SaveEvents(FILE_NAME_TOP, topDayEvents);

            foreach (var eventsDays in outDayEvents)
            {
                foreach (var justEvent in eventsDays.Events)
                    if (!justEvent.IsBSPParsed)
                 if (String.IsNullOrEmpty(justEvent.BSP) || justEvent.BSP == "0")
                    Parser.Parse_BSP_PLACE_COEF(eventsDays.EventDate, justEvent);
                 else justEvent.IsBSPParsed = true;
            }
            SaveEvents(FILE_NAME_OUT, outDayEvents);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveEvents(FILE_NAME_TOP, topDayEvents);
            SaveEvents(FILE_NAME_OUT, outDayEvents);
        }


        private void AnalizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                try
                {
                    float monthBank = float.Parse(BankTextBox.Text);
                    float oddPersent = float.Parse(OddTextBox.Text);
                    float maxBSP = float.Parse(MaxBSP.Text);
                    oddPersent /= 100;
                    int oddsCount = int.Parse(OddsCountTextBox.Text);


                    var events = currentDayEvents.Where(x => x.RightDate.Year == year).OrderBy(x => x.RightDate.Month);
                    
                    List<DayAndPercentData> dayStatistic = new List<DayAndPercentData>();
                    List<MonthAndPercentData> monthStatistic = new List<MonthAndPercentData>();
                    
                    for (int i = 1; i <= 12; i++)
                    {
                        summToWin = 0;
                        var monthDayEvents = events.Where(x => x.RightDate.Month == i).OrderBy(x => x.RightDate.Day).ToList();
                        if (monthDayEvents.Count != 0)
                        {
                            var currentMonthBank = monthBank;
                            odd = currentMonthBank * oddPersent;
                            foreach (var dayEvents in monthDayEvents)
                            {
                                if (currentMonthBank >= 0)
                                {
                                    var newMonthBankUsingCurrent = GetFinalSummByDay(dayEvents, oddsCount, currentMonthBank, oddPersent, 0,maxBSP);
                                    currentMonthBank = newMonthBankUsingCurrent;
                                    if (Math.Abs(Math.Round(summToWin, 1)) > 0)
                                    {
                                        if (summToWin > currentMonthBank)
                                        {

                                            break;
                                        }
                                        odd = summToWin / 2;
                                        odd /= bigComission;
                                        lusCount = 1;
                                    }
                                    else
                                    {
                                        odd = currentMonthBank * oddPersent;
                                    }

                                }

                               
                            }


                            var percentic = (currentMonthBank - monthBank) / monthBank * 100;

                            monthStatistic.Add(new MonthAndPercentData { MonthYear = monthDayEvents[0].RightDate.Month.ToString() + "-" + monthDayEvents[0].RightDate.Year.ToString(), Percent = (float)Math.Round(percentic, 2) });

                            foreach (var dayEvents in monthDayEvents)
                            {
                                var temp = odd;
                                var temp1 = summToWin;
                                var temp2 = lusCount;
                                lusCount = 0;
                                summToWin = 0;
                                odd = monthBank * oddPersent;
                                var newMonthBankAfterDay = GetFinalSummByDay(dayEvents, oddsCount, monthBank, oddPersent, 0, maxBSP);
                                var percent = (newMonthBankAfterDay - monthBank) / monthBank * 100;
                                if (stopRace == 0)
                                {
                                    stopRace = dayEvents.Events.Count;
                                }
                                odd = temp;
                                summToWin = temp1;
                                lusCount = temp2;
                                dayStatistic.Add(new DayAndPercentData { Date = dayEvents.RightDate.ToShortDateString(), Percent = (float)Math.Round(percent, 2), Last_Race = stopRace });
                            }

                           
                        }
                        
                    }
                    DayResultsDataGrid.ItemsSource = dayStatistic.OrderBy(x => x.Percent);
                    MonthResultsDataGrid.ItemsSource = monthStatistic.OrderBy(x => x.Percent);
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
        }

        private int lusCount = 0;
        private int winCount = 0;
        private float odd = 0f;
        private float summToWin = 0;
        private int stopRace = 0;
        private float GetFinalSummByDay(DayEvents dayEvents, int oddsCount, float startBank, float oddPersent, int startRaceNumber, float maxBSP)
        {
            winCount = 0;
            stopRace = 0;
            var bank = startBank;
            while (oddsCount != 0 && startRaceNumber<dayEvents.Events.Count)
            {
                Event race = dayEvents.Events.Reverse<Event>().ToList()[startRaceNumber];

                if (!race.IsBSPParsed)
                {
                    if (String.IsNullOrEmpty(race.BSP) || race.BSP == "0")
                    {
                        Parser.Parse_BSP_PLACE_COEF(dayEvents.EventDate, race);
                    }
                    else
                        race.IsBSPParsed = true;
                }
                if (startBank < odd)
                {
                    stopRace = startRaceNumber;
                    return startBank;
                }
                if (GetBSP(race) <= maxBSP)
                {
                    //если вин
                    if (race.PlusMinus == "1")
                    {
                        //если не было лузов и вин
                        if (lusCount == 0)
                        {
                            oddsCount--;
                            startBank += (float)(bigComission * odd);
                            //startBank += (float)(1.95 * (startBank * oddPersent));
                            odd = startBank * oddPersent;
                        }
                        //если есть незакрытые лузы
                        else
                        {

                            winCount++;
                            startBank += (float)(bigComission * (odd));
                            summToWin -= (float)(bigComission * (odd));

                            if (summToWin < 0) summToWin = 0;
                            //eсли этим вином закрываем последовательность лузов
                            if (winCount - lusCount == 1)
                            {
                                lusCount = 0;
                                winCount = 0;
                                odd = startBank * oddPersent;
                            }
                            //если еще не закрываем
                            else
                            {

                            }
                        }
                    }
                    //если луз
                    else
                    {
                        //если нет незакрытой последовательности лузов
                        if (lusCount == 0)
                        {
                            lusCount++;
                            float minusMoney = (GetBSP(race) - 1) * (odd);

                            summToWin = minusMoney;
                            startBank -= summToWin;
                            odd = minusMoney / (lusCount + 1);
                            odd /= bigComission;
                        }
                        //если уже есть последовательность незакрытых лузов
                        else
                        {
                            //если винами еще не начали закрывать
                            if (winCount == 0)
                            {
                                lusCount++;
                                //zabiraem na stavky iz banka
                                float minusMoney = (GetBSP(race) - 1) * (odd);

                                startBank -= minusMoney;
                                summToWin += minusMoney;
                                odd = summToWin / (lusCount + 1);
                                odd /= bigComission;
                            }
                            //если уже были вины, но они не закрыли до конца луз
                            else
                            {
                                lusCount = 1;
                                winCount = 0;
                                float minusMoney = (GetBSP(race) - 1) * odd;

                                startBank -= minusMoney;
                                summToWin += minusMoney;
                                odd = summToWin / (lusCount + 1);
                                odd /= bigComission;
                            }
                        }
                    }
                }
               /* if (startBank / bank <= 1 - STOP_PERCENT)
                {
                    stopRace = startRaceNumber + 1;
                    return startBank;
                }*/
                
                startBank = (float)Math.Round(startBank, 1);
                odd = (float)Math.Round(odd, 1);
                summToWin = (float)Math.Round(summToWin, 1);
                startRaceNumber++;
            }
            lusCount = 0;
            odd = 0;
            stopRace = startRaceNumber;
            return (float)Math.Round(startBank, 1);
        }

        private float GetBSP(Event race)
        {
            try
            {
                if (race.BSP != "0")
                    return float.Parse(race.BSP);
                else
                    throw new Exception();
            }
            catch (Exception ex)
            {
                try
                {
                    return float.Parse(race.SP) + 0.15f * float.Parse(race.SP);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        private float GetPlace(Event race)
        {
            try
            {      
              return float.Parse(race.PlaceCoef);
               
            }
            catch (Exception ex)
            {
              
                    return 0;
            }
        }

        private void MonthListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                DayListBox.ItemsSource = currentDayEvents.Where(x=>x.RightDate.Year == year && x.RightDate.Month == month).GroupBy(x => x.RightDate.Day).Select(x => x.Key).OrderBy(x => x);
            }
        }

        private void DayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null && DayListBox.SelectedValue!=null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                int day = (int)DayListBox.SelectedValue;

                DayEvents justDayEvents = currentDayEvents.FirstOrDefault(x => x.RightDate.Year == year && x.RightDate.Month == month && x.RightDate.Day == day);
                DisplayDayEventsResults(justDayEvents);
                
            }
        }

        private void DisplayDayEventsResults(DayEvents justDayEvents)
        {
            if (justDayEvents != null)
            {
                List<TableData> results = new List<TableData>();
                foreach (var justEvent in justDayEvents.Events.Reverse<Event>())
                {
                    if (justEvent.PlusMinus == "0")
                    {
                        results.Add(new TableData { BSP = justEvent.BSP, Lus = "-", Place = justEvent.PlaceCoef, SP = justEvent.SP, ParticipantCount=justEvent.ParticipantCount, PlaceInRace=justEvent.Place });
                    }
                    else
                    {
                        results.Add(new TableData { BSP = justEvent.BSP, Lus = "+", Place = justEvent.PlaceCoef, SP = justEvent.SP, ParticipantCount = justEvent.ParticipantCount, PlaceInRace = justEvent.Place });
                    }
                }
                ResultsTable.ItemsSource = results;
            }
        }

        private void YearListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                DayListBox.ItemsSource = currentDayEvents.Where(x => x.RightDate.Year == year && x.RightDate.Month == month).GroupBy(x => x.RightDate.Day).Select(x => x.Key).OrderBy(x => x);
            }
        }

        double minCoef = 0;
        double maxCoef = 0;

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            bool isLusPlay = LusRadioButton.IsChecked.Value;
            bool isBSP = BSPRadioButton.IsChecked.Value;
            bool isBSPPlace = BSPPlaceRadioButton.IsChecked.Value;
            bool isSP = SPRadioButton.IsChecked.Value;
            bool isNo4Participants = No4Race.IsChecked.Value;
            bool isNo5Participants = No5Race.IsChecked.Value;

            bool isNo3Participants = No3Race.IsChecked.Value;
            bool isNo6Participants = No6Race.IsChecked.Value;

            bool isYearBank = YearBank.IsChecked.Value;

            bool isMonthBank = MonthBank.IsChecked.Value;

            bool isUseStops = UseStops.IsChecked.Value;
            bool isUseStopLus = false;
            double stopprofit = 0;
            double stoplus = 0;

            if(!double.TryParse(StopProfitValue.Text, out stopprofit))
                return;

            if(double.TryParse(StopLusValue.Text, out stoplus)){
                isUseStopLus = true;
            }


            double bank = 0;
            double percent = 0;
            if (!(double.TryParse(BankTextBox1.Text, out bank) && double.TryParse(OddTextBox1.Text, out percent)))
            {
                return;
            }

            if (YearListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                try
                {
                    double odd = double.Parse(DayOdd.Text);
                    Dictionary<int, double> MonthProfit = new Dictionary<int, double>();
                    List<Object> raceResult = new List<object>();
                    Dictionary<string, double> DayProfit = new Dictionary<string, double>();

                    minCoef = double.Parse(MinCoef.Text);
                    maxCoef = double.Parse(MaxCoef.Text);

                    double startBank = bank;
                    double currentBank = bank;

                    var yearEvents = currentDayEvents.Where(x => x.RightDate.Year == year);
                    for (int i = 1; i <= 12; i++)
                    {
                        double monthProfit = 0;

                        startBank = bank;
                        if (!isYearBank)
                        {
                            currentBank = bank;
                        }

                        var monthEvents = yearEvents.Where(x => x.RightDate.Month == i).OrderBy(x=>x.RightDate.Day);
                        foreach (var dayEvents in monthEvents)
                        {
                           double startDayBank = currentBank;
                           double currentDayBank = currentBank;

                           if (!(isMonthBank ))
                           {
                               currentDayBank = 0;
                               startDayBank = 0;
                           }
                           
                            foreach (var race in dayEvents.Events.OrderBy(x=>x.Time))
                            {
                                if (!(isMonthBank ))
                                {
                                    currentDayBank += currentBank;
                                    startDayBank += currentBank;
                                }

                                if(isUseStops && isMonthBank){
                                    if (currentDayBank / startDayBank - 1 > stopprofit / 100)
                                        break;
                                    if ((isUseStopLus) && (1 - currentDayBank / startDayBank > stoplus / 100))
                                        break;
                                }

                                double plus = 0;
                                int raceParticipant = 0;
                                int.TryParse(race.ParticipantCount, out raceParticipant);
                                if (!(isNo4Participants && raceParticipant == 4))
                                    if(!(isNo5Participants && raceParticipant == 5))
                                        if (!(isNo6Participants && raceParticipant == 6))
                                            if (!(isNo3Participants && raceParticipant == 3))
                                {
                                    if (isMonthBank)
                                    {
                                        if (currentBank  >= 0)
                                        {
                                            odd = currentBank * percent/100;
                                            
                                        }
                                        else
                                            continue;
                                    }
                                    if (isBSPPlace)
                                    {
                                        #region BSP + Place
                                        if (isLusPlay)
                                        {
                                            if ((race.Place == "1"))
                                            {
                                                plus = GetDayMinus(dayEvents.EventDate, odd, race, true);
                                            }
                                            else
                                            {
                                                plus = GetDayPlus(dayEvents.EventDate, odd, race, true);
                                            }
                                            if (isFirstPlace(race.Place, race.ParticipantCount))
                                            {
                                                plus += GetDayMinus(dayEvents.EventDate, odd, race, false);
                                            }
                                            else
                                            {
                                                plus += GetDayPlus(dayEvents.EventDate, odd, race, false);
                                            }

                                        }
                                        else
                                        {
                                            if ((race.Place == "1"))
                                            {

                                                plus = GetMinusZeroDotZeroFiveFromNumber(GetDayMinus(dayEvents.EventDate, odd, race, true));
                                            }
                                            else
                                            {
                                                plus = GetFullNumberFromZeroDotNineFive(GetDayPlus(dayEvents.EventDate, odd, race, true));
                                            }
                                            if (isFirstPlace(race.Place, race.ParticipantCount))
                                            {
                                                plus += GetMinusZeroDotZeroFiveFromNumber(GetDayMinus(dayEvents.EventDate, odd, race, false));
                                            }
                                            else
                                            {
                                                plus += GetFullNumberFromZeroDotNineFive(GetDayPlus(dayEvents.EventDate, odd, race, false));
                                            }
                                            plus *= -1;
                                        }
                                        #endregion
                                    }

                                    else
                                        if (!isSP)
                                        {
                                            #region BSP or Place
                                            if (isLusPlay)
                                            {

                                                if (((race.Place == "1") && (isBSP)) || ((!isBSP) && isFirstPlace(race.Place, race.ParticipantCount)))
                                                {
                                                    plus = GetDayMinus(dayEvents.EventDate, odd, race, isBSP);
                                                }
                                                else
                                                {
                                                    plus = GetDayPlus(dayEvents.EventDate, odd, race, isBSP);
                                                }

                                            }
                                            else
                                            {
                                                if (((race.Place == "1") && (isBSP)) || ((!isBSP) && isFirstPlace(race.Place, race.ParticipantCount)))
                                                {
                                                    plus = GetMinusZeroDotZeroFiveFromNumber(GetDayMinus(dayEvents.EventDate, odd, race, isBSP));
                                                }
                                                else
                                                {
                                                    plus = GetFullNumberFromZeroDotNineFive(GetDayPlus(dayEvents.EventDate, odd, race, isBSP));
                                                }
                                                plus *= -1;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region isSP
                                            double SP = 0;
                                            plus = 0;
                                            double.TryParse(race.SP, out SP);
                                            if ((SP >= minCoef) && (SP <= maxCoef))
                                            {
                                                if (isLusPlay)
                                                {

                                                    if (race.Place == "1")
                                                    {

                                                        plus = -1 * (SP - 1) * odd;
                                                    }
                                                    else
                                                    {
                                                        plus = odd - 0.05 * odd;
                                                    }

                                                }
                                                else
                                                {
                                                    if (race.Place == "1")
                                                    {
                                                        plus = GetMinusZeroDotZeroFiveFromNumber((SP - 1) * odd);
                                                    }
                                                    else
                                                    {
                                                        plus = -1 * odd;
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                }
                                else
                                {
                                    continue;
                                }

                                if (isMonthBank)
                                {
                                    currentBank += plus;
                                }

                                currentDayBank += plus;

                                if (plus > 0)
                                {
                                    raceResult.Add(new { Date_Time = dayEvents.RightDate.Month + "-" + dayEvents.RightDate.Day + " " + race.Time, Summary = Math.Round(plus, 2), Plus = "   " });
                                }
                                else
                                if(plus<0)
                                {
                                    
                                    raceResult.Add(new { Date_Time = dayEvents.RightDate.Month + "-" + dayEvents.RightDate.Day + " " + race.Time, Summary = Math.Round(plus, 2), Plus = " " });
                                }
                                else
                                {
                                    raceResult.Add(new { Date_Time = dayEvents.RightDate.Month + "-" + dayEvents.RightDate.Day + " " + race.Time, Summary = Math.Round(plus, 2), Plus = "  " });
                                }

                                if (!DayProfit.ContainsKey(dayEvents.EventDate))
                                {
                                    DayProfit.Add(dayEvents.EventDate, plus);
                                }
                                else
                                {
                                    DayProfit[dayEvents.EventDate] += plus;
                                }

                                if (!MonthProfit.ContainsKey(i))
                                {
                                    MonthProfit.Add(i, plus);
                                }
                                else
                                {
                                    MonthProfit[i]+=plus;
                                }

                            }
                        }
                    }

                    RaceProfitDataGrid.ItemsSource = raceResult;

                    List<object> dayResult = new List<object>();
                    foreach (var dayProfit in DayProfit)
                    {
                        if (dayProfit.Value < 0)
                        {
                            dayResult.Add(new { Day = dayProfit.Key, Summ = Math.Round(dayProfit.Value, 2), Plus=" " });
                        }
                        else
                            if (dayProfit.Value == 0)
                            {
                                dayResult.Add(new { Day = dayProfit.Key, Summ = Math.Round(dayProfit.Value, 2), Plus = "  " });
                            }
                            else
                            {
                                dayResult.Add(new { Day = dayProfit.Key, Summ = Math.Round(dayProfit.Value, 2), Plus = "   " });
                            }
                        //DayProfitDataGrid.ItemsSource = DayProfit.Select(x => new { Day = x.Key, Summ = Math.Round(x.Value, 2) });
                    }
                    DayProfitDataGrid.ItemsSource = dayResult;

                    List<object> monthResult = new List<object>();

                    foreach (var monthProfit in MonthProfit)
                    {
                        if (monthProfit.Value < 0)
                        {
                            monthResult.Add(new {Month= monthProfit.Key.ToString(), Summ=Math.Round(monthProfit.Value,2), Plus=" "});
                        }
                        else
                            if (monthProfit.Value == 0)
                            {
                                monthResult.Add(new { Month = monthProfit.Key.ToString(), Summ = Math.Round(monthProfit.Value, 2), Plus = "  " });
                            }
                            else
                            {
                                monthResult.Add(new { Month = monthProfit.Key.ToString(), Summ = Math.Round(monthProfit.Value, 2), Plus = "   " });
                            }
                    }

                   
                    var yearSumm =  Math.Round(MonthProfit.Values.Sum(),2);
                    if(yearSumm<0)
                    {
                    monthResult.Add(new { Month = "year", Summ = yearSumm, Plus=" " });
                    }
                    else
                        if(yearSumm==0)
                        {
                            monthResult.Add(new { Month = "year", Summ = yearSumm, Plus="  " });
                        }
                        else{
                            monthResult.Add(new { Month = "year", Summ = yearSumm, Plus="   " });
                        }
                    MonthProfitDataGrid.ItemsSource = monthResult;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private double GetMinusZeroDotZeroFiveFromNumber(double number)
        {
            return number - 0.05 * number;
        }

        private double GetFullNumberFromZeroDotNineFive(double number)
        {
            return (number / 0.95);
        }

        private bool isFirstPlace(string place, string participatnCount)
        {
            try
            {
                int participants = int.Parse(participatnCount);

                if (participants < 4)
                    if (place == "1") return true;
                    else
                        return false;

                if (participants >= 4 && participants <= 7)
                    if (place == "1" || place == "2")
                        return true;
                    else
                        return false;

                if (place == "1" || place == "2" || place == "3")
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private double GetDayPlus(string date, double odd, Event race, bool isBSP)
        {
            if (!race.IsBSPParsed)
            {
                Parser.Parse_BSP_PLACE_COEF(date, race);
            }
            double koef = isBSP ? GetBSP(race) : GetPlace(race);

            if ((koef >= minCoef) && (koef <= maxCoef))
            {
                return odd - 0.05 * odd;
            }
            else
            {
                return 0;
            }
        }

        private double GetDayMinus(string date, double odd, Event race, bool isBSP)
        {
            if (!race.IsBSPParsed)
            {
                Parser.Parse_BSP_PLACE_COEF(date, race);
            }
            double koef = isBSP ? GetBSP(race) : GetPlace(race);
            if((koef>=minCoef)&&(koef<=maxCoef))
            {
                return koef!=0?-1*(koef - 1) * odd:0;
            }
            else
                return 0;

            

        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            SetTimeDif();
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null && DayListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                int day = (int)DayListBox.SelectedValue;

                var justEvents = currentDayEvents.FirstOrDefault(x => x.RightDate.Day == day && x.RightDate.Month == month && x.RightDate.Year == year);
                foreach (var justEvent in justEvents.Events)
                {
                    Parser.Parse_BSP_PLACE_COEF(justEvents.EventDate, justEvent);
                }
                DayListBox_SelectionChanged(null, null);
            }
        }

        public void SetTimeDif()
        {
            int timeDif = 0;
            if (!int.TryParse(TimeDifTextBox.Text, out timeDif))
                TIME_DIFF = -4;
            TIME_DIFF = timeDif;
        }

        public static int TIME_DIFF = -4;

        private void TimeDifTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void MonthUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearListBox.SelectedValue != null && MonthListBox.SelectedValue != null)
            {
                int year = (int)YearListBox.SelectedValue;
                int month = (int)MonthListBox.SelectedValue;
                SetTimeDif();
                ParseDataForMonth(topDayEvents, TOP_ID, FILE_NAME_TOP, year, month);
                ParseDataForMonth(outDayEvents, OUT_ID, FILE_NAME_OUT, year, month);
            }
        }

            private void ParseDataForMonth(List<DayEvents> dayEventsList, string linkId, string fileName, int year, int month)
        {
            int pageNumber = 1;
            while (Parser.ParseDataForMonth(String.Format(LINK_TEMPLATE, linkId, pageNumber), dayEventsList, pageNumber, year, month))
            {
                pageNumber++;
            }
            SaveEvents(fileName, dayEventsList);
        }

    }
}
