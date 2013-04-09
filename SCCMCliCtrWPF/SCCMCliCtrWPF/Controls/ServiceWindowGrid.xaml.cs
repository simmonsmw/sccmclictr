﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using System.Diagnostics;
using sccmclictr.automation;
using sccmclictr.automation.functions;
using sccmclictr.automation.schedule;

namespace ClientCenter.Controls
{
    /// <summary>
    /// Interaction logic for ServiceWindowGrid.xaml
    /// </summary>
    public partial class ServiceWindowGrid : UserControl
    {
        private SCCMAgent oAgent;
        public MyTraceListener Listener;
        public event EventHandler RequestRefresh;

        public ServiceWindowGrid()
        {
            InitializeComponent();
            scheduleControl1.DeleteClick += scheduleControl1_DeleteClick;
        }

        void scheduleControl1_DeleteClick(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(CloseButton))
            {
                string SWindowID = ((CloseButton)sender).ID;
                if (oAgent.isConnected)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        oAgent.Client.RequestedConfig.DeleteServiceWindow(SWindowID);
                        if (this.RequestRefresh != null)
                            this.RequestRefresh(sender, e);

                    }
                    catch (Exception ex)
                    {
                        ex.Message.ToString();
                    }
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
                
            }

            try
            {
                if (oAgent.isConnected)
                {
                    scheduleControl1.ScheduledTimes.Clear();
                    SCCMAgentConnection = oAgent;
                    scheduleControl1.Refresh();
                }
            }
            catch(Exception ex)
            {
                ex.Message.ToString();
            }
        }

        public SCCMAgent SCCMAgentConnection
        {
            get
            {
                return oAgent;
            }
            set
            {
                if (value.isConnected)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        oAgent = value;
                        scheduleControl1.DaysVisible = 7;
                        if (oAgent.isConnected)
                        {
                            scheduleControl1.ScheduledTimes.Clear();
                            foreach (sccmclictr.automation.policy.requestedConfig.CCM_ServiceWindow oSRW in oAgent.Client.RequestedConfig.ServiceWindow)
                            {
                                GetSchedules(oSRW.DecodedSchedule, oSRW.ServiceWindowID, oSRW.PolicySource);
                            }
                        }

                    }
                    catch(Exception ex)
                    {
                        ex.Message.ToString();
                    }
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }
        }

        public void GetSchedules(object Schedule)
        {
            GetSchedules(Schedule, "", "");
        }
        public void GetSchedules(object Schedule, string ServiceWindowID)
        {
            GetSchedules(Schedule, ServiceWindowID, "");
        }
        public void GetSchedules(object Schedule, string ServiceWindowID, string PolicySource)
        {
            Boolean isLocal = false;
            if(string.Compare(PolicySource, "LOCAL", true) == 0)
                isLocal = true;

            var oWin = Schedule;
            switch (oWin.GetType().Name)
            {
                case ("List`1"):
                    foreach (var subsched in oWin as List<object>)
                    {
                        GetSchedules(subsched, ServiceWindowID, PolicySource);
                    }
                    break;
                case ("SMS_ST_NonRecurring"):
                     ScheduleDecoding.SMS_ST_NonRecurring oSchedNonRec = ((ScheduleDecoding.SMS_ST_NonRecurring)oWin);

                    string sDayNonRec = new DateTime(2012, 7, oSchedNonRec.StartTime.Day).DayOfWeek.ToString();
                    //DateTime dNextStartTime = oSchedNonRec.NextStartTime;
                    DateTime dNextRunNonRec = oSchedNonRec.NextStartTime;
                    if (oSchedNonRec.StartTime + new TimeSpan(oSchedNonRec.DayDuration, oSchedNonRec.HourDuration, 0, 0) >= DateTime.Now.Date)
                    {
                        ScheduleControl.ScheduledTime oControl = new ScheduleControl.ScheduledTime(dNextRunNonRec, new TimeSpan(oSchedNonRec.DayDuration, oSchedNonRec.HourDuration, oSchedNonRec.MinuteDuration, 0), Colors.Blue, "Non Recuring", isLocal, ServiceWindowID);
                        scheduleControl1.ScheduledTimes.Add(oControl);
                    }
                    break;
                case ("SMS_ST_RecurInterval"):
                    ScheduleDecoding.SMS_ST_RecurInterval oSchedInt = ((ScheduleDecoding.SMS_ST_RecurInterval)oWin);
                    DateTime dNextStartTimeInt = oSchedInt.NextStartTime;
                    DateTime dNextRunInt = dNextStartTimeInt;

                    string sRecurTextInt = string.Format("Occours Every ({0})Day(s)", oSchedInt.DaySpan);
                    if(oSchedInt.DaySpan == 0)
                        sRecurTextInt = string.Format("Occours Every ({0})Hour(s)", oSchedInt.HourSpan);

                    //Check if there is a schedule today... (past)
                    if (oSchedInt.PreviousStartTime.Date == DateTime.Now.Date)
                        dNextRunInt = oSchedInt.PreviousStartTime;
                    while (dNextRunInt.Date < DateTime.Now.Date + new TimeSpan(scheduleControl1.DaysVisible, 0, 0, 0))
                    {
                        scheduleControl1.ScheduledTimes.Add(new ScheduleControl.ScheduledTime(dNextRunInt, new TimeSpan(oSchedInt.DayDuration, oSchedInt.HourDuration, oSchedInt.MinuteDuration, 0), Colors.Green, sRecurTextInt + " " + ServiceWindowID, isLocal, ServiceWindowID));
                        dNextRunInt = dNextRunInt + new TimeSpan(oSchedInt.DaySpan, oSchedInt.HourSpan, oSchedInt.MinuteSpan, 0);
                    }
                    break;
                case ("SMS_ST_RecurWeekly"):
                    ScheduleDecoding.SMS_ST_RecurWeekly oSched = ((ScheduleDecoding.SMS_ST_RecurWeekly)oWin);

                    string sDay = new DateTime(2012,7, oSched.Day).DayOfWeek.ToString();
                    DateTime dNextStartTime = oSched.NextStartTime;
                    string sRecurText = string.Format("Occours Every ({0})weeks on {1} {2}", oSched.ForNumberOfWeeks, sDay, ServiceWindowID);
                    DateTime dNextRun = dNextStartTime;

                    //Check if there is a schedule today... (past)
                    if (oSched.PreviousStartTime.Date == DateTime.Now.Date)
                        dNextRun = oSched.PreviousStartTime;

                    while (dNextRun.Date < DateTime.Now.Date + new TimeSpan(scheduleControl1.DaysVisible, 0, 0, 0))
                    {
                        scheduleControl1.ScheduledTimes.Add(new ScheduleControl.ScheduledTime(dNextRun, new TimeSpan(oSched.DayDuration, oSched.HourDuration, oSched.MinuteDuration, 0), Colors.Red, sRecurText, isLocal, ServiceWindowID));
                        //add_Appointment(dNextRun, dNextRun + new TimeSpan(oSchedule.DayDuration, oSchedule.HourDuration, oSchedule.MinuteDuration, 0), oSchedule.IsGMT);
                        dNextRun = dNextRun + new TimeSpan(oSched.ForNumberOfWeeks * 7, 0, 0, 0);
                    }
                    break;
                case ("SMS_ST_RecurMonthlyByWeekday"):
                    break;
                case ("SMS_ST_RecurMonthlyByDate"):
                    break;
            }
        }

        private void bt_Reload_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                scheduleControl1_DeleteClick(sender, null);
            }
            catch { }
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}