using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Threading.Tasks;
using Windows.UI.Core;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace RpiGpioTool
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GpioController _gpioController = null;
        private GpioPin[] _gpioPinList = null;
        private GpioPin _selectedPin = null;
        private List<Task> _pulsGeneratorList = null;

        /// <summary>
        /// Initilizing the on board gpio controller.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeGpio()
        {
            lbxLogs.Items.Add("Beginn initialize GPIO Controller.");
            _gpioController = await GpioController.GetDefaultAsync();

            lbxLogs.Items.Add("GPIO Controller Pin Count: " + _gpioController.PinCount);

            // Show an error if there is no GPIO controller
            if (_gpioController == null)
            {
                lbxLogs.Items.Add("ERROR: GPIO Controller not found.");
            }

            _gpioPinList = new GpioPin[]
            {
                _gpioController.OpenPin(2),
                _gpioController.OpenPin(3),
                _gpioController.OpenPin(4),
                _gpioController.OpenPin(5),
                _gpioController.OpenPin(6),
                _gpioController.OpenPin(7),
                _gpioController.OpenPin(8),
                _gpioController.OpenPin(9),
                _gpioController.OpenPin(10),
                _gpioController.OpenPin(11),
                _gpioController.OpenPin(12),
                _gpioController.OpenPin(13),
                _gpioController.OpenPin(16),
                _gpioController.OpenPin(17),
                _gpioController.OpenPin(18),
                _gpioController.OpenPin(19),
                _gpioController.OpenPin(20),
                _gpioController.OpenPin(21),
                _gpioController.OpenPin(22),
                _gpioController.OpenPin(23),
                _gpioController.OpenPin(24),
                _gpioController.OpenPin(25),
                _gpioController.OpenPin(26),
                _gpioController.OpenPin(27)
            };

            foreach (GpioPin pin in _gpioPinList)
            {
                lbxLogs.Items.Add("GpioPin: "  + pin.PinNumber + " Current drive mode: " + pin.GetDriveMode().ToString() + " Current level: " + pin.Read().ToString());
            }

            //_currentPin = _gpioController.OpenPin(17);
            //_currentPin.Write(GpioPinValue.High);
            //_currentPin.SetDriveMode(GpioPinDriveMode.Output);

            lbxLogs.Items.Add("End initialize GPIO Controller.");
        }

        /// <summary>
        /// Initializing the ui of the app.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeUi()
        {
            tbxDriveModeMsg.Visibility = Visibility.Collapsed;

            lbxLogs.Items.Add("Begin initialize UI.");
            if (_gpioPinList != null)
            {
                cbxGpioSelector.Items.Clear();

                for (int i = 0; i < _gpioPinList.Length; i++)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = "GPIO " + _gpioPinList[i].PinNumber;
                    cbxGpioSelector.Items.Add(item);
                }
            }

            lbxLogs.Items.Add("End initialize UI.");
        }

        /// <summary>
        /// Check the driving mode of the selected gpio pin.
        /// </summary>
        /// <returns></returns>
        private async Task CheckDriveMode()
        {
            if (_selectedPin.GetDriveMode() == GpioPinDriveMode.Input || _selectedPin.GetDriveMode() == GpioPinDriveMode.InputPullDown || _selectedPin.GetDriveMode() == GpioPinDriveMode.InputPullUp)
            {
                tglSwGpioLevel.IsEnabled = false;
                btnGpioPushSwitch.IsEnabled = false;
                tbxTLow.IsEnabled = false;
                tbxTHigh.IsEnabled = false;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    tbxDriveModeMsg.Text = "The pin is currently not in output drive mode.";
                    tbxDriveModeMsg.Visibility = Visibility.Visible;
                });

                btnSwitchDriveMode.Visibility = Visibility.Visible;
            }
            else
            {
                tglSwGpioLevel.IsEnabled = true;
                btnGpioPushSwitch.IsEnabled = true;
                tbxTLow.IsEnabled = true;
                tbxTHigh.IsEnabled = true;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    tbxDriveModeMsg.Visibility = Visibility.Collapsed;
                });

                btnSwitchDriveMode.Visibility = Visibility.Collapsed;
            }
        }

        private void Pulse(int tLow, int tHigh)
        {
            while (true)
            {
                _selectedPin.Write(GpioPinValue.Low);
                Task.Delay(tLow);

                _selectedPin.Write(GpioPinValue.High);
                Task.Delay(tHigh);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void cbxGpioSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxGpioSelector.SelectedIndex != -1)
            {
                ComboBoxItem item = (ComboBoxItem)cbxGpioSelector.SelectedItem;
                int pinNumber = Int32.Parse(item.Content.ToString().Remove(0, 4));
                _selectedPin = _gpioPinList.SingleOrDefault(x => x.PinNumber == pinNumber);

                await CheckDriveMode();

                if (_selectedPin.Read() == GpioPinValue.High)
                {
                    tglSwGpioLevel.IsOn = true;
                }
                else
                {
                    tglSwGpioLevel.IsOn = false;
                }

            }
            else
            {
                _selectedPin = null;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeGpio();
            await InitializeUi();

            _pulsGeneratorList = new List<Task>();
        }

        private async void btnSwitchDriveMode_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                _selectedPin.SetDriveMode(GpioPinDriveMode.Output);
                _selectedPin.Write(GpioPinValue.Low);
                await CheckDriveMode();
            }
        }

        private void tglSwGpioLevel_Toggled(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                if (tglSwGpioLevel.IsOn)
                {
                    _selectedPin.Write(GpioPinValue.High);
                }
                else
                {
                    _selectedPin.Write(GpioPinValue.Low);
                }
            }
        }

        private void btnGpioPushSwitch_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                _selectedPin.Write(GpioPinValue.High);
            }
        }

        private void btnGpioPushSwitch_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                _selectedPin.Write(GpioPinValue.Low);
            }
        }

        private async void tglSwPulseGenerator_Toggled(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                Task pulseTask = null;

                if (tglSwPulseGenerator.IsOn)
                {
                    int low = Int32.Parse(tbxTLow.Text);
                    int high = Int32.Parse(tbxTHigh.Text);

                    pulseTask = new Task(() => Pulse(low, high));
                    pulseTask.Start();
                }
                else
                {
                }
            }
        }
    }
}
