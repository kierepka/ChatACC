using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChatAAC.ViewModels;

namespace ChatAAC.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Verify DataContext
        DataContext = new MainViewModel();
    
        // Optional: Add this for debugging
        this.AttachedToVisualTree += (s, e) =>
        {
            Console.WriteLine(DataContext is MainViewModel vm
                ? $"Window loaded with {vm.Buttons.Count} buttons"
                : "DataContext is not set correctly!");
        };

       
#if DEBUG
        this.AttachDevTools();
#endif
    }
    
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}