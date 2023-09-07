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
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using System.Management;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace TempGraph
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
            InitializeComponent();
            FindSensors();
            InitializeCharts();
            InitializeTimer();
        }
        // Event Handlers
        private void Window_Closed(object sender, EventArgs e)
        {
            melfina.Close();
            timer.Dispose();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {

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
                        if (hardware.HardwareType == HardwareType.Cpu && sensor.Name == "Core Average")
                        {
                            cpuSensor = sensor;
                            cpuSensor.ValuesTimeWindow = TimeSpan.FromSeconds(5);
                            Console.WriteLine($"Found CPU Sensor: {cpuSensor.Name} Value: {cpuSensor.Value}");
                        }
                        if ((hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel) && gpuSensor == null)
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
            var plotModel = new PlotModel { Title = "Temperature Data" };
            var cpuSeries = new LineSeries { Title = "CPU" };
            var gpuSeries = new LineSeries { Title = "GPU" };
            plotModel.Series.Add(cpuSeries);
            plotModel.Series.Add(gpuSeries);
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Temperature (C)" });
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Seconds" });
            MainPlot.Model = plotModel;
        }
        private void InitializeTimer()
        {
            timer.Interval = 250;  
            timer.Elapsed += CollectData;  
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void CollectData(object sender, ElapsedEventArgs e)
        {
            double cpuTemp = 0;
            double gpuTemp = 0;

            if (cpuSensor != null)
            {
                cpuSensor.Hardware.Update();
                if (cpuSensor.Value.HasValue)
                {
                    if (cpuSensor.Values.Count() > 0)
                    {
                        cpuTemp = cpuSensor.Values.Select(v => v.Value).Average();  // average when historical data exists
                    }
                    else
                    {
                        cpuTemp = cpuSensor.Value.Value;  // if no history
                    }
                }
            }

            if (gpuSensor != null)
            {
                gpuSensor.Hardware.Update();
                if (gpuSensor.Value.HasValue)
                {
                    gpuTemp = (double)gpuSensor.Value;
                }
            }

            Dispatcher.Invoke(() => {
                int elapsedSeconds = ((LineSeries)MainPlot.Model.Series[0]).Points.Count;
                ((LineSeries)MainPlot.Model.Series[0]).Points.Add(new DataPoint(elapsedSeconds, cpuTemp));
                ((LineSeries)MainPlot.Model.Series[1]).Points.Add(new DataPoint(elapsedSeconds, gpuTemp));
                MainPlot.Model.InvalidatePlot(true);
            });
        }
    }
}
