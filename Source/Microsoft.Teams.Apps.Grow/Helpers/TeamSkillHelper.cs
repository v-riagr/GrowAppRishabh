﻿// <copyright file="TeamSkillHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Helpers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.Grow.Common;
    using Microsoft.Teams.Apps.Grow.Common.Interfaces;

    /// <summary>
    ///  Implements team skill helper which helps for team skills configuration.
    /// </summary>
    public class TeamSkillHelper : ITeamSkillHelper
    {
        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<TeamSkillHelper> logger;

        /// <summary>
        /// Instance of team skill storage provider.
        /// </summary>
        private readonly ITeamSkillStorageProvider teamSkillStorageProvider;

        /// <summary>
        /// Bot adapter.
        /// </summary>
        private readonly IBotFrameworkHttpAdapter botAdapter;

        /// <summary>
        /// Microsoft application credentials.
        /// </summary>
        private readonly MicrosoftAppCredentials microsoftAppCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamSkillHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="teamSkillStorageProvider">Team skill storage provider dependency injection.</param>
        /// <param name="botAdapter">Bot adapter.</param>
        /// <param name="microsoftAppCredentials">Microsoft application credentials.</param>
        public TeamSkillHelper(
            ILogger<TeamSkillHelper> logger,
            ITeamSkillStorageProvider teamSkillStorageProvider,
            IBotFrameworkHttpAdapter botAdapter,
            MicrosoftAppCredentials microsoftAppCredentials)
        {
            this.logger = logger;
            this.teamSkillStorageProvider = teamSkillStorageProvider;
            this.botAdapter = botAdapter;
            this.microsoftAppCredentials = microsoftAppCredentials;
        }

        /// <summary>
        /// To fetch team member information for specified team.
        /// Return null if the member is not found in team id or either of the information is incorrect.
        /// Caller should handle null value to throw unauthorized if required
        /// </summary>
        /// <param name="teamId">Team id.</param>
        /// <param name="userId">User object id.</param>
        /// <returns>Returns team member information.</returns>
        public async Task<TeamsChannelAccount> GetTeamMemberAsync(string teamId, string userId)
        {
            TeamsChannelAccount teamMember = new TeamsChannelAccount();

            try
            {
                var teamDetails = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);
                string serviceUrl = teamDetails?.ServiceUrl;

                var conversationReference = new ConversationReference
                {
                    ChannelId = Constants.TeamsBotFrameworkChannelId,
                    ServiceUrl = serviceUrl,
                };

                await ((BotFrameworkAdapter)this.botAdapter).ContinueConversationAsync(
                    this.microsoftAppCredentials.MicrosoftAppId,
                    conversationReference,
                    async (context, token) =>
                    {
                        teamMember = await TeamsInfo.GetTeamMemberAsync(context, userId, teamId, CancellationToken.None);
                    }, default);
            }
#pragma warning disable CA1031 // Catching general exceptions to log exception details in telemetry client.
            catch (Exception ex)
#pragma warning restore CA1031 // Catching general exceptions to log exception details in telemetry client.
            {
                this.logger.LogError(ex, $"Error occurred while fetching team member for team: {teamId} - user object id: {userId} ");

                // Return null if the member is not found in team id or either of the information is incorrect.
                // Caller should handle null value to throw unauthorized if required.
                return null;
            }

            return teamMember;
        }
    }
}
