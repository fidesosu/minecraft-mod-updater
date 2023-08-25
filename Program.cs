using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MinecraftModNameExtractor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<ModMapping> modMappings = LoadModMappings();

            Console.WriteLine("Enter the path to the folder containing .jar files:");
            string folderPath = Console.ReadLine();

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Invalid folder path.");
                return;
            }

            Console.WriteLine("Enter the Minecraft version in the format 1.x.x:");
            string minecraftVersion = Console.ReadLine();

            Console.WriteLine("Enter the path where the mods should be installed:");
            string installationFolder = Console.ReadLine();

            if (!Directory.Exists(installationFolder))
            {
                Console.WriteLine("Invalid installation folder path.");
                return;
            }

            string[] jarFiles = Directory.GetFiles(folderPath, "*.jar");

            if (jarFiles.Length == 0)
            {
                Console.WriteLine("No .jar files found in the specified folder.");
                return;
            }

            foreach (string jarFilePath in jarFiles)
            {
                string modName = GetMinecraftModName(jarFilePath);

                if (!string.IsNullOrEmpty(modName))
                {
                    string apiModName = FindApiModName(modMappings, modName);

                    bool hasFileForVersion = await CheckModFileForVersion(apiModName, minecraftVersion);

                    Console.WriteLine($"Minecraft Mod Id: {modName}");

                    if (hasFileForVersion)
                    {
                        Console.WriteLine($"Compatible with Minecraft {minecraftVersion}");

                        Console.WriteLine($"{apiModName}");

                        // Get the slug for the mod from the API
                        string slug = await GetModSlug(apiModName);
                        if (!string.IsNullOrEmpty(slug))
                        {
                            // Construct the download URL using the slug
                            string downloadUrl = $"https://modrinth.com/mod/{slug}/versions?l=fabric&g={minecraftVersion}";
                            await DownloadModFile(downloadUrl, installationFolder);
                        }
                        else
                        {
                            Console.WriteLine("Failed to retrieve mod information from Modrinth API.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Not compatible with Minecraft {minecraftVersion}");
                    }

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Failed to extract mod name for {Path.GetFileName(jarFilePath)}.");
                    Console.WriteLine();
                }
            }
        }

        static List<ModMapping> LoadModMappings()
        {
            string configFilePath = "mod_mappings.json";

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("Configuration file 'mod_mappings.json' not found. Creating a template.");

                // Create a template mod mappings list
                var templateModMappings = new List<ModMapping>
                {
                    new ModMapping { ExpectedName = "Example Mod", ApiName = "example-mod-api" }
                };

                // Serialize the template mod mappings to JSON and save to the file
                string templateJsonContent = JsonSerializer.Serialize(templateModMappings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, templateJsonContent);

                Console.WriteLine("A template 'mod_mappings.json' has been created. Add your mod mappings and press any key to continue.");

                // Wait for the user to press a key before proceeding
                Console.ReadKey();

                // Clear the CMD window after pressing a key
                Console.Clear();
            }

            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                return JsonSerializer.Deserialize<List<ModMapping>>(jsonContent);
            }
            catch (JsonException)
            {
                Console.WriteLine("Error while parsing configuration file. Using default mappings.");
                return new List<ModMapping>();
            }
        }

        static string FindApiModName(List<ModMapping> modMappings, string expectedName)
        {
            foreach (var mapping in modMappings)
            {
                if (mapping.ExpectedName == expectedName)
                {
                    return mapping.ApiName;
                }
            }

            return expectedName;
        }


        static async Task<string> GetModSlug(string modName)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string apiUrl = $"https://api.modrinth.com/v2/project/{modName}";
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        using (JsonDocument document = JsonDocument.Parse(content))
                        {
                            if (document.RootElement.TryGetProperty("slug", out JsonElement slugElement))
                            {
                                return slugElement.GetString();
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Handle HTTP request exceptions here if needed
            }
            catch (JsonException)
            {
                // Handle JSON parsing exceptions here if needed
            }

            return null;
        }

        static async Task DownloadModFile(string modPageUrl, string installationFolder)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(modPageUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string htmlContent = await response.Content.ReadAsStringAsync();

                        // Use HtmlAgilityPack to parse the HTML content
                        HtmlDocument htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(htmlContent);

                        // Find the first anchor element with the class 'download-button'
                        HtmlNode downloadLinkNode = htmlDoc.DocumentNode.SelectSingleNode("//a[contains(@class, 'download-button')]");
                        if (downloadLinkNode != null)
                        {
                            string downloadLink = downloadLinkNode.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(downloadLink))
                            {
                                Console.WriteLine($"Download URL: {downloadLink}"); // Print the download URL

                                // Download the mod file
                                HttpResponseMessage fileResponse = await httpClient.GetAsync(downloadLink);
                                if (fileResponse.IsSuccessStatusCode)
                                {
                                    string fileName = Path.Combine(installationFolder, Path.GetFileName(downloadLink));
                                    byte[] fileBytes = await fileResponse.Content.ReadAsByteArrayAsync();

                                    File.WriteAllBytes(fileName, fileBytes);

                                    Console.WriteLine($"Mod downloaded and installed: {fileName}");
                                    await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for 5 seconds, because of modrinth rate limit
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to download mod from URL: {downloadLink}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No download links found in the mod page.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch mod page from URL: {modPageUrl}");
                    }
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Error while making HTTP request.");
            }
        }

        static async Task<bool> CheckModFileForVersion(string modName, string minecraftVersion)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string apiUrl = $"https://api.modrinth.com/v2/project/{modName}";
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        using (JsonDocument document = JsonDocument.Parse(content))
                        {
                            // Assuming the "game_versions" field contains the list of versions
                            if (document.RootElement.TryGetProperty("game_versions", out JsonElement versionsElement))
                            {
                                foreach (var versionElement in versionsElement.EnumerateArray())
                                {
                                    string version = versionElement.GetString();
                                    if (version == minecraftVersion)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Handle HTTP request exceptions here if needed
            }
            catch (JsonException)
            {
                // Handle JSON parsing exceptions here if needed
            }

            return false;
        }

        static string GetMinecraftModName(string jarFilePath)
        {
            string modName = null;

            using (ZipArchive zipArchive = ZipFile.OpenRead(jarFilePath))
            {
                var fabricModJsonEntry = zipArchive.GetEntry("fabric.mod.json");

                if (fabricModJsonEntry != null)
                {
                    using (StreamReader streamReader = new StreamReader(fabricModJsonEntry.Open()))
                    {
                        string fabricModJson = streamReader.ReadToEnd();
                        modName = ParseModNameFromJson(fabricModJson);
                    }
                }
                else
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        if (entry.FullName.Equals("mcmod.info", StringComparison.OrdinalIgnoreCase))
                        {
                            using (StreamReader streamReader = new StreamReader(entry.Open()))
                            {
                                string mcModInfoJson = streamReader.ReadToEnd();
                                modName = ParseModNameFromJson(mcModInfoJson);
                                break;
                            }
                        }
                    }
                }
            }

            return modName;
        }

        static string ParseModNameFromJson(string jsonContent)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(jsonContent))
                {
                    if (document.RootElement.TryGetProperty("id", out JsonElement modIdElement))
                    {
                        return modIdElement.GetString();
                    }
                    else if (document.RootElement.TryGetProperty("name", out JsonElement nameElement))
                    {
                        return nameElement.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Handle any JSON parsing exceptions here if needed
            }

            return null;
        }
    }
    public class ModMapping
    {
        public string ExpectedName { get; set; }
        public string ApiName { get; set; }
    }
}

