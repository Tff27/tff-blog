﻿using Tff.Blog.Shared.Enum;

namespace Tff.Blog.Shared.Models
{
    public class PostModel
    {
        public PostModel()
        {
            Tags = new List<TagEnum>();
        }

        public string Image { get; set; }

        public DateTime Date { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public IEnumerable<TagEnum> Tags { get; set; }

        public string Slug { get; set; }
    }
}