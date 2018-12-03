using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkerforge;

namespace IoT
{
    class Program
    {
        static void Main(string[] args)
        {
            Component cp = new Component();
            cp.Connect();

            Console.WriteLine("Temperature: " + cp.ReadTemperature() / 100.0 + " °C");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            cp.Disconnect();
        }
    }
}