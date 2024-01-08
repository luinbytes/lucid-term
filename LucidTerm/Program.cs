using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using static CustomTerminal.Terminal;

namespace CustomTerminal
{
    public class AppConfig
    {
        public string Prefix { get; set; }
        public string CustomWelcomeMessage { get; set; }
        public string PrimaryColour { get; set; }
        public string Debug { get; set; }
    }

    public enum SeverityLevel
    {
        Success,
        Info,
        Warning,
        Error
    }

    public class Script
    {
        public string id { get; set; }
        public string software { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        public string last_update { get; set; }
        public string update_notes { get; set; }

        [JsonPropertyName("script")]
        public string scriptContent { get; set; }

        public string core { get; set; }
        public string forums { get; set; }
        public string library { get; set; }

        public string elapsed { get; set; }
    }

    public class ApiResponse
    {
        public string Username { get; set; }
        public List<Script> Scripts { get; set; }
    }

    public class software
    {
        public string Version { get; set; }
        public long LastUpdate { get; set; }
        public string Elapsed { get; set; }
        public string Name { get; set; }
    }

    class Program
    {
        static HttpClient httpClient = new HttpClient();
        static AppConfig appConfig = new AppConfig();
        public static ConsoleColor term_colour = ConsoleColor.White;
        static string apiKey = ""; // Global variable to store the validated key
        static bool debug = false;
        static string VER = "v1.1.0";

        static Dictionary<string, ConsoleColor> colorMap = new Dictionary<string, ConsoleColor>()
        {
            { "Black", ConsoleColor.Black },
            { "DarkBlue", ConsoleColor.DarkBlue },
            { "DarkGreen", ConsoleColor.DarkGreen },
            { "DarkCyan", ConsoleColor.DarkCyan },
            { "DarkRed", ConsoleColor.DarkRed },
            { "DarkMagenta", ConsoleColor.DarkMagenta },
            { "DarkYellow", ConsoleColor.DarkYellow },
            { "Gray", ConsoleColor.Gray },
            { "DarkGray", ConsoleColor.DarkGray },
            { "Blue", ConsoleColor.Blue },
            { "Green", ConsoleColor.Green },
            { "Cyan", ConsoleColor.Cyan },
            { "Red", ConsoleColor.Red },
            { "Magenta", ConsoleColor.Magenta },
            { "Yellow", ConsoleColor.Yellow },
            { "White", ConsoleColor.White }
        };

        static async Task Main(string[] args)
        {
            Console.Title = $"Lucid Term {VER}";
            bool isAdmin = IsUserAnAdmin();

            //Config
            string[] default_config =
            {
                "PrimaryColour = White",
                "Prefix = Lucid",
                "CustomWelcome = Welcome!!! <3 YOU CAN CHANGE ME IN LT_CONFIG.INI :D",
                "Debug = false" //useless rn
            };

            if (!File.Exists("lt_config.ini"))
            {
                Terminal.WriteLine("lt_config.ini does not exist!", SeverityLevel.Error);
                Terminal.WriteLine("Generating one...", SeverityLevel.Warning);
                File.WriteAllLines("lt_config.ini", default_config);
                Terminal.WriteLine("Success!", SeverityLevel.Success);
                Terminal.WriteLine("Please restart LucidTerm to see changes!", SeverityLevel.Error);
            } else
            {
                string[] lines = File.ReadAllLines("lt_config.ini");
                foreach (string line in lines)
                {
                    string[] parts = line.Split("=");
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (key.Equals("Prefix", StringComparison.OrdinalIgnoreCase))
                        {
                            appConfig.Prefix = value;
                        }
                        else if (key.Equals("CustomWelcome", StringComparison.OrdinalIgnoreCase))
                        {
                            appConfig.CustomWelcomeMessage = value;
                        }
                        else if (key.Equals("PrimaryColour", StringComparison.OrdinalIgnoreCase))
                        {
                            // Map PrimaryColour string representation to ConsoleColor
                            if (colorMap.TryGetValue(value, out ConsoleColor parsedColor))
                            {
                                term_colour = parsedColor;
                            }
                            else
                            {
                                // Use White as default if the mapping is not found
                                term_colour = ConsoleColor.White;
                            }
                        }
                        else if (key.Equals("Debug", StringComparison.OrdinalIgnoreCase)) 
                        {
                            appConfig.Debug = value;
                        }
                    }
                }
            }

