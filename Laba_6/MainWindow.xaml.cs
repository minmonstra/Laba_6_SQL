using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Laba_6
{

    public partial class MainWindow : Window
    {
        private ObservableCollection<Apartment> apartments = new ObservableCollection<Apartment>();
        private ObservableCollection<Room> rooms = new ObservableCollection<Room>();

        private bool isLoading;          // true, пока идёт полная перезагрузка данных
        private int roomsLoadVersion;    // защита от «гонки» запросов при быстром вводе в поиске

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // Создаёт файл БД из schema.sql и seed.sql, если его ещё нет
                DatabaseManager.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подготовить базу данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            TxtDbPath.Text = "БД: " + DatabaseManager.PathToDBFile;
            InitializeDataGrids();
            Loaded += async (s, e) => await LoadData();
        }

        private void InitializeDataGrids()
        {
            DgApartments.ItemsSource = apartments;
            DgRooms.ItemsSource = rooms;
        }

        // ------------------------------------------------------------------
        //  Загрузка данных
        // ------------------------------------------------------------------

        private async Task LoadData()
        {
            isLoading = true;
            try
            {
                int? selectedApartId = (DgApartments.SelectedItem as Apartment)?.Id;

                // Загрузка квартир
                var _apartments = await DatabaseManager.GetAllApartments();
                apartments.Clear();
                foreach (var item in _apartments)
                {
                    item.OnApartChanged += UpdateApartInDB;
                    apartments.Add(item);
                }

                // Возвращаем выделение, которое сбросилось при Clear()
                if (selectedApartId != null)
                    DgApartments.SelectedItem = apartments.FirstOrDefault(a => a.Id == selectedApartId);

                await LoadRooms();
                await RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {Describe(ex)}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>Загружает комнаты с учётом поиска и фильтра «только выбранная квартира».</summary>
        private async Task LoadRooms()
        {
            int version = ++roomsLoadVersion;

            int? apartId = ChkOnlySelected.IsChecked == true
                ? (DgApartments.SelectedItem as Apartment)?.Id
                : null;
            string? search = string.IsNullOrWhiteSpace(TxtSearch.Text) ? null : TxtSearch.Text.Trim();

            try
            {
                var _rooms = await DatabaseManager.GetRooms(apartId, search);

                // Пока ждали ответ, мог начаться более новый запрос — старый результат отбрасываем
                if (version != roomsLoadVersion) return;

                rooms.Clear();
                foreach (var item in _rooms)
                {
                    item.OnRoomChanged += UpdateRoomInDB;
                    rooms.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке комнат: {Describe(ex)}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshStats()
        {
            var s = await DatabaseManager.GetStatistics();
            TxtStats.Text = $"Квартир: {s.Apartments}  |  Комнат: {s.Rooms}  |  Общая площадь: {s.TotalArea:F1} м²  |  " +
                            $"Средняя температура: {s.AvgTemperature:F1} °C  |  Свет включён: {s.LightsOn}";
        }

        private static string Describe(Exception ex)
        {
            // Код 19 в SQLite — нарушение ограничения (UNIQUE, CHECK, FOREIGN KEY, NOT NULL)
            if (ex is SqliteException { SqliteErrorCode: 19 })
                return $"нарушено ограничение базы данных (дубликат, несуществующая ссылка или недопустимое значение).\n{ex.Message}";
            return ex.Message;
        }

        // ------------------------------------------------------------------
        //  Сохранение правок из таблиц
        // ------------------------------------------------------------------

        private async void UpdateApartInDB(Apartment apart)
        {
            try
            {
                await DatabaseManager.UpdateApartments(apart);
                await RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении квартиры: {Describe(ex)}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadData(); // возвращаем в таблицу то, что реально лежит в БД
            }
        }

        private async void UpdateRoomInDB(Room room)
        {
            try
            {
                await DatabaseManager.UpdateRoom(room);
                await RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении комнаты: {Describe(ex)}\nИсправьте данные и попробуйте еще раз :)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadRooms();
            }
        }

        // ------------------------------------------------------------------
        //  Меню
        // ------------------------------------------------------------------

        private async void AddApartment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Apartment r = new Apartment();

                // Вставляем объект в базу данных (номер и ID назначает БД)
                await DatabaseManager.AddApart(r);

                // Подписываемся уже после вставки, чтобы присвоение ID не вызвало лишний UPDATE
                r.OnApartChanged += UpdateApartInDB;

                // Добавим объект в список
                apartments.Add(r);
                DgApartments.SelectedItem = r;
                DgApartments.ScrollIntoView(r);

                await RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении квартиры: {Describe(ex)}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddRoom_Click(object sender, RoutedEventArgs e)
        {
            // Комната добавляется в выбранную квартиру, а если выбора нет — в последнюю добавленную
            var apart = DgApartments.SelectedItem as Apartment ?? apartments.LastOrDefault();
            if (apart == null)
            {
                MessageBox.Show("Сначала добавьте квартиру.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Room r = new Room("Новая комната")
                {
                    ApartId = apart.Id
                };

                // Вставляем в БД (две таблицы, одна транзакция)
                await DatabaseManager.AddRoom(r);

                // Перечитываем список, чтобы новая комната появилась с учётом фильтров
                await LoadRooms();
                var added = rooms.FirstOrDefault(x => x.Id == r.Id);
                if (added != null)
                {
                    DgRooms.SelectedItem = added;
                    DgRooms.ScrollIntoView(added);
                }

                await RefreshStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении комнаты: {Describe(ex)}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteApartment_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgApartments.SelectedItem as Apartment;
            if (selected == null)
            {
                MessageBox.Show("Выберите квартиру для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить квартиру №{selected.Number} и все её комнаты?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await DatabaseManager.DeleteApartment(selected.Id);

                    // Перечитываем квартиры и комнаты (комнаты удалились каскадно)
                    DgApartments.SelectedItem = null;
                    await LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении квартиры: {Describe(ex)}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgRooms.SelectedItem as Room;
            if (selected == null)
            {
                MessageBox.Show("Выберите комнату для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить комнату '{selected.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await DatabaseManager.DeleteRoom(selected.Id);
                    rooms.Remove(selected);
                    await RefreshStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении комнаты: {Describe(ex)}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenQueries_Click(object sender, RoutedEventArgs e)
        {
            var window = new QueriesWindow { Owner = this };
            window.ShowDialog();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void ResetDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Все данные будут удалены, а база создана заново из schema.sql и seed.sql. Продолжить?",
                "Сброс базы данных", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                DatabaseManager.ResetDatabase();
                DgApartments.SelectedItem = null;
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось пересоздать базу: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------------------------------------------------------------------
        //  Фильтры и выделение
        // ------------------------------------------------------------------

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await LoadRooms();
        }

        private async void ChkOnlySelected_Click(object sender, RoutedEventArgs e)
        {
            await LoadRooms();
        }

        private async void DgApartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || ChkOnlySelected.IsChecked != true) return;
            await LoadRooms();
        }

        private void DgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgRooms.SelectedItem is Room room)
            {
                var apart = apartments.FirstOrDefault(a => a.Id == room.ApartId);
                TxtSelected.Text = apart != null
                    ? $"Выбрано: «{room.Name}», квартира №{apart.Number}"
                    : $"Выбрано: «{room.Name}»";
            }
            else
            {
                TxtSelected.Text = string.Empty;
            }
        }
    }

}
