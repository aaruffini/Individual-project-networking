using System;
using libplctag;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using libplctag.DataTypes.Simple;
using Microsoft.VisualBasic; // ai found this package for me
using System.Collections.Concurrent;

Console.WriteLine("Starting Ride Middleware Service...");

  

String[] rideBoolTagNames =
{
    "PB_1", "PB_2", "PB_3",
    "SW_1", "SW_2", "Selector_switch_4_NO_3",
    "DC_Motor_1", "DC_Motor_2",
    "Tower_Light_Green",  "Tower_Light_Red",
    "Traf_Light_A_G", "Traf_Light_A_Y",
    "TrafLightGreenB", "TrafLightYellB",
    "Conveyor_Line_A_Green",
    "Ride_Cycle.TT", "Ride_Cycle.DN",
    "Brakes.TT", "Brakes.DN",
    "Ride_Power", "Gates_Closed", "Lap_Bar_Locked",
    "Ride_Ready", "Ride_Running", "Fault_detected"
};

ConcurrentDictionary<string, bool> rideState = new(StringComparer.OrdinalIgnoreCase);

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
    var rideTags = new List<(string currentName, TagBool Tag)>(); // ai suggestion

    foreach (var currentName in rideBoolTagNames)
    {

        rideTags.Add((currentName, new TagBool
        {
            Name = "Program:MainProgram." + currentName,
            Gateway = "Secret", // we know this works 
            Path = "1,0",
            PlcType = PlcType.ControlLogix,
            Protocol = Protocol.ab_eip,
            Timeout = TimeSpan.FromMilliseconds(5000)
        }));
       
    }
    
    try
    {
        ConsoleKey keyPressed = ConsoleKey.None;
        while (keyPressed != ConsoleKey.Escape)
        {
            try
            {
                foreach(var (currentName, tag) in rideTags)
                {
                    tag.Read();
                    bool value = tag.Value;
                    rideState[currentName] = value;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {currentName} = {tag.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PLC read error: {ex.Message}");
                await Task.Delay(1000); // Retry after 1 second
            }
            await Task.Delay(30);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Fatal PLC error: {ex.Message}");
    }
}

// REST API server (vibe coded)
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

                if (request.Url?.AbsolutePath == "/api/state")
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