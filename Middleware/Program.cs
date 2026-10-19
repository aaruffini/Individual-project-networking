using System;
using libplctag;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using libplctag.DataTypes.Simple; // ai found this package for me

Console.WriteLine("Starting Ride Middleware Service...");

// Global state
var rideState = new { PB_2_Value = false };

// Start PLC reader in background
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
                rideState = new { PB_2_Value = myTag.Value };
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