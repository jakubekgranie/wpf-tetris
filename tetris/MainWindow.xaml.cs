using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace tetris
{
    public partial class MainWindow : Window
    {
        private static readonly DispatcherTimer licznik = new(), pomocniczy = new(), GOT = new(); // odliczanie
        private static readonly List<List<Button>> odzwierciedlenie = [], // konwersja grida
                                                   previewGrid = []; // podglad nastepnego klocka
        private static readonly List<int[][]> ksztalty = []; // lista ksztaltow
        private bool isAlive = false, // czy klocek leci
                    _lock = true, // czy wykonywana jest akcja
                    resetTimer = false, // czy nalezy przywrocic licznik
                    GOTCheck = false, // czy gra zakonczyla sie
                    hasBeenChanged = false; // czy zamiana jest mozliwa
        private readonly int[][] poczatkoweSiatki = [[4, 0, 2], // kwadrat
                                                    [3, 0, 4], // slup
                                                    [3, 0, 3], // lhh
                                                    [3, 0, 3], // hll
                                                    [4, 0, 3], // 3-1
                                                    [4, 0, 3], // 1-3
                                                    [3, 0, 3]]; // szpic
        private int[][] koordynatySiatki = [[0, 0, 0], [0, 0, 0]], // potrzebne do definiowania rotacji siatki i zamiany klocka
                        klocek = [[]], // dane o pozycji klocka
                        klocek2 = [[]]; // dane o pozycji nastepnego klocka
        private readonly int[] rotacja = [0, 0]; // do rotacji
        private int[] nrKlocka = [0, 0], // numer klocka - wybor gamy
                      czas = [1250, 75]; // czas spadania klocka ([czas spadania, interwal ponawiania automatycznego spadania])
        private int rzadGOT; // potrzebne do animacji końcowej
        private double[] punkty = [0, 2000, 1, 1]; // punktacja ([punkty, do nastepnego poziomu, mnoznik, poziom])
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
                                             [new SolidColorBrush(Color.FromRgb(145, 145, 145)),new SolidColorBrush(Color.FromRgb(35, 35, 35))], // lv5
                                             [new SolidColorBrush(Color.FromRgb(178, 127, 127)),new SolidColorBrush(Color.FromRgb(102, 0, 0))]]; // game over block
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
            rzadGOT = odzwierciedlenie[0].Count - 1;
            WypelnijKolorem();
            licznik.Tick += Czasomierz;
            licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[0]);
            GOT.Tick += (s, e) => {
                if (rzadGOT == -1)
                {
                    GOTCheck = true;
                    GOT.Stop();
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                        {
                            previewGrid[i][j].Background = kolorystykaKlocka[12][0];
                            previewGrid[i][j].BorderBrush = kolorystykaKlocka[12][1];
                        }
                    previewBorder.BorderBrush = extBorder.BorderBrush = kolorystykaKlocka[12][1];
                    _lock = false;
                    return;
                }
                for (int i = 0; i < odzwierciedlenie.Count; i++)
                {
                    odzwierciedlenie[i][rzadGOT].Background = kolorystykaKlocka[12][0];
                    odzwierciedlenie[i][rzadGOT].BorderBrush = kolorystykaKlocka[12][1];
                }
                rzadGOT--;
            };
            GOT.Interval = new TimeSpan(0, 0, 0, 0, 100);
            pomocniczy.Tick += Zapisy;
            pomocniczy.Interval = new TimeSpan(0, 0, 0, 0, 1);
            ksztalty.Add([[5, 1], [4, 1], [5, 0], [4, 0]]); // kwadrat
            ksztalty.Add([[6, 1], [5, 1], [4, 1], [3, 1]]); // slup
            ksztalty.Add([[4, 1], [3, 1], [5, 0], [4, 0]]); // lhh
            ksztalty.Add([[5, 1], [4, 1], [4, 0], [3, 0]]); // hll
            ksztalty.Add([[6, 1], [6, 0], [5, 1], [4, 1]]); // 3-1
            ksztalty.Add([[6, 1], [5, 1], [4, 1], [4, 0]]); // 1-3
            ksztalty.Add([[5, 1], [4, 1], [3, 1], [4, 0]]); // szpic
            LosujPrzyszlyKlocek();
            ZastapKlocek();
            _lock = false;
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
        private void OdswiezPodglad()
        {
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
        private void LosujPrzyszlyKlocek(bool zamien = false)
        {
            if(!zamien)
                nrKlocka[1] = random.Next(7);
            koordynatySiatki[1] = (int[])poczatkoweSiatki[nrKlocka[1]].Clone();
            klocek2 = ksztalty[nrKlocka[1]].Select(x => x.ToArray()).ToArray();
            rotacja[1] = 0;
            OdswiezPodglad();
        }
        private void DodajPunkty(double ilosc) // opcjonalna
        {
            punkty[0] += Math.Floor(ilosc * punkty[2] * 1.2 * punkty[3]);
            iloscPunktow.Content = punkty[0];
        }
        private void ResetujCzasomierz()
        {
            licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[0]);
            licznik.Stop();
            licznik.Start();
            resetTimer = false;
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
        private void ZastapKlocek(bool zamien = false)
        {
            // sprawdz, czy mozna usunac rzad; dodaj punkty i spowoduj upadek pozostalosci
            for (int j = 0; j < odzwierciedlenie[0].Count; j++) {
                bool rowIntegrity = true;
                //int j = 0; // nie widac w for(k)
                for (int i = 0; i < odzwierciedlenie.Count; i++)
                    if (odzwierciedlenie[i][j].Background == kolory[0])
                        rowIntegrity = false;
                if (rowIntegrity)
                {
                    DodajPunkty(500);
                    for (int k = 0; k < odzwierciedlenie.Count; k++)
                    { // usuwanie rzedu
                        odzwierciedlenie[k][j].Background = kolory[0];
                        odzwierciedlenie[k][j].BorderBrush = kolory[1];
                    }
                    for (int k = j - 1; k > -1; k--) // spadanie klockow wyzej; i - 1 = rzad nad usunietym
                        for (int l = 0; l < odzwierciedlenie.Count; l++)
                            if (odzwierciedlenie[l][k].Background != kolory[0])
                            {
                                SolidColorBrush[] temp = [(SolidColorBrush)odzwierciedlenie[l][k].Background, (SolidColorBrush)odzwierciedlenie[l][k].BorderBrush];
                                odzwierciedlenie[l][k].Background = kolory[0];
                                odzwierciedlenie[l][k].BorderBrush = kolory[1];
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
                for (; punkty[0] >= punkty[1]; punkty[1] += 1000 + 200 * 1.2 * punkty[3])
                    punkty[3]++;
                if (czas[0] != 70)
                    czas[0] = 1250 - 150 * (int)punkty[3];
                if (czas[0] < 70)
                    czas[0] = 70;
                poziom.Content = punkty[3];
                WypelnijKolorem();
                LosujPrzyszlyKlocek();
                licznik.Stop();
                ResetujCzasomierz();
            }
            klocek = klocek2.Select(x => x.ToArray()).ToArray();
            for (int i = 0; i < klocek.Length; i++)
                if (((SolidColorBrush)odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background).Color != kolory[0].Color)
                {
                    licznik.Stop();
                    GOT.Start();
                    return;
                }
            if (!zamien) { 
                hasBeenChanged = false;
                nrKlocka[0] = nrKlocka[1];
                koordynatySiatki[0] = koordynatySiatki[1];
            }
            for (int i = 0; i < klocek.Length; i++)
            {
                odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = kolorystykaKlocka[nrKlocka[0]][0];
                odzwierciedlenie[klocek[i][0]][klocek[i][1]].BorderBrush = kolorystykaKlocka[nrKlocka[0]][1];
            }
            LosujPrzyszlyKlocek(zamien);
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
        private void Przemiesc(bool reverse, bool falling, bool space, int cols = 0, int rows = 1, bool manual = false)
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
            if (manual)
                DodajPunkty(2);
            if (space)
                Przemiesc(reverse, falling, space, cols, rows, manual); // rekurencja
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
                Przemiesc(false, true, false);
            }
            else
                PrzyspieszCzasomierz();
        }
        private void Core(int cols, int rows, bool reverse = false, bool falling = false, bool manual = false, bool space = false)
        {
            if (!_lock && !GOTCheck)
            {
                _lock = true;
                if (!isAlive) // automatyczne spadanie (2 ify)
                {
                    ZastapKlocek();
                    return;
                }
                // sprawdz, czy mozna przesunac klocek
                Przemiesc(reverse, falling, space, cols, rows, manual);
            }
        }
        private void Resetuj(bool force = false)
        {
            if (!_lock)
            {
                _lock = true;
                if (force || MessageBox.Show("Czy na pewno chcesz zresetować grę?", "Resetowanie gry", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    licznik.Stop();
                    resetTimer = false;
                    koordynatySiatki = [[0, 0, 0], [0, 0, 0]];
                    nrKlocka = [0, 0];
                    czas = [1250, 75];
                    licznik.Interval = new TimeSpan(0, 0, 0, 0, czas[0]);
                    punkty = [0, 2000, 1, 1];
                    DodajPunkty(0);
                    WypelnijKolorem();
                    LosujPrzyszlyKlocek();
                    ZastapKlocek();
                    extBorder.BorderBrush = previewBorder.BorderBrush = kolory[1];
                    _lock = GOTCheck = false;
                    rzadGOT = odzwierciedlenie[0].Count - 1;
                    licznik.Start();
                }
                _lock = false;
            }
        }
        private void Zapisy(object s, EventArgs e)
        {
            if (!_lock && !GOTCheck)
            {
                _lock = true; // na wszelki
                pomocniczy.Stop();
                licznik.Stop();
                var oknoZapisow = new Window1();
                oknoZapisow.ShowDialog();
                switch (oknoZapisow.wybor)
                {
                    case 1:
                        try
                        {
                            string[] contents = new string[27];
                            StreamReader sr = new(oknoZapisow.zapis);
                            for (int i = 0; i < 27; i++)
                                contents[i] = sr.ReadLine();
                            sr.Close();
                            bool temp = false;
                            string temp2 = "";
                            for(int i = 0; i < contents[21].Length; i++)
                            {
                                if (contents[21][i] != ' ')
                                    temp2 += contents[21][i];
                                else
                                {
                                    if (!temp)
                                    {
                                        temp = true; // przelacz na poziom
                                        punkty[0] = Int32.Parse(temp2);
                                    }
                                    else
                                        punkty[3] = Int32.Parse(temp2);
                                    temp2 = "";
                                }
                            }
                            temp = false;
                            for (; punkty[0] > punkty[1]; punkty[1] += 1000 + 200 * punkty[3]); // odtworz prog poziomowy
                            for (int i = 0; i < contents[25].Length; i++) // czas
                            {
                                if (contents[25][i] != ' ')
                                    temp2 += contents[25][i];
                                else
                                    czas[0] = Int32.Parse(temp2);
                            }
                            temp2 = "";
                            int[][] temp3 = [[-1, -1, -1], [-1, -1, -1]], temp5 = [[-1, -1], [-1, -1], [-1, -1], [-1, -1]];
                            int temp4 = 0;
                            for(int i = 0; i < contents[26].Length; i++)
                            {
                                if (contents[26][i] != ' ')
                                    temp2 += contents[26][i];
                                else
                                {
                                    temp3[temp4 / 3][temp4 % 3] = Int32.Parse(temp2);
                                    temp4++;
                                    temp2 = "";
                                }
                            }
                            temp4 = 0;
                            for (int i = 0; i < temp3.Length; i++)
                                for (int j = 0; j < temp3[0].Length; j++)
                                    if (temp3[i][j] == -1)
                                        throw new Exception("Błędny zapis siatki rotacyjnej.");
                            koordynatySiatki = temp3;
                            temp3 = [[-1, -1, -1], [-1, -1, -1]];
                            for(int i = 0; i < contents[24].Length; i++)
                            {
                                if (contents[24][i] != ' ')
                                    temp2 += contents[24][i];
                                else
                                {
                                    if (!temp)
                                    {
                                        temp = true;
                                        temp3[0][0] = Int32.Parse(temp2);
                                    }
                                    else
                                        temp3[0][1] = Int32.Parse(temp2);
                                    temp2 = "";
                                }
                            }
                            temp = false;
                            for(int i = 0; i < 2; i++)
                                if (temp3[0][i] == -1)
                                    throw new Exception("Błędny zapis numeru klocka.");
                            for (int i = 0; i < 2; i++)
                                nrKlocka[i] = temp3[0][i];
                            for(int i = 0; i < contents[22].Length; i++)
                            {
                                if (contents[22][i] != ' ')
                                    temp2 += contents[22][i];
                                else
                                {
                                    temp5[temp4 / 2][temp4 % 2] = Int32.Parse(temp2);
                                    temp4++;
                                    temp2 = "";
                                }
                            }
                            temp4 = 0;
                            for(int i = 0; i < temp5.Length; i++)
                                for(int j = 0; j < temp5[i].Length; j++)
                                    if (temp5[i][j] == -1)
                                        throw new Exception("Błędny zapis koordynatów klocka.");
                            for (int i = 0; i < temp5.Length; i++)
                                for (int j = 0; j < temp5[i].Length; j++)
                                    klocek[i][j] = temp5[i][j];
                            temp5 = [[-1, -1], [-1, -1], [-1, -1], [-1, -1]];
                            for (int i = 0; i < contents[23].Length; i++)
                            {
                                if (contents[23][i] != ' ')
                                    temp2 += contents[23][i];
                                else
                                {
                                    temp5[temp4 / 2][temp4 % 2] = Int32.Parse(temp2);
                                    temp4++;
                                    temp2 = "";
                                }
                            }
                            for (int i = 0; i < temp5.Length; i++)
                                for (int j = 0; j < temp5[i].Length; j++)
                                    if (temp5[i][j] == -1)
                                        throw new Exception("Błędny zapis koordynatów klocka (2).");
                            for (int i = 0; i < temp5.Length; i++)
                                for (int j = 0; j < temp5[i].Length; j++)
                                    klocek2[i][j] = temp5[i][j];
                            for(int i = 0; i < 20; i++)
                                for(int j = 0; j < contents[0].Length; j++)
                                {
                                    int pole = 0, znakowa = contents[i][j];
                                    if (znakowa > 64)
                                        pole = znakowa - 55;
                                    else
                                        pole = znakowa - 48;
                                    odzwierciedlenie[j][i].Background = kolorystykaKlocka[pole][0];
                                    odzwierciedlenie[j][i].BorderBrush = kolorystykaKlocka[pole][1];
                                }
                            OdswiezPodglad();
                            ResetujCzasomierz();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Błąd podczas odczytywania pliku:\n" + ex.Message, "Błąd odczytu", new(), MessageBoxImage.Warning);
                        }
                        break;
                    case 2:
                        try
                        {
                            string output = "";
                            for (int i = 0; i < odzwierciedlenie[0].Count; i++) // zapis grida
                            {
                                for (int j = 0; j < odzwierciedlenie.Count; j++)
                                    for (int k = 0; k < kolorystykaKlocka.Length; k++)
                                        if (((SolidColorBrush)odzwierciedlenie[j][i].Background).Color == kolorystykaKlocka[k][0].Color)
                                        {
                                            if (k < 10)
                                                output += k;
                                            else
                                                output += (char)(55 + k);
                                        }
                                output += "\n";
                            }
                            output += "\n" + iloscPunktow.Content + " " + poziom.Content + " \n"; // zapis punktow i poziomow
                            for (int i = 0; i < klocek.Length; i++)
                                for (int j = 0; j < klocek[0].Length; j++)
                                    output += klocek[i][j] + " "; // zapis klockow
                            output += "\n";
                            for (int i = 0; i < klocek2.Length; i++)
                                for (int j = 0; j < klocek2[0].Length; j++)
                                    output += klocek2[i][j] + " "; // zapis klockow (2)
                            output += "\n";
                            for (int i = 0; i < nrKlocka.Length; i++) // nrKlocka
                                output += nrKlocka[i] + " ";
                            output += "\n" + czas[0] + " \n";
                            for (int i = 0; i < koordynatySiatki.Length; i++)
                                for (int j = 0; j < koordynatySiatki[0].Length; j++)
                                    output += koordynatySiatki[i][j] + " "; // siatka
                            byte[] parsedOutput = new UTF8Encoding(true).GetBytes(output);
                            using FileStream fs = File.Create(oknoZapisow.zapis);
                            fs.Write(parsedOutput);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Błąd podczas kreacji pliku:\n" + ex.Message, "Błąd zapisu", new(), MessageBoxImage.Warning);
                            _lock = false;
                            Resetuj();
                        }
                        break;
                }
                _lock = false;
                licznik.Start();
            }
            else
                pomocniczy.Start();
        }
        private void Rotacja(bool left = false)
        {
            if (!_lock)
            {
                _lock = true;
                if (nrKlocka[0] == 0)
                {
                    _lock = false;
                    return;
                }
                else
                {
                    List<List<Button>> rotationMatrix = [], tempMatrix = []; // rotacja z aliasingiem
                    if (koordynatySiatki[0][0] + koordynatySiatki[0][2] > odzwierciedlenie.Count || koordynatySiatki[0][1] + koordynatySiatki[0][2] > odzwierciedlenie[0].Count || koordynatySiatki[0][0] < 0 || koordynatySiatki[0][1] > odzwierciedlenie.Count)
                    { // zbyt malo przestrzeni
                        _lock = false;
                        return;
                    }
                    for (int i = 0; i < koordynatySiatki[0][2]; i++)
                    {
                        rotationMatrix.Add([]);
                        for (int j = 0; j < koordynatySiatki[0][2]; j++)
                            rotationMatrix[i].Add(odzwierciedlenie[koordynatySiatki[0][0] + i][koordynatySiatki[0][1] + j]);
                    }
                    for (int i = 0; i < koordynatySiatki[0][2]; i++)
                    {
                        tempMatrix.Add([]);
                        for (int j = 0; j < koordynatySiatki[0][2]; j++)
                            tempMatrix[i].Add(new());
                    }
                    int iterations = 1;
                    if (!left)
                        iterations = 3;
                    for (int _i = 0; _i < iterations; _i++)
                    {
                        int[][] oldKlocek = [[], [], [], []];
                        for (int i = 0; i < klocek.Length; i++) // deep copy
                            oldKlocek[i] = (int[])klocek[i].Clone();
                        for (int i = 0; i < koordynatySiatki[0][2]; i++)
                            for (int j = 0; j < koordynatySiatki[0][2]; j++)
                            {
                                tempMatrix[j][koordynatySiatki[0][2] - 1 - i].Background = rotationMatrix[i][j].Background;
                                tempMatrix[j][koordynatySiatki[0][2] - 1 - i].BorderBrush = rotationMatrix[i][j].BorderBrush;
                                if (((SolidColorBrush)tempMatrix[j][koordynatySiatki[0][2] - 1 - i].Background).Color == kolorystykaKlocka[nrKlocka[0]][0].Color)
                                    for (int k = 0; k < klocek.Length; k++)
                                        if (klocek[k][0] == koordynatySiatki[0][0] + i && klocek[k][1] == koordynatySiatki[0][1] + j)
                                        {
                                            klocek[k][0] = koordynatySiatki[0][0] + j;
                                            klocek[k][1] = koordynatySiatki[0][1] + koordynatySiatki[0][2] - 1 - i;
                                            break;
                                        }
                            }
                        int[][] tempKlocek = [[0, 0], [0, 0], [0, 0], [0, 0]]; // sortowanie
                        bool[] wasSegmentUsed = [false, false, false, false];
                        for (int i = 0; i < klocek.Length; i++)
                        {
                            int[] segment = [int.MinValue, int.MinValue];
                            int segmentMarked = 0;
                            for (int j = 0; j < klocek.Length; j++)
                                if (!wasSegmentUsed[j] && (klocek[j][0] >= segment[0] || (klocek[j][0] == segment[0] && klocek[j][1] > segment[1])))
                                {
                                    segment = [klocek[j][0], klocek[j][1]];
                                    segmentMarked = j;
                                }
                            tempKlocek[i] = segment;
                            wasSegmentUsed[segmentMarked] = true;
                        }
                        for(int i = 0; i < klocek.Length; i++)
                            if(((SolidColorBrush)rotationMatrix[tempKlocek[i][0] - koordynatySiatki[0][0]][tempKlocek[i][1] - koordynatySiatki[0][1]].Background).Color != kolory[0].Color)
                            {
                                bool isCurrentBlock = false;
                                for (int k = 0; k < tempKlocek.Length; k++)
                                    if (oldKlocek[k][0] == tempKlocek[i][0] && oldKlocek[k][1] == tempKlocek[i][1])
                                        isCurrentBlock = true;
                                if (!isCurrentBlock)
                                {
                                    klocek = oldKlocek;
                                    _lock = false;
                                    return;
                                }
                            }
                        klocek = tempKlocek;
                        for(int i = 0; i < klocek.Length; i++)
                        {
                            rotationMatrix[oldKlocek[i][0] - koordynatySiatki[0][0]][oldKlocek[i][1] - koordynatySiatki[0][1]].Background = kolory[0];
                            rotationMatrix[oldKlocek[i][0] - koordynatySiatki[0][0]][oldKlocek[i][1] - koordynatySiatki[0][1]].BorderBrush = kolory[1];
                        }
                        for (int i = 0; i < klocek.Length; i++)
                        {
                            rotationMatrix[klocek[i][0] - koordynatySiatki[0][0]][klocek[i][1] - koordynatySiatki[0][1]].Background = kolorystykaKlocka[nrKlocka[0]][0];
                            rotationMatrix[klocek[i][0] - koordynatySiatki[0][0]][klocek[i][1] - koordynatySiatki[0][1]].BorderBrush = kolorystykaKlocka[nrKlocka[0]][1];
                        }
                    }
                }
                _lock = false;
            }
        }
        private void Zmien()
        {
            if(!_lock && !hasBeenChanged)
            {
                _lock = hasBeenChanged = true;
                for(int i = 0; i < klocek.Length; i++)
                {
                    odzwierciedlenie[klocek[i][0]][klocek[i][1]].Background = kolory[0];
                    odzwierciedlenie[klocek[i][0]][klocek[i][1]].BorderBrush = kolory[1];
                }
                int[][] temp = klocek2.Select(x => x.ToArray()).ToArray();
                (nrKlocka[1], nrKlocka[0]) = (nrKlocka[0], nrKlocka[1]);
                int[] temp2 = (int[])koordynatySiatki[0].Clone();
                koordynatySiatki[0] = (int[])koordynatySiatki[1].Clone();
                koordynatySiatki[1] = temp2;
                ZastapKlocek(true);
                _lock = false;
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
                    Core(0, 1, false, true, true);
                    licznik.Start();
                    break;
                case Key.F5:
                    Zapisy(new(), e);
                    break;
                case Key.Space:
                    Core(0, 1, false, true, true, true); // piaty atrybut do rekurencji
                    break;
                case Key.F9:
                    Resetuj();
                    break;
                case Key.Right:
                    Rotacja();
                    break;
                case Key.Left:
                    Rotacja(true);
                    break;
                case Key.Z:
                    Zmien();
                    break;
            }
        }
    }
}