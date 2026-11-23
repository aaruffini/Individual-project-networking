using System;
using libplctag;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using libplctag.DataTypes.Simple;
using Microsoft.VisualBasic; // ai found this package for me

Console.WriteLine("Starting Ride Middleware Service...");

/*
    *****************
    *Global Variables*
    ******************
    PLC | What it does 
    ** Inputs **
   
    ** Momentary Buttons **
    PB_1 -> Ride Start 
    PB_2 -> Ride Stop 
    PB_3 -> Ride Reset
   
    ** Selector Switches **
    SW_1 -> Ride Power
    SW_2 -> Ride Gates
    
    ** Dual Selector Switches **
    Selector_Switch_3_Left ->  Ride Restraints
    
    ** Estop **
    Estop1 -> Emergency Stop

    ** Outputs **
    
    ** Physical Outputs **

    ** Motors **
    DC_Motor_1 -> Ride's Motor
    DC_Motor_2 -> Ride's Break

    ** Tower Light**
    Tower_Light_Green -> Ride Running
    Tower_Light_Red -> Ride Fault

    ** Traffic Light **
    Traf_Light_A_G ->
    Traf_Light_A_Y ->
    TrafLightGreenB
    TrafLighYellB

    **Conveyor Light** 
    Conveyor_Line_A_Green -> Ride Cycle Done

    ** Non-Physical Outputs **

    ** Timer On Delay Timers **
    Ride_Cycle -> Times the Ride's Cycle
    Ride_Cycle.TT -> Timing Bit
    Ride_Cycle.DN -> Done Bit
    Brakes -> Simulates applying the brakes
    Brakes.TT -> Timing Bit
    Brakes.DN -> Done

    ** State Outputs/Inputs **
    Ride_Power
    Gates_Closed
    Lap_Bar_Closed
    Ride_Ready
    Ride_Running
    Fault_Detected
    
*/

String[] systemTags =
{
    "PB_1", "PB_2", "PB_3",
    "SW_1", "SW_2", "Selector_Switch_3_Left",
    "DC_Motor_1", "DC_Motor_2",
    "Tower_Light_Green",  "Tower_Light_Red",
    "Traf_Light_A_G", "Traf_Light_A_Y",
    "TrafLightGreenB", "TrafLighYellB",
    "Conveyor_Line_A_Green",
    "Ride_Cycle", " Ride_Cycle.TT", "Ride_Cycle.DN",
    "Breaks", "Breaks.TT", "Breaks.DN",
    "Ride_Power", "Gates_Closed", "Lap_Bar_Closed",
    "Ride_Ready", "Ride_Running", "Fault_Detected"
};
var rideState = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

//  Buttons
var pb1 = new{PB_1 = false}; //start
var pb2 = new { PB_2 = false }; //stop
var pb3 = new { PB_3 = false }; //reset fault
var estop = new{ Estop1 = false}; // emergency stop

// Switches
var sw1 = new { SW_1 = false }; // ride power
var sw2 = new { SW_2 = false }; // ride gates
var ss3l = new {Selector_Switch_3_Left = false}; // lap bars

// Motors
var dc1 = new { DC_Motor_1 = false}; //  ride motor
var dc2 = new { DC_Motor_2 = false }; // ride break

// Lights
var towLightG = new { Tower_Light_Green  = false}; // ride running light
var towLightR= new { Tower_Light_Red = false}; // ride fault light
var trafLightAG = new { Traf_Light_A_G  = false}; // ride power light
var trafLightAY = new { TrafLighYellB = false}; // gates closed light
var trafLightBG = new { TrafLightGreenB = false }; // ride is ready light
var trafLightBY = new { Traf_Light_A_Y = false }; //  lap bar is locked light
var convLightAG = new { Conveyor_Line_A_Green = false}; // ride is done light



// ** State Inputs/Outputs **
var ridePower = new {Ride_Power = false};
var gatesClosed = new{Gates_Closed = false};
var lapBarClosed = new{Lap_Bar_Closed = false};
var rideRunning = new {Ride_Running = false};
var faultDetected = new{Fault_Detected = false};

// Timers
var rideCycle = new { Ride_Cycle = false};
var breaks = new {Breaks = false};



var plcTask = Task.Run(() => ReadPLCContinuously());

// Start REST API
StartRestServer();

Console.WriteLine("✓ Middleware running on http://localhost:8080");
Console.WriteLine("✓ Access PB_2 state at: http://localhost:8080/api/state");

// Keep running
await Task.Delay(Timeout.Infinite);

// Background task: continuously read from PLC
async Task ReadPLCContinuously()
{
    var myTag = new TagBool
    {
        Name = "Program:MainProgram.PB_2",
        Gateway = "number",
        Path = "1,0",
        PlcType = PlcType.ControlLogix,
        Protocol = Protocol.ab_eip,
        Timeout = TimeSpan.FromMilliseconds(5000)
    };

    try
    {
        while (true)
        {
            try
            {
                myTag.Read();
                
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] PB_2: {rideState.PB_2_Value}");
                await Task.Delay(30); // Read every 30ms (PLC cycle)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PLC read error: {ex.Message}");
                await Task.Delay(1000); // Retry after 1 second
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Fatal PLC error: {ex.Message}");
    }
}

// REST API server
void StartRestServer()
{
    var listener = new HttpListener();
    listener.Prefixes.Add("http://localhost:8080/");
    listener.Start();
    Console.WriteLine("✓ REST server started");

    Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // Enable CORS (so Unity can call from browser)
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                response.AddHeader("Content-Type", "application/json");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/api/state")
                {
                    string json = JsonSerializer.Serialize(rideState);
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
                    response.ContentType = "application/json";   // ← use the property
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.StatusCode = 200;
                }
                else
                {
                    response.StatusCode = 404;
                }

                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ REST error: {ex.Message}");
            }
        }
    });
}