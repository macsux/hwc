using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;

namespace NMica.Hwc 
{
    public class HwcServer : IDisposable 
    {
        private delegate int FnWebCoreActivate([In, MarshalAs(UnmanagedType.LPWStr)]string appHostConfig, [In, MarshalAs(UnmanagedType.LPWStr)]string rootWebConfig, [In, MarshalAs(UnmanagedType.LPWStr)]string instanceName);
        private delegate int FnWebCoreShutdown(bool immediate);

        private static FnWebCoreActivate WebCoreActivate;
        private static FnWebCoreShutdown WebCoreShutdown;
        private static string hwebcoreDll = Environment.ExpandEnvironmentVariables(@"%windir%\system32\inetsrv\hwebcore.dll");
        private string _applicationHostPath;

        static HwcServer() {
            
            // Load the library and get the function pointers for the WebCore entry points
            IntPtr hwc = NativeMethods.LoadLibrary(hwebcoreDll);

            IntPtr procaddr = NativeMethods.GetProcAddress(hwc, "WebCoreActivate");
            WebCoreActivate = (FnWebCoreActivate)Marshal.GetDelegateForFunctionPointer(procaddr, typeof(FnWebCoreActivate));

            procaddr = NativeMethods.GetProcAddress(hwc, "WebCoreShutdown");
            WebCoreShutdown = (FnWebCoreShutdown)Marshal.GetDelegateForFunctionPointer(procaddr, typeof(FnWebCoreShutdown));
        }

        /// <summary>
        /// Specifies if Hostable WebCore ha been activated
        /// </summary>
        public bool IsActivated { get; private set; }

        public void Start(Application application)
        {
            _applicationHostPath = CreateApplicationHost(application);
            var systemWebConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework64", "v4.0.30319", "Config", "web.config");

            Start(_applicationHostPath, systemWebConfigPath, application.Name);
        }
        /// <summary>
        /// Activate the HWC
        /// </summary>
        /// <param name="appHostConfig">Path to ApplicationHost.config to use</param>
        /// <param name="rootWebConfig">Path to the Root Web.config to use</param>
        /// <param name="instanceName">Name for this instance</param>
        public void Start(string appHostConfig, string rootWebConfig, string instanceName)
        {
            int result = WebCoreActivate(appHostConfig, rootWebConfig, instanceName);
            if (result != 0) {
                Marshal.ThrowExceptionForHR(result);
            }

            IsActivated = true;
        }

        /// <summary>
        /// Shutdown HWC
        /// </summary>
        public void Shutdown(bool immediate) 
        {
            if (IsActivated) {
                WebCoreShutdown(immediate);
                IsActivated = false;
            }

            try
            {
                File.Delete(_applicationHostPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static class NativeMethods {
            [DllImport("kernel32.dll")]
            internal static extern IntPtr LoadLibrary(String dllname);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, String procname);
        }

        public void Dispose()
        {
            Shutdown(true);
        }

        private string CreateApplicationHost(Application application)
        {
            var systemApplicationHostPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "inetsrv", "config", "ApplicationHost.config");

            var applicationHost = new XmlDocument();
            applicationHost.Load(systemApplicationHostPath);
            ValidateApplicationHost(applicationHost);
            ConfigureHost(applicationHost, application);
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), $"applicationHost-{Guid.NewGuid():N}.config");
            applicationHost.Save(configPath);
            return configPath;
        }

        private static void ValidateApplicationHost(XmlDocument applicationHost)
        {
            var missingDlls = applicationHost
                .SelectNodes("//configuration/system.webServer/globalModules/add")
                .OfType<XmlElement>()
                .Select(x => Environment.ExpandEnvironmentVariables(x.GetAttribute("image")))
                .Where(x => !File.Exists(x))
                .ToList();
            if (missingDlls.Any())
            {
                throw new ValidationException($"Missing required ddls:\n{string.Join("\n", missingDlls)}");
            }
        }


        private void ConfigureHost(XmlDocument doc, Application config)
        {
            var sites = doc.SelectNodes("/configuration/system.applicationHost/sites/site");
            foreach(var existing in sites.OfType<XmlElement>())
            {
                existing.ParentNode.RemoveChild(existing);
            }
            var site = doc.CreateElement("site");
            site.SetAttribute("name", "Default Web Site");
            site.SetAttribute("id", "1");
            var application = doc.CreateElement("application");
            application.SetAttribute("path", "/");
            site.AppendChild(application);
            var virtualDirectory = doc.CreateElement("virtualDirectory");
            virtualDirectory.SetAttribute("path", "/");
            var appDir = Path.IsPathRooted(config.ApplicationRoot) ? config.ApplicationRoot : Path.Combine(Directory.GetCurrentDirectory(), config.ApplicationRoot);
            virtualDirectory.SetAttribute("physicalPath", appDir);
            application.AppendChild(virtualDirectory);
            var bindings = doc.CreateElement("bindings");
            var binding =  doc.CreateElement("binding");
            binding.SetAttribute("protocol", "http");
            binding.SetAttribute("bindingInformation", $"*:{config.Port}:");
            bindings.AppendChild(binding);
            site.AppendChild(bindings);

            var sitesNode = (XmlNode)doc.SelectSingleNode("/configuration/system.applicationHost/sites");
            sitesNode.AppendChild(site);
        }
    }

}
