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
using System.Threading;
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
        private List<Task> _pulseGeneratorTasks = null;
        private CancellationTokenSource _cancelationTokenSource = null;

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
                tglSwDriveMode.IsOn = false;

                tglSwGpioLevel.IsEnabled = false;
                tbxTLow.IsEnabled = false;
                tbxTHigh.IsEnabled = false;
                btnStartPulseGenerator.IsEnabled = false;
                btnStopPulseGenerator.IsEnabled = false;
            }
            else
            {
                tglSwDriveMode.IsOn = true;

                tglSwGpioLevel.IsEnabled = true;
                tbxTLow.IsEnabled = true;
                tbxTHigh.IsEnabled = true;
                btnStartPulseGenerator.IsEnabled = true;
                btnStopPulseGenerator.IsEnabled = true;
            }
        }

        /// <summary>
        /// A pulse generator with specifiec low and high time. THE MAGIC IS, DO NOT CALL THIS METHOD WITH AWAIT!
        /// </summary>
        /// <param name="tLow">The time of the low signal.</param>
        /// <param name="tHigh">The time of the high signal.</param>
        /// <returns></returns>
        private async Task RunInfinitePulseTask(int tLow, int tHigh)
        {
            CancellationToken ct = _cancelationTokenSource.Token;

            Task t = Task.Factory.StartNew(async () =>
            {
                ct.ThrowIfCancellationRequested();

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    _selectedPin.Write(GpioPinValue.Low);
                    await Task.Delay(tLow);

                    _selectedPin.Write(GpioPinValue.High);
                    await Task.Delay(tHigh);
                }
            }, _cancelationTokenSource.Token);
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

            _pulseGeneratorTasks = new List<Task>();
            _cancelationTokenSource = new CancellationTokenSource();
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

        private async void tglSwDriveMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                if (tglSwDriveMode.IsOn)
                {
                    _selectedPin.SetDriveMode(GpioPinDriveMode.Output);
                }
                else
                {
                    _selectedPin.SetDriveMode(GpioPinDriveMode.Input);
                }

                await CheckDriveMode();
            }
        }

        private void btnStartPulseGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                int low = Int32.Parse(tbxTLow.Text);
                int high = Int32.Parse(tbxTHigh.Text);

                RunInfinitePulseTask(low, high);
            }
        }

        private void btnStopPulseGenerator_Click(object sender, RoutedEventArgs e)
        {
            _cancelationTokenSource.Cancel();
        }
    }
}
