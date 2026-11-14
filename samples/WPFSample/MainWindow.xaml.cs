using IES.NET.Parsing;
using IES.NET.Rendering;
using IES.NET.Options;
using System.Windows;

namespace WPFSample
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var iesPath = System.IO.Path.Combine(AppContext.BaseDirectory, "1.ies");
            var iesData = new IesParser().Parse(iesPath);
            IMG.Source = IesBitmapRenderer.RenderToBitmapSource(iesData, new RenderOptions());
        }
    }
}