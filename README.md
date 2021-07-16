# Silk!
Silk is a simple and fastest Discord bot written in C# until proven otherwise. Silk is built on top of the [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) .NET Discord library, with the goal to not only be a great bot but to also fill in the gaps that some bots have. Silk aims to be a bot your members will want to use, while not being a cookie-cutter game, moderation, or entertainment bot. Silk is also made with large guilds in mind. Feel free to join the [Silk! Server](https://discord.gg/HZfZb95) to ask any questions you may have or any general support you may need. Want this bot on your server? [Feel free to invite it!](https://discord.com/api/oauth2/authorize?client_id=721514294587424888&permissions=502656214&scope=bot%20applications.commands)

[![CodeQuality](https://www.codefactor.io/repository/github/velvetthepanda/silk/badge)](https://www.codefactor.io/repository/github/velvetthepanda/silk)
![CodeSize](https://img.shields.io/github/languages/code-size/VelvetThePanda/Silk)
![Lines of code](https://img.shields.io/tokei/lines/github/VelvetThePanda/Silk)
![GitHub closed issues](https://img.shields.io/github/issues-closed-raw/VelvetThePanda/Silk)
![Discord](https://img.shields.io/discord/721518523704410202)


---


## **Development** 
Silk! is an open-source bot, and if you know C#, you're more than welcome to contribute! If you're on the server, and your PR gets merged, you get a nice light blue role on the server to signify your contributions. Active contributors are given a special role as well.

### **Resources**
- [PostgreSQL](https://www.postgresql.org/)
- [App Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Discord Developer Portal](https://discord.com/developers)


---


### **Database**
Silk uses [PostgreSQL](https://www.postgresql.org/) for its backend database store. So if you don't have PostgreSQL installed, make sure to install it before trying to run the Visual Studio solution.


---


### **Configuration / Secrets Management**
- To manage storage of the needed `Discord Bot Token` and `Database Connection String`, and other important project configuration/settings, Silk uses an `appSettings.json` file.

#### **Default**
#### When you clone Silk, before running project, you'll need to edit the template `appSettings.json` file in the following directory: `src\Silk.Core`

<br/>

1. For the **Database**, look for the `Persistence` section of the file, it will look something like this:
   - ```json 
      "Persistence": {
         "Host": "localhost",
         "Port": "5432",
         "Database": "",
         "Username": "postgres",
         "Password": ""
      }
     ```
   - From there, just give the `Username` key a value (database name), and the `Password` key a value (password for the Postgres user).

<br/>

2. Now for the **Discord Bot Token**, look for the  `Discord` section of the file, it will look something like this:
    - ```json
      "Discord": {
         "Shards": 1,
         "ClientId": "",
         "ClientSecret": "",
         "BotToken": ""
      }
      ```
   - From there, just give set the `BotToken` key a value (your Discord Bot Token)
     - If you don't have your token off-hand, you can get it from the [Discord Developer Portal](https://discord.com/developers); Select your application once logged in, then select `Bot` in the menu, and you should be able to Reveal your token from there.

#### **App Secrets**
- The alternative, and more recommended (but still **NOT** suitable for `Production`) for managing the configuration file is using `User Secrets`

- The great advantage of using User Secrets, is that the file is stored in a separate location from the project tree, and because of this, secrets also aren't checked into source control.

- The location of the `secrets.json` file is different between Operating Systems, but it's stored in a system-protected user profile folder on your computer.
   - **Windows** 
     - `%APPDATA%\Microsoft\UserSecrets\VelvetThePanda-SilkBot\secrets.json`
   - **Mac / Linux** 
     - `~/.microsoft/usersecrets/VelvetThePanda-SilkBot/secrets.json`

1. In the `Silk.Core` project, a `UserSecretsId` property is defined in the `Silk.Core.csproj` file which looks like:
   - ```
     <UserSecretsId>VelvetThePanda-SilkBot</UserSecretsId>
     ```

2. The structure is the same for the [Default](#default) - `appSettings.json` approach
   - Just copy what's in the template, paste it in the `secrets.json` file and fill in the needed pieces and you're good to go!


---


### Running the Project
- If you're starting fresh, just cloned the repo, then you'll need to make sure you've done the needed [Configuration](#configuration--secrets-management), before running or debugging Silk.

- If you're already using Silk, you may need to do any or all of the following to ensure that you have everything you need to run the latest and greatest!

#### Update your PostgeSQL database to the latest migration
1. If you have `dotnet ef` command line tools installed, you can run the following command in the root folder of Silk to apply the latest migration
   - ```
     dotnet ef database update -s Silk.Core -p Silk.Core.Data --verbose
     ``` 
   - You can install the `dotnet ef` command line tool by following this document [Entity Framework Core Tools CLI](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)

2. If you're using Visual Studio you can the `Package Manager Console`. **Note**: Make sure in the tool window, to change the `Default project` to `src\Silk.Core.Data`. Then execute the following command:
   - ```
     Update-Database
     ``` 
   - You can install the `Package Manager Console` tools by following this document [Entity Framework Core Tools - Package Manager Console in Visual Studio](https://docs.microsoft.com/en-us/ef/core/cli/powershell)


---


<br/>

## ⚠️ A NOTE ABOUT SELF-HOSTING ⚠️
### Self-hosting Silk! is easy. Instructions have been provided to self-host locally.
## Docker: 
To run Silk! as a docker container, you can simply create an `appSettings.json` file in the root directory, and run `docker pull velvetthepanda/silk`, assuming you have a postgres database running already. If not, you can download a pre-configured `docker-compose` file [here](https://files.velvetthepanda.dev/docker-compose.yml). 

Both Postgres and Silk! will have to initialize on the first run, which may cause slightly degraded startup times as Silk! creates requisite tables on the database. 

Further startup times should be no more than ~3 seconds to fully initialize and do cache runs.
![](https://files.velvetthepanda.dev/silk.png)