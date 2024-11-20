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
        private static readonly DispatcherTimer licznik = new(); // odliczanie
        private static readonly List<List<Button>> odzwierciedlenie = [], previewGrid = []; // konwersja grida, podglad nastepnego klocka
        private static readonly List<int[][]> ksztalty = []; // lista ksztaltow
        bool isAlive, // czy klocek leci
            _lock = false, // czy wykonywana jest akcja
            resetTimer = false; // czy nalezy przywrocic licznik
        int[][] klocek = [[]], klocek2 = [[]]; // dane o pozycji klocka, dane o pozycji nastepnego klocka
        int[] wylosowany = []; // potrzebne do definiowania rotacji siatki
        private static readonly Random random = new(); // generator liczb losowych
        public MainWindow()
        {
            InitializeComponent();
            for (int i = 0; i < 10; i++)
            {
                odzwierciedlenie.Add([]);
                for (int j = 0; j < 20; j++)
                {
                    Button przycisk = new()
                    {
                        Background = new SolidColorBrush(Colors.SkyBlue),
                        BorderBrush = new SolidColorBrush(Colors.CadetBlue),
                        BorderThickness = new Thickness(1)
                    };
                    this.siatka.Children.Add(przycisk);
                    Grid.SetColumn(przycisk, i);
                    Grid.SetRow(przycisk, j);
                    odzwierciedlenie[^1].Add(przycisk);
                }
            }
            for (int i = 0; i < 10; i++)
                odzwierciedlenie[i][^1].Background = new SolidColorBrush(Colors.White);
            for (int i = 0; i < 4; i++)
            {
                previewGrid.Add([]);
                for(int j = 0; j < 4; j++)
                {
                    Button przycisk = new()
                    {
                        Background = new SolidColorBrush(Colors.SkyBlue),
                        BorderBrush = new SolidColorBrush(Colors.CadetBlue),
                        BorderThickness = new Thickness(1)
                    };
                    this.preview.Children.Add(przycisk);
                    Grid.SetColumn(przycisk, i);
                    Grid.SetRow(przycisk, j);
                    previewGrid[^1].Add(przycisk);
                }
            }
            licznik.Tick += Czasomierz;
            licznik.Interval = new TimeSpan(0, 0, 1);
            ksztalty.Add([[5, 1], [4, 1], [5, 0], [4, 0]]); // kwadrat
            ksztalty.Add([[6, 0], [5, 0], [4, 0], [3, 0]]); // slup
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // lhh
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // hll
            ksztalty.Add([[5, 2], [4, 2], [4, 1], [4, 0]]); // 3-1
            ksztalty.Add([[5, 2], [5, 1], [5, 0], [4, 0]]); // 1-3
            ksztalty.Add([[4, 1], [5, 0], [4, 0], [3, 0]]); // szpic
            LosujPrzyszlyKlocek();
            licznik.Start();
        }
        public void LosujPrzyszlyKlocek()
        {
            klocek2 = ksztalty[random.Next(7)].Select(x => x.ToArray()).ToArray();
            for (int i = 0; i < 4; i++)
                for(int j = 0; j < 4; j++)
                    previewGrid[i][j].Background = new SolidColorBrush(Colors.SkyBlue); // nowe przyciski nie dzialaja
            for (int i = 0; i < klocek2.Length; i++)
                previewGrid[klocek2[i][0] - 3][klocek2[i][1]].Background = new SolidColorBrush(Colors.White);
        }
        public void DodajPunkty(double ilosc) // opcjonalna
        {
            iloscPunktow.Content = Int32.Parse(iloscPunktow.Content.ToString()) + ilosc;
        }
        public void ZastapKlocek()
        {
            // sprawdz, czy mozna usunac rzad; dodaj punkty i spowoduj upadek pozostalosci
            double mnoznik = 1;
            for (int j = 0; j < odzwierciedlenie[0].Count; j++) {
                bool rowIntegrity = true;
                //int j = 0; // nie widac w for(k)
                for (int i = 0; i < odzwierciedlenie.Count; i++)
                    if(((SolidColorBrush)odzwierciedlenie[i][j].Background).Color != Colors.White)
                        rowIntegrity = false;
                if (rowIntegrity)
                {
                    DodajPunkty(mnoznik * 1000);
                    for(int k = 0; k < odzwierciedlenie.Count; k++) // usuwanie rzedu
                        odzwierciedlenie[k][j].Background = new SolidColorBrush(Colors.SkyBlue);
                    for (int k = j - 1; k > -1; k--) // spadanie klockow wyzej; i - 1 = rzad nad usunietym
                        for (int l = 0; l < odzwierciedlenie.Count; l++)
                        {
                            List<List<Button>> a = odzwierciedlenie;
                            if (((SolidColorBrush)odzwierciedlenie[l][k].Background).Color == Colors.White)
                            {
                                odzwierciedlenie[l][k].Background = new SolidColorBrush(Colors.SkyBlue);
                                odzwierciedlenie[l][k + 1].Background = new SolidColorBrush(Colors.White); // klocek nizej; k = kolumny
                            }
                        }
                    mnoznik += 0.5;
                }
                else
                    mnoznik = 1;
            }
            // zdefiniuj nastepny klocek
            klocek = klocek2.Select(x => x.ToArray()).ToArray();
            for (int i = 0; i < klocek.Length; i++)
                odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = new SolidColorBrush(Colors.White);
            LosujPrzyszlyKlocek();
            isAlive = true;
            _lock = false;
        }
        public void PrzyspieszCzasomierz()
        {
            if (!resetTimer) // by nie wybuchlo
            {
                licznik.Interval = new TimeSpan(0, 0, 0, 0, 200);
                licznik.Stop();
                licznik.Start();
                resetTimer = true;
            }
        }
        public void ResetujCzasomierz()
        {
            if (resetTimer) // usprawnione odstepy czasowe
            {
                licznik.Interval = new TimeSpan(0, 0, 1);
                licznik.Stop();
                licznik.Start();
                resetTimer = false;
            }
        }
        private bool Znajdz(int col, int row)
        {
            for (int i = 0; i < 4; i++)
                if (klocek[i][0] == col && klocek[i][1] == row)
                    return true;
            return false;
        }
        public void Zamien(int i, int cols, int rows)
        {
            klocek[i][0] += cols;
            klocek[i][1] += rows;

            SolidColorBrush kolor;
            if (odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background == new SolidColorBrush(Colors.White))
                kolor = new SolidColorBrush(Colors.White);
            else
                kolor = new SolidColorBrush(Colors.SkyBlue);
            odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].Background = kolor;
            odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = new SolidColorBrush(Colors.White);
        }
        public bool SprawdzKompatybilnosc(int i, int cols, int rows, bool falling)
        {
            if (klocek[i][0] + cols > 10 - cols || klocek[i][0] + cols < 0) // scenario 1: klocek jest na maksymalnie niskiej pozycji/pod nim znajduje sie klocek; zakoncz dzialanie i stworz nowy
                return true;
            if (klocek[i][1] + rows > 20 - rows)
            {
                isAlive = false;
                ZastapKlocek();
                return true;
            }
            if (((SolidColorBrush)odzwierciedlenie[klocek[i][0] + cols][klocek[i][1] + rows].Background).Color == Colors.White && !Znajdz(klocek[i][0] + cols, klocek[i][1] + rows))
            {
                if (falling)
                {
                    isAlive = false;
                    ZastapKlocek();
                }
                return true;
            }
            return false;
        }
        public void Przemiesc(bool reverse, bool falling, int cols = 0, int rows = 1)
        {
            bool kill = false;
            if (!reverse)
            {
                for (int i = 0; i < 4; i++)
                {
                    kill = SprawdzKompatybilnosc(i, cols, rows, falling);
                    if (kill)
                        break;
                }
                if (!kill)
                    for (int i = 0; i < 4; i++)
                        Zamien(i, cols, rows);
                else
                {
                    _lock = false;
                    return;
                }
            }
            else
            {
                for (int i = 3; i > -1; i--)
                {
                    kill = SprawdzKompatybilnosc(i, cols, rows, falling);
                    if (kill)
                        break;
                }
                if (!kill)
                    for (int i = 3; i > -1; i--)
                        Zamien(i, cols, rows);
                else
                {
                    _lock = false;
                    return;
                }
            }
            _lock = false;
        }
        public void Czasomierz(object s, EventArgs e)
        {
            if (!_lock) 
            {
                _lock = true;
                ResetujCzasomierz();
                if (!isAlive) // automatyczne spadanie (2 ify)
                {
                    ZastapKlocek();
                    return;
                }
                // sprawdz, czy mozna przesunac klocek
                Przemiesc(false, true);
            }
            else
                PrzyspieszCzasomierz();
        }
        public void Core(int cols, int rows, bool reverse = false, bool falling = false)
        {
            if (!_lock)
            {
                _lock = true;
                if (!isAlive) // automatyczne spadanie (2 ify)
                {
                    ZastapKlocek();
                    return;
                }
                // sprawdz, czy mozna przesunac klocek
                Przemiesc(reverse, falling, cols, rows);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.A:
                    Core(-1, 0, true);
                    break;
                case Key.D:
                    Core(1, 0, false); 
                    break;
                case Key.S:
                    licznik.Stop();
                    Core(0, 1, false, true);
                    licznik.Start();
                    break;
            }
        }
    }
}