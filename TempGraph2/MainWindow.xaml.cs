using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using LiveCharts;
using LiveCharts.Wpf;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using System.Management;


namespace TempGraph2
{
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public partial class MainWindow : Window
    {
        public SeriesCollection Temps { get; set; }
        private ISensor cpuSensor;
        private ISensor gpuSensor;
        private Timer timer = new Timer();
        private Computer melfina;

        public MainWindow()
        {
            melfina = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true
            };

            FindSensors();
            InitializeCharts();
            InitializeTimer();
            DataContext = this;
        }
        // Event Handlers
        private void Window_Closed(object sender, EventArgs e)
        {
            melfina.Close();
            timer.Dispose();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            cartesianChart.AxisY.Add(new Axis
            {
                Title = "Temperature (C)",
                LabelFormatter = value => value.ToString("F2")
            });
        }
        private void FindSensors()
        {
            melfina.Open();
            melfina.Accept(new UpdateVisitor());

            foreach (IHardware hardware in melfina.Hardware)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu && sensor.Name == "Core Average" && cpuSensor == null)
                        {
                            cpuSensor = sensor;
                            Console.WriteLine($"Found CPU Sensor: {cpuSensor.Name} Value: {cpuSensor.Value}");
                        }
                        if (hardware.HardwareType == HardwareType.GpuNvidia && gpuSensor == null)
                        {
                            gpuSensor = sensor;
                            Console.WriteLine($"Found GPU Sensor: {gpuSensor.Name} Value: {gpuSensor.Value}");
                        }
                    }
                }
            }
        }
        private void InitializeCharts()
        {
            Temps = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "CPU Temp",
                    Values = new ChartValues<float>()
                },
                new LineSeries
                {
                    Title = "GPU Temp",
                    Values = new ChartValues<float>()
                }
            };
        }
        private void InitializeTimer()
        {
            timer.Interval = 500;  
            timer.Elapsed += CollectData;  
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void CollectData(object sender, ElapsedEventArgs e)
        {
            if (cpuSensor != null)
            {
                cpuSensor.Hardware.Update();
                double? cpuTemp = cpuSensor.Value;
                Dispatcher.Invoke(() => {
                    if (cpuTemp.HasValue)
                    {
                        if (cpuSensor.Values.Count() == 0)
                        {
                            Temps[0].Values.Add(cpuSensor.Value.Value); // The first time the loop runs, there's nothing to average yet.
                        }
                        else
                        {
                            Temps[0].Values.Add(cpuSensor.Values.Select(v => v.Value).Average());
                        }
                    }
                });
            }

            if (gpuSensor != null)
            {
                gpuSensor.Hardware.Update();
                double? gpuTemp = gpuSensor.Value;
                Dispatcher.Invoke(() => {
                    if (gpuTemp.HasValue)
                    {
                        Temps[1].Values.Add((float)gpuTemp.Value);
                    }
                });
            }
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
