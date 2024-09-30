using System;
using System.Diagnostics;

namespace SimpleWebSocketServer.Lib.Utilities
{
    static internal class SslCertificate
    {
        /// <summary>
        /// Installs an SSL certificate using the specified certificate path and password.
        /// </summary>
        /// <param name="certificatePath">The path to the certificate file.</param>
        /// <param name="certificatePathPassword">The password for the certificate file.</param>
        public static void InstallSslCertificate(string certificatePath, string certificatePathPassword)
        {
            try
            {
                // Prepare the command to install the certificate
                string command = $"/c certutil -f -p {certificatePathPassword} -importpfx \"{certificatePath}\"";

                // Create a new process to run the command
                var processStartInfo = new ProcessStartInfo("cmd.exe", command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // This elevates the command to run as administrator
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // Check for output or error
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Certificate installed successfully. Output: {output}");
                    }
                    else
                    {
                        Console.WriteLine($"Error installing certificate. Error: {error}. Output: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an SSL certificate binding already exists for the specified IP and port.
        /// </summary>
        /// <param name="ip">The IP address to check.</param>
        /// <param name="port">The port number to check.</param>
        /// <returns></returns>
        public static bool CheckExistingBinding(string ip, int port)
        {
            string command = $"http show sslcert";

            ProcessStartInfo processInfo = new ProcessStartInfo("netsh", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Check if the output contains the specified ip:port
                    return output.Contains($"{ip}:{port}");
                }
            }
            catch
            {
                // Handle exceptions as needed
                return false;
            }
        }

        /// <summary>
        /// Deletes an existing SSL certificate binding for the specified IP and port.
        /// </summary>
        /// <param name="ip">The IP address to delete.</param>
        /// <param name="port">The port number to delete.</param>
        public static void DeleteSslCertificateBinding(string ip, int port)
        {
            string command = $"http delete sslcert ipport={ip}:{port}";

            ProcessStartInfo processInfo = new ProcessStartInfo("netsh", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Existing SSL certificate binding deleted successfully. Output: {output}");
                    }
                    else
                    {
                        Console.WriteLine($"Error deleting certificate binding. Error: {error}. Output: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Binds an SSL certificate to the specified IP and port.
        /// </summary>
        /// <param name="prefix">The IP and port to bind the certificate to.</param>
        /// <param name="certHash">The hash of the certificate to bind.</param>
        /// <param name="appId">The application ID for the certificate.</param>
        public static void BindSslCertificate(string prefix, string certHash, string appId)
        {
            var ipPort = GetIpAndPort(prefix);

            // Check if the SSL certificate binding already exists
            if (CheckExistingBinding(ipPort.ip, ipPort.port))
            {
                // Delete the existing SSL certificate binding
                DeleteSslCertificateBinding(ipPort.ip, ipPort.port);
            }

            // The netsh command to bind the SSL certificate
            string command = $"http add sslcert ipport={ipPort.ip}:{ipPort.port} certhash={certHash} appid={{{appId}}}";

            // Create a new process to run the netsh command
            ProcessStartInfo processInfo = new ProcessStartInfo("netsh", command)
            {
                RedirectStandardOutput = true, // Capture the output if needed
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true, // No window for command prompt
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    // Read the standard output (optional)
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();

                    // Check if the command was successful
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"SSL certificate bound successfully. Output: {output}");
                    }
                    else
                    {
                        Console.WriteLine($"Error binding certificate. Error: {error}. Output: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts the IP and port from the specified input string.
        /// </summary>
        /// <param name="input">The input string containing the IP and port.</param>
        /// <returns>The IP address and port number.</returns>
        public static (string ip, int port) GetIpAndPort(string input)
        {
            // Extract the part after "http://"
            string address = input.Substring(input.IndexOf("://") + 3).TrimEnd('/');

            // Split by ':' to separate IP and port
            var parts = address.Split(':');

            string ip;
            int port;

            if (parts[0] == "+")
            {
                // If the first part is '+', use "0.0.0.0"
                ip = "0.0.0.0";
                port = int.Parse(parts[1]);
            }
            else
            {
                // Otherwise, take the IP and port from the input
                ip = parts[0];
                port = int.Parse(parts[1]);
            }

            return (ip, port);
        }
    }
}
