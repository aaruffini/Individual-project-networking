using System.Net;
using libplctag;
using libplctag.DataTypes.Simple; // ai found this package for me

// The code below is the starter code 
Console.WriteLine("Hello! Welcome to the PLC Program");
Console.WriteLine("Please enter the path needed of of the variable ");
string inputPath = Console.ReadLine();
Console.WriteLine("Your input is: " + inputPath);

// starter code brought up by PLC.
var myTag = new TagDint
{
    Name = "Program:MainProgram.DINT_TEST",
    Gateway = "", // DO NOT COMMIT GATEWAY
    Path = inputPath,
    PlcType = PlcType.ControlLogix,
    Protocol = Protocol.ab_eip
};
// Read the value from the PLC and output to console
int output;
try
{
    output = myTag.Read();
}
catch (Exception)
{
    Console.WriteLine("Error reading tag!");
    return;
}

Console.WriteLine($"Original value: {output}");


// Write a new value to the PLC, then read it back, and output to console
myTag.Write(37);
output = myTag.Read();
Console.WriteLine($"Updated value: {output}");