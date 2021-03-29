using System;

namespace NMica.Hwc
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var application = Application.Parse(args);
                Console.WriteLine($"Starting web server for in {application.ApplicationRoot} on port {application.Port}");
                using (var webServer = new HwcServer())
                {
                    webServer.Start(application);
                    Console.WriteLine($"Server started");
                    Console.WriteLine($"Application Root: {application.ApplicationRoot}");
                    Console.WriteLine($"Port: {application.Port}");
                    Console.WriteLine($"PRESS Ctrl+C to stop");
                    SystemEvents.Wait();
                    Console.WriteLine("Shutting down...");
                    webServer.Shutdown(false);
                    return 0;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Access denied starting hostable web core. Start the application as administrator");
            }
            catch (ValidationException ve)
            {
                Console.Error.WriteLine(ve.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            return 1;
        }
    }
}
