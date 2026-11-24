using System.IO;
using System.Text;

namespace Laba_6
{
   
    internal record QueryItem(string Title, string Description, string Sql);

 
    internal static class QueryLibrary
    {
        public static List<QueryItem> Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Database", "queries.sql");
            var result = new List<QueryItem>();
            if (!File.Exists(path)) return result;

            string? title = null;
            var description = new StringBuilder();
            var sql = new StringBuilder();

            void Flush()
            {
                if (title != null && sql.ToString().Trim().Length > 0)
                    result.Add(new QueryItem(title, description.ToString().Trim(), sql.ToString().Trim()));
                description.Clear();
                sql.Clear();
            }

            foreach (string raw in File.ReadLines(path))
            {
                string line = raw.TrimEnd();

                if (line.StartsWith("-- #"))
                {
                    Flush();
                    title = line.Substring(4).Trim();
                }
                else if (title == null)
                {
                    continue; // шапка файла до первого запроса
                }
                else if (line.StartsWith("--"))
                {
                    description.AppendLine(line.Substring(2).Trim());
                }
                else
                {
                    sql.AppendLine(line);
                }
            }
            Flush();

            return result;
        }
    }
}
