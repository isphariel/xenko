﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SiliconStudio.Paradox.EffectCompilerServer
{
    partial class Program
    {
        private static bool IsElevated
        {
            get
            {
                return new WindowsPrincipal
                    (WindowsIdentity.GetCurrent()).IsInRole
                    (WindowsBuiltInRole.Administrator);
            }
        }

        private static void RegisterWindowsPhonePortMapping()
        {
            if (!IsElevated)
            {
                Console.WriteLine("Not enough permissions to install Windows Phone IpOverUsb port mapping, relaunching as administrator...");

                // No entry for effet compiler, let's create it
                var info = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "--register-windowsphone-portmapping"
                };
                var process = Process.Start(info);
                process.WaitForExit();
                return;
            }

            Console.WriteLine("Installing Windows Phone IpOverUsb port mapping");

            // Add Windows Phone port mapping to registry
            using (var ipOverUsb = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsb", true))
            {
                if (ipOverUsb == null)
                {
                    Console.WriteLine("There is no IpOverUsb in registry. Is Windows Phone SDK properly installed?");
                    return;
                }
                using (var ipOverUsbParadox = ipOverUsb.CreateSubKey(IpOverUsbParadoxName))
                {
                    ipOverUsbParadox.SetValue("LocalAddress", "127.0.0.1");
                    ipOverUsbParadox.SetValue("LocalPort", 40153);
                    ipOverUsbParadox.SetValue("DestinationAddress", "127.0.0.1");
                    ipOverUsbParadox.SetValue("DestinationPort", 1245);
                }
            }

            // Restart Windows Phone IP over USB service (IpOverUsbSvc)
            RestartService("IpOverUsbSvc", 4000);
        }

        private static void RestartService(string serviceName, int timeout)
        {
            var serviceController = new ServiceController(serviceName);
            try
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));

                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout));
            }
            catch
            {
                Console.WriteLine("Error restarting {0} service", serviceName);
            }
        }

        private static void TrackWindowsPhoneDevice(ShaderCompilerHost shaderCompilerServer)
        {
            // Find AppDeployCmd.exe
            var programFilesX86 = Environment.GetEnvironmentVariable(Environment.Is64BitOperatingSystem ? "COMMONPROGRAMFILES(X86)" : "COMMONPROGRAMFILES");
            var ipOverUsbEnum = Path.Combine(programFilesX86, @"Microsoft Shared\Phone Tools\CoreCon\11.0\Bin\IpOverUsbEnum.exe");
            if (!File.Exists(ipOverUsbEnum))
            {
                return;
            }

            var portRegex = new Regex(string.Format(@"{0} (\d+) ->", IpOverUsbParadoxName));
            var currentWinPhoneDevices = new Dictionary<int, ConnectedDevice>();

            bool checkIfPortMappingIsSetup = false;

            while (true)
            {
                ProcessOutputs devicesOutputs;
                try
                {
                    devicesOutputs = ShellHelper.RunProcessAndGetOutput(ipOverUsbEnum, "");
                }
                catch (Exception)
                {
                    continue;
                }

                if (devicesOutputs.ExitCode != 0)
                    continue;

                var newWinPhoneDevices = new Dictionary<int, string>();

                // First time a device is detected, we check port mapping is properly setup in registry
                var isThereAnyDevices = devicesOutputs.OutputLines.Any(x => x == "Partner:");
                if (isThereAnyDevices && !checkIfPortMappingIsSetup)
                {

                    using (var ipOverUsb = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsb"))
                    {
                        if (ipOverUsb != null)
                        {
                            using (var ipOverUsbParadox = ipOverUsb.OpenSubKey(IpOverUsbParadoxName))
                            {
                                if (ipOverUsbParadox == null)
                                {
                                    RegisterWindowsPhonePortMapping();
                                }
                            }
                        }
                    }

                    checkIfPortMappingIsSetup = true;
                }

                // Match forwarded ports
                foreach (var outputLine in devicesOutputs.OutputLines)
                {
                    int port;
                    var match = portRegex.Match(outputLine);
                    if (match.Success && Int32.TryParse(match.Groups[1].Value, out port))
                    {
                        newWinPhoneDevices.Add(port, "Device");
                    }
                }

                ManageDevices("Windows Phone", newWinPhoneDevices, currentWinPhoneDevices, (connectedDevice) =>
                {
                    // Launch a client thread that will automatically tries to connect to this port
                    var localPort = (int)connectedDevice.Key;

                    Console.WriteLine("{0} Device connected: {1}; mapped port {2}", connectedDevice.Type, connectedDevice.Name, localPort);

                    Task.Run(() => LaunchPersistentClient(connectedDevice, shaderCompilerServer, "localhost", localPort));
                });
            }
        }
    }
}