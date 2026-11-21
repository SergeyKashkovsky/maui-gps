namespace GpsTrackerPrototype
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Views.SecondPage), typeof(Views.SecondPage));
        }
    }
}
