using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatAAC.Views;

public partial class BoardPathsWindow : Window
{
    public BoardPathsWindow()
    {
        InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}