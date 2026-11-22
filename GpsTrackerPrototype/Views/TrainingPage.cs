using GpsTrackerPrototype.ViewModels;

namespace GpsTrackerPrototype.Views;
/// <summary>
/// Оено тренировки
/// </summary>
public partial class TrainingPage : ContentPage
{
    /// <summary>
    /// Окно тренировки
    /// </summary>
    /// <param name="vm"></param>
    public TrainingPage(TrainingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    /// <summary>
    /// Переопределение кнопки назад с защитой от случайного нажатия (запрос разрешения в диалоговом окне)
    /// </summary>
    /// <returns></returns>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is TrainingViewModel vm && vm.IsRecording)
        {
            _ = vm.StopCommand.ExecuteAsync(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
