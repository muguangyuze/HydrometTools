﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Reclamation.TimeSeries;
//using Steema.TeeChart.Styles;
using Reclamation.TimeSeries.Hydromet.Operations;

namespace FcPlot
{
    public partial class HydrometTeeChart : UserControl
    {

        public HydrometTeeChart()
        {
            InitializeComponent();
            InitTChart();
        }

        private void InitTChart()
        {
            this.tChart1.Axes.Bottom.Labels.DateTimeFormat = "MMMdd";

        }

        private Steema.TeeChart.Styles.Line CreateSeries(System.Drawing.Color color , string title, Series s, string axis, bool dash=false)
        {
            var rval = new Steema.TeeChart.Styles.Line();
            rval.Brush.Color = color;
            rval.Color = color;
            rval.Title = title;
            if(axis == "right")
            {
                rval.VertAxis = Steema.TeeChart.Styles.VerticalAxis.Right;
            }
            if (dash)
            {
                rval.LinePen.Style = System.Drawing.Drawing2D.DashStyle.Dash;
            }
            tChart1.Series.Add(rval);
            ReadIntoTChart(s, rval);
            return rval;
        }

        //added for target not sure if needed
        public Steema.TeeChart.Styles.Line CreateTarget(System.Drawing.Color color, string title, Series s, string axis)
        {
            var rval = new Steema.TeeChart.Styles.Line();
            rval.LinePen.Width = 2;
            rval.Brush.Color = color;
            rval.Color = color;
            rval.Title = title;
            if (axis == "right")
            {
                rval.VertAxis = Steema.TeeChart.Styles.VerticalAxis.Right;
            }
            tChart1.Series.Add(rval);
            ReadIntoTChart(s, rval);
            return rval;
        }

        internal void SetLabels(string title, string yAxis)
        {
            tChart1.Axes.Left.Title.Caption = yAxis;
            tChart1.Axes.Left.FixedLabelSize = false;
            tChart1.Text = title;            
        }

        internal void Fcplot(Series actual, Series required, Series alternateRequiredContent,
            Series alternateActualContent, SeriesList ruleCurves, DateTime[] labelDates, 
            String RequiredLegend, SeriesList userInput,bool greenLines, bool dashed) 
        {
            tChart1.Zoom.Undo();
            this.tChart1.Series.RemoveAllSeries();
            tChart1.Axes.Bottom.Labels.Style = Steema.TeeChart.AxisLabelStyle.Value;
            
            if (labelDates.Length != ruleCurves.Count)
                throw new Exception("Error: The number of label dates " + labelDates.Length
                    + " must match the number of forecast levels " + ruleCurves.Count);
            //add green lines
            if (greenLines)
            {
                AddRuleCurves(ruleCurves, labelDates,dashed);
            }

            Color[] colors = {Color.Black,Color.Orange,Color.DarkKhaki,Color.Brown,
                                 Color.Aqua,Color.Olive,Color.BurlyWood,Color.MediumSpringGreen,
                                 Color.CadetBlue,Color.Chartreuse, Color.Chocolate,Color.Coral,Color.CornflowerBlue};
            for (int i = 0; i < userInput.Count; i++)
            {
                if (i <= colors.Length)
                {
                    var s = Reclamation.TimeSeries.Math.ShiftToYear(userInput[i], required[0].DateTime.Year);
                    CreateSeries(colors[i], userInput[i].Name, s, "right");
                }
                else
                {
                    var s = Reclamation.TimeSeries.Math.ShiftToYear(userInput[i], required[0].DateTime.Year);
                    CreateSeries(colors[i-colors.Length], userInput[i].Name, s, "right");
                }
            }
            //add alternative lines
            if (alternateRequiredContent.Count > 0) //&& required.Count >0)
            {
                var s = Reclamation.TimeSeries.Math.ShiftToYear(alternateRequiredContent, required[0].DateTime.Year);
                CreateSeries(Color.BlueViolet, alternateRequiredContent.Name + " " + RequiredLegend, s, "left");
                s = Reclamation.TimeSeries.Math.ShiftToYear(alternateActualContent, required[0].DateTime.Year);
                CreateSeries(Color.LightSkyBlue, alternateActualContent.Name, s, "left");
            }
            //add lines
            CreateSeries(Color.Red, required.Name + " " + RequiredLegend, required, "left");
            CreateSeries(Color.Blue, actual.Name, actual,"left");

           

            // zoom out a little..
           double min=0, max=0;
           tChart1.Axes.Left.Automatic = true;
           tChart1.Axes.Left.CalcMinMax(ref min, ref max);
           tChart1.Axes.Left.Maximum = max * 1.01;
           tChart1.Axes.Left.Minimum = min - 1000;
           tChart1.Axes.Left.Automatic = false;

            //tChart1.Axes.Bottom.in
           //tChart1.Axes.Left.Automatic = true;

           //tChart1.Zoom.ZoomPercent(10);


        }

