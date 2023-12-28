using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace ProhibitedWords
{

    public partial class App : System.Windows.Application
    {
        private List<Otchet> otchets { get; set; } = new List<Otchet>();
        private List<string> ls { get; set; } = new List<string>();
        private List<string> words { get; set; } = new List<string>();
        private char[] delimiterChars = { ',', '.', ':', '\t', '!', '?' };
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new MainWindow();
            if (e.Args.Length == 0)
            {
                wnd.Show();
            }
            else
            {
                AttachConsole(-1);
                if (e.Args.Length == 3)
                {
                    Console.WriteLine(e.Args.Length.ToString());
                    DirectoryInfo dirCopy = new DirectoryInfo(e.Args[0]);
                    DirectoryInfo dirCreate = new DirectoryInfo(e.Args[1]);
                    if (!dirCopy.Exists && !dirCreate.Exists) { Console.WriteLine("Неверно введем адрес папки!"); wnd.Close(); return; }

                    words = e.Args[2].Split(delimiterChars).Select(x => x.Trim()).ToList();
                    GreateFolder(dirCreate.FullName);

                    Task t1 = Task.Run(() => GetRecursFiles(dirCopy.FullName));
                    Task t2 = t1.ContinueWith(task => CopyFile(ls, words, dirCreate.FullName));
                    Task t3 = t2.ContinueWith(task => CorrectText(dirCreate.FullName));
                    t3.Wait();
                    foreach (var o in otchets)
                    {
                        Console.WriteLine(o);
                    }
                    TopSlov top = new TopSlov(otchets);
                    Console.WriteLine(top.ToString());
                }
                else {
                    Console.WriteLine("Неверное количество аргументов!");
                }
                wnd.Close();
            }
        }
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        private async Task GetRecursFiles(string start_path)
        {
            try
            {
                string[] folders = Directory.GetDirectories(start_path);
                foreach (string folder in folders)
                {
                    await GetRecursFiles(folder);
                }
                string[] files = Directory.GetFiles(start_path);

                foreach (string filename in files)
                {
                    if (Path.GetExtension(filename) == ".txt" || Path.GetExtension(filename) == ".doc")
                    {
                        ls.Add(filename);
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Нет прав доступа:  {e.Message}");
                return;
            }
        }

        private Task CopyFile(List<string> str, List<string> words, string newPath)
        {           
            str.AsParallel().ForAll(s =>
            {
                using (StreamReader reader = new StreamReader(s))
                {
                    string line = reader.ReadToEnd();
                    foreach (var slovo in words)
                    {
                        Thread.Sleep(1000);
                        if (line.Contains(slovo) && !File.Exists($"{newPath}\\{s.Substring(s.LastIndexOf("\\") + 1)}"))
                        {
                            otchets.Add(new Otchet(s));
                            File.Copy(s, $"{newPath}\\{s.Substring(s.LastIndexOf("\\"))}", true);
                            File.Copy(s, $"{newPath}\\Corrected\\{s.Substring(s.LastIndexOf("\\"))}", true);
                        }
                    }
                }

            });
            return Task.CompletedTask;
        }

        private Task CorrectText(string path)
        {
            string repl = "*******";
            path += "\\Corrected";
            if (Directory.Exists(path))
            {
                List<string> strings = Directory.GetFiles(path).ToList();
                double proc = 50 / (double)strings.Count;
                strings.AsParallel().ForAll(s =>
                {
                    string str = "";
                    int index = otchets.FindIndex(x => x.file.Name == s.Substring(s.LastIndexOf("\\") + 1));
                    using (StreamReader reader = new StreamReader(s))
                    {
                        str = reader.ReadToEnd();
                    }
                    foreach (var slovo in words)
                    {
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
                });
            }
            return Task.CompletedTask;
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
    }
}
