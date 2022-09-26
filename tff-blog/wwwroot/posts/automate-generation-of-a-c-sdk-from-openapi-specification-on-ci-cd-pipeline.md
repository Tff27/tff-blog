---
draft: true
title: Automate generation of a C# SDK from OpenApi specification on CI/CD pipeline
date: 2022-09-27T19:00:00Z
image: images/gears.jpg
slug: auto-generate-c-sharp-sdk
tags:
- '3'
- '0'
description: How to automate a C# SDK generation as part of a CI/CD pipeline using
  OpenApi specification

---
### Summary

The goal of automating repetitive work as much as possible as a developer is always present. The primary focus of any person within a company should be creating value, by creating value I mean delivering the work that has a more significant impact on the organization.

In my experience, I came across the need to build SDKs on multiple occasions, often for APIs that were delivered previously by the team. These SDKs on many occasions will be only used by solutions within a team or the company and without any bespoke requirement.

The idea to automate this piece of software came naturally, as I saw that leveraging the API specification was an alternative to getting an SDK always up to date and that is generated as part of a CI/CD pipeline.

# Getting Started

This was not the first time that I tried to automate SDK generation, past experiences failed and this automation never fully worked, so I was skeptical about going at it again but then I saw a new approach that I was willing to try.

![Suspicious Face](images/suspicious.jpg "Suspicious")

This new approach relies on the OpenApi specification, the specification that was already being implemented on every single API at the time.

## Conclusion

It's possible to automate the SDK generation as part of a CI/CD pipeline. There are some caveats that you should consider before going down this path:

* How detailed is your specification (better specifications will produce better SDKs)
* Constraints on the level of customization
  * Naming
  * Code structure
  * Feature flags
  * etc

From an engineering perspective you will be:

* Remove the burden of developing an SDK
* Your SDK will always be up to date with the latest code changes
* The SDK generation can be part of a CI/CD pipeline