            //Paths
            string con_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "constellation.bat");
            string uni_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launch.bat");
            string blender_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blender.bat");
            string inj_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "injector.bat");
            string wh_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whitehat.bat");
            string para_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parallax2.bat");

            if (!File.Exists("key.txt"))
            {
                string userKey = "";
                do
                {
                    Terminal.WriteLine($"No key file found or invalid key format. Please enter your key in the format 'ABCD-EFGH-IJKL-MNOP'.", SeverityLevel.Error);

                    Console.Write($"{appConfig.Prefix} Enter your key: ");
                    userKey = Console.ReadLine();
                } while (!IsValidKeyFormat(userKey));

                apiKey = userKey; // Set the validated key to the global variable
                File.WriteAllText("key.txt", apiKey);
                Terminal.WriteLine($"Key saved successfully!", SeverityLevel.Success);
            }
            else
            {
                apiKey = File.ReadAllText("key.txt");
                Terminal.WriteLine($"{appConfig.CustomWelcomeMessage}", SeverityLevel.Success);
                Terminal.WriteLine($"Admin: {isAdmin}", SeverityLevel.Warning);
            }

            bool isRunning = true;
            Terminal.WriteLine($"Type 'help' for a list of available commands.", SeverityLevel.Info);

            while (isRunning)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                string[] commandArgs = input.Split(' ');

                string command = commandArgs[0].ToLower();
                List<string> arguments = new List<string>(commandArgs);
                arguments.RemoveAt(0);