        private void AddRuleCurves(SeriesList ruleCurves, DateTime[] labelDates,bool dashed)
        {
            for (int i = 0; i < ruleCurves.Count; i++)
            { 
                var s = ruleCurves[i];
                var ts = new Steema.TeeChart.Styles.Line(tChart1.Chart);
                ts.Marks.Visible = true;
                ts.Color = System.Drawing.Color.Green;
                ts.Marks.Style = Steema.TeeChart.Styles.MarksStyles.Label;
                ts.Marks.Arrow.Visible = false;
                ts.Marks.ArrowLength = 0;
                ts.Legend.Visible = false;

                if( dashed)
                  ts.LinePen.Style = System.Drawing.Drawing2D.DashStyle.Dash;

                ts.Title = s.Name;
                ReadIntoTChart(s, ts);

                //int idx = FindLabelIndex(s);
                int idx = FindLabelIndex(s, labelDates[i].Month, labelDates[i].Day);
                if (idx > 0 && idx < ts.Marks.Items.Count - 1)
                {
                    ts[idx].Label = s.Name;
                    //ts.Marks.Items[idx].Text = s.Name;
                    ts.Marks.Items[idx].Visible = true;
                }

            }
        }

      

        public void Edit()
        {
            var e = new Steema.TeeChart.Editors.ChartEditor(tChart1.Chart);
            e.ShowDialog();
        }
        private static void ReadIntoTChart(Series series1, Steema.TeeChart.Styles.Line tSeries, bool rightAxis = false)
        {
            tSeries.Clear();
            tSeries.XValues.DateTime = true;

            for (int i = 0; i < series1.Count; i++)
            {
                if (!series1[i].IsMissing)
                {
                    tSeries.Add(series1[i].DateTime, series1[i].Value);
                }
                //tSeries[tSeries.Count - 1].Label ="";
                //tSeries.Marks.Items[tSeries.Count - 1].Text = "\n";
                if( tSeries.Count >0)
                tSeries.Marks.Items[tSeries.Count - 1].Visible = false;
                if (rightAxis)
                {
                    tSeries.VertAxis = Steema.TeeChart.Styles.VerticalAxis.Right;
                }
            }
        }

        //static void tSeries_GetSeriesMark(Steema.TeeChart.Styles.Series series,
        //    Steema.TeeChart.Styles.GetSeriesMarkEventArgs e)
        //{
           
        //    if (series.Tag != null)
        //    {
        //        var idx = Convert.ToInt32(series.Tag);

        //        if (e.ValueIndex == idx)
        //        {
        //            e.MarkText = series.Title;
        //        }
        //    }
        //    else
        //    {
        //        e.MarkText = null;
        //    }
        //}


        /// <summary>
        /// Find the first index to the day and month.
        /// used for label position
        /// </summary>
        /// <param name="s"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private int FindLabelIndex(Series s, int month, int day)
        {
            for (int i = 0; i < s.Count; i++)
            {
                var t = s[i].DateTime;
                if(t.Day == day && t.Month == month)
                    return i;
            }
            return 0;
        }
        ///// <summary>
        ///// Find inflextion (minimum) point to place rule curve/forecast label
        ///// </summary>
        ///// <param name="series1"></param>
        ///// <returns></returns>
        //private static int FindLabelIndex(Series series1)
        //{
        //    if( series1.Count == 0)
        //        return 0;

        //    double previous = series1[0].Value;
        //    double first = series1[0].Value;

        //    for (int i = series1.Count/2; i < series1.Count; i++)
        //    {
        //        if (series1[i].IsMissing)
        //            return System.Math.Max(0 ,i-10);

        //        var val = series1[i].Value;

        //        if( val >= previous && val != first) // first part or curve is often flat
        //            return System.Math.Max(0 ,i-10);

        //        previous = val;
    
        //    }
        //    return series1.Count - 10 ;
        //}

        private void linkLabelEdit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Edit();
        }

