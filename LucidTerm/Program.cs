using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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

    public class Terminal
    {
        public class Script
        {
            public string id { get; set; }
            public string software { get; set; }
            public string name { get; set; }
            public string author { get; set; }
            public string last_update { get; set; }
            public string update_notes { get; set; }
            public string script { get; set; }
            public string core { get; set; }
            public string forums { get; set; }
            public string library { get; set; }
            public List<string> team { get; set; }
            public string last_bonus { get; set; }
            public string elapsed { get; set; }
        }

        public class ApiResponse
        {
            public List<Script> Scripts { get; set; }
        }


        public class TeamConverter : JsonConverter<List<string>>
        {
            public override List<string> ReadJson(JsonReader reader, Type objectType, List<string> existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                List<string> teamList = new List<string>();
                JToken token = JToken.Load(reader);

                if (token.Type == JTokenType.Array)
                {
                    teamList = token.ToObject<List<string>>();
                }
                else if (token.Type == JTokenType.Object)
                {
                    // Assuming the object contains keys and values, extract the values as strings
                    var teamDict = token.ToObject<Dictionary<string, string>>();
                    teamList = teamDict.Values.ToList();
                }

                return teamList;
            }

            public override void WriteJson(JsonWriter writer, List<string> value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

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
                File.WriteAllText("key.txt", apiKey); // For demonstration purposes, save it to file
                Terminal.WriteLine($"Key saved successfully!", SeverityLevel.Success);
            }
            else
            {
                apiKey = File.ReadAllText("key.txt"); // Read the key from file if it exists
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
                            Terminal.WriteLine(SendAPIRequest(apiKey, "setProtection", "protection", protectionLevel.ToString()), SeverityLevel.Success);

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
                            string apiResponse = SendAPIRequest(apiKey, "getAllScripts");
                            string activeResponse = SendAPIRequest(apiKey, "getMember", "scripts");

                            if (apiResponse != null || activeResponse != null)
                            {
                                // Deserialize the API response into a list of Script objects
                                List<Script> scripts = JsonConvert.DeserializeObject<List<Script>>(apiResponse);
                                List<Script> activeObj = JsonConvert.DeserializeObject<List<Script>>(activeResponse);

                                // Display the scripts in a tabulated format
                                Terminal.WriteLine($"| {"Name",-30} | {"ID",-10} | {"Last Update",-25} | {"Author",-20} | {"Active",-7} |", SeverityLevel.Info);
                                Terminal.WriteLine($"| {"",-30} | {"",-10} | {"",-25} | {"",-20} | {"",-7} |", SeverityLevel.Info);

                                foreach (var script in scripts)
                                {
                                    string formattedLastUpdate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(script.last_update)).DateTime.ToString();

                                    // Check if the current script is active
                                    bool isActive = activeObj.Any(activeScript => activeScript.id == script.id);

                                    Terminal.WriteLine($"| {script.name,-30} | {script.id,-10} | {formattedLastUpdate,-25} | {script.author,-20} | {isActive,-7} |", SeverityLevel.Info);
                                }
                                Terminal.WriteLine("|", SeverityLevel.Info);
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
                            string userApiResponse = SendAPIRequest(apiKey, "toggleScriptStatus", "id", userArg);

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
                    case "clear":
                        Terminal.ClearTerminal();
                        break;
                    case "send":
                        if (arguments.Count >= 2)
                        {
                            SendAPIRequest(apiKey, arguments[0], arguments[1]);
                        }
                        else
                        {
                            Terminal.WriteLine("Invalid 'send' command. Usage: send cmd");
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
            Terminal.WriteLine("|   |──    3 = Minimum (Usermode)", SeverityLevel.Warning);
            Terminal.WriteLine("|   └──    4 = Minimum (Kernel)", SeverityLevel.Warning);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("|── Misc", SeverityLevel.Info);
            Terminal.WriteLine("|   |── help: Display available commands and descriptions", SeverityLevel.Info);
            Terminal.WriteLine("|   |── debug: Enables verbose logging straight to the terminal", SeverityLevel.Info);
            Terminal.WriteLine("|   |── allsoftware: Shows information on all available software", SeverityLevel.Info);
            Terminal.WriteLine("|   |── exit: Exit the terminal", SeverityLevel.Info);
            Terminal.WriteLine("|   └── clear: Clear the terminal", SeverityLevel.Info);
            Terminal.WriteLine("|── API Requests", SeverityLevel.Info);
            Terminal.WriteLine("|   └── send <cmd>: Send API request with specified command", SeverityLevel.Info);
            Terminal.WriteLine("|       |──    The following are optional arguments", SeverityLevel.Warning);
            Terminal.WriteLine("|       |──    Argument Name", SeverityLevel.Info);
            Terminal.WriteLine("|       └──    Value", SeverityLevel.Info);
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
            // API request to check if the key is valid
            string url = $"https://constelia.ai/api.php?key={key}";
            try
            {
                var request = WebRequest.Create(url);
                request.Method = "GET";

                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        return true; // Key is valid
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"API Request Error: {ex.Message}");
            }

            return false; // Key is invalid
        }

        static string SendAPIRequest(string key, string cmd, string argName = null, string argVal = null)
        {
            string url = $"https://constelia.ai/api.php?key={key}&cmd={cmd}";

            if (!string.IsNullOrEmpty(argVal) || !string.IsNullOrEmpty(argName))
            {
                url += $"&{argName}={argVal}";
            }

            try
            {
                if (debug) 
                { 
                    Terminal.WriteLine($"Sending API request with following params: &cmd={cmd}, &{argName}={argVal}");
                }
                var request = WebRequest.Create(url);
                request.Method = "GET";

                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var streamReader = new StreamReader(response.GetResponseStream()))
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
            catch (WebException ex)
            {
                Terminal.WriteLine($"API Request Error: {ex.Message}", SeverityLevel.Error);
            }

            return null; // Return null in case of an error or unsuccessful request
        }


    }
}
