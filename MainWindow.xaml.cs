using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Threading;

namespace LightningZoom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region window size management
        // because window size should be based on content not whatever theme happens to be running
        private int windowMarginX;
        private int windowMarginY;
        private bool windowMarginInitialized;
        public int RealHeight {
            get { return (int)(this.Height - windowMarginY); }
            set { this.Height = value + windowMarginY; }
        }
        public int RealWidth {
            get { return (int)(this.Width - windowMarginX); }
            set { this.Width = value + windowMarginX; }
        }
        private void grdWrapper_LayoutUpdated(object sender, EventArgs e) {
            if(!windowMarginInitialized) {
                windowMarginInitialized = true;
                windowMarginX = (int)(this.Width - this.grdWrapper.ActualWidth);
                windowMarginY = (int)(this.Height - this.grdWrapper.ActualHeight);
                RealWidth = (int)this.Width;
                RealHeight = (int)this.Height;
            }
        }
        #endregion


        private string saveFilePath;
        private string executablePath;
        //System.Diagnostics.Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Zoom\bin\Zoom.exe", "\"--url=zoommtg://zoom.us/join?action=join&confno=123456789&pwd=\"");

        private bool saveChanges;
        private bool active;
        private System.Windows.Forms.NotifyIcon notify;

        private List<Event> events;
        private Event current { get {
                if(list.SelectedIndex != -1 && list.SelectedIndex < events.Count) return events[list.SelectedIndex];
                else return null;
            }
        }

        public MainWindow() {
            InitializeComponent();
            // the part that makes this all work!
            executablePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Zoom\bin\Zoom.exe";
            saveFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\LightningZoom\locations.txt";
            if(!System.IO.File.Exists(executablePath)) {
                System.Windows.MessageBox.Show("LightningZoom was unable to find your installation of Zoom\r\nVerify that it is in the correct location:\r\n"+executablePath, "Cannot Find Zoom", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            // window properties
            RealHeight = 245;
            RealWidth = 430;

            // configure notify icon
            active = false;
            notify = new System.Windows.Forms.NotifyIcon();
            notify.Click += Notify_Click;
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/LightningZoom;component/lightning_zoom.ico")).Stream;
            notify.Icon = new System.Drawing.Icon(iconStream);
            notify.Text = "Lightning Zoom";

            this.Closing += MainWindow_Closing;

            events = new List<Event>();
            loadFromFile();
            DailyAutoCheck();
            if(chkStartHidden.IsChecked == true) BtnHide_Click(null, null);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if(events != null) {
                if(saveChanges) {
                    MessageBoxResult result;
                    if(chkSaveClose.IsChecked == false) result = MessageBox.Show("There are unsaved changes to the configuration.\r\nWould you like to save before closing?",
                        "LightingZoom", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                    else result = MessageBoxResult.Yes;
                    if(result == MessageBoxResult.Yes) BtnSave_Click(null, null);
                    else if(result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
                }
                for(int i = 0; i < events.Count(); i++) {
                    if(events[i].autoRun != null) {
                        events[i].canceller.Cancel();
                        if(events[i].autoRun.IsCompleted) events[i].autoRun.Dispose();
                    }
                }
            }
            if(notify != null) notify.Visible = false;
        }

        private void Notify_Click(object sender, EventArgs e) {
            if(e is System.Windows.Forms.MouseEventArgs) {
                if(((System.Windows.Forms.MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Left) {
                    this.Show();
                    active = true;
                    notify.Visible = false;
                }
                else BtnQuickLanch_Click(null, null);
            }
        }
        
        private void BtnHide_Click(object sender, RoutedEventArgs e) {
            this.Hide();
            active = false;
            notify.Visible = true;
        }

        private void BtnQuickLanch_Click(object sender, RoutedEventArgs e) {
            int next = FindNextEvent();
            if(next == -1) MessageBox.Show("No events for today", "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Error);
            else if(next == -2) MessageBox.Show("Click on the edit tab to define an event", "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Error);
            else System.Diagnostics.Process.Start(executablePath, events[next].args);

            if(sender != null && chkLH.IsChecked == true) BtnHide_Click(null, null);
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e) {
            if(current != null) System.Diagnostics.Process.Start(executablePath, current.args);
            else MessageBox.Show("Click on an event in the list\r\n or the edit tab to define a new one", "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Error);

            if(chkLH.IsChecked == true) BtnHide_Click(null, null);
        }

        /// <summary>
        /// Finds the next event based on the start time
        /// </summary>
        /// <returns>index of next event, -2 if empty, -1 if none for today</returns>
        private int FindNextEvent() {
            if(events.Count == 0) return -2;

            int best = -1;
            TimeSpan tillB= TimeSpan.FromSeconds(0);
            for(int i= events.Count - 1; i>=0; i--) if(events[i].IsToday()) {
                    TimeSpan tillI = events[i].time.ToDateTime() - DateTime.Now;
                    if(tillI.TotalMinutes < 0 && !events[i].endtime.static_ && DateTime.Now < events[i].endtime.ToDateTime()) return i;
                    else if(best == -1) { best = i; tillB = tillI; }
                    else if(tillI.TotalMinutes > 0 && tillI < tillB) { best = i; tillB = tillI; }
                }

            return best;
        }

        #region auto launching

        private void CheckAutoLaunch() {
            for(int i=0; i<events.Count(); i++) {
                if(events[i].autostart && events[i].IsToday() && !events[i].time.static_ && events[i].autoRun == null) {
                    TimeSpan time = events[i].time.ToDateTime() - TimeSpan.FromMinutes(events[i].autoTime) - DateTime.Now;
                    if(time.TotalSeconds > 0) ScheduleAutoLaunch(events[i], time);
                }
            }
        }

        private void ScheduleAutoLaunch(Event e, TimeSpan delay) {
            e.canceller = new CancellationTokenSource();
            e.autoRun = Task.Delay(delay, e.canceller.Token).ContinueWith((x) => AutoLaunch(e, x));
            e.autoRunTime = new Time((DateTime.Now + delay).Hour, (DateTime.Now + delay).Minute);
        }

        private void AutoLaunch(Event e, Task t) {
            if(t.IsCanceled) return;
            TimeSpan s = e.autoRunTime.ToDateTime() - DateTime.Now;
            if((s.TotalSeconds < 2 && s.TotalSeconds > -20) ||
                MessageBox.Show("Event " + e.name + " passed auto start time.\r\nWould you like to start anyway?",
                "LightningZoom", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                System.Diagnostics.Process.Start(executablePath, e.args);
            }
            if(!e.repeat) {
                e.days[(int)DateTime.Now.DayOfWeek] = false;
                saveChanges = true;
            }
            //if(e.autoRun != null) e.autoRun.Dispose();
        }

        private void DailyAutoCheck() {
            CheckAutoLaunch();
            Task.Delay(new Time(1,0).ToDateTime().AddDays(1) - DateTime.Now).ContinueWith(x=>DailyAutoCheck());
        }
#endregion

        #region edit ui
        private void NumbersOnly(object sender, TextCompositionEventArgs e) {
            for(int i = 0; i < e.Text.Length; i++) if(!"0123456789".Contains(e.Text[i]) || i > 2) { e.Handled = true; return; }
        }
        
        private void TimeOnly(object sender, RoutedEventArgs e) {
            TextBox txt = (TextBox)sender;
            if(!Time.CheckFormat(txt.Text)) txt.BorderBrush = Brushes.Red;
            else txt.BorderBrush = Brushes.LightGray;
        }

        private void ChkPassCode_Checked(object sender, RoutedEventArgs e) {
            txtPasscode.IsEnabled = true;
        }

        private void ChkAutoStart_Checked(object sender, RoutedEventArgs e) {
            txtAutoStart.IsEnabled = true;
            if(txtAutoStart.Text == "0") txtAutoStart.Text = "5";
        }

        private void ChkAutoStart_Unchecked(object sender, RoutedEventArgs e) {
            txtAutoStart.IsEnabled = false;
            txtAutoStart.Text = "0";
        }

        private void ChkPassCode_Unchecked(object sender, RoutedEventArgs e) {
            txtPasscode.IsEnabled = false;
        }

        bool denyList_SelectionChanged = false;
        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(!denyList_SelectionChanged) {
                int i = list.SelectedIndex;
                Event ev = current;
                if(current != null) {
                    txtName.Text = ev.name;
                    txtAutoStart.Text = ev.autoTime.ToString();
                    txtPasscode.Text = ev.pass;
                    txtStartTime.Text = ev.time.ToString();
                    txtEndTime.Text = ev.endtime.ToString();
                    txtZoomCode.Text = ev.code;
                    chkAutoStart.IsChecked = ev.autostart;
                    chkRepeat.IsChecked = ev.repeat;
                    chkPassCode.IsChecked = ev.requirePass;
                    chkSun.IsChecked = ev.days[0];
                    chkMon.IsChecked = ev.days[1];
                    chkTue.IsChecked = ev.days[2];
                    chkWed.IsChecked = ev.days[3];
                    chkThr.IsChecked = ev.days[4];
                    chkFri.IsChecked = ev.days[5];
                    chkSat.IsChecked = ev.days[6];
                }
            }
            if(current == null) lblAuto.Content = "No event is selected";
            else if(!current.autostart) lblAuto.Content = "This event is not set to auto start";
            else if(!current.IsToday()) lblAuto.Content = "This event is not set for today";
            else if(current.time.ToDateTime() < DateTime.Now) lblAuto.Content = "This event is already passed";
            else if(current.autoRun == null) lblAuto.Content = "This event is not scheduled to run";
            else lblAuto.Content = "This event will start at " + current.autoRunTime;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e) {
            if(current == null) { BtnAdd_Click(sender, e); return; }
            if(!validate()) return;

            Time t; Time.TryParse(txtStartTime.Text, out t);
            Time et; Time.TryParse(txtEndTime.Text, out et);
            bool[] days = new bool[7];
            days[0] = chkSun.IsChecked == true; days[1] = chkMon.IsChecked == true; days[2] = chkTue.IsChecked == true; days[3] = chkWed.IsChecked == true;
            days[4] = chkThr.IsChecked == true; days[5] = chkFri.IsChecked == true; days[6] = chkSat.IsChecked == true;

            current.Update(txtName.Text, t, et, chkRepeat.IsChecked == true, days, chkAutoStart.IsChecked == true,
                int.Parse(txtAutoStart.Text), txtZoomCode.Text, chkPassCode.IsChecked == true, txtPasscode.Text);
            ((ListBoxItem)list.SelectedItem).Content = current.ToString();

            if(current.autoRun != null) {
                current.canceller.Cancel();
                //current.autoRun.Dispose();
                current.autoRun = null;
            }
            CheckAutoLaunch();
            denyList_SelectionChanged = true;
            List_SelectionChanged(null, null);
            denyList_SelectionChanged = false;

            btnSave.BorderBrush = Brushes.Green;
            saveChanges = true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) {
            if(!validate()) return;

            Time t; Time.TryParse(txtStartTime.Text, out t);
            Time et; Time.TryParse(txtEndTime.Text, out et);
            bool[] days = new bool[7];
            days[0] = chkSun.IsChecked == true; days[1] = chkMon.IsChecked == true; days[2] = chkTue.IsChecked == true; days[3] = chkWed.IsChecked == true;
            days[4] = chkThr.IsChecked == true; days[5] = chkFri.IsChecked == true; days[6] = chkSat.IsChecked == true;

            Event ev = new Event(txtName.Text, t, et, chkRepeat.IsChecked == true, days, chkAutoStart.IsChecked == true,
                int.Parse(txtAutoStart.Text), txtZoomCode.Text, chkPassCode.IsChecked == true, txtPasscode.Text);
            events.Add(ev);
            list.Items.Add(new ListBoxItem() { Content=ev.ToString() });
            denyList_SelectionChanged = true;
            list.SelectedIndex = list.Items.Count-1;
            denyList_SelectionChanged = false;

            CheckAutoLaunch();

            btnSave.BorderBrush = Brushes.Green;
            saveChanges = true;
        }

        private bool validate() {
            string msg = ""; int a;
            if(chkAutoStart.IsChecked == false) txtAutoStart.Text = "0";
            if(txtName.Text.Contains("\n")) { msg = "Name field has illegal characters (return)"; txtName.Focus(); } 
            else if(!int.TryParse(txtAutoStart.Text, out a)) { msg = "Auto start time field did not evaluate to an integer"; txtAutoStart.Focus(); }
            else if(!Time.CheckFormat(txtStartTime.Text)) { msg = "Start time field did not evaluate to a valid time"; txtStartTime.Focus(); }
            else if(!Time.CheckFormat(txtEndTime.Text)) { msg = "End time field did not evaluate to a valid time"; txtEndTime.Focus(); }
            else if(txtStartTime.Text == "" && chkAutoStart.IsChecked == true) { msg = "Cannot auto start at an unknown time"; txtStartTime.Focus(); }

            lblError.Content = msg;
            return string.IsNullOrEmpty(msg);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e) {
            int i = list.SelectedIndex;
            if(i < 0 || i >= events.Count) return;

            if(events[i].autoRun != null) {
                events[i].canceller.Cancel();
                events[i].autoRun.Dispose();
            }

            list.Items.RemoveAt(i);
            events.RemoveAt(i);
            if(i >= events.Count) list.SelectedIndex = i - 1;
            else list.SelectedIndex = i;
            
            btnSave.BorderBrush = Brushes.Green;
            saveChanges = true;
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            int dir = (int)e.NewValue;
            ((System.Windows.Controls.Primitives.ScrollBar)sender).Value = 0;
            
            if(current != null) {
                int i = list.SelectedIndex;
                if(!(i == 0 && dir == -1) && !(i == events.Count - 1 && dir == 1)) {
                    Event ev = events[i];
                    events.RemoveAt(i);
                    events.Insert(i+dir, ev);

                    object item = list.Items[i];
                    list.Items.RemoveAt(i);
                    list.Items.Insert(i+dir, item);

                    denyList_SelectionChanged = true;
                    list.SelectedIndex = i + dir;
                    denyList_SelectionChanged = false;
                }
            }

            saveChanges = true;
            btnSave.BorderBrush = Brushes.Green;
        }
#endregion

        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            if(!Directory.Exists(saveFilePath.Substring(0, saveFilePath.LastIndexOf('\\')))) Directory.CreateDirectory(saveFilePath.Substring(0, saveFilePath.LastIndexOf('\\')));
            string file = "LightningZoom save file version 1.0\r\nstartHidden="+(chkStartHidden.IsChecked==true)+
                "\r\nautoSaveOnClose=" + (chkSaveClose.IsChecked == true) +
                "\r\nlaunchAndHide=" + (chkLH.IsChecked == true)+"\r\n";

            for(int i = 0; i < events.Count; i++) file += "\r\n" + events[i].Serialize();

            File.WriteAllText(saveFilePath, file);

            btnSave.BorderBrush = Brushes.Gray;
            saveChanges = false;
        }

        private void loadFromFile(string path = null) {
            if(path == null) path = saveFilePath;
            if(File.Exists(path)) {
                try {
                    string[] data = File.ReadAllText(path).Split(new string[] { "\r\n" }, 6, StringSplitOptions.None);
                    for(int i=1; i<4; i++) {
                        int ind = data[i].IndexOf('=');
                        if(ind == -1 || ind + 1 > data[i].Length) throw new Exception("Line " + i + " in save file had issue with token '='");
                        data[i] = data[i].Substring(ind + 1);
                    }
                    chkStartHidden.IsChecked = data[1] == "True";
                    chkSaveClose.IsChecked = data[2] == "True";
                    chkLH.IsChecked = data[3] == "True";
                    events = Event.DeserializeMany(data[5]).ToList();
                }
                catch(Exception e) {
                    MessageBox.Show("Unable to load from save file. See help if needed.\r\n" + e.Message + (e.InnerException != null ? "\r\n" + e.InnerException.Message : ""), "LightningZoom");
                    return;
                }
                list.Items.Clear();
                for(int i = 0; i < events.Count; i++) list.Items.Add(new ListBoxItem() { Content = events[i].ToString() });
            }
        }

        private void BtnOpenSaveFolder_Click(object sender, RoutedEventArgs e) {
            string path = saveFilePath.Substring(0, saveFilePath.LastIndexOf('\\'));
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("LightingZoom v1.0 Updated 3/20/2021"+
                "\r\n\r\nCopyright 2021 Jonathan Rabideau\r\nunder GNU General Public License" +
                "\r\n\r\nLightningZoom is a utility intended\r\nto redesign the Zoom interface.\r\nIt allows management of external"+
                "\r\nmeetings and can launch them\r\nin one click or fully hands free.",
                "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnHiding_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("LightningZoom can hide in your\r\nnotification tray, next to\r\nvolume, internet, and the like."+
                "\r\n\r\nWhen hidden, left click the icon\r\nto show LightningZoom or right click\r\nto quick launch your next meeting."+
                "\r\nYou can hide on launch or\r\nhide on startup by ticking\r\nthe checkboxes on the Main tab.",
                "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEditing_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("To setup, click on the Edit tab. Enter meeting" +
                "\r\ndata such as name, start/end time, whether you" +
                "\r\nwant LightningZoom to automatically launch it" +
                "\r\nfor you without even having to press buttons," +
                "\r\nthe room code, and the passcode if it is" +
                "\r\nrequired (NOTE: the passcode needs to be hashed" +
                "\r\nfirst and at time of writing further research" +
                "\r\nis required, but it is something along the lines" +
                "\r\nof getting a link from Zoom first and taking it" +
                "\r\nout of the browser). Also what days the meeting" +
                "\r\nwill take place on and whether it repeats, see" +
                "\r\nsection on auto launching.",
                "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void BtnAutoLaunching_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("If you want LightningZoom to auto start your" +
                "\r\nmeetings for you, tick the Autostart checkbox." +
                "\r\nYou can set how many minutes ahead of time you" +
                "\r\nwant LightningZoom to start the meeting. It" +
                "\r\nwill only auto start on days you have selected" +
                "\r\nand if repeat is not ticked LightningZoom will" +
                "\r\nuntick the day the meeting was started on so it" +
                "\r\nwon’t automatically start again. To view which" +
                "\r\nevents are set to auto start for the day, click" +
                "\r\non the Info tab and select the meeting in" +
                "\r\nquestion in the list and the desired" +
                "\r\ninformation will appear on screen." +
                "\r\n\r\nTo launch LightningZoom automatically on"+
                "\r\nstartup, place this exe in your startups\r\n"+
                @"\r\nfolder, located at C:\ProgramData\Microsoft\"+
                "\r\nWindows\\Start Menu\\Programs\\Startup",
                "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaving_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("For changes to take effect Update or Add" +
                "\r\nmust be clicked, and for changes to persist" +
                "\r\nacross application closing/opening Save" +
                "\r\nmust be clicked. To view save location," +
                "\r\n click on the Info tab and then the" +
                "\r\nconveniently placed button. The save" +
                "\r\nbutton will outline in green when there" +
                "\r\nare changes to save. If LightningZoom is" +
                "\r\nclosed when there are changes to save," +
                "\r\nyou will be prompted if you want to save." +
                "\r\nAlternatively you can tick the save on" +
                "\r\nclose checkbox on the Main tab to" +
                "\r\nautomatically save on close.",
                "LightningZoom", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGithub_Click(object sender, RoutedEventArgs e) {
            if(MessageBox.Show("LightningZoom Repository:\r\n"
                + @"https://github.com/Jonathan-Rabideau/LightningZoom"+
                "\r\n\r\nWould you like to copy to clipboard?",
                "LightningZoom", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                Clipboard.SetText(@"https://github.com/Jonathan-Rabideau/LightningZoom");
            }
        }
    }
}
