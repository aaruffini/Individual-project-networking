using System.Net;
using libplctag;
using libplctag.DataTypes.Simple; // ai found this package for me

// The code below is the starter code 
Console.WriteLine("Hello! Welcome to the PLC Program");
Console.WriteLine("Please enter your tag's name ");
string name = Console.ReadLine();
Console.WriteLine("The name of the tag is " + name);
Console.WriteLine("Enter first variable of input path");
string inputPath = Console.ReadLine();
inputPath += ",";
Console.WriteLine("Please enter the second number of the variable's path ");
inputPath += Console.ReadLine();
Console.WriteLine("Your input is: " + inputPath);

// starter code brought up by PLC.
var myTag = new TagBool
{
    Name = name,
    Gateway = "", // DO NOT COMMIT GATEWAY
    Path = inputPath,
    PlcType = PlcType.ControlLogix,
    Protocol = Protocol.ab_eip,
    // timeout = 5000 //wrong need to consult documentation
};
// Read the value from the PLC and output to console

Console.WriteLine("Reading tag... "  + myTag.Read());
Console.WriteLine("Tag Type: " + myTag.GetType());
// Write a new value to the PLC, then read it back, and output to console
myTag.Write(true);
Console.WriteLine($"Updated value: " + myTag.Read());