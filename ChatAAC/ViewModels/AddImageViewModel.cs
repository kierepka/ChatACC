using ReactiveUI;
using System.Reactive;
using ChatAAC.Models.Obf;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace ChatAAC.ViewModels;

public class AddImageViewModel : ReactiveObject
{
    private string _id = "";
    private string _url = "";
    private string _dataUrl = "";
    private string _path = "";
    private string _contentType = "image/png";
    private int _width;
    private int _height;

    public bool IsConfirmed { get; private set; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public AddImageViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    #region Properties
    public string Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    public string DataUrl
    {
        get => _dataUrl;
        set => this.RaiseAndSetIfChanged(ref _dataUrl, value);
    }

    public string Path
    {
        get => _path;
        set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    public string ContentType
    {
        get => _contentType;
        set => this.RaiseAndSetIfChanged(ref _contentType, value);
    }

    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }
    #endregion

    private void Confirm()
    {
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
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Windows[^1].Close();
        }
    }

    /// <summary>
    /// Creates a new Image object from the user inputs.
    /// </summary>
    public Image CreateImage()
    {
        return new Image
        {
            Id = _id,
            Url = _url,
            DataUrl = _dataUrl,
            Path = _path,
            ContentType = _contentType,
            Width = _width,
            Height = _height
        };
    }
}