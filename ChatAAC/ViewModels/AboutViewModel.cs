using System;
using System.Reactive;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public AboutViewModel()
    {
        CloseInteraction = new Interaction<Unit, Unit>();
        CloseCommand = ReactiveCommand.Create(() =>
        {
            // Trigger the CloseInteraction to close the window
            CloseInteraction.Handle(Unit.Default).Subscribe();
        });

        // Handle exceptions from the command
        CloseCommand.ThrownExceptions.Subscribe(ex =>
        {
            Console.WriteLine($"Error in CloseCommand: {ex.Message}");
        });
    }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public Interaction<Unit, Unit> CloseInteraction { get; }
}