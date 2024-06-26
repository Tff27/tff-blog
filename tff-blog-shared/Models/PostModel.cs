﻿namespace Tff.Blog.Shared.Models;

public class PostModel : MarkdownModel
{
    public PostModel()
    {
        Tags = new List<string>();
    }

    public string Image { get; set; }

    public DateTime Date { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public IEnumerable<string> Tags { get; set; }

    public string Slug { get; set; }

    public bool Draft { get; set; }

    public int ReadTimeInMinutes { get; set; }
}
