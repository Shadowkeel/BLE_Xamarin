using Google.Protobuf;
using Meshtastic.Protobufs;
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
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static System.Net.Mime.MediaTypeNames;
using XamarinEssentials = Xamarin.Essentials;
namespace Hams_Final.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtCharPage : ContentPage
    {
        private MyNodeInfo _myNodeInfo;
        private readonly IDevice _connectedDevice;                                     
        private readonly IService _selectedService;                                   
        private readonly List<ICharacteristic> _charList = new List<ICharacteristic>();
        List<byte> buffer = new List<byte>();
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

        private async Task AskForMyNodeInfo()
        {
            try
            {
                if (_char != null)
                {

                    if (_char.CanWrite)
                    {
                        byte[] array = Encoding.UTF8.GetBytes(CommandTxt.Text);
                        var id = new Random().Next(1, 1000);
                        var Packet = new ToRadio { WantConfigId = (uint)id };
                        var BytePacket = Packet.ToByteArray();

                        await _char.WriteAsync(BytePacket);
                        Output.Text += GetTimeNow() + " from you: sent want config with id = " + id + Environment.NewLine;
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
                        _char.ValueUpdated += OnValueUpdated;
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

        private void OnValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs args)
        {
            /*var receivedBytes = args.Characteristic.Value;
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
            });*/
            buffer.AddRange(args.Characteristic.Value);
        }

        private async void ReceiveCommandButton_Clicked(object sender, EventArgs e)              
        {
            try
            {
                if (_char != null)                                                             
                {
                 
                    if (_char.CanRead)                                                           
                    {
                        try
                        {
                            var receivedBytes = await _char.ReadAsync();
                            var bytes = _char.Value;
                            var a = await _char.GetDescriptorsAsync();
                            foreach (var descriptor in a)
                            {
                                /*Output.Text += descriptor.Value.ToString();*/
                                var value = descriptor.Value;
                                var fromRadio2 = FromRadio.Parser.ParseFrom(descriptor.Value);
                            }
                            var fromRadio = FromRadio.Parser.ParseFrom(receivedBytes);
                            try
                            {
                                var meshPacket = fromRadio.Packet;
                                var text = meshPacket.Decoded.Payload.ToString();
                                Output.Text += text + Environment.NewLine;
                            }
                            catch 
                            {
                                _myNodeInfo = fromRadio.MyInfo;
                            }
                        }
                        catch
                        {
                            var receivedBytes = await _char.ReadAsync();
                            var bytes = _char.Value;
                            Output.Text += Encoding.UTF8.GetString(receivedBytes) + Environment.NewLine;
                        }
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
                        var meshPacket = new MeshPacket
                        {
                            To = (0xffffffff),
                            Decoded = new Data
                            {
                                Payload = Google.Protobuf.ByteString.CopyFromUtf8(CommandTxt.Text),
                                Portnum = PortNum.TextMessageApp
                            },
                            WantAck = false
                        };
                        var packet = new ToRadio { Packet = meshPacket}.ToByteArray();

                        await _char.WriteAsync(packet);
                        Output.Text += GetTimeNow() + " from you: " + CommandTxt.Text + Environment.NewLine;

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
        private async void GetConfigCommandButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_char != null)
                {

                    if (_char.CanWrite)
                    {
                        byte[] array = Encoding.UTF8.GetBytes(CommandTxt.Text);
                        var meshPacket = new MeshPacket
                        {
                            To = uint.MaxValue,
                            Decoded = new Data
                            {
                                Payload = Google.Protobuf.ByteString.CopyFromUtf8(CommandTxt.Text),
                                Portnum = PortNum.TextMessageApp
                            },
                            WantAck = false
                        };
                        var packet = meshPacket.ToByteArray();

                        await _char.WriteAsync(packet);
                        Output.Text += GetTimeNow() + " from you: " + CommandTxt.Text + Environment.NewLine;

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