
using System;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;


namespace ChatAAC.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public Interaction<Unit, Unit> CloseInteraction { get; }
    public AboutViewModel()
    {
        CloseInteraction = new Interaction<Unit, Unit>();
        CloseCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await CloseInteraction.Handle(Unit.Default);
        });
        
        // Obsługa wyjątków
        CloseCommand.ThrownExceptions.Subscribe(ex =>
        {
            // Logowanie lub obsługa błędów
            Console.WriteLine($"Błąd w CloseCommand: {ex.Message}");
        });
    }

  
}