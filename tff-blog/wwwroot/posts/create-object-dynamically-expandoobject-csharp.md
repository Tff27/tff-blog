---
draft: false
slug: create-object-dynamically-expandoobject-csharp
title: Create an object dynamically using ExpandoObject in C#
date: 2022-11-15T18:51:00Z
image: "/images/brickwall.png"
tags:
- '0'
description: The ExpandoObject is a dynamic type that allows you to create objects
  at runtime and then add/remove properties, methods and events to it.

---
## Summary

Sometimes you don't know what to expect from the data structure when you are consuming data from third parties.

What if we need to ingest data from many data points with different schemas so we use that data in downstream services?

I came across the challenge of creating a model from YAML files frontmatter and persisting them for future use. Although the files have similarities between them there were different fields that I needed to store. That's when I found [ExpandoObject](https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=net-7.0 "ExpandoObject"), an object that can have members added/removed at run time.

In this article, I will walk you through understanding ExpandoObject type and in how to use this dynamic class.

## Understand the ExpandoObject Class

The ExpandoObject class is an implementation of the dynamic object as such ExpandoObject enables you to add and delete members of its instances at run time and set/get values of these members.

ExpandoObject class also implements the IDictionary<String, Object> interface so is possible to enumerate the instance's members, this gives us the chance to take advantage of this interface when adding or removing members.

### How to initialize a ExpandoObject

    dynamic myDynamicObject = new ExpandoObject();

### How to add/remove members on a ExpandoObject

**Add members:**

    myDynamicObject.MyTextMember = "Hello World!";

Or

    ((IDictionary<string, object>)myDynamicObject).Add("MyTextMember", "Hello World!");

If you need even more flexibility, you can add ExpandoObjects members to your previously created ExpandoObject.

    myDynamicObject.MyTextMember = "Hello World!"
    myDynamicObject.MyExpandoObjectMember = new ExpandoObject();
    myDynamicObject.MyExpandoObjectMember.WorldName = "Earth";
    myDynamicObject.MyExpandoObjectMember.WorldNumberOfMoons = 1;

**Remove members:**

    ((IDictionary<String, Object>)myDynamicObject).Remove("MyTextMember");

### How to add/remove methods on a ExpandoObject

    myDynamicObject.Counter = 0;
    myDynamicObject.Increment = (Action)(() => { myDynamicObject.Counter++; });
    myDynamicObject.Increment();

## Conclusion

The ExpandoObject type allows you to create objects easily at runtime and then add/remove properties to them whenever you need them.

Since it can take delegates as members, allows you to attach methods and events to these dynamic types as well.

It can be useful when dealing with data that you have no control over to set up a type without worrying to create specific models.