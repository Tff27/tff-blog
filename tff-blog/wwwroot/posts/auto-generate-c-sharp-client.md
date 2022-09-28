---
draft: false
title: Automate generation of a C# Client from OpenApi specification on CI/CD pipeline
date: 2022-09-27T19:00:00.000+00:00
image: "/images/pipelinev2.jpg"
slug: auto-generate-c-sharp-client
tags:
- '3'
- '0'
description: How to automate a C# SDK generation as part of a CI/CD pipeline using
  OpenApi specification

---
### Summary

The goal of automating repetitive work as much as possible as a developer is always present. A primary focus of any person within a company should be creating value. By creating value I mean delivering work that has a more significant impact on the organization.

In my experience, I came across the need to build API clients on multiple occasions, often for APIs that before the team delivered. These API clients, on many occasions, will be only used by solutions within a team or the company and without any bespoke requirement.

The idea to automate this piece of software came naturally, as I saw that leveraging the API specification was an alternative to getting an API client always up to date and that is generated as part of a CI/CD pipeline.

# Getting Started

This was not the first time that I tried to automate API clients generation, past experiences failed and this automation never fully worked, so I was skeptical about going at it again but then I saw a new approach that I was willing to try.

![Suspicious Face](images/suspicious.jpg "Suspicious")

This new approach relies on the OpenApi specification, the specification that was already being implemented on every single API at the time.

## The **API project**:

On the API project, we want to produce a well-described API. The better described the API is the better the generated code will be.

Let's start by having a look in one controlled method:

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WeatherForecast))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(int))]
    public ActionResult<WeatherForecast> Post(WeatherForecastCreate weatherForecast)
    {
    	//Create operation code
    
    	return CreatedAtAction(nameof(GetById), new { id = weatherForecast.Id }, weatherForecast);
    }

The above code when used to generate an API client will consider:

* HTTP Method
* The Authorize requirement
* All the possible response codes and return types

Remember that for each method you should consider adding as many details as possible. 

Since we want to make this part of a CI/CD pipeline we will generate and save our OpenApi specification, for that we are going to use the [Swashbuckle.AspNetCore.Cli](https://www.nuget.org/packages/Swashbuckle.AspNetCore.Cli " Swashbuckle.AspNetCore.Cli") dotnet tool. To do this go to the API project location and install the tool using the following command:

    dotnet tool install swashbuckle.aspnetcore.cli

A "dotnet-tools.json" file should have been created on your project. The file should contain the following:

    {
      "version": 1,
      "isRoot": true,
      "tools": {
        "swashbuckle.aspnetcore.cli": {
          "version": "6.4.0",
          "commands": [
            "swagger"
          ]
        }
      }
    }

At the point of writing this article, "6.4.0" is the latest version.

Now let's use the tool to generate the OpenApi specification file, the following code should be added to the .csproj file

    <Target Name="SwaggerToFile" AfterTargets="Build">
    	<Exec Condition="$(Configuration)==Debug" Command="dotnet swagger tofile --output swagger.json $(OutputPath)$(AssemblyName).dll v1" />
    </Target>

What this will do is generate and save the specification file after every build, you can specify the output location. I recommend have this on an easy to access location since this is the file that we will be using to generate the API client. 

## The SDK project:

The SDK project will contain the generated code. We will need to have a couple of nuget packages installed

    <ItemGroup>
      <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
      <PackageReference Include="NSwag.ApiDescription.Client" Version="13.17.0" />
    </ItemGroup>

[NSwag](NSwag.org "NSwag") will be the tool that will handle the generation. We will need to add to our .csproj file the configurations settings for our client, such as:

* OpenApi specification location (this can be a file path or url)
* Namespace
* Output Path
* ... and others

This can be achieved by one of these two methods

#### Option 1

Add the configuration settings manually to the .csproj file. A simple configuration should look like this

    <ItemGroup>
      <OpenApiReference Include="../ClientGenerator/swagger.json" CodeGenerator="NSwagCSharp">
        <Namespace>SDK</Namespace>
        <ClassName>WeatherServiceClientSDK</ClassName>
        <OutputPath>../../SDK/WeatherServiceClientSDK.cs</OutputPath>
        <Options>/GenerateClientInterfaces:true </Options>
      </OpenApiReference>
    </ItemGroup>

#### Option 2 

Use the Visual Studio connected services, to do this using Visual Studio right click on the .csproj file > Add > Connected service

![Add connected service using visual studio](/images/connected-services.png "Add connected service")

Click on the add button for the Service References

![Service References add option](/images/connected-services-references.png "Service References")

Pick the OpenAPI option

![OpenAPI option](/images/connected-services-open-api.png "OpenAPI option")

Set the configurations accordingly

![OpenAPI settings](/images/connected-services-open-api-configs.png "OpenAPI settings")

**Note that you can also generate a client in Typescript** 

## Last, let's use our new API client in a client application

In order to use our client, we first need to reference the client (or install it if it was packed as a nuget) in our client application. Perform an API call should be simple now:

    // Created a new http client
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("ApiKey", "ApiKey123");
    
    var testClient = new WeatherServiceClientSDK("http://localhost:49183", httpClient);
    
    // Call a generated method
    var getresult = await testClient.WeatherForecastAllAsync();

**Note that I'm sending an authorization header since I protected some of my API endpoints with the need for authentication.** 

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
* Your client will always be up to date with the latest code changes
* The client generation can be part of a CI/CD pipeline
* The client can be packed as nuget package easily

The demo code can be found here:

[Tff27/Api-Client-Autogeneration: API Client Autogeneration (github.com)](https://github.com/Tff27/Api-Client-Autogeneration)