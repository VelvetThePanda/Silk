using System.Collections.Generic;
using System.Reflection;
using Serilog;
using Silk.Shared.Constants;

namespace Silk.Shared.Configuration
{
    /// <summary>
    /// A class which holds configuration information bound from an AppSettings or UserSecrets file.
    /// To be used with IOptions and configured in ConfigureServices in a Startup.cs file.<br/>
    ///
    /// <para>More info about <b>Options Pattern</b> can be found on Microsoft Docs
    /// <a href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0">Options pattern in ASP.NET Core</a>
    /// </para><br/>
    /// 
    /// Properties within this class correlate to the name of the key or sub-key property in the configuration file:
    /// Ex. Below, the "Persistence" key correlates to the Persistence property in this class with the same name
    /// <code>
    /// {
    ///     /* Root */
    ///     "Silk":
    ///     {
    ///         /* Sub-Key property name */
    ///         "Persistence": {...}
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class SilkConfigurationOptions
    {
        /// <summary>
        /// The name of the root configuration options property in the file
        /// </summary>
        public const string SectionKey = "Silk";

        /// <summary>
        /// Property for whether this bot is self-hosted or not. 
        /// <remarks>Setting to true may lead to broken features that rely on specific information/services that are unavailable in self-hosted instances.</remarks>
        /// </summary>
        public bool SelfHosted { get; set; }

        /// <summary>
        /// Property for controlling the level of Logging
        /// </summary>
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// Property for holding Persistence options (property name matching sub-key property in configuration file)
        /// </summary>
        public SilkPersistenceOptions Persistence { get; set; }

        /// <summary>
        /// Property for holding Discord Developer Api options (property name matching sub-key property in configuration file)
        /// </summary>
        public SilkDiscordOptions Discord { get; set; }

        /// <summary>
        /// Property for holding Discord Developer Api options (property name matching sub-key property in configuration file)
        /// </summary>
        public SilkE621Options E621 { get; set; }

        /// <summary>
        /// Property for holding serialized emoji Ids in json form to populate <see cref="Silk.Shared.Constants.Emojis"/>.
        /// </summary>
        public SilkEmojiOptions? Emojis { get; set; }
    }

    /// <summary>
    /// Class which holds configuration information for the Database Connection properties
    /// <para>Note: Silk by default uses PostgresSQL, so the class is templated based off connection string convention for PostgreSQL</para>
    /// <para>A pre-configured <b>docker-compose.yml</b> file can be found
    /// <a href="https://files.velvetthepanda.dev/docker/postgres/docker-compose.yml">here</a></para>
    /// <para>Default Username and Password: "silk".</para>
    /// </summary>
    public class SilkPersistenceOptions
    {
        public string Host { get; set; } = "localhost";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Convenience method to compose the ConnectionString based on the provided Persistence options
        /// <see cref="SilkPersistenceOptions"/>
        /// </summary>
        /// <returns>The full connection string given the options, or invalid/incomplete connection string if pieces of configuration file are left blank</returns>
        public string GetConnectionString()
        {
            return $"Server={Host};" +
                   $"Port={Port};" +
                   $"Database={Database};" +
                   $"Username={Username};" +
                   $"Password={Password};" +
                   "Include Error Detail = true";
        }
    }

    /// <summary>
    /// Class which holds configuration information for the Discord Developer Api properties
    /// </summary>
    public class SilkDiscordOptions
    {
        public int Shards {get; set; } = 1;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BotToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class which holds configuration information for the E621 Api properties.
    ///
    /// 99% of content will be accessible without this, but <i>some</i> content may be on the public blacklist. Use at your own discretion.
    /// </summary>
    public class SilkE621Options
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class SilkEmojiOptions
    {
        public Dictionary<string, ulong> EmojiIds { get; set; }

        public void PopulateEmojiConstants()
        {
            foreach (var prop in typeof(Emojis).GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                if (!EmojiIds.TryGetValue(prop.Name, out var val)) continue;
                
                prop.SetValue(null, val);
                Log.Logger.Verbose("Successfully set {Property} to {Value}", prop.Name, val);
            }
        }
    }
}