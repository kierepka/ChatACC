using System;
using Avalonia;
using Avalonia.Controls;

namespace ChatAAC.Helpers;

public static class ButtonStyleHelper
{
    public static readonly AttachedProperty<object> ActionProperty =
        AvaloniaProperty.RegisterAttached<Button, object>("LoadBoard", typeof(ButtonStyleHelper));

    static ButtonStyleHelper()
    {
        ActionProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<object>>(
            e =>
            {
                if (e.Sender is Button button) UpdateButtonClasses(button, e.NewValue.Value);
            }));
    }

    public static object GetAction(Button button)
    {
        return button.GetValue(ActionProperty);
    }

    public static void SetAction(Button button, object? value)
    {
        button.SetValue(ActionProperty!, value);
        UpdateButtonClasses(button, value);
    }

    private static void UpdateButtonClasses(Button button, object? actionValue)
    {
        if (actionValue is not null)
            button.Classes.Add("action");
        else
            button.Classes.Remove("action");

        if (!button.Classes.Contains("symbol")) button.Classes.Add("symbol");
    }
}

internal class AnonymousObserver<T>(Action<T> onNext) : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(T value)
    {
        onNext(value);
    }
}