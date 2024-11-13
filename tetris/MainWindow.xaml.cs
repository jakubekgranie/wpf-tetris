using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer licznik = new System.Windows.Threading.DispatcherTimer(); // odliczanie
        List<List<Button>> odzwierciedlenie = new List<List<Button>>(); // konwersja grida
        List<int[][]> ksztalty = new List<int[][]>(); // lista ksztaltow
        bool isAlive = false, // czy klocek leci
            _lock = false, // czy wykonywana jest akcja
            resetTimer = false; // czy nalezy przywrocic licznik
        int[][] klocek; // dane o pozycji klocka
        SolidColorBrush filler = new SolidColorBrush(Colors.White);
        public MainWindow()
        {
            InitializeComponent();
            for (int i = 0; i < 10; i++)
            {
                odzwierciedlenie.Add(new List<Button>());
                for (int j = 0; j < 20; j++)
                {
                    Button przycisk = new Button();
                    przycisk.Background = new SolidColorBrush(Colors.SkyBlue);
                    przycisk.BorderBrush = new SolidColorBrush(Colors.CadetBlue);
                    przycisk.BorderThickness = new Thickness(1);
                    this.siatka.Children.Add(przycisk);
                    Grid.SetColumn(przycisk, i);
                    Grid.SetRow(przycisk, j);
                    odzwierciedlenie[odzwierciedlenie.Count - 1].Add(przycisk);
                }
            }
            licznik.Tick += czasomierz;
            licznik.Interval = new TimeSpan(0, 0, 1);
            licznik.Start();
            ksztalty.Add([[5, 1], [4, 1], [5, 0], [4, 0]]); // kwadrat
            ksztalty.Add([[6, 0], [5, 0], [4, 0], [3, 0]]); // slup
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // lhh
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // hll
            ksztalty.Add([[5, 2], [4, 2], [4, 1], [4, 0]]); // 3-1
            ksztalty.Add([[5, 2], [5, 1], [5, 0], [4, 0]]); // 1-3
            ksztalty.Add([[4, 1], [5, 0], [4, 0], [3, 0]]); // szpic
        }
        public void przyspieszCzasomierz()
        {
            if (!resetTimer) // by nie wybuchlo
            {
                licznik.Interval = new TimeSpan(0, 0, 0, 0, 200);
                licznik.Stop();
                licznik.Start();
                resetTimer = true;
            }
        }
        public void resetujCzasomierz()
        {
            if (resetTimer) // usprawnione odstepy czasowe
            {
                licznik.Interval = new TimeSpan(0, 0, 1);
                licznik.Stop();
                licznik.Start();
                resetTimer = false;
            }
        }
        private bool znajdz(int col, int row)
        {
            for (int i = 0; i < 4; i++)
                if (klocek[i][0] == col && klocek[i][1] == row)
                    return true;
            return false;
        }
        public void zamien(int i, int cols, int rows)
        {
            klocek[i][0] += cols;
            klocek[i][1] += rows;

            SolidColorBrush kolor;
            int i2 = i;
            int os2 = klocek[i][0], i3 = klocek[i][1];
            if (odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background == new SolidColorBrush(Colors.White))
                kolor = new SolidColorBrush(Colors.White);
            else
                kolor = new SolidColorBrush(Colors.SkyBlue);
            odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].Background = kolor;
            Grid.SetColumn(odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows], klocek[i][0] - cols);
            Grid.SetRow(odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows], klocek[i][1] - rows);

            odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = new SolidColorBrush(Colors.White);
            Grid.SetColumn(odzwierciedlenie[klocek[i][0]][klocek[i][1]], klocek[i][0]);
            Grid.SetRow(odzwierciedlenie[klocek[i][0]][klocek[i][1]], klocek[i][1]);
        }
        public bool sprawdzKompatybilnosc(int i, int cols, int rows, Key wasd)
        {
            if (klocek[i][0] + cols > 10 - cols || klocek[i][0] + cols < - cols) // scenario 1: klocek jest na maksymalnie niskiej pozycji/pod nim znajduje sie klocek; zakoncz dzialanie i stworz nowy
                return true;
            if (klocek[i][1] + rows > 20 - rows)
            {
                isAlive = false;
                return true;
            }
            if ((odzwierciedlenie[klocek[i][0] + cols][klocek[i][1] + rows].Background as SolidColorBrush).Color == Colors.White && !znajdz(klocek[i][0] + cols, klocek[i][1] + rows))
            {
                if(wasd == Key.S)
                    isAlive = false;
                return true;
            }
            return false;
        }
        public void przemiesc(Key wasd, bool reverse, int cols = 0, int rows = 1)
        {
            bool kill = false;
            if (!reverse)
            {
                for (int i = 0; i < 4; i++)
                    if(!kill)
                        kill = sprawdzKompatybilnosc(i, cols, rows, wasd);
                if (!kill)
                    for (int i = 0; i < 4; i++)
                        zamien(i, cols, rows);
                else
                {
                    _lock = false;
                    return;
                }
            }
            else
            {
                for (int i = 3; i > -1; i--)
                    if (!kill)
                        kill = sprawdzKompatybilnosc(i, cols, rows, wasd);
                if (!kill)
                    for (int i = 0; i < 4; i++)
                        zamien(i, cols, rows);
                else
                {
                    _lock = false;
                    return;
                }
            }
            _lock = false;
        }
        public void czasomierz(object s, EventArgs e)
        {
            if (!_lock) 
            {
                _lock = true;
                resetujCzasomierz();
                if (!isAlive) // automatyczne spadanie (2 ify)
                {
                    Random random = new Random();
                    klocek = ksztalty[random.Next(7)];
                    // sprawdź, czy można zrespić
                    for (int i = 0; i < klocek.Length; i++)
                        odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = new SolidColorBrush(Colors.White);
                    // licznik.Stop();
                    isAlive = true;
                    _lock = false;
                    return;
                }
                // sprawdz, czy mozna przesunac klocek
                przemiesc(Key.S, false);
            }
            else
                przyspieszCzasomierz();
        }
        public void core(int cols, int rows, bool reverse = false, Key wasd = Key.S)
        {
            if (!_lock)
            {
                _lock = true;
                if (!isAlive) // automatyczne spadanie (2 ify)
                {
                    Random random = new Random();
                    klocek = ksztalty[random.Next(7)];
                    // sprawdź, czy można zrespić
                    for (int i = 0; i < klocek.Length; i++)
                        odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = new SolidColorBrush(Colors.White);
                    // licznik.Stop();
                    isAlive = true;
                    _lock = false;
                    return;
                }
                przemiesc(wasd, reverse, cols, rows);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.A:
                    core(-1, 0, true);
                    break;
                case Key.D:
                    core(1, 0); 
                    break;
                case Key.S:
                    licznik.Stop();
                    core(0, 1, false, Key.S);
                    licznik.Start();
                    break;
            }
        }
    }
}