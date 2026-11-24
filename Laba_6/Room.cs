using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Laba_6
{
    public class Room : INotifyPropertyChanged
    {
         
        public const int MinTemperature = 15;
        public const int MaxTemperature = 25;

        public event Action<Room>? OnRoomChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private string name;
        private int temperature;
        private bool isLight;
        private double area;
        private int id;
        private int apartId;

        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Название комнаты не может быть пустым.");
                if (name == value) return;

                name = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public int Temperature
        {
            get { return temperature; }
            set
            {
                 
                if (value < MinTemperature || value > MaxTemperature)
                    throw new ArgumentException($"Температура должна быть от {MinTemperature} до {MaxTemperature} °C.");
                if (temperature == value) return;

                temperature = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public bool IsLight
        {
            get { return isLight; }
            set
            {
                if (isLight == value) return;
                isLight = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public double Area
        {
            get { return area; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Площадь не может быть отрицательной.");
                if (area == value) return;

                area = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public int Id
        {
            get { return id; }
            set
            {
                if (id == value) return;
                id = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public int ApartId
        {
            get { return apartId; }
            set
            {
                if (apartId == value) return;
                apartId = value;
                OnPropertyChanged();
                OnRoomChanged?.Invoke(this);
            }
        }

        public Room(string name) : this(0, 0, name, 0, 21, false) { }

        public Room(int id,
                    int apartId,
                    string name,
                    double area,
                    int temperature,
                    bool isLight)
        {
             
            this.name = name;
            this.temperature = temperature;
            this.isLight = isLight;
            this.apartId = apartId;
            this.area = area;
            this.id = id;
        }
    }
}
