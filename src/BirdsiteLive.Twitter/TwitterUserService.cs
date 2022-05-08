﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Statistics.Domain;
using BirdsiteLive.Twitter.Models;
using BirdsiteLive.Twitter.Tools;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Twitter
{
    public interface ITwitterUserService
    {
        TwitterUser GetUser(string username);
        bool IsUserApiRateLimited();
    }

    public class TwitterUserService : ITwitterUserService
    {
        private readonly ITwitterAuthenticationInitializer _twitterAuthenticationInitializer;
        private readonly ITwitterStatisticsHandler _statisticsHandler;
        private readonly ILogger<TwitterUserService> _logger;
        private HttpClient _httpClient = new HttpClient();

        #region Ctor
        public TwitterUserService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ILogger<TwitterUserService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _logger = logger;
        }
        #endregion

        public TwitterUser GetUser(string username)
        {
            return GetUserAsync(username).Result;
        }
        public async Task<TwitterUser> GetUserAsync(string username)
        {
            //Check if API is saturated 
            if (IsUserApiRateLimited()) throw new RateLimitExceededException();

            //Proceed to account retrieval
            await _twitterAuthenticationInitializer.EnsureAuthenticationIsInitialized();

            JsonDocument res;
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.twitter.com/2/users/by/username/"+ username + "?user.fields=name,username,protected,profile_image_url,url,description"))
    {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _twitterAuthenticationInitializer.Token); 

                    var httpResponse = await _httpClient.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();

                    var c = await httpResponse.Content.ReadAsStringAsync();
                    res = JsonDocument.Parse(c);
                }
            }
            catch (HttpRequestException e)
            {
                throw;
                //if (e.TwitterExceptionInfos.Any(x => x.Message.ToLowerInvariant().Contains("User has been suspended".ToLowerInvariant())))
                //{
                //    throw new UserHasBeenSuspendedException();
                //}
                //else if (e.TwitterExceptionInfos.Any(x => x.Message.ToLowerInvariant().Contains("User not found".ToLowerInvariant())))
                //{
                //    throw new UserNotFoundException();
                //}
                //else if (e.TwitterExceptionInfos.Any(x => x.Message.ToLowerInvariant().Contains("Rate limit exceeded".ToLowerInvariant())))
                //{
                //    throw new RateLimitExceededException();
                //}
                //else
                //{
                //    throw;
                //}
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving user {Username}", username);
                throw;
            }
            finally
            {
                _statisticsHandler.CalledUserApi();
            }

            // Expand URLs
            //var description = user.Description;
            //foreach (var descriptionUrl in user.Entities?.Description?.Urls?.OrderByDescending(x => x.URL.Length))
            //    description = description.Replace(descriptionUrl.URL, descriptionUrl.ExpandedURL);

            return new TwitterUser
            {
                Id = long.Parse(res.RootElement.GetProperty("data").GetProperty("id").GetString()),
                Acct = res.RootElement.GetProperty("data").GetProperty("username").GetString(),
                Name = res.RootElement.GetProperty("data").GetProperty("name").GetString(),
                Description = res.RootElement.GetProperty("data").GetProperty("description").GetString(),
                Url = res.RootElement.GetProperty("data").GetProperty("url").GetString(),
                ProfileImageUrl = res.RootElement.GetProperty("data").GetProperty("profile_image_url").GetString(),
                ProfileBackgroundImageUrl = res.RootElement.GetProperty("data").GetProperty("profile_image_url").GetString(), //for now
                ProfileBannerURL = res.RootElement.GetProperty("data").GetProperty("profile_image_url").GetString(), //for now
                Protected = res.RootElement.GetProperty("data").GetProperty("protected").GetBoolean(), 
            };
        }


        public ExtractedTweet Extract(JsonElement tweet)
        {
            bool IsRetweet = false;
            bool IsReply = false;
            long? replyId = null;
            JsonElement replyAccount;
            string? replyAccountString = null;
            JsonElement referenced_tweets;
            if(tweet.TryGetProperty("in_reply_to_user_id", out replyAccount))
            {
                replyAccountString = replyAccount.GetString();

            }
            if(tweet.TryGetProperty("referenced_tweets", out referenced_tweets))
            {
                var first = referenced_tweets.EnumerateArray().ToList()[0];
                if (first.GetProperty("type").GetString() == "retweeted")
                {
                    IsRetweet = true;
                    var statusId = Int64.Parse(first.GetProperty("id").GetString());
                    var extracted = GetTweet(statusId);
                    extracted.IsRetweet = true;
                    return extracted;

                }
                if (first.GetProperty("type").GetString() == "replied_to")
                {
                    IsReply = true;
                    replyId = Int64.Parse(first.GetProperty("id").GetString());
                }
                if (first.GetProperty("type").GetString() == "quoted")
                {
                    IsReply = true;
                    replyId = Int64.Parse(first.GetProperty("id").GetString());
                }
            }

            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("id").GetString()),
                InReplyToStatusId = replyId,
                InReplyToAccount = replyAccountString,
                MessageContent = tweet.GetProperty("text").GetString(),
                Media = Array.Empty<ExtractedMedia>(),
                CreatedAt = DateTime.Now, // tweet.GetProperty("data").GetProperty("in_reply_to_status_id").GetDateTime(),
                IsReply = IsReply,
                IsThread = false,
                IsRetweet = IsRetweet,
                RetweetUrl = "https://t.co/123"
            };

            return extractedTweet;
        }
        public bool IsUserApiRateLimited()
        {
            // Retrieve limit from tooling
            //_twitterAuthenticationInitializer.EnsureAuthenticationIsInitialized();
            //ExceptionHandler.SwallowWebExceptions = false;
            //RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;

            //try
            //{
            //    var queryRateLimits = RateLimit.GetQueryRateLimit("https://api.twitter.com/1.1/users/show.json?screen_name=mastodon");

            //    if (queryRateLimits != null)
            //    {
            //        return queryRateLimits.Remaining <= 0;
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError(e, "Error retrieving rate limits");
            //}

            //// Fallback
            //var currentCalls = _statisticsHandler.GetCurrentUserCalls();
            //var maxCalls = _statisticsHandler.GetStatistics().UserCallsMax;
            //return currentCalls >= maxCalls;
            return false;
        }
    }
}