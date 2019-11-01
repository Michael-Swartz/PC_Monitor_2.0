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
using System.Management;


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
            //Timer is set to 750ms 
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

        //This is the function that is called at the end of the specified interval
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            /** Modify the following line to set it as either the IP of the monitor, or the brodcast address of 
             *  your LAN  (EXAMPLE: If your LAN is 10.0.0.0/24 it is 10.0.0.255, if it is 192.168.1.0/24 it would be 192.168.1.255)
            **/
            string broadcast_address = "10.0.0.255";




            IPAddress broadcast = IPAddress.Parse(broadcast_address);


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

            foreach (var hardware in computer.Hardware)
            {
                //CPU Information 
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        
                        //Pull CPU Temperature Information 
                        if (sensors.SensorType == SensorType.Temperature)
                        {
                            if (sensors.Name == "CPU Package")
                            {
                                cput = format_string(sensors.Value.ToString());
                            }
                        }
                    }
                }

                //GPU Information
                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        //Pull GPU utilization information 
                        if (sensors.SensorType == SensorType.Load)
                        {
                            if (sensors.Name == "GPU Core")
                            {
                                gpuu = format_string(sensors.Value.ToString());
                            }
                        }

                        //Pull GPU temperature infomration
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

                //Pull memory information
                if (hardware.HardwareType == HardwareType.RAM)
                {
                    hardware.Update();
                    foreach (var sensors in hardware.Sensors)
                    {
                        if (sensors.SensorType == SensorType.Data)
                        {
                            //Pulls used memory 
                            if (sensors.Name == "Used Memory")
                            {
                                Console.WriteLine(sensors.Name + ": " + Math.Round(sensors.Value.GetValueOrDefault(), 0));
                                used_mem = sensors.Value.GetValueOrDefault();
                            }

                            //Pulls available memory
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

            //CPU utilization WMI query
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    cpuu = format_string(queryObj["LoadPercentage"].ToString());
                }
            }
            catch
            {
                //Send XXX out as a packet if there is an issue pulling the data
                cpuu = "XXX";
            }





            //Find the total amount of memory
            total_mem_pre_round = used_mem + free_mem;
            total_mem = format_string(Math.Ceiling(total_mem_pre_round).ToString());
            used_mem_string = format_string(Math.Round(used_mem,0).ToString());

            //Create the string to send in the UDP packet
            string packet_pre = cput + cpuu + gput + gpuu + used_mem_string + total_mem;

            computer.Close();

            //Networky packety stuffz
            string packet_data = packet_pre;
            byte[] sendbuff = Encoding.ASCII.GetBytes(packet_data);
            IPEndPoint ep = new IPEndPoint(broadcast, 6666);
            s.SendTo(sendbuff, ep);



        }



        //Write to logs to check it is working
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
