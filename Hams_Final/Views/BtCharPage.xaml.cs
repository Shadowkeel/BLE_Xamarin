using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinEssentials = Xamarin.Essentials;
namespace Hams_Final.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtCharPage : ContentPage
    {
        private readonly IDevice _connectedDevice;                                     
        private readonly IService _selectedService;                                   
        private readonly List<ICharacteristic> _charList = new List<ICharacteristic>();
        private ICharacteristic _char;                                                 

        public BtCharPage(IDevice connectedDevice, IService selectedService)       
        {
            InitializeComponent();

            _connectedDevice = connectedDevice;
            _selectedService = selectedService;
            _char = null;                                                          

            bleDevice.Text = "Selected BLE device: " + _connectedDevice.Name;          
            bleService.Text = "Selected BLE service: " + _selectedService.Name;
        }

        protected async override void OnAppearing()                                   
        {
            base.OnAppearing();
            try
            {
                if (_selectedService != null)
                {
                    var charListReadOnly = await _selectedService.GetCharacteristicsAsync();     

                    _charList.Clear();
                    var charListStr = new List<String>();
                    for (int i = 0; i < charListReadOnly.Count; i++)                             
                    {
                        _charList.Add(charListReadOnly[i]);                                      
                       
                        charListStr.Add(i.ToString() + ": " + charListReadOnly[i].Name);        
                    }
                    foundBleChars.ItemsSource = charListStr;                                    
                }
                else
                {
                    ErrorLabel.Text = GetTimeNow() + "UART GATT service not found." + Environment.NewLine;
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void FoundBleChars_ItemTapped(object sender, ItemTappedEventArgs e)     
        {

            _char = _charList[e.ItemIndex];                                                    
            if (_char != null)                                                                 
            {
                bleChar.Text = _char.Name + "\n" +                                            
                    "UUID: " + _char.Uuid.ToString() + "\n" +
                    "Read: " + _char.CanRead + "\n" +                                           
                    "Write: " + _char.CanWrite + "\n" +                                        
                    "Update: " + _char.CanUpdate;                                              

                var charDescriptors = await _char.GetDescriptorsAsync();                       

                bleChar.Text += "\nDescriptors (" + charDescriptors.Count + "): ";             
                for (int i = 0; i < charDescriptors.Count; i++)
                    bleChar.Text += charDescriptors[i].Name + ", ";
            }
        }

        private async void RegisterCommandButton_Clicked(object sender, EventArgs e)                  
        {
            try
            {
                if (_char != null)                                                                   
                {
                   
                    if (_char.CanUpdate)                                                             
                    {
                        _char.ValueUpdated += (o, args) =>                                            
                        {
                            var receivedBytes = args.Characteristic.Value;                             
                            Console.WriteLine("byte array: " + BitConverter.ToString(receivedBytes));  


                            string _charStr = "";                                                                         
                            if (receivedBytes != null)
                            {
                                _charStr = "Bytes: " + BitConverter.ToString(receivedBytes);                               
                                _charStr += " | UTF8: " + Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length); 
                            }

                            if (receivedBytes.Length <= 4)
                            {                                                                                           
                                int char_val = 0;
                                for (int i = 0; i < receivedBytes.Length; i++)
                                {
                                    char_val |= (receivedBytes[i] << i * 8);
                                }
                                _charStr += " | int: " + char_val.ToString();
                            }
                            _charStr += Environment.NewLine;                                                              

                            XamarinEssentials.MainThread.BeginInvokeOnMainThread(() =>                                    
                            {
                                Output.Text += _charStr;
                            });

                        };
                        await _char.StartUpdatesAsync();

                        ErrorLabel.Text = GetTimeNow() + ": Notify callback function registered successfully.";
                    }
                    else
                    {
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not have a notify function.";
                    }
                }
                else
                {
                    ErrorLabel.Text = GetTimeNow() + ": No characteristic selected.";
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error initializing UART GATT service.";
            }
        }

        private async void ReceiveCommandButton_Clicked(object sender, EventArgs e)              
        {
            try
            {
                if (_char != null)                                                             
                {
                 
                    if (_char.CanRead)                                                           
                    {
                        var receivedBytes = await _char.ReadAsync();
                        Output.Text += Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length) + Environment.NewLine; 
                    }
                    else
                    {
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not support read.";
                    }
                }
                else
                    ErrorLabel.Text = GetTimeNow() + ": No Characteristic selected.";
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error receiving Characteristic.";
            }
        }
        private async void SendCommandButton_Clicked(object sender, EventArgs e)                  
        {
            try
            {
                if (_char != null)                                                                  
                {
                 
                    if (_char.CanWrite)                                                           
                    {
                        byte[] array = Encoding.UTF8.GetBytes(CommandTxt.Text);              
                        
                        await _char.WriteAsync(array);                                           
                    }
                    else
                    {
                        ErrorLabel.Text = GetTimeNow() + ": Characteristic does not support Write";
                    }
                }
            }
            catch
            {
                ErrorLabel.Text = GetTimeNow() + ": Error receiving Characteristic.";
            }
        }

        private string GetTimeNow()
        {
            var timestamp = DateTime.Now;
            return timestamp.Hour.ToString() + ":" + timestamp.Minute.ToString() + ":" + timestamp.Second.ToString();
        }
    }
}