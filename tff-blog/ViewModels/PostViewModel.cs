using System;
using System.Collections.Generic;
using tffBlog.Enum;

namespace tffBlog.ViewModels
{
    public class PostViewModel
    {
        public PostViewModel()
        {
            Tags = new List<TagEnum>();
        }

        public string Image { get; set; }

        public DateTime Date { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public IEnumerable<TagEnum> Tags { get; set; }
    }
}
