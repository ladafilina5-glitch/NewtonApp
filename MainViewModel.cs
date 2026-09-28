using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NewtonApp.Models;
using NewtonApp.Helpers;
using NewtonApp.Database;

namespace NewtonApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseHelper _db = new();

        private double _a = 0;
        private double _b = 1;
        private int _n = 100000;
        private int _taskCount = 4;
        private double _result;
        private double _elapsedMs;
        private string _status = "";
        private bool _isCalculating;

        public ObservableCollection<IntegralRecord> History { get; set; } = new();

        public double A { get => _a; set { _a = value; OnPropertyChanged(); } }
        public double B { get => _b; set { _b = value; OnPropertyChanged(); } }
        public int N { get => _n; set { _n = value; OnPropertyChanged(); } }
        public int TaskCount { get => _taskCount; set { _taskCount = value; OnPropertyChanged(); } }
        public double Result { get => _result; set { _result = value; OnPropertyChanged(); } }
        public double ElapsedMs { get => _elapsedMs; set { _elapsedMs = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public bool IsCalculating
        {
            get => _isCalculating;
            set { _isCalculating = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand CalculateCommand { get; }
        public ICommand ClearCommand { get; }

        public MainViewModel()
        {
            ClearCommand = new RelayCommand(_ => Clear());
            CalculateCommand = new RelayCommand(_ => CalculateAsync(), _ => !IsCalculating);

            if (!_db.TestConnection())
            {
                Status = "Нет подключения к БД (проверь пароль и имя базы)";
                return;
            }

            LoadHistory();
            Status = "Готов. Введи пределы и нажми 'Вычислить'.";
        }

        private double Function(double x) => x * x;

        private double TrapezoidPart(double a, double b, int n)
        {
            double h = (b - a) / n;
            double sum = (Function(a) + Function(b)) / 2.0;
            for (int i = 1; i < n; i++)
                sum += Function(a + i * h);
            return sum * h;
        }

        private async Task<(double result, double ms)> CalculateParallelAsync(
            double a, double b, int n, int taskCount)
        {
            var sw = Stopwatch.StartNew();

            if (taskCount <= 0) taskCount = 1;
            if (taskCount > n) taskCount = n;

            int perTask = n / taskCount;
            int remainder = n % taskCount;
            var tasks = new Task<double>[taskCount];

            for (int i = 0; i < taskCount; i++)
            {
                int startIdx = i * perTask;
                int count = perTask + (i == taskCount - 1 ? remainder : 0);
                double localA = a + startIdx * (b - a) / n;
                double localB = a + (startIdx + count) * (b - a) / n;
                int localN = count;

                tasks[i] = Task.Run(() => TrapezoidPart(localA, localB, localN));
            }

            double[] results = await Task.WhenAll(tasks);
            double total = 0;
            foreach (var r in results) total += r;

            sw.Stop();
            return (total, sw.Elapsed.TotalMilliseconds);
        }

        private async void CalculateAsync()
        {
            if (A >= B) { Status = "Ошибка: A должно быть меньше B"; return; }
            if (N <= 0) { Status = "Ошибка: N должно быть > 0"; return; }
            if (TaskCount <= 0) { Status = "Ошибка: Task > 0"; return; }

            try
            {
                IsCalculating = true;
                Status = "Вычисление...";

                var (res, ms) = await CalculateParallelAsync(A, B, N, TaskCount);

                Result = res;
                ElapsedMs = ms;

                var record = new IntegralRecord
                {
                    CalcTime = DateTime.Now,
                    A = A,
                    B = B,
                    N = N,
                    Result = res,
                    ElapsedMs = ms,
                    TaskCount = TaskCount
                };

                if (_db.SaveRecord(record))
                {
                    LoadHistory();
                    Status = $"Готово! I = {res:F10}, время = {ms:F2} мс";
                }
                else
                {
                    Status = "Результат получен, но не сохранён в БД";
                }
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsCalculating = false;
            }
        }

        private void LoadHistory()
        {
            History.Clear();
            foreach (var r in _db.GetAll())
                History.Add(r);
        }

        private void Clear()
        {
            var res = MessageBox.Show("Удалить всю историю?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _db.ClearAll();
                LoadHistory();
                Status = "История очищена";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}