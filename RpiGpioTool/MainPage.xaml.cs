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
using Windows.System.Threading;

namespace RpiGpioTool
{
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// A Delegate type to change status of an physical pin by changing the back color of the canvas.
        /// </summary>
        /// <param name="physicalPin">The physical ping of the gpio port on the raspberry pi.</param>
        private delegate void VisualizePinLevelHandler(int physicalPin);

        /// <summary>
        /// A instance of a <see cref="VisualizePinLevelHandler"/> Delegate to showing the high status of an physical pin by light grey back color of the canvas. 
        /// </summary>
        private VisualizePinLevelHandler pinHigh = null;

        /// <summary>
        /// A instance of a <see cref="VisualizePinLevelHandler"/> Delegate to showing the low status of an physical pin by deep grey back color of the canvas. 
        /// </summary>
        private VisualizePinLevelHandler pinLow = null;

        /// <summary>
        /// A instance of gpio controller of the raspberry pi.
        /// </summary>
        private GpioController _gpioController = null;

        /// <summary>
        /// A list that contains all gpio pins and their respective physical pin numbers.
        /// </summary>
        private List<KeyValuePair<int, GpioPin>> _pinList; //Key = Physical Pin, Value = GPIO Pin

        /// <summary>
        /// A field to store the instance of selected gpio pin.
        /// </summary>
        private GpioPin _selectedPin = null;

        /// <summary>
        /// A <see cref="CancellationTokenSource"/> instance to stop the pulse generator.
        /// </summary>
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

            _pinList = new List<KeyValuePair<int, GpioPin>>()
            {
                new KeyValuePair<int, GpioPin>(1, null),
                new KeyValuePair<int, GpioPin>(2, null),
                new KeyValuePair<int, GpioPin>(3, _gpioController.OpenPin(2)),
                new KeyValuePair<int, GpioPin>(4, null),
                new KeyValuePair<int, GpioPin>(5, _gpioController.OpenPin(3)),
                new KeyValuePair<int, GpioPin>(6, null),
                new KeyValuePair<int, GpioPin>(7, _gpioController.OpenPin(4)),
                new KeyValuePair<int, GpioPin>(8, null),
                new KeyValuePair<int, GpioPin>(9, null),
                new KeyValuePair<int, GpioPin>(10, null),
                new KeyValuePair<int, GpioPin>(11, _gpioController.OpenPin(17)),
                new KeyValuePair<int, GpioPin>(12, _gpioController.OpenPin(18)),
                new KeyValuePair<int, GpioPin>(13, _gpioController.OpenPin(27)),
                new KeyValuePair<int, GpioPin>(14, null),
                new KeyValuePair<int, GpioPin>(15, _gpioController.OpenPin(22)),
                new KeyValuePair<int, GpioPin>(16, _gpioController.OpenPin(23)),
                new KeyValuePair<int, GpioPin>(17, null),
                new KeyValuePair<int, GpioPin>(18, _gpioController.OpenPin(24)),
                new KeyValuePair<int, GpioPin>(19, _gpioController.OpenPin(10)),
                new KeyValuePair<int, GpioPin>(20, null),
                new KeyValuePair<int, GpioPin>(21, _gpioController.OpenPin(9)),
                new KeyValuePair<int, GpioPin>(22, _gpioController.OpenPin(25)),
                new KeyValuePair<int, GpioPin>(23, _gpioController.OpenPin(11)),
                new KeyValuePair<int, GpioPin>(24, _gpioController.OpenPin(8)),
                new KeyValuePair<int, GpioPin>(25, null),
                new KeyValuePair<int, GpioPin>(26, _gpioController.OpenPin(7)),
                new KeyValuePair<int, GpioPin>(27, null),
                new KeyValuePair<int, GpioPin>(28, null),
                new KeyValuePair<int, GpioPin>(29, _gpioController.OpenPin(5)),
                new KeyValuePair<int, GpioPin>(30, null),
                new KeyValuePair<int, GpioPin>(31, _gpioController.OpenPin(6)),
                new KeyValuePair<int, GpioPin>(32, _gpioController.OpenPin(12)),
                new KeyValuePair<int, GpioPin>(33, _gpioController.OpenPin(13)),
                new KeyValuePair<int, GpioPin>(34, null),
                new KeyValuePair<int, GpioPin>(35, _gpioController.OpenPin(19)),
                new KeyValuePair<int, GpioPin>(36, _gpioController.OpenPin(16)),
                new KeyValuePair<int, GpioPin>(37, _gpioController.OpenPin(26)),
                new KeyValuePair<int, GpioPin>(38, _gpioController.OpenPin(20)),
                new KeyValuePair<int, GpioPin>(39, null),
                new KeyValuePair<int, GpioPin>(40, _gpioController.OpenPin(21))
            };

            _pinList.ForEach((pin) =>
            {
                if (pin.Value != null)
                {
                    pin.Value.ValueChanged += PinLevel_ValueChanged;
                }
            });

