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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Piano
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String KeyName;
        private String ButtonName;
        private String MusicSheetName;
        private String BeatMeasure;
        private String BeatLength;



        public MainWindow()
        {
            InitializeComponent();
            // for back programming.
            BackCode backcode = new BackCode();
            
        }

        

//Key Controller (Each Key has has its own parts)
//
//
//White Keys
        private void MouseOver(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Aqua;
        }

        private void MouseLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.White;
        }



//Black Keys
        private void MouseBlackOver(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Aqua;
        }

        private void MouseBlackLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Black;
        }



//Capture Mouse Click
        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Yellow;
            KeyName = (((FrameworkElement)e.Source).Name);
            Console.WriteLine(KeyName);
        }
        
        private void MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Aqua;
            KeyName = "";
            Console.WriteLine(KeyName);
        }


//Button actions
        private void ButtonDown(object sender, RoutedEventArgs e)
        {
            ButtonName = (((FrameworkElement)e.Source).Name);
            Console.WriteLine(ButtonName); //test name capture
            //do something here on backside
        }


//Popup Window
        private void PopUp(object sender, RoutedEventArgs e)
        {
            NewPop.IsOpen = true;
        }



//Music Sheet Name
        private void MusicName(object sender, EventArgs e)
        {
           TextBox textbox = sender as TextBox;
            if(textbox != null){
                String TCount = textbox.Text;
                MusicSheetName = TCount;
                Console.WriteLine(MusicSheetName);// tested passed
                //do something back end with this top count
            }
        }


//Close popup and load NoteViewer
        private void NoteViewer_Loaded(object sender, RoutedEventArgs e)
        {
            NewPop.IsOpen = false;
        }
        


//combo box Beats/ Measure
        private void BeatsMeasureBox(object sender, SelectionChangedEventArgs e)
        {
            BeatMeasure = (Beats_Measure.SelectedItem as ComboBoxItem).Content.ToString();

        }



//Combo Box Beat Length
        private void BeatLengthBox(object sender, SelectionChangedEventArgs e)
        {
            BeatLength = (Beat_Length.SelectedItem as ComboBoxItem).Content.ToString();
        }



      
    }
}
