﻿using System;
using System.Collections.Generic;
using System.Text;
using Statiq.App;
using Statiq.Common;
using Statiq.Web.Commands;
using Statiq.Web.Shortcodes;

namespace Statiq.Web
{
    public static class BootstrapperFactoryExtensions
    {
        /// <summary>
        /// Creates a bootstrapper with all functionality for Statiq Web.
        /// </summary>
        /// <param name="factory">The bootstrapper factory.</param>
        /// <param name="args">The command line arguments.</param>
        /// <returns>A bootstrapper.</returns>
        public static Bootstrapper CreateWeb(this BootstrapperFactory factory, string[] args) =>
            factory
                .CreateDefault(args)
                .AddPipelines(typeof(BootstrapperFactoryExtensions).Assembly)
                .AddHostingCommands()
                .ConfigureEngine(x => x.FileSystem.InputPaths.Add("theme"))
                .AddSettingsIfNonExisting(new Dictionary<string, object>
                {
                    { WebKeys.MirrorResources, true }
                });
    }
}
