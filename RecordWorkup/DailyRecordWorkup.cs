﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Reclamation.Core;
using Reclamation.TimeSeries.Hydromet;
using Reclamation.TimeSeries.Graphing;
using Steema.TeeChart.Styles;
using System.IO;

namespace HydrometTools.RecordWorkup
{
    public partial class DailyRecordWorkup : UserControl
    {
        private ITimeSeriesSpreadsheet timeSeriesSpreadsheet1;
        DataTable siteListTable;
        DataTable hydrometDataTable;
        public DailyRecordWorkup()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            int yr = DateTime.Now.Year - 1;
            this.textBoxWaterYear.Text = yr.ToString();

            var fn = Path.Combine("yak","yakima_record_list.csv");
             fn = FileUtility.GetFileReference(fn);

            siteListTable = new CsvFile(fn, CsvFile.FieldTypes.AllText);

            comboBoxSiteList.DataSource = siteListTable;
            comboBoxSiteList.DisplayMember = "name";
            comboBoxSiteList.ValueMember = "siteid";
            comboBoxSiteList.SelectedIndex = -1;
            comboBoxSiteList.DropDownStyle = ComboBoxStyle.DropDownList;

#if SpreadsheetGear
            var uc = new TimeSeriesSpreadsheetSG();
#else
            var uc = new TimeSeriesSpreadsheet();
#endif
            uc.Parent = this.panelGraphTable;
            uc.BringToFront();
            uc.Dock = DockStyle.Fill;
            timeSeriesSpreadsheet1 = uc;
            uc.Dock = DockStyle.Fill;
            uc.BringToFront();



        }

        private void comboBoxSiteList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSiteList.SelectedIndex >=0  && comboBoxSiteList.ValueMember != "" )
            {
                labelSiteid.Text = comboBoxSiteList.SelectedValue.ToString();
            }
        }

        private void linkLabelRead_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            HydrometHost svr = HydrometInfoUtility.HydrometServerFromPreferences();

            int yr;
            if (!int.TryParse(textBoxWaterYear.Text, out yr))
            {
                MessageBox.Show("Error parsing water year '" + textBoxWaterYear.Text + "'");
                return;
            }
            if (comboBoxSiteList.SelectedIndex < 0)
                return;

            DateTime t1 = new DateTime(yr - 1, 10, 1);
            DateTime t2 = new DateTime(yr, 9, 30);

            int idx = this.comboBoxSiteList.SelectedIndex;
            string siteId = comboBoxSiteList.SelectedValue.ToString();

            string query = siteId + " " + siteListTable.Rows[idx]["parameters"].ToString();
            hydrometDataTable = HydrometDataUtility.ArchiveTable(svr, query, t1, t2);
            hydrometDataTable.AcceptChanges();
            bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;
            timeSeriesSpreadsheet1.Clear();
            timeSeriesSpreadsheet1.SetDataTable(hydrometDataTable, Reclamation.TimeSeries.TimeInterval.Daily, ctrl);

            ReadSeries();
            hydrometDataTable.RowChanged += hydrometDataTable_RowChanged;

        }

        private void hydrometDataTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            if (!(timeSeriesSpreadsheet1.SuspendUpdates))
                   ReadSeries();
        }

        private void ReadSeries()
        {
            
            int sz = hydrometDataTable.Columns.Count;

            
            
            tChart1.Series.Clear();
            tChart1.Zoom.Undo();
            InitAxis();
            tChart1.Panel.MarginLeft = 10;
            tChart1.Panel.MarginUnits = Steema.TeeChart.PanelMarginUnits.Percent;
            TChartDataLoader loader = new TChartDataLoader(this.tChart1);
            for (int i = 1; i < sz; i += 1)
            {
                try
                {
                    string columnName = hydrometDataTable.Columns[i].ColumnName;
                    Steema.TeeChart.Styles.Line series = loader.CreateSeries(hydrometDataTable, columnName, Reclamation.TimeSeries.TimeInterval.Daily, true);

                    series.VertAxis = Steema.TeeChart.Styles.VerticalAxis.Left;
                    series.Pointer.Visible = true;

                    var tokens = TextFile.Split(columnName);
                    string pcode = "";
                    string cbtt = "";
                    if (tokens.Length == 2)
                    {
                        cbtt = tokens[0].Trim();
                        pcode = tokens[1].Trim();
                    }

                    SetAxis(series, pcode);

                    tChart1.Series.Add(series);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString() + " series index " + i);
                    Logger.WriteLine(ex.ToString(), "ui");
                }
            }

            tChart1.Axes.Left.Automatic = true;
        }

        private Steema.TeeChart.Axis shift;
        private Steema.TeeChart.Axis flow;
        private Steema.TeeChart.Axis stage;

        private void InitAxis()
        {
            tChart1.Axes.Custom.RemoveAll();
            tChart1.Panel.MarginLeft = 3;
            tChart1.Axes.Left.Title.Text = "";
            tChart1.Axes.Right.Title.Text = "";


            flow = AddCustomAxis("flow - cfs", 0, 30);
            stage = AddCustomAxis("stage - feet",35,60);
            shift = AddCustomAxis("shift - feet", 76, 100);

        }

        private Steema.TeeChart.Axis AddCustomAxis(string name, int start, int end)
        {
            var a = new Steema.TeeChart.Axis();
            tChart1.Axes.Custom.Add(a);


            //stageAxis.RelativePosition = -10;
            //stageAxis.StartEndPositionUnits = Steema.TeeChart.PositionUnits.Percent;
            a.StartPosition = start;
            a.EndPosition = end;
            a.Grid.Visible = true;
            a.Title.Angle = 90;
            a.Title.Text = name;
            return a;
        }

        private void SetAxis(Line series, string pcode)
        {
            var cn = pcode.ToLower().Trim();

            if (cn == "qd" || cn == "qj") // flow
                series.CustomVertAxis = flow;
            else if (cn == "hh" || cn == "hj")
            { //  shift 
                series.CustomVertAxis = shift;
            }
            else
                series.CustomVertAxis = stage;
        }

        

        private void linkLabelCompute_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void linkLabelGraphOption_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Steema.TeeChart.Editor.Show(tChart1);
        }

    }
}
