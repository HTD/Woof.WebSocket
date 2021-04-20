using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace Woof.WebSocket.Test.Server {
    
    internal static class Config {


        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        const string Path =
#if DEBUG
            "Config-dev.json"
#else
            "Config.json"
#endif
        ;

        /// <summary>
        /// Gets the server configuration data from the file.
        /// </summary>
        internal static IConfiguration Data { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(System.IO.Path.GetFullPath(Path), optional: false, reloadOnChange: true)
            .Build();



    }

}
