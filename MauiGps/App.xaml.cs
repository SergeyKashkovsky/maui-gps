namespace MauiGps;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }
    /// <inheritdoc/>
    /// <remarks>Через систему DI получаем главную страницу со внедренной зависимостью от представления</remarks>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = _services.GetRequiredService<MainPage>();
        return new Window(page);
    }
}