using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Laba_6
{
    public class Apartment : INotifyPropertyChanged
    {
        
        public event Action<Apartment>? OnApartChanged;

        private int id;
        private int number;
        private string description;

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public Apartment(int id = 0, int number = 0, string description = "Описание квартиры")
        {
           
            this.id = id;
            this.number = number;
            this.description = description;
        }

        public int Id
        {
            get { return id; }
            set
            {
                if (id == value) return;
                id = value;
                OnPropertyChanged();
                OnApartChanged?.Invoke(this);
            }
        }

        public int Number
        {
            get { return number; }
            set
            {
                if (number == value) return;
                if (value <= 0)
                    throw new ArgumentException("Номер квартиры должен быть положительным числом.");

                number = value;
                OnPropertyChanged();
                OnApartChanged?.Invoke(this);
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                value ??= string.Empty;
                if (description == value) return;

                description = value;
                OnPropertyChanged();
                OnApartChanged?.Invoke(this);
            }
        }
    }
}
