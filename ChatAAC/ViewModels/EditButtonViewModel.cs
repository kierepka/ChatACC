using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using ChatAAC.Models.Obf;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using ChatAAC.Views;

namespace ChatAAC.ViewModels;

public class EditButtonViewModel : ReactiveObject
{
    private readonly Button _originalButton;
    private readonly IList<Image> _obfImages; // direct reference to the underlying ObfFile.Images

    private string _id;
    private string _label;
    private string _borderColor;
    private string _backgroundColor;
    private Color _borderColorAvalonia = Colors.Black;
    private Color _backgroundColorAvalonia = Colors.White;
    private string _vocalization;
    private string _action;
    private string? _loadBoardPath;

    private Image? _selectedImage;

    public bool IsConfirmed { get; private set; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> AddImageCommand { get; }

    public EditButtonViewModel(Button button, IList<Image> obfImages)
    {
        _originalButton = button ?? throw new ArgumentNullException(nameof(button));
        _obfImages = obfImages;

        // Copy existing fields
        _id = button.Id;
        _label = button.Label;
        _borderColor = button.BorderColor;
        _backgroundColor = button.BackgroundColor;
        
        // Convert any existing string to an Avalonia color
        if (Color.TryParse(button.BorderColor, out var bc))
            _borderColorAvalonia = bc;
        if (Color.TryParse(button.BackgroundColor, out var bg))
            _backgroundColorAvalonia = bg;

        
        _vocalization = button.Vocalization;
        _action = button.Action;
        _loadBoardPath = button.LoadBoard?.Path;

        // Try to find the currently selected image from the button’s ImageId
        var found = obfImages.FirstOrDefault(img => img.Id == button.ImageId);
        _selectedImage = found;

        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);
        AddImageCommand = ReactiveCommand.Create(AddImage); 
    }

    #region Properties

    public string Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }


    public string BorderColor
    {
        get => _originalButton.BorderColor;
        set
        {
            _originalButton.BorderColor = value;
            // Optionally parse it to keep your color pickers in sync
            if (Color.TryParse(value, out var c))
                BorderColorAvalonia = c;
            this.RaisePropertyChanged();
        }
    }

    public string BackgroundColor
    {
        get => _originalButton.BackgroundColor;
        set
        {
            _originalButton.BackgroundColor = value;
            // Optionally parse it to keep your color pickers in sync
            if (Color.TryParse(value, out var c))
                BackgroundColorAvalonia = c;
            this.RaisePropertyChanged();
        }
    }

    public Color BorderColorAvalonia
    {
        get => _borderColorAvalonia;
        set => this.RaiseAndSetIfChanged(ref _borderColorAvalonia, value);
    }

    public Color BackgroundColorAvalonia
    {
        get => _backgroundColorAvalonia;
        set => this.RaiseAndSetIfChanged(ref _backgroundColorAvalonia, value);
    }

    public string Vocalization
    {
        get => _vocalization;
        set => this.RaiseAndSetIfChanged(ref _vocalization, value);
    }

    public string Action
    {
        get => _action;
        set => this.RaiseAndSetIfChanged(ref _action, value);
    }

    public string? LoadBoardPath
    {
        get => _loadBoardPath;
        set => this.RaiseAndSetIfChanged(ref _loadBoardPath, value);
    }

    /// <summary>
    /// Currently selected image from the available list
    /// </summary>
    public Image? SelectedImage
    {
        get => _selectedImage;
        set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
    }

    /// <summary>
    /// The list of images in the OBF. Bound in the dialog for preview/selection.
    /// </summary>
    public IList<Image> AvailableImages => _obfImages;

    #endregion

    #region Methods

    private void Confirm()
    {
        // Save changes back to the original button
        _originalButton.Id = _id;
        _originalButton.Label = _label;
        _originalButton.BorderColor = _borderColorAvalonia.ToString();
        _originalButton.BackgroundColor = _backgroundColorAvalonia.ToString();
        _originalButton.Vocalization = _vocalization;
        _originalButton.Action = _action;

        // If an image is selected, store its ID
        _originalButton.ImageId = _selectedImage?.Id ?? string.Empty;

        if (!string.IsNullOrEmpty(_loadBoardPath))
        {
            _originalButton.LoadBoard ??= new LoadBoard();
            _originalButton.LoadBoard.Path = _loadBoardPath;
        }
        else
        {
            _originalButton.LoadBoard = null;
        }

        IsConfirmed = true;
        CloseWindow();
    }

    private void Cancel()
    {
        IsConfirmed = false;
        CloseWindow();
    }

    /// <summary>
    /// Creates a new image by opening a separate "AddImageWindow".
    /// Once added, we pick that image as selected.
    /// </summary>
    private void AddImage()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var vm = new AddImageViewModel();
        var addWindow = new AddImageWindow
        {
            DataContext = vm
        };
        if (desktop.MainWindow != null) addWindow.ShowDialog(desktop.MainWindow);

        if (!vm.IsConfirmed)
            return;

        // user created a new image
        var newImage = vm.CreateImage();
        // Add to the underlying list
        _obfImages.Add(newImage);

        // Now set the newly created image as selected
        SelectedImage = newImage;
    }

    private void CloseWindow()
    {
        var lifetime = Application.Current?.ApplicationLifetime;
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Windows[^1].Close();
        }
    }

    #endregion
}