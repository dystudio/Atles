﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atles.Data.Caching;
using Atles.Data.Extensions;
using Atles.Domain.Posts;
using Atles.Models;
using Atles.Models.Public.Search;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Atles.Data.Builders.Public
{
    public class SearchModelBuilder : ISearchModelBuilder
    {
        private readonly AtlesDbContext _dbContext;
        private readonly ICacheManager _cacheManager;
        private readonly IGravatarService _gravatarService;

        public SearchModelBuilder(AtlesDbContext dbContext,
            ICacheManager cacheManager,
            IGravatarService gravatarService)
        {
            _dbContext = dbContext;
            _cacheManager = cacheManager;
            _gravatarService = gravatarService;
        }

        public async Task<SearchPageModel> BuildSearchPageModelAsync(Guid siteId, IList<Guid> forumIds, QueryOptions options)
        {
            var result = new SearchPageModel
            {
                Posts = await SearchPostModels(forumIds, options)
            };

            return result;
        }

        public async Task<PaginatedData<SearchPostModel>> SearchPostModels(IList<Guid> forumIds, QueryOptions options, Guid? memberId = null)
        {
            var postsQuery = _dbContext.Posts
                .Where(x =>
                    forumIds.Contains(x.ForumId) &&
                    x.Status == PostStatusType.Published &&
                    (x.Topic == null || x.Topic.Status == PostStatusType.Published));

            if (options.SearchIsDefined())
            {
                postsQuery = postsQuery.Where(x => x.Title.Contains(options.Search) || x.Content.Contains(options.Search));
            }

            postsQuery = options.OrderByIsDefined() 
                ? postsQuery.OrderBy(options) 
                : postsQuery.OrderByDescending(x => x.CreatedOn);

            if (memberId != null)
            {
                postsQuery = postsQuery.Where(x => x.CreatedBy == memberId);
            }

            var posts = await postsQuery
                .Skip(options.Skip)
                .Take(options.PageSize)
                .Select(p => new
                {
                    p.Id,
                    TopicId = p.TopicId ?? p.Id,
                    IsTopic = p.TopicId == null,
                    Title = p.Title ?? p.Topic.Title,
                    Slug = p.Slug ?? p.Topic.Slug,
                    p.Content,
                    TimeStamp = p.CreatedOn,
                    UserId = p.CreatedBy,
                    UserDisplayName = p.CreatedByUser.DisplayName,
                    p.ForumId,
                    ForumName = p.Forum.Name,
                    ForumSlug = p.Forum.Slug
                })
                .ToListAsync();

            var items = posts.Select(post => new SearchPostModel
            {
                Id = post.Id,
                TopicId = post.TopicId,
                IsTopic = post.IsTopic,
                Title = post.Title,
                Slug = post.Slug,
                Content = Markdown.ToHtml(post.Content),
                TimeStamp = post.TimeStamp,
                UserId = post.UserId,
                UserDisplayName = post.UserDisplayName,
                ForumId = post.ForumId,
                ForumName = post.ForumName,
                ForumSlug = post.ForumSlug
            }).ToList();

            var totalRecords = await postsQuery.CountAsync();

            return new PaginatedData<SearchPostModel>(items, totalRecords, options.PageSize);
        }
    }
}