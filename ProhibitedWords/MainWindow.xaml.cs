using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;


namespace ProhibitedWords
{

    public partial class MainWindow : Window
    {
        private const int Max_Copies = 1;
        private static Semaphore semaphor = new Semaphore(Max_Copies, Max_Copies, "stmaphor");

        static CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;

        AutoResetEvent mCopy = new AutoResetEvent(true);
        bool pausesCopy = false;
        AutoResetEvent mCorrect = new AutoResetEvent(true);
        bool pausesCorr = false;

        TopSlov top;

        private List<Otchet> otchets { get; set; } = new List<Otchet>();
        private List<string> ls { get; set; } = new List<string>();
        private List<string> words { get; set; } = new List<string>();
        private char[] delimiterChars = { ',', '.', ':', '\t', '!', '?' };

        public MainWindow()
        {
            if (semaphor != null && semaphor.WaitOne(0))
            {
                InitializeComponent();
                this.MinWidth = 600;
                this.MinHeight = 500;
                //SourcePath.Text = "C:\\Users\\Фаниль\\Desktop\\TESSS\\test";
                //setwords.Text = "привет, пока, завтра, днем, ужин";
                //copyaddress.Text = "C:\\Users\\Фаниль\\Desktop\\TESSS\\testCopy";
                //if (Directory.Exists(copyaddress.Text)) Directory.Delete(copyaddress.Text, true);
            }
            else
            {
                WPF.MessageBox.Show($"Нельзя запускать больше одной копии!!!", "Info", MessageBoxButton.OK);
                Close();
            }
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            pausesCopy = true;
            pausesCorr = true;
        }

        private void addsource_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == WinForms.DialogResult.OK)
            {
                SourcePath.Text = folderBrowser.SelectedPath;
            }
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {
            otchet.Items.Clear();
            source = new CancellationTokenSource();
            token = source.Token;

            if (SourcePath.Text == string.Empty && copyaddress.Text == string.Empty && setwords.Text == string.Empty)
            {
                WPF.MessageBox.Show("Выберите папку", "Info", MessageBoxButton.OK);
                return;
            }
            words = setwords.Text.Split(delimiterChars).Select(x => x.Trim()).ToList();
            GreateFolder(copyaddress.Text);

            Task t1 = Task.Run(() => GetRecursFiles(Dispatcher.Invoke(() => SourcePath.Text)), token);
            Task t2 = t1.ContinueWith((token) => CopyFile(ls, words, Dispatcher.Invoke(() => copyaddress.Text)));
            Task t3 = t2.ContinueWith((token) => CorrectText());
            await t3;
            if (token.IsCancellationRequested)
            {
                Dispatcher.Invoke(() => { otchet.Items.Add($"Операция прервана!"); });
                return;
            }
            double proc = (double)10 / (double)otchets.Count;
            foreach (var o in otchets)
            {
                Thread.Sleep(100);
                otchet.Items.Add(o);
                progres.Value += proc;
            }

            top = new TopSlov(otchets);
            otchet.Items.Add(top.ToString());

        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            pausesCopy = false;
            pausesCorr = false;
            mCopy.Set();
            mCorrect.Set();
        }

        private async Task GetRecursFiles(string start_path)
        {
            try
            {
                string[] folders = Directory.GetDirectories(start_path);
                foreach (string folder in folders)
                {
                    if (token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => { otchet.Items.Add($"Операция прервана!"); });
                        return;
                    }
                    await GetRecursFiles(folder);
                }
                string[] files = Directory.GetFiles(start_path);

                foreach (string filename in files)
                {
                    if (token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => { otchet.Items.Add($"Операция прервана!"); });
                        return;
                    }
                    if (Path.GetExtension(filename) == ".txt" || Path.GetExtension(filename) == ".doc")
                    {
                        ls.Add(filename);
                    }
                }
            }
            catch (System.Exception e)
            {
                WPF.MessageBox.Show("Нет прав доступа", e.Message, MessageBoxButton.OK);
                return;
            }
        }

        private async Task CopyFile(List<string> str, List<string> words, string newPath)
        {
            double progr = (double)40 / (double)str.Count;
            foreach (string s in str) 
            {
                if (token.IsCancellationRequested) return;
                if (pausesCopy) { mCopy.WaitOne(); }

                using (StreamReader reader = new StreamReader(s))
                {
                    string line = reader.ReadToEnd();
                    foreach (var slovo in words)
                    {
                        Thread.Sleep(500); 
                        if (line.Contains(slovo) && !File.Exists($"{newPath}\\{s.Substring(s.LastIndexOf("\\"))}"))
                        {
                            Dispatcher.Invoke(() => otchet.Items.Add($"Скопирован файл: {s.Substring(s.LastIndexOf("\\") + 1)}"));
                            otchets.Add(new Otchet(s));
                            File.Copy(s, $"{newPath}\\{s.Substring(s.LastIndexOf("\\"))}", true);
                            File.Copy(s, $"{newPath}\\Corrected\\{s.Substring(s.LastIndexOf("\\"))}", true);
                        }
                    }
                }
                Dispatcher.Invoke(() => progres.Value += progr);

            }
        }

        private void GreateFolder(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo.CreateSubdirectory("Corrected");
        }

        private void addwords_Click(object sender, RoutedEventArgs e)
        {
            string path = "";
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    path = openFileDialog.FileName;
                }
                else { return; }
            }
            using (StreamReader str = new StreamReader(path))
            {
                setwords.Text = string.Join(", ", str.ReadToEnd().Split(delimiterChars).Select(x => x.Trim()).ToList());
            }
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            source.Cancel();
        }

        private void copath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

            if (folderBrowser.ShowDialog() == WinForms.DialogResult.OK)
            {
                copyaddress.Text = folderBrowser.SelectedPath;
            }
        }

        private async Task CorrectText()
        {
            string repl = "*******";
            string path = Dispatcher.Invoke(() => copyaddress.Text) + "\\Corrected";
            if (Directory.Exists(path))
            {

                List<string> strings = Directory.GetFiles(path).ToList();
                double proc = (double)50 / (double)strings.Count;
                foreach (string s in strings) 
                {
                    if (token.IsCancellationRequested) return;
                    if (pausesCorr) { mCorrect.WaitOne(); }

                    string str = "";
                    int index = otchets.FindIndex(x => x.file.Name == s.Substring(s.LastIndexOf("\\") + 1));
                    using (StreamReader reader = new StreamReader(s))
                    {
                        str = reader.ReadToEnd();
                    }
                    foreach (var slovo in words)
                    {
                        Thread.Sleep(500); 

                        int amount = new Regex(slovo).Matches(str).Count;
                        if (amount != 0)
                        {
                            str = str.Replace(slovo, repl);
                            otchets[index].values.Add(slovo, amount);
                        }
                    }

                    using (StreamWriter write = new StreamWriter(s, false))
                    {
                        write.WriteLine(str);
                    }
                    Dispatcher.Invoke(() => { progres.Value += proc; });

                }
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            semaphor.Release();
        }
    }
}