                switch (command)
                {
                    //User Control
                    case "protection":
                        if (arguments.Count == 1 && int.TryParse(arguments[0], out int protectionLevel) && protectionLevel >= 0 && protectionLevel <= 4)
                        {
                            Terminal.WriteLine(SendAPIRequest(apiKey, false, "setProtection", "protection", protectionLevel.ToString()), SeverityLevel.Success);

                        }
                        else
                        {
                            Terminal.WriteLine("Invalid 'protection' command. Usage: protection [0-4]", SeverityLevel.Error);
                        }
                        break;
                    case "launch":
                        if (arguments.Count == 1 && int.TryParse(arguments[0], out int softwareType) && softwareType >= 0 && softwareType <= 6)
                        {
                            if (debug)
                            {
                                Terminal.WriteLine($"launching {softwareType.ToString()}...", SeverityLevel.Error);
                            }
                                //Universe
                            if (softwareType == 0)
                            {
                                string download_url = "https://constelia.ai/launch.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(uni_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(uni_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{uni_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("launch.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                            //Constellation
                            if (softwareType == 1)
                            {
                                string download_url = "https://constelia.ai/constellation.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(con_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(con_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{con_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("constellation.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                            //Blender
                            if (softwareType == 2)
                            {
                                string download_url = "https://constelia.ai/blender.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(blender_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(blender_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{blender_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("blender.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                            //Injector
                            if (softwareType == 3)
                            {
                                string download_url = "https://constelia.ai/injector.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(inj_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(inj_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{inj_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("injector.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                            //Whitehat
                            if (softwareType == 4)
                            {
                                string download_url = "https://constelia.ai/whitehat.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(wh_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(wh_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{wh_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("whitehat.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                            //Parallax2
                            if (softwareType == 5)
                            {
                                string download_url = "https://constelia.ai/parallax2.bat";
                                using (HttpClient client = new HttpClient())
                                {
                                    var launchBatUrlResponse = await client.GetAsync(download_url);

                                    if (launchBatUrlResponse.IsSuccessStatusCode)
                                    {
                                        var content = await launchBatUrlResponse.Content.ReadAsByteArrayAsync();
                                        File.WriteAllBytes(para_path, content);
                                    }
                                    else
                                    {
                                        Terminal.WriteLine($"Unable to download file from {download_url}", SeverityLevel.Error);
                                        return;
                                    }
                                }
                                if (File.Exists(para_path))
                                {
                                    Process.Start("cmd.exe", $"/c start \"\" \"{para_path}\"");
                                }
                                else
                                {
                                    Terminal.WriteLine("parallax2.bat does not exist.", SeverityLevel.Error);
                                }
                            }
                        }
                        else
                        {
                            Terminal.WriteLine("Invalid 'launch' command. Usage: protection [0-5]", SeverityLevel.Error);
                        }
                        break;

                    //Scripts
                    case "scripts":
                        if (arguments.Count == 0)
                        {
                            // Run API command for "scripts" without arguments
                            string apiResponse = SendAPIRequest(apiKey, false, "getAllScripts");
                            string activeResponse = SendAPIRequest(apiKey, false, "getMember", "scripts");
                            //Terminal.WriteLine(input + apiResponse);

                            if (apiResponse != null)
                            {
                                // Deserialize the API response into a list of Script objects
                                List<Script> scripts = JsonConvert.DeserializeObject<List<Script>>(apiResponse);
                                ApiResponse apiResponse2 = JsonConvert.DeserializeObject<ApiResponse>(activeResponse);
                                List<Script> activeScripts = apiResponse2.Scripts;

                                // Prioritize and display active scripts first
                                List<Script> prioritizedScripts = new List<Script>();

                                // Add active scripts to the prioritized list
                                foreach (var activeScript in activeScripts)
                                {
                                    Script foundScript = scripts.FirstOrDefault(s => s.id == activeScript.id);
                                    if (foundScript != null)
                                    {
                                        prioritizedScripts.Add(foundScript);
                                        scripts.Remove(foundScript); // Remove the active script from the original list
                                    }
                                }

                                // Add remaining scripts (inactive ones) after active scripts in the prioritized list
                                prioritizedScripts.AddRange(scripts);

                                // Display the scripts in a tabulated format
                                Console.WriteLine($"| {"Name",-30} | {"ID",-10} | {"Last Update",-25} | {"Author",-20} | {"Active",-7} |", SeverityLevel.Info);
                                Console.WriteLine($"| {"",-30} | {"",-10} | {"",-25} | {"",-20} | {"",-7} |", SeverityLevel.Info);

                                foreach (var script in prioritizedScripts)
                                {
                                    string formattedLastUpdate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(script.last_update)).DateTime.ToString();

                                    // Check if the current script is active
                                    bool isActive = activeScripts.Any(activeScript => activeScript.id == script.id);

                                    // Truncate the script name if it exceeds 25 characters
                                    string truncatedName = script.name.Length > 25 ? script.name.Substring(0, 22) + "..." : script.name;

                                    // Displaying scripts with 'x' for active and '+' for inactive
                                    Console.WriteLine($"| {truncatedName,-30} | {script.id,-10} | {formattedLastUpdate,-25} | {script.author,-20} | {(isActive ? "Active" : "Inactive"),-7} |", SeverityLevel.Info);
                                }

                                Console.WriteLine("|", SeverityLevel.Info);
                            }
                            else
                            {
                                Terminal.WriteLine("Failed to fetch scripts. Check API response.", SeverityLevel.Error);
                            }
                        }
                        else if (arguments.Count == 1)
                        {
                            // Logic for when the argument is provided by the user
                            string userArg = arguments[0]; // Placeholder for user-provided argument
                            string userApiResponse = SendAPIRequest(apiKey, false, "toggleScriptStatus", "id", userArg);

                            Terminal.WriteLine($"Toggling script ID: {userArg}", SeverityLevel.Warning);
                            // Add your logic using the user-provided argument here
                        }
                        else
                        {
                            Terminal.WriteLine("Invalid 'scripts' command. Usage: scripts [number]", SeverityLevel.Error);
                        }
                        break;
                    case "config":
                        string viewConfigResponse = SendAPIRequest(apiKey, true, "getConfiguration");
                        if (viewConfigResponse != null)
                        {
                            string cleanJson = RemoveHtmlTags(viewConfigResponse);
                            Terminal.WriteLine(cleanJson, SeverityLevel.Info);
                        }
                        break;
                    case "editconfig":
                        string editConfigResponse = SendAPIRequest(apiKey, true, "getConfiguration");
                        if (editConfigResponse != null)
                        {
                            string cleanJson = RemoveHtmlTags(editConfigResponse);
                            File.WriteAllText("config.json", cleanJson);
                            ExecuteCommand("cmd.exe", "/C " + "code -r config.json");
                        }
                        break;
                    case "pushconfig":
                        if (File.Exists("config.json")) {
                            string pushConfigContents = File.ReadAllText("config.json");
                            //I know this is a weird way of doing this leave me alone.
                            //No shot im remaking my SendAPIRequest func.
                            string encodedConfig = HttpUtility.UrlEncode(pushConfigContents);
                            StringContent content = new StringContent($"value={encodedConfig}", Encoding.UTF8, "application/x-www-form-urlencoded");
                            HttpClient client = new HttpClient();
                            if (debug)
                            {
                                Terminal.WriteLine($"Sending API request: https://constelia.ai/api.php?key={apiKey}&cmd=setConfiguration", SeverityLevel.Info);
                            }
                            HttpResponseMessage response = await client.PostAsync($"https://constelia.ai/api.php?key={apiKey}&cmd=setConfiguration", content);
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                Terminal.WriteLine("Config pushed to cloud.", SeverityLevel.Success);
                            }
                            File.Delete("config.json");
                            if (debug)
                            {
                                Terminal.WriteLine("Removed old config.json", SeverityLevel.Success);
                            }
                        }
                        break;



                    //Misc
                    case "exit":
                        isRunning = false;
                        break;
                    case "debug":
                        debug = !debug;
                        Terminal.WriteLine($"Debug logs: {debug}", SeverityLevel.Warning);
                        break;
                    case "help":
                        DisplayHelp();
                        break;
                    case "info":
                        Terminal.WriteLine("Info (!)");
                        Terminal.WriteLine("|", SeverityLevel.Info);
                        Terminal.WriteLine("|── Minimum (Usermode) & Minimum (Kernel) are marked as they force the solutions to run silently.", SeverityLevel.Info);
                        Terminal.WriteLine("|    |── This means they do not show in the task bar.", SeverityLevel.Info);
                        Terminal.WriteLine("|    └── Essentially hides the solutions. Can cause issues if min-mode is chosen by accident.", SeverityLevel.Info);
                        Terminal.WriteLine("|", SeverityLevel.Info);
                        Terminal.WriteLine("└── Edit config requires you have Visual Studio Code installed and 'code' added to your system PATH.", SeverityLevel.Info);
                        break;
                    case "clear":
                        Terminal.ClearTerminal();
                        break;

                    case "allsoftware":
                        string softwareResponse = SendAPIRequest(apiKey, false, "getAllSoftware");
                        if (softwareResponse != null)
                        {
                            List<software> softwareList = JsonConvert.DeserializeObject<List<software>>(softwareResponse);
                            // Access and work with the softwareList
                            foreach (var software in softwareList)
                            {
                                Console.WriteLine($"Name: {software.Name}, Version: {software.Version}, Last Updated: {software.Elapsed}");
                            }
                        }
                        else
                        {
                            // Handle null response
                            Terminal.WriteLine("Failed to fetch software. Check API response.", SeverityLevel.Error);
                        }
                        break;

                    //Cmd not found
                    default:
                        HandleCommandNotFound(command);
                        break;
                }
            }
        }

        static void HandleCommandNotFound(string command)
        {
            Terminal.WriteLine($"Command '{command}' not found", SeverityLevel.Error);
            Terminal.WriteLine($"Type 'help' for a list of available commands.", SeverityLevel.Info);
        }

        static void DisplayHelp()
        {
            Terminal.WriteLine("Available commands:", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("|── User", SeverityLevel.Info);
            Terminal.WriteLine("|   |── protection: Sets the protection level [0-4]", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    0 = Standard (usermode)", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    1 = IPC/Zombie", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    2 = Kernel Mode Protection", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    3 = Minimum (Usermode)(!)", SeverityLevel.Warning);
            Terminal.WriteLine("|   |──    4 = Minimum (Kernel)(!)", SeverityLevel.Warning);
            Terminal.WriteLine("|   |── launch: Launch various solutions [0-5]", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    0 = Universe4", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    1 = Constellation4", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    2 = Blender", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    3 = Injector", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    4 = Whitehat", SeverityLevel.Info);
            Terminal.WriteLine("|   └──    5 = Parallax2", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("|── Config", SeverityLevel.Info);
            Terminal.WriteLine("|   |── config: Grabs and prints your current raw config.", SeverityLevel.Info);
            Terminal.WriteLine("|   |── editconfig: Grabs your config and allows you to edit and reupload(!).", SeverityLevel.Warning);
            Terminal.WriteLine("|   |── pushconfig: Pushes any changes made via editconfig to the cloud.", SeverityLevel.Info);
            Terminal.WriteLine("|   └── resetconfig: Resets your config back to {}.", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("└── Misc", SeverityLevel.Info);
            Terminal.WriteLine("    |── help: Display available commands and descriptions", SeverityLevel.Info);
            Terminal.WriteLine("    |── info: Shows info about all commands coloured in yellow(!)", SeverityLevel.Info);
            Terminal.WriteLine("    |── debug: Enables verbose logging straight to the terminal", SeverityLevel.Info);
            Terminal.WriteLine("    |── allsoftware: Shows information on all available software", SeverityLevel.Info);
            Terminal.WriteLine("    |── exit: Exit the terminal", SeverityLevel.Info);
            Terminal.WriteLine("    └── clear: Clear the terminal", SeverityLevel.Info);
            // Add other available commands and descriptions here
        }

        public static void ResetApplication()
        {
            Console.Clear();
            Terminal.WriteLine($"Type 'help' for a list of available commands.", SeverityLevel.Info);
        }

        static bool IsValidKeyFormat(string key)
        {
            // Validate the key format
            string pattern = @"^[A-Z]{4}-[A-Z]{4}-[A-Z]{4}-[A-Z]{4}$";
            return System.Text.RegularExpressions.Regex.IsMatch(key, pattern);
        }

        static string SendAPIRequest(string key, bool beautify, string cmd, string argName = null, string argVal = null)
        {
            string url = $"https://constelia.ai/api.php?key={key}&cmd={cmd}";

            if (!string.IsNullOrEmpty(argName) && !string.IsNullOrEmpty(argVal))
            {
                url += $"&{argName}={argVal}";
            }
            else if (!string.IsNullOrEmpty(argName) && string.IsNullOrEmpty(argVal))
            {
                url += $"&{argName}";
            }

            if (beautify)
            {
                url += $"&beautify";
            }

            try
            {
                if (debug)
                {
                    Terminal.WriteLine($"Sending API request: {url}", SeverityLevel.Warning);
                }
                using (var response = httpClient.GetAsync(url).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var streamReader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            string responseData = streamReader.ReadToEnd();
                            return responseData; // Return the API response
                        }
                    }
                    else
                    {
                        Terminal.WriteLine("API Request failed", SeverityLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Terminal.WriteLine($"API Request Error: {ex.Message}", SeverityLevel.Error);
            }

            return null; // Return null in case of an error or unsuccessful request
        }

        static void ExecuteCommand(string command, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = command;
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;

            process.Start();
            process.WaitForExit();
        }

        static string RemoveHtmlTags(string input)
        {
            // Regular expression to remove <pre> and </pre> tags
            string pattern = @"<\s*\/?\s*pre\s*[^>]*>";
            string replacement = "";

            // Remove HTML tags using Regex.Replace
            string result = Regex.Replace(input, pattern, replacement);

            return result;
        }

        static bool IsUserAnAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static class Terminal
        {
            public static void WriteLine(string text, SeverityLevel severity = SeverityLevel.Info)
            {
                string prefix = $"[{appConfig.Prefix}]";

                Console.Write($"{prefix} > ");

                switch (severity)
                {
                    case SeverityLevel.Success:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case SeverityLevel.Info:
                        Console.ForegroundColor = term_colour;
                        break;
                    case SeverityLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case SeverityLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        Console.ForegroundColor = term_colour;
                        break;
                }

                Console.WriteLine(text);
                Console.ResetColor();
            }

            public static void ClearTerminal()
            {
                Console.Clear();
                Program.ResetApplication(); // Restart the application
            }
        }
    }
}