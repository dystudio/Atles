﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Atlas.Models.Public
{
    public class TopicPageModel
    {
        public ForumModel Forum { get; set; } = new ForumModel();

        public IList<PermissionModel> Permissions { get; set; } = new List<PermissionModel>();
        public TopicModel Topic { get; set; } = new TopicModel();
        public PaginatedData<ReplyModel> Replies { get; set; } = new PaginatedData<ReplyModel>();
        public PostModel Post { get; set; } = new PostModel();

        public class ForumModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class TopicModel
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public Guid MemberId { get; set; }
            public string MemberDisplayName { get; set; }
            public DateTime TimeStamp { get; set; }
            public string UserId { get; set; }
        }

        public class ReplyModel
        {
            public Guid Id { get; set; }
            public string Content { get; set; }
            public string OriginalContent { get; set; }
            public string UserId { get; set; }
            public Guid MemberId { get; set; }
            public string MemberDisplayName { get; set; }
            public DateTime TimeStamp { get; set; }
        }

        public class PostModel
        {
            public Guid? Id { get; set; }

            [Required]
            public string Content { get; set; }

            public Guid MemberId { get; set; }
        }
    }
}
