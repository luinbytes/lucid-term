using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CustomTerminal.Terminal;

namespace CustomTerminal
{
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

    public static class Terminal
    {
        public static void WriteLine(string text, SeverityLevel severity = SeverityLevel.Info)
        {
            string prefix = "[lucidTerm]";

            Console.Write($"{prefix} > ");

            switch (severity)
            {
                case SeverityLevel.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case SeverityLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case SeverityLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case SeverityLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
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

    class Program
    {
        static HttpClient httpClient = new HttpClient();
        static string apiKey = ""; // Global variable to store the validated key
        static bool debug = false;

        static async Task Main(string[] args)
        {
            string prefix = "[lucidTerm]";

            if (!File.Exists("key.txt"))
            {
                string userKey = "";
                do
                {
                    Terminal.WriteLine($"No key file found or invalid key format. Please enter your key in the format 'ABCD-EFGH-IJKL-MNOP'.", SeverityLevel.Error);

                    Console.Write($"{prefix} Enter your key: ");
                    userKey = Console.ReadLine();
                } while (!IsValidKeyFormat(userKey) || !IsValidAPIKey(userKey));

                apiKey = userKey; // Set the validated key to the global variable
                File.WriteAllText("key.txt", apiKey);
                Terminal.WriteLine($"Key saved successfully!", SeverityLevel.Success);
            }
            else
            {
                apiKey = File.ReadAllText("key.txt");
                Terminal.WriteLine($"Welcome back!", SeverityLevel.Success);
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
                        Terminal.WriteLine("└── Minimum (Usermode) & Minimum (Kernel) are marked as they force the solutions to run silently.", SeverityLevel.Info);
                        Terminal.WriteLine("    |── This means they do not show in the task bar.", SeverityLevel.Info);
                        Terminal.WriteLine("    └── Essentially hides the solutions. Can cause issues if min-mode is chosen by accident.", SeverityLevel.Info);
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
            Terminal.WriteLine("|   └──    4 = Minimum (Kernel)(!)", SeverityLevel.Warning);
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

        static bool IsValidAPIKey(string key)
        {
            // Local validation method
            // Implement the logic to validate the API key locally
            // ...

            return true; // Placeholder for local validation
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
    }
}