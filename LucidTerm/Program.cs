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
using System.Linq;
//using static CustomTerminal.Terminal;

namespace CustomTerminal
{
    public enum SeverityLevel
    {
        Success,
        Info,
        Warning,
        Error
    }

    public class AppConfig
    {
        public string Prefix { get; set; }
        public string CustomWelcomeMessage { get; set; }
        public string PrimaryColour { get; set; }
        public string Debug { get; set; }
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
        static string VER = "v1.4.0";

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

            //Paths
            string con_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "constellation.bat");
            string uni_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launch.bat");
            string blender_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blender.bat");
            string inj_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "injector.bat");
            string wh_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whitehat.bat");
            string para_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parallax2.bat");
            string script_profiles_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "/lt_script_profiles/");

            //Config
            string[] default_config =
            {
                "PrimaryColour = White",
                "Prefix = Lucid",
                "CustomWelcome = Welcome!!! <3 YOU CAN CHANGE ME IN LT_CONFIG.INI :D",
                "Debug = false" //useless rn
            };

            //Check if config exists
            if (!File.Exists("lt_config.ini"))
            {
                Terminal.WriteLine("lt_config.ini does not exist! Generating one...", SeverityLevel.Error);
                File.WriteAllLines("lt_config.ini", default_config);
                Terminal.WriteLine("Success!", SeverityLevel.Success);
                Terminal.WriteLine("!!!Please restart LucidTerm to see changes!!!", SeverityLevel.Error);
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
                            if (colorMap.TryGetValue(value, out ConsoleColor parsedColor))
                            {
                                term_colour = parsedColor;
                            }
                            else
                            {
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

            //Check if script profiles dir exists. If not, make one.
            if (!Directory.Exists(script_profiles_path))
            {
                Directory.CreateDirectory(script_profiles_path);
                if (debug)
                {
                    Terminal.WriteLine("Script Profiles (lt_script_profiles) dir created.");
                }
            } else
            {
                if (debug)
                {
                    Terminal.WriteLine("Script Profiles dir exists... Skipping.");
                }
            }

            if (!File.Exists("key.txt"))
            {
                string userKey = "";
                do
                {
                    Terminal.WriteLine($"No key file found or invalid key format. Please enter your key in the format 'ABCD-EFGH-IJKL-MNOP'.", SeverityLevel.Error);

                    Console.Write($"{appConfig.Prefix} Enter your key: ");
                    userKey = Console.ReadLine();
                } while (!IsValidKeyFormat(userKey));

                apiKey = userKey;
                File.WriteAllText("key.txt", apiKey);
                Terminal.WriteLine($"Key saved successfully!", SeverityLevel.Success);
            }
            else
            {
                apiKey = File.ReadAllText("key.txt");
                Terminal.WriteLine($"{appConfig.CustomWelcomeMessage}", SeverityLevel.Info);
                if (isAdmin)
                {
                    Terminal.WriteLine($"Admin: {isAdmin}. Can launch solutions in kernel mode.", SeverityLevel.Success);
                } else
                {
                    Terminal.WriteLine($"Admin: {isAdmin}. Cannot launch solutions in kernel mode.", SeverityLevel.Warning);
                }
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
                            SendAPIRequest(true, apiKey, false, "setProtection", "protection", protectionLevel.ToString());

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
                    case "link":
                        if (arguments.Count == 1 && int.TryParse(arguments[0], out int linkInt) && linkInt >= 0 && linkInt <= 254)
                        {
                            SendAPIRequest(true, apiKey, false, "setKeys", "link", linkInt.ToString());
                        }
                        break;
                    case "stop":
                        if (arguments.Count == 1 && int.TryParse(arguments[0], out int stopInt) && stopInt >= 0 && stopInt <= 254)
                        {
                            SendAPIRequest(true, apiKey, false, "setKeys", "stop", stopInt.ToString());
                        }
                        else
                        {
                            Terminal.WriteLine("Please provide a valid key code https://cherrytree.at/misc/vk.htm", SeverityLevel.Error);
                        }
                        break;
                    case "forumposts":
                        if (arguments.Count == 1 && int.TryParse(arguments[0], out int postCount) && postCount >= 0 && postCount <= 5)
                        {
                            SendAPIRequest(true, apiKey, true, "getForumPosts", "count", postCount.ToString());
                        }
                        else
                        {
                            Terminal.WriteLine("Please provide a valid count of forum posts to list [0-5]", SeverityLevel.Error);
                        }
                        break;

                    //Scripts
                    case "scripts":
                        if (arguments.Count == 0)
                        {
                            // Run API command for "scripts" without arguments
                            string apiResponse = SendAPIRequest(false, apiKey, false, "getAllScripts");
                            string activeResponse = SendAPIRequest(false, apiKey, false, "getMember", "scripts");

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
                            string userApiResponse = SendAPIRequest(false, apiKey, false, "toggleScriptStatus", "id", userArg);

                            Terminal.WriteLine($"Toggling script ID: {userArg}", SeverityLevel.Warning);
                            // Add your logic using the user-provided argument here
                        }
                        else
                        {
                            Terminal.WriteLine("Invalid 'scripts' command. Usage: scripts [number]", SeverityLevel.Error);
                        }
                        break;
                    case "config":
                        string viewConfigResponse = SendAPIRequest(false, apiKey, true, "getConfiguration");
                        if (viewConfigResponse != null)
                        {
                            string cleanJson = RemoveHtmlTags(viewConfigResponse);
                            Terminal.WriteLine(cleanJson, SeverityLevel.Info);
                        }
                        break;
                    case "editconfig":
                        string editConfigResponse = SendAPIRequest(false, apiKey, true, "getConfiguration");
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
                    case "resetconfig":
                        SendAPIRequest(true, apiKey, false, "resetConfiguration");
                        break;

                    //Script Profiles
                    case "createprofile":
                        if (arguments.Count >= 2)
                        {
                            string profileName = arguments[0];
                            List<int> scriptIds = new List<int>();

                            // Parsing script ids
                            for (int i = 1; i < arguments.Count; i++)
                            {
                                if (int.TryParse(arguments[i], out int id))
                                {
                                    scriptIds.Add(id);
                                }
                                else
                                {
                                    Console.WriteLine($"Invalid script ID: {arguments[i]}");
                                    return; // Exit the command if any invalid ID is encountered
                                }
                            }

                            // Create the profile
                            CreateProfile(profileName, scriptIds);
                        }
                        else
                        {
                            Console.WriteLine("Invalid arguments for creating a profile.");
                        }
                        break;
                    case "readprofile":
                        if (arguments.Count == 1)
                        {
                            string profileName = arguments[0];
                            ReadProfile(profileName);
                        }
                        else
                        {
                            Console.WriteLine("Invalid arguments for reading a profile.");
                        }
                        break;
                    case "setprofile":
                        if (arguments.Count == 1)
                        {
                            string profileName = arguments[0];
                            SyncProfileScripts(profileName);
                        }
                        break;


                    //API
                    case "api":
                        Console.Clear();
                        bool beautify = false;
                        string api_command = null;
                        string api_arg = null;
                        string api_argVal = null;
                        bool built = false;

                        Terminal.WriteLine("< LucidTerm Request Builder >", SeverityLevel.Success);
                        Terminal.WriteLine("");
                        Terminal.WriteLine("Would you like to beautify the JSON response? (Y/N): ");
                        string user_beautify = Console.ReadLine().ToLower();
                        if (user_beautify == "y")
                        {
                            beautify = true;
                        }

                        Terminal.WriteLine("Enter the command you would like to request (e.g. getAllScripts): ");
                        api_command = Console.ReadLine();

                        Terminal.WriteLine("Enter the commands argument (if any. Leave empty if not required): ");
                        api_arg = Console.ReadLine();

                        if (!string.IsNullOrEmpty(api_arg))
                        {
                            Terminal.WriteLine("Enter the argument value (if any. Leave empty if not required): ");
                            api_argVal = Console.ReadLine();
                        }
                        
                        Terminal.WriteLine("API Request built!", SeverityLevel.Success);
                        Terminal.WriteLine($"Beautify: {beautify}");
                        Terminal.WriteLine($"CMD: {api_command}");
                        Terminal.WriteLine($"Argument: {api_arg}");
                        Terminal.WriteLine($"Argument Value: {api_argVal}");
                        Terminal.WriteLine("");
                        Terminal.WriteLine("Press enter to send!", SeverityLevel.Warning);
                        built = true;
                        Console.ReadLine();

                        if (built)
                        {
                            SendAPIRequest(true, apiKey, beautify, api_command, api_arg, api_argVal);
                            built = false;
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
                        Terminal.WriteLine("|── Minimum (Usermode) & Minimum (Kernel) are marked as they force the solutions to run silently", SeverityLevel.Info);
                        Terminal.WriteLine("|    |── This means they do not show in the task bar.", SeverityLevel.Info);
                        Terminal.WriteLine("|    └── Essentially hides the solutions. Can cause issues if min-mode is chosen by accident", SeverityLevel.Info);
                        Terminal.WriteLine("|", SeverityLevel.Info);
                        Terminal.WriteLine("|── Edit config requires you have Visual Studio Code installed and 'code' added to your system PATH", SeverityLevel.Info);
                        Terminal.WriteLine("|", SeverityLevel.Info);
                        Terminal.WriteLine("|── constelia is marked as you need the perk 'Bond Between Human and AI'", SeverityLevel.Info);
                        Terminal.WriteLine("|", SeverityLevel.Info);
                        Terminal.WriteLine("└── API Command is marked as it is the only arg required", SeverityLevel.Info);
                        break;
                    case "clear":
                        Terminal.ClearTerminal();
                        break;

                    case "allsoftware":
                        string softwareResponse = SendAPIRequest(false, apiKey, false, "getAllSoftware");
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
                    case "constelia":
                        //W.I.P.
                        break;
                    case "roll":
                        SendAPIRequest(true, apiKey, true, "rollLoot");
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
            Terminal.WriteLine("|   |", SeverityLevel.Info);
            Terminal.WriteLine("|   |── launch: Launch various solutions [0-5]", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    0 = Universe4", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    1 = Constellation4", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    2 = Blender", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    3 = Injector", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    4 = Whitehat", SeverityLevel.Info);
            Terminal.WriteLine("|   |──    5 = Parallax2", SeverityLevel.Info);
            Terminal.WriteLine("|   |", SeverityLevel.Info);
            Terminal.WriteLine("|   |── link: set your link key (https://cherrytree.at/misc/vk.htm)", SeverityLevel.Info);
            Terminal.WriteLine("|   |── stop: set your stop key (https://cherrytree.at/misc/vk.htm)", SeverityLevel.Info);
            Terminal.WriteLine("|   └── forumposts: grabs recent forum posts [0-5]", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("|── Config", SeverityLevel.Info);
            Terminal.WriteLine("|   |── config: Grabs and prints your current raw config", SeverityLevel.Info);
            Terminal.WriteLine("|   |── editconfig: Grabs your config and allows you to edit and reupload(!)", SeverityLevel.Warning);
            Terminal.WriteLine("|   |── pushconfig: Pushes any changes made via editconfig to the cloud", SeverityLevel.Info);
            Terminal.WriteLine("|   └── resetconfig: Resets your config", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("|── API (Request Builder)", SeverityLevel.Info);
            Terminal.WriteLine("|   |── Beautify: Optional. Beautify's the JSON response. Useful for big JSON blocks", SeverityLevel.Info);
            Terminal.WriteLine("|   |── Command: Constelia.ai API command (e.g. getAllScripts, getMember)", SeverityLevel.Warning);
            Terminal.WriteLine("|   |── Argument: Optional. Command Argument, some commands require an argument and arg value", SeverityLevel.Info);
            Terminal.WriteLine("|   └── Arg Value: Optional. Value for argument", SeverityLevel.Info);
            Terminal.WriteLine("|", SeverityLevel.Info);
            Terminal.WriteLine("└── Misc", SeverityLevel.Info);
            Terminal.WriteLine("    |── help: Display available commands and descriptions", SeverityLevel.Info);
            Terminal.WriteLine("    |── info: Shows info about all commands coloured in yellow(!)", SeverityLevel.Info);
            Terminal.WriteLine("    |── debug: Enables verbose logging straight to the terminal", SeverityLevel.Info);
            Terminal.WriteLine("    |── allsoftware: Shows information on all available software", SeverityLevel.Info);
            Terminal.WriteLine("    |── constelia: Ask constelia! (!)", SeverityLevel.Warning);
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

        static string SendAPIRequest(bool print, string key, bool beautifyJson, string cmd, string cmdArgument = null, string cmdArgValue = null)
        {
            string baseUrl = $"https://constelia.ai/api.php?key={key}&cmd={cmd}";

            if (string.IsNullOrEmpty(cmd)) {
                Terminal.WriteLine("cmd paramater required!", SeverityLevel.Error);
            }
            else
            {
                if (!string.IsNullOrEmpty(cmdArgument) && string.IsNullOrEmpty(cmdArgValue))
                {
                    baseUrl += $"&{cmdArgument}";
                } 
                else if (!string.IsNullOrEmpty(cmdArgument) &&  !string.IsNullOrEmpty(cmdArgValue))
                {
                    baseUrl += $"&{cmdArgument}={cmdArgValue}";
                }

                if (beautifyJson)
                {
                    baseUrl += $"&beautify";
                }

                try
                {
                    if (debug)
                    {
                        Terminal.WriteLine($"POSTing API request: {baseUrl}", SeverityLevel.Info);
                    }
                    using (var response = httpClient.GetAsync(baseUrl).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (var streamReader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                            {
                                string responseData = RemoveHtmlTags(streamReader.ReadToEnd());
                                if (print)
                                {
                                    Terminal.WriteLine(responseData, SeverityLevel.Warning);
                                }
                                return responseData;
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
            }
            return null;
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

        static void CreateProfile(string profileName, List<int> scriptIds)
        {
            string script_profiles_path = Path.Combine(Directory.GetCurrentDirectory(), "lt_script_profiles");
            string profileFilePath = Path.Combine(script_profiles_path, $"{profileName}.ini");
            if (debug)
            {
                Terminal.WriteLine($"Writing Script Profile {profileName}.ini to {profileFilePath}...");
            }

            using (StreamWriter writer = new StreamWriter(profileFilePath))
            {
                writer.WriteLine("[Profile]");
                writer.WriteLine($"Name={profileName}");

                writer.WriteLine("[ScriptIDs]");
                foreach (int id in scriptIds)
                {
                    writer.WriteLine($"ID={id}");
                }
            }
            if (debug)
            {
                Terminal.WriteLine("Finished writing profile.");
            }
        }

        static void ReadProfile(string profileName)
        {
            // Directory path for profiles
            string profilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "lt_script_profiles");

            // File path for the profile INI
            string profileFilePath = Path.Combine(profilesDirectory, $"{profileName}.ini");

            // Check if the profile INI file exists
            if (!File.Exists(profileFilePath))
            {
                Console.WriteLine($"Profile '{profileName}' does not exist.");
                return;
            }

            // Read profile information from the INI file
            Dictionary<string, List<int>> profileData = new Dictionary<string, List<int>>();
            string currentSection = "";

            using (StreamReader reader = new StreamReader(profileFilePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.StartsWith("["))
                    {
                        currentSection = line.Trim('[', ']');
                        profileData[currentSection] = new List<int>();
                    }
                    else if (!string.IsNullOrWhiteSpace(currentSection) && line.Contains("="))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2 && parts[0].Trim() == "ID" && int.TryParse(parts[1].Trim(), out int id))
                        {
                            profileData[currentSection].Add(id);
                        }
                    }
                }
            }

            // Display profile information
            Terminal.WriteLine($"Profile '{profileName}' information:", SeverityLevel.Success);
            foreach (KeyValuePair<string, List<int>> section in profileData)
            {
                Terminal.WriteLine($"[{section.Key}]");
                foreach (int id in section.Value)
                {
                    Terminal.WriteLine($"ID={id}");
                }
            }
        }

        static void SyncProfileScripts(string profileName)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string baseUrl = "https://constelia.ai/api.php";
                baseUrl += $"?key={apiKey}";

                try
                {
                    // Run API command to get all available scripts
                    HttpResponseMessage allScriptsResponse = httpClient.GetAsync($"{baseUrl}&cmd=getAllScripts").Result;
                    allScriptsResponse.EnsureSuccessStatusCode();
                    string allScriptsContent = allScriptsResponse.Content.ReadAsStringAsync().Result;

                    // Run API command to get currently enabled scripts
                    HttpResponseMessage activeResponse = httpClient.GetAsync($"{baseUrl}&cmd=getMember&scripts").Result;
                    activeResponse.EnsureSuccessStatusCode();
                    string activeContent = activeResponse.Content.ReadAsStringAsync().Result;

                    List<Script> allScripts = JsonConvert.DeserializeObject<List<Script>>(allScriptsContent);
                    ApiResponse activeApiResponse = JsonConvert.DeserializeObject<ApiResponse>(activeContent);
                    List<Script> activeScripts = activeApiResponse.Scripts;

                    List<int> profileScriptIds = GetProfileScriptIds(profileName);

                    // Enable/disable scripts based on profileScriptIds and activeScripts
                    foreach (var script in allScripts)
                    {
                        bool isActive = profileScriptIds.Contains(Int32.Parse(script.id));
                        bool currentlyEnabled = activeScripts.Any(activeScript => activeScript.id == script.id);

                        if ((isActive && !currentlyEnabled) || (!isActive && currentlyEnabled))
                        {
                            string toggleUrl = $"{baseUrl}&cmd=toggleScriptStatus&id={script.id}";
                            HttpResponseMessage toggleResponse = httpClient.GetAsync(toggleUrl).Result;
                            toggleResponse.EnsureSuccessStatusCode();

                            if (debug)
                            {
                                Terminal.WriteLine($"Toggled script with ID: {script.id}");
                            }
                        }
                    }

                    Terminal.WriteLine($"<{profileName}> Script sync complete.");
                }
                catch (HttpRequestException ex)
                {
                    Terminal.WriteLine($"HTTP Request Error: {ex.Message}", SeverityLevel.Error);
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine($"Error: {ex.Message}", SeverityLevel.Error);
                }
            }
        }

        static List<int> GetProfileScriptIds(string profileName)
        {
            List<int> profileScriptIds = new List<int>();

            // Example: Read script IDs from an INI file
            string profilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "lt_script_profiles");
            string profileFilePath = Path.Combine(profilesDirectory, $"{profileName}.ini");

            if (File.Exists(profileFilePath))
            {
                try
                {
                    if (debug)
                    {
                        Terminal.WriteLine($"Reading profile file: {profileFilePath}");
                    }

                    // Read all lines from the INI file
                    string[] lines = File.ReadAllLines(profileFilePath);

                    bool isProfileSection = false;
                    bool isScriptIDsSection = false;

                    // Iterate through each line in the file
                    foreach (string line in lines)
                    {
                        if (debug)
                        {
                            Terminal.WriteLine($"Reading line: {line}");
                        }

                        // Check if the line starts with "[Profile]"
                        if (line.Trim().StartsWith("[Profile", StringComparison.OrdinalIgnoreCase))
                        {
                            isProfileSection = true;
                        }
                        else if (isProfileSection)
                        {
                            // Check if the line starts with "[ScriptIDs]" or contains it
                            if (line.Trim().StartsWith("[ScriptIDs", StringComparison.OrdinalIgnoreCase) ||
                                line.Trim().Equals("[ScriptIDs]", StringComparison.OrdinalIgnoreCase))
                            {
                                // Start reading script IDs
                                isScriptIDsSection = true;
                                isProfileSection = false; // Exit profile section
                            }
                        }
                        else if (isScriptIDsSection)
                        {
                            if (debug)
                            {
                                Terminal.WriteLine("Parsing script ID...");
                            }

                            // Parse the script ID from the line and add it to the list
                            if (int.TryParse(line.Trim().Replace("ID=", ""), out int scriptId))
                            {
                                if (debug)
                                {
                                    Terminal.WriteLine($"Found script ID: {scriptId}", SeverityLevel.Success);
                                }
                                profileScriptIds.Add(scriptId);
                            }
                            else
                            {
                                Terminal.WriteLine($"Invalid script ID format: {line}", SeverityLevel.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine($"Error reading profile file: {ex.Message}", SeverityLevel.Error);
                }
            }
            else
            {
                Terminal.WriteLine($"Profile '{profileName}' does not exist.", SeverityLevel.Error);
            }

            return profileScriptIds;
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