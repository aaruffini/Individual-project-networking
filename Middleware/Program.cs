
Console.WriteLine("Hello, World!");
// Example tag configuration for a global DINT tag in an Allen-Bradley CompactLogix/ControlLogix PLC
var myTag = new TagDint()
{
    Name = "SomeDINT",
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