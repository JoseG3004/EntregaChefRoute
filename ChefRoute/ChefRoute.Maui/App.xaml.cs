namespace ChefRoute.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage());
            window.Title = "ChefRoute — Cocina Interactiva & Grafo de Preparacion";
            window.Width = 1340;
            window.Height = 860;
            window.MinimumWidth = 1060;
            window.MinimumHeight = 720;
            return window;
        }
    }
}
