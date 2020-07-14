using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace ColinChang.QuartzHelper
{
    public static class QuartzHelper
    {
        /// <summary>
        /// 获取全局唯一Scheduler(GlobalScheduler)
        /// </summary>
        /// <returns></returns>
        public static IScheduler GetScheduler() =>
            Scheduler.Singleton;

        /// <summary>
        /// 构建Scheduler配置
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection BuildSchedulerProperties(string instanceName, int threadCount = 5)
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = instanceName,

                // 设置线程池
                ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
                ["quartz.threadPool.threadCount"] = threadCount.ToString(),
                ["quartz.threadPool.threadPriority"] = "Normal"
            };

            return properties;
        }

        /// <summary>
        /// 创建一个通用Job
        /// </summary>
        /// <param name="key">Job名称和组名</param>
        /// <param name="jobDelegate">Job要执行的方法</param>
        /// <returns></returns>
        public static IJobDetail CreateSimpleJob(JobKey key, Action jobDelegate)
        {
            return JobBuilder.Create<SimpleJob>()
                .WithIdentity(key)
                .UsingJobData(new JobDataMap((IDictionary<string, object>) new Dictionary<string, object>
                    {{"jobDelegate", jobDelegate}}))
                .Build();
        }

        public static IJobDetail CreateSimpleJob(string name, string group, Action jobDelegate)
        {
            return CreateSimpleJob(JobKey.Create(name, group), jobDelegate);
        }

        /// <summary>
        /// 尝试销毁Job
        /// </summary>
        /// <returns></returns>
        /// <param name="jobTrigger"></param>
        public static async Task<bool> TryDestroyJobAsync(JobTrigger jobTrigger)
        {
            var scheduler = GetScheduler();

            await scheduler.PauseTrigger(jobTrigger.Trigger);
            await scheduler.PauseJob(jobTrigger.Job);
            var deleted = await scheduler.DeleteJob(jobTrigger.Job);
            var unscheduled = await scheduler.UnscheduleJob(jobTrigger.Trigger);

            return deleted && unscheduled;
        }

        /// <summary>
        /// 删除指定名称Job
        /// </summary>
        /// <param name="name">Job名称</param>
        /// <returns>true if the Job was found and deleted.</returns>
        public static async Task<bool> DeleteJobAsync(string name)
        {
            return await GetScheduler().DeleteJob(new JobKey(name));
        }

        /// <summary>
        /// 删除指定分组的Jobs
        /// </summary>
        /// <param name="group">Job分组名称</param>
        /// <param name="compareWith">分组名称匹配规则</param>
        /// <returns>true if all of the Jobs were found and deleted, false if one or more were not</returns>
        public static async Task<bool> DeleteJobsAsync(string group, StringOperator compareWith)
        {
            GroupMatcher<JobKey> matcher;
            if (Equals(compareWith, StringOperator.Contains))
                matcher = GroupMatcher<JobKey>.GroupContains(group);
            else if (Equals(compareWith, StringOperator.EndsWith))
                matcher = GroupMatcher<JobKey>.GroupEndsWith(group);
            else if (Equals(compareWith, StringOperator.Equality))
                matcher = GroupMatcher<JobKey>.GroupEquals(group);
            else if (Equals(compareWith, StringOperator.StartsWith))
                matcher = GroupMatcher<JobKey>.GroupStartsWith(group);
            else
                matcher = GroupMatcher<JobKey>.AnyGroup();

            return await GetScheduler().DeleteJobs((await GetScheduler().GetJobKeys(matcher)).ToList());
        }

        /// <summary>
        /// 创建一个触发器
        /// </summary>
        /// <param name="key">Trigger名称和组名</param>
        /// <param name="cron">Cron表达式</param>
        /// <param name="startTimeUtc">开始时间</param>
        /// <returns></returns>
        public static ITrigger CreateTrigger(TriggerKey key, string cron, DateTime startTimeUtc)
        {
            return TriggerBuilder.Create()
                .WithIdentity(key)
                .WithCronSchedule(cron)
                .StartAt(startTimeUtc)
                .Build();
        }

        /// <summary>
        /// 创建一个触发器
        /// </summary>
        /// <param name="key">Trigger名称和组名</param>
        /// <param name="cron">Cron表达式</param>
        /// <param name="startTimeUtc">开始时间</param>
        /// <returns></returns>
        public static ITrigger CreateTrigger(TriggerKey key, string cron, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            return TriggerBuilder.Create()
                .WithIdentity(key)
                .WithCronSchedule(cron)
                .StartAt(startTimeUtc)
                .EndAt(endTimeUtc)
                .Build();
        }

        public static ITrigger CreateTrigger(TriggerKey key, string cron)
        {
            return CreateTrigger(key, cron, DateTime.UtcNow);
        }

        public static ITrigger CreateTrigger(string name, string group, string cron)
        {
            return CreateTrigger(new TriggerKey(name, group), cron);
        }

        /// <summary>
        /// 创建一个触发器(仅执行一次)
        /// </summary>
        /// <param name="key">名称和分组</param>
        /// <param name="startTime">开始时间</param>
        /// <returns></returns>
        public static ITrigger CreateTrigger(TriggerKey key, DateTime startTime)
        {
            return TriggerBuilder.Create().WithIdentity(key).StartAt(startTime).Build();
        }

        /// <summary>
        /// 计划简单任务
        /// </summary>
        /// <param name="nameKeyword">Job名称关键字</param>
        /// <param name="jobDelegate">Job内容</param>
        /// <param name="cron">Trigger Cron表达式</param>
        public static async Task<JobTrigger> ScheduleSimpleJobAsync(string nameKeyword, Action jobDelegate, string cron)
        {
            var jobTrigger = nameKeyword.ToJobTrigger();
            await GetScheduler().DeleteJob(jobTrigger.Job);

            var job = CreateSimpleJob(jobTrigger.Job, jobDelegate);
            var trigger = CreateTrigger(jobTrigger.Trigger, cron);
            await GetScheduler().ScheduleJob(job, trigger);

            return jobTrigger;
        }


        /// <summary>
        /// 按照$"{keyword}_Job", $"{keyword}_JobGroup", $"{keyword}_Trigger", $"{keyword}_TriggerGroup"
        /// 规则生成默认Job和Trigger名称和分组名称
        /// </summary>
        /// <param name="keyword">名称关键字</param>
        /// <returns></returns>
        public static JobTrigger ToJobTrigger(
            this string keyword)
        {
            return new JobTrigger(
                $"{keyword}_Job",
                $"{keyword}_JobGroup",
                $"{keyword}_Trigger",
                $"{keyword}_TriggerGroup"
            );
        }
    }

    /// <summary>
    /// 单例Scheduler
    /// </summary>
    static class Scheduler
    {
        public static IScheduler Singleton;

        static Scheduler()
        {
            async Task Initialize()
            {
                Singleton = await new StdSchedulerFactory(
                        QuartzHelper.BuildSchedulerProperties("GlobalSchedulerClient"))
                    .GetScheduler();
                await Singleton.Start();
            }

            Initialize();
        }
    }

    class SimpleJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            if (!(context.JobDetail.JobDataMap["jobDelegate"] is Action job))
                return;

            await Task.Run(job);
        }
    }

    public class JobTrigger
    {
        public string JobName { get; }

        public string JobGroup { get; }

        public string TriggerName { get; }

        public string TriggerGroup { get; }

        public JobTrigger(string jobName, string jobGroup, string triggerName, string triggerGroup)
        {
            JobName = jobName;
            JobGroup = jobGroup;
            TriggerName = triggerName;
            TriggerGroup = triggerGroup;
        }

        public JobKey Job => JobKey.Create(JobName, JobGroup);

        public TriggerKey Trigger => new TriggerKey(TriggerName, TriggerGroup);
    }
}