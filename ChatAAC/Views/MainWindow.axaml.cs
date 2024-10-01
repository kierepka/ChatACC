using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChatAAC.ViewModels;

namespace ChatAAC.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Set DataContext to MainViewModel instance
        var viewModel = new MainViewModel();
        DataContext = viewModel;

       
#if DEBUG
        this.AttachDevTools();
#endif
    }
    
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}