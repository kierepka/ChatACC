using System.Threading.Tasks;

namespace ChatAAC.Services;

public interface ITtsService
{
    Task SpeakAsync(string text);
}