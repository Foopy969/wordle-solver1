using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wordle_solver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string AllWords;
        int select = 0;
        List<Label> labels = new();

        private void SetGridColor(int i, int color)
        {
            labels[i].Background = new SolidColorBrush(color switch
            {
                0 => Color.FromArgb(255, 83, 141, 78),
                1 => Color.FromArgb(255, 181, 159, 59),
                2 => Color.FromArgb(255, 58, 58, 60),
                3 => Color.FromArgb(0, 0, 0, 0),
            });

            ((Border)labels[i].Parent).BorderThickness = new Thickness(color switch
            {
                0 or 1 or 2 => 0,
                3 => 1
            });
        }
        private int GetGridColor(int i)
        {
            return ((SolidColorBrush)labels[i].Background).Color switch
            {
                { R: var r, G: var g, B: var b } when r == 83  && g == 141 && b == 78 => 0,
                { R: var r, G: var g, B: var b } when r == 181 && g == 159 && b == 59 => 1,
                { R: var r, G: var g, B: var b } when r == 58  && g == 58  && b == 60 => 2,
                _ => 3
            };
        }
        private void SetGridLetter(int i, string letter)
        {
            labels[i].Content = letter;
        }
        private string GetGridLetter(int i)
        {
            return ((string)labels[i].Content).ToLower();
        }

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Border border = new()
                    {
                        Style = Resources["WordleBd"] as Style,
                    };

                    Label label = new()
                    {
                        Content = "",
                        Style = Resources["WordleLtr"] as Style,
                    };

                    border.Child = label;

                    Grid.SetColumn(border, j);
                    Grid.SetRow(border, i);

                    WordleGrid.Children.Add(border);

                    labels.Add(label);
                }
            }

            AllWords = File.ReadAllText(@"wordlist.txt");
            PossibleWordTextBox.Text = AllWords;
        }

        private void WordleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(sender as Grid);
            select = (int)p.X / 40  + (int)p.Y / 40 * 5;
            UpdateHighLight();
            RegexTextBox.Text = GenerateRegex();
        }

        private void WordleGrid_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                SetGridColor(select, 2);
                SetGridLetter(select, ((char)(e.Key - Key.A + 'A')).ToString());
            }
            else if (e.Key == Key.Back)
            {
                SetGridColor(select, 3);
                SetGridLetter(select, "");
            }

            select += e.Key switch
            {
                Key.Up => select > 4 ? -5 : 0,
                Key.Down => select < 25 ? 5 : 0,
                Key.Left or Key.Back => -1,
                Key.Right or (>= Key.A and <= Key.Z) => 1,
                _ => 0
            };

            select = (select + 30) % 30;
            UpdateHighLight();
            RegexTextBox.Text = GenerateRegex();
        }

        private void UpdateHighLight()
        {
            Grid.SetColumn(HighLight, select % 5);
            Grid.SetRow(HighLight, select / 5);
        }

        private string GenerateRegex()
        {
            string[] regex = Enumerable.Repeat(".", 5).ToArray();
            string result = "", yellow = "", grey = "";

            for (int i = 0; i < 30; i++)
            {
                string c = GetGridLetter(i);
                _ = GetGridColor(i) switch
                {
                    1 => yellow += c,
                    2 => grey += c,
                    _ => ""
                };
            }

            for (int i = 0; i < 5; i++)
            {
                IEnumerable<int> index = Enumerable.Range(0, 6).Select(x => i + x * 5);
                IEnumerable<int> temp;

                if ((temp = index.Where(x => GetGridColor(x) == 0)).Any())
                {
                    regex[i] = GetGridLetter(temp.First());
                }
                else if ((temp = index.Where(x => GetGridColor(x) == 1)).Any())
                {
                    regex[i] = "[^" + string.Join("", temp.Select(GetGridLetter).Distinct()) + "]";
                }
            }

            if (grey.Any())
            {
                result = "(?!.*[" + string.Concat(grey.Distinct()) + "])";
            }

            if (yellow.Any())
            {
                result += "(?=.*" + string.Join(")(?=.*", yellow.Distinct())+ ")";
            }

            return result + string.Join("", regex) + "\\n";
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GetGridLetter(select) == "") return;
            SetGridColor(select, Convert.ToInt32(((Rectangle)sender).Tag));
            RegexTextBox.Text = GenerateRegex();
        }

        

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (RegexTextBox.Text == ".....\\n")
            {
                PossibleWordTextBox.Text = AllWords;
                return;
            }

            PossibleWordTextBox.Text = "";
            foreach (Match match in Regex.Matches(AllWords, RegexTextBox.Text))
            {
                PossibleWordTextBox.Text += match.Value;
            }

            Dictionary<char, int> frequency = new();

            foreach (char c in PossibleWordTextBox.Text.Distinct())
            {
                frequency[c] = PossibleWordTextBox.Text.Count(x => x == c);
            }

            foreach (var pair in frequency)
            {
                if (pair.Key == '\n' || pair.Value < frequency['\n']) continue;
                frequency[pair.Key] = 0;
            }

            Dictionary<string, int> words = new();

            foreach (string w in AllWords.Split("\n"))
            {
                int count = 0;
                foreach (char c in w.Distinct())
                {
                    if (frequency.ContainsKey(c)) count += frequency[c];
                }
                if (count > 0) words.Add(w, count);
            }

            RecommendedTextBox.Text = string.Join("\n", words.ToList().OrderByDescending(x => x.Value).Select(x => x.Key));
        }
    }
}
