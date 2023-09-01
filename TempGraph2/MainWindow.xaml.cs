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

            melfina.Open();
            FindSensors();
            InitializeCharts();
            InitializeTimer();
            DataContext = this;
        }

        private void FindSensors()
        {
            melfina.Accept(new UpdateVisitor());

            foreach (var hardware in melfina.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu && sensor.Name == "Core Average" && cpuSensor == null)
                        {
                            cpuSensor = sensor;
                            Console.WriteLine($"Found CPU: {cpuSensor.Name} Value: {cpuSensor.Value}");
                        }
                        if (hardware.HardwareType == HardwareType.GpuNvidia && gpuSensor == null)
                        {
                            gpuSensor = sensor;
                            Console.WriteLine($"Found GPU: {gpuSensor.Name}");
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
                    Values = new ChartValues<double>()
                },
                new LineSeries
                {
                    Title = "GPU Temp",
                    Values = new ChartValues<double>()
                }
            };
        }
        private void InitializeTimer()
        {
            timer.Interval = 1000;  
            timer.Elapsed += CollectData;  
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void CollectData(object sender, ElapsedEventArgs e)
        {
            melfina.Accept(new UpdateVisitor());

            if (cpuSensor != null)
            {
                cpuSensor.Hardware.Update();

                double? cpuTemp = cpuSensor.Value;
                Dispatcher.Invoke(() => {
                    if (cpuTemp.HasValue)
                    {
                        Temps[0].Values.Add(cpuTemp.Value);
                    }
                });
            }

            if (gpuSensor != null)
            {
                double? gpuTemp = gpuSensor.Value;
                Dispatcher.Invoke(() => {
                    if (gpuTemp.HasValue)
                    {
                        Temps[1].Values.Add(gpuTemp.Value);
                    }
                });
            }
        }
    }
}
