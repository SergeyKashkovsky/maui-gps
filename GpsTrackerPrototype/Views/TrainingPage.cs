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
}
