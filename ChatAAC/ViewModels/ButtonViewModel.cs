using ChatAAC.Models.Obf;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class ButtonViewModel(Button button, int row, int column) : ReactiveObject
{
    public Button Button { get; set; } = button;
    public int Row { get; set; } = row;
    public int Column { get; set; } = column;
}