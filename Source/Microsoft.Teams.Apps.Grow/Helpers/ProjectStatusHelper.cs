// <copyright file="ProjectStatusHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Helpers
{
    using Microsoft.Extensions.Localization;
    using Microsoft.Teams.Apps.Grow.Models;
    using Microsoft.Teams.Apps.Grow.Resources;

    /// <summary>
    ///  Class that handles the project status.
    /// </summary>
    public class ProjectStatusHelper
    {
        /// <summary>
        /// The current cultures' string localizer.
        /// </summary>
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectStatusHelper"/> class.
        /// </summary>
        /// <param name="localizer">The current cultures' string localizer.</param>
        public ProjectStatusHelper(IStringLocalizer<Strings> localizer)
        {
            this.localizer = localizer;
        }

        /// <summary>
        /// Valid post types.
        /// </summary>
        public enum StatusEnum
        {
            /// <summary>
            /// No status.
            /// </summary>
            None = 0,

            /// <summary>
            /// Project not yet started.
            /// </summary>
            NotStarted = 1,

            /// <summary>
            /// Project is active.
            /// </summary>
            Active = 2,

            /// <summary>
            /// Project is blocked.
            /// </summary>
            Blocked = 3,

            /// <summary>
            /// Project is closed.
            /// </summary>
            Closed = 4,
        }

        /// <summary>
        /// Get the status using its id.
        /// </summary>
        /// <param name="key">Status id value.</param>
        /// <returns>Returns a localized status from the id value.</returns>
        public ProjectStatus GetStatus(int key)
        {
            switch (key)
            {
                case (int)StatusEnum.NotStarted:
                    return new ProjectStatus { StatusName = this.localizer.GetString("NotStartedStatusType"), IconName = "notStartedStatusDot.png", StatusId = 1 };

                case (int)StatusEnum.Active:
                    return new ProjectStatus { StatusName = this.localizer.GetString("ActiveStatusType"), IconName = "activeStatusDot.png", StatusId = 2 };

                case (int)StatusEnum.Blocked:
                    return new ProjectStatus { StatusName = this.localizer.GetString("BlockedStatusType"), IconName = "blockedStatusDot.png", StatusId = 3 };

                case (int)StatusEnum.Closed:
                    return new ProjectStatus { StatusName = this.localizer.GetString("ClosedStatusType"), IconName = "closedStatusDot.png", StatusId = 4 };

                default:
                    return null;
            }
        }
    }
}
