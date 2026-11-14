using IES.NET.Parsing;
using IES.NET.Rendering;
using System.Windows;

namespace WPFSample
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var data = new IesParser();
            var iesData = data.Parse(@"C:\1.IES");
            IMG.Source = IesBitmapRenderer.RenderToBitmapSource(iesData, new IES.NET.Options.RenderOptions());
        }
    }
}