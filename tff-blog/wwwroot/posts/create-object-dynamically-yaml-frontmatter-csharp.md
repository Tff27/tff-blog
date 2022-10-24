---
draft: true
slug: create-object-dynamically-yaml-frontmatter-csharp
title: Create an object dynamically from YAML frontmatter in C#
date: 2022-10-24T23:00:00Z
image: "/images/brickwall.png"
tags:
- '0'
description: Create an object dynamically from YAML frontmatter in C#

---
# Create an object dynamically from YAML frontmatter in C#

## Summary

Sometimes you don't know what to expect in terms of the data structure when you are consuming data from third parties. 

What if we need to ingest data from multiple data points so it can be used by downstream services?

I came across the challenge of creating a model from YAML files frontmatter and persisting them for future use. That's when I found [ExpandoObject](https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=net-7.0 "ExpandoObject"), an object that can have members added/removed at run time.