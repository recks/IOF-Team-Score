using IOF_Team_Score.Model;
using IOF_Team_Score.Services;
using IOF_Team_Score.Util;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.XmlAccess;
using System.Configuration;
using System.Text;

namespace IOF_Team_Score
{
    public partial class IOFTeamScore : Form
    {
        // Configuration
        IConfigurationRoot config;
        Options options;

        // Parser for IOF XML 3.0 file
        EventFileParser EventFileParser;
        
        // Events
        List<Event> ActiveEvents;
        BindingSource bindingSource_Events = new BindingSource();
        
        // Team results
        List<Country> CountryScores = [];

        // Fonts used
        private static Font fontNormal = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static Font fontBold = new Font("Segoe UI", 9F, FontStyle.Bold);

        // Excel styles
        ExcelNamedStyleXml? style;
        ExcelNamedStyleXml? countryHeader;

        public IOFTeamScore()
        {
            config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            InitializeComponent();

            options = OptionsService.Load();
            // Bind Event Type label to Options
            EventTypeLabel.DataBindings.Add("Text", options, "EventType", false, DataSourceUpdateMode.OnPropertyChanged);

            // Bind events to listbox with results
            ActiveEvents = new List<Event>();
            bindingSource_Events.DataSource = ActiveEvents;
            listbox_ResultFiles.DataSource = bindingSource_Events;

            EventFileParser = new EventFileParser();
        }

        #region HTML handling

