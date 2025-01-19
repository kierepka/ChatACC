using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using ChatAAC.Models.Obf;

namespace ChatAAC.ViewModels;

public class EditButtonViewModel : ReactiveObject
{
    private readonly Button _originalButton;

    private string _id;
    private string _label;
    private Image? _selectedImage;      // Replaces the old "string _imageId"
    private string _borderColor;
    private string _backgroundColor;
    private string _vocalization;
    private string _action;
    private string? _loadBoardPath;

    public bool IsConfirmed { get; private set; }

    public IReadOnlyList<Image> AvailableImages { get; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public EditButtonViewModel(Button button, IEnumerable<Image> availableImages)
    {
        _originalButton = button ?? throw new ArgumentNullException(nameof(button));

        // Initialize local fields
        _id = button.Id;
        _label = button.Label;
        _borderColor = button.BorderColor;
        _backgroundColor = button.BackgroundColor;
        _vocalization = button.Vocalization;
        _action = button.Action;
        _loadBoardPath = button.LoadBoard?.Path;

        // If the original button has an ImageId, find the matching Image object in the list
        var imagesList = availableImages.ToList();
        AvailableImages = imagesList;

        var foundImage = imagesList.FirstOrDefault(img => img.Id == button.ImageId);
        _selectedImage = foundImage; // If found, this will pre-select it in the ComboBox
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

    public Image? SelectedImage
    {
        get => _selectedImage;
        set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
    }

    public string BorderColor
    {
        get => _borderColor;
        set => this.RaiseAndSetIfChanged(ref _borderColor, value);
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set => this.RaiseAndSetIfChanged(ref _backgroundColor, value);
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

    #endregion

    #region Methods

    private void Confirm()
    {
        // Write fields back to the original button
        _originalButton.Id = _id;
        _originalButton.Label = _label;
        _originalButton.BorderColor = _borderColor;
        _originalButton.BackgroundColor = _backgroundColor;
        _originalButton.Vocalization = _vocalization;
        _originalButton.Action = _action;

        // If user picked an image from the combo, store its ID
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

    private void CloseWindow()
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;
        var topWindow = desktop.Windows[^1];
        topWindow.Close();
    }

    #endregion
}