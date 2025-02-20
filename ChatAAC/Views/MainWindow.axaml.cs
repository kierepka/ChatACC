using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatAAC.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
            this.AttachDevTools();

        // (Optional) If you want a debug trace:
        AttachedToVisualTree += (s, e) =>
        {
            // This is just to confirm your VM is present
            System.Diagnostics.Debug.WriteLine(DataContext is null
                ? "DataContext is not set or is null!"
                : "MainWindow is attached with a valid DataContext.");
        };
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}