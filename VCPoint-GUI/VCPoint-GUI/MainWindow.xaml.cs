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
using System.IO;
using System.Diagnostics;
using System.Net;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using Newtonsoft.Json;
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
            string wb = "https://api.bilibili.com/medialist/gateway/base/resource/info?type=2&rid=" + st;
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
            string stmp;
            stmp = jo["data"]["cnt_info"]["danmaku"].ToString();
            int d = int.Parse(stmp);
            stmp = jo["data"]["cnt_info"]["play"].ToString();
            int v = int.Parse(stmp);
            stmp = jo["data"]["cnt_info"]["reply"].ToString();
            int r = int.Parse(stmp);
            stmp = jo["data"]["cnt_info"]["collect"].ToString();
            int f = int.Parse(stmp);
            string UnixTime = jo["data"]["pubtime"].ToString();
            string up = jo["data"]["upper"]["name"].ToString();
            string title = jo["data"]["title"].ToString();
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            DateTime TranslateDate = startTime.AddSeconds(double.Parse(UnixTime));
            string pubt = String.Format("{0}", TranslateDate);

            string datat="";
            MessageBoxResult las = MessageBox.Show("是否需要考虑上周数据？", "上周数据", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (las == MessageBoxResult.Yes)
            {
                wb = "http://api.bunnyxt.com/tdd/v2/video/" + st + "/record";
                try
                {
                    wbc = getwebcode1(wb, "utf-8");
                }
                catch
                {
                    MessageBox.Show("服务器故障！", "计算失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (wbc.Length < 10)
                {
                    MessageBox.Show("输入的av号有误！", "计算失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                DateTime NowTime = DateTime.Now;
                int DayDiff = (Convert.ToInt32(NowTime.DayOfWeek) + 1) % 7;
                DateTime TargetDay = NowTime.AddDays(-DayDiff);
                DateTime TargetTime = new DateTime(TargetDay.Year, TargetDay.Month, TargetDay.Day, 3, 0, 0);
                int TargetTick = (int)(TargetTime - startTime).TotalSeconds;
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
                int lft = 0, rft = JArr.Count;
                while (lft + 1 < rft)
                {
                    int mid = (lft + rft) / 2;
                    stmp = JArr[mid]["added"].ToString();
                    int Tick = int.Parse(stmp);
                    if (Tick <= TargetTick)
                    {
                        lft = mid;
                    }
                    else
                    {
                        rft = mid;
                    }
                }
                stmp = JArr[lft]["added"].ToString();
                int NearestTick = int.Parse(stmp), TargetPos = lft;
                stmp = JArr[lft + 1]["added"].ToString();
                if (TargetTick - NearestTick > int.Parse(stmp) - TargetTick)
                {
                    NearestTick = int.Parse(stmp);
                    TargetPos = lft + 1;
                }
                stmp = JArr[TargetPos]["danmaku"].ToString();
                int lasd = int.Parse(stmp);
                stmp = JArr[TargetPos]["view"].ToString();
                int lasv = int.Parse(stmp);
                stmp = JArr[TargetPos]["reply"].ToString();
                int lasr = int.Parse(stmp);
                stmp = JArr[TargetPos]["favorite"].ToString();
                int lasf = int.Parse(stmp);
                TranslateDate = startTime.AddSeconds(NearestTick);
                datat = String.Format("{0}", TranslateDate);
                v -= lasv; r -= lasr; d -= lasd; f -= lasf;
            }

            double bf = 0, xza = 0, xzb = 0; int tot = 0;
            if (v > 10000) bf = v * 0.5 + 5000; else bf = v;
            xza = Math.Round((bf + f) * 1.0 / (bf + f + d * 10 + r * 20), 2);
            xzb = Math.Round(f * 1.0 / v * 250, 2);
            if (xzb > 50) xzb = 50;
            if (xzb < 10) bf = bf * xzb * 0.1;
            tot = (int)Math.Round(bf + (r * 25 + d) * xza + f * xzb);

            string tm = DateTime.Now.ToString();
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
                sw.Write("当前时间：");
                sw.WriteLine(tm);
                if (las == MessageBoxResult.Yes)
                {
                    sw.Write("上周数据采集时间：");
                    sw.WriteLine(datat);
                }
                sw.WriteLine("av号：{0}\r\n稿件标题：{1}\r\nUP主：{2}\r\n投稿时间：{3}\r\n\r\n播放数：{4}\r\n弹幕数：{5}\r\n评论数：{6}\r\n收藏数：{7}\r\n\r\n修正A：{8:0.00}\r\n修正B：{9:0.00}\r\n总分：{10}", st, title, up, pubt, v, d, r, f, xza, xzb, tot);
                sw.Close();
                Process.Start("notepad.exe", path);
                string opt = "数据已保存在" + path + "!";
                MessageBox.Show(opt, "保存成功！", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string opt = tm + "\r\n";
                opt = "当前时间：" + opt;
                if (las == MessageBoxResult.Yes)
                {
                    opt += "上周数据采集时间：";
                    opt += datat.ToString();
                    opt += "\r\n";
                }
                opt += "av号：" + st.ToString() + "\r\n稿件标题：" + title + "\r\nUP主：" + up + "\r\n投稿时间：" + pubt + "\r\n\r\n播放数：" + v.ToString() + "\r\n弹幕数：" + d.ToString() + "\r\n评论数：" + r.ToString() + "\r\n收藏数：" + f.ToString() + "\r\n\r\n修正A：" + xza.ToString() + "\r\n修正B：" + xzb.ToString() + "\r\n总分：" + tot.ToString();
                MessageBox.Show(opt, "当前数据", MessageBoxButton.OK);
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
