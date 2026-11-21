using GpsTrackerPrototype.ViewModels;

namespace GpsTrackerPrototype.Views;

public partial class SecondPage : ContentPage
{
    public SecondPage(SecondViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
