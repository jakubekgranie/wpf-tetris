using System.Windows;

namespace tetris
{
    public partial class Window1 : Window
    {
        public string zapis = ""; // sciezka
        internal int wybor = 0;
        public Window1()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Dokumenty tekstowe (.txt)|*.txt"
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                zapis = dialog.FileName;
                savefile.Text = zapis;
                loadfile.Text = null;
                wybor = 1;
                feed.Text = "Wybrano opcję załadowania pliku.";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Dokumenty tekstowe (.txt)|*.txt"
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                zapis = dialog.FileName;
                loadfile.Text = zapis;
                savefile.Text = null;
                wybor = 2;
                feed.Text = "Wybrano opcję nowego pliku.";
            }
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            wybor = 0;
            loadfile.Text = savefile.Text = null;
            feed.Text = "Wyłączono zapis.";
        }
    }
}
