using Microsoft.Win32;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laba_6
{
    public partial class QueriesWindow : Window
    {
        private DataTable? lastResult;

        public QueriesWindow()
        {
            InitializeComponent();

            var queries = QueryLibrary.Load();
            LstQueries.ItemsSource = queries;

            if (queries.Count > 0)
                LstQueries.SelectedIndex = 0;
            else
                TxtStatus.Text = "Файл Database/queries.sql не найден или пуст.";
        }

        private void LstQueries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstQueries.SelectedItem is QueryItem q)
            {
                TxtDescription.Text = q.Description;
                TxtSql.Text = q.Sql;
            }
        }

        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Foreground = Brushes.Black;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                DataTable table = await DatabaseManager.ExecuteReadOnlyQuery(TxtSql.Text);
                stopwatch.Stop();

                lastResult = table;
                DgResult.ItemsSource = table.DefaultView;
                TxtStatus.Text = $"Строк: {table.Rows.Count}  |  {stopwatch.ElapsedMilliseconds} мс";
            }
            catch (Exception ex)
            {
                lastResult = null;
                DgResult.ItemsSource = null;
                TxtStatus.Foreground = Brushes.Firebrick;
                TxtStatus.Text = "Ошибка: " + ex.Message;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (lastResult == null || lastResult.Rows.Count == 0)
            {
                MessageBox.Show("Сначала выполните запрос, который вернёт данные.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "result.csv"
            };
            if (dialog.ShowDialog(this) != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Join(";", lastResult.Columns.Cast<DataColumn>().Select(c => CsvCell(c.ColumnName))));
                foreach (DataRow row in lastResult.Rows)
                    sb.AppendLine(string.Join(";", row.ItemArray.Select(CsvCell)));

                // UTF-8 с BOM и разделитель «;» — так файл корректно откроется в русском Excel
                File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
                TxtStatus.Foreground = Brushes.Black;
                TxtStatus.Text = $"Сохранено: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить файл: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string CsvCell(object? value)
        {
            string s = value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

            if (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
