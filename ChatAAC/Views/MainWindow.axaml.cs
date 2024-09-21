using System;
using System.ComponentModel;
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
        // Set DataContext to MainViewModel instance
        var viewModel = new MainViewModel();
        DataContext = viewModel;

        // Subscribe to the ViewModel's PropertyChanged event
        if (viewModel is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += ViewModel_PropertyChanged;
        }
#if DEBUG
        this.AttachDevTools();
#endif
    }
    
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsFullScreen)) return;
        if (sender is MainViewModel vm)
        {
            SetWindowFullScreen(vm.IsFullScreen);
        }
    }

    private void SetWindowFullScreen(bool isFullScreen)
    {
        if (isFullScreen)
        {
            Console.WriteLine("Entering full-screen mode.");
            WindowState = WindowState.FullScreen;
        }
        else
        {
            Console.WriteLine("Exiting full-screen mode.");
            WindowState = WindowState.Normal;
        }
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}