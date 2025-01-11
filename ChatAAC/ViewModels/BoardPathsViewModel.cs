using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class BoardPathsViewModel : ReactiveObject
{
    private string? _selectedBoardPath;

    public BoardPathsViewModel(ObservableCollection<string> boardPaths)
    {
        BoardPaths = boardPaths;

        OkCommand = ReactiveCommand.Create(Ok);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    // The list of paths
    public ObservableCollection<string> BoardPaths { get; }

    // The currently selected path
    public string? SelectedBoardPath
    {
        get => _selectedBoardPath;
        set => this.RaiseAndSetIfChanged(ref _selectedBoardPath, value);
    }

    // The commands for user confirmation
    public ReactiveCommand<Unit, Unit> OkCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    private void Ok()
    {
        CloseWindow();
    }

    private void Cancel()
    {
        // Ensure no path is selected if canceled (optional).
        SelectedBoardPath = null;
        CloseWindow();
    }

    private void CloseWindow()
    {
        // If this is shown as a dialog, we can close the window.  
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // We look for the active window, or a specialized approach
            var topMost = desktop.Windows[^1]; 
            topMost.Close();
        }
    }
}