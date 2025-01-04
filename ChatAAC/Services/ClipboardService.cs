using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ChatAAC.Lang;

namespace ChatAAC.Services
{
    public static class ClipboardService
    {
        public static void CopyToClipboard(string textToClipboard)
        {
            if (string.IsNullOrEmpty(textToClipboard))
                return;

            Dispatcher.UIThread.Post(() =>
            {
                switch (Application.Current?.ApplicationLifetime)
                {
                    case IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }:
                        window.Clipboard?.SetTextAsync(textToClipboard);
                        break;
                    case ISingleViewApplicationLifetime { MainView: { } mainView }:
                        if (mainView.GetVisualRoot() is TopLevel topLevel)
                            topLevel.Clipboard?.SetTextAsync(textToClipboard);
                        break;
                    default:
                        Console.WriteLine(Resources.ClipboardService_CopyToClipboard_Clipboard_is_not_available_);
                        break;
                }
            });
        }
    }
}