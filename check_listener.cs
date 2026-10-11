using System;
using System.Net;
using JModbus;
class Program {
static void Main() {
try {
var m = new Modbus(IPAddress.Any, 502);
Console.WriteLine("LISTENER_STARTED_SUCCESSFULLY");
while (true) { System.Threading.Thread.Sleep(1000); }
} catch (Exception ex) {
Console.WriteLine("EXCEPTION: " + ex);
}
}
}