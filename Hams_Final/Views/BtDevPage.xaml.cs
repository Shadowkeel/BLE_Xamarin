using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Xamarin.Essentials.Permissions;
using XamarinEssentials = Xamarin.Essentials;

namespace Hams_Final.Views
{
    [DesignTimeVisible(false)]
    public partial class BtDevPage : ContentPage
    {
        private readonly IAdapter _bluetoothAdapter;                          
        private readonly List<IDevice> _gattDevices = new List<IDevice>();      

        public BtDevPage()                                                     
        {
            InitializeComponent();

            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;              
            _bluetoothAdapter.DeviceDiscovered += (sender, foundBleDevice) =>  
            {
                if (foundBleDevice.Device != null && !string.IsNullOrEmpty(foundBleDevice.Device.Name))
                    _gattDevices.Add(foundBleDevice.Device);
            };
        }

        private async Task<bool> PermissionsGrantedAsync()    
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return status == PermissionStatus.Granted;
        }

        private async void ScanButton_Clicked(object sender, EventArgs e)         
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = false);        
            foundBleDevicesListView.ItemsSource = null;                                                     

            if (!await PermissionsGrantedAsync())                                                         
            {
                await DisplayAlert("Permission required", "Application needs location permission", "OK");
                IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);
                return;
            }

            _gattDevices.Clear();                                                                           

            if (!_bluetoothAdapter.IsScanning)                                                              
            {
                await _bluetoothAdapter.StartScanningForDevicesAsync();
            }

            foreach (var device in _bluetoothAdapter.ConnectedDevices)                                    
                _gattDevices.Add(device);

            foundBleDevicesListView.ItemsSource = _gattDevices.ToArray();                                   
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);         
        }

        private async void FoundBluetoothDevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)   
        {
            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = false);        
            IDevice selectedItem = e.Item as IDevice;                                                      

            if (selectedItem.State == DeviceState.Connected)                                               
            {
                await Navigation.PushAsync(new BtServPage(selectedItem));                                  
            }
            else
            {
                try
                {
                    var connectParameters = new ConnectParameters(false, true);
                    await _bluetoothAdapter.ConnectToDeviceAsync(selectedItem, connectParameters);         
                    await Navigation.PushAsync(new BtServPage(selectedItem));                              
                }
                catch
                {
                    await DisplayAlert("Error connecting", $"Error connecting to BLE device: {selectedItem.Name ?? "N/A"}", "Retry");     
                }
            }

            IsBusyIndicator.IsVisible = IsBusyIndicator.IsRunning = !(ScanButton.IsEnabled = true);        
        }
    }
}