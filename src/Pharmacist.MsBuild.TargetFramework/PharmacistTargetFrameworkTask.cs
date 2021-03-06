﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Pharmacist.Core;
using Pharmacist.Core.NuGet;
using Splat;

namespace Pharmacist.MsBuild.TargetFramework
{
    /// <summary>
    /// A task for generating events.
    /// </summary>
    [SuppressMessage("Design", "CA1031: Catch specific exceptions", Justification = "Final logging location for exceptions.")]
    public class PharmacistTargetFrameworkTask : Task, IEnableLogger
    {
        /// <summary>
        /// Gets or sets the target framework.
        /// </summary>
        [Required]
        public string TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets the output file.
        /// </summary>
        [Required]
        public string OutputFile { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var funcLogManager = new FuncLogManager(type => new WrappingFullLogger(new WrappingPrefixLogger(new MsBuildLogger(Log, LogLevel.Debug), type)));
            Locator.CurrentMutable.RegisterConstant(funcLogManager, typeof(ILogManager));

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                Log.LogError($"{nameof(OutputFile)} is not set");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TargetFramework))
            {
                Log.LogError($"{nameof(TargetFramework)} is not set");
                return false;
            }

            using (var writer = new StreamWriter(Path.Combine(OutputFile)))
            {
                ObservablesForEventGenerator.WriteHeader(writer, TargetFramework).ConfigureAwait(false).GetAwaiter().GetResult();

                try
                {
                    ObservablesForEventGenerator.ExtractEventsFromTargetFramework(writer, TargetFramework.ToFrameworks()).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            return true;
        }
    }
}
