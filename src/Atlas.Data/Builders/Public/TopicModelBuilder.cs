﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Data.Caching;
using Atlas.Domain;
using Atlas.Models;
using Atlas.Models.Public.Topics;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Data.Builders.Public
{
    public class TopicModelBuilder : ITopicModelBuilder
    {
        private readonly AtlasDbContext _dbContext;
        private readonly ICacheManager _cacheManager;
        private readonly IGravatarService _gravatarService;

        public TopicModelBuilder(AtlasDbContext dbContext,
            ICacheManager cacheManager,
            IGravatarService gravatarService)
        {
            _dbContext = dbContext;
            _cacheManager = cacheManager;
            _gravatarService = gravatarService;
        }

        public async Task<TopicPageModel> BuildTopicPageModelAsync(Guid siteId, string forumSlug, string topicSlug, QueryOptions options)
        {
            var topic = await _dbContext.Posts
                .Include(x => x.Forum).ThenInclude(x => x.Category)
                .Include(x => x.CreatedByUser)
                .FirstOrDefaultAsync(x =>
                    x.TopicId == null &&
                    x.Forum.Category.SiteId == siteId &&
                    x.Forum.Slug == forumSlug &&
                    x.Slug == topicSlug &&
                    x.Status == StatusType.Published);

            if (topic == null)
            {
                return null;
            }

            var result = new TopicPageModel
            {
                Forum = new TopicPageModel.ForumModel
                {
                    Id = topic.Forum.Id,
                    Name = topic.Forum.Name,
                    Slug = topic.Forum.Slug
                },
                Topic = new TopicPageModel.TopicModel
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    Slug = topic.Slug,
                    Content = Markdown.ToHtml(topic.Content),
                    MemberId = topic.CreatedByUser.Id,
                    MemberDisplayName = topic.CreatedByUser.DisplayName,
                    TimeStamp = topic.TimeStamp,
                    UserId = topic.CreatedByUser.IdentityUserId,
                    GravatarHash = _gravatarService.HashEmailForGravatar(topic.CreatedByUser.Email),
                    Pinned = topic.Pinned,
                    Locked = topic.Locked,
                    HasAnswer = topic.HasAnswer
                },
                Replies = await BuildTopicPageModelRepliesAsync(topic.Id, options)
            };

            if (topic.HasAnswer)
            {
                var answer = await _dbContext.Posts
                    .Include(x => x.CreatedByUser)
                    .Where(x =>
                        x.TopicId == topic.Id &&
                        x.Status == StatusType.Published &&
                        x.IsAnswer)
                    .FirstOrDefaultAsync();

                if (answer != null)
                {
                    result.Answer = new TopicPageModel.ReplyModel
                    {
                        Id = answer.Id,
                        Content = Markdown.ToHtml(answer.Content),
                        OriginalContent = answer.Content,
                        UserId = answer.CreatedByUser.IdentityUserId,
                        MemberId = answer.CreatedByUser.Id,
                        MemberDisplayName = answer.CreatedByUser.DisplayName,
                        TimeStamp = answer.TimeStamp,
                        GravatarHash = _gravatarService.HashEmailForGravatar(answer.CreatedByUser.Email),
                        IsAnswer = answer.IsAnswer
                    };
                }
            }

            return result;
        }

        public async Task<PaginatedData<TopicPageModel.ReplyModel>> BuildTopicPageModelRepliesAsync(Guid topicId, QueryOptions options)
        {
            var repliesQuery = _dbContext.Posts
                .Include(x => x.CreatedByUser)
                .Where(x =>
                    x.TopicId == topicId &&
                    x.Status == StatusType.Published &&
                    x.IsAnswer == false);

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                repliesQuery = repliesQuery.Where(x => x.Content.Contains(options.Search));
            }

            var replies = await repliesQuery
                .OrderBy(x => x.TimeStamp)
                .Skip(options.Skip)
                .Take(options.PageSize)
                .ToListAsync();

            var items = replies.Select(reply => new TopicPageModel.ReplyModel
            {
                Id = reply.Id,
                Content = Markdown.ToHtml(reply.Content),
                OriginalContent = reply.Content,
                UserId = reply.CreatedByUser.IdentityUserId,
                MemberId = reply.CreatedByUser.Id,
                MemberDisplayName = reply.CreatedByUser.DisplayName,
                TimeStamp = reply.TimeStamp,
                GravatarHash = _gravatarService.HashEmailForGravatar(reply.CreatedByUser.Email),
                IsAnswer = reply.IsAnswer
            }).ToList();

            var totalRecords = await repliesQuery.CountAsync();

            var result = new PaginatedData<TopicPageModel.ReplyModel>(items, totalRecords, options.PageSize);

            return result;
        }
    }
}