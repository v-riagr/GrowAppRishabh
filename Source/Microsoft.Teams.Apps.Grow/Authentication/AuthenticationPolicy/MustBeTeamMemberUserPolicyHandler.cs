﻿// <copyright file="MustBeTeamMemberUserPolicyHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Grow.Authentication.AuthenticationPolicy
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Teams.Apps.Grow.Common;
    using Microsoft.Teams.Apps.Grow.Common.Interfaces;
    using Microsoft.Teams.Apps.Grow.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This authorization handler is created to handle project creator's user policy.
    /// The class implements AuthorizationHandler for handling MustBeTeamMemberUserPolicyRequirement authorization.
    /// </summary>
    public class MustBeTeamMemberUserPolicyHandler : AuthorizationHandler<MustBeTeamMemberUserPolicyRequirement>
    {
        /// <summary>
        /// Microsoft application credentials.
        /// </summary>
        private readonly MicrosoftAppCredentials microsoftAppCredentials;

        /// <summary>
        /// Bot adapter.
        /// </summary>
        private readonly IBotFrameworkHttpAdapter botAdapter;

        /// <summary>
        /// Cache for storing authorization result.
        /// </summary>
        private readonly IMemoryCache memoryCache;

        private readonly ITeamSkillStorageProvider teamSkillStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MustBeTeamMemberUserPolicyHandler"/> class.
        /// </summary>
        /// <param name="memoryCache">MemoryCache instance for caching authorization result.</param>
        /// <param name="botAdapter">Bot adapter for getting team members.</param>
        /// <param name="teamSkillStorageProvider">Provides methods for team skills operations from database.</param>
        /// <param name="microsoftAppCredentials">Microsoft application credentials.</param>
        public MustBeTeamMemberUserPolicyHandler(
            IMemoryCache memoryCache,
            IBotFrameworkHttpAdapter botAdapter,
            ITeamSkillStorageProvider teamSkillStorageProvider,
            MicrosoftAppCredentials microsoftAppCredentials)
        {
            this.memoryCache = memoryCache;
            this.botAdapter = botAdapter;
            this.teamSkillStorageProvider = teamSkillStorageProvider;
            this.microsoftAppCredentials = microsoftAppCredentials;
        }

        /// <summary>
        /// This method handles the authorization requirement.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext instance.</param>
        /// <param name="requirement">IAuthorizationRequirement instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MustBeTeamMemberUserPolicyRequirement requirement)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));

            string teamId = string.Empty;
            var oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

            var oidClaim = context.User.Claims.FirstOrDefault(p => oidClaimType == p.Type);

            if (context.Resource is AuthorizationFilterContext authorizationFilterContext)
            {
                // Wrap the request stream so that we can rewind it back to the start for regular request processing.
                authorizationFilterContext.HttpContext.Request.EnableBuffering();

                if (string.IsNullOrEmpty(authorizationFilterContext.HttpContext.Request.QueryString.Value))
                {
                    // Read the request body, parse out the activity object, and set the parsed culture information.
                    var streamReader = new StreamReader(authorizationFilterContext.HttpContext.Request.Body, Encoding.UTF8, true, 1024, leaveOpen: true);
                    using var jsonReader = new JsonTextReader(streamReader);
                    var obj = JObject.Load(jsonReader);
                    var teamEntity = obj.ToObject<TeamSkillEntity>();
                    authorizationFilterContext.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                    teamId = teamEntity.TeamId;
                }
                else
                {
                    var requestQuery = authorizationFilterContext.HttpContext.Request.Query;
                    teamId = requestQuery.Where(queryData => queryData.Key == "teamId").Select(queryData => queryData.Value.ToString()).FirstOrDefault();
                }
            }

            if (await this.ValidateUserIsPartOfTeamAsync(teamId, oidClaim.Value))
            {
                context.Succeed(requirement);
            }
        }

        /// <summary>
        /// Check if a user has admin access in a certain team.
        /// </summary>
        /// <param name="teamId">The team id that the validator uses to check if the user is a member of the team. </param>
        /// <param name="userAadObjectId">The user's Azure Active Directory object id.</param>
        /// <returns>The flag indicates that the user is a part of certain team or not.</returns>
        private async Task<bool> ValidateUserIsPartOfTeamAsync(string teamId, string userAadObjectId)
        {
            var teamInfo = await this.teamSkillStorageProvider.GetTeamSkillsDataAsync(teamId);
            if (teamInfo == null)
            {
                return false;
            }

            this.memoryCache.TryGetValue(userAadObjectId, out bool isUserValid);
            if (isUserValid == false)
            {
                TeamsChannelAccount teamMember = new TeamsChannelAccount();

                var conversationReference = new ConversationReference
                {
                    ChannelId = Constants.TeamsBotFrameworkChannelId,
                    ServiceUrl = teamInfo.ServiceUrl,
                };
                await ((BotFrameworkAdapter)this.botAdapter).ContinueConversationAsync(
                    this.microsoftAppCredentials.MicrosoftAppId,
                    conversationReference,
                    async (context, token) =>
                    {
                        teamMember = await TeamsInfo.GetTeamMemberAsync(context, userAadObjectId, teamId, CancellationToken.None);
                    }, default);

                var isValid = teamMember != null;
                this.memoryCache.Set(userAadObjectId, isValid, TimeSpan.FromHours(1));
                return isValid;
            }

            return isUserValid;
        }
    }
}