        /// <summary>
        /// Method to perform operations given an inflow and an outflow
        /// </summary>
        /// <param name="ui"></param>
        internal void FcOps(FcPlotUI ui, FloodControlPoint pt, FcOpsMenu opsUI)
        {
            // Get required variables from the available resources
            string inflowCode = opsUI.textBoxInflowCode.Text;
            string outflowCode = opsUI.textBoxOutflowCode.Text;
            decimal inflowYear = opsUI.numericUpDownInflowYear.Value;
            decimal outflowYear = opsUI.numericUpDownOutflowYear.Value;
            string[] resCodes = pt.UpstreamReservoirs;
            double maxSpace = pt.TotalUpstreamActiveSpace;
            decimal inflowShift = opsUI.numericUpDownInflowShift.Value;
            decimal outflowShift = opsUI.numericUpDownOutflowShift.Value;
            string inflowScale = opsUI.textBoxInflowScale.Text;
            string outflowScale = opsUI.textBoxOutflowScale.Text;

            // redraw the main graph
            ui.GraphData();

            // Process current inflow curve
            var sInflow = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(inflowCode.Split(' ')[0], inflowCode.Split(' ')[1]);
            DateTime t1 = DateTime.Now;
            DateTime t2 = DateTime.Now;
            int yr = Convert.ToInt32(ui.textBoxWaterYear.Text);
            ui.SetupDates(yr, ref t1, ref t2, false);
            sInflow.Read(t1, t2);
            var sInflowShifted = Reclamation.TimeSeries.Math.ShiftToYear(sInflow, Convert.ToInt16(ui.textBoxWaterYear.Text) - 1);
            CreateSeries(System.Drawing.Color.DarkGoldenrod, ui.textBoxWaterYear.Text + "-Inflow", sInflowShifted, "right");

            // Process current outflow curve
            var sOutflow = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(outflowCode.Split(' ')[0], outflowCode.Split(' ')[1]);
            t1 = DateTime.Now;
            t2 = DateTime.Now;
            yr = Convert.ToInt32(ui.textBoxWaterYear.Text);
            ui.SetupDates(yr, ref t1, ref t2, false);
            sOutflow.Read(t1, t2);
            var sOutflowShifted = Reclamation.TimeSeries.Math.ShiftToYear(sOutflow, Convert.ToInt16(ui.textBoxWaterYear.Text) - 1);
            CreateSeries(System.Drawing.Color.DarkViolet, ui.textBoxWaterYear.Text + "-Outflow", sOutflowShifted, "right");

            // Process simulation inflow curve
            double inflowScaleValue;
            try { inflowScaleValue = Convert.ToDouble(inflowScale); }
            catch { inflowScaleValue = 1.0; }
            sInflow = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(inflowCode.Split(' ')[0], inflowCode.Split(' ')[1]);
            t1 = DateTime.Now;
            t2 = DateTime.Now;
            yr = Convert.ToInt32(inflowYear);
            ui.SetupDates(yr, ref t1, ref t2, false);
            sInflow.Read(t1, t2);
            sInflowShifted = Reclamation.TimeSeries.Math.ShiftToYear(sInflow, Convert.ToInt16(ui.textBoxWaterYear.Text) - 1);
            t1 = sInflowShifted.MinDateTime;
            t2 = sInflowShifted.MaxDateTime;
            sInflowShifted = Reclamation.TimeSeries.Math.Shift(sInflowShifted, Convert.ToInt32(inflowShift));
            sInflowShifted = sInflowShifted.Subset(t1, t2);
            sInflowShifted = sInflowShifted * inflowScaleValue;
            CreateSeries(System.Drawing.Color.Gold, inflowYear + "-Inflow", sInflowShifted, "right", true);

            // Process simulation outflow curve
            double outflowScaleValue;
            try { outflowScaleValue = Convert.ToDouble(outflowScale); }
            catch { outflowScaleValue = 1.0; }
            sOutflow = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(outflowCode.Split(' ')[0], outflowCode.Split(' ')[1]);
            t1 = DateTime.Now;
            t2 = DateTime.Now;
            yr = Convert.ToInt32(outflowYear);
            ui.SetupDates(yr, ref t1, ref t2, false);
            sOutflow.Read(t1, t2);
            sOutflowShifted = Reclamation.TimeSeries.Math.ShiftToYear(sOutflow, Convert.ToInt16(ui.textBoxWaterYear.Text) - 1);
            t1 = sOutflowShifted.MinDateTime;
            t2 = sOutflowShifted.MaxDateTime;
            sOutflowShifted = Reclamation.TimeSeries.Math.Shift(sOutflowShifted, Convert.ToInt32(outflowShift));
            sOutflowShifted = sOutflowShifted.Subset(t1, t2);
            sOutflowShifted = sOutflowShifted * outflowScaleValue;
            if (opsUI.checkBoxUseCustomOutflow.Checked) //apply custom outflow
            {
                DateTime customT1 = opsUI.dateTimePickerCustomOutflow1.Value;
                DateTime customT2 = opsUI.dateTimePickerCustomOutflow2.Value;
                DateTime customT3 = opsUI.dateTimePickerCustomOutflow3.Value;
                var customV1 = Convert.ToInt32(opsUI.textBoxCustomOutflow1.Text);
                var customV2 = Convert.ToInt32(opsUI.textBoxCustomOutflow2.Text);

                Series rval = sOutflowShifted.Clone();
                foreach (var item in sOutflowShifted)
                {
                    DateTime sDate1 = new DateTime(sOutflowShifted.MinDateTime.Year, customT1.Month, customT1.Day);
                    if (customT1.Month <= 9) { sDate1 = new DateTime(sOutflowShifted.MaxDateTime.Year, customT1.Month, customT1.Day); }
                    DateTime sDate2 = new DateTime(sOutflowShifted.MinDateTime.Year, customT2.Month, customT2.Day);
                    if (customT1.Month <= 9) { sDate2 = new DateTime(sOutflowShifted.MaxDateTime.Year, customT2.Month, customT2.Day); }
                    DateTime sDate3 = new DateTime(sOutflowShifted.MinDateTime.Year, customT3.Month, customT3.Day);
                    if (customT1.Month <= 9) { sDate3 = new DateTime(sOutflowShifted.MaxDateTime.Year, customT3.Month, customT3.Day); }

                    var ithDate = item.DateTime;
                    if (ithDate >= sDate1 && ithDate < sDate2 && customV1 >= 0)
                    {
                        rval.Add(ithDate, customV1);
                    }
                    else if (ithDate >= sDate2 && ithDate < sDate3 && customV2 >= 0)
                    {
                        rval.Add(ithDate, customV2);
                    }
                    else
                    {
                        rval.Add(item);
                    }
                }                
            }
            CreateSeries(System.Drawing.Color.Plum, outflowYear + "-Outflow", sOutflowShifted, "right", true);

            // Get observed storage contents
            var ithStorage = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(resCodes[0], "AF");
            t1 = DateTime.Now;
            t2 = DateTime.Now;
            yr = Convert.ToInt32(ui.textBoxWaterYear.Text);
            ui.SetupDates(yr, ref t1, ref t2, false);
            ithStorage.Read(t1, t2);
            Series sStorage = new Series(ithStorage.Table, "content", TimeInterval.Daily);
            for (int i = 1; i < resCodes.Count(); i++)
            {
                ithStorage = new Reclamation.TimeSeries.Hydromet.HydrometDailySeries(resCodes[i], "AF");
                t1 = DateTime.Now;
                t2 = DateTime.Now;
                yr = Convert.ToInt32(ui.textBoxWaterYear.Text);
                ui.SetupDates(yr, ref t1, ref t2, false);
                ithStorage.Read(t1, t2);
                Series sStorageTemp = new Series(ithStorage.Table, "content", TimeInterval.Daily);
                sStorage = sStorage + sStorageTemp;
            }
            Reclamation.TimeSeries.Point lastPt = sStorage[sStorage.Count() - 1];

            // Process simulated storage curve and run storage simulation forward
            DateTime minDate = new DateTime(System.Math.Min(sOutflowShifted.MaxDateTime.Ticks, sInflowShifted.MaxDateTime.Ticks));
            if (lastPt.DateTime < minDate)
            {
                var t = lastPt.DateTime;
                var stor = lastPt.Value;
                var simStorage = new Series();
                var simSpill = new Series();
                simStorage.Add(lastPt);
                while (t < minDate)
                {
                    t = t.AddDays(1);
                    var volIn = (sInflowShifted[t].Value * 86400.0 / 43560.0);
                    var volOut = (sOutflowShifted[t].Value * 86400.0 / 43560.0);
                    var tempStor = stor + volIn - volOut;
                    if (tempStor >= maxSpace)
                    {
                        var spill = tempStor - maxSpace;
                        simSpill.Add(t, spill);
                        stor = maxSpace;
                    }
                    else
                    {
                        stor = tempStor;
                    }
                    simStorage.Add(t, stor);
                }
                CreateSeries(System.Drawing.Color.DodgerBlue, "Simulated Storage (" + inflowYear + "-Qin | " + outflowYear + "-Qout)", simStorage, "left", true);
                if (simSpill.Count() > 0)
                {
                    CreateSeries(System.Drawing.Color.OrangeRed, "Simulated Spill (" + inflowYear + "-Qin | " + outflowYear + "-Qout)", simStorage, "right", true);
                }

            }

        }

    }
}
