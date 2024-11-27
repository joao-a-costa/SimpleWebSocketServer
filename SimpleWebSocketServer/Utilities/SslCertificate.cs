using System;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Security.Cryptography.X509Certificates;

namespace SimpleWebSocketServer.Lib.Utilities
{
    static internal class SslCertificate
    {

        private static string BuildInstallCertificateFile(string certificatePath, string certificatePathPassword,
            string prefix, string certHash, string appId)
        {
            var ipPort = GetIpAndPort(prefix);

            string commandBase =
                $"chcp 65001{Environment.NewLine}" +
                $"certutil -f -p {certificatePathPassword} -importpfx \"{Path.GetFullPath(certificatePath)}\"{Environment.NewLine}" +
                $"netsh http delete sslcert ipport={ipPort.ip}:{ipPort.port}{Environment.NewLine}" +
                $"netsh http add sslcert ipport={ipPort.ip}:{ipPort.port} certhash={certHash} appid={{{appId}}}{Environment.NewLine}" +
                $"pause";

            var tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bat");

            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            File.WriteAllText(tempFileName, commandBase);

            return tempFileName;
        }

        /// <summary>
        /// Imports an SSL certificate using the specified certificate path and password.
        /// </summary>
        /// <param name="certificatePath">The path to the certificate file.</param>
        /// <param name="certificatePathPassword">The password for the certificate file.</param>
        public static Tuple<bool, string> ImportSslCertificate(string certificatePath, string certificatePathPassword,
            string prefix, string certHash, string appId)
        {
            var resSuccess = false;
            var resSuccessMessage = string.Empty;

            try
            {
                // Prepare the command to install the certificate
                string commandBase = $"certutil -f -p {certificatePathPassword} -importpfx \"{certificatePath}\"";
                string command = $"/c {commandBase}";

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
                        resSuccess = true;
                        resSuccessMessage = $"Certificate imported successfully. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                    else
                    {
                        resSuccessMessage = $"Error importing certificate. Error: {error}. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                    if (!resSuccess)
                    {
                        var tempFileName = BuildInstallCertificateFile(certificatePath, certificatePathPassword, prefix, certHash, appId);

                        if (!string.IsNullOrEmpty(tempFileName))
                            resSuccessMessage += $"Run the following file as adminitrator to install the certificate: {Path.GetFullPath(tempFileName)}{Environment.NewLine}";
                        else
                            resSuccessMessage += $"Unable to create the file to install the certificate.";
                    }
                }
            }
            catch (Exception ex)
            {
                resSuccessMessage = $"Exception occurred: {ex.Message}";
            }
            finally
            {
                Console.WriteLine(resSuccessMessage);
            }

            return Tuple.Create(resSuccess, resSuccessMessage);
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
        public static Tuple<bool, string> DeleteSslCertificateBinding(string ip, int port)
        {
            var resSuccess = false;
            var resSuccessMessage = string.Empty;

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
                        resSuccess = true;
                        resSuccessMessage = $"Existing SSL certificate binding deleted successfully. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                    else
                    {
                        resSuccessMessage = $"Error deleting certificate binding. Error: {error}. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                }
            }
            catch (Exception ex)
            {
                resSuccessMessage = $"Exception occurred: {ex.Message}";
            }
            finally
            {
                Console.WriteLine(resSuccessMessage);
            }

            return Tuple.Create(resSuccess, resSuccessMessage);
        }

        /// <summary>
        /// Binds an SSL certificate to the specified IP and port.
        /// </summary>
        /// <param name="prefix">The IP and port to bind the certificate to.</param>
        /// <param name="certHash">The hash of the certificate to bind.</param>
        /// <param name="appId">The application ID for the certificate.</param>
        public static Tuple<bool, string> BindSslCertificate(string prefix, string certHash, string appId)
        {
            var resSuccess = false;
            var resSuccessMessage = string.Empty;

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
                        resSuccess = true;
                        resSuccessMessage = $"SSL certificate bound successfully. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                    else
                    {
                        resSuccessMessage = $"Error importing certificate. Error: {error}. Output: {output.Replace("\r\n", " ")}. {Environment.NewLine}";
                    }
                }
            }
            catch (Exception ex)
            {
                resSuccessMessage = $"Exception occurred: {ex.Message}";
            }
            finally
            {
                Console.WriteLine(resSuccessMessage);
            }

            return Tuple.Create(resSuccess, resSuccessMessage);
        }

        //public static string GetSslCertificateThumbprint(string certificatePath, string certificatePathPassword)
        //{
        //    var certificateThumbprint = string.Empty;

        //    using (var _certificate = new X509Certificate2(certificatePath, certificatePathPassword))
        //    {
        //        certificateThumbprint = _certificate.Thumbprint;
        //    }

        //    return certificateThumbprint;
        //}

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