            lbxLogs.Items.Add("End initialize GPIO Controller.");
        }

        /// <summary>
        /// Triggered if the value of the gpio pin has changed.
        /// </summary>
        /// <param name="sender">The gpio pin that has triggered the event.</param>
        /// <param name="args"></param>
        private void PinLevel_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (sender.Read() == GpioPinValue.High)
            {
                pinHigh(_pinList.Single(x => x.Value != null && x.Value.PinNumber == sender.PinNumber).Key);
            }
            else
            {
                pinLow(_pinList.Single(x => x.Value != null && x.Value.PinNumber == sender.PinNumber).Key);
            }
        }

        /// <summary>
        /// Initializing the ui of the app.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeUi()
        {
            lbxLogs.Items.Add("Begin initialize UI.");
            if (_pinList != null)
            {
                cbxGpioSelector.Items.Clear();

                foreach (KeyValuePair<int, GpioPin> pin in _pinList)
                {
                    if (pin.Value != null)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = pin.Value.PinNumber;
                        cbxGpioSelector.Items.Add(item);
                    }
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
            }
        }

        /// <summary>
        /// Showing the high status of an physical pin by light grey back color of the canvas.
        /// </summary>
        /// <param name="physicalPin">The number of physical pin to change the back color.</param>
        private async void VisualizePinHigh(int physicalPin)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Border b = cnvsGpio.FindName("physicalPin" + physicalPin) as Border;
                b.Background = this.Resources["BrushLightGrey"] as SolidColorBrush;
            });
        }

        /// <summary>
        /// Showing the low status of an physical pin by deep grey back color of the canvas.
        /// </summary>
        /// <param name="physicalPin">The number of physical pin to change the back color.</param>
        private async void VisualizePinLow(int physicalPin)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Border b = cnvsGpio.FindName("physicalPin" + physicalPin) as Border;
                b.Background = this.Resources["BrushDeepGrey"] as SolidColorBrush;
            });

        }

        /// <summary>
        /// A pulse generator with specifiec low and high time. THE MAGIC IS, DO NOT CALL THIS METHOD WITH AWAIT!
        /// </summary>
        /// <param name="tLow">The time of the low signal.</param>
        /// <param name="tHigh">The time of the high signal.</param>
        /// <returns></returns>
        private async Task RunInfinitePulseTask(int tLow, int tHigh)
        {
            _cancelationTokenSource = new CancellationTokenSource();

            Task t = Task.Factory.StartNew(async () =>
            {
                CancellationToken ct = _cancelationTokenSource.Token;
                ct.ThrowIfCancellationRequested();

                while (ct.IsCancellationRequested == false)
                {
                    _selectedPin.Write(GpioPinValue.Low);
                    await Task.Delay(tLow);

                    _selectedPin.Write(GpioPinValue.High);
                    await Task.Delay(tHigh);
                }

                t = null;

            }, _cancelationTokenSource.Token);
        }

        /// <summary>
        /// Main page constructor that initialize the ui components.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Triggered if the selected index has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void cbxGpioSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxGpioSelector.SelectedIndex != -1)
            {
                ComboBoxItem item = (ComboBoxItem)cbxGpioSelector.SelectedItem;
                int pinNumber = Int32.Parse(item.Content.ToString());
                _selectedPin = _pinList.Single(x => x.Value != null && x.Value.PinNumber == pinNumber).Value;

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

                btnStartPulseGenerator.IsEnabled = false;
                btnStopPulseGenerator.IsEnabled = false;
                tglSwDriveMode.IsEnabled = false;
                tglSwGpioLevel.IsEnabled = false;
                tbxTLow.IsEnabled = false;
                tbxTHigh.IsEnabled = false;
            }
        }

        /// <summary>
        /// Initialize gpio and ui and create some instances.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //HACK: Comment out if you want to debug the app on local computer.
            await InitializeGpio();
            await InitializeUi();

            cbxGpioSelector.SelectedIndex = -1;

            pinHigh = new VisualizePinLevelHandler(VisualizePinHigh);
            pinLow = new VisualizePinLevelHandler(VisualizePinLow);

            foreach (GpioPin pin in _pinList.Where(x => x.Value != null).Select(x => x.Value))
            {
                lbxLogs.Items.Add("GPIO" + pin.PinNumber + " Drive Mode: " + pin.GetDriveMode().ToString());
            }
        }

        /// <summary>
        /// Triggered to change the level of gpio pin value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Triggered to change the drive mode of a gpio pin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

                //UpdatePinoutOverview();
                await CheckDriveMode();
                
                if (_selectedPin.Read() == GpioPinValue.High)
                {
                    tglSwGpioLevel.IsOn = true;
                }
            }
        }

        /// <summary>
        /// Starts the infinite puls generator of selected gpio pin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartPulseGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPin != null)
            {
                int low = Int32.Parse(tbxTLow.Text);
                int high = Int32.Parse(tbxTHigh.Text);

                btnStartPulseGenerator.IsEnabled = false;
                btnStopPulseGenerator.IsEnabled = true;
                cbxGpioSelector.IsEnabled = false;
                tglSwDriveMode.IsEnabled = false;
                tglSwGpioLevel.IsEnabled = false;
                tbxTLow.IsEnabled = false;
                tbxTHigh.IsEnabled = false;

                RunInfinitePulseTask(low, high);
            }
        }

        /// <summary>
        /// Stops the infinite puls generator of selected gpio pin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopPulseGenerator_Click(object sender, RoutedEventArgs e)
        {
            _cancelationTokenSource.Cancel();

            btnStartPulseGenerator.IsEnabled = true;
            btnStopPulseGenerator.IsEnabled = false;
            cbxGpioSelector.IsEnabled = true;
            tglSwDriveMode.IsEnabled = true;
            tglSwGpioLevel.IsEnabled = true;
            tbxTLow.IsEnabled = true;
            tbxTHigh.IsEnabled = true;

            if (_selectedPin.Read() == GpioPinValue.High)
            {
                tglSwGpioLevel.IsOn = true;
            }
            else
            {
                tglSwGpioLevel.IsOn = false;
            }
        }
    }
}
