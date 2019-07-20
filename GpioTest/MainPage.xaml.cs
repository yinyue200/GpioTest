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
using Windows.UI.Popups;
using Windows.Devices.Pwm;
using Microsoft.IoT.DeviceCore.Pwm;
using Microsoft.IoT.Devices.Pwm;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;
using Windows.ApplicationModel.Core;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GpioTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        GpioController gpioController;
        GpioPin gpioPin;
        PwmPin pwmPin;
        PwmController pwmController;

        public MainPage()
        {
            this.InitializeComponent();
        }
        public async Task load()
        {
            try
            {
                switch (pwmselect.SelectedIndex)
                {
                    case 1:
                        if (LightningProvider.IsLightningEnabled)
                        {
                            LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                            var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                            pwmController = pwmControllers[int.Parse(pwmidhd.Text)]; // use the on-device controller
                            pwmController.SetDesiredFrequency(double.Parse(frahz.Text));

                            //var _pin = pwmController.OpenPin(22);
                            //_pin.SetActiveDutyCyclePercentage(.25);
                            //_pin.Start();
                        }
                        else
                        {
                            await new MessageDialog("驱动程序不正常").ShowAsync();
                        }
                        break;
                    case 0:
                        {
                            var pwmManager = new PwmProviderManager();
                            pwmManager.Providers.Add(new SoftPwm());
                            var pwmControllers = await pwmManager.GetControllersAsync();

                            //use the first available PWM controller an set refresh rate (Hz)
                            pwmController = pwmControllers[0];
                            pwmController.SetDesiredFrequency(double.Parse(frahz.Text));
                        }

                        break;
                    default:
                        gpioController = await GpioController.GetDefaultAsync();
                        break;
                }
                pwmselect.IsEnabled = false;

            }
            catch(Exception err)
            {
                await new MessageDialog("初始化设备失败："+err.ToString()).ShowAsync();
                throw;
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await load();
            try
            {
                var id = int.Parse(gpio.Text);
                switch (pwmselect.SelectedIndex)
                {
                    case 1:
                    case 0:
                        {
                            gpioPin?.Dispose();
                            gpioPin = null;
                            pwmPin?.Dispose();
                            pwmPin = null;
                            try
                            {
                                pwmPin = pwmController.OpenPin(id);
                            }
                            catch (Exception err)
                            {
                                _ = new MessageDialog("打开指定硬件PWM GPIO接口失败" + err.ToString()).ShowAsync();
                            }
                        }
                        break;
                    default:
                        {
                            if (gpioController.TryOpenPin(id, GpioSharingMode.Exclusive, out var pin, out var gpioOpenStatus))
                            {
                                pwmPin?.Dispose();
                                pwmPin = null;
                                gpioPin?.Dispose();
                                gpioPin = null;
                                gpioPin = pin;
                            }
                            else
                            {
                                _ = new MessageDialog("打开指定GPIO接口失败").ShowAsync();
                            }
                        }
                        break;
                }

            }
            catch (Exception err1)
            {
                await new MessageDialog("建立连接失败：" + err1.ToString()).ShowAsync();
                throw;
            }
            
        }

        private void Gpio_Unloaded(object sender, RoutedEventArgs e)
        {
            gpioPin?.Dispose();
            gpioPin = null;
            pwmPin?.Dispose();
            pwmPin = null;
        }
        bool isenable = false;
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {

            try
            {
                if (isenable)
                {
                    if (gpioPin != null)
                    {
                        gpioPin.Write(GpioPinValue.Low);
                    }
                    else if (pwmPin != null)
                    {
                        pwmPin.Stop();
                    }
                    startbtn.Content = "启动";
                    isenable = false;
                }
                else
                {
                    if (gpioPin != null)
                    {
                        gpioPin.SetDriveMode(GpioPinDriveMode.Input);
                        gpioPin.Write(GpioPinValue.High);
                        startbtn.Content = "停止";
                        isenable = true;
                    }
                    else if (pwmPin != null)
                    {
                        pwmPin.SetActiveDutyCyclePercentage(double.Parse(tcdptxt.Text));
                        pwmPin.Start();
                        startbtn.Content = "停止PWM";
                        isenable = true;
                    }
                }
            }
            catch(Exception err1)
            {
                await new MessageDialog("状态变更失败：" + err1.ToString()).ShowAsync();
                throw;
            }
            finally
            {

            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await CoreApplication.RequestRestartAsync(string.Empty);
        }
    }
}
