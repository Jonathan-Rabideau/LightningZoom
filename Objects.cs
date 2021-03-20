using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightningZoom
{
    public class Event
    {
        public const int TOTAL_VALUES= 10;

        public string name;
        public Time time;
        public Time endtime;
        public bool repeat;
        public bool[] days;
        public bool autostart;
        public int autoTime;
        public string code;
        public bool requirePass;
        public string pass;

        public string args;
        public Task autoRun;
        public CancellationTokenSource canceller;
        public Time autoRunTime;

        public Event(string name, Time time, Time endtime, bool repeat, bool[] days, bool autostart, int autoTime, string code, bool requirePass, string pass) {
            this.name = name; this.time = time; this.endtime = endtime; this.repeat = repeat; this.days = days; this.autostart = autostart;
            this.autoTime = autoTime; this.code = code; this.requirePass = requirePass; this.pass = pass;
            args = "\"--url=zoommtg://zoom.us/join?action=join&confno="+code+(requirePass ? "&pwd="+pass+"\"" : "\"");
        }
        public void Update(string name, Time time, Time endtime, bool repeat, bool[] days, bool autostart, int autoTime, string code, bool requirePass, string pass) {
            this.name = name; this.time = time; this.endtime = endtime; this.repeat = repeat; this.days = days; this.autostart = autostart;
            this.autoTime = autoTime; this.code = code; this.requirePass = requirePass; this.pass = pass;
            args = "\"--url=zoommtg://zoom.us/join?action=join&confno=" + code + (requirePass ? "&pwd=" + pass + "\"" : "\"");
        }


        public bool IsToday() {
            int today = (int)DateTime.Now.DayOfWeek;
            return days[today];
        }

        public string Serialize() {
            string daysS = "";
            for(int i = 0; i < 7; i++) if(days[i]) daysS += "1"; else daysS += "0";
            return "name=" + name + "\r\ntime=" + time + "\r\nendtime=" + endtime + "\r\nrepeat=" + repeat + "\r\ndays=" + daysS + "\r\nautostart=" + autostart
                + "\r\nautoTime=" + autoTime + "\r\ncode=" + code + "\r\nrequirePass=" + requirePass + "\r\npass=" + pass;
            // 10 lines each
        }

        public override string ToString() {
            return name + "  " + time;
        }

        private static Event DeserializeOne(string[] data) {
            Time t; if(!Time.TryParse(data[1], out t)) throw new ArgumentException("Line 1 (time) of object \"" + data[0] + "\" was in an incorrect format", "str");
            Time et; if(!Time.TryParse(data[2], out et)) throw new ArgumentException("Line 2 (endtime) of object \"" + data[0] + "\" was in an incorrect format", "str");
            bool[] days = new bool[7];
            for(int i = 0; i < 7; i++) days[i] = data[4][i] == '1';
            return new Event(data[0], t, et, data[3] == "True", days, data[5] == "True", int.Parse(data[6]), data[7], data[8] == "True", data[9]);
        }
        public static Event Deserialize(string str) {
            try {
                string[] data = str.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for(int i = 0; i < data.Length; i++) {
                    int ind = data[i].IndexOf('=');
                    if(ind == -1 || ind + 1 > data[i].Length) throw new ArgumentException("Line " + i + " in object \"" + data[0] + "\" had issue with token '='", "str");
                    data[i] = data[i].Substring(ind + 1);
                }
                return DeserializeOne(data);
            }
            catch(Exception e) {
                if(e is ArgumentException) throw e;
                throw new Exception("Error in deserializing data", e);
            }
        }
        public static Event[] DeserializeMany(string str) {
            try {
                string[] data = str.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for(int i = 0; i < data.Length; i++) {
                    int ind = data[i].IndexOf('=');
                    if(ind == -1 || ind + 1 > data[i].Length) throw new ArgumentException("Line " + i + " in object \"" + data[0] + "\" had issue with token '='", "str");
                    data[i] = data[i].Substring(ind + 1);
                }
                Event[] events = new Event[data.Length / TOTAL_VALUES];
                int j = 0;
                while(data.Length >= TOTAL_VALUES) {
                    events[j] = DeserializeOne(data);
                    data = (string[])data.Skip(TOTAL_VALUES).ToArray();
                    j++;
                }
                return events;
            }
            catch(Exception e) {
                if(e is ArgumentException || e.Message.StartsWith("Error in des")) throw e;
                throw new Exception("Error in deserializing data", e);
            }
        }
        
    }

    public class Time {
        public bool static_;
        private bool army;
        private int hours;
        private int minutes;

        private Time() { army = false; hours = 0; minutes = 0; }
        public Time(int hours, int minutes) { army = false; this.hours = hours; this.minutes = minutes; }

        public static bool CheckFormat(string str) {
            Time t;
            return TryParse(str, out t);
        }
        public static bool TryParse(string str, out Time t) {
            if(str == "") { t = new Time(); t.static_ = true; return true; }
            t = null;
            if(str.Contains("-") || str.Length > 10) return false;
            int s;
            if(int.TryParse(str, out s) && s >= 100) {
                t = new Time();
                t.army = true;
                t.hours = s / 100;
                t.minutes = s % 100;
            }
            else {
                t = new Time();
                if(str.EndsWith("pm")) t.hours = 12;
                string[] st = str.Replace(" ", "").Replace("pm", "").Replace("am", "").Split(':');
                if(st.Length == 0 || st.Length > 2) return false;
                if(!int.TryParse(st[0], out s)) return false;
                if(s != 12) t.hours += s;
                if(st.Length == 2) {
                    if(!int.TryParse(st[1], out s)) return false;
                    t.minutes = s;
                }
            }
            if(t.hours >= 24 || t.minutes >= 60) return false;
            return true;
        }
        public static Time Parse(string str) {
            Time t; TryParse(str, out t); return t;
        }

        public DateTime ToDateTime() {
            DateTime t = DateTime.Now;
            return t.AddMilliseconds(-t.Millisecond).AddSeconds(-t.Second).AddMinutes(minutes - t.Minute).AddHours(hours - t.Hour);
        }
        public DateTime ToDateTime(int day) {
            DateTime t = DateTime.Now;
            int tday = (int)t.DayOfWeek;
            return t.AddMilliseconds(-t.Millisecond).AddSeconds(-t.Second).AddMinutes(minutes - t.Minute).AddHours(hours - t.Hour).AddDays(day - tday);
        }

        public override string ToString() {
            if(static_) return "";
            string r;
            if(!army) {
                if(hours > 12) r = (hours - 12).ToString();
                else if(hours == 0) r = "12";
                else r = hours.ToString();
                if(minutes != 0) r += ":" + minutes;
                if(hours >= 12) r += "pm";
                else r += "am";
            }
            else {
                r = hours.ToString();
                if(minutes < 10) r += "0" + minutes;
                else r += minutes;
            }
            return r;
        }
    }

}
