using System.IO;
using System.Linq;

namespace ProhibitedWords
{
    public class Otchet
    {
        public Otchet(string path)
        {
            file = new FileInfo(path);
            values = new Dictionary<string, int>();
        }

        public FileInfo file { get; set; }
        public Dictionary<string, int> values { get; set; }

        public override string ToString()
        {
            string slovo = "";
            foreach (var s in values)
            {
                slovo += $"\n <{s.Key}> - количесвто исправлений {s.Value} ";
            }
            return $"Имя файла: {file.Name}  размер файла: {file.Length / 1024} KB, \n Найденные слова: {slovo} ";
        }
    }


    public class TopSlov
    {

        public Dictionary<string, int> topslov { get; set; } = new Dictionary<string, int>();


        public TopSlov(List<Otchet> ot)
        {
            foreach (var sl in ot)
            {
                foreach (var t in sl.values)
                {
                    if (topslov.ContainsKey(t.Key))
                    {
                        topslov[t.Key] += t.Value;
                    }
                    else
                    {
                        topslov.Add(t.Key, t.Value);
                    }
                }
            }         
        }

        public override string ToString()
        {
            string s = "Топ 10 популярных запрещенных слов: ";
            foreach (var top in topslov.OrderByDescending(x=>x.Value).Take(10))
            {
                s += $"\n <{top.Key}> - количесвто исправлений {top.Value} ";
            }
            return s;
        }
    }
}
