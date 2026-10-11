using JModbus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modbus_Example
{
    public partial class Strtup : Form
    {
        private Modbus modbus;
        public Strtup()
        {
            InitializeComponent();
        }
        private void Strtup_Load(object sender, EventArgs e)
        {
            modbus = new Modbus(IPAddress.Any);
            Thread thread = new Thread(new ThreadStart(ModbusMapping));
            thread.Start();
        }
        private void ModbusMapping()
        {
            //Random random = new Random();
            int i = 0;
            while (true)
            {
                modbus.SetInputRegister(0, modbus.TransmitCount());
                modbus.SetInputRegister(12,100.890d);
                if (i < 50)
                {
                    modbus.SetInputRegister(2, "Bharat");
                    modbus.SetInputRegister(7, "INDIA");
                    modbus.SetInputRegister(17, 3.14159f);
                    i++;
                }
                else
                {
                    modbus.SetInputRegister(2, "Pooja");
                    modbus.SetInputRegister(7, "MST01");
                    modbus.SetInputRegister(17, 9.81f);
                    i++;
                    if(i>100)
                        i = 0;
                }
                Thread.Sleep(100);
            }
        }
    }
}
