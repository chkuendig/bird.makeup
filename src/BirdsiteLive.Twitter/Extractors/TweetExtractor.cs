﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BirdsiteLive.Twitter.Models;

namespace BirdsiteLive.Twitter.Extractors
{
    public interface ITweetExtractor
    {
        ExtractedTweet Extract(JsonDocument tweet);
    }

    public class TweetExtractor : ITweetExtractor
    {
        public ExtractedTweet Extract(JsonDocument tweet)
        {
            var extractedTweet = new ExtractedTweet
            {
                Id = tweet.RootElement.GetProperty("data").GetProperty("id").GetInt64(),
                InReplyToStatusId = tweet.RootElement.GetProperty("data").GetProperty("in_reply_to_status_id").GetInt64(),
                InReplyToAccount = tweet.RootElement.GetProperty("data").GetProperty("in_reply_to_status_id").GetString(),
                MessageContent = ExtractMessage(tweet),
                Media = ExtractMedia(tweet),
                CreatedAt = tweet.RootElement.GetProperty("data").GetProperty("in_reply_to_status_id").GetDateTime(),
                IsReply = false,
                IsThread = false,
                IsRetweet = false,
                RetweetUrl = ExtractRetweetUrl(tweet)
            };

            return extractedTweet;
        }

        private string ExtractRetweetUrl(JsonDocument tweet)
        {
            var retweetId = "123";
            return $"https://t.co/{retweetId}";

        }

        private string ExtractMessage(JsonDocument tweet)
        {
            return "hello world";
            //var message = tweet.FullText;
            //var tweetUrls = tweet.Media.Select(x => x.URL).Distinct();
            
            //if (tweet.IsRetweet && message.StartsWith("RT") && tweet.RetweetedTweet != null)
            //{
            //    message = tweet.RetweetedTweet.FullText;
            //    tweetUrls = tweet.RetweetedTweet.Media.Select(x => x.URL).Distinct();
            //}

            //foreach (var tweetUrl in tweetUrls)
            //{
            //    if(tweet.IsRetweet)
            //        message = tweet.RetweetedTweet.FullText.Replace(tweetUrl, string.Empty).Trim();
            //    else 
            //        message = message.Replace(tweetUrl, string.Empty).Trim();
            //}

            //if (tweet.QuotedTweet != null) message = $"[Quote {{RT}}]{Environment.NewLine}{message}";
            //if (tweet.IsRetweet)
            //{
            //    if (tweet.RetweetedTweet != null && !message.StartsWith("RT"))
            //        message = $"[{{RT}} @{tweet.RetweetedTweet.CreatedBy.ScreenName}]{Environment.NewLine}{message}";
            //    else if (tweet.RetweetedTweet != null && message.StartsWith($"RT @{tweet.RetweetedTweet.CreatedBy.ScreenName}:"))
            //        message = message.Replace($"RT @{tweet.RetweetedTweet.CreatedBy.ScreenName}:", $"[{{RT}} @{tweet.RetweetedTweet.CreatedBy.ScreenName}]{Environment.NewLine}");
            //    else
            //        message = message.Replace("RT", "[{{RT}}]");
            //}

            //// Expand URLs
            //foreach (var url in tweet.Urls.OrderByDescending(x => x.URL.Length))
            //    message = message.Replace(url.URL, url.ExpandedURL);

            //return message;
        }

        private ExtractedMedia[] ExtractMedia(JsonDocument tweet)
        {
            //var media = tweet.Media;
            //if (tweet.IsRetweet && tweet.RetweetedTweet != null)
            //    media = tweet.RetweetedTweet.Media;

            //var result = new List<ExtractedMedia>();
            //foreach (var m in media)
            //{
            //    var mediaUrl = GetMediaUrl(m);
            //    var mediaType = GetMediaType(m.MediaType, mediaUrl);
            //    if (mediaType == null) continue;

            //    var att = new ExtractedMedia
            //    {
            //        MediaType = mediaType,
            //        Url = mediaUrl
            //    };
            //    result.Add(att);
            //}

            //return result.ToArray();
            return Array.Empty<ExtractedMedia>();
        }


        private string GetMediaType(string mediaType, string mediaUrl)
        {
            switch (mediaType)
            {
                case "photo":
                    var pExt = Path.GetExtension(mediaUrl);
                    switch (pExt)
                    {
                        case ".jpg":
                        case ".jpeg":
                            return "image/jpeg";
                        case ".png":
                            return "image/png";
                    }
                    return null;

                case "animated_gif":
                    var vExt = Path.GetExtension(mediaUrl);
                    switch (vExt)
                    {
                        case ".gif":
                            return "image/gif";
                        case ".mp4":
                            return "video/mp4";
                    }
                    return "image/gif";
                case "video":
                    return "video/mp4";
            }
            return null;
        }
    }
}