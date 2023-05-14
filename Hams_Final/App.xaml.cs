using Hams_Final.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Hams_Final
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var navigationPage = new NavigationPage(new BtDevPage());
            MainPage = navigationPage;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
