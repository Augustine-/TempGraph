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
using System.Threading;


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
        private System.Timers.Timer timer = new System.Timers.Timer();
        private Computer melfina;
        private int timeWindowInSeconds = 0;
        private List<float> cpuTempsMasterList = new List<float>();
        private List<float> gpuTempsMasterList = new List<float>();


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

        private void TimeWindowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (timeWindowTextBox != null && timeWindowSlider != null) {
                timeWindowTextBox.Text = timeWindowSlider.Value.ToString();
                timeWindowInSeconds = (int)e.NewValue;
                UpdateChartData();
            }
        }

        private void TimeWindowTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(timeWindowTextBox.Text, out int value))
            {
                timeWindowSlider.Value = value;
            }
            else
            {
                // revert to prior value on invalid input
                timeWindowTextBox.Text = timeWindowSlider.Value.ToString();
            }
            UpdateChartData();
        }

        // Methods
        private void UpdateChartData()
        {
            if (timeWindowInSeconds == 0) // 'All time'
            {
                Temps[0].Values = new ChartValues<float>(cpuTempsMasterList);
                Temps[1].Values = new ChartValues<float>(gpuTempsMasterList);
            }
            else
            {
                Temps[0].Values = new ChartValues<float>(cpuTempsMasterList.GetRange(timeWindowInSeconds, timeWindowInSeconds));
                Temps[1].Values = new ChartValues<float>(gpuTempsMasterList.GetRange(timeWindowInSeconds, timeWindowInSeconds));
            }
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
                            cpuSensor.ValuesTimeWindow = TimeSpan.FromSeconds(5);
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
            timer.Interval = 1000;  
            timer.Elapsed += CollectData;  
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void CollectData(object sender, ElapsedEventArgs e)
        {
            if (cpuSensor != null)
            {
                cpuSensor.Hardware.Update();
                Dispatcher.Invoke(() => {
                    if (cpuSensor.Value.HasValue)
                    {
                        float cpuTemp;
                        if (cpuSensor.Values.Any())
                        {
                            cpuTemp = (float)cpuSensor.Values.Select(v => v.Value).Average();
                            Temps[0].Values.Add(cpuTemp);
                            cpuTempsMasterList.Add(cpuTemp);
                        }
                        else
                        {
                            Temps[0].Values.Add(cpuSensor.Value.Value); // The first time the loop runs, there's nothing to average yet.
                            cpuTempsMasterList.Add(cpuSensor.Value.Value);
                        }
                    }
                });
            }

            if (gpuSensor != null)
            {
                gpuSensor.Hardware.Update();
                Dispatcher.Invoke(() => {
                    if (gpuSensor.Value.HasValue)
                    {
                        float gpuTemp = (float)gpuSensor.Value.Value;
                        gpuTempsMasterList.Add(gpuTemp); // Add to master list
                        Temps[1].Values.Add(gpuTemp);
                    }
                });
            }
        }

    }
}
