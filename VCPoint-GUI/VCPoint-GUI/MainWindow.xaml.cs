using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;

namespace VCPoint_GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public class OldWindow : System.Windows.Forms.IWin32Window
    {
        IntPtr _handle;
        public OldWindow(IntPtr handle)
        {
            _handle = handle;
        }

        #region IWin32Window Members
        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get { return _handle; }
        }
        #endregion
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public static string getwebcode1(string url, string encoder)
        {
            WebClient myWebClient = new WebClient();
            byte[] myDataBuffer = myWebClient.DownloadData(url);
            string SourceCode = Encoding.GetEncoding(encoder).GetString(myDataBuffer);
            return SourceCode;
        }
        private void Button_calc_Click(object sender, RoutedEventArgs e)
        {
            string st = av.Text;
            string wb = "https://api.bilibili.com/x/web-interface/view?aid=" + st;
            if (bv_used.IsChecked == true)
            {
                wb = "https://api.bilibili.com/x/web-interface/view?bvid=" + st;
            }
            string wbc;
            try
            {
                wbc = getwebcode1(wb, "utf-8");
            }
            catch
            {
                MessageBox.Show("输入的av号有误！", "计算失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (wbc.Length<20)
            {
                MessageBox.Show("输入的av号有误！", "计算失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            JObject jo = JObject.Parse(wbc);
            string aid, bvid;
            if (bv_used.IsChecked == true)
            {
                bvid = st;
                string ttmp;
                ttmp = jo["data"]["aid"].ToString();
                aid = ttmp;
            }
            else
            {
                aid = st;
                string ttmp;
                ttmp = jo["data"]["bvid"].ToString();
                bvid = ttmp;
            }
            string stmp;
            stmp = jo["data"]["stat"]["danmaku"].ToString();
            int danmaku = int.Parse(stmp);
            stmp = jo["data"]["stat"]["view"].ToString();
            int view = int.Parse(stmp);
            stmp = jo["data"]["stat"]["reply"].ToString();
            int reply = int.Parse(stmp);
            stmp = jo["data"]["stat"]["favorite"].ToString();
            int favorite = int.Parse(stmp);
            stmp = jo["data"]["stat"]["like"].ToString();
            int like = int.Parse(stmp);
            stmp = jo["data"]["stat"]["coin"].ToString();
            int coin = int.Parse(stmp);
            long UnixTime = long.Parse(jo["data"]["pubdate"].ToString());
            string up = jo["data"]["owner"]["name"].ToString();
            string title = jo["data"]["title"].ToString();
            var pubTime = DateTimeOffset.FromUnixTimeSeconds(UnixTime);
            var pubTimeUTC = pubTime.UtcDateTime;
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            var pubTimeCST = TimeZoneInfo.ConvertTimeFromUtc(pubTimeUTC, cstZone);
            string pubt = String.Format("{0}", pubTimeCST);

            var NowTimeUTC = DateTime.UtcNow;
            var NowTimeCST = TimeZoneInfo.ConvertTimeFromUtc(NowTimeUTC, cstZone);
            int DayDiff = (Convert.ToInt32(NowTimeCST.DayOfWeek) + 1) % 7;
            var TargetDay = NowTimeCST.AddDays(-DayDiff);
            var TargetTime = new DateTime(TargetDay.Year,
                TargetDay.Month, TargetDay.Day, 3, 0, 0);
            bool GetLastWeek = false;
            string datat = "";
            if (DateTime.Compare(pubTimeCST, TargetTime) < 0)
            {
                MessageBoxResult las = MessageBox.Show("是否需要考虑上周数据？",
                    "上周数据", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (las == MessageBoxResult.Yes)
                {
                    GetLastWeek = true;
                    DateTimeOffset TargetTimeUTC = TimeZoneInfo.ConvertTimeToUtc(
                        TargetTime, cstZone);
                    long TargetUnixTime = TargetTimeUTC.ToUnixTimeSeconds();
                    long StartUnixTime = TargetUnixTime - 3600 * 6;
                    long EndUnixTime = TargetUnixTime + 3600 * 6;
                    wb = String.Concat("http://api.bunnyxt.com/tdd/v2/video/",
                        aid, "/record?start_ts=", StartUnixTime, "&end_ts=", EndUnixTime);
                    try
                    {
                        wbc = getwebcode1(wb, "utf-8");
                    }
                    catch
                    {
                        MessageBox.Show("服务器故障！", "计算失败！",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (wbc.Length < 10)
                    {
                        MessageBox.Show("未能获取到上周数据！", "计算失败！",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    List<JObject> JArr = new List<JObject>();
                    for (int i = 1; i < wbc.Length;)
                    {
                        int Start = wbc.IndexOf('{', i);
                        int End = wbc.IndexOf('}', i);
                        if (Start == -1 || End == -1) break;
                        stmp = wbc.Substring(Start, End - Start + 1);
                        jo = JObject.Parse(stmp);
                        JArr.Add(jo);
                        i = End + 1;
                    }
                    if (JArr.Count == 0)
                    {
                        MessageBox.Show("未能获取到上周数据！", "计算失败！",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    int lft = 0, rft = JArr.Count;
                    while (lft + 1 < rft)
                    {
                        int mid = (lft + rft) / 2;
                        stmp = JArr[mid]["added"].ToString();
                        long Tick = long.Parse(stmp);
                        if (Tick <= TargetUnixTime)
                        {
                            lft = mid;
                        }
                        else
                        {
                            rft = mid;
                        }
                    }
                    stmp = JArr[lft]["added"].ToString();
                    long NearestTick = long.Parse(stmp);
                    int TargetPos = lft;
                    if (lft + 1 != JArr.Count)
                    {
                        stmp = JArr[lft + 1]["added"].ToString();
                        if (TargetUnixTime - NearestTick >
                            long.Parse(stmp) - TargetUnixTime)
                        {
                            NearestTick = long.Parse(stmp);
                            TargetPos = lft + 1;
                        }
                    }
                    stmp = JArr[TargetPos]["danmaku"].ToString();
                    int lastDanmaku = int.Parse(stmp);
                    stmp = JArr[TargetPos]["view"].ToString();
                    int lastView = int.Parse(stmp);
                    stmp = JArr[TargetPos]["reply"].ToString();
                    int lastReply = int.Parse(stmp);
                    stmp = JArr[TargetPos]["favorite"].ToString();
                    int lastFavorite = int.Parse(stmp);
                    stmp = JArr[TargetPos]["like"].ToString();
                    int lastLike = int.Parse(stmp);
                    stmp = JArr[TargetPos]["coin"].ToString();
                    int lastCoin = int.Parse(stmp);
                    stmp = JArr[TargetPos]["added"].ToString();
                    long RealRecordUnixTime = long.Parse(stmp);
                    var RecordTime = DateTimeOffset.FromUnixTimeSeconds(RealRecordUnixTime);
                    var RecordTimeUTC = RecordTime.UtcDateTime;
                    var RecordTimeCST = TimeZoneInfo.ConvertTimeFromUtc(
                        RecordTimeUTC, cstZone);
                    datat = String.Format("{0}", RecordTimeCST);
                    view -= lastView; reply -= lastReply; 
                    danmaku -= lastDanmaku; favorite -= lastFavorite;
                    like -= lastLike; coin -= lastCoin;
                }
            }
            
            double bf = 0, dz = 0, xza = 0, xzb = 0, xzc = 0;
            int tot = 0;
            if (view > 10000) bf = view * 0.5 + 5000; else bf = view;
            if (like > 2000) dz = like * 2 + 4000; else dz = like * 4;
            xza = (bf + favorite) * 1.0 / (bf + favorite + danmaku * 10 + reply * 20);
            xzb = favorite * 1.0 / view * 250;
            xzc = coin * 1.0 / view * 150;
            if (xzb > 50) xzb = 50;
            if (xzb < 10) bf = bf * xzb * 0.1;
            if (xzc > 20) xzc = 20;
            if (xzc < 5) dz = dz * xzc * 0.2;
            tot = (int)Math.Round(bf + (reply * 25 + danmaku) * xza + dz + favorite * xzb + coin * xzc);

            string outputText = NowTimeCST + "\r\n";
            outputText = "当前时间：" + outputText;
            if (GetLastWeek)
            {
                outputText += "上周数据采集时间：";
                outputText += datat.ToString();
                outputText += "\r\n";
            }
            outputText += String.Format("av号：{0}\r\nbv号：{1}\r\n稿件标题：{2}\r\nUP主：{3}\r\n投稿时间：{4}\r\n\r\n播放数：{5}\r\n弹幕数：{6}\r\n评论数：{7}\r\n收藏数：{8}\r\n点赞数：{9}\r\n硬币数：{10}\r\n\r\n修正A：{11:0.00}\r\n修正B：{12:0.00}\r\n修正C：{13:0.00}\r\n总分：{14}", aid, bvid, title, up, pubt, view, danmaku, reply, favorite, like, coin, xza, xzb, xzc, tot);

            MessageBoxResult sav = MessageBox.Show("是否需要保存数据？", "保存数据", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (sav == MessageBoxResult.Yes)
            {
                here:
                MessageBoxResult rs = MessageBox.Show("现在需要键入数据保存位置！\r\n是否需要从此程序目录下path.txt导入？\r\n（建议您从此文件导入，否则文件名将被指定为result.txt）", "数据保存位置键入", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                string path = "";
                bool notfind_file = false;
                if (rs == MessageBoxResult.Yes)
                {
                    try
                    {
                        FileStream tmp = new FileStream("path.txt", FileMode.Open, FileAccess.Read);
                        tmp.Close();
                    }
                    catch
                    {
                        MessageBox.Show("path.txt文件不存在！", "保存失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                        notfind_file = true;
                    }
                    if (notfind_file) goto here;
                    FileStream fr = new FileStream("path.txt", FileMode.Open, FileAccess.Read);
                    StreamReader ph = new StreamReader(fr);
                    path = ph.ReadLine();
                }
                else
                {
                    if (rs == MessageBoxResult.No)
                    {
                        System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
                        System.Windows.Interop.HwndSource source = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
                        System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);
                        System.Windows.Forms.DialogResult result = dlg.ShowDialog(win);
                        path = dlg.SelectedPath + "\\result.txt";
                    }
                    else
                    {
                        MessageBox.Show("您已取消保存！", "保存被取消！", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                notfind_file = false;
                if (path[0] == '\\')
                {
                    MessageBox.Show("路径有误！", "保存失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                    notfind_file = true;
                }
                if (notfind_file) goto here;
                try
                {
                    FileStream tmp = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                    tmp.Close();
                }
                catch
                {
                    MessageBox.Show("路径有误！", "保存失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                    notfind_file = true;
                }
                if (notfind_file) goto here;
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(outputText);
                sw.Close();
                Process.Start("notepad.exe", path);
                string opt = "数据已保存在" + path + "!";
                MessageBox.Show(opt, "保存成功！", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(outputText, "当前数据", MessageBoxButton.OK);
            }
            
        }

        private void Button_exit_Click(object sender, RoutedEventArgs e)
        {
            /*FileStream fs = new FileStream("lastav.txt", FileMode.Create, FileAccess.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(av.Text);
            fs.Close();*/
            MessageBox.Show("感谢使用本软件！", "感谢使用！", MessageBoxButton.OK, MessageBoxImage.Information);
            Environment.Exit(0);
        }
        private void Text_about_Click(object sender, RoutedEventArgs e)
        {
            About r = new About();
            r.ShowDialog();
        }
    }
}
