using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatAAC.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // DataContext jest ustawiony w XAML
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}