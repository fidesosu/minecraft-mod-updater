# Minecraft Mod Downloader

This C# program performs the following tasks:

1. **Load Mod Mappings:** It starts by loading mod mappings from a JSON file named mod_mappings.json. This JSON file contains a list of objects where each object represents a mod mapping. The mapping links an expected mod name (the name you extract from the mod JAR) to an API name (the name used in the Modrinth API).

2. **User Input:** It prompts the user to provide the following inputs:
        Path to the folder containing .jar files.
        Minecraft version in the format 1.x.x.
        Path where the mods should be installed.

3. **Finding .jar Files:** It searches the provided folder for .jar files and stores their paths in an array.

4. **Iterate Through Mods:** For each .jar file found:<br>
        It extracts the Minecraft mod name from the JAR's fabric.mod.json or mcmod.info (using ZipArchive).<br>
        It tries to find the corresponding API name based on the loaded mod mappings.<br>
        It checks if the mod is compatible with the provided Minecraft version by querying the Modrinth API using the API name.<br>
        If compatible, it gets the mod's slug from the API and constructs the download URL.<br>
        It downloads the mod's .jar file using the constructed URL and places it in the specified installation folder.

5. **Downloading Mods:** The DownloadModFile method performs the actual downloading of mods. It uses HttpClient to fetch the HTML content of the mod page, then it uses HtmlAgilityPack to parse the HTML and find the download link (.download-button class) for the mod with the specified Minecraft version. It downloads the mod's .jar file and saves it to the installation folder. It also waits for 5 seconds after downloading a mod due to rate limits from Modrinth.

6. **Checking Compatibility:** The CheckModFileForVersion method queries the Modrinth API to check if a mod is compatible with the provided Minecraft version. It does this by retrieving the mod's data from the API and checking if the provided version is listed in the "game_versions" field.

7. **Error Handling:** The code is wrapped in try-catch blocks to handle exceptions that might occur during HTTP requests, JSON parsing, and file operations. If an error occurs, an appropriate error message is displayed.

8. **Configuration File Handling:** The program reads the mod_mappings.json file to load mod mappings. If the file doesn't exist, it creates a template file with an example mod mapping.

This program automates the process of downloading mods for specific versions of Minecraft Fabric using the Modrinth API. It provides error handling and user prompts for input, making it a functional tool for managing Minecraft mods.

## Prerequisites

- .NET Core SDK (version 3.1 or later)

## How to Use

1. Download and open the executable.

2. Enter the path to your Minecraft mod folder, what version you want to download the mods for and where you want to download them to.

3. Wait until the program has finished downloading the mods and the mods should have downloaded.

### NOTE:
The program probably will not download all of the mods, because it takes the id from the fabric.mod.json

## Configuration

The application uses a JSON configuration file named mod_mappings.json to map expected mod names to API names. If the file doesn't exist, a template will be created for you to add your mod mappings. In the template the ```ExpectedName``` is the id inside the ```fabric.mod.json``` that is inside the .jar file and the ApiName is found in the API the easiest way to find the slug is to find the mod you want in the modrinth website and look at the end of the url when on the mod's page.
