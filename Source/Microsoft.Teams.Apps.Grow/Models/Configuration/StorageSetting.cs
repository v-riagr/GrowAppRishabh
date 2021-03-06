﻿// <copyright file="StorageSetting.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Models.Configuration
{
    /// <summary>
    /// Class which will help to provide Microsoft Azure Table storage settings.
    /// </summary>
    public class StorageSetting : BotSettings
    {
        /// <summary>
        /// Gets or sets Microsoft Azure Table storage connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
