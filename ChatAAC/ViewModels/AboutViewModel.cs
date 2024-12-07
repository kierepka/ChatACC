using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public AboutViewModel()
    {
        CloseInteraction = new Interaction<Unit, Unit>();
        CloseCommand = ReactiveCommand.CreateFromTask(async () => { await CloseInteraction.Handle(Unit.Default); });

        // Obsługa wyjątków
        CloseCommand.ThrownExceptions.Subscribe(ex =>
        {
            // Logowanie lub obsługa błędów
            Console.WriteLine($"Błąd w CloseCommand: {ex.Message}");
        });
    }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public Interaction<Unit, Unit> CloseInteraction { get; }
}