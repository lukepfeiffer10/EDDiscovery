using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFLibrary
{
    /// <summary>
    /// Interaction logic for SystemViewWindow.xaml
    /// </summary>
    public partial class SystemViewWindow : Window
    {
        public SystemViewWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //textBlockSystem

            BitmapImage img = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/fge-logo-001-dark.ico"));

            Image image = new Image();
            image.Source = img;
            image.Width = 15;
            image.Height = 15;
            image.Visibility = Visibility.Visible;

            InlineUIContainer container = new InlineUIContainer(image);

            var originLastrText = ""; // new Run(strBuild.ToString());
            textBlockSystem.Inlines.Add(originLastrText);
            textBlockSystem.Inlines.Add(container);

        }
    }
}
