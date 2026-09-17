using libplctag;
using libplctag.DataTypes.Simple; // ai found this package for me

// The code below is the starter code 
Console.WriteLine("Hello, World!");

var myTag = new TagDint
{
    Name = "DINT_TEST",
    Gateway = "10.10.10.10",
    Path = "1,0",
    PlcType = PlcType.ControlLogix,
    Protocol = Protocol.ab_eip
};
// Read the value from the PLC and output to console

int output = myTag.Read();
Console.WriteLine($"Original value: {output}");


// Write a new value to the PLC, then read it back, and output to console
myTag.Write(37);
output = myTag.Read();
Console.WriteLine($"Updated value: {output}");