        private void updateIndividualReport()
        {
            StringBuilder report = new StringBuilder($@"
<html>
<head>
  <link rel=""stylesheet"" href=""teamscore.css"">
</head>
<body>
<h1 class='o-h1'>IOF Team Score</h1>
");
            foreach (Event evt in ActiveEvents)
            {
                report.AppendLine($"<h2 class='o-h2'>{evt.Name}</h2>");
                foreach (Clazz cls in evt.Clazzes)
                {
                    report.AppendLine($"<h3 class='o-h3'>{cls.Name}</h3>");
                    report.AppendLine("<table class='o-table'><tr class='o-tr'><th class='o-th'></th><th class='o-th'>Name</th><th class='o-th'>Country</th><th class='o-th'>Time</th><th class='o-th'>Points</th></tr>");
                    cls.PersonsOrTeams.Sort((a, b) => (a.Time, a.Name).CompareTo((b.Time, b.Name)));  // Sort by time, but in case of a tie, sort by name
                    foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                    {
                        report.AppendLine($"<tr class='o-tr'><td class='o-td'>{pot.Place}</td><td class='o-td o-l'>{pot.Name}</td><td class='o-td o-l'>{pot.Country}</td><td class='o-td'>{ (pot.Time<int.MaxValue ? secondsToHHMMSS(pot.Time) : "") }</td><td class='o-td'>{pot.Score}</td></tr>");
                    }
                    report.AppendLine("</table>");
                }
            }
            report.AppendLine("</body></html>");

            htmlPanel_Individual.Text = report.ToString();
        }

        private void updateTotalReport()
        {
            StringBuilder report = new StringBuilder($@"
<html>
<head>
  <link rel=""stylesheet"" href=""teamscore.css"">
</head>
<body>
<h1 class='o-h1'>IOF Team Score - Result</h1>
<table class='o-table'>
<tr class='o-tr'><th class='o-th'></th><th class='o-th'></th> <!-- Event Header -->
");
            var classheader = new StringBuilder("<tr class='o-tr'><th class='o-th'></th><th class='o-th'>Country</th>");
            for (int i = 0; i < ActiveEvents.Count; i++)
            {
                Event evt = ActiveEvents[i];
                foreach (Clazz cls in evt.Clazzes)
                {
                    classheader.Append($"<th class='o-th-{i % 3}'>{cls.Name}</th>");
                }
                classheader.Append($"<th class='o-th-{i % 3}'>Total</th>");
                report.AppendLine($"<th class='o-th-{i % 3}' colspan='{evt.Clazzes.Count + 1}'>{evt.Name}</th>");  // Event header
            }
            report.AppendLine($"</tr>");  // Event header
            report.AppendLine($"{classheader}<th class='o-th-total'>TOTAL</th></tr>");

            // Country scores
            var sortedDict = Event.StringToEventType[options.EventType] == EventType.EYOC  // Of course they have different sorting order :-)
                ? CountryScores.OrderByDescending(country => country.TotalScore())
                : CountryScores.OrderBy(country => country.TotalScore());
            int place = 0;
            int sameplace = 1;
            int lastCountrysScore = 0;
            foreach (var country in sortedDict)
            {
                var totalScore = country.TotalScore();
                // Calculate place with same points getting same place.
                if (totalScore != lastCountrysScore)
                {
                    place += sameplace;
                    sameplace = 1;
                }
                else
                {
                    sameplace++;
                }
                report.Append($"<tr class='o-tr'><td align='right' class='o-td'>{place}</td><td align='center' class='o-td o-l'>{country.Name}</td>");
                foreach (Event evt in ActiveEvents)
                {
                    foreach (Clazz cls in evt.Clazzes)
                    {
                        Tuple<string, string> scoreTuple = new Tuple<string, string>(evt.Name, cls.Name);
                        object score = country.Scores.ContainsKey(scoreTuple) ? country.Scores[scoreTuple] : "";
                        report.Append($"<td align='center' class='o-td'>{score}</td>");
                    }
                    report.Append($"<td align='center' class='o-td'>{country.TotalScoreForEvent(evt.Name)}</td>");
                }
                report.AppendLine($"<td align='center' class='o-td'>{totalScore}</td></tr>");
                lastCountrysScore = totalScore;
            }
            report.AppendLine("</table>");
            report.AppendLine("</body></html>");

            htmlPanel_Total.Text = report.ToString();
        }

        #endregion

        #region Excel handling

        private void initializeExcelStyles(ExcelWorkbook workBook)
        {
            style = workBook.Styles.CreateNamedStyle("Default");
            //style.Style.Font.Name = "Calibri";
            style.Style.Font.Family = 2;
            style.Style.Font.Size = 12;

            style = workBook.Styles.CreateNamedStyle("Header");
            style.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.Style.Font.Name = "Calibri";
            style.Style.Font.Bold = true;
            style.Style.Font.Family = 2;
            style.Style.Font.Size = 11;
            style.Style.Font.Color.SetColor(Color.White);
            style.Style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Style.Fill.BackgroundColor.SetColor(Color.DarkGreen);

            style = workBook.Styles.CreateNamedStyle("Country");
            style.Style.Font.Name = "Calibri";
            style.Style.Font.Bold = true;
            style.Style.Font.Family = 2;
            style.Style.Font.Size = 12;
            style.Style.Font.Color.SetColor(Color.Black);
            style.Style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

            for (int i = 0; i < 3; i++)
            {
                style = workBook.Styles.CreateNamedStyle("EventHeader" + i);
                style.Style.Font.Bold = true;
                style.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                style.Style.Fill.PatternType = ExcelFillStyle.Solid;
                style.Style.Fill.BackgroundColor.SetColor(GetColour(i, true));

                style = workBook.Styles.CreateNamedStyle("EventScores" + i);
                style.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                style.Style.Fill.PatternType = ExcelFillStyle.Solid;
                style.Style.Fill.BackgroundColor.SetColor(GetColour(i, false));
            }
        }

        private void writeExcelSheet(string filename)
        {
            using (var package = new ExcelPackage())
            {
                initializeExcelStyles(package.Workbook);

                // Temp variable used to calculate placings
                int place;
                int sameplace;
                int lastCountrysScore;
                // Temp variables used to navigate worksheet
                ExcelWorksheet workSheet;
                int i;
                int j;

                // Sheets per event:
                // - One total
                // - A sheet per class with individual results
                foreach (Event evt in ActiveEvents)
                {
                    //// First: one sheet with country scores

                    workSheet = package.Workbook.Worksheets.Add(evt.Name);
                    workSheet.DefaultRowHeight = 13;
                    //workSheet.Rows.StyleName = "Default";
                    //evt.Clazzes.Sort();

                    // Worksheet header
                    workSheet.Row(1).Height = 20;
                    j = 2;  // column
                    workSheet.Cells[1, j++].Value = "Name";
                    foreach (Clazz cls in evt.Clazzes)
                    {
                        workSheet.Cells[1, j++].Value = cls.Name;
                    }
                    workSheet.Cells[1, j].Value = "Total";
                    //workSheet.Cells[1, 1, 1, j].StyleName = "Header";  // Dark green background

                    // Worksheet content
                    var countries = Event.StringToEventType[options.EventType] == EventType.EYOC  // Of course they have different sorting order :-)
                        ? CountryScores.OrderByDescending(country => country.TotalScoreForEvent(evt.Name))
                        : CountryScores.OrderBy(country => country.TotalScoreForEvent(evt.Name));

                    place = 0;
                    sameplace = 1;
                    lastCountrysScore = 0;
                    i = 2;  // We start at row 2
                    foreach (var country in countries)
                    {
                        var totalScore = country.TotalScoreForEvent(evt.Name);
                        // Calculate place with same points getting same place.
                        if (totalScore != lastCountrysScore)
                        {
                            place += sameplace;
                            sameplace = 1;
                            lastCountrysScore = totalScore;
                        }
                        else
                        {
                            sameplace++;
                        }

                        // Row content - country
                        workSheet.Row(i).Height = 18;
                        j = 1;  // Column
                        workSheet.Cells[i, j++].Value = place;
                        workSheet.Cells[i, j++].Value = country.Name;
                        foreach (Clazz cls in evt.Clazzes)
                        {
                            // Look at each class for the country
                            Tuple<string, string> scoreTuple = new Tuple<string, string>(evt.Name, cls.Name);
                            int score = country.Scores.ContainsKey(scoreTuple) ? country.Scores[scoreTuple] : 0;
                            workSheet.Cells[i, j++].Value = score != 0 ? score : "";
                        }
                        workSheet.Cells[i, j++].Value = totalScore;
                        //workSheet.Cells[i, 1, i, j-1].StyleName = "Country";  // Light green background
                        i++;

                        // Row content - runners
                        var runnerLists = from cls in evt.Clazzes select cls.PersonsOrTeams;
                        j = 3;  // Start column for scores
                        foreach (List<PersonOrTeam> runners in runnerLists)
                        {
                            List<PersonOrTeam> countingRunners = runners.FindAll(r => r.Country == country.Name && r.Counting).ToList();
                            foreach (PersonOrTeam runner in countingRunners)
                            {
                                workSheet.Cells[i, 2].Value = runner.Name;
                                workSheet.Cells[i, j].Value = runner.Score;
                                i++;
                            }
                            j++;
                        }
                    }
                    // Fit columns
                    workSheet.Cells[1, 1, i, j].AutoFitColumns();

                    //// Second: one sheet per class

                    foreach (Clazz cls in evt.Clazzes)
                    {
                        // Header
                        workSheet = package.Workbook.Worksheets.Add($"{evt.Name}, {cls.Name}");
                        workSheet.Cells[1, 2].Value = "Name";
                        workSheet.Cells[1, 3].Value = "Country";
                        workSheet.Cells[1, 4].Value = "Time";
                        workSheet.Cells[1, 5].Value = "Points";

                        // Content, 1 row per runner.
                        i = 2;  // row
                        cls.PersonsOrTeams.Sort((a, b) => (a.Time, a.Name).CompareTo((b.Time, b.Name)));
                        foreach (PersonOrTeam pot in cls.PersonsOrTeams)
                        {
                            workSheet.Cells[i, 1].Value = pot.Place;
                            workSheet.Cells[i, 2].Value = pot.Name;
                            workSheet.Cells[i, 3].Value = pot.Country;
                            workSheet.Cells[i, 4].Value = pot.Time<int.MaxValue ? secondsToHHMMSS(pot.Time) : "";
                            workSheet.Cells[i, 5].Value = pot.Score;
                            i++;
                        }
                        workSheet.Cells[1, 1, i, 5].AutoFitColumns();
                    }

                }

                // Sheet with total
                workSheet = package.Workbook.Worksheets.Add("Total");
                workSheet.DefaultRowHeight = 13;
                //workSheet.Rows.StyleName = "Default";
                //evt.Clazzes.Sort();

                // Worksheet header
                workSheet.Row(1).Height = 20;
                j = 3;  // column
                for (int k = 0; k < ActiveEvents.Count; k++)
                {
                    int start = j;
                    Event evt = ActiveEvents[k];
                    workSheet.Cells[1, j, 1, j + evt.Clazzes.Count].Merge = true;  // Event header
                    workSheet.Cells[1, j].Value = evt.Name;
                    //workSheet.Cells[1, j].StyleName = "EventHeader" + (k % 3);
                    foreach (Clazz cls in evt.Clazzes)
                    {
                        workSheet.Cells[2, j++].Value = cls.Name;
                    }
                    workSheet.Cells[2, j++].Value = "Total";
                    //workSheet.Cells[2, start, 2, j - 1].StyleName = "EventScores" + (k % 3);
                    workSheet.Cells[2, start, 2, j - 1].Style.Font.Bold = true;
                }
                workSheet.Cells[2, j].Value = "TOTAL";
                workSheet.Cells[2, j].Style.Font.Bold = true;
                workSheet.Cells[2, j].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Worksheet content
                var totalCountries = Event.StringToEventType[options.EventType] == EventType.EYOC  // Of course they have different sorting order :-)
                    ? CountryScores.OrderByDescending(country => country.TotalScore())
                    : CountryScores.OrderBy(country => country.TotalScore());
                place = 0;
                sameplace = 1;
                lastCountrysScore = 0;
                i = 3;  // We start at row 3
                foreach (var country in totalCountries)
                {
                    j = 1;  // Column
                    var totalScore = country.TotalScore();
                    // Calculate place with same points getting same place.
                    if (totalScore != lastCountrysScore)
                    {
                        place += sameplace;
                        sameplace = 1;
                        lastCountrysScore = totalScore;
                    }
                    else
                    {
                        sameplace++;
                    }

                    workSheet.Cells[i, j++].Value = place;
                    workSheet.Cells[i, j++].Value = country.Name;
                    for (int k = 0; k < ActiveEvents.Count; k++)
                    {
                        Event evt = ActiveEvents[k];
                        foreach (Clazz cls in evt.Clazzes)
                        {
                            Tuple<string, string> scoreTuple = new Tuple<string, string>(evt.Name, cls.Name);
                            workSheet.Cells[i, j].Value = country.Scores.ContainsKey(scoreTuple) ? country.Scores[scoreTuple] : "";
                            //workSheet.Cells[i, j].StyleName = "EventScores" + (k % 3);
                            j++;
                        }
                        workSheet.Cells[i, j].Value = country.TotalScoreForEvent(evt.Name);
                        //workSheet.Cells[i, j].StyleName = "EventScores" + (k % 3);
                        j++;
                    }
                    workSheet.Cells[i, j++].Value = country.TotalScore();

                    i++;
                }


                // Save to file
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                FileStream objFileStrm = File.Create(filename);
                objFileStrm.Close();
                File.WriteAllBytes(filename, package.GetAsByteArray());
            }
        }

        #endregion

        #region Utility methods

        private string secondsToHHMMSS(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.Hours > 0 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }

        // Creates three different colours based on index and if it should be saturated
        private static readonly Color[] Colours =
        {
            ColorTranslator.FromHtml("#CCFFCC"), ColorTranslator.FromHtml("#FFCCCC"), ColorTranslator.FromHtml("#CCCCFF"),
            ColorTranslator.FromHtml("#66FF66"), ColorTranslator.FromHtml("#FF6666"), ColorTranslator.FromHtml("#6666FF")
        };
        private Color GetColour(int index, bool saturated)
        {
            index = index % 3;
            return Colours[index + (saturated ? 3 : 0)];
        }
        #endregion

        #region GUI Events

        private void ImportFile_Click(object sender, EventArgs e)
        {
            if (openResultFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var eventType = Event.StringToEventType[options.EventType];
                    Event @event = EventFileParser.Parse(openResultFileDialog.FileName, eventType);
                    if (ActiveEvents.Exists(e => e.Id == @event.Id))
                    {
                        MessageBox.Show("This event is already loaded.", "Duplicate", MessageBoxButtons.OK);
                        return;
                    }
                    ActiveEvents.Add(@event);
                    bindingSource_Events.ResetBindings(false);
                    btn_CalculateTeamScores.Font = fontBold;
                }
                catch (IofXmlParseException ex)
                {
                    MessageBox.Show("The file doesn't seem to contain a valid IOF XML 3.0 file.", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (KeyNotFoundException ex)
                {
                    MessageBox.Show("You need to specify the type of event before loading a result file.\nThis is done in Tools -> Options.", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportReport_Click(object sender, EventArgs e)
        {
            if (exportTeamScoresIndividualDialog.ShowDialog() == DialogResult.OK)
            {
                var filename = exportTeamScoresIndividualDialog.FileName;
                File.WriteAllText(filename, htmlPanel_Individual.Text);
            }
            if (exportTeamScoresTotalDialog.ShowDialog() == DialogResult.OK)
            {
                var filename = exportTeamScoresTotalDialog.FileName;
                File.WriteAllText(filename, htmlPanel_Total.Text);
            }
            var csssrc = config.GetRequiredSection("CSSsrc").Get<string>();
            if (csssrc != null && options.ExportCSS && exportTeamScoresCSSDialog.ShowDialog() == DialogResult.OK)
            {
                var dst = exportTeamScoresCSSDialog.FileName;
                File.Copy(csssrc, dst, true);
            }
        }

        private void ExportSheet_Click(object sender, EventArgs e)
        {
            if (exportExcelDialog.ShowDialog() == DialogResult.OK)
            {
                var filename = exportExcelDialog.FileName;
                try
                {
                    writeExcelSheet(filename);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Couldn't write to {filename}\nIs it open in another application?", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Options_Click(object sender, EventArgs e)
        {
            OptionsBox box = new OptionsBox(options);
            box.Show();
        }

        private void About_Click(object sender, EventArgs e)
        {
            AboutBox box = new AboutBox();
            box.Show();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_CalculateTeamScores_Click(object sender, EventArgs e)
        {
            IScoreCalculator scoreCalculator = new EYOCCalculator(config);
            try
            {
                switch (Event.StringToEventType[options.EventType])
                {
                    case EventType.EYOC:
                        scoreCalculator = new EYOCCalculator(config);
                        break;
                    case EventType.JWOC:
                        scoreCalculator = new JWOCCalculator();
                        break;
                }

                scoreCalculator.CalculateScores(ActiveEvents);
                updateIndividualReport();
                CountryScores = scoreCalculator.CalculateTotalScores(ActiveEvents);
                updateTotalReport();
                btn_CalculateTeamScores.Font = fontNormal;
            }
            catch (ConfigurationErrorsException ex)
            {
                DialogResult result = MessageBox.Show(ex.Message, "Failed to load configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void ctxmenu_DeleteResultFile_Click(object sender, EventArgs e)
        {
            // Delete the selected file from the EventList
            ActiveEvents.RemoveAt(listbox_ResultFiles.SelectedIndex);
            bindingSource_Events.ResetBindings(false);
            btn_CalculateTeamScores.Font = fontBold;
        }

        private void listbox_ResultFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                MouseEventArgs myMEArs = new MouseEventArgs(MouseButtons.Left, e.Clicks, e.X, e.Y, e.Delta);
                var p = new Point(e.X, e.Y);
                int selectedIndx = this.listbox_ResultFiles.IndexFromPoint(p);
                if (selectedIndx != ListBox.NoMatches)
                {
                    listbox_ResultFiles.SelectedIndex = selectedIndx;
                    ctxmenu_DeleteResultFile.Show((ListBox)sender, p);
                }
            }
        }

        #endregion
    }
}
