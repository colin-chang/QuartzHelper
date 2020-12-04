# QuartzHelper
A helper for Quartz.Net.Make it easier to use Quartz in .Net Application.

## Nuget
```sh
# Package Manager
Install-Package ColinChang.QuartzHelper

# .NET CLI
dotnet add package ColinChang.QuartzHelper
```

About how to use this,please check the [Unit Test](https://github.com/colin-chang/QuartzHelper/tree/master/ColinChang.QuartzHelper.Test) project. 

## Compensation Mechanism

There is a task compensation mechanism in most of the timed task frameworks, such as Quartz,Hangfire.Let's briefly explain how this mechanism works.

if you change the time in your OS during running your task program,and the interval is longer than at least one period of a trigger, the quartz framework will automatically trigger the last job related to the trigger.Hard to understand ? have a look at the example below.

Suppose we have a job related to a trigger with cron `0 0 0 1 * ? *`.we start the program at `2020-1-1 0:0:0`, the job will be called immediately, then we change the time to `2020-3-2 0:0:0`, the job would be triggered once immediately, because the interval contains `2020-2-1 0:0:0` and `2020-3-1 0:0:0` and both of them have been passed away. the quartz framework automatically trigger the job for the moment `2020-3-1 0:0:0` as a compensation.
