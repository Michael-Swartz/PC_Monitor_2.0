using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using OpenHardwareMonitor.Hardware;


namespace MichaelsService2
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        public Service1()
        {
            InitializeComponent();
        }

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

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is Started at " + DateTime.Now);
            
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval += 750;
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }

        //This function formats the output from sensors into three digit chucks which are combined and sent in a packet to the Arduino. 
        public static string format_string(string input)
        {
            if (input.Length == 3)
            {
                return input;
            }

            if (input.Length == 2)
            {
                string new_input = " " + input;
                return new_input;
            }

            if (input.Length == 1)
            {
                string new_input = "  " + input;
                return new_input;
            }

            else
            {
                string new_input = "   ";
                return new_input;
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("10.0.0.164");


            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.RAMEnabled = true;
            computer.GPUEnabled = true;
            computer.Accept(updateVisitor);
            string cput = "0";
            string cpuu = "0";
            string gput = "0";
            string gpuu = "0";
            float used_mem = 0;
            float free_mem = 0;
            string used_mem_string = "0";
            string total_mem = "0";
            double total_mem_pre_round = 0;

            // Old way below
            /**
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                //GET CPU TEMP AND UTILIZATION 
                if (computer.Hardware[i].HardwareType == HardwareType.CPU) ;
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        //GET CPU TEMP
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            if (computer.Hardware[i].Sensors[j].Name == "CPU Package")
                            {
                                //Console.WriteLine(computer.Hardware[i].Sensors[j].Name + ": " + format_string(computer.Hardware[i].Sensors[j].Value.ToString()) + "\r");
                                cput = format_string(computer.Hardware[i].Sensors[j].Value.ToString());

                            }
                        }

                        //GET CPU UTILIZATION
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                        {
                            if (computer.Hardware[i].Sensors[j].Name == "CPU Total")
                            {
                                //Console.WriteLine(computer.Hardware[i].Sensors[j].Name + ": " + computer.Hardware[i].Sensors[j].Value.ToString() + "\r");
                                cpuu = format_string(computer.Hardware[i].Sensors[j].Value.GetValueOrDefault().ToString());
                            }                                            
                        }
                    }
                }
                //GET GPY TEMP AND UTILIZATION
                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        //GET CPU TEMP
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {

                            Console.WriteLine("T:" + ": " + computer.Hardware[i].Sensors[j].Value.ToString() + "\r");
                            gput = format_string(computer.Hardware[i].Sensors[j].Value.ToString());
                        }

                        //GET CPU UTILIZATION
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                        {

                            if (computer.Hardware[i].Sensors[j].Name == "GPU Core")
                            {
                                Console.WriteLine("U: " + computer.Hardware[i].Sensors[j].Name + ": " + computer.Hardware[i].Sensors[j].Value.ToString() + "\r");
                                gpuu = format_string(computer.Hardware[i].Sensors[j].Value.ToString());

                            }
                        }
                    }
                }

                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Load)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gpuu = sensors.Value.ToString();
                            }

                        }

                        if (sensors.SensorType == SensorType.Temperature)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gput = sensors.Value.ToString();
                            }
                        }

                    }
                }

                if (hardware.HardwareType == HardwareType.RAM)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Data)
                        {
                            if (sensors.Name == "Used Memory")
                            {
                                Console.WriteLine(sensors.Name + ": " + Math.Round(sensors.Value.GetValueOrDefault(), 0));
                                used_mem = sensors.Value.GetValueOrDefault();
                            }

                            if (sensors.Name == "Available Memory")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                free_mem = sensors.Value.GetValueOrDefault();
                            }
                        }
                    }
                }




                //end of for loop
            }
            **/


            //New 
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Load)
                        {   

                            if (sensors.Name == "CPU Total")
                            {
                                // Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                cpuu = format_string(sensors.Value.ToString());
                            }
                        }

                        if (sensors.SensorType == SensorType.Temperature)
                        {
                            if (sensors.Name == "CPU Package")
                            {
                                // Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                cput = format_string(sensors.Value.ToString());
                            }
                        }
                    }
                }

                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Load)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                // Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gpuu = format_string(sensors.Value.ToString());
                            }
                        }

                        if (sensors.SensorType == SensorType.Temperature)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                //Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gput = format_string(sensors.Value.ToString());
                            }
                        }
                    }
                }

                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Load)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gpuu = format_string(sensors.Value.ToString());
                            }

                        }

                        if (sensors.SensorType == SensorType.Temperature)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                gput = format_string(sensors.Value.ToString());
                            }
                        }

                    }
                }

                if (hardware.HardwareType == HardwareType.RAM)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Data)
                        {
                            if (sensors.Name == "Used Memory")
                            {
                                Console.WriteLine(sensors.Name + ": " + Math.Round(sensors.Value.GetValueOrDefault(), 0));
                                used_mem = sensors.Value.GetValueOrDefault();
                            }

                            if (sensors.Name == "Available Memory")
                            {
                                Console.WriteLine(sensors.Name + ": " + sensors.Value.ToString());
                                free_mem = sensors.Value.GetValueOrDefault();
                            }
                        }
                    }
                }








                //End of the the foreach loop
            }

           

            total_mem_pre_round = used_mem + free_mem;
            total_mem = format_string(Math.Ceiling(total_mem_pre_round).ToString());
            used_mem_string = format_string(Math.Round(used_mem,0).ToString());

            string packet_pre = cput + cpuu + gput + gpuu + used_mem_string + total_mem;

            computer.Close();

            string packet_data = packet_pre;
            byte[] sendbuff = Encoding.ASCII.GetBytes(packet_data);
            IPEndPoint ep = new IPEndPoint(broadcast, 6666);
            s.SendTo(sendbuff, ep);
            //Random rnd = new Random();
            //int num = rnd.Next();
            // WriteToFile("Sent packet " + packet_data);
            //string packet_data = GetRandomNumber(0, 234957675).ToString();

        }
        private static readonly Random getrandom = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            lock (getrandom)
            {
                return getrandom.Next(min, max);
            }
        }


        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
           
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Service" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            
            if (File.Exists(filepath)) 
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }    
            } 

            else
            {
                using(StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
