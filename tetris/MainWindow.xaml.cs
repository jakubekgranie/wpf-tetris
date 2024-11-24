using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly DispatcherTimer licznik = new(); // odliczanie
        private static readonly List<List<Button>> odzwierciedlenie = [], // konwersja grida
                                                   previewGrid = []; // podglad nastepnego klocka
        private static readonly List<int[][]> ksztalty = []; // lista ksztaltow
        private bool isAlive = false, // czy klocek leci
            _lock = false, // czy wykonywana jest akcja
            resetTimer = false; // czy nalezy przywrocic licznik
        private int[][] koordynatySiatki = [[0, 0, 0], [0, 0, 0]], // potrzebne do definiowania rotacji siatki i zamiany klocka
                klocek = [[]], // dane o pozycji klocka
                klocek2 = [[]], // dane o pozycji nastepnego klocka
                poczatkoweSiatki = [[4, 0, 2], // kwadrat
                                    [3, 0, 4], // slup
                                    [3, 0, 3], // lhh
                                    [3, 0, 3], // hll
                                    [4, 0, 3], // 3-1
                                    [4, 0, 3], // 1-3
                                    [3, 0, 3]]; // szpic
        private readonly int[] nrKlocka = [0, 0], // numer klocka - wybor gamy
                               czas = [1250, 75]; // czas spadania klocka ([czas spadania, interwal ponawiania automatycznego spadania])
        private readonly double[] punkty = [0, 2000, 1, 1]; // punktacja ([punkty, do nastepnego poziomu, mnoznik, poziom])
        private static readonly Random random = new(); // generator liczb losowych
        private SolidColorBrush[] kolory = []; // kolor obramowania
        private readonly SolidColorBrush[][] kolorystykaKlocka = [
                                             [new SolidColorBrush(Color.FromRgb(127, 202, 195)), new SolidColorBrush(Color.FromRgb(0, 150, 136))],
                                             [new SolidColorBrush(Color.FromRgb(249, 161, 54)), new SolidColorBrush(Color.FromRgb(244, 67, 54))],
                                             [new SolidColorBrush(Color.FromRgb(159, 168, 218)), new SolidColorBrush(Color.FromRgb(63, 81, 181))],
                                             [new SolidColorBrush(Color.FromRgb(255, 203, 127)), new SolidColorBrush(Color.FromRgb(255, 152, 0))],
                                             [new SolidColorBrush(Color.FromRgb(255, 245, 157)), new SolidColorBrush(Color.FromRgb(255, 235, 59))],
                                             [new SolidColorBrush(Color.FromRgb(197, 225, 164)), new SolidColorBrush(Color.FromRgb(139, 195, 74))],
                                             [new SolidColorBrush(Color.FromRgb(205, 147, 215)), new SolidColorBrush(Color.FromRgb(156, 39, 176))],
                                             [new SolidColorBrush(Colors.SkyBlue), new SolidColorBrush(Colors.CadetBlue)], // lv1
                                             [new SolidColorBrush(Color.FromRgb(216, 127, 191)), new SolidColorBrush(Color.FromRgb(177, 0, 127))], // lv2
                                             [new SolidColorBrush(Color.FromRgb(127, 219, 129)), new SolidColorBrush(Color.FromRgb(0, 184, 3))], // lv3
                                             [new SolidColorBrush(Color.FromRgb(159, 127, 219)),new SolidColorBrush(Color.FromRgb(64, 0, 184))], // lv4
                                             [new SolidColorBrush(Color.FromRgb(145, 145, 145)),new SolidColorBrush(Color.FromRgb(35, 35, 35))]]; // lv5
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
                        BorderThickness = new Thickness(1)
                    };
                    this.siatka.Children.Add(przycisk);
                    Grid.SetColumn(przycisk, i);
                    Grid.SetRow(przycisk, j);
                    odzwierciedlenie[^1].Add(przycisk);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                previewGrid.Add([]);
                for(int j = 0; j < 4; j++)
                {
                    Button przycisk = new()
                    {
                        BorderThickness = new Thickness(1)
                    };
                    this.preview.Children.Add(przycisk);
                    Grid.SetColumn(przycisk, i);
                    Grid.SetRow(przycisk, j);
                    previewGrid[^1].Add(przycisk);
                }
            }
            WypelnijKolorem();
            for (int j = 3; j > 0; j--)
                for (int i = 0; i < 10; i++) // [TEST]
                    odzwierciedlenie[i][^j].Background = new SolidColorBrush(Colors.White);
            licznik.Tick += Czasomierz;
            licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[0]);
            ksztalty.Add([[5, 1], [4, 1], [5, 0], [4, 0]]); // kwadrat
            ksztalty.Add([[6, 0], [5, 0], [4, 0], [3, 0]]); // slup
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // lhh
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // hll
            ksztalty.Add([[5, 2], [4, 2], [4, 1], [4, 0]]); // 3-1
            ksztalty.Add([[5, 2], [5, 1], [5, 0], [4, 0]]); // 1-3
            ksztalty.Add([[4, 1], [5, 0], [4, 0], [3, 0]]); // szpic
            LosujPrzyszlyKlocek();
            ZastapKlocek();
            licznik.Start();
        }
        private void WypelnijKolorem()
        {
            kolory = kolorystykaKlocka[6 + (int)punkty[3] % (kolorystykaKlocka.Length - 6)];
            for(int i = 0; i < odzwierciedlenie.Count; i++) 
                for(int j = 0; j < odzwierciedlenie[0].Count; j++)
                {
                    odzwierciedlenie[i][j].Background = kolory[0];
                    odzwierciedlenie[i][j].BorderBrush = kolory[1];
                }
        }
        private void LosujPrzyszlyKlocek()
        {
            nrKlocka[1] = random.Next(7);
            koordynatySiatki[1] = poczatkoweSiatki[nrKlocka[1]];
            klocek2 = ksztalty[nrKlocka[1]].Select(x => x.ToArray()).ToArray();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    previewGrid[i][j].Background = kolory[0]; // nowe przyciski nie dzialaja
                    previewGrid[i][j].BorderBrush = kolory[1];
                }
            for (int i = 0; i < klocek2.Length; i++)
            {
                previewGrid[klocek2[i][0] - 3][klocek2[i][1]].Background = kolorystykaKlocka[nrKlocka[1]][0];
                previewGrid[klocek2[i][0] - 3][klocek2[i][1]].BorderBrush = kolorystykaKlocka[nrKlocka[1]][1];
            }
        }
        private void DodajPunkty(double ilosc) // opcjonalna
        {
            punkty[0] += ilosc * punkty[2] * punkty[3];
            iloscPunktow.Content = punkty[0];
        }
        private void ResetujCzasomierz(bool force = false)
        {
            if (force || resetTimer) // usprawnione odstepy czasowe
            {
                licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[0]);
                licznik.Stop();
                licznik.Start();
                resetTimer = false;
            }
        }
        private void PrzyspieszCzasomierz()
        {
            if (!resetTimer) // by nie wybuchlo
            {
                licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[1]);
                licznik.Stop();
                licznik.Start();
                resetTimer = true;
            }
        }
        private void ZastapKlocek()
        {
            // sprawdz, czy mozna usunac rzad; dodaj punkty i spowoduj upadek pozostalosci
            for (int j = 0; j < odzwierciedlenie[0].Count; j++) {
                bool rowIntegrity = true;
                //int j = 0; // nie widac w for(k)
                for (int i = 0; i < odzwierciedlenie.Count; i++)
                    if(odzwierciedlenie[i][j].Background == kolory[0])
                        rowIntegrity = false;
                if (rowIntegrity)
                {
                    DodajPunkty(500);
                    for(int k = 0; k < odzwierciedlenie.Count; k++) // usuwanie rzedu
                        odzwierciedlenie[k][j].Background = kolory[0];
                    for (int k = j - 1; k > -1; k--) // spadanie klockow wyzej; i - 1 = rzad nad usunietym
                        for (int l = 0; l < odzwierciedlenie.Count; l++)
                            if (odzwierciedlenie[l][k].Background != kolory[0])
                            {
                                SolidColorBrush[] temp = [(SolidColorBrush)odzwierciedlenie[l][k].Background, (SolidColorBrush)odzwierciedlenie[l][k + 1].BorderBrush];
                                odzwierciedlenie[l][k].Background = kolory[0];
                                odzwierciedlenie[l][k + 1].Background = temp[0]; // klocek nizej; k = kolumny
                                odzwierciedlenie[l][k + 1].BorderBrush = temp[1];
                            }
                    punkty[2] += 0.5;
                }
            }
            punkty[2] = 1;
            // zdefiniuj nastepny klocek
            if (punkty[0] >= punkty[1])
            {
                for (; punkty[0] >= punkty[1]; punkty[1] += 1000 + 200 * punkty[3])
                    punkty[3]++;
                if (czas[0] != 200)
                    czas[0] = 1250 - 150 * (int)punkty[3];
                poziom.Content = punkty[3];
                WypelnijKolorem();
                LosujPrzyszlyKlocek();
                licznik.Stop();
                ResetujCzasomierz(true);
            }
            klocek = klocek2.Select(x => x.ToArray()).ToArray();
            nrKlocka[0] = nrKlocka[1];
            koordynatySiatki[0] = koordynatySiatki[1];
            for (int i = 0; i < klocek.Length; i++)
            {
                odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = kolorystykaKlocka[nrKlocka[0]][0];
                odzwierciedlenie[klocek[i][0]][klocek[i][1]].BorderBrush = kolorystykaKlocka[nrKlocka[0]][1];
            }
            LosujPrzyszlyKlocek();
            isAlive = true;
            _lock = false;
        }
        private bool Znajdz(int col, int row)
        {
            for (int i = 0; i < 4; i++)
                if (klocek[i][0] == col && klocek[i][1] == row)
                    return true;
            return false;
        }
        private void Zamien(int i, int cols, int rows)
        {
            klocek[i][0] += cols;
            klocek[i][1] += rows;

            SolidColorBrush[] temp = [(SolidColorBrush)odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background, (SolidColorBrush)odzwierciedlenie[klocek[i][0]][klocek[i][1]].BorderBrush];
            odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].Background;
            odzwierciedlenie[klocek[i][0]][klocek[i][1]].BorderBrush = odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].BorderBrush;
            odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].Background = temp[0];
            odzwierciedlenie[klocek[i][0] - cols][klocek[i][1] - rows].BorderBrush = temp[1];
        }
        private bool SprawdzKompatybilnosc(int i, int cols, int rows, bool falling)
        {
            if (klocek[i][0] + cols > 10 - cols || klocek[i][0] + cols < 0) // scenario 1: klocek jest na maksymalnie niskiej pozycji/pod nim znajduje sie klocek; zakoncz dzialanie i stworz nowy
                return true;
            if (klocek[i][1] + rows > 20 - rows)
            {
                isAlive = false;
                ZastapKlocek();
                return true;
            }
            if (((SolidColorBrush)odzwierciedlenie[klocek[i][0] + cols][klocek[i][1] + rows].Background).Color != kolory[0].Color && !Znajdz(klocek[i][0] + cols, klocek[i][1] + rows))
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
        private void Przemiesc(bool reverse, bool falling, int cols = 0, int rows = 1)
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
            koordynatySiatki[0][0] += cols;
            koordynatySiatki[0][1] += rows;
            _lock = false;
        }
        private void Czasomierz(object s, EventArgs e)
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
        private void Core(int cols, int rows, bool reverse = false, bool falling = false)
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
        private void Zmien()
        {

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