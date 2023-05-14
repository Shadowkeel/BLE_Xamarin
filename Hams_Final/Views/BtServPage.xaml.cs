using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinEssentials = Xamarin.Essentials;

namespace Hams_Final.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtServPage : ContentPage
    {
        private readonly IDevice _connectedDevice;                             
        private readonly List<IService> _servicesList = new List<IService>();  

        public BtServPage(IDevice connectedDevice)                            
        {
            InitializeComponent();

            _connectedDevice = connectedDevice;                                
            bleDevice.Text = "Selected BLE device: " + _connectedDevice.Name;   
        }

        protected async override void OnAppearing()                            
        {
            base.OnAppearing();

            try
            {
                var servicesListReadOnly = await _connectedDevice.GetServicesAsync();          

                _servicesList.Clear();
                var servicesListStr = new List<String>();
                for (int i = 0; i < servicesListReadOnly.Count; i++)                            
                {
                    _servicesList.Add(servicesListReadOnly[i]);                                
                    servicesListStr.Add(servicesListReadOnly[i].Name + ", UUID: " + servicesListReadOnly[i].Id.ToString());                         
                }
                foundBleServs.ItemsSource = servicesListStr;                                  
            }
            catch
            {
                await DisplayAlert("Error initializing", $"Error initializing UART GATT service.", "OK");
            }
        }

        private async void FoundBleServs_ItemTapped(object sender, ItemTappedEventArgs e)       
        {
            var selectedService = _servicesList[e.ItemIndex];
            if (selectedService != null && _connectedDevice != null)                                                       
            {
                await Navigation.PushAsync(new BtCharPage(_connectedDevice, selectedService)); 
            }
        }
    }
}