# DataOnion
[![Nuget package](https://img.shields.io/nuget/v/DataOnion)](https://www.nuget.org/packages/DataOnion/)	[![Nuget package downloads](https://img.shields.io/nuget/dt/DataOnion?label=nuget%20downloads)](https://www.nuget.org/packages/DataOnion/)

DataOnion provides a unified collection of useful wrappers around EF Core, Dapper, and other packages that aid in accelerating early project development.

## Overview
.NET and its community of packages and libraries offers many different ways to interact with your data stores. DataOnion's goal is to offer developers a convenient way to utilize these methods of data store interaction.

### The Problem
In our API projects, we have found needs that no one library could solve. One library would have convenient features that we wanted to use, but would be too slow for time-sensitive operations, and the next would have the opposite issue. 

Eventually, we opted to use multiple libraries for interfacing with our data store. This approach fit our needs, and the project was successful. Going forward, we knew we would want to implement this strategy again. However, employing multiple libraries for what was essentially the same purpose required a lot of setup code that we would not care to repeat writing in the future. 

### Our Solution
DataOnion is a library that acts as a wrapper around EFCore and Dapper, with a high-level abstraction for Redis. This allows the user to use all three of these services with minimal setup. The library also provides fluent method chaining. 

### Code Sample
```
TODO
```

## Requirements and Installation
* This library requires the .NET6 SDK 
* To add this library to your project, run the following command


  `dotnet add package DataOnion --version 0.0